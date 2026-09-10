using UnityEngine;

public abstract class TriggerController : MonoBehaviour
{
    private int _targetLayers;
    private bool _entered = false;

    private void Awake()
    {
        _targetLayers = PlayerManager.PlayerLayerBitMask;
        gameObject.layer = 2;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_targetLayers.Contains(other))
        {
            _entered = true;
            Entered();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (_targetLayers.Contains(other))
        {
            _entered = false;
            Exited();
        }
    }

    private void OnDisable()
    {
        if (_entered)
        {
            _entered = false;
            Exited();
        }
    }

    protected abstract void Entered();

    protected abstract void Exited();
}
