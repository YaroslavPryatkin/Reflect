using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoolInputPrefabController : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("UI")] 
    [SerializeField] private TextMeshProUGUI header;
    [SerializeField] private BoolInputButtonController buttonController;
    

    public void SetupInEditor(BoolSettingValue settingValue)
    {
        if (header != null)
        {
            header.text = settingValue.name;
        }
        else
        {
            Debug.LogWarning("No header assigned");
        }
        
        buttonController.SetupInEditor(settingValue);
    }
#endif
}