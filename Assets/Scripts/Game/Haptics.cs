using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// On a phone, a short buzz when the player is hurt, so a hit is felt as well as seen. A setting turns it off.
    /// Does nothing on a PC.
    /// </summary>
    public static class Haptics
    {
        /// <summary>Buzzes counted, for tests (they run on a PC, where nothing buzzes).</summary>
        public static int Buzzes { get; private set; }
        static PlayerController _hooked;
        static float _last;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() => SceneManager.sceneLoaded += (scene, mode) => Attach();

        static void Attach()
        {
            var player = PlayerController.Instance;
            if (player == null || player == _hooked) return;
            _hooked = player;
            player.Health.Damaged += (amount, direction) => { if (amount > 0) Buzz(); };
        }

        public static void Buzz()
        {
            if (!GameSettings.Vibration || Time.unscaledTime - _last < 0.15f) return;
            _last = Time.unscaledTime;
            Buzzes++;
#if UNITY_ANDROID || UNITY_IOS
            if (Application.isMobilePlatform) Handheld.Vibrate();
#endif
        }
    }
}
