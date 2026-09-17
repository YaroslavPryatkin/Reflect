using UnityEngine;
using TMPro;

public class ExplanationTextController : SceneLocalSingleton<ExplanationTextController>
{
    [SerializeField] private TextMeshProUGUI textMesh;
    
    private UtilityStructures.BoolCounter _isActive;
    private UtilityStructures.BoolCounter _isHidden;
    
    private void Start()
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
    
    public static void Hide()
    {
        Instance._isHidden.Set();
        Instance.ChangeSetActive();
    }

    public static void StopHiding()
    {
        Instance._isHidden.Unset();
        Instance.ChangeSetActive();
    }
}
