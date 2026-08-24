using UnityEngine.Events;

public class ContinueButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => UIManager.ContinuePressed;
}