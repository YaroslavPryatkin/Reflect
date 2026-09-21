using UnityEngine.Events;

public class ResetBindingsButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => GameSettings.ResetBindings;
}
