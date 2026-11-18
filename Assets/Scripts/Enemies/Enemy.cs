using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Shared by every enemy: the list of live enemies, knockback, and dying. Behaviours (Chaser, Charger…) extend this.</summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public abstract class Enemy : MonoBehaviour
    {
        /// <summary>Every enemy currently in play. Used by aim assist and the spawner.</summary>
        public static readonly List<Enemy> Active = new List<Enemy>();
        public static event Action<Enemy> Killed;

        [Tooltip("The body mesh; scaled for telegraphs and the death pop.")]
        public Transform visual;
        public float knockbackOnHit = 5f;
        public float knockbackDecay = 14f;

        public Health Health { get; private set; }
        protected CharacterController Controller { get; private set; }
        protected Vector3 Knockback;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Controller = GetComponent<CharacterController>();
        }

        protected virtual void OnEnable()
        {
            Active.Add(this);
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            Knockback = Vector3.zero;
            if (visual != null) visual.localScale = Vector3.one;
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
        }

        void OnDamaged(int amount, Vector3 direction)
        {
            direction.y = 0f;
            Knockback = direction.normalized * knockbackOnHit;
        }

        void OnDied()
        {
            Killed?.Invoke(this);
            StartCoroutine(DeathPop());
        }

        IEnumerator DeathPop()
        {
            const float duration = 0.15f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (visual != null) visual.localScale = Vector3.one * Mathf.Lerp(1.2f, 0f, t / duration);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        /// <summary>Moves by the behaviour's own velocity plus any knockback, keeping the enemy on the floor.</summary>
        protected void Move(Vector3 velocity)
        {
            Knockback = Vector3.MoveTowards(Knockback, Vector3.zero, knockbackDecay * Time.deltaTime);
            velocity += Knockback;
            velocity.y = -5f;
            Controller.Move(velocity * Time.deltaTime);
        }

        protected void Face(Vector3 direction, float degreesPerSecond)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            var target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
        }
    }
}
