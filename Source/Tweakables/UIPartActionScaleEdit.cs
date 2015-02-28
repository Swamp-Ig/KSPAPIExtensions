using System;
using System.Globalization;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{

    [UI_ScaleEdit]
    public class UIPartActionScaleEdit : UIPartActionFieldItem
    {
        public float[] intervals = { 0.625f, 1.25f, 2.5f, 5f };
        public float[] incrementSlide = {0.01f, 0.025f, 0.05f };
        public int intervalNo = 0;

        public SpriteText fieldName;
        public SpriteText fieldValue;
        public UIButton incLargeDown;
        public SpriteText incLargeDownLabel;
        public UIButton incLargeUp;
        public SpriteText incLargeUpLabel;
        public UIProgressSlider slider;

        private float value;
        private uint controlState;

        public static UIPartActionScaleEdit CreateTemplate()
        {
            // Create the control
            GameObject editGo = new GameObject("UIPartActionScaleEdit", SystemUtils.VersionTaggedType(typeof(UIPartActionScaleEdit)));
            UIPartActionScaleEdit edit = editGo.GetTaggedComponent<UIPartActionScaleEdit>();
            editGo.SetActive(false);

            // TODO: since I don'type have access to EZE GUI, I'm copying out bits from other existing GUIs 
            // if someone does have access, they could do this better although really it works pretty well.
            UIPartActionButton evtp = UIPartActionController.Instance.eventItemPrefab;
            GameObject srcTextGo = evtp.transform.Find("Text").gameObject;
            GameObject srcBackgroundGo = evtp.transform.Find("Background").gameObject;
            GameObject srcButtonGo = evtp.transform.Find("Btn").gameObject;

            UIPartActionFloatRange paFlt = (UIPartActionFloatRange)UIPartActionController.Instance.fieldPrefabs.Find(cls => cls.GetType() == typeof(UIPartActionFloatRange));
            GameObject srcSliderGo = paFlt.transform.Find("Slider").gameObject;


            // Start building our control
            GameObject backgroundGo = (GameObject)Instantiate(srcBackgroundGo);
            backgroundGo.transform.parent = editGo.transform;

            GameObject sliderGo = (GameObject)Instantiate(srcSliderGo);
            sliderGo.transform.parent = editGo.transform;
            sliderGo.transform.localScale = new Vector3(0.65f, 1, 1);
            edit.slider = sliderGo.GetComponent<UIProgressSlider>();
            edit.slider.ignoreDefault = true;


            GameObject fieldNameGo = (GameObject)Instantiate(srcTextGo);
            fieldNameGo.transform.parent = editGo.transform;
            fieldNameGo.transform.localPosition = new Vector3(40, -8, 0);
            edit.fieldName = fieldNameGo.GetComponent<SpriteText>();

            GameObject fieldValueGo = (GameObject)Instantiate(srcTextGo);
            fieldValueGo.transform.parent = editGo.transform;
            fieldValueGo.transform.localPosition = new Vector3(110, -8, 0);
            edit.fieldValue = fieldValueGo.GetComponent<SpriteText>();


            GameObject incLargeDownGo = (GameObject)Instantiate(srcButtonGo);
            incLargeDownGo.transform.parent = edit.transform;
            incLargeDownGo.transform.localScale = new Vector3(0.45f, 1.1f, 1f);
            incLargeDownGo.transform.localPosition = new Vector3(11.5f, -9, 0); //>11
            edit.incLargeDown = incLargeDownGo.GetComponent<UIButton>();

            GameObject incLargeDownLabelGo = (GameObject)Instantiate(srcTextGo);
            incLargeDownLabelGo.transform.parent = editGo.transform;
            incLargeDownLabelGo.transform.localPosition = new Vector3(5.5f, -7, 0); // <6
            edit.incLargeDownLabel = incLargeDownLabelGo.GetComponent<SpriteText>();
            edit.incLargeDownLabel.Text = "<<";

            GameObject incLargeUpGo = (GameObject)Instantiate(srcButtonGo);
            incLargeUpGo.transform.parent = edit.transform;
            incLargeUpGo.transform.localScale = new Vector3(0.45f, 1.1f, 1f);
            incLargeUpGo.transform.localPosition = new Vector3(187.5f, -9, 0); // >187
            edit.incLargeUp = incLargeUpGo.GetComponent<UIButton>();

            GameObject incLargeUpLabelGo = (GameObject)Instantiate(srcTextGo);
            incLargeUpLabelGo.transform.parent = editGo.transform;
            incLargeUpLabelGo.transform.localPosition = new Vector3(181.5f, -7, 0); //<182
            edit.incLargeUpLabel = incLargeUpLabelGo.GetComponent<SpriteText>();
            edit.incLargeUpLabel.Text = ">>";
            return edit;
        }


        protected UI_ScaleEdit FieldInfo
        {
            get
            {
                return (UI_ScaleEdit)control;
            }
        }

        // ReSharper disable ParameterHidesMember
        public override void Setup(UIPartActionWindow window, Part part, PartModule partModule, UI_Scene scene, UI_Control control, BaseField field)
        {
            base.Setup(window, part, partModule, scene, control, field);
            incLargeDown.SetValueChangedDelegate(obj => buttons_ValueChanged(false));
            incLargeUp.SetValueChangedDelegate(obj => buttons_ValueChanged(true));
            slider.SetValueChangedDelegate(slider_OnValueChanged);

            // so update runs.
            value = GetFieldValue() + 0.1f;
            UpdateFieldInfo();
        }
        // ReSharper restore ParameterHidesMember

        private void buttons_ValueChanged(bool up)
        {
            float newValue = this.value;
            if (up) 
            {
                if (intervalNo < intervals.Length - 2)
                    intervalNo++;
                else
                    newValue = intervals [intervals.Length - 1];
            }
            else
            {
                if (intervalNo > 0)
                    intervalNo--;
                else
                    newValue = intervals [0];
            }
            RestrictToInterval (newValue);
        }

        private void slider_OnValueChanged(IUIObject obj)
        {
            float valueLow = intervals [intervalNo];
            float valueHi  = intervals [intervalNo + 1];
            float newValue = Mathf.Lerp(valueLow, valueHi, slider.Value);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (incrementSlide[intervalNo] != 0)
                newValue = Mathf.Round(newValue / incrementSlide[intervalNo]) * incrementSlide[intervalNo];

            RestrictToInterval(newValue);
        }

        private void RestrictToInterval(float newValue)
        {
            newValue = Math.Max(newValue, intervals [intervalNo]);
            newValue = Math.Min(newValue, intervals [intervalNo + 1]);

            SetValueFromGUI(newValue);
        }

        private void SetValueFromGUI(float newValue)
        {
            UpdateValueDisplay(newValue);

            field.SetValue(newValue, field.host);
            if (scene == UI_Scene.Editor)
                SetSymCounterpartValue(newValue);
        }

        private float GetFieldValue()
        {
            return isModule ? field.GetValue<float>(partModule) : field.GetValue<float>(part);
        }

        public override void UpdateItem()
        {
            // update from fieldName. No listeners.
            fieldName.Text = field.guiName;

            // Update the value.
            float fValue = GetFieldValue();
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (fValue != value)
                UpdateValueDisplay(fValue);

            uint newHash = FieldInfo.GetHashedState();
            if (controlState != newHash)
            {
                UpdateFieldInfo();
                controlState = newHash;
            }
        }

        private void UpdateValueDisplay(float newValue)
        {
            this.value = newValue;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            float inc = incrementSlide [intervalNo];
            if (inc != 0)
                newValue = Mathf.Round(newValue / inc) * inc;

            float valueLow = intervals [intervalNo];
            float valueHi  = intervals [intervalNo+1];
            slider.Value = Mathf.InverseLerp(valueLow, valueHi, newValue);

            fieldValue.Text = newValue.ToStringExt(field.guiFormat) + field.guiUnits;
        }

        private void UpdateFieldInfo()
        {
            incLargeDown.gameObject.SetActive(true);
            incLargeDownLabel.gameObject.SetActive(true);
            incLargeUp.gameObject.SetActive(true);
            incLargeUpLabel.gameObject.SetActive(true);

            slider.transform.localScale = new Vector3(0.81f, 1, 1);
            fieldName.transform.localPosition = new Vector3(24, -8, 0); //>23

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (incrementSlide[intervalNo] == 0)
                slider.gameObject.SetActive(false);
            else
                slider.gameObject.SetActive(true);
        }
    }

    // ReSharper disable once InconsistentNaming
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public class UI_ScaleEdit : UI_Control
    {
        private const string UIControlName = "UIPartActionScaleEdit";

        public float minValue = float.NegativeInfinity;
        public float maxValue = float.PositiveInfinity;

        public override void Load(ConfigNode node, object host)
        {
            base.Load(node, host);
            if (!ParseFloat(out minValue, node, "minValue", UIControlName, null))
            {
                minValue = float.NegativeInfinity;
            }
            if (!ParseFloat(out maxValue, node, "maxValue", UIControlName, null))
            {
                maxValue = float.PositiveInfinity;
            }
        }

        public override void Save(ConfigNode node, object host)
        {
            base.Save(node, host);
            if (!float.IsNegativeInfinity(minValue))
                node.AddValue("minValue", minValue.ToString(CultureInfo.InvariantCulture));
            if (!float.IsPositiveInfinity(maxValue))
                node.AddValue("maxValue", maxValue.ToString(CultureInfo.InvariantCulture));
        }

        internal unsafe uint GetHashedState()
        {
            // ReSharper disable LocalVariableHidesMember
            fixed (float* minValue = &this.minValue, maxValue = &this.maxValue)
            {
                return *((uint*)minValue) ^ *((uint*)maxValue);
            }
            // ReSharper restore LocalVariableHidesMember
        }
    }
}
