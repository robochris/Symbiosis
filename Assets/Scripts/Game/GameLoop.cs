using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>Counts kills and drops loot. In the test arena it also restarts the scene when the player dies.</summary>
    public class GameLoop : MonoBehaviour
    {
        public static int Kills => RunState.Kills;

        public float restartDelay = 1.5f;
        public float hitShake = 0.35f;

        void Awake()
        {
            RunState.Reset();
            Application.targetFrameRate = 60;
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
        }

        void Start()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            player.Health.Damaged += OnPlayerDamaged;
            player.Health.Died += OnPlayerDied;
        }

        void OnEnemyKilled(Enemy enemy)
        {
            RunState.AddKill();
            Loot.Drop(enemy);
        }

        void OnPlayerDamaged(int amount, Vector3 direction)
        {
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(hitShake);
            Sfx.Play(SfxId.PlayerHurt);
        }

        void OnPlayerDied()
        {
            // In a real run the RunDirector settles up and sends the player back to the lab.
            if (RunDirector.Instance != null) return;
            StartCoroutine(Restart());
        }

        IEnumerator Restart()
        {
            var player = PlayerController.Instance;
            if (player != null && player.visual != null) player.visual.gameObject.SetActive(false);
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
