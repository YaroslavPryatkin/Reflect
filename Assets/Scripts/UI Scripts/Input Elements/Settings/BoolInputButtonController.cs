using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class BoolInputButtonController : GenericButtonController
{
    [Header("UI")] 
    [SerializeField] private Image imageOn;
    [SerializeField] private Image imageOff;
    [Header("Settings")]
    [SerializeField] private BoolSettingValue settingValue;

    protected override UnityEngine.Events.UnityAction FunctionToCall => OnClick;

    private bool _haveImageOn = false;
    private bool _haveImageOff = false;
    
    
    private void Awake()
    {
        if (settingValue == null)
        {
            Debug.LogError("BoolSettingValue is null");
            return;
        }

        if (imageOn == null && imageOff == null)
        {
            Debug.LogWarning("Both images are null");
        }

        SetHaveImage();
    }


    
    private void OnEnable()
    {
        UpdateUI();
        settingValue.OnExternalValueUpdated += UpdateUI;
    }

    private void OnDisable()
    {
        settingValue.OnExternalValueUpdated -= UpdateUI;
    }
    
#if UNITY_EDITOR
    
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        if (settingValue != null)
        {
            SetHaveImage();
            UpdateUI();
        }
    }
    
    public void SetupInEditor(BoolSettingValue settingValue)
    {
        this.settingValue = settingValue;
        if (settingValue != null)
        {
            SetHaveImage();
            UpdateUI();
        }
    }
#endif

    private void OnClick()
    {
        settingValue.value = !settingValue.value;
        UpdateUI();
    }

    private void SetHaveImage()
    {
        _haveImageOn = imageOn != null;
        _haveImageOff = imageOff != null;
    }

    private void UpdateUI()
    {
        if (_haveImageOn)
        {
            imageOn.gameObject.SetActive(settingValue.value);
        }

        if (_haveImageOff)
        {
            imageOff.gameObject.SetActive(!settingValue.value);
        }
    }
}
