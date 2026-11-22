using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// Music, made by the game itself: each world gets its own key, mode and tempo (picked from its name, so it is the
    /// same every time), as a short loop in two layers. The calm layer (a soft pad, a bass, a slow arpeggio) plays
    /// while exploring; when a fight starts the fight layer (drums and a quick arpeggio over the same chords) fades in,
    /// and out again when the room is cleared. The lab has a gentle theme of its own. Nothing is stored on disk.
    /// </summary>
    public class Music : MonoBehaviour
    {
        public const int Rate = 22050;
        /// <summary>Tests switch it off: the loops take a moment to make.</summary>
        public static bool Off;
        public static Music Instance { get; private set; }
        public static string Playing { get; private set; }

        static readonly Dictionary<string, AudioClip[]> Made = new Dictionary<string, AudioClip[]>();
        AudioSource _calm, _fight;
        float _fightLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<Music>();
        }

        void Awake()
        {
            _calm = gameObject.AddComponent<AudioSource>();
            _fight = gameObject.AddComponent<AudioSource>();
            foreach (var source in new[] { _calm, _fight })
            {
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0f;
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            FloorManager.FloorBuilt += OnFloorBuilt;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            FloorManager.FloorBuilt -= OnFloorBuilt;
        }

        void Start() => PickTheme();

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PickTheme();
        void OnFloorBuilt(FloorManager floors) => PickTheme();

        /// <summary>Picks the tune again (the jukebox has changed it).</summary>
        public static void Refresh()
        {
            if (Instance != null) Instance.PickTheme();
        }

        void PickTheme()
        {
            if (Off) return;
            var floors = FloorManager.Instance;
            string theme = floors != null && floors.biome != null ? floors.biome.id : Jukebox.Playing ?? "lab";
            if (theme == Playing) return;
            Playing = theme;
            if (!Made.TryGetValue(theme, out var clips))
            {
                // Only the lab's and this world's are kept: on a phone, every world's would add up.
                foreach (var key in new List<string>(Made.Keys))
                {
                    if (key == "lab") continue;
                    foreach (var old in Made[key]) Destroy(old);
                    Made.Remove(key);
                }
                clips = new[] { MakeClip(theme, false), MakeClip(theme, true) };
                Made[theme] = clips;
            }
            _calm.clip = clips[0];
            _fight.clip = clips[1];
            _calm.Play();
            _fight.Play();
            // The two layers are the same length and start together, so they stay in step.
            _fight.timeSamples = _calm.timeSamples;
        }

        void Update()
        {
            BossRecords.Watch();
            if (Off) { _calm.volume = _fight.volume = 0f; return; }
            // A boss's last phase: the music quickens.
            var boss = BossEnemy.InPlay;
            float pitch = boss != null && boss.Phase >= 3 ? 1.08f : 1f;
            _calm.pitch = _fight.pitch = Mathf.MoveTowards(_calm.pitch, pitch, Time.unscaledDeltaTime * 0.2f);
            var floors = FloorManager.Instance;
            bool fighting = floors != null && (floors.IsLocked || BossEnemy.InPlay != null);
            _fightLevel = Mathf.MoveTowards(_fightLevel, fighting ? 1f : 0f, Time.unscaledDeltaTime / 1.5f);
            float volume = GameSettings.MusicVolume * GameSettings.Volume;
            _calm.volume = volume * 0.55f;
            _fight.volume = volume * 0.55f * _fightLevel;
            // Paused, the music goes quiet rather than stopping.
            if (Time.timeScale == 0f) { _calm.volume *= 0.4f; _fight.volume *= 0.4f; }
        }

        // ---------- making the loops ----------

        static AudioClip MakeClip(string theme, bool fight)
        {
            float[] data = Render(theme, fight);
            var clip = AudioClip.Create("Music " + theme + (fight ? " fight" : ""), data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>One loop of a theme's calm or fight layer, as samples. Public for tests.</summary>
        public static float[] Render(string theme, bool fight)
        {
            int seed = 0;
            foreach (char c in theme) seed = seed * 31 + c;
            var rng = new System.Random(seed);
            bool lab = theme == "lab";
            bool minor = !lab && rng.Next(3) != 0;
            int root = 45 + rng.Next(12);              // A2 to G#3
            float bpm = lab ? 84f : 96f + rng.Next(5) * 8f;
            int[] scale = minor ? new[] { 0, 2, 3, 5, 7, 8, 10 } : new[] { 0, 2, 4, 5, 7, 9, 11 };
            int[][] progressions = minor
                ? new[] { new[] { 0, 5, 2, 6 }, new[] { 0, 3, 4, 0 }, new[] { 0, 6, 5, 4 } }
                : new[] { new[] { 0, 5, 3, 4 }, new[] { 0, 3, 4, 3 }, new[] { 0, 4, 5, 3 } };
            int[] chords = progressions[rng.Next(progressions.Length)];
            int arpStyle = rng.Next(3);

            int beat = Mathf.RoundToInt(Rate * 60f / bpm);
            int bars = 8;
            int length = beat * 4 * bars;
            var data = new float[length];
            var noise = new System.Random(seed ^ 0x5EED);

            for (int bar = 0; bar < bars; bar++)
            {
                int degree = chords[bar % chords.Length];
                int[] tones = { Note(root, scale, degree), Note(root, scale, degree + 2), Note(root, scale, degree + 4) };
                int barStart = bar * beat * 4;

                if (!fight)
                {
                    // The pad: the chord, soft, all bar long.
                    foreach (int tone in tones) Pad(data, barStart, beat * 4, Freq(tone + 12), 0.05f);
                    // The bass: on the one and the three.
                    for (int b = 0; b < 4; b += 2) Pluck(data, barStart + b * beat, beat * 2, Freq(tones[0] - 12), 0.16f, 0);
                    // A slow arpeggio, a note a beat, a little bell-like.
                    for (int b = 0; b < 4; b++) Pluck(data, barStart + b * beat, beat, Freq(tones[(b + arpStyle) % 3] + 24), 0.035f, 2);
                }
                else
                {
                    // The fight layer: a quick arpeggio in sixteenths, a driving bass in eighths, and drums.
                    for (int s = 0; s < 16; s++)
                    {
                        int index = arpStyle == 0 ? s % 3 : arpStyle == 1 ? (s % 4 == 3 ? 1 : s % 3) : 2 - s % 3;
                        Pluck(data, barStart + s * beat / 4, beat / 4, Freq(tones[index] + 24), 0.03f, 1);
                    }
                    for (int e = 0; e < 8; e++) Pluck(data, barStart + e * beat / 2, beat / 2, Freq(tones[0] - 12), 0.07f, 1);
                    for (int b = 0; b < 4; b++)
                    {
                        if (b % 2 == 0) Kick(data, barStart + b * beat);
                        else Snare(data, barStart + b * beat, noise);
                        Hat(data, barStart + b * beat + beat / 2, noise);
                    }
                }
            }

            // Smooth the very start and end of the loop, and keep it in bounds.
            for (int i = 0; i < length; i++) data[i] = Mathf.Clamp((float)System.Math.Tanh(data[i] * 1.2f), -1f, 1f);
            return data;
        }

        static int Note(int root, int[] scale, int degree) => root + scale[degree % 7] + 12 * (degree / 7);
        static float Freq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static void Pad(float[] data, int start, int length, float freq, float volume)
        {
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, i / (Rate * 0.4f)) * Mathf.Min(1f, (length - i) / (Rate * 0.3f));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 1.003f * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
                data[start + i] += wave * env * volume;
            }
        }

        // shape 0: triangle bass; 1: soft square; 2: bell (sine with a fast fall).
        static void Pluck(float[] data, int start, int length, float freq, float volume, int shape)
        {
            float decay = shape == 2 ? 6f : 4f;
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float phase = freq * t % 1f;
                float wave = shape == 0 ? 1f - 4f * Mathf.Abs(phase - 0.5f)
                    : shape == 1 ? (phase < 0.5f ? 0.6f : -0.6f)
                    : Mathf.Sin(2f * Mathf.PI * freq * t);
                float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, (length - i) / (Rate * 0.01f));
                data[start + i] += wave * env * volume;
            }
        }

        static void Kick(float[] data, int start)
        {
            int length = (int)(Rate * 0.18f);
            float phase = 0f;
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float freq = Mathf.Lerp(130f, 45f, t / 0.18f);
                phase += freq / Rate;
                data[start + i] += Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Exp(-t * 18f) * 0.4f;
            }
        }

        static void Snare(float[] data, int start, System.Random noise)
        {
            int length = (int)(Rate * 0.12f);
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)Rate;
                data[start + i] += ((float)noise.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 30f) * 0.16f;
            }
        }

        static void Hat(float[] data, int start, System.Random noise)
        {
            int length = (int)(Rate * 0.04f);
            float last = 0f;
            for (int i = 0; i < length && start + i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float n = (float)noise.NextDouble() * 2f - 1f;
                data[start + i] += (n - last) * Mathf.Exp(-t * 80f) * 0.05f;
                last = n;
            }
        }
    }
}
