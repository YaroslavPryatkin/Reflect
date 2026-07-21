using UnityEngine;
using System.Collections.Generic;

public class ArenaController : MonoBehaviour
{
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private List<GameObject> enemies=new();
    [SerializeField] private bool resetUponPlayerDeath = false;
    
    
    private readonly List<Vector3> _enemiesSpawnPositions=new();
    private readonly List<Quaternion> _enemiesSpawnRotations=new();
    private readonly List<HealthController> _enemiesHealth=new();
    private HealthController _playerHealth;

    private void Awake()
    {
        foreach (var enemy in enemies)
        {
            _enemiesSpawnPositions.Add(enemy.transform.position);
            _enemiesSpawnRotations.Add(enemy.transform.rotation);
            var h = enemy.GetComponent<HealthController>();
            h.SetArenaController(this);
            _enemiesHealth.Add(h);
        }
        _playerHealth=GlobalGameManager.Player.GetComponent<HealthController>();
        _playerHealth.SetArenaController(this);
        GlobalUIManager.Instance.EscapeMenuController.ResetButton.onClick.AddListener(ResetArena);
    }

    public void ResetArena()
    {
        for(var i=0;i<enemies.Count;i++)
        {
            enemies[i].SetActive(true);
            enemies[i].transform.position=_enemiesSpawnPositions[i];
            enemies[i].transform.rotation=_enemiesSpawnRotations[i];
            _enemiesHealth[i].ResetHealth();
        }
        
        _playerHealth.ResetHealth();
        GlobalGameManager.Player.transform.position=playerSpawnPoint.position;
        GlobalGameManager.Player.transform.rotation=playerSpawnPoint.rotation;
    }

    public void PlayerDied()
    {
        if(resetUponPlayerDeath)
            ResetArena();
    }
}
