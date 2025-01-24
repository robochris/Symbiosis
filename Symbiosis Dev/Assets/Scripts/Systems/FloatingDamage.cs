using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float floatDuration = 1f;
    [SerializeField] private Vector3 floatDirection = Vector3.up;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float fadeDuration = 0.5f;

    private System.Action onComplete;

    private void Awake()
    {
        if (damageText == null)
        {
            Debug.LogError("FloatingDamage: damageText is not assigned.");
        }
    }

    // Initialize the floating damage with the damage amount and callback
    public void Initialize(int damageAmount, System.Action callback)
    {
        damageText.text = damageAmount.ToString();
        onComplete = callback;
        Debug.Log($"FloatingDamage: Initialized with damage {damageAmount}");
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float elapsed = 0f;
        Color originalColor = damageText.color;

        while (elapsed < floatDuration)
        {
            // Move upwards
            transform.Translate(floatDirection * floatSpeed * Time.deltaTime, Space.World);

            // Handle fading
            if (elapsed > (floatDuration - fadeDuration))
            {
                float fadeElapsed = elapsed - (floatDuration - fadeDuration);
                float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                damageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }

            // Optional: Log position and alpha for debugging
            if (elapsed % 0.2f < Time.deltaTime) // Log every 0.2 seconds
            {
                Debug.Log($"FloatingDamage: Position {transform.position}, Alpha {damageText.color.a}");
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset color
        damageText.color = originalColor;

        // Invoke callback to return to pool or destroy
        onComplete?.Invoke();

        Debug.Log("FloatingDamage: Animation complete, returning to pool.");
    }
}
