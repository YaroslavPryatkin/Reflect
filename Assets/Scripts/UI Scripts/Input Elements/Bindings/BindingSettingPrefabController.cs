using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class BindingSettingPrefabController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI actionNameLabel;
    [SerializeField] private RebindUIController rebindController;

    public void SetupInEditor(string displayName, InputActionAsset asset, string mapName, string actionName, int bindingIndex)
    {
        actionNameLabel.text = displayName;
        
        rebindController.SetupInEditor(asset, mapName, actionName, bindingIndex);
    }
}