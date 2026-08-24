using System;
using UnityEngine;

public class JumpPadController : MonoBehaviour
{
    [SerializeField] public float jumpForce;
    [SerializeField] public Color playerParticlesColor=Color.blue;
    [SerializeField] 
    [Tooltip("Applies force in jumpDirection.forward. If null, then transform.up will be used.")]
    private Transform jumpDirection;
    
    private Collider _player;
    private PlayerJumpController _playerJumpController;

    public Vector3 JumpForceVector { get; private set; }=Vector3.up;
    
    private void Awake()
    {
        _player = PlayerManager.Player.GetComponent<Collider>();
        _playerJumpController = _player.GetComponent<PlayerJumpController>();
        if(jumpDirection==null)
            JumpForceVector = transform.up * jumpForce;
        else
            JumpForceVector = jumpDirection.forward * jumpForce;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other == _player)
        {
            _playerJumpController.EnterJumpPad(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == _player)
        {
            _playerJumpController.ExitJumpPad();
        }
    }
}
