using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSPAPIExtensions.PartMessage;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{
    internal class UIPartActionsExtendedRegistration : MonoBehaviour
    {
        private static bool loaded;
        private static bool isLatestVersion;
        private bool isRunning;

        public void Start()
        {
            if (loaded)
            {
                // prevent multiple copies of same object
                Destroy(gameObject);
                return;
            }
            loaded = true;

            DontDestroyOnLoad(gameObject);

            isLatestVersion = SystemUtils.RunTypeElection(typeof(UIPartActionsExtendedRegistration), "KSPAPIExtensions");
        }

        public void OnLevelWasLoaded(int level)
        {
            if(isRunning)
                StopCoroutine("Register");
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;
            isRunning = true;
            StartCoroutine("Register");
        }

        internal IEnumerator Register()
        {
            UIPartActionController controller;
            while((controller = UIPartActionController.Instance) == null)
                yield return false;

            FieldInfo typesField = (from fld in controller.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                    where fld.FieldType == typeof(List<Type>)
                                    select fld).First();
            List<Type> fieldPrefabTypes;
            while((fieldPrefabTypes = (List<Type>)typesField.GetValue(controller)) == null
                || fieldPrefabTypes.Count == 0 
                || !UIPartActionController.Instance.fieldPrefabs.Find(cls => cls.GetType() == typeof(UIPartActionFloatRange)))
                yield return false;

            Debug.Log("[KAE] Registering field prefabs for version " + Assembly.GetExecutingAssembly().GetName().Version + (isLatestVersion?" (latest)":""));

            // Register prefabs. This needs to be done for every version of the assembly. (the types might be called the same, but they aren't the same)
            controller.fieldPrefabs.Add(UIPartActionFloatEdit.CreateTemplate());
            fieldPrefabTypes.Add(typeof(UI_FloatEdit));

            controller.fieldPrefabs.Add(UIPartActionScaleEdit.CreateTemplate());
            fieldPrefabTypes.Add(typeof(UI_ScaleEdit));

            controller.fieldPrefabs.Add(UIPartActionChooseOption.CreateTemplate());
            fieldPrefabTypes.Add(typeof(UI_ChooseOption));

            // Register the label and resource editor fields. This should only be done by the most recent version.
            if (isLatestVersion && GameSceneFilter.AnyEditor.IsLoaded())
            {
                int idx = controller.fieldPrefabs.FindIndex(item => item.GetType() == typeof (UIPartActionLabel));
                controller.fieldPrefabs[idx] = UIPartActionLabelImproved.CreateTemplate((UIPartActionLabel)controller.fieldPrefabs[idx]);
                controller.resourceItemEditorPrefab = UIPartActionResourceEditorImproved.CreateTemplate(controller.resourceItemEditorPrefab);
            }
            isRunning = false;
        }
    }


    internal class UIPartActionResourceEditorImproved : UIPartActionResourceEditor
    {
        // ReSharper disable ParameterHidesMember
        public override void Setup(UIPartActionWindow window, Part part, UI_Scene scene, UI_Control control, PartResource resource)
        {
            double amount = resource.amount;
            base.Setup(window, part, scene, control, resource);
            this.resource.amount = amount;

            slider.SetValueChangedDelegate(OnSliderChanged);
        }
        // ReSharper restore ParameterHidesMember

        private float oldSliderValue;

        public override void UpdateItem()
        {
            base.UpdateItem();

            SIPrefix prefix = (resource.maxAmount).GetSIPrefix();
            // ReSharper disable once InconsistentNaming
            Func<double, string> Formatter = prefix.GetFormatter(resource.maxAmount, sigFigs: 4);

            resourceMax.Text = Formatter(resource.maxAmount) + " " + prefix.PrefixString();
            resourceAmnt.Text = Formatter(resource.amount);

            oldSliderValue = slider.Value = (float)(resource.amount / resource.maxAmount);
        }

        private void OnSliderChanged(IUIObject obj)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (oldSliderValue == slider.Value)
                return;
            oldSliderValue = slider.Value;

            SIPrefix prefix = resource.maxAmount.GetSIPrefix();
            resource.amount = prefix.Round(slider.Value * resource.maxAmount, digits:4);
            PartMessageService.Send<PartResourceInitialAmountChanged>(this, part, resource, resource.amount);
            if (scene == UI_Scene.Editor)
                SetSymCounterpartsAmount(resource.amount);
            resourceAmnt.Text = resource.amount.ToString("F1");
            GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
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
                PartMessageService.Send<PartResourceInitialAmountChanged>(this, sym, symResource, symResource.amount);
            }
        }

        internal static UIPartActionResourceEditorImproved CreateTemplate(UIPartActionResourceEditor oldEditor)
        {
            GameObject editGo = (GameObject)Instantiate(oldEditor.gameObject);
            Destroy(editGo.GetComponent<UIPartActionResourceEditor>());
            UIPartActionResourceEditorImproved edit = editGo.AddTaggedComponent<UIPartActionResourceEditorImproved>();
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

        public void Awake()
        {
            label = gameObject.GetComponentInChildren<SpriteText>();
        }

        public override void UpdateItem()
        {
            object target = isModule ? (object)partModule : part;

            Type fieldType = field.FieldInfo.FieldType;
            if (fieldType == typeof(double))
            {
                double value = (double)field.FieldInfo.GetValue(target);
                label.Text = (string.IsNullOrEmpty(field.guiName) ? field.name : field.guiName) + " " +
                    (string.IsNullOrEmpty(field.guiFormat) ? value.ToString(CultureInfo.CurrentUICulture) : value.ToStringExt(field.guiFormat))
                    + field.guiUnits;
            }
            if (fieldType == typeof(float))
            {
                float value = (float)field.FieldInfo.GetValue(target);
                label.Text = (string.IsNullOrEmpty(field.guiName) ? field.name : field.guiName) + " " +
                    (string.IsNullOrEmpty(field.guiFormat) ? value.ToString(CultureInfo.CurrentUICulture) : value.ToStringExt(field.guiFormat))
                    + field.guiUnits;
            }
            else
                label.Text = field.GuiString(target);
        }

        internal static UIPartActionLabelImproved CreateTemplate(UIPartActionLabel oldLabel)
        {
            GameObject labelGo = (GameObject)Instantiate(oldLabel.gameObject);
            Destroy(labelGo.GetComponent<UIPartActionLabel>());
            UIPartActionLabelImproved label = labelGo.AddTaggedComponent<UIPartActionLabelImproved>();
            labelGo.SetActive(false);
            label.transform.parent = oldLabel.transform.parent;
            label.transform.localPosition = oldLabel.transform.localPosition;
            
            return label;
        }
    }

}
