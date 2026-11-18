using System;

namespace Shrunk
{
    /// <summary>What the player has collected in the current run.</summary>
    public static class RunState
    {
        public static int Kills { get; private set; }
        /// <summary>Spent at the shop; gone when the run ends.</summary>
        public static int Coins { get; private set; }
        /// <summary>Brought home by extracting, or partly kept on death.</summary>
        public static int Samples { get; private set; }

        public static event Action Changed;

        public static void Reset()
        {
            Kills = 0;
            Coins = 0;
            Samples = 0;
            Changed?.Invoke();
        }

        public static void AddKill() { Kills++; Changed?.Invoke(); }
        public static void AddCoins(int amount) { Coins += amount; Changed?.Invoke(); }
        public static void AddSamples(int amount) { Samples += amount; Changed?.Invoke(); }

        public static bool TrySpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Puts a saved run's totals back.</summary>
        public static void Restore(int kills, int coins, int samples)
        {
            Kills = kills;
            Coins = coins;
            Samples = samples;
            Changed?.Invoke();
        }
    }
}
