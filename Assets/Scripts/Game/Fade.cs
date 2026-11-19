using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// A black screen that fades out and in, so going through a portal or down a hole is a dip to black instead of a
    /// hard cut. It makes itself the first time it is used and lives across scenes. Headless test runs skip it.
    /// </summary>
    public class Fade : MonoBehaviour
    {
        /// <summary>Tests set this to turn fades on or off; left alone, they are on except in headless runs.</summary>
        public static bool? EnabledOverride;
        public static bool Enabled => EnabledOverride ?? !Application.isBatchMode;

        public static float OutTime = 0.22f;
        public static float InTime = 0.4f;

        /// <summary>How black the screen is right now, 0 to 1.</summary>
        public static float Amount => _instance != null ? _instance._image.color.a : 0f;
        /// <summary>True while a scene change is fading out.</summary>
        public static bool Leaving => _instance != null && _instance._leaving;

        static Fade _instance;
        Image _image;
        bool _leaving;
        Coroutine _running;

        static Fade Get()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("Fade", typeof(Canvas));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // over everything

            var panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(go.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            _instance = go.AddComponent<Fade>();
            _instance._image = panel.GetComponent<Image>();
            _instance._image.color = new Color(0f, 0f, 0f, 0f);
            _instance._image.raycastTarget = false;
            return _instance;
        }

        /// <summary>Fades to black, loads the scene, and fades back in.</summary>
        public static void LoadScene(string scene)
        {
            if (!Enabled)
            {
                SceneManager.LoadScene(scene);
                return;
            }
            Fade fade = Get();
            if (fade._leaving) return;
            fade.Run(fade.Leave(scene));
        }

        /// <summary>Cuts to black and fades back in: for a new floor, which is built in a single frame.</summary>
        public static void In(float seconds = -1f)
        {
            if (!Enabled) return;
            Fade fade = Get();
            if (fade._leaving) return;
            fade.Run(fade.To(1f, 0f, seconds > 0f ? seconds : InTime));
        }

        void Run(IEnumerator routine)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(routine);
        }

        IEnumerator Leave(string scene)
        {
            _leaving = true;
            yield return To(_image.color.a, 1f, OutTime);
            SceneManager.LoadScene(scene);
            // Two frames for the new scene to build what it builds on arrival.
            yield return null;
            yield return null;
            _leaving = false;
            yield return To(1f, 0f, InTime);
            _running = null;
        }

        IEnumerator To(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _image.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            _image.color = new Color(0f, 0f, 0f, to);
        }
    }
}
