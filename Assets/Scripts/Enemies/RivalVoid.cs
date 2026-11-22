using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The rival: another Void, from another universe, with red eyes and a bandana. Now and then it turns up in a
    /// fight on a world's later floors, shoots, side-steps when hit, and at half health grabs some coins and bolts,
    /// tougher each time. The third time it stays to the end, and its bandana is yours.
    /// </summary>
    public class RivalVoid : ShooterEnemy
    {
        public const int Meetings = 3;
        /// <summary>Chance, per fight on a world's later floors, that it turns up (once a run at most).</summary>
        public const float Chance = 0.08f;
        public static float? ChanceForAll;
        public static bool MetThisRun;

        public int coinsOnEscape = 8;
        public float dashSpeed = 11f, dashTime = 0.22f, dashChance = 0.35f;

        bool _final, _escaped;
        float _dashUntil, _nextDash;
        Vector3 _dash;

        public bool Escaped => _escaped;
        public bool Final => _final;

        /// <summary>Whether it turns up in the fight starting now.</summary>
        public static bool ShouldAppear(FloorManager floors)
        {
            if (floors == null || MetThisRun || floors.biome == null || floors.biome.isBottom || floors.InSpecial || floors.InGauntlet) return false;
            if (SaveSystem.Data.meta.rivalBeaten > 0) return false;
            if (FloorLayout.IsBossFloor(floors.FloorNumber) || FloorLayout.FloorOfBiome(floors.FloorNumber) < 3) return false;
            return Random.value < (ChanceForAll ?? Chance);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _escaped = false;
            int met = SaveSystem.Data.meta.rivalMet;
            _final = met >= Meetings - 1;
            Health.SetMaxHealth(Mathf.CeilToInt(Health.maxHealth * (1f + 0.5f * met)), true);
            Health.Damaged += OnHurt;
            dropsLoot = true;
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(met == 0 ? "Another Void. Red eyes. It looks at you like it knows you." : _final ? "The rival again. This time it is not running." : "The rival is back, and it has been practising.", 4f);
        }

        protected override void OnDisable()
        {
            Health.Damaged -= OnHurt;
            base.OnDisable();
        }

        void OnHurt(int amount, Vector3 direction)
        {
            if (Health.IsDead || Time.time < _nextDash || Random.value > dashChance) return;
            // A side-step, across the line of the shot.
            Vector3 across = Vector3.Cross(Vector3.up, direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward);
            if (Random.value < 0.5f) across = -across;
            _dash = across * dashSpeed;
            _dashUntil = Time.time + dashTime;
            _nextDash = Time.time + 1.2f;
        }

        protected override void Think()
        {
            if (!_final && !_escaped && !Health.IsDead && Health.Current * 2 <= Health.maxHealth)
            {
                Escape();
                return;
            }
            if (Time.time < _dashUntil)
            {
                Move(_dash);
                return;
            }
            base.Think();
        }

        void Escape()
        {
            _escaped = true;
            var meta = SaveSystem.Data.meta;
            meta.rivalMet++;
            SaveSystem.Save();
            for (int i = 0; i < coinsOnEscape; i++)
            {
                Vector2 scatter = Random.insideUnitCircle * 1.5f;
                Pickup.Coin(transform.position + new Vector3(scatter.x, 0f, scatter.y), 1);
            }
            dropsLoot = false;
            var hud = FindAnyObjectByType<Hud>();
            int left = Meetings - meta.rivalMet;
            if (hud != null) hud.Toast($"The rival drops some coins and bolts. It will be back. ({left} more time{(left == 1 ? "" : "s")})", 4f);
            Health.Kill();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            if (_escaped) return;
            var meta = SaveSystem.Data.meta;
            if (_final)
            {
                meta.rivalMet = Meetings;
                meta.rivalBeaten = 1;
                SaveSystem.Save();
                var hud = FindAnyObjectByType<Hud>();
                if (hud != null) hud.Toast("The rival is beaten, for good. It leaves its bandana behind: the hat stand has it.", 5f);
            }
            else
            {
                // Knocked down from more than half in one go: it still counts as a meeting.
                meta.rivalMet++;
                SaveSystem.Save();
            }
        }
    }
}
