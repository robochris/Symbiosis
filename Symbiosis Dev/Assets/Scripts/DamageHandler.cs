using UnityEngine;

public class DamageHandler : MonoBehaviour
{
    public GameObject damagePrefab;  // Assign the FloatingDamage prefab in Inspector

    public void TakeDamage(float damage)
    {
        GameObject damageInstance = Instantiate(damagePrefab, transform.position, Quaternion.identity);
        damageInstance.GetComponent<FloatingDamage>().SetDamage(damage);
    }
}