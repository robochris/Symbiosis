using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    public enum SfxId
    {
        Shoot, Hit, EnemyDie, PlayerHurt, Coin, Item, Dodge, PowerOn, DoorLock, DoorOpen, Blast, Buy, NextFloor, BossAppear, Blocked
    }

    /// <summary>
    /// Placeholder sound. Every effect is a short beep, sweep or burst of noise made in code, so there are no audio files
    /// to manage until real sound is chosen. Call Sfx.Play(id) from anywhere.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int SampleRate = 44100;
        const int Voices = 10;

        static Sfx _instance;
        static bool _quitting;

        readonly Dictionary<SfxId, AudioClip> _clips = new Dictionary<SfxId, AudioClip>();
        readonly Dictionary<SfxId, float> _lastPlayed = new Dictionary<SfxId, float>();
        AudioSource[] _sources;
        int _next;

        /// <summary>How many sounds have been played. For tests.</summary>
        public static int Played { get; private set; }

        public static void Play(SfxId id, float volume = 1f)
        {
            if (_quitting || !Application.isPlaying) return;
            if (_instance == null)
            {
                var go = new GameObject("Sfx");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<Sfx>();
            }
            _instance.PlayNow(id, volume);
        }

        void Awake()
        {
            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].spatialBlend = 0f;
            }
        }

        void OnApplicationQuit()
        {
            _quitting = true;
        }

        void PlayNow(SfxId id, float volume)
        {
            // The same sound many times in one instant (a split shot hitting three ants) is just one louder noise.
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < 0.04f) return;
            _lastPlayed[id] = now;

            if (!_clips.TryGetValue(id, out var clip))
            {
                clip = Make(id);
                _clips[id] = clip;
            }

            AudioSource source = _sources[_next];
            _next = (_next + 1) % Voices;
            source.pitch = Random.Range(0.94f, 1.06f);
            source.volume = Mathf.Clamp01(volume) * GameSettings.Volume;
            source.clip = clip;
            source.Play();
            Played++;
        }

        // ---------- the sounds ----------

        static AudioClip Make(SfxId id)
        {
            switch (id)
            {
                case SfxId.Shoot: return Tone("shoot", 0.07f, 900f, 420f, 0.22f, Wave.Square);
                case SfxId.Hit: return Tone("hit", 0.05f, 300f, 160f, 0.3f, Wave.Noise);
                case SfxId.EnemyDie: return Tone("die", 0.16f, 520f, 90f, 0.35f, Wave.Square);
                case SfxId.PlayerHurt: return Tone("hurt", 0.25f, 180f, 70f, 0.5f, Wave.Saw);
                case SfxId.Coin: return Tone("coin", 0.08f, 1200f, 1700f, 0.2f, Wave.Sine);
                case SfxId.Item: return Notes("item", 0.09f, 0.3f, 660f, 880f, 1320f);
                case SfxId.Dodge: return Tone("dodge", 0.14f, 500f, 1400f, 0.2f, Wave.Noise);
                case SfxId.PowerOn: return Tone("power", 0.5f, 200f, 1600f, 0.35f, Wave.Saw);
                case SfxId.DoorLock: return Tone("lock", 0.18f, 140f, 60f, 0.5f, Wave.Square);
                case SfxId.DoorOpen: return Notes("open", 0.1f, 0.3f, 440f, 660f);
                case SfxId.Blast: return Tone("blast", 0.35f, 220f, 40f, 0.6f, Wave.Noise);
                case SfxId.Buy: return Notes("buy", 0.08f, 0.3f, 880f, 1100f, 1320f, 1760f);
                case SfxId.NextFloor: return Tone("floor", 0.45f, 700f, 180f, 0.3f, Wave.Sine);
                case SfxId.BossAppear: return Tone("boss", 0.7f, 110f, 55f, 0.6f, Wave.Saw);
                default: return Tone("blocked", 0.06f, 1500f, 1300f, 0.2f, Wave.Square);
            }
        }

        enum Wave { Sine, Square, Saw, Noise }

        // A single sweep from one pitch to another, fading out.
        static AudioClip Tone(string name, float seconds, float fromHz, float toHz, float volume, Wave wave)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            var random = new System.Random(name.GetHashCode());
            float phase = 0f, held = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float hz = Mathf.Lerp(fromHz, toHz, t);
                phase += hz / SampleRate;
                float envelope = (1f - t) * (1f - t) * Mathf.Min(1f, i / 60f);
                data[i] = Sample(wave, phase, random, ref held, hz) * envelope * volume;
            }
            return Clip(name, data);
        }

        // A quick run of notes, for the "something good happened" sounds.
        static AudioClip Notes(string name, float noteSeconds, float volume, params float[] notes)
        {
            int perNote = Mathf.CeilToInt(noteSeconds * SampleRate);
            var data = new float[perNote * notes.Length];
            float held = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float phase = 0f;
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / perNote;
                    phase += notes[n] / SampleRate;
                    float envelope = (1f - t * 0.7f) * Mathf.Min(1f, i / 60f);
                    data[n * perNote + i] = Sample(Wave.Sine, phase, null, ref held, notes[n]) * envelope * volume;
                }
            }
            return Clip(name, data);
        }

        static float Sample(Wave wave, float phase, System.Random random, ref float held, float hz)
        {
            float cycle = phase - Mathf.Floor(phase);
            switch (wave)
            {
                case Wave.Square: return cycle < 0.5f ? 0.6f : -0.6f;
                case Wave.Saw: return cycle * 2f - 1f;
                case Wave.Noise:
                    // Noise that changes at the sweep's pitch: lower pitch = rougher, deeper noise.
                    if (cycle < hz / SampleRate) held = (float)random.NextDouble() * 2f - 1f;
                    return held;
                default: return Mathf.Sin(cycle * Mathf.PI * 2f);
            }
        }

        static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create("sfx-" + name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
