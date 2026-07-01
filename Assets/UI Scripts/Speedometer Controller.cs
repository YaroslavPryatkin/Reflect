using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedometerController : MonoBehaviour
{
    [SerializeField] private PlayerSensors playerSensors;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private Image speedBar;

    [Header("Settings")]
    [SerializeField] private float maxSpeedForUI = 20f;
    [SerializeField] private float speedMultiplier = 3.6f;

    private void Update()
    {
        var rawSpeed = playerSensors.HorizontalSpeed;
        var displayedSpeed = rawSpeed * speedMultiplier;

        speedText.text = displayedSpeed.ToString("F0");
        var fillRatio = rawSpeed / maxSpeedForUI;
        speedBar.fillAmount = Mathf.Clamp01(fillRatio);
    }
}
