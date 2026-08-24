using UnityEngine;
using UnityEngine.Events;

public class CloseGameButtonController : GenericButtonController
{
    protected override UnityAction FunctionToCall => CloseGame;

    public static void CloseGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
