using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Room fights with a little extra. Now and then a fight comes with a clock: clear the room in time and an item
    /// is left in the middle. And any fight cleared without being hit is flawless: two coins more.
    /// </summary>
    public static class RoomChallenge
    {
        public const float Chance = 0.07f, Seconds = 25f;
        public const int FlawlessCoins = 2;
        /// <summary>Tests pin it.</summary>
        public static float? ChanceForAll;
        /// <summary>Tests switch off the flawless coins, so their coin counts stay their own.</summary>
        public static bool FlawlessOff;

        public static bool Timed { get; private set; }
        public static float Deadline { get; private set; }
        public static int Flawless { get; private set; }
        static bool _hit;
        static PlayerController _hooked;

        public static void FightStarted(FloorManager floors)
        {
            var player = PlayerController.Instance;
            if (player != null && player != _hooked)
            {
                _hooked = player;
                player.Health.Damaged += (amount, direction) => { if (amount > 0) _hit = true; };
            }
            _hit = false;
            Timed = Unlocks.Open(Unlocks.Feature.Challenges) && floors.FloorNumber >= 2 && !floors.InGauntlet && Random.value < (ChanceForAll ?? Chance);
            if (!Timed) return;
            Deadline = Time.time + Seconds;
            var hud = Object.FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"Challenge: clear this room in {Seconds:0} seconds and something will be left for you.", 3.5f);
        }

        public static void FightOver(FloorManager floors, Vector3 middle)
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            if (Timed)
            {
                Timed = false;
                bool made = Time.time <= Deadline;
                // Coins and samples, not an item: items stay rare.
                if (made)
                {
                    for (int i = 0; i < 6; i++) Pickup.Coin(middle + new Vector3(-1.2f + (i % 3) * 0.8f, 0f, -1.5f + (i / 3) * 0.8f), 1);
                    Pickup.Sample(middle + new Vector3(1.5f, 0f, -1.5f), 10);
                }
                if (hud != null) hud.Toast(made ? "Challenge done: coins and samples left for you." : "Too slow for the challenge, this time.", 3f);
            }
            if (_hit || FlawlessOff) return;
            Flawless++;
            for (int i = 0; i < FlawlessCoins; i++) Pickup.Coin(middle + new Vector3(i * 0.6f, 0f, 0.8f), 1);
            if (hud != null && !Timed) hud.Toast("Flawless: not a scratch. Two coins more.", 2f);
        }
    }
}
