using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class BindingSettingPrefabController : MonoBehaviour
{
#if UNITY_EDITOR
    
    [SerializeField] private TextMeshProUGUI actionNameLabel;
    [SerializeField] private RebindUIButtonController rebindController1;
    [SerializeField] private RebindUIButtonController rebindController2;

    public void SetupInEditor(string displayName, InputActionAsset asset, string mapName, string actionName, int ind1, int ind2)
    {
        actionNameLabel.text = displayName;
        rebindController1.SetupInEditor(asset, mapName, actionName, ind1);
        rebindController2.SetupInEditor(asset, mapName, actionName, ind2);
    }
#endif
}