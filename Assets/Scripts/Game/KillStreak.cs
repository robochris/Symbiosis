using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Counts kills that follow each other quickly. Each kill within a few seconds of the last adds to the streak;
    /// every fifth in a row throws out a few extra coins; a pause ends it. The count shows beside the kills on
    /// screen. One per scene, on the Game object.
    /// </summary>
    public class KillStreak : MonoBehaviour
    {
        /// <summary>Seconds after a kill in which the next one still counts.</summary>
        public const float Window = 2.5f;
        public const int PaysEvery = 5, Pays = 3;

        static int _count;
        static float _last = -100f;

        /// <summary>The streak as it stands, or 0 once it has lapsed.</summary>
        public static int Live => Time.time - _last <= Window ? _count : 0;
        /// <summary>The longest streak of this scene.</summary>
        public static int Best { get; private set; }

        void OnEnable()
        {
            _count = 0;
            _last = -100f;
            Best = 0;
            Enemy.Killed += OnKilled;
        }

        void OnDisable() => Enemy.Killed -= OnKilled;

        void OnKilled(Enemy enemy)
        {
            if (!enemy.dropsLoot) return;
            _count = Live + 1;
            _last = Time.time;
            Best = Mathf.Max(Best, _count);
            if (_count % PaysEvery != 0) return;

            for (int i = 0; i < Pays; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.9f;
                Pickup.Coin(enemy.transform.position + new Vector3(offset.x, 0f, offset.y), 1);
            }
            Fx.Burst(enemy.transform.position + Vector3.up, new Color(1f, 0.82f, 0.2f), 16, 6f, 0.16f, 0.45f);
            Sfx.Play(SfxId.Coin);
        }
    }
}
