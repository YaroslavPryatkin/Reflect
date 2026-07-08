using UnityEngine;

public class DamageController : MonoBehaviour
{
    [SerializeField] private float gunDamage = 40f;
    [SerializeField] private LayerMask enemyLayer;
    public int EnemyLayer => enemyLayer;
    public float GunDamage => gunDamage;

}
