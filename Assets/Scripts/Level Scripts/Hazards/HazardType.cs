
using UnityEngine;

[CreateAssetMenu(fileName = "New Hazard Type", menuName = "Hazards/Hazard Type")]
public class HazardType : ScriptableObject
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float invulnerabilityDuration = 0.3f;

    public float Damage => damage;
    public float InvulnerabilityDuration => invulnerabilityDuration;
}
