using UnityEngine;

namespace Shrunk
{
    /// <summary>Flashes every renderer under this object white for a moment when its Health takes damage.</summary>
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        public float duration = 0.08f;
        public Color color = Color.white;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        Health _health;
        Renderer[] _renderers;
        MaterialPropertyBlock _block;
        float _until;
        bool _flashing;

        void Awake()
        {
            _health = GetComponent<Health>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            Clear();
        }

        void OnDisable()
        {
            _health.Damaged -= OnDamaged;
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
            if (_flashing && Time.time >= _until) Clear();
        }

        void Clear()
        {
            _flashing = false;
            if (_renderers == null) return;
            foreach (var r in _renderers) r.SetPropertyBlock(null);
        }
    }
}
