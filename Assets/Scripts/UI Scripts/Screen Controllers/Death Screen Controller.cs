using UnityEngine;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{
    private static DeathScreenController _instance;
    
    [SerializeField] private Button resetButton;
    [SerializeField] private Button exitButton;
    
    public static Button ResetButton => _instance.resetButton;
    public static Button ExitButton => _instance.exitButton;
    
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
