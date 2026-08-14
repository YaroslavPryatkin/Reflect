using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200 )]
public class GlobalGameManager : MonoBehaviour
{
    private static GlobalGameManager _instance;
    [SerializeField] private GameObject player;

    
    public static GameObject Player => _instance.player;
    public static int PlayerLayerBitMask => 1 << _instance.player.layer;
    
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
}
