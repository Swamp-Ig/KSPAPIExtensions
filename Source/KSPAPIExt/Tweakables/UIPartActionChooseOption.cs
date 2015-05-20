using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{
    [UI_ChooseOption]
    public class UIPartActionChooseOption : UIPartActionFieldItem
    {
        public SpriteText fieldName;
        public UIButton incDown;
        public UIButton incUp;
        public UIProgressSlider slider;

        private int selectedIdx = -1;
        //private string selectedValue;

        public static UIPartActionChooseOption CreateTemplate()
        {
            // Create the control
            GameObject editGo = new GameObject("UIPartActionChooseOption", SystemUtils.VersionTaggedType(typeof(UIPartActionChooseOption)));
            UIPartActionChooseOption edit = editGo.GetTaggedComponent<UIPartActionChooseOption>();
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
            sliderGo.transform.localScale = new Vector3(0.81f, 1, 1);
            edit.slider = sliderGo.GetComponent<UIProgressSlider>();
            edit.slider.ignoreDefault = true;

            GameObject fieldNameGo = (GameObject)Instantiate(srcTextGo);
            fieldNameGo.transform.parent = editGo.transform;
            fieldNameGo.transform.localPosition = new Vector3(24, -8, 0);
            edit.fieldName = fieldNameGo.GetComponent<SpriteText>();

            GameObject incDownGo = (GameObject)Instantiate(srcButtonGo);
            incDownGo.transform.parent = edit.transform;
            incDownGo.transform.localScale = new Vector3(0.45f, 1.1f, 1f);
            incDownGo.transform.localPosition = new Vector3(11.5f, -9, 0); //>11
            edit.incDown = incDownGo.GetComponent<UIButton>();

            GameObject incDownLabelGo = (GameObject)Instantiate(srcTextGo);
            incDownLabelGo.transform.parent = editGo.transform;
            incDownLabelGo.transform.localPosition = new Vector3(5.5f, -7, 0); // <6
            SpriteText incDownLabel = incDownLabelGo.GetComponent<SpriteText>();
            incDownLabel.Text = "<<";

            GameObject incUpGo = (GameObject)Instantiate(srcButtonGo);
            incUpGo.transform.parent = edit.transform;
            incUpGo.transform.localScale = new Vector3(0.45f, 1.1f, 1f);
            incUpGo.transform.localPosition = new Vector3(187.5f, -9, 0); // >187
            edit.incUp = incUpGo.GetComponent<UIButton>();

            GameObject incUpLabelGo = (GameObject)Instantiate(srcTextGo);
            incUpLabelGo.transform.parent = editGo.transform;
            incUpLabelGo.transform.localPosition = new Vector3(181.5f, -7, 0); //<182
            SpriteText incUpLabel = incUpLabelGo.GetComponent<SpriteText>();
            incUpLabel.Text = ">>";

            return edit;
        }

        protected UI_ChooseOption FieldInfo
        {
            get
            {
                return (UI_ChooseOption)control;
            }
        }

        // ReSharper disable ParameterHidesMember
        public override void Setup(UIPartActionWindow window, Part part, PartModule partModule, UI_Scene scene, UI_Control control, BaseField field)
        {
            base.Setup(window, part, partModule, scene, control, field);
            incDown.SetValueChangedDelegate(obj => IncrementValue(false));
            incUp.SetValueChangedDelegate(obj => IncrementValue(true));
            slider.SetValueChangedDelegate(OnValueChanged);
        }
        // ReSharper restore ParameterHidesMember

        private void IncrementValue(bool up)
        {
            if (FieldInfo.options == null || FieldInfo.options.Length == 0)
                selectedIdx = -1;
            else
                selectedIdx = (selectedIdx + FieldInfo.options.Length + (up ? 1 : -1)) % FieldInfo.options.Length;
            SetValueFromIdx();
        }

        private void OnValueChanged(IUIObject obj)
        {
            slider.SetValueChangedDelegate(null);
            if (FieldInfo.options == null || FieldInfo.options.Length == 0)
                selectedIdx = -1;
            else
                selectedIdx = Mathf.RoundToInt(slider.Value * (FieldInfo.options.Length - 1));
            SetValueFromIdx();
            slider.SetValueChangedDelegate(OnValueChanged);
        }

        private void SetValueFromIdx()
        {
            if (selectedIdx >= 0)
            {
                if (field.FieldInfo.FieldType == typeof(int))
                {
                    field.SetValue(selectedIdx, field.host);
                    if (scene == UI_Scene.Editor)
                        SetSymCounterpartValue(selectedIdx);
                }
                else
                {
                    string selectedValue = FieldInfo.options[selectedIdx];
                    field.SetValue(selectedValue, field.host);
                    if (scene == UI_Scene.Editor)
                        SetSymCounterpartValue(selectedValue);
                }
            }
            UpdateControls();
        }

        private void UpdateControls()
        {
            if (selectedIdx < 0)
            {
                fieldName.Text = "**Not Found**";
                slider.Value = 0;
                return;
            }

            if (FieldInfo.display != null && FieldInfo.display.Length > selectedIdx)
            {
                fieldName.Text = field.guiName + ": " + FieldInfo.display[selectedIdx];
            }
            else
            {
                fieldName.Text = field.guiName + ": " + FieldInfo.options[selectedIdx];
            }
            int length = (FieldInfo.options ?? FieldInfo.display).Length;
            if (length > 1)
            {
                slider.Value = selectedIdx / (float)(length - 1);
            }
            else
            {
                slider.Value = 1;
            }
        }


        bool exceptionPrinted;
        public override void UpdateItem()
        {
            try
            {
                if (field.FieldInfo.FieldType == typeof(int))
                {
                    int newSelectedIdx = field.GetValue<int>(field.host);
                    if (selectedIdx == newSelectedIdx)
                        return;

                    selectedIdx = newSelectedIdx;
                    if (FieldInfo.options == null || selectedIdx < 0 || selectedIdx >= FieldInfo.options.Length)
                        selectedIdx = -1;
                }
                else
                {
                    string newSelectedValue = field.GetValue<string>(field.host);
                    if (selectedIdx >= 0 && newSelectedValue == FieldInfo.options[selectedIdx])
                        return;

                    selectedIdx = -1;
                    for (int i = 0; i < FieldInfo.options.Length; ++i)
                        if (newSelectedValue == FieldInfo.options[i])
                        {
                            selectedIdx = i;
                            break;
                        }
                }
                UpdateControls();
                exceptionPrinted = false;
            }
            catch (Exception ex)
            {
                if (!exceptionPrinted)
                    print(ex);
                exceptionPrinted = true;
            }
        }
    }


    // ReSharper disable once InconsistentNaming
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public class UI_ChooseOption : UI_Control
    {

        public string[] options;
        public string[] display;

    }
}
