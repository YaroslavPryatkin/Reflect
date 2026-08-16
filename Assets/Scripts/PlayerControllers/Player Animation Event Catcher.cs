using UnityEngine;

public class PlayerAnimationEventCatcher : MonoBehaviour
{
    private PlayerMeleeController _playerMeleeController;

    private void Awake()
    {
        _playerMeleeController = GlobalGameManager.Player.GetComponent<PlayerMeleeController>();
    }

    private int _lastFrame = 0;
    public void FinishHimEvent()
    {
        if (_lastFrame != Time.frameCount)
        {
            _playerMeleeController.OnFinishHimEvent();
        }
        _lastFrame = Time.frameCount;
    }
}
