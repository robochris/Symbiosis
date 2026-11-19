using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Small bursts of coloured bits: a shot landing, an ant dying, a pod popping, a drop splashing.
    /// One particle system does all of them, so a busy fight costs one draw call, not hundreds of objects.
    /// Call Fx.Burst from anywhere; with no Fx in the scene it does nothing.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        public static Fx Instance { get; private set; }
        /// <summary>Bursts asked for since the scene loaded. For tests.</summary>
        public static int Bursts { get; private set; }

        [Tooltip("A particle material. Each burst sets its own colour.")]
        public Material material;
        public int maxParticles = 400;

        ParticleSystem _system;

        void Awake()
        {
            Instance = this;
            Bursts = 0;

            _system = gameObject.AddComponent<ParticleSystem>();
            _system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.gravityModifier = 1.6f;
            main.startLifetime = 0.5f;

            var emission = _system.emission;
            emission.enabled = false;
            var shape = _system.shape;
            shape.enabled = false;

            // Bits shrink away instead of popping out of existence.
            var size = _system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _system.Play();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Throws out a handful of bits from a point.</summary>
        /// <param name="speed">How fast they fly, in metres per second.</param>
        /// <param name="size">How big each one starts, in metres.</param>
        public static void Burst(Vector3 position, Color colour, int count, float speed = 4f, float size = 0.16f, float lifetime = 0.45f)
        {
            Bursts++;
            if (Instance == null || Instance._system == null) return;

            var emit = new ParticleSystem.EmitParams { startColor = colour };
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) * 0.9f + 0.3f; // up and out, never into the floor
                emit.position = position + direction * 0.1f;
                emit.velocity = direction * (speed * Random.Range(0.5f, 1f));
                emit.startSize = size * Random.Range(0.6f, 1.2f);
                emit.startLifetime = lifetime * Random.Range(0.7f, 1.2f);
                Instance._system.Emit(emit, 1);
            }
        }

        /// <summary>The colour a thing is drawn in, for bits that should match it. White if it can't be told.</summary>
        public static Color ColourOf(Component thing)
        {
            var renderer = thing != null ? thing.GetComponentInChildren<Renderer>() : null;
            var material = renderer != null ? renderer.sharedMaterial : null;
            return material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
        }
    }
}
