using UnityEngine;
using UnityEngine.UI;

public class HealthBarController : MonoBehaviour
{
    [SerializeField] private GameObject healthSlider;
    [SerializeField] private GameObject deadImage;
    [SerializeField] private float halfScreenAmountOfHealth = 300f;
    
    private PlayerHealthController _playerHealthController;
    private Slider _healthSliderSlider;
    private RectTransform _healthSliderRect;

    private void Awake()
    {
        _playerHealthController = GlobalGameManager.Player.GetComponent<PlayerHealthController>();
        _healthSliderSlider = healthSlider.GetComponent<Slider>();
        _healthSliderRect = healthSlider.GetComponent<RectTransform>();

        deadImage.SetActive(false);
        
        UpdateSliderMaxHealth();
    }

    void Update()
    {
        _healthSliderSlider.value = _playerHealthController.HealthFraction;
        deadImage.SetActive(_playerHealthController.IsDead);

    }

    public void UpdateSliderMaxHealth()
    {
        var anchorMax = _healthSliderRect.anchorMax;
        anchorMax.x = _playerHealthController.MaxHealth/halfScreenAmountOfHealth;
        _healthSliderRect.anchorMax = anchorMax;
        var anchoredPosition = _healthSliderRect.anchoredPosition;
        anchoredPosition.x = 0f;
        _healthSliderRect.anchoredPosition = anchoredPosition;
    }
}
