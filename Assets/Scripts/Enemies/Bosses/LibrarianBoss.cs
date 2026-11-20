using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Librarian, and (with the shushing switched off) the Trainee before them. They keep their distance and
    /// throw books in a short run, each one marked on the floor before it lands, the later ones aimed where the
    /// player is heading. The Librarian's own trick is the shush: after a moment's warning the room has to be quiet,
    /// and for as long as it lasts every shot the player fires brings a book down on their head. The answer is to
    /// stop shooting and keep moving. At two thirds and one third health they speed up and send for overdue notices.
    /// </summary>
    public class LibrarianBoss : BossEnemy
    {
        [Header("Moving")]
        public float moveSpeed = 2.6f;
        public float turnSpeed = 300f;
        [Tooltip("The distance from the player they try to keep.")]
        public float keepRange = 9f;

        [Header("Books")]
        public float bookInterval = 4.6f;
        [Tooltip("Books in a run, in each phase.")]
        public int[] booksPerRun = { 2, 3, 4 };
        public float bookRadius = 1.9f;
        public float bookFuse = 1.3f;
        [Tooltip("Seconds between one book of a run being marked and the next.")]
        public float bookGap = 0.45f;

        [Header("Shush")]
        public bool canShush = true;
        public float shushInterval = 13f;
        [Tooltip("Seconds between the warning and the quiet starting.")]
        public float shushWarning = 1.3f;
        [Tooltip("Seconds the room has to stay quiet.")]
        public float quietTime = 3.2f;
        public float punishRadius = 2.4f;
        public float punishFuse = 0.55f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        [Tooltip("What they send for at each phase change.")]
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public int BooksThrown { get; private set; }
        public int Shushes { get; private set; }
        /// <summary>Books brought down on the player for making a noise during a shush.</summary>
        public int Punished { get; private set; }
        /// <summary>True from the warning until the quiet starts.</summary>
        public bool Warning => Time.time < _quietFrom && _quietFrom > 0f && Time.time >= _quietFrom - shushWarning;
        /// <summary>True while the room has to be quiet.</summary>
        public bool IsQuiet => _quietFrom > 0f && Time.time >= _quietFrom && Time.time < _quietUntil;

        float _nextRun, _nextBook, _nextShush, _quietFrom, _quietUntil, _nextPunish, _strafe = 1f, _nextTurn;
        int _booksLeft, _bookIndex, _shotsSeen;
        Vector3 _lastTargetPosition, _targetVelocity;
        bool _started, _wasQuiet;

        protected override void OnEnable()
        {
            base.OnEnable();
            BooksThrown = Shushes = Punished = 0;
            _booksLeft = 0;
            _quietFrom = _quietUntil = 0f;
            _started = _wasQuiet = false;
        }

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
                _nextRun = Time.time + 1.5f;
                _nextShush = Time.time + shushInterval * 0.6f;
                _lastTargetPosition = targetPosition;
            }

            if (Time.deltaTime > 0f)
            {
                Vector3 velocity = (targetPosition - _lastTargetPosition) / Time.deltaTime;
                velocity.y = 0f;
                _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 0.15f);
                _lastTargetPosition = targetPosition;
            }

            float health = Health01;
            if ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At)) NextPhase();

            Shush(targetPosition);
            ThrowBooks(targetPosition);
            Walk(targetPosition);
        }

        void Walk(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            bool sees = CanSee(targetPosition);
            Vector3 heading;
            if (!sees)
            {
                heading = SteerToward(targetPosition) - transform.position;
                heading.y = 0f;
            }
            else
            {
                Vector3 radial = distance > 0.01f ? toTarget / distance : transform.forward;
                Vector3 tangent = Quaternion.Euler(0f, 90f * _strafe, 0f) * radial;
                heading = tangent * 0.6f + radial * Mathf.Clamp((distance - keepRange) / 3f, -1f, 1f);
            }

            bool blocked = (Controller.collisionFlags & CollisionFlags.Sides) != 0;
            if (Time.time >= _nextTurn && (blocked || Random.value < Time.deltaTime * 0.2f))
            {
                _strafe = -_strafe;
                _nextTurn = Time.time + 1f;
            }

            Face(sees ? toTarget : heading, turnSpeed);
            // A finger to the lips, and they stand still for it.
            bool still = Warning || IsQuiet;
            SetVisualScale(still ? 1.15f : 1f);
            Move(still || heading.sqrMagnitude < 0.01f ? Vector3.zero : heading.normalized * (moveSpeed * SpeedScale));
        }

        void ThrowBooks(Vector3 targetPosition)
        {
            if (_booksLeft == 0 && !Warning && !IsQuiet && Time.time >= _nextRun)
            {
                _nextRun = Time.time + bookInterval / SpeedScale;
                _booksLeft = booksPerRun != null && booksPerRun.Length >= Phase ? booksPerRun[Phase - 1] : 2;
                _bookIndex = 0;
                _nextBook = Time.time;
            }
            if (_booksLeft == 0 || Time.time < _nextBook) return;

            // The first at their feet; each one after a little further along the way they are going.
            Vector3 mark = targetPosition + _targetVelocity * (0.45f * _bookIndex);
            GroundHazard.SpawnBlast(new Vector3(mark.x, 0f, mark.z), bookRadius, bookFuse, telegraphMaterial);
            BooksThrown++;
            _bookIndex++;
            _booksLeft--;
            _nextBook = Time.time + bookGap;
        }

        void Shush(Vector3 targetPosition)
        {
            var player = PlayerController.Instance;
            int shots = player != null && player.weapon != null ? player.weapon.ShotsFired : 0;

            if (canShush && _quietFrom <= 0f && _booksLeft == 0 && Time.time >= _nextShush)
            {
                _quietFrom = Time.time + shushWarning;
                _quietUntil = _quietFrom + quietTime;
                _nextShush = _quietUntil + shushInterval / SpeedScale;
                _nextRun = Mathf.Max(_nextRun, _quietUntil + 0.6f);
                Shushes++;
                Sfx.Play(SfxId.BossAppear);
                Say("SHH. Stop shooting.", shushWarning + 0.4f);
            }

            bool quiet = IsQuiet;
            if (quiet && !_wasQuiet) Say("Quiet. Every shot brings a book down on you.", quietTime);
            if (!quiet && _wasQuiet)
            {
                _quietFrom = 0f;
                Say("You may shoot.", 1.5f);
            }
            _wasQuiet = quiet;

            // A noise in the quiet: a book, on the head of whoever made it, and not much warning.
            if (quiet && shots > _shotsSeen && Time.time >= _nextPunish)
            {
                _nextPunish = Time.time + 0.4f;
                GroundHazard.SpawnBlast(new Vector3(targetPosition.x, 0f, targetPosition.z), punishRadius, punishFuse, telegraphMaterial);
                Punished++;
            }
            _shotsSeen = shots;
        }

        void Say(string words, float seconds)
        {
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(words, seconds);
        }

        void NextPhase()
        {
            Phase++;
            SpeedMultiplier = phaseSpeed != null && phaseSpeed.Length >= Phase ? phaseSpeed[Phase - 1] : 1f;
            Sfx.Play(SfxId.BossAppear);
            for (int i = 0; friends != null && friends.Length > 0 && i < friendsPerPhase; i++)
            {
                Enemy friend = friends[Random.Range(0, friends.Length)];
                float side = i % 2 == 0 ? -1f : 1f;
                if (friend != null) RequestMinion(friend, transform.position + transform.right * (side * 3.5f), false);
            }
        }
    }
}
