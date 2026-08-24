using UnityEngine.Events;

public class ResetToBaseSettingsButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => GameSettings.ResetToBaseSettings;
}