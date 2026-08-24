using UnityEngine.Events;

public class SaveSettingsButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => GameSettings.SaveSettings;
}