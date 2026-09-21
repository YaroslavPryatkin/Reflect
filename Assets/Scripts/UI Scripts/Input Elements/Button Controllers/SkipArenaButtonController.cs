using UnityEngine.Events;

public class SkipArenaButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => UIManager.SkipArenaPressed;
}