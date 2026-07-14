using UnityEngine;
using UnityEngine.UI;

public class EscapeMenuController : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private InputSlider mouseSensX;
    [SerializeField] private InputSlider mouseSensY;
    [SerializeField] private InputSlider mouseSensAim;
    
    public Button ContinueButton => continueButton;
    public Button ExitButton => exitButton;
    public InputSlider MouseSensX => mouseSensX;
    public InputSlider MouseSensY => mouseSensY;
    
    public InputSlider MouseSensAim => mouseSensAim;
}
