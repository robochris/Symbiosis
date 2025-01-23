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
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float elapsed = 0f;
        Color originalColor = damageText.color;

        while (elapsed < floatDuration)
        {
            // Move upwards
            transform.Translate(floatDirection * floatSpeed * Time.deltaTime);

            // Handle fading
            if (elapsed > (floatDuration - fadeDuration))
            {
                float fadeElapsed = elapsed - (floatDuration - fadeDuration);
                float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                damageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset color
        damageText.color = originalColor;

        // Invoke callback to return to pool or destroy
        onComplete?.Invoke();
    }
}
