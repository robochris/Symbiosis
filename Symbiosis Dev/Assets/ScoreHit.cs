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
            UpdateScoreText();
        }
        
    }

    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }
}