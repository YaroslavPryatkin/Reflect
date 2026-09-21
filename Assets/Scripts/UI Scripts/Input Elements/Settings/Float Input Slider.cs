using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatInputSlider : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private TextMeshProUGUI header; 
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueShower;
    [Header("Settings")]
    [SerializeField] private string valueShowerTextBefore = "Current value is ";
    [SerializeField] private string valueShowerTextAfter = "";
    [SerializeField] private int digitsAfterDot = 2;
    [SerializeField] private FloatSettingValue settingValue;

    private string _format;
    
    private void Awake()
    {
        if (settingValue == null)
        {
            Debug.LogError("FloatSettingValue is null");
            return;
        }
        
        if (header != null)
        {
            header.text = settingValue.name;
        }
        
        _format = "F" + (digitsAfterDot > 0 ? digitsAfterDot.ToString() : "0");
        
        slider.minValue = settingValue.minValue;
        slider.maxValue = settingValue.maxValue;
        slider.onValueChanged.AddListener(OnValueChanged);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        _format = "F" + (digitsAfterDot > 0 ? digitsAfterDot.ToString() : "0");
        
        if (header != null && settingValue!=null)
        {
            header.text = settingValue.name;
        }

        if (valueShower != null)
        {
            ChangeText(settingValue!=null ?  settingValue.Value : 0);
        }
        
        if(slider!=null && settingValue!=null)
        {
            slider.minValue = settingValue.minValue;
            slider.maxValue = settingValue.maxValue;
        }
    }
    
    public void SetupInEditor(FloatSettingValue settingValue)
    {
        this.settingValue = settingValue;
        if(slider!=null)
        {
            slider.value = settingValue.Value;
            slider.minValue = settingValue.minValue;
            slider.maxValue = settingValue.maxValue;
        }
    }
    #endif

    private void OnEnable()
    {
        UpdateUI();
        settingValue.OnExternalValueUpdated += UpdateUI;
    }

    private void OnDisable()
    {
        settingValue.OnExternalValueUpdated -= UpdateUI;
    }

    private void UpdateUI()
    {
        slider.value = settingValue.Value;
        ChangeText(settingValue.Value);
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
        settingValue.Value = digitsAfterDot == 0 ? Mathf.Round(value) : value;
        ChangeText(value);
    }

    private void ChangeText(float value)
    {
        valueShower.text = valueShowerTextBefore + 
                           (digitsAfterDot == 0 ?  Mathf.Round(value).ToString(_format) : value.ToString(_format))
                           + valueShowerTextAfter;
    }
}
