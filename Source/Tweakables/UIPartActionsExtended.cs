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
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    internal class UIPartActionsExtendedRegistration : MonoBehaviour
    {

        private static EventData<GameScenes>.OnEvent sceneChangedListener;

        internal void Start()
        {
            // Do the version election
            // If we are loaded from the first loaded assembly that has this class, then we are responsible to destroy
            var candidates = from ass in AssemblyLoader.loadedAssemblies
                             where ass.assembly.GetType(typeof(UIPartActionsExtendedRegistration).FullName, false) != null
                             orderby ass.assembly.GetName().Version descending, ass.path ascending
                             select ass;
            bool winner = candidates.First().assembly == Assembly.GetExecutingAssembly();


            // If we are the winner, then we need to register the label and resource editor controlls
            // If not then we still need to register the float edit and choose option controlls as the
            // types will not compare equal between different versions.
            if (sceneChangedListener == null)
            {
                sceneChangedListener = scene => Register(winner);
                GameEvents.onGameSceneLoadRequested.Add(sceneChangedListener);
            }
        }

        internal void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(sceneChangedListener);
            sceneChangedListener = null;
        }

        internal static void Register(bool registerLabels)
        {
            if (!(GameSceneFilter.AnyEditor | GameSceneFilter.Flight).IsLoaded())
                return;

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

            // Register prefabs. This needs to be done for every version of the assembly.
            controller.fieldPrefabs.Add(UIPartActionFloatEdit.CreateTemplate());
            fieldPrefabTypes.Add(typeof(UI_FloatEdit));

            controller.fieldPrefabs.Add(UIPartActionChooseOption.CreateTemplate());
            fieldPrefabTypes.Add(typeof(UI_ChooseOption));

            // Register the label and resource editor fields. This should only be done by the most recent version.
            if (registerLabels)
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
            PartMessageFinder.Service.SendPartMessage(part, typeof(PartResourceInitialAmountChanged), resource);
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
                PartMessageFinder.Service.SendPartMessage(sym, typeof(PartResourceInitialAmountChanged), symResource);
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