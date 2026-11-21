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
        [Tooltip("When a boss dies the world slows to this speed...")]
        public float bossDeathSlow = 0.22f;
        [Tooltip("...for this many real seconds.")]
        public float bossDeathTime = 1.1f;

        /// <summary>Tests set this to turn the slow motion on or off; left alone, it is on except in headless runs.</summary>
        public static bool? SlowMotionOverride;
        /// <summary>A boss has given up its part of the machine.</summary>
        public static event System.Action<MachinePart> PieceFound;
        public const string PieceHeading = "A PART OF THE MACHINE";
        /// <summary>True while a boss's death is playing out slowly.</summary>
        public static bool InSlowMotion { get; private set; }
        float _speedBefore = 1f;

        void Awake()
        {
            RunState.Reset();
            Application.targetFrameRate = 60;
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
            if (GetComponent<FieldGuideWatcher>() == null) gameObject.AddComponent<FieldGuideWatcher>();
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
            EndSlowMotion();
        }

        IEnumerator BossDeath(Enemy boss)
        {
            Vector3 at = boss.transform.position + Vector3.up * 1.5f;
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.9f);
            Fx.Burst(at, Color.white, 40, 12f, 0.3f, 0.9f);

            if (!(SlowMotionOverride ?? !Application.isBatchMode) || InSlowMotion || Time.timeScale <= 0f) yield break;
            InSlowMotion = true;
            _speedBefore = Time.timeScale;
            for (float t = 0f; t < bossDeathTime; t += Time.unscaledDeltaTime)
            {
                // Ease back up over the last third. A pause or a card holding the world still is left alone.
                float back = Mathf.InverseLerp(bossDeathTime * 0.66f, bossDeathTime, t);
                if (Time.timeScale > 0f) Time.timeScale = Mathf.Lerp(bossDeathSlow, 1f, back) * _speedBefore;
                yield return null;
            }
            EndSlowMotion();
        }

        void EndSlowMotion()
        {
            if (!InSlowMotion) return;
            InSlowMotion = false;
            if (Time.timeScale > 0f) Time.timeScale = _speedBefore;
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
            if (enemy is BossEnemy)
            {
                SaveSystem.Data.meta.bossesBeaten++;
                Specimens.Keep(enemy);
                var floors = FloorManager.Instance;
                bool last = enemy is CatBoss || enemy is MachineBoss || enemy is FinchBoss
                            || (floors != null && floors.biome != null && floors.biome.boss != null && enemy.name.StartsWith(floors.biome.boss.name));
                Progress.Add(last ? Progress.BossXp : Progress.MiniBossXp);
                // A world's own last boss, not the machine or the cat: one more world behind this run.
                if (floors != null && !floors.InSpecial && floors.biome != null && floors.biome.boss != null && enemy.name.StartsWith(floors.biome.boss.name)) floors.WorldsBeaten++;
                // The last boss of each world has one of the machine's parts. Once a cycle, it gives it up.
                MachinePart part = MachineParts.From(enemy);
                if (MachineParts.Take(part))
                {
                    // On a card of its own: the line of text under it is busy with achievements at a moment like this.
                    string rest = MachineParts.Complete ? "That is all four. The panel in the lab will take them." : "The rest are with the others.";
                    string line = $"{char.ToUpper(part.name[0])}{part.name.Substring(1)}. {MachineParts.Count} of {MachineParts.All.Length}. {rest}";
                    if (ScreenCards.Instance != null) ScreenCards.Instance.ShowCard(PieceHeading, line, 4.5f);
                    else
                    {
                        var hud = FindAnyObjectByType<Hud>();
                        if (hud != null) hud.Toast(PieceHeading + ": " + line, 6f);
                    }
                    PieceFound?.Invoke(part);
                }
                StartCoroutine(BossDeath(enemy));
            }
            Loot.Drop(enemy);
        }

        void OnPlayerDamaged(int amount, Vector3 direction)
        {
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(hitShake);
            var player = PlayerController.Instance;
            if (player != null) Fx.Burst(player.transform.position + Vector3.up * 1f, new Color(1f, 0.3f, 0.3f), 10, 4f, 0.16f);
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
