using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Charger (Soldier ant): slow and tanky. When it has a clear line to the player it winds up, then dashes
    /// in a straight line. It can't turn mid-charge, so stepping aside (or dodging) beats it.
    /// </summary>
    public class ChargerEnemy : Enemy
    {
        public enum State { Approach, Windup, Charge, Recover }

        [Header("Walk")]
        public float moveSpeed = 3f;
        public float turnSpeed = 300f;

        [Header("Charge")]
        [Tooltip("Starts winding up when the player is this close and in plain sight.")]
        public float triggerRange = 9f;
        public float windupTime = 0.8f;
        public float chargeSpeed = 14f;
        public float chargeTime = 0.9f;
        [Tooltip("The charge hits if the player is within this distance.")]
        public float hitReach = 1.9f;
        public int damage = 1;
        public float recoverTime = 1.1f;
        [Tooltip("The warning strip shown along the charge's path while it winds up.")]
        public Material telegraphMaterial;
        public float telegraphWidth = 1.1f;

        public State Current { get; private set; }

        float _timer;
        float _chargedFor;
        bool _hit;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Approach;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                HideChargeLine();
                SetVisualScale(1f);
                Move(Vector3.zero);
                return;
            }

            Vector3 toPlayer = targetPosition - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            switch (Current)
            {
                case State.Approach:
                    Face(SteerToward(targetPosition) - transform.position, turnSpeed);
                    Move(transform.forward * (moveSpeed * SpeedScale));
                    if (distance <= triggerRange && CanSee(targetPosition)) Enter(State.Windup, windupTime);
                    break;

                case State.Windup:
                    // Lowers its head and swells while it takes aim: the telegraph.
                    Face(toPlayer, turnSpeed);
                    SetVisualScale(Mathf.Lerp(1.35f, 1f, _timer / windupTime));
                    ShowChargeLine(1f - _timer / windupTime, chargeSpeed * SpeedScale * chargeTime, telegraphWidth * BaseScale, telegraphMaterial);
                    Move(Vector3.zero);
                    if (Tick())
                    {
                        HideChargeLine();
                        _hit = false;
                        _chargedFor = 0f;
                        Enter(State.Charge, chargeTime);
                    }
                    break;

                case State.Charge:
                    SetVisualScale(1f);
                    Move(transform.forward * (chargeSpeed * SpeedScale));
                    _chargedFor += Time.deltaTime;
                    if (!_hit && distance <= hitReach + ReachBonus(targetHealth)) _hit = targetHealth.TakeDamage(damage, transform.forward);

                    // Running into a wall (or the player) ends the charge early.
                    bool crashed = _chargedFor > 0.1f && (Controller.collisionFlags & CollisionFlags.Sides) != 0;
                    if (crashed || Tick()) Enter(State.Recover, recoverTime);
                    break;

                case State.Recover:
                    Move(Vector3.zero);
                    if (Tick()) Enter(State.Approach, 0f);
                    break;
            }
        }

        void Enter(State state, float duration)
        {
            Current = state;
            _timer = duration;
        }

        bool Tick()
        {
            _timer -= Time.deltaTime * (Current == State.Charge ? 1f : SpeedScale);
            return _timer <= 0f;
        }
    }
}
