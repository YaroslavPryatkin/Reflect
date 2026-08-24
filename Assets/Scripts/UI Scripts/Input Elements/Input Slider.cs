using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueShower;
    [SerializeField] private string valueShowerTextBefore = "Current value is ";
    [SerializeField] private string valueShowerTextAfter = "";
    [SerializeField] private int digitsAfterDot = 2;
    [SerializeField] private string settingName="";

    private string _format;
    
    private void Awake()
    {
        _format = "F" + (digitsAfterDot > 0 ? digitsAfterDot.ToString() : "0");
        
        slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnEnable()
    {
        var setting = GameSettings.Get<FloatSettingsValue>(settingName);
        slider.value = setting.Value;
        ChangeText(setting.Value);
    }
    
    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnValueChanged);
        }
    }

    private void OnValueChanged(float value)
    {
        var setting = GameSettings.Get<FloatSettingsValue>(settingName);
        setting.Value = digitsAfterDot == 0 ? Mathf.Round(value) : value;
         ChangeText(value);
    }

    private void ChangeText(float value)
    {
        valueShower.text = valueShowerTextBefore + 
                           (digitsAfterDot == 0 ?  Mathf.Round(value).ToString(_format) : value.ToString(_format))
                           + valueShowerTextAfter;
    }
}
