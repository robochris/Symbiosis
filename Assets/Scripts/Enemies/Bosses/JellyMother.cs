using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Birthday Cake's mini boss: a gummy bear the size of a shed. She lumbers after the player and belly-flops
    /// when they are close (a disc round her fills in; get out of it), then lies there wobbling for a moment, which
    /// is the time to hurt her. She lobs gumdrops that grow into gummy bears if they are left. Twice, at two thirds
    /// and one third health, a piece of her tears off as two full-grown bears, and what is left is smaller and
    /// quicker. The jelly on her back takes double.
    /// </summary>
    public class JellyMother : BossEnemy
    {
        public enum State { Lumber, FlopWindup, Wobble, Split }

        [Header("Lumber")]
        public float walkSpeed = 2.6f;
        [Tooltip("She hurries when the player is far off, so a big arena can't be used to keep away for ever.")]
        public float farSpeed = 5f;
        public float hurryFrom = 8f;
        public float hurryFull = 15f;
        public float turnSpeed = 110f;

        [Header("Belly flop")]
        [Tooltip("Flops when the player is this close.")]
        public float flopRange = 4.6f;
        public float flopWindup = 0.85f;
        public float flopRadius = 4.8f;
        [Tooltip("Seconds she lies there afterwards.")]
        public float wobbleTime = 1.5f;
        public float flopCooldown = 1.2f;

        [Header("Gumdrops")]
        public LarvaEnemy gumdropPrefab;
        public float lobInterval = 7f;
        public int lobCount = 3;
        [Tooltip("No more are lobbed while this many are still about.")]
        public int maxGumdrops = 6;

        [Header("Splitting")]
        [Tooltip("What tears off her: two of these each time.")]
        public Enemy splitInto;
        public int bearsPerSplit = 2;
        public float splitTime = 1f;
        [Tooltip("Health shares at which a piece tears off.")]
        public float[] splitAt = { 0.66f, 0.33f };
        [Tooltip("Her size after each split.")]
        public float[] sizeAfter = { 0.84f, 0.7f };
        [Tooltip("Her speed after each split.")]
        public float[] speedAfter = { 1.25f, 1.55f };

        [Header("Weak point")]
        public int weakPointMultiplier = 2;

        public State Current { get; private set; }
        public int Flops { get; private set; }
        public int Lobbed { get; private set; }
        public int Splits { get; private set; }

        float _timer, _nextFlop, _nextLob, _fromSize, _toSize;
        float _radius, _height;

        protected override void Awake()
        {
            base.Awake();
            _radius = Controller.radius;
            _height = Controller.height;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Lumber;
            Flops = Lobbed = Splits = 0;
            _nextFlop = 0f;
            _nextLob = Time.time + lobInterval * 0.5f;
            SetSize(1f);
        }

        void SetSize(float size)
        {
            BaseScale = size;
            SetVisualScale(1f);
            Controller.radius = _radius * size;
            Controller.height = _height * size;
            Controller.center = new Vector3(0f, Controller.height * 0.5f, 0f);
        }

        // A shot travelling the same way she faces came from behind: it hit the jelly.
        int WeakPoint(Vector3 direction)
        {
            direction.y = 0f;
            return Vector3.Dot(direction.normalized, transform.forward) > 0.3f ? weakPointMultiplier : 1;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            if (Health.DamageScaler == null) Health.DamageScaler = WeakPoint;

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (Current != State.Split && Current != State.FlopWindup && splitAt != null && Splits < splitAt.Length && Health01 <= splitAt[Splits])
            {
                Current = State.Split;
                _timer = splitTime;
                _fromSize = BaseScale;
                _toSize = sizeAfter != null && sizeAfter.Length > Splits ? sizeAfter[Splits] : BaseScale;
            }

            switch (Current)
            {
                case State.Split:
                    // Swells, then tears.
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    SetVisualScale(1f + 0.25f * Mathf.Sin((1f - Mathf.Clamp01(_timer / splitTime)) * Mathf.PI));
                    if (_timer <= 0f) Tear();
                    break;

                case State.FlopWindup:
                    // Rears up: the warning, along with the disc on the floor.
                    SetVisualScale(Mathf.Lerp(1.2f, 1f, _timer / flopWindup));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Flops++;
                        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.45f);
                        Current = State.Wobble;
                        _timer = wobbleTime / SpeedScale;
                    }
                    break;

                case State.Wobble:
                    SetVisualScale(1f + 0.06f * Mathf.Sin(Time.time * 18f));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        _nextFlop = Time.time + flopCooldown;
                        Current = State.Lumber;
                    }
                    break;

                default:
                    Face(SteerToward(targetPosition) - transform.position, turnSpeed * SpeedScale);
                    Move(transform.forward * (SpeedAt(distance) * SpeedScale));

                    if (Time.time >= _nextLob)
                    {
                        _nextLob = Time.time + lobInterval / SpeedScale;
                        Lob(targetPosition);
                    }

                    if (distance <= flopRange * BaseScale + ReachBonus(targetHealth) && Time.time >= _nextFlop)
                    {
                        Current = State.FlopWindup;
                        _timer = flopWindup;
                        GroundHazard.SpawnBlast(transform.position, flopRadius * BaseScale, flopWindup, telegraphMaterial);
                    }
                    break;
            }
        }

        public float SpeedAt(float distance) => Mathf.Lerp(walkSpeed, farSpeed, Mathf.InverseLerp(hurryFrom, hurryFull, distance));

        void Lob(Vector3 targetPosition)
        {
            if (gumdropPrefab == null) return;
            int about = 0;
            foreach (var enemy in Active) if (enemy is LarvaEnemy && !enemy.Health.IsDead) about++;
            for (int i = 0; i < lobCount && about + i < maxGumdrops; i++)
            {
                Vector3 at = targetPosition + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2.5f, 4.5f);
                RequestMinion(gumdropPrefab, at, false);
                Fx.Burst(at + Vector3.up * 0.4f, Fx.ColourOf(visual), 8, 4f, 0.18f, 0.4f);
                Lobbed++;
            }
        }

        void Tear()
        {
            SetSize(_toSize);
            SpeedMultiplier = speedAfter != null && speedAfter.Length > Splits ? speedAfter[Splits] : SpeedMultiplier;
            Splits++;
            Phase = Splits + 1;
            Current = State.Lumber;
            _nextFlop = Time.time + 0.8f;
            Fx.Burst(transform.position + Vector3.up * 1.5f, Fx.ColourOf(visual), 30, 8f, 0.3f, 0.6f);
            Sfx.Play(SfxId.BossAppear);

            for (int i = 0; splitInto != null && i < bearsPerSplit; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                RequestMinion(splitInto, transform.position + transform.right * (side * 3.5f) - transform.forward * 1.5f, true);
            }
        }

        public override void DropRewards()
        {
            base.DropRewards();
            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null) return;
            var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            if (picks.Count > 0) Pickup.Item(picks[0], transform.position, 0);
        }
    }
}
