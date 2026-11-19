using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>The player's options, remembered on the device. Changed from the pause menu.</summary>
    public static class GameSettings
    {
        const string Prefix = "shrunk.";

        public static event Action Changed;

        /// <summary>
        /// A new player's aim assist: Low with a mouse or pad, where the crosshair does the aiming and full assist feels
        /// like the game locking on for you; Medium on a touch screen, where a thumb can't aim that finely.
        /// </summary>
        public static float DefaultAimAssist => UnityEngine.Device.Application.isMobilePlatform ? 0.7f : 0.35f;

        /// <summary>0 = off, 1 = shots go straight at the target.</summary>
        public static float AimAssist
        {
            // "aimHelp", not the older "aimAssist": everyone starts on the new default once.
            get => PlayerPrefs.GetFloat(Prefix + "aimHelp", DefaultAimAssist);
            set => Set("aimHelp", Mathf.Clamp01(value));
        }

        /// <summary>Multiplier on how fast the view turns.</summary>
        public static float LookSpeed
        {
            get => PlayerPrefs.GetFloat(Prefix + "lookSpeed", 1f);
            set => Set("lookSpeed", Mathf.Clamp(value, 0.3f, 3f));
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(Prefix + "volume", 1f);
            set => Set("volume", Mathf.Clamp01(value));
        }

        /// <summary>Swaps the move and aim sides of the screen.</summary>
        public static bool LeftHanded
        {
            get => PlayerPrefs.GetInt(Prefix + "leftHanded", 0) == 1;
            set => SetInt("leftHanded", value ? 1 : 0);
        }

        /// <summary>On: the view only turns left and right.</summary>
        public static bool LockVertical
        {
            get => PlayerPrefs.GetInt(Prefix + "lockVertical", 1) == 1;
            set => SetInt("lockVertical", value ? 1 : 0);
        }

        public static CameraMode Camera
        {
            // "cameraView", not the older "camera": everyone starts on Auto once, whatever they had picked before it existed.
            get => (CameraMode)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "cameraView", (int)CameraMode.Auto), 0, 4);
            set => SetInt("cameraView", (int)value);
        }

        /// <summary>How much Dr. Finch says on the radio: 0 nothing, 1 only the big moments, 2 everything.</summary>
        public static int Radio
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "radio", 2), 0, 2);
            set => SetInt("radio", Mathf.Clamp(value, 0, 2));
        }

        /// <summary>Arrows at the screen edge for nearby enemies the camera cannot see.</summary>
        public static bool EnemyArrows
        {
            get => PlayerPrefs.GetInt(Prefix + "enemyArrows", 1) == 1;
            set => SetInt("enemyArrows", value ? 1 : 0);
        }

        /// <summary>Relaxed mode: two more points of health on every run. For learning the game, or for an easier evening.</summary>
        public static bool Relaxed
        {
            get => PlayerPrefs.GetInt(Prefix + "relaxed", 0) == 1;
            set => SetInt("relaxed", value ? 1 : 0);
        }

        public const int RelaxedHealth = 2;

        /// <summary>Numbers floating off enemies for hits bigger than the ordinary one.</summary>
        public static bool DamageNumbers
        {
            get => PlayerPrefs.GetInt(Prefix + "damageNumbers", 1) == 1;
            set => SetInt("damageNumbers", value ? 1 : 0);
        }

        static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            Changed?.Invoke();
        }

        static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(Prefix + key, value);
            Changed?.Invoke();
        }

        /// <summary>Back to the defaults.</summary>
        public static void Reset()
        {
            foreach (string key in new[] { "aimAssist", "aimHelp", "lookSpeed", "volume", "leftHanded", "lockVertical", "camera", "cameraView", "radio", "enemyArrows", "relaxed", "damageNumbers" })
            {
                PlayerPrefs.DeleteKey(Prefix + key);
            }
            Changed?.Invoke();
        }
    }
}
