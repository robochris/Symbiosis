using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Lobber: keeps well back and throws things over everybody's heads. Each throw lands where the player is
    /// heading, after a warning disc has filled in, so it punishes running in a straight line and standing still
    /// alike. Cover does not help; closing the distance does, because it has to stop and back away.
    /// </summary>
    public class LobberEnemy : Enemy
    {
        [Header("Positioning")]
        public float moveSpeed = 3f;
        public float turnSpeed = 300f;
        [Tooltip("Backs away when the player is closer than this.")]
        public float minRange = 8f;
        [Tooltip("Moves in when the player is farther than this.")]
        public float maxRange = 14f;

        [Header("Throw")]
        public float lobInterval = 3.6f;
        [Tooltip("Swells for this long before each throw: the telegraph.")]
        public float windupTime = 0.6f;
        public float blastRadius = 2.3f;
        [Tooltip("Seconds between the throw and the landing.")]
        public float fuse = 1.15f;
        [Range(0f, 1f), Tooltip("How much it aims ahead of a moving target. 1 = exactly where they will be.")]
        public float lead = 0.75f;
        [Tooltip("The warning disc.")]
        public Material blastMaterial;
        [Tooltip("The thing in the air.")]
        public Material lobMaterial;

        public int Lobs { get; private set; }
        /// <summary>Where the last throw was aimed.</summary>
        public Vector3 LastTarget { get; private set; }

        float _nextLob, _windup;
        bool _winding;
        Vector3 _lastTargetPosition, _targetVelocity;
        bool _tracking;

        protected override void OnEnable()
        {
            base.OnEnable();
            _winding = false;
            _tracking = false;
            _nextLob = Time.time + 1.2f;
            Lobs = 0;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                SetVisualScale(1f);
                Move(Vector3.zero);
                return;
            }

            if (!_tracking)
            {
                _tracking = true;
                _lastTargetPosition = targetPosition;
                _targetVelocity = Vector3.zero;
            }
            if (Time.deltaTime > 0f)
            {
                Vector3 velocity = (targetPosition - _lastTargetPosition) / Time.deltaTime;
                velocity.y = 0f;
                _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 0.15f);
                _lastTargetPosition = targetPosition;
            }

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (_winding)
            {
                Face(toTarget, turnSpeed);
                SetVisualScale(Mathf.Lerp(1.3f, 1f, _windup / windupTime));
                Move(Vector3.zero);
                _windup -= Time.deltaTime * SpeedScale;
                if (_windup <= 0f) Lob(targetPosition);
                return;
            }

            SetVisualScale(1f);
            if (distance < minRange)
            {
                Face(toTarget, turnSpeed);
                Move(-toTarget.normalized * (moveSpeed * SpeedScale));
            }
            else if (distance > maxRange)
            {
                Face(SteerToward(targetPosition) - transform.position, turnSpeed);
                Move(transform.forward * (moveSpeed * SpeedScale));
            }
            else
            {
                Face(toTarget, turnSpeed);
                Move(Vector3.zero);
            }

            // It throws over walls, so it does not need to see its target: only to be near enough.
            if (Time.time >= _nextLob && distance <= maxRange + 3f)
            {
                _winding = true;
                _windup = windupTime;
            }
        }

        void Lob(Vector3 targetPosition)
        {
            _winding = false;
            _nextLob = Time.time + lobInterval / SpeedScale;
            Lobs++;

            Vector3 at = targetPosition + _targetVelocity * (fuse * lead);
            at.y = 0f;
            LastTarget = at;
            GroundHazard.SpawnBlast(at, blastRadius, fuse, blastMaterial);
            LobVisual.Create(transform.position + Vector3.up * 1.4f, at, fuse, lobMaterial != null ? lobMaterial : blastMaterial);
        }
    }

    /// <summary>The thing a Lobber threw, in the air: it follows an arc and is gone when it lands. Only for show; the disc does the damage.</summary>
    public class LobVisual : MonoBehaviour
    {
        Vector3 _from, _to;
        float _time, _age;

        public static void Create(Vector3 from, Vector3 to, float seconds, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Lob";
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.5f;
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) renderer.sharedMaterial = material;
            var lob = go.AddComponent<LobVisual>();
            lob._from = from;
            lob._to = to;
            lob._time = Mathf.Max(0.05f, seconds);
        }

        void Update()
        {
            _age += Time.deltaTime;
            float along = Mathf.Clamp01(_age / _time);
            Vector3 at = Vector3.Lerp(_from, _to, along);
            at.y += 5f * 4f * along * (1f - along);
            transform.position = at;
            if (along >= 1f) Destroy(gameObject);
        }
    }
}
