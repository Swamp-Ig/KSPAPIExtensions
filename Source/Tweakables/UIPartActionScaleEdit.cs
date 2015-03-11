using System;
using System.Globalization;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{

    [UI_ScaleEdit]
    public class UIPartActionScaleEdit : UIPartActionFieldItem
    {
        public SpriteText fieldName;
        public SpriteText fieldValue;
        public UIButton incLargeDown;
        public SpriteText incLargeDownLabel;
        public UIButton incLargeUp;
        public SpriteText incLargeUpLabel;
        public UIProgressSlider slider;

        private float value;
        public int intervalNo = 0;

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
                if (intervalNo < FieldInfo.intervals.Length - 2)
                {
                    if (newValue == FieldInfo.intervals[intervalNo+1])
                        newValue = FieldInfo.intervals[intervalNo+2];
                    intervalNo++;
                }
                else
                    newValue = FieldInfo.intervals [intervalNo+1];
            }
            else
            {
                if (intervalNo > 0)
                {
                    if (newValue == FieldInfo.intervals[intervalNo])
                        newValue = FieldInfo.intervals[intervalNo-1];
                    intervalNo--;
                }
                else
                    newValue = FieldInfo.intervals [0];
            }
            RestrictToInterval (newValue);
        }

        private void slider_OnValueChanged(IUIObject obj)
        {
            float valueLow = FieldInfo.intervals [intervalNo];
            float valueHi  = FieldInfo.intervals [intervalNo + 1];
            float newValue = Mathf.Lerp(valueLow, valueHi, slider.Value);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            float inc = GetIncrementSlide ();
            if (inc != 0)
                newValue = valueLow + Mathf.Round((newValue-valueLow) / inc) * inc;

            SetValueFromGUI(newValue);
        }

        private void OnValueChanged(float newValue)
        {
            //update intervalNo
            intervalNo = 0;

            for( int i=0; i<FieldInfo.intervals.Length-1; i++)
                if(newValue >= FieldInfo.intervals[i])
                    intervalNo = i;

            UpdateValueDisplay (newValue);
        }

        private void RestrictToInterval(float newValue)
        {
            newValue = Math.Max(newValue, FieldInfo.intervals [intervalNo]);
            newValue = Math.Min(newValue, FieldInfo.intervals [intervalNo + 1]);

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

        private float GetIncrementSlide()
        {
            if (FieldInfo.incrementSlide.Length > 1)
                return FieldInfo.incrementSlide [intervalNo];
            else if (FieldInfo.incrementSlide.Length == 1)
                return FieldInfo.incrementSlide [0];
            else
                return 0;
        }

        public override void UpdateItem()
        {
            // update from fieldName. No listeners.
            fieldName.Text = field.guiName;

            // Update the value.
            float fValue = GetFieldValue();
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (fValue != value)
                OnValueChanged(fValue);

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
            float inc = GetIncrementSlide();
            if (inc != 0)
            {
                float valueLow = FieldInfo.intervals [intervalNo];
                float valueHi = FieldInfo.intervals [intervalNo + 1];
                newValue = valueLow + Mathf.Round((newValue - valueLow) / inc) * inc;
                slider.gameObject.SetActive(true);
                slider.Value = Mathf.InverseLerp (valueLow, valueHi, newValue);
            }
            else
                slider.gameObject.SetActive(false);

            fieldValue.Text = newValue.ToStringExt(field.guiFormat) + field.guiUnits;
        }

        private void UpdateFieldInfo()
        {
            if (CheckConsistency ())
            {
                incLargeDown.gameObject.SetActive (true);
                incLargeDownLabel.gameObject.SetActive (true);
                incLargeUp.gameObject.SetActive (true);
                incLargeUpLabel.gameObject.SetActive (true);

                slider.transform.localScale = new Vector3(0.81f, 1, 1);
                fieldName.transform.localPosition = new Vector3(24, -8, 0);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (GetIncrementSlide() == 0)
                    slider.gameObject.SetActive(false);
                else
                    slider.gameObject.SetActive(true);
            }
            else
            {
                incLargeDown.gameObject.SetActive (false);
                incLargeDownLabel.gameObject.SetActive (false);
                incLargeUp.gameObject.SetActive (false);
                incLargeUpLabel.gameObject.SetActive (false);

                slider.gameObject.SetActive(false);
            }
        }

        public bool CheckConsistency()
        {
            if (FieldInfo.intervals.Length < 2)
                return false;

            if ((FieldInfo.incrementSlide.Length > 1) &&
                (FieldInfo.incrementSlide.Length < FieldInfo.intervals.Length - 1))
            {
                Debug.LogWarning("[KAE Warning] UI_ScaleEdit: not enough incrementSlide values. Using only the first." + Environment.NewLine + StackTraceUtility.ExtractStackTrace());
                float first = FieldInfo.incrementSlide[0];
                FieldInfo.incrementSlide = new float[1];
                FieldInfo.incrementSlide [0] = first;
                return true;
            }

            for (int i = 0; i < FieldInfo.intervals.Length-2; i++)
            {
                if (FieldInfo.intervals [i] == FieldInfo.intervals [i + 1])
                {
                    Debug.LogWarning("[KAE Warning] UI_ScaleEdit: duplicate value in intervals list" + Environment.NewLine + StackTraceUtility.ExtractStackTrace());
                    return false;
                }
                else if (FieldInfo.intervals [i] > FieldInfo.intervals [i + 1])
                {
                    Debug.LogWarning("[KAE Warning] UI_ScaleEdit: intervals list not sorted" + Environment.NewLine + StackTraceUtility.ExtractStackTrace());
                    return false;
                }
            }
            return true;
        }
    }

    // ReSharper disable once InconsistentNaming
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public class UI_ScaleEdit : UI_Control
    {
        private const string UIControlName = "UIPartActionScaleEdit";

        public float[] intervals = { 1, 2, 4 };
        public float[] incrementSlide = {0.02f, 0.04f };

        public float MinValue()
        {
            return intervals [0];
        }
        public float MaxValue()
        {
            return intervals [intervals.Length-1];
        }

        public override void Load(ConfigNode node, object host)
        {
            base.Load(node, host);
        }

        public override void Save(ConfigNode node, object host)
        {
            base.Save(node, host);
        }

        internal uint GetHashedState()
        {
            return ((uint)intervals.GetHashCode()) ^ ((uint)incrementSlide.GetHashCode());
        }
    }
}
