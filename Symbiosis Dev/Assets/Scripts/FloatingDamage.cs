using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatingDamage : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float fadeSpeed = 2f;
    public float lifetime = 1f;

    private TextMeshProUGUI damageText;
    private Color textColor;
 
    void Start()
    {
        damageText = GetComponent<TextMeshProUGUI>();
        textColor = damageText.color;
        Destroy(gameObject, lifetime);
    }

  
    void Update()
    {
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);
        textColor.a -= fadeSpeed * Time.deltaTime;
        damageText.color = textColor;
    }
    public void SetDamage(float damageAmount)
    {
        damageText.text = damageAmount. ToString();
    }
}
