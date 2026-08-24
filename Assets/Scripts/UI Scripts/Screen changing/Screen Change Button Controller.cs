using System;
using CustomAttributes;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ScreenChangeButtonController : MonoBehaviour
{
    [SerializeField] private ScreenFatherController father;
    [SerializeField] private bool useIndex = false;
    [SerializeField, EnableIf("!useIndex")] private GameObject targetScreen;
    [SerializeField, EnableIf("useIndex")] private int index = 0;
    
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (father == null)
        {
            var parent = transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent(out father))
                {
                    CheckFather();
                    return;
                }
                parent = parent.parent;
            }
            Debug.LogError("No screen father controller found", this);
        }
        else
        {
            CheckFather();
        }
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(ChangeScreen);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(ChangeScreen);
    }

    private void CheckFather()
    {
        if (useIndex)
        {
            if (!father.DoesHaveIndex(index))
            {
                Debug.LogError("Screen father controller does not have index " + index, this);
                index = 0;
            }
        }
        else
        {
            index = father.GetScreenIndex(targetScreen);
        }
    }
    
    public void ChangeScreen()
    {
        father.OpenScreen(index);
    }
}
