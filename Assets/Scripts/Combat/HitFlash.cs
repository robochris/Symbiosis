using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Colours the body: a white flash for a moment when its Health takes damage, and an optional lasting tint
    /// (used by mutations) the rest of the time.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        public float duration = 0.08f;
        public Color color = Color.white;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        Health _health;
        Renderer[] _renderers;
        Color[] _original;
        Color[] _tinted;
        MaterialPropertyBlock _block;
        float _until;
        bool _flashing;
        bool _hasTint;

        void Awake()
        {
            _health = GetComponent<Health>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            _original = new Color[_renderers.Length];
            _tinted = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var material = _renderers[i].sharedMaterial;
                _original[i] = material != null && material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : Color.white;
            }
        }

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _hasTint = false;
            _flashing = false;
            ApplyResting();
        }

        void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        /// <summary>Blends every part of the body toward a colour. Strength 0 = untouched, 1 = solid colour.</summary>
        public void SetTint(Color tint, float strength)
        {
            for (int i = 0; i < _renderers.Length; i++) _tinted[i] = Color.Lerp(_original[i], tint, strength);
            _hasTint = true;
            if (!_flashing) ApplyResting();
        }

        public void ClearTint()
        {
            _hasTint = false;
            if (!_flashing) ApplyResting();
        }

        void OnDamaged(int amount, Vector3 direction)
        {
            _block.SetColor(BaseColor, color);
            foreach (var r in _renderers) r.SetPropertyBlock(_block);
            _until = Time.time + duration;
            _flashing = true;
        }

        void Update()
        {
            if (_flashing && Time.time >= _until)
            {
                _flashing = false;
                ApplyResting();
            }
        }

        void ApplyResting()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_hasTint)
                {
                    _block.SetColor(BaseColor, _tinted[i]);
                    _renderers[i].SetPropertyBlock(_block);
                }
                else
                {
                    _renderers[i].SetPropertyBlock(null);
                }
            }
        }
    }
}
