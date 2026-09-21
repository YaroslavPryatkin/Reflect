using UnityEngine;
using System.Globalization;


[CreateAssetMenu(fileName = "FloatSettingValue", menuName = "SettingValues/FloatSettingValue")]
public class FloatSettingValue : AbstractSettingValue
{
    [SerializeField] public float baseValue;
    [SerializeField] public float value;
    [SerializeField] public float minValue;
    [SerializeField] public float maxValue;

    public float Value
    {
        get => value;
        set => this.value = Mathf.Clamp(value, minValue, maxValue);
    }
    
    public override void Set(string data)
    {
        if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            Value=result;
    }

    public override string Get() => Value.ToString("F2", CultureInfo.InvariantCulture);

    public override void SettingReset()
    {
        Value=baseValue;
        base.SettingReset();
    }
}