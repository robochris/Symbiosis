using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Flyer (Winged ant): hovers and circles the player just out of reach, then dives in to bite and pulls away.
    /// It is airborne, so ground hazards like acid pools never touch it.
    /// </summary>
    public class FlyerEnemy : Enemy
    {
        public enum State { Circle, Windup, Dive, PullAway }

        [Header("Flight")]
        public float moveSpeed = 5f;
        public float turnSpeed = 540f;
        [Tooltip("How far from the player it circles.")]
        public float circleRadius = 5f;
        public float hoverHeight = 1.1f;
        public float bobAmount = 0.2f;

        [Header("Dive")]
        public float minCircleTime = 1.4f;
        public float maxCircleTime = 2.8f;
        public float windupTime = 0.35f;
        public float diveSpeed = 13f;
        public float diveTime = 0.55f;
        public float biteReach = 1.5f;
        public int damage = 1;
        public float pullAwayTime = 0.8f;

        public State Current { get; private set; }
        public override bool IgnoresGroundHazards => true;

        float _timer;
        float _side = 1f;
        float _height;
        bool _bit;

        protected override void OnEnable()
        {
            base.OnEnable();
            _side = Random.value < 0.5f ? -1f : 1f;
            _height = hoverHeight;
            Enter(State.Circle, Random.Range(minCircleTime, maxCircleTime));
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                SetVisualScale(1f);
                Move(Vector3.zero);
                Hover(hoverHeight);
                return;
            }

            Vector3 toPlayer = targetPosition - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            switch (Current)
            {
                case State.Circle:
                    // Aim for a point on a ring around the player, a little further round than where we are now.
                    Vector3 outward = distance > 0.1f ? -toPlayer / distance : transform.forward;
                    Vector3 ringPoint = targetPosition + Quaternion.Euler(0f, 50f * _side, 0f) * outward * circleRadius;
                    Face(SteerToward(ringPoint) - transform.position, turnSpeed);
                    Move(transform.forward * (moveSpeed * SpeedScale));
                    Hover(hoverHeight);
                    if (Tick() && CanSee(targetPosition)) Enter(State.Windup, windupTime);
                    break;

                case State.Windup:
                    // Rises and swells before the dive: the telegraph.
                    Face(toPlayer, turnSpeed);
                    SetVisualScale(Mathf.Lerp(1.3f, 1f, _timer / windupTime));
                    Move(Vector3.zero);
                    Hover(hoverHeight + 0.5f);
                    if (Tick()) { _bit = false; Enter(State.Dive, diveTime); }
                    break;

                case State.Dive:
                    SetVisualScale(1f);
                    Move(transform.forward * (diveSpeed * SpeedScale));
                    Hover(0.35f);
                    if (!_bit && distance <= biteReach + ReachBonus(targetHealth)) _bit = targetHealth.TakeDamage(damage, transform.forward);
                    if (Tick()) Enter(State.PullAway, pullAwayTime);
                    break;

                case State.PullAway:
                    Face(-toPlayer, turnSpeed);
                    Move(transform.forward * (moveSpeed * SpeedScale));
                    Hover(hoverHeight);
                    if (Tick())
                    {
                        _side = -_side;
                        Enter(State.Circle, Random.Range(minCircleTime, maxCircleTime));
                    }
                    break;
            }
        }

        // Only the body goes up and down; the collider stays on the ground so shots at ground height still hit.
        void Hover(float target)
        {
            if (visual == null) return;
            _height = Mathf.MoveTowards(_height, target, 6f * Time.deltaTime);
            float bob = Mathf.Sin(Time.time * 6f + GetInstanceID()) * bobAmount;
            visual.localPosition = Vector3.up * (_height + bob);
        }

        void Enter(State state, float duration)
        {
            Current = state;
            _timer = duration;
        }

        bool Tick()
        {
            _timer -= Time.deltaTime * SpeedScale;
            return _timer <= 0f;
        }
    }
}
