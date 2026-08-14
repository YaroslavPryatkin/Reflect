using System;
using CustomAttributes;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class ArenaFinishTriggerController : MonoBehaviour
{
    [SerializeField] private bool finishLevel = false;
    [SerializeField, EnableIf("!finishLevel")] private ArenaController nextArena;
    private ArenaController _arenaController;
    private int _targetLayers;

    private void Awake()
    {
        if (!finishLevel && nextArena == null)
        {
            Debug.LogError("ArenaFinishTriggerController: nextArena is null", this);
            enabled = false;
            return;
        }
        _targetLayers = GlobalGameManager.PlayerLayerBitMask;
        gameObject.layer = 2;
    }
    public void SetArenaController(ArenaController arenaController)
    {
        _arenaController = arenaController;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & _targetLayers) == 0) return;
        
        _arenaController.PlayerEnteredTrigger(nextArena);
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & _targetLayers) == 0) return;
        
        _arenaController.PlayerExitedTrigger();
    }

    private void OnDrawGizmos()
    {
        transform.DrawColliderGizmo(Color.softYellow);
    }
}
