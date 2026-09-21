using UnityEngine;
using UnityEngine.Events;

public class SaveSettingsButtonController : GenericButtonController
{
    [SerializeField] private SettingsHolder holder;

    protected override UnityAction FunctionToCall
    {
        get
        {
            if (holder == null)
            {
                Debug.LogError("No holder assigned");
                return () => { };
            }

            return holder.Save;
        }
    }

    
#if UNITY_EDITOR
    public void SetupInEditor(SettingsHolder holder)
    {
        this.holder = holder;
    }
#endif
}