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
            foreach (string key in new[] { "aimAssist", "aimHelp", "lookSpeed", "volume", "leftHanded", "lockVertical", "camera", "cameraView" })
            {
                PlayerPrefs.DeleteKey(Prefix + key);
            }
            Changed?.Invoke();
        }
    }
}
