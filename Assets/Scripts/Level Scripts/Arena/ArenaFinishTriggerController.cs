using System;
using CustomAttributes;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class ArenaFinishTriggerController : MonoBehaviour
{
    [SerializeField] private bool pressButtonToActivate = false;
    [SerializeField] private bool finishLevel = false;
    [SerializeField, EnableIf("!finishLevel")] private ArenaController nextArena;
    private ArenaController _arenaController;
    private int _targetLayers;

    public bool HaveNextArena => !finishLevel;
    public ArenaController NextArena => nextArena;

    private void Awake()
    {
        if (!finishLevel && nextArena == null)
        {
            Debug.LogError("ArenaFinishTriggerController: nextArena is null", this);
            enabled = false;
            return;
        }
        _targetLayers = PlayerManager.PlayerLayerBitMask;
        gameObject.layer = 2;
    }
    public void SetArenaController(ArenaController arenaController)
    {
        _arenaController = arenaController;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_targetLayers.Contains(other)) return;
        
        _arenaController.PlayerEnteredTrigger(!finishLevel, nextArena, pressButtonToActivate);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_targetLayers.Contains(other)) return;
        
        _arenaController.PlayerExitedTrigger(pressButtonToActivate);
    }

    private void OnDrawGizmos()
    {
        transform.DrawColliderGizmo(Color.softGreen);
    }
}
