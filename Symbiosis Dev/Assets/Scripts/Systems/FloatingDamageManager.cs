// Assets/Scripts/FloatingDamageManager.cs
using UnityEngine;

public class FloatingDamageManager : MonoBehaviour
{
    public static FloatingDamageManager Instance { get; private set; }

    [SerializeField] private FloatingDamage floatingDamagePrefab; // Assign via Inspector
    [SerializeField] private Transform damageParent;               // Assign via Inspector
    [SerializeField] private int poolSize = 20;                   // Optional: Adjust pool size as needed

    private ObjectPool<FloatingDamage> damagePool;

    private void Awake()
    {
        // Implement Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            Debug.LogWarning("FloatingDamageManager: Duplicate instance found and destroyed.");
            return;
        }

        Instance = this;
        Debug.Log("FloatingDamageManager: Instance has been set.");

        // Validate assignments
        if (floatingDamagePrefab == null)
        {
            Debug.LogError("FloatingDamageManager: FloatingDamage prefab is not assigned.");
            return;
        }

        if (damageParent == null)
        {
            Debug.LogError("FloatingDamageManager: DamageParent Transform is not assigned.");
            return;
        }

        // Initialize the Object Pool
        damagePool = new ObjectPool<FloatingDamage>(floatingDamagePrefab, damageParent, poolSize);
    }

    // Method to spawn floating damage
    public void SpawnFloatingDamage(Vector3 position, int damageAmount)
    {
        if (damagePool == null)
        {
            Debug.LogError("FloatingDamageManager: Object pool is not initialized.");
            return;
        }

        FloatingDamage floatingDamage = damagePool.GetFromPool();
        floatingDamage.transform.position = position;
        floatingDamage.transform.rotation = Quaternion.identity;
        floatingDamage.gameObject.SetActive(true);
        floatingDamage.Initialize(damageAmount, () => damagePool.ReturnToPool(floatingDamage));
    }
}
