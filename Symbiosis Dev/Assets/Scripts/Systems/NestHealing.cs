using UnityEngine;

public class PlayerHealing : MonoBehaviour
{
    public Transform healingPoint;        // The localized healing point
    public float healingRadius = 2f;        // Radius within which healing is allowed
    public float stationaryThreshold = 0.1f;// Velocity below which the player is considered stationary
    public int healAmount = 5;           // Amount healed each interval
    public float healInterval = 1f;         // Time interval between heals
    private HealthSystem healthSystem;
    private Rigidbody rb;
    private float healTimer = 0f;
    

    void OnTriggerStay(Collider other)
    {
        Debug.Log("Player is healing");
        if (other.CompareTag("Player"))
        {
            healTimer += Time.deltaTime;
            if (healTimer >= healInterval)
            {
                // Call the Heal method in your PlayerStats script
                PlayerManagement.Instance.Heal(healAmount);
                healTimer = 0f;
            }
        }
    }
}