using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Fridge's boss. It has one job and it does it at the player. It trundles after them slowly and takes turns
    /// between two things: a volley of ice cubes lobbed at and around where they stand, each leaving a patch of
    /// frost that slows whoever walks through it, and a row of icicles that comes up out of the floor in a line from
    /// the machine through the player, one after another. Below half health every third turn is a deep freeze: a
    /// very large disc round the machine itself, with frozen peas let out when it goes off. Stay close for the
    /// volleys and the rows; be somewhere else for the freeze.
    /// </summary>
    public class IceMaker : BossEnemy
    {
        public enum State { Trundle, VolleyWindup, RowWindup, FreezeWindup }

        [Header("Trundling")]
        public float moveSpeed = 1.9f;
        public float turnSpeed = 150f;
        [Tooltip("Seconds between one attack ending and the next winding up.")]
        public float attackGap = 2.4f;

        [Header("Volley of ice cubes")]
        public float volleyWindup = 0.6f;
        public int cubes = 4;
        public float cubeRadius = 2.2f;
        public float cubeFuse = 1.15f;
        [Tooltip("The frost a cube leaves where it lands. It slows; it does not hurt.")]
        public Material frostMaterial;
        public Material cubeMaterial;

        [Header("Row of icicles")]
        public float rowWindup = 0.8f;
        public int icicles = 7;
        public float icicleSpacing = 2.6f;
        public float icicleRadius = 1.8f;
        [Tooltip("Seconds before the first one comes up, and between each one after it.")]
        public float icicleFirst = 0.45f;
        public float icicleStep = 0.14f;

        [Header("Deep freeze (below half health)")]
        [Range(0f, 1f)] public float freezeAt = 0.5f;
        public float freezeRadius = 9f;
        public float freezeFuse = 1.7f;
        public Enemy[] friends;
        public int friendsPerFreeze = 2;

        public State Current { get; private set; }
        public int Volleys { get; private set; }
        public int Rows { get; private set; }
        public int Freezes { get; private set; }

        // Cubes in the air: where each lands, and when.
        readonly List<Vector3> _fallingAt = new List<Vector3>();
        readonly List<float> _fallingWhen = new List<float>();

        float _timer, _nextAttack, _friendsAt;
        int _turns;
        Vector3 _lastTargetPosition, _targetVelocity;
        bool _started;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Trundle;
            Volleys = Rows = Freezes = 0;
            _turns = 0;
            _started = false;
            _friendsAt = 0f;
            _fallingAt.Clear();
            _fallingWhen.Clear();
        }

        protected override void Think()
        {
            Land();

            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            if (!_started)
            {
                _started = true;
                _nextAttack = Time.time + 1.5f;
                _lastTargetPosition = targetPosition;
            }
            if (Time.deltaTime > 0f)
            {
                Vector3 velocity = (targetPosition - _lastTargetPosition) / Time.deltaTime;
                velocity.y = 0f;
                _targetVelocity = Vector3.Lerp(_targetVelocity, velocity, 0.15f);
                _lastTargetPosition = targetPosition;
            }

            Phase = Health01 <= freezeAt ? 2 : 1;
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            switch (Current)
            {
                case State.Trundle:
                    Face(SteerToward(targetPosition) - transform.position, turnSpeed);
                    Move(distance > 6f ? transform.forward * (moveSpeed * SpeedScale) : Vector3.zero);
                    if (Time.time < _nextAttack) break;

                    _turns++;
                    if (Phase >= 2 && _turns % 3 == 0)
                    {
                        Current = State.FreezeWindup;
                        _timer = freezeFuse;
                        Freezes++;
                        _friendsAt = Time.time + freezeFuse;
                        GroundHazard.SpawnBlast(transform.position, freezeRadius, freezeFuse, telegraphMaterial);
                    }
                    else if (_turns % 2 == 1)
                    {
                        Current = State.VolleyWindup;
                        _timer = volleyWindup;
                    }
                    else
                    {
                        Current = State.RowWindup;
                        _timer = rowWindup;
                    }
                    break;

                case State.VolleyWindup:
                    Face(toTarget, turnSpeed);
                    SetVisualScale(Mathf.Lerp(1.12f, 1f, _timer / volleyWindup));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        Volley(targetPosition);
                        Rest();
                    }
                    break;

                case State.RowWindup:
                    // The line on the floor is where the row will come up.
                    Face(toTarget, turnSpeed * 1.5f);
                    ShowChargeLine(1f - _timer / rowWindup, 3f + icicles * icicleSpacing, icicleRadius * 1.6f, telegraphMaterial);
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime * SpeedScale;
                    if (_timer <= 0f)
                    {
                        HideChargeLine();
                        Row();
                        Rest();
                    }
                    break;

                case State.FreezeWindup:
                    // Shudders while the disc fills in.
                    SetVisualScale(1f + 0.05f * Mathf.Sin(Time.time * 26f));
                    Move(Vector3.zero);
                    _timer -= Time.deltaTime;
                    if (_timer <= 0f)
                    {
                        SetVisualScale(1f);
                        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.5f);
                        for (int i = 0; friends != null && friends.Length > 0 && i < friendsPerFreeze; i++)
                        {
                            Enemy friend = friends[Random.Range(0, friends.Length)];
                            float side = i % 2 == 0 ? -1f : 1f;
                            if (friend != null) RequestMinion(friend, transform.position + transform.right * (side * 4f), false);
                        }
                        Rest();
                    }
                    break;
            }
        }

        void Rest()
        {
            Current = State.Trundle;
            _nextAttack = Time.time + attackGap / SpeedScale;
        }

        // One where the player is heading, the rest round where they stand.
        void Volley(Vector3 targetPosition)
        {
            Volleys++;
            int count = cubes + (Phase >= 2 ? 1 : 0);
            float turn = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                Vector3 at = i == 0
                    ? targetPosition + _targetVelocity * (cubeFuse * 0.7f)
                    : targetPosition + Quaternion.Euler(0f, turn + 360f * i / (count - 1), 0f) * Vector3.forward * (cubeRadius * 1.9f);
                at.y = 0f;
                GroundHazard.SpawnBlast(at, cubeRadius, cubeFuse, telegraphMaterial);
                LobVisual.Create(transform.position + Vector3.up * 3f, at, cubeFuse, cubeMaterial != null ? cubeMaterial : frostMaterial);
                _fallingAt.Add(at);
                _fallingWhen.Add(Time.time + cubeFuse);
            }
        }

        void Land()
        {
            for (int i = _fallingAt.Count - 1; i >= 0; i--)
            {
                if (Time.time < _fallingWhen[i]) continue;
                if (frostMaterial != null) GroundHazard.SpawnStickyPatch(_fallingAt[i], frostMaterial);
                _fallingAt.RemoveAt(i);
                _fallingWhen.RemoveAt(i);
            }
        }

        // A line of them, from the machine outwards, each a beat after the last.
        void Row()
        {
            Rows++;
            Vector3 direction = transform.forward;
            for (int i = 0; i < icicles; i++)
            {
                Vector3 at = transform.position + direction * (3f + i * icicleSpacing);
                at.y = 0f;
                GroundHazard.SpawnBlast(at, icicleRadius, icicleFirst + i * icicleStep, telegraphMaterial);
            }
        }
    }
}
