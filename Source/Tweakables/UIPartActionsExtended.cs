using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using KSPAPIExtensions.PartMessage;
using KSPAPIExtensions.DebuggingUtils;

namespace KSPAPIExtensions
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    internal class UIPartActionsExtendedEditorRegistrationAddon : MonoBehaviour
    {
        public void Start()
        {
            UIPartActionsExtendedRegistration.Register();
        }
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal class UIPartActionsExtendedFlightRegistrationAddon : MonoBehaviour
    {
        public void Start()
        {
            UIPartActionsExtendedRegistration.Register();
        }
    }

    internal static class UIPartActionsExtendedRegistration
    {
        private static bool? master;

        private static bool CheckMaster()
        {
            // Do the version election
            // If we are loaded from the first loaded assembly that has this class, then we are responsible to destroy
            var candidates = from ass in AssemblyLoader.loadedAssemblies
                             where ass.assembly.GetType(typeof(UIPartActionsExtendedRegistration).FullName, false) != null
                             orderby ass.assembly.GetName().Version descending, ass.path ascending
                             select ass;
            return candidates.First().assembly == Assembly.GetExecutingAssembly();
        }

        internal static void Register()
        {
            if (master == null)
                master = CheckMaster();

            UIPartActionController controller = UIPartActionController.Instance;
            if (controller == null)
            {
                Debug.LogError("Controller instance is null");
                return;
            }

            FieldInfo typesField = (from fld in controller.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                    where fld.FieldType == typeof(List<Type>)
                                    select fld).First();
            List<Type> fieldPrefabTypes = (List<Type>)typesField.GetValue(controller);

            // Register prefabs. This needs to be done for every version of the assembly. (the types might be called the same, but they aren't the same)
            controller.fieldPrefabs.Add(UIPartActionFloatEdit.CreateTemplate());
            fieldPrefabTypes.Add(typeof(UI_FloatEdit));

            controller.fieldPrefabs.Add(UIPartActionChooseOption.CreateTemplate());
            fieldPrefabTypes.Add(typeof(UI_ChooseOption));

            // Register the label and resource editor fields. This should only be done by the most recent version.
            if (GameSceneFilter.AnyEditor.IsLoaded() && (master ?? (master = CheckMaster())).Value)
            {
                int idx = controller.fieldPrefabs.FindIndex(item => item.GetType() == typeof(UIPartActionLabel));
                controller.fieldPrefabs[idx] = UIPartActionLabelImproved.CreateTemplate((UIPartActionLabel)controller.fieldPrefabs[idx]);

                controller.resourceItemEditorPrefab = UIPartActionResourceEditorImproved.CreateTemplate(controller.resourceItemEditorPrefab);
            }
        }
    }


    internal class UIPartActionResourceEditorImproved : UIPartActionResourceEditor
    {
        public override void Setup(UIPartActionWindow window, Part part, UI_Scene scene, UI_Control control, PartResource resource)
        {
            double amount = resource.amount;
            base.Setup(window, part, scene, control, resource);
            this.resource.amount = amount;

            slider.SetValueChangedDelegate(OnSliderChanged);
        }

        private float oldSliderValue;

        public override void UpdateItem()
        {
            base.UpdateItem();

            SIPrefix prefix = (resource.maxAmount).GetSIPrefix();
            Func<double, string> Formatter = prefix.GetFormatter(resource.maxAmount, sigFigs: 4);

            resourceMax.Text = Formatter(resource.maxAmount) + " " + prefix.PrefixString();
            resourceAmnt.Text = Formatter(resource.amount);

            oldSliderValue = slider.Value = (float)(resource.amount / resource.maxAmount);
        }

        private void OnSliderChanged(IUIObject obj)
        {
            if (oldSliderValue == slider.Value)
                return;
            oldSliderValue = slider.Value;

            SIPrefix prefix = resource.maxAmount.GetSIPrefix();
            resource.amount = prefix.Round((double)slider.Value * this.resource.maxAmount, sigFigs:4);
            PartMessageFinder.Service.SendPartMessage(this, part, typeof(PartResourceInitialAmountChanged), resource);
            if (this.scene == UI_Scene.Editor)
                SetSymCounterpartsAmount(resource.amount);
        }

        protected new void SetSymCounterpartsAmount(double amount)
	    {
            if (part == null)
                return;

            foreach (Part sym in part.symmetryCounterparts)
            {
                if (sym == part)
                    continue;
                PartResource symResource = sym.Resources[resource.info.name];
                symResource.amount = amount;
                PartMessageFinder.Service.SendPartMessage(this, sym, typeof(PartResourceInitialAmountChanged), symResource);
            }
	    }

        internal static UIPartActionResourceEditorImproved CreateTemplate(UIPartActionResourceEditor oldEditor)
        {
            GameObject editGo = (GameObject)Instantiate(oldEditor.gameObject);
            Destroy(editGo.GetComponent<UIPartActionResourceEditor>());
            UIPartActionResourceEditorImproved edit = editGo.AddComponent<UIPartActionResourceEditorImproved>();
            editGo.SetActive(false);
            edit.transform.parent = oldEditor.transform.parent;
            edit.transform.localPosition = oldEditor.transform.localPosition;

            // Find all the bits.
            edit.slider = editGo.transform.Find("Slider").GetComponent<UIProgressSlider>();
            edit.resourceAmnt = editGo.transform.Find("amnt").GetComponent<SpriteText>();
            edit.resourceName = editGo.transform.Find("name").GetComponent<SpriteText>();
            edit.resourceMax = editGo.transform.Find("total").GetComponent<SpriteText>();
            edit.flowBtn = editGo.transform.Find("StateBtn").GetComponent<UIStateToggleBtn>();

            return edit;
        }
    }


    internal class UIPartActionLabelImproved : UIPartActionLabel
    {
        private SpriteText label;
        private void Awake()
        {
            label = base.gameObject.GetComponentInChildren<SpriteText>();
        }

        public override void UpdateItem()
        {
            object target = isModule ? (object)partModule : part;

            Type fieldType = field.FieldInfo.FieldType;
            if (fieldType == typeof(double))
            {
                double value = (double)field.FieldInfo.GetValue(target);
                label.Text = (string.IsNullOrEmpty(field.guiName) ? field.name : field.guiName) + " " +
                    (string.IsNullOrEmpty(field.guiFormat) ? value.ToString() : value.ToStringExt(field.guiFormat))
                    + field.guiUnits;
            }
            if (fieldType == typeof(float))
            {
                float value = (float)field.FieldInfo.GetValue(target);
                label.Text = (string.IsNullOrEmpty(field.guiName) ? field.name : field.guiName) + " " +
                    (string.IsNullOrEmpty(field.guiFormat) ? value.ToString() : value.ToStringExt(field.guiFormat))
                    + field.guiUnits;
            }
            else
                label.Text = this.field.GuiString(target);
        }

        internal static UIPartActionLabelImproved CreateTemplate(UIPartActionLabel oldLabel)
        {
            GameObject labelGo = (GameObject)Instantiate(oldLabel.gameObject);
            Destroy(labelGo.GetComponent<UIPartActionLabel>());
            UIPartActionLabelImproved label = labelGo.AddComponent<UIPartActionLabelImproved>();
            labelGo.SetActive(false);
            label.transform.parent = oldLabel.transform.parent;
            label.transform.localPosition = oldLabel.transform.localPosition;
            
            return label;
        }
    }

}