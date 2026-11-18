using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Chaser (Worker ant): runs straight at the player, rears back for a moment, then lunges to bite.
    /// The wind-up is the tell that gives the player time to dodge.
    /// </summary>
    public class ChaserEnemy : Enemy
    {
        public enum State { Idle, Approach, Windup, Lunge, Recover }

        [Header("Chase")]
        public float moveSpeed = 4.2f;
        public float turnSpeed = 420f;

        [Header("Bite")]
        [Tooltip("Starts the wind-up when the player is this close.")]
        public float attackRange = 2.2f;
        public float windupTime = 0.4f;
        public float lungeSpeed = 11f;
        public float lungeTime = 0.2f;
        [Tooltip("The bite lands if the player is within this distance during the lunge.")]
        public float biteReach = 1.4f;
        public int biteDamage = 1;
        public float recoverTime = 0.6f;

        public State Current { get; private set; }

        float _timer;
        bool _bitLanded;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Approach;
        }

        void Update()
        {
            if (Health.IsDead) return;

            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead)
            {
                Current = State.Idle;
                SetVisualScale(1f);
                Move(Vector3.zero);
                return;
            }
            if (Current == State.Idle) Current = State.Approach;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            switch (Current)
            {
                case State.Approach:
                    Face(toPlayer, turnSpeed);
                    Move(transform.forward * moveSpeed);
                    if (distance <= attackRange) Enter(State.Windup, windupTime);
                    break;

                case State.Windup:
                    // Rear back and swell: this is the telegraph.
                    Face(toPlayer, turnSpeed * 0.5f);
                    SetVisualScale(Mathf.Lerp(1.3f, 1f, _timer / windupTime));
                    Move(-transform.forward * 0.8f);
                    if (Tick()) { _bitLanded = false; Enter(State.Lunge, lungeTime); }
                    break;

                case State.Lunge:
                    SetVisualScale(1f);
                    Move(transform.forward * lungeSpeed);
                    if (!_bitLanded && distance <= biteReach)
                    {
                        _bitLanded = player.Health.TakeDamage(biteDamage, transform.forward);
                    }
                    if (Tick()) Enter(State.Recover, recoverTime);
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
            _timer -= Time.deltaTime;
            return _timer <= 0f;
        }

        void SetVisualScale(float scale)
        {
            if (visual != null) visual.localScale = Vector3.one * scale;
        }
    }
}
