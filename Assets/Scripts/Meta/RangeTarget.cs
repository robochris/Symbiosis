using System;
using System.Collections;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A target on the lab's firing range. One shot knocks it down; it stands back up a moment later. Some slide from
    /// side to side. It is not an enemy: nothing aims itself at it, which is the point of a range.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class RangeTarget : MonoBehaviour
    {
        /// <summary>A target has just been knocked down.</summary>
        public static event Action<RangeTarget> Popped;

        [Tooltip("The part that disappears while it is down.")]
        public GameObject face;
        [Tooltip("How far it slides either side of where it stands, and along which way. Zero stands still.")]
        public Vector3 slide;
        public float slideSeconds = 3f;
        public float standsUpAfter = 1.2f;
        [Tooltip("This one is Dr. Finch's radio set, not a target: shooting it counts for nothing and he says so.")]
        public bool isRadio;
        [Tooltip("Above zero, this one is only up now and then: the seconds between its appearances.")]
        public float appearsEvery;
        [Tooltip("How long it stays up when it does appear.")]
        public float staysFor = 3.5f;
        [Tooltip("What knocking it down counts for on the range.")]
        public int worth = 1;

        /// <summary>True while a now-and-then target is out of sight, waiting for its next appearance.</summary>
        public bool IsHidden { get; private set; }
        float _nextChange;

        public bool IsDown { get; private set; }
        /// <summary>Up, in sight, not the secret one, and a round on: something aim assist may turn a shot toward.</summary>
        public bool Aimable => !IsDown && !IsHidden && !isRadio && FiringRange.Live;

        /// <summary>Every target in the scene, for aim assist: on a phone, shots only go off at something to aim at.</summary>
        public static readonly System.Collections.Generic.List<RangeTarget> All = new System.Collections.Generic.List<RangeTarget>();

        Health _health;
        Collider _collider;
        Vector3 _home;
        float _phase;

        void Awake()
        {
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
            _home = transform.position;
            _phase = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        void Start()
        {
            if (appearsEvery > 0f) Hide(true);
        }

        void Hide(bool hide)
        {
            IsHidden = hide;
            if (face != null) face.SetActive(!hide);
            if (_collider != null) _collider.enabled = !hide;
            _nextChange = Time.time + (hide ? appearsEvery : staysFor);
        }

        void OnEnable()
        {
            _health.Died += OnDied;
            All.Add(this);
        }

        void OnDisable()
        {
            _health.Died -= OnDied;
            All.Remove(this);
        }

        void Update()
        {
            if (appearsEvery > 0f && !IsDown && Time.time >= _nextChange) Hide(!IsHidden);
            if (slide.sqrMagnitude < 0.0001f || slideSeconds <= 0f) return;
            transform.position = _home + slide * Mathf.Sin(_phase + Time.time * (Mathf.PI * 2f / slideSeconds));
        }

        void OnDied()
        {
            if (IsDown) return;
            IsDown = true;
            if (face != null) face.SetActive(false);
            if (_collider != null) _collider.enabled = false;
            Fx.Burst(transform.position + Vector3.up, new Color(1f, 0.35f, 0.3f), 14, 5f, 0.16f, 0.4f);
            Sfx.Play(SfxId.Hit);
            if (isRadio)
            {
                if (Radio.Instance != null) Radio.Instance.Say(RadioMoment.ShotRadio);
            }
            else Popped?.Invoke(this);
            StartCoroutine(StandUp());
        }

        IEnumerator StandUp()
        {
            yield return new WaitForSeconds(standsUpAfter);
            _health.Restore(_health.maxHealth);
            IsDown = false;
            if (appearsEvery > 0f)
            {
                Hide(true);
                yield break;
            }
            if (face != null) face.SetActive(true);
            if (_collider != null) _collider.enabled = true;
        }
    }
}
