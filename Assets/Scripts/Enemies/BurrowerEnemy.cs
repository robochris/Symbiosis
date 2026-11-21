using UnityEngine;
using UnityEngine.AI;

namespace Shrunk
{
    /// <summary>
    /// The eighth kind: the Burrower (the antlion). Up top it edges toward the player for a little while, and can be
    /// shot. Then it sinks: underground it cannot be hit or seen. A moment later a circle is marked under the player,
    /// and when it lands the burrower comes up there, and hurts whoever is standing on it. The time to shoot it is
    /// just after it surfaces.
    /// </summary>
    public class BurrowerEnemy : Enemy
    {
        public enum State { Up, Under, Rising }

        public float moveSpeed = 1.8f;
        public float turnSpeed = 300f;
        [Tooltip("Seconds up top between dives.")]
        public float upTime = 2.6f;
        [Tooltip("Seconds underground before its mark appears.")]
        public float underTime = 1.1f;
        public float riseRadius = 1.6f;
        public float riseFuse = 0.9f;
        public Material telegraphMaterial;

        public State Current { get; private set; }
        /// <summary>Times it has come up under somebody.</summary>
        public int Rises { get; private set; }

        float _timer;
        Vector3 _riseAt;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Up;
            _timer = upTime * Random.Range(0.5f, 1f);
            Rises = 0;
            Show(true);
        }

        protected override void OnDisable()
        {
            Show(true);
            base.OnDisable();
        }

        void Show(bool shown)
        {
            if (visual != null) visual.gameObject.SetActive(shown);
            Targetable = shown;
            if (Controller != null) Controller.detectCollisions = shown;
            if (Health == null) return;
            if (shown) Health.ClearInvulnerability();
            else Health.SetInvulnerable(underTime + riseFuse + 1f);
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _timer -= Time.deltaTime * SpeedScale;

            switch (Current)
            {
                case State.Under:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    // The mark goes down where the player is now, on the floor.
                    _riseAt = targetPosition;
                    if (NavMesh.SamplePosition(_riseAt, out var hit, 2f, NavMesh.AllAreas)) _riseAt = hit.position;
                    _riseAt.y = transform.position.y;
                    GroundHazard.SpawnBlast(new Vector3(_riseAt.x, 0f, _riseAt.z), riseRadius, riseFuse, telegraphMaterial);
                    Current = State.Rising;
                    _timer = riseFuse;
                    break;

                case State.Rising:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    Controller.enabled = false;
                    transform.position = _riseAt;
                    Controller.enabled = true;
                    Show(true);
                    Rises++;
                    Sfx.Play(SfxId.Blast, 0.6f);
                    Current = State.Up;
                    _timer = upTime;
                    break;

                default:
                    Face(toTarget, turnSpeed);
                    Move(toTarget.sqrMagnitude > 4f ? (SteerToward(targetPosition) - transform.position).normalized * (moveSpeed * SpeedScale) : Vector3.zero);
                    if (_timer > 0f) break;
                    Show(false);
                    Current = State.Under;
                    _timer = underTime;
                    break;
            }
        }
    }
}
