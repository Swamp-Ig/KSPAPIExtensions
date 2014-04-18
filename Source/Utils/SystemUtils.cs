using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace KSPAPIExtensions
{
    public static class SystemUtils
    {

        private static ModuleBuilder moduleBuilder;
        private static Dictionary<ProxyTypeKey, Type> generated = new Dictionary<ProxyTypeKey,Type>();

        private sealed class ProxyTypeKey
        {
            public Type baseType;
            public Type interfaceType;
            public Type targetType;

            public override bool Equals(object obj)
            {
                ProxyTypeKey other = obj as ProxyTypeKey;
                if (other == null)
                    return false;
                return baseType.Equals(other.baseType)
                    && interfaceType.Equals(other.interfaceType)
                    && targetType.Equals(other.targetType);
            }

            public override int GetHashCode()
            {
                return baseType.GetHashCode()
                    ^ interfaceType.GetHashCode() 
                    ^ targetType.GetHashCode();
            }

            public override string ToString()
            {
                return baseType.AssemblyQualifiedName + ":"
                    + interfaceType.AssemblyQualifiedName + ":"
                    + targetType.AssemblyQualifiedName;
            }
        }

        public static object BuildProxy(Type interfaceType, Type baseType, object target)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Proxy type must be interface");
            if(!interfaceType.IsAssignableFrom(baseType))
                throw new ArgumentException("Base type does not implement interface: " + interfaceType, "baseType");

            ProxyTypeKey key = new ProxyTypeKey() {
                baseType = baseType ?? typeof(object), 
                interfaceType = interfaceType, 
                targetType = target.GetType() 
            };

            Type proxyType;
            if (!generated.TryGetValue(key, out proxyType))
                proxyType = DefineClass(key);

            ConstructorInfo info = proxyType.GetConstructor(new Type[] { key.targetType });
            return info.Invoke(new object[] { target });
        }

        private static Type DefineClass(ProxyTypeKey key)
        {
            if (moduleBuilder == null)
            {
                AssemblyName aName = new AssemblyName("BuildProxy");
                AssemblyBuilder ab =
                    AppDomain.CurrentDomain.DefineDynamicAssembly(
                        aName,
                        AssemblyBuilderAccess.Run);

                moduleBuilder = ab.DefineDynamicModule(aName.Name);
            }

            TypeBuilder typeBuilder = moduleBuilder.DefineType(key.ToString(), TypeAttributes.Public, key.baseType, new Type[] { key.interfaceType });

            // Private value field
            FieldBuilder fbTarget = typeBuilder.DefineField("target", key.targetType, FieldAttributes.Private);

            // Single argument constructor
            DefineConstructor(key, typeBuilder, fbTarget);

            foreach (MethodInfo methodInfo in key.interfaceType.GetMethods())
                DefineMethod(key, typeBuilder, fbTarget, methodInfo);

            foreach (PropertyInfo propertyInfo in key.interfaceType.GetProperties())
                DefineProperty(key, typeBuilder, fbTarget, propertyInfo);


            Type proxyType = typeBuilder.CreateType();
            generated[key] = proxyType;
            return proxyType;
        }

        private static void DefineProperty(ProxyTypeKey key, TypeBuilder typeBuilder, FieldBuilder fbTarget, PropertyInfo propertyInfo)
        {
            ParameterInfo[] propParams = propertyInfo.GetIndexParameters();
            Type[] args;
            Type[][] argsRequiredParams;
            Type[][] argsOptionalParams;
            BuildParameters(propParams, out args, out argsRequiredParams, out argsOptionalParams);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(key.interfaceType.FullName + "." + propertyInfo.Name, propertyInfo.Attributes,
                propertyInfo.PropertyType, propertyInfo.GetRequiredCustomModifiers(), propertyInfo.GetOptionalCustomModifiers(),
                args, argsRequiredParams, argsOptionalParams);

            if (propertyInfo.CanRead)
                propertyBuilder.SetGetMethod(DefineMethod(key, typeBuilder, fbTarget, propertyInfo.GetGetMethod()));
            if (propertyInfo.CanWrite)
                propertyBuilder.SetSetMethod(DefineMethod(key, typeBuilder, fbTarget, propertyInfo.GetSetMethod()));
        }

        private static void DefineConstructor(ProxyTypeKey key, TypeBuilder typeBuilder, FieldBuilder fbTarget)
        {
            ConstructorBuilder ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { key.targetType });
            ILGenerator ctorBody = ctor.GetILGenerator();
            // Call the base constructor
            ctorBody.Emit(OpCodes.Ldarg_0);
            ctorBody.Emit(OpCodes.Call, key.baseType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
            // Assign the first argument (Ldarg_1) to the target field.
            ctorBody.Emit(OpCodes.Ldarg_0);
            ctorBody.Emit(OpCodes.Ldarg_1);
            ctorBody.Emit(OpCodes.Stfld, fbTarget);
            ctorBody.Emit(OpCodes.Ret);
        }

        private static MethodBuilder DefineMethod(ProxyTypeKey key, TypeBuilder typeBuilder, FieldBuilder fbTarget, MethodInfo methodInfo)
        {
            ParameterInfo[] ifParams = methodInfo.GetParameters();
            Type[] args;
            Type[][] argsRequiredParams;
            Type[][] argsOptionalParams;
            BuildParameters(ifParams, out args, out argsRequiredParams, out argsOptionalParams);

            // Try explicit interface first (I think these are always private, but hey)
            MethodInfo targetMethod = key.targetType.GetMethod(key.interfaceType.FullName + "." + methodInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding,
                null, args, null);
            // If that fails, try straight name
            if(targetMethod == null)
                targetMethod = key.targetType.GetMethod(methodInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding,
                    null, args, null);
            // Otherwise give up
            if (targetMethod == null)
                throw new ArgumentException("Target object of type: " + key.targetType.AssemblyQualifiedName + " does not implement appropriate method: " + methodInfo.Name + " from interface: " + key.interfaceType.AssemblyQualifiedName, "target");

            MethodBuilder method = typeBuilder.DefineMethod(
                key.interfaceType.FullName + "." + methodInfo.Name, // Explicitly implement interface in the proxy
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                CallingConventions.Standard,
                methodInfo.ReturnType, methodInfo.ReturnParameter.GetRequiredCustomModifiers(), methodInfo.ReturnParameter.GetOptionalCustomModifiers(),
                args, argsRequiredParams, argsOptionalParams
                );

            for (int i = 0; i < ifParams.Length; i++)
            {
                ParameterBuilder param = method.DefineParameter(i + 1, ifParams[i].Attributes, ifParams[i].Name);
                if (ifParams[i].IsOptional)
                    param.SetConstant(ifParams[i].DefaultValue);
            }

            ILGenerator methodBody = method.GetILGenerator();

            // Push the target field onto the stack
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Ldfld, fbTarget);

            // Push the arguments
            for (byte i = 0; i < ifParams.Length; ++i)
                methodBody.Emit(OpCodes.Ldarg_S, i);

            // Call the target method
            methodBody.Emit(OpCodes.Callvirt, targetMethod);

            // The return value, if any, will be on the stack
            methodBody.Emit(OpCodes.Ret);

            return method;
        }

        private static void BuildParameters(ParameterInfo[] ifParams, out Type[] args, out Type[][] argsRequiredParams, out Type[][] argsOptionalParams)
        {
            args = new Type[ifParams.Length];
            argsRequiredParams = new Type[ifParams.Length][];
            argsOptionalParams = new Type[ifParams.Length][];
            for (int i = 0; i < ifParams.Length; i++)
            {

                args[i] = ifParams[i].ParameterType;
                argsRequiredParams[i] = ifParams[i].GetRequiredCustomModifiers();
                argsOptionalParams[i] = ifParams[i].GetOptionalCustomModifiers();
            }
        }

    }

    interface I
    {
        void MethodA();

        void MethodB();

        int MethodC(int arg, object arg2);

        int MethodD(int arg);

        int MethodE(int arg1, ref int arg2, out int arg3);
    }

    public class A : I
    {
        object target = new B();

        public void MethodA()
        {
            ((B)target).MethodA();
        }

        void I.MethodB()
        {
            ((B)target).MethodB();
        }

        public int MethodC(int arg, object arg2)
        {
            return ((B)target).MethodC(arg, arg2);
        }

        public int MethodD(int arg)
        {
            return ((B)target).MethodD(arg);
        }

        public int MethodE(int arg1, ref int arg2, out int arg3)
        {
            return ((B)target).MethodE(arg1, ref arg2, out arg3);
        }

        public void Blah() { }
    }


    public class B
    {
        public void MethodA()
        {

        }

        virtual public void MethodB()
        {

        }

        public int MethodC(int arg, object arg2)
        {
            return 0;
        }

        public virtual int MethodD(int arg)
        {
            return arg;
        }

        public int MethodE(int arg1, ref int arg2, out int arg3)
        {
            arg3 = arg1;
            return arg1;
        }
    }
}
