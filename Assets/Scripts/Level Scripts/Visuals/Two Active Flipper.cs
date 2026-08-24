using UnityEngine;

[DefaultExecutionOrder(-20)]
public class TwoActiveFlipper : MonoBehaviour
{
    [SerializeField] private GameObject open;
    [SerializeField] private GameObject close;
    [SerializeField] private bool startWithOpen = false;
    
    private void Awake()
    {
        SetOpen(startWithOpen);
    }

    public void SetOpen(bool isOpen)
    {
        open.SetActive(isOpen);
        close.SetActive(!isOpen);
    }
}
