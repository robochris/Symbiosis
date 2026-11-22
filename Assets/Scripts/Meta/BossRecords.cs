using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Records for every boss: the fastest it has ever been beaten (from when it appeared to when it fell), and a medal
    /// for beating it without being hit. Both are said when it falls and written on its jar. The music quickens for a
    /// boss's last phase, too (see Music).
    /// </summary>
    public static class BossRecords
    {
        static string Key(BossEnemy boss) => boss.name.Replace("(Clone)", "").Trim();

        /// <summary>When the boss in play appeared, and whether the player has been hit since.</summary>
        static float _startedAt;
        static bool _hit;
        static BossEnemy _watching;
        static PlayerController _hooked;

        /// <summary>Called every frame by the music player, which is always about: notices a new boss.</summary>
        public static void Watch()
        {
            var boss = BossEnemy.InPlay;
            if (boss == null || boss == _watching) return;
            _watching = boss;
            _startedAt = Time.time;
            _hit = false;
            var player = PlayerController.Instance;
            if (player != null && player != _hooked)
            {
                _hooked = player;
                player.Health.Damaged += (amount, direction) => { if (amount > 0) _hit = true; };
            }
        }

        public static float? Best(string key)
        {
            var meta = SaveSystem.Data.meta;
            int index = meta.bossTimeKeys.IndexOf(key);
            return index >= 0 ? meta.bossTimes[index] : (float?)null;
        }

        public static bool Medal(string key) => SaveSystem.Data.meta.noHitMedals.Contains(key);

        /// <summary>A boss is down: time it, and give a medal if nothing touched the player.</summary>
        public static void Beaten(BossEnemy boss)
        {
            if (boss == null || boss != _watching) return;
            _watching = null;
            string key = Key(boss);
            float seconds = Time.time - _startedAt;
            var meta = SaveSystem.Data.meta;
            float? best = Best(key);
            bool record = best == null || seconds < best.Value;
            if (record)
            {
                int index = meta.bossTimeKeys.IndexOf(key);
                if (index >= 0) meta.bossTimes[index] = seconds;
                else { meta.bossTimeKeys.Add(key); meta.bossTimes.Add(seconds); }
            }
            bool medal = !_hit && !meta.noHitMedals.Contains(key);
            if (medal) meta.noHitMedals.Add(key);
            SaveSystem.Save();

            string line = $"{boss.displayName} down in {Format(seconds)}" + (record && best != null ? " — a new best!" : best != null ? $" (best {Format(best.Value)})" : ".");
            if (medal) line += " Not a scratch on you: a medal for its jar.";
            var hud = Object.FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(line, 4f);
        }

        public static string Format(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";

        /// <summary>What the jar's plaque says, if anything.</summary>
        public static string Plaque(string key)
        {
            float? best = Best(key);
            string text = best != null ? $"  Best: {Format(best.Value)}." : "";
            if (Medal(key)) text += " No-hit medal.";
            return text;
        }
    }
}
