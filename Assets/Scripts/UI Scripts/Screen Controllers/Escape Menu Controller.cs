using UnityEngine;
using UnityEngine.UI;


public class EscapeMenuController : MonoBehaviour
{
    private static EscapeMenuController _instance;
    
    [SerializeField] private Button continueButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private InputSlider mouseSensX;
    [SerializeField] private InputSlider mouseSensY;
    [SerializeField] private InputSlider mouseSensAim;
    
    public static Button ContinueButton => _instance.continueButton;
    public static Button ResetButton => _instance.resetButton;
    public static Button ExitButton => _instance.exitButton;
    public static InputSlider MouseSensX => _instance.mouseSensX;
    public static InputSlider MouseSensY => _instance.mouseSensY;
    
    public static InputSlider MouseSensAim => _instance.mouseSensAim;

    public void SetSingleTone()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
