using UnityEngine.Events;

public class InGameLevelResetButton : GenericButtonController
{
    protected override UnityAction FunctionToCall => ResetLevel;
    
    private void ResetLevel()
    {
        LevelController.ResetLevel();
        UIManager.ContinuePressed();
    }
}