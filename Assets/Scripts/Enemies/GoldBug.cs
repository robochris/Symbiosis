using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The gold bug. It never attacks: it turns up in a fight now and then, runs from the player, and after a few
    /// seconds is gone, taking what it was carrying with it. Put down before it goes, it bursts into coins. The room
    /// does not wait for it: when everything else is dead it leaves at once.
    /// </summary>
    public class GoldBug : Enemy
    {
        /// <summary>Coins it drops on top of what anything its size would.</summary>
        public const int Coins = 12;

        public float moveSpeed = 6.2f;
        public float turnSpeed = 720f;
        [Tooltip("Seconds it stays before it is gone.")]
        public float staysFor = 11f;

        /// <summary>True once it has got away.</summary>
        public bool Escaped { get; private set; }

        float _leaves, _side = 1f, _nextTurn;

        protected override void OnEnable()
        {
            base.OnEnable();
            _leaves = Time.time + staysFor;
            Escaped = false;
        }

        protected override void Think()
        {
            // Out of time, or the last one left in the room: gone.
            bool alone = true;
            foreach (var other in Active) if (other != this && !(other is GoldBug) && !other.Health.IsDead && other.gameObject.activeSelf) alone = false;
            if (Time.time >= _leaves || (alone && Time.time >= _leaves - staysFor + 2f))
            {
                Escaped = true;
                Fx.Burst(transform.position + Vector3.up * 0.5f, new Color(1f, 0.82f, 0.2f), 10, 4f, 0.12f, 0.4f);
                Vanish();
                return;
            }

            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            Vector3 away = transform.position - targetPosition;
            away.y = 0f;
            away = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
            // Along the wall when it meets one, and it changes its mind about which way.
            bool blocked = (Controller.collisionFlags & CollisionFlags.Sides) != 0;
            if (blocked && Time.time >= _nextTurn)
            {
                _side = -_side;
                _nextTurn = Time.time + 0.7f;
            }
            Vector3 heading = away + Quaternion.Euler(0f, 90f * _side, 0f) * away * (blocked ? 1.6f : 0.5f);
            Face(heading, turnSpeed);
            Move(heading.normalized * (moveSpeed * SpeedScale));
        }
    }
}
