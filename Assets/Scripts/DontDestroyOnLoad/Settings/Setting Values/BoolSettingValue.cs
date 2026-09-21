using UnityEngine;

[CreateAssetMenu(fileName = "BoolSettingValue", menuName = "SettingValues/BoolSettingValue")]
public class BoolSettingValue : AbstractSettingValue
{
    [SerializeField] public bool baseValue;
    [SerializeField] public bool value;
    
    public override void Set(string data)
    {
        if (bool.TryParse(data, out var result))
            this.value = result;
    }

    public override string Get() => value.ToString();

    public override void SettingReset()
    { 
        value=baseValue;
        base.SettingReset();
    }
}