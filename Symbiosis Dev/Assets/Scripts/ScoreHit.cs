using UnityEngine;
using TMPro;
public class ScoreOnHit : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    public int score = 0;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            score++;
            UIManager.Instance.UpdateScore(score);
        }
        
    }
}
