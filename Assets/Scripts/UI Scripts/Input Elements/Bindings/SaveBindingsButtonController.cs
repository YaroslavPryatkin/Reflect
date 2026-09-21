using UnityEngine.Events;

public class SaveBindingsButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => GameSettings.SaveBindings;
}
