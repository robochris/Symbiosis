using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A line of help the first time something comes up in a run: the first enemy, the first hit taken, the first
    /// item lying about, the first coins, the first boss, the power filling up, the first portals. Each is said once,
    /// ever, and never two close together. Worded for a phone or a keyboard, whichever is in use.
    /// </summary>
    public class FirstTips : MonoBehaviour
    {
        public enum Tip { Fire, Dodge, Item, Coins, Boss, Power, Portals }

        /// <summary>Tests switch it off, so its toasts never get in the way of what they read.</summary>
        public static bool Off;
        /// <summary>Seconds between two tips.</summary>
        public const float Gap = 7f;

        static FirstTips _instance;
        float _quietUntil, _nextCheck;
        bool _hurt;
        PlayerController _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("First tips");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FirstTips>();
        }

        public static bool Seen(Tip tip) => (SaveSystem.Data.meta.tipsSeen & (1 << (int)tip)) != 0;

        static bool Touch => Interact.OnTouch;

        public static string Line(Tip tip)
        {
            switch (tip)
            {
                case Tip.Fire: return Touch
                    ? "Drag on the right side to aim. You fire by yourself whenever something is in your sights."
                    : "Hold the mouse button to fire. Shots lean toward whatever you are facing.";
                case Tip.Dodge: return Touch
                    ? "Ouch. Tap the dodge button to roll: nothing can touch you mid-roll."
                    : "Ouch. Press Space to roll: nothing can touch you mid-roll.";
                case Tip.Item: return "An item. Walk over it to take it. It lasts until the run ends, and they stack up.";
                case Tip.Coins: return "Coins are for the shops on this run. Samples are what you take home.";
                case Tip.Boss: return "A boss. Its big attacks are marked on the floor first: get out of the marks.";
                case Tip.Power: return Touch
                    ? "Your power is ready. Tap the power button."
                    : "Your power is ready. Press Q.";
                case Tip.Portals: return "Two ways on: deeper, for more samples and more danger, or home, to keep what you carry.";
            }
            return "";
        }

        void Update()
        {
            if (Off || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            if (player == null || floors == null) return;
            if (_hooked != player)
            {
                _hooked = player;
                _hurt = false;
                player.Health.Damaged += (amount, direction) => { if (amount > 0) _hurt = true; };
            }
            if (Time.unscaledTime < _quietUntil || ScreenCards.Blocking) return;

            Vector3 at = player.transform.position;
            if (!Seen(Tip.Boss) && BossEnemy.InPlay != null && Say(Tip.Boss)) return;
            if (!Seen(Tip.Fire) && NearestEnemy(at) < 14f && Say(Tip.Fire)) return;
            if (!Seen(Tip.Dodge) && _hurt && Say(Tip.Dodge)) return;
            if (!Seen(Tip.Item) && Pickup.NearestItem(at, 6f) != null && Say(Tip.Item)) return;
            if (!Seen(Tip.Coins) && RunState.Coins > 0 && Say(Tip.Coins)) return;
            if (!Seen(Tip.Power) && player.Power != null && player.Power.IsReady && Say(Tip.Power)) return;
            if (!Seen(Tip.Portals) && floors.Portals.Count > 1) Say(Tip.Portals);
        }

        static float NearestEnemy(Vector3 at)
        {
            float best = float.MaxValue;
            foreach (var enemy in Enemy.Active)
            {
                if (enemy == null || enemy.Health.IsDead) continue;
                best = Mathf.Min(best, Vector3.Distance(enemy.transform.position, at));
            }
            return best;
        }

        bool Say(Tip tip)
        {
            var hud = FindAnyObjectByType<Hud>();
            if (hud == null) return false;
            SaveSystem.Data.meta.tipsSeen |= 1 << (int)tip;
            SaveSystem.Save();
            hud.Toast(Line(tip), 5.5f);
            _quietUntil = Time.unscaledTime + Gap;
            return true;
        }
    }
}
