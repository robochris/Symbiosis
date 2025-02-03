using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private Stats enemyStats; // Assign your enemy ScriptableObject here

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f; // Adjust as needed
    private Transform playerTransform;

    [Header("Health System")]
    private HealthSystem healthSystem;

    [Header("Loot Settings")]
    public LootTable lootTable;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("Enemy: HealthSystem component missing.");
            return;
        }
        // Initialize health using the enemy's max health from the ScriptableObject
        healthSystem.SetStats(enemyStats.maxHealth);
        healthSystem.OnHealthChanged += HandleHealthChanged;
        healthSystem.OnDeath += HandleEnemyDied;
    }

    private void Start()
    {
        // Find the player (ensure your player is tagged "Player")
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogError("Enemy: Player not found!");
    }

    private void Update()
    {
        if (playerTransform != null)
        {
            // Lock y axis for target position
            Vector3 targetPosition = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);

            // Move toward the target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Calculate direction from enemy to player, ignoring y difference
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                // Calculate target rotation and smoothly rotate toward it
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }


    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        if (healthSystem != null)
            healthSystem.TakeDamage(damage);
    }

    public void Heal(int amount)
    {
        if (healthSystem != null)
            healthSystem.Heal(amount);
    }

    public int GetCurrentHealth()
    {
        return healthSystem != null ? healthSystem.GetCurrentHealth() : 0;
    }

    public int GetMaxHealth()
    {
        return healthSystem != null ? healthSystem.GetMaxHealth() : 0;
    }

    private void HandleHealthChanged(int newHealth)
    {
        // (Optional) Update enemy UI or effects here
    }

    private void HandleEnemyDied()
    {
        // Spawn loot from the loot table
        List<GameObject> drops = lootTable.GetDrops();
        foreach (GameObject drop in drops)
        {
            GameObject dropInstance = Instantiate(drop, transform.position, Quaternion.identity);
            ItemPickup itemPickup = dropInstance.GetComponent<ItemPickup>();
            if (itemPickup != null)
            {
                itemPickup.canRespawn = false;
            }
        }

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.SpawnEnemy();
        }

        Debug.Log($"{gameObject.name} has died");
        Destroy(gameObject);
    }


    // On collision, deal damage to the player using the enemy's attack damage from the ScriptableObject
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(enemyStats.attackDamage);
            }
        }
    }
}
