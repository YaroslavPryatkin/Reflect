using UnityEngine;
using TMPro;

public class ExplanationTextController : SceneLocalSingleton<ExplanationTextController>
{
    [SerializeField] private TextMeshProUGUI textMesh;

    private readonly UtilityClasses.MultipleBoolValue _isActive = new();
    private readonly UtilityClasses.MultipleBoolValue _isHidden = new();
    
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void ChangeSetActive()
    {
        gameObject.SetActive(_isActive.Value && !_isHidden.Value);
    }
    
    public static void Activate(string text)
    {
        Instance.textMesh.text = text;
        Instance._isActive.Set();
        Instance.ChangeSetActive();
    }
    
    public static void Deactivate()
    {
        Instance._isActive.Unset();
        Instance?.ChangeSetActive();
    }
    
    public static void HideText()
    {
        Instance._isHidden.Set();
        Instance.ChangeSetActive();
    }

    public static void StopHidingText()
    {
        Instance._isHidden.Unset();
        Instance.ChangeSetActive();
    }
}
