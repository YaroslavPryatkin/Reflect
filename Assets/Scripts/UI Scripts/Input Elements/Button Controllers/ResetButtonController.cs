using UnityEngine.Events;

public class ResetButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => UIManager.ResetPressed;
}