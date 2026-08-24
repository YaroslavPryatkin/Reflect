using System.Globalization;



public class FloatSettingsValue : ISaveValue
{
    public float Value;

    public FloatSettingsValue(float value)
    {
        Value = value;
    }

    public void Set(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            Value = result;
    }

    public string Get() => Value.ToString("F2", CultureInfo.InvariantCulture);
}

public class BindSettingsValue : ISaveValue
{
    public string ActionName { get; private set; }
    public int BindingIndex { get; private set; }
    public string OverridePath { get; private set; }

    public BindSettingsValue() { }

    public BindSettingsValue(string actionName, int bindingIndex, string overridePath)
    {
        ActionName = actionName;
        BindingIndex = bindingIndex;
        OverridePath = overridePath;
    }

    public void Set(string actionName, int bindingIndex, string overridePath)
    {
        ActionName = actionName;
        BindingIndex = bindingIndex;
        OverridePath = overridePath;
    }

    public void Set(string value)
    {
        var parts = value.Split(';');
        if (parts.Length >= 3)
        {
            ActionName = parts[0];
            if (int.TryParse(parts[1], out int index))
                BindingIndex = index;
            OverridePath = parts[2];
        }
    }

    public string Get()
    {
        return $"{ActionName};{BindingIndex};{OverridePath}";
    }
}