using System;
using System.Collections;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The cat, at the bottom. It is not angry and it cannot be killed: its bar is how interested it still is. It
    /// stalks, bats at where the player is standing, and now and then crouches and pounces on where they are going.
    /// Twice in the fight (at two thirds and one third interest) it sits down and washes, which is the time to hurt
    /// it, and gets up quicker and starts knocking things off shelves. When it has no interest left it does not die.
    /// It turns round and leaves.
    /// </summary>
    public class CatBoss : BossEnemy
    {
        public enum State { Stalk, Swipe, Crouch, Pounce, Groom }

        /// <summary>The cat has sat down to wash. The radio has something to say about it.</summary>
        public static event Action<CatBoss> Bored;

        [Header("Stalking")]
        public float moveSpeed = 3.2f;
        public float turnSpeed = 220f;
        [Tooltip("It bats at the player from inside this distance.")]
        public float swipeRange = 7f;

        [Header("Paw")]
        public float swipeInterval = 3.8f;
        public float swipeRadius = 2.8f;
        [Tooltip("Seconds between the warning and the paw landing.")]
        public float swipeFuse = 0.85f;

        [Header("Pounce")]
        public float pounceInterval = 8f;
        [Tooltip("Seconds it wiggles before it goes. The warning is on the floor from the start.")]
        public float crouchTime = 1.1f;
        public float pounceTime = 0.35f;
        public float pounceRadius = 4f;
        [Range(0f, 1.5f), Tooltip("How far ahead of a moving player it aims, in seconds of their movement.")]
        public float lead = 0.7f;

        [Header("Washing")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        [Tooltip("Seconds it sits still, washing. The time to hurt it.")]
        public float groomTime = 2.8f;
        [Tooltip("Speed in each phase.")]
        public float[] phaseSpeed = { 1f, 1.2f, 1.4f };

        [Header("Things off shelves (after the first wash)")]
        public float shelfInterval = 7f;
        public float shelfRadius = 2.2f;
        public float shelfFuse = 1.4f;
        [Tooltip("How many things come down each time, in phase 2 and in phase 3.")]
        public int[] shelfCount = { 2, 4 };

        public State Current { get; private set; }
        public int Swipes { get; private set; }
        public int Pounces { get; private set; }
        public int Washes { get; private set; }
        public int ThingsKnockedOff { get; private set; }
        /// <summary>The cat walking away, after the last fight. Null until it has left.</summary>
        public static CatLeaving LastSeen { get; private set; }

        float _timer, _nextSwipe, _nextPounce, _nextShelf;
        Vector3 _pounceFrom, _pounceTo, _lastTargetPosition, _targetVelocity;
        bool _started;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Stalk;
            Swipes = Pounces = Washes = ThingsKnockedOff = 0;
            _started = false;
        }

        float Speed(int phase) => phaseSpeed != null && phaseSpeed.Length >= phase ? phaseSpeed[phase - 1] : 1f;

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            if (!_started)
            {
                _started = true;
                _nextSwipe = Time.time + 1.6f;
                _nextPounce = Time.time + pounceInterval * 0.6f;
                _nextShelf = Time.time + shelfInterval;
                _lastTargetPosition = targetPosition;
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

            float interest = Health01;
            bool busy = Current == State.Groom || Current == State.Crouch || Current == State.Pounce;
            if (!busy && ((Phase == 1 && interest <= phase2At) || (Phase == 2 && interest <= phase3At))) StartWash();

            switch (Current)
            {
                case State.Groom:
                    Move(Vector3.zero);
                    // A slow nod, as if at a paw.
                    SetVisualScale(1f + Mathf.Sin(Time.time * 6f) * 0.03f);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) EndWash();
                    break;

                case State.Swipe:
                    Face(toTarget, turnSpeed);
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f) Current = State.Stalk;
                    break;

                case State.Crouch:
                    Face(_pounceTo - transform.position, turnSpeed * 2f);
                    Move(Vector3.zero);
                    // The wiggle.
                    SetVisualScale(1f - 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 16f)));
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Current = State.Pounce;
                        _timer = pounceTime;
                        _pounceFrom = transform.position;
                        Pounces++;
                    }
                    break;

                case State.Pounce:
                    {
                        // Through the air to the mark: it lands as the warning goes off.
                        _timer -= Time.deltaTime;
                        Vector3 leg = _pounceTo - _pounceFrom;
                        leg.y = 0f;
                        Move(pounceTime > 0f ? leg / pounceTime : Vector3.zero);
                        if (_timer <= 0f)
                        {
                            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.5f);
                            _nextSwipe = Time.time + 1.2f;
                            Current = State.Stalk;
                        }
                        break;
                    }

                default:
                    Stalk(targetPosition, toTarget, distance);
                    break;
            }
        }

        void Stalk(Vector3 targetPosition, Vector3 toTarget, float distance)
        {
            Vector3 heading = SteerToward(targetPosition) - transform.position;
            heading.y = 0f;
            Face(CanSee(targetPosition) ? toTarget : heading, turnSpeed);
            // It closes to paw's reach and then stops, the way they do.
            bool close = distance < swipeRange * 0.6f;
            Move(close || heading.sqrMagnitude < 0.01f ? Vector3.zero : heading.normalized * (moveSpeed * SpeedScale));

            if (Time.time >= _nextPounce)
            {
                _nextPounce = Time.time + pounceInterval / SpeedScale;
                Vector3 mark = targetPosition + _targetVelocity * lead;
                mark.y = 0f;
                // Not through the wall: if the way to the mark is not clear, it goes for where they are.
                if (!CanSee(mark)) mark = new Vector3(targetPosition.x, 0f, targetPosition.z);
                // And only onto floor it can walk on afterwards, or it could land where nothing can reach it.
                if (UnityEngine.AI.NavMesh.SamplePosition(mark, out var onFloor, 1.5f, UnityEngine.AI.NavMesh.AllAreas)) mark = new Vector3(onFloor.position.x, 0f, onFloor.position.z);
                else mark = new Vector3(transform.position.x, 0f, transform.position.z);
                _pounceTo = mark;
                GroundHazard.SpawnBlast(mark, pounceRadius, crouchTime / Mathf.Max(0.1f, SpeedScale) + pounceTime, telegraphMaterial);
                Current = State.Crouch;
                _timer = crouchTime;
                return;
            }

            if (distance <= swipeRange && Time.time >= _nextSwipe)
            {
                _nextSwipe = Time.time + swipeInterval / SpeedScale;
                GroundHazard.SpawnBlast(new Vector3(targetPosition.x, 0f, targetPosition.z), swipeRadius, swipeFuse, telegraphMaterial);
                Swipes++;
                Current = State.Swipe;
                _timer = swipeFuse;
                return;
            }

            if (Phase >= 2 && Time.time >= _nextShelf)
            {
                _nextShelf = Time.time + shelfInterval / SpeedScale;
                int count = shelfCount != null && shelfCount.Length >= Phase - 1 ? shelfCount[Phase - 2] : 3;
                for (int i = 0; i < count; i++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * 7f;
                    GroundHazard.SpawnBlast(new Vector3(targetPosition.x + offset.x, 0f, targetPosition.z + offset.y), shelfRadius, shelfFuse + i * 0.15f, telegraphMaterial);
                    ThingsKnockedOff++;
                }
            }
        }

        void StartWash()
        {
            Phase++;
            Washes++;
            Current = State.Groom;
            _timer = groomTime;
            Sfx.Play(SfxId.BossAppear);
            Bored?.Invoke(this);
        }

        void EndWash()
        {
            SetVisualScale(1f);
            SpeedMultiplier = Speed(Phase);
            _nextSwipe = Time.time + 1f;
            _nextPounce = Time.time + 2.5f;
            _nextShelf = Time.time + 1.5f;
            Current = State.Stalk;
        }

        // Dr. Finch, from inside the wall, pushes out what is left in his flask. It is the nearest he gets to thanks.
        // The item is always the dose.
        protected override ItemDefinition RewardItem(ItemLibrary items)
        {
            var dose = items.Get(ItemType.FinchsDose);
            return dose != null ? dose : base.RewardItem(items);
        }

        // It does not fall over. A copy of how it looks turns away from the player and walks off, and this one is gone.
        protected override void OnDeath()
        {
            Sfx.Play(SfxId.BossAppear);
            Vector3 away = transform.forward * -1f;
            if (TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                away = transform.position - targetPosition;
                away.y = 0f;
                away = away.sqrMagnitude > 0.01f ? away.normalized : -transform.forward;
            }
            if (visual != null)
            {
                var copy = Instantiate(visual.gameObject, visual.position, visual.rotation);
                copy.name = "The Cat, leaving";
                copy.transform.localScale = visual.lossyScale;
                LastSeen = copy.AddComponent<CatLeaving>();
                LastSeen.direction = away;
            }
            StartCoroutine(Gone());
        }

        IEnumerator Gone()
        {
            yield return null;
            gameObject.SetActive(false);
        }
    }

    /// <summary>What is left of the cat once it has lost interest: its back, getting further away.</summary>
    public class CatLeaving : MonoBehaviour
    {
        public Vector3 direction = Vector3.back;
        public float speed = 4.5f;
        public float seconds = 3.5f;

        /// <summary>How far it has walked.</summary>
        public float Walked { get; private set; }

        float _age;
        Vector3 _size;

        void Start() => _size = transform.localScale;

        void Update()
        {
            _age += Time.deltaTime;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 240f * Time.deltaTime);
            }
            // Unhurried at first, the way they leave a room they have decided was beneath them.
            float pace = speed * Mathf.Clamp01(_age / 0.8f);
            transform.position += transform.forward * (pace * Time.deltaTime);
            Walked += pace * Time.deltaTime;
            float left = seconds - _age;
            if (left < 0.6f) transform.localScale = _size * Mathf.Clamp01(left / 0.6f);
            if (left <= 0f) Destroy(gameObject);
        }
    }
}
