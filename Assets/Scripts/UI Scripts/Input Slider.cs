using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputSlider : MonoBehaviour, UIManager.IInitializable
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueShower;
    [SerializeField] private string valueShowerTextBefore = "Current value is ";
    [SerializeField] private string valueShowerTextAfter = "";
    [SerializeField] private int digitsAfterDot = 2;
    public float Value { get; private set; }

    private string format;
    
    public void Initialize()
    {
        slider.onValueChanged.AddListener(OnValueChanged);
        format = "F" + (digitsAfterDot > 0 ? digitsAfterDot.ToString() : "0");
        OnValueChanged(slider.value);
    }
    
    public void OnValueChanged(float value)
    {
        valueShower.text = valueShowerTextBefore + 
                           (digitsAfterDot == 0 ?  Mathf.Round(value).ToString(format) : value.ToString(format))
                           + valueShowerTextAfter;
        Value = digitsAfterDot == 0 ? Mathf.Round(slider.value) : slider.value;
    }
}
