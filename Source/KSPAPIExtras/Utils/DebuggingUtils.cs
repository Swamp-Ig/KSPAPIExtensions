﻿using System;
using System.Reflection;
using System.Text;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions.DebuggingUtils
{
    [Flags]
    public enum DumpTreeOption
    {
        None = 0x0,
        Default = 0x0,

        Active = 0x1,
        TransformPosition = 0x2,
        TransformRotation = 0x4,

        // Optons that require iterating through components should have the 0x10 bit set.
        Components = 0x30,
        Materials = 0x50,
        Mesh = 0x90,

        Rigidbody = 0x110,
    }

    public static class Debugging
    {

        public static string DumpTypeHeirachy(Type type)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Declared class: ").Append(type.AssemblyQualifiedName);
            while(true)
            {
                Type [] interfaces = type.GetInterfaces();
                if (interfaces.Length > 0)
                {
                    sb.AppendLine("    Interfaces:");
                    for (int i = 0; i < interfaces.Length; i++)
                        sb.Append("      ").Append(interfaces[i].AssemblyQualifiedName);
                }
                type = type.BaseType;
                if (type == null)
                {
                    sb.Length = sb.Length - 1;
                    return sb.ToString();               
                }
                sb.AppendLine("  Base Type: " + type.AssemblyQualifiedName);
            }
        }

        /// <summary>
        /// Produce a string that displays the heirachy and other information about a transform.
        /// </summary>
        /// <returns>String containing the dump.</returns>
        public static string DumpTree(this Transform t, DumpTreeOption options = DumpTreeOption.Default)
        {
            StringBuilder sb = new StringBuilder();
            DumpTree(t, options, 0, sb);
            return sb.ToString();
        }

        private static void DumpTree(Transform t, DumpTreeOption options, int level, StringBuilder sb)
        {
            string space = "";
            for (int i = 0; i < level; ++i)
                space += '-';
            sb.AppendLine(space + t.name);
            if ((options & DumpTreeOption.Active) == DumpTreeOption.Active)
            {
                sb.AppendLine(space + "+ activeSelf=" + t.gameObject.activeSelf + " activeInHeirachy=" + t.gameObject.activeInHierarchy);
            }
            if ((options & DumpTreeOption.TransformPosition) == DumpTreeOption.TransformPosition)
            {
                sb.AppendLine(space + "+ position: " + t.position + " localPosition: " + t.localPosition);
            }
            if ((options & DumpTreeOption.TransformRotation) == DumpTreeOption.TransformRotation)
            {
                sb.AppendLine(space + "+ rotation: " + t.rotation + " localRotation: " + t.localRotation);
            }

            if ((((int)options & 0x10) == 0))
                goto skipComponents;

            foreach (Component c in t.gameObject.GetComponents<Component>())
            {

                if (!(c is Transform) && ((options & DumpTreeOption.Components) == DumpTreeOption.Components))
                    sb.AppendLine(space + "+ component:" + c.GetType());

                if (c is Renderer && (options & DumpTreeOption.Materials) == DumpTreeOption.Materials)
                    foreach (Material m in t.renderer.sharedMaterials)
                        sb.AppendLine(space + "++ mat:" + m.name);

                if (c is MeshFilter && (options & DumpTreeOption.Mesh) == DumpTreeOption.Mesh)
                {
                    MeshFilter filter = (MeshFilter)c;
                    sb.AppendLine(space + "++ mesh:" + ((filter.sharedMesh == null) ? "*null*" : (filter.sharedMesh.name + " verts:" + filter.sharedMesh.vertexCount)));
                }

                if (c is MeshCollider && (options & DumpTreeOption.Mesh) == DumpTreeOption.Mesh)
                {
                    MeshCollider collider = (MeshCollider)c;
                    sb.AppendLine(space + "++ mesh:" + ((collider.sharedMesh == null) ? "*null*" : (collider.sharedMesh.name + " verts:" + collider.sharedMesh.vertexCount)));
                }

                if (c is Rigidbody && (options & DumpTreeOption.Rigidbody) == DumpTreeOption.Rigidbody)
                {
                    Rigidbody body = (Rigidbody)c;
                    sb.AppendLine(space + "++ Mass:" + body.mass.ToString("F3"));
                    sb.AppendLine(space + "++ CoM:" + body.centerOfMass.ToString("F3"));
                }
                if (c is Joint && (options & DumpTreeOption.Rigidbody) == DumpTreeOption.Rigidbody)
                {
                    Joint joint = (Joint)c;
                    sb.AppendLine(space + "++ anchor:" + joint.anchor.ToString("F3"));

                    sb.AppendLine(space + "++ connectedBody: " + (joint.connectedBody != null));
                    if (joint.connectedBody != null)
                        DumpTree(joint.connectedBody.transform, options, level + 1, sb);
                }
            }

        skipComponents:
            for (int i = 0; i < t.childCount; ++i)
                DumpTree(t.GetChild(i), options, level + 1, sb);
        }

        /// <summary>
        /// Create a string with the contents of a mesh.
        /// </summary>
        public static string DumpMesh(this Mesh mesh)
        {
            Vector3[] verticies = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            Vector2[] uv = mesh.uv;

            StringBuilder sb = new StringBuilder().AppendLine();
            for (int i = 0; i < mesh.vertexCount; ++i)
            {
                sb
                    .Append(verticies[i].ToString("F4")).Append(", ")
                    .Append(uv[i].ToString("F4")).Append(", ")
                    .Append(normals[i].ToString("F4")).Append(", ")
                    .Append(tangents[i].ToString("F4")).AppendLine();
            }
            sb.Replace("(", "").Replace(")", "");
            sb.AppendLine();

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                sb
                    .Append(mesh.triangles[i]).Append(',')
                    .Append(mesh.triangles[i + 1]).Append(',')
                    .Append(mesh.triangles[i + 2]).AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Dump out all the fields from an object
        /// </summary>
        public static string DumpObjectFields(this object obj, BindingFlags flags = BindingFlags.Default)
        {
            StringBuilder sb = new StringBuilder();
            Type type = obj.GetType();

            foreach (FieldInfo field in type.GetFields(flags))
            {
                object value = field.GetValue(obj);
                if (value == null)
                    sb.AppendLine(field.FieldType.Name + " " + field.Name + "is null");
                else
                    sb.AppendLine(field.FieldType.Name + " " + field.Name + " = " + value);
            }
            return sb.ToString();
        }

        public static string DumpNotEqualFields<T>(this T obj, T that, BindingFlags flags = BindingFlags.Default)
        {
            StringBuilder sb = new StringBuilder();
            if (ReferenceEquals(obj, that))
                sb.AppendLine("Same object");

            Type type = typeof(T);

            foreach (FieldInfo field in type.GetFields(flags))
            {
                object thisValue = field.GetValue(obj);
                object thatValue = field.GetValue(that);

                if (field.FieldType.IsPrimitive && Equals(thisValue, thatValue))
                    continue;
                if (ReferenceEquals(thisValue, thatValue))
                    continue;
                sb.AppendLine("Different fields: " + field.FieldType.Name + " " + field.Name + (Equals(thisValue, thatValue) ? "(compute equal)" : (" " + thisValue + " != " + thatValue)));
            }
            return sb.ToString();
        }
    }
}
