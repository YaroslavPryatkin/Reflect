using UnityEngine.Events;

public class LoadSettingsButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => GameSettings.LoadValues;
}