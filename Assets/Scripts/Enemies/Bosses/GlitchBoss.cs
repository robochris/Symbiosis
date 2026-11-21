using UnityEngine;
using UnityEngine.AI;

namespace Shrunk
{
    /// <summary>
    /// The Glitch, and (with blinking and scanning switched off) the Cursor before it. Three moves in turn.
    /// Click: three marks in quick succession on the player, each landing a moment later. Blink: a mark where it is
    /// going, near the player; when the mark lands, it is there. Scan: rows of marks sweep across the floor toward
    /// the player, one row after another, each row with a gap in it; stand in the gap. At two thirds and one third it
    /// speeds up and sends for bugs.
    /// </summary>
    public class GlitchBoss : BossEnemy
    {
        public enum Act { Drift, Click, Blink, Scan }

        [Header("Moving")]
        public float moveSpeed = 2.4f;
        public float turnSpeed = 300f;
        public float keepRange = 8f;

        [Header("Click")]
        public int clicks = 3;
        public float clickRadius = 1.5f;
        public float clickFuse = 0.9f;
        public float clickGap = 0.35f;

        [Header("Blink")]
        public bool canBlink = true;
        public float blinkRange = 7f;
        public float blinkFuse = 0.9f;
        public float blinkRadius = 2.2f;

        [Header("Scan")]
        public bool canScan = true;
        [Tooltip("Rows in a sweep, in each phase.")]
        public int[] rowsPerScan = { 2, 3, 4 };
        public float rowGap = 0.55f;
        public float rowFuse = 1f;
        public float markRadius = 1.1f;
        [Tooltip("Marks either side of the middle of a row; one of them is left out.")]
        public int halfRow = 4;

        [Header("Rhythm")]
        public float restBetween = 1.5f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Clicks { get; private set; }
        public int Blinks { get; private set; }
        public int Rows { get; private set; }
        /// <summary>The gap left in the last row laid, as a position, for tests.</summary>
        public Vector3 LastGap { get; private set; }

        float _timer;
        int _left, _turn;
        Vector3 _blinkTo, _scanFrom, _scanAlong, _scanAcross;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = Act.Drift;
            _timer = 1.5f;
            Clicks = Blinks = Rows = 0;
            _turn = 0;
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            float health = Health01;
            if ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At)) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _timer -= Time.deltaTime * SpeedScale;

            switch (Current)
            {
                case Act.Click:
                    Move(Vector3.zero);
                    Face(toTarget, turnSpeed);
                    if (_timer > 0f) break;
                    if (_left-- > 0)
                    {
                        GroundHazard.SpawnBlast(new Vector3(targetPosition.x, 0f, targetPosition.z), clickRadius, clickFuse, telegraphMaterial);
                        Clicks++;
                        _timer = clickGap;
                        break;
                    }
                    Rest();
                    break;

                case Act.Blink:
                    Move(Vector3.zero);
                    // Flickers while the mark fills; when it lands, so does it.
                    SetVisualScale(Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 1f : 0.85f);
                    if (_timer > 0f) break;
                    SetVisualScale(1f);
                    Controller.enabled = false;
                    transform.position = _blinkTo;
                    Controller.enabled = true;
                    Blinks++;
                    Sfx.Play(SfxId.Dodge, 0.8f);
                    Rest();
                    break;

                case Act.Scan:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    if (_left > 0)
                    {
                        LayRow();
                        _left--;
                        _timer = rowGap;
                        break;
                    }
                    Rest();
                    break;

                default:
                    Drift(targetPosition, toTarget);
                    if (_timer <= 0f) NextMove(targetPosition);
                    break;
            }
        }

        // One row of marks across the sweep's line, with one left out: the way through.
        void LayRow()
        {
            int row = (rowsPerScan != null && rowsPerScan.Length >= Phase ? rowsPerScan[Phase - 1] : 3) - _left;
            Vector3 middle = _scanFrom + _scanAlong * (row * markRadius * 2.2f);
            int gap = Random.Range(-halfRow + 1, halfRow);
            for (int i = -halfRow; i <= halfRow; i++)
            {
                Vector3 at = middle + _scanAcross * (i * markRadius * 2f);
                if (i == gap)
                {
                    LastGap = at;
                    continue;
                }
                GroundHazard.SpawnBlast(new Vector3(at.x, 0f, at.z), markRadius, rowFuse, telegraphMaterial);
            }
            Rows++;
        }

        void NextMove(Vector3 targetPosition)
        {
            _turn++;
            // Click, then blink, then scan, in turn; without them, clicks only.
            int pick = _turn % 3;
            if (pick == 1 && canBlink && FindBlink(targetPosition, out Vector3 to))
            {
                _blinkTo = to;
                GroundHazard.SpawnBlast(new Vector3(to.x, 0f, to.z), blinkRadius, blinkFuse / SpeedScale, telegraphMaterial);
                Current = Act.Blink;
                _timer = blinkFuse;
                return;
            }
            if (pick == 2 && canScan)
            {
                // From a little behind the player's far side of it, sweeping back past them.
                Vector3 toPlayer = targetPosition - transform.position;
                toPlayer.y = 0f;
                _scanAlong = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
                _scanAcross = Vector3.Cross(Vector3.up, _scanAlong);
                _scanFrom = transform.position + _scanAlong * 2.5f;
                _left = rowsPerScan != null && rowsPerScan.Length >= Phase ? rowsPerScan[Phase - 1] : 3;
                Current = Act.Scan;
                _timer = 0f;
                return;
            }
            Current = Act.Click;
            _left = clicks;
            _timer = 0f;
        }

        static NavMeshPath _blinkPath;

        // Somewhere near the player it could have walked to: on the floor, inside the room, never through a wall.
        bool FindBlink(Vector3 targetPosition, out Vector3 to)
        {
            if (_blinkPath == null) _blinkPath = new NavMeshPath();
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 around = Random.insideUnitCircle.normalized * Random.Range(3f, blinkRange);
                Vector3 candidate = targetPosition + new Vector3(around.x, 0f, around.y);
                if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, _blinkPath) || _blinkPath.status != NavMeshPathStatus.PathComplete) continue;
                to = hit.position;
                to.y = transform.position.y;
                return true;
            }
            to = transform.position;
            return false;
        }

        void Rest()
        {
            Current = Act.Drift;
            _timer = restBetween;
        }

        void Drift(Vector3 targetPosition, Vector3 toTarget)
        {
            float distance = toTarget.magnitude;
            Vector3 heading;
            if (!CanSee(targetPosition))
            {
                heading = SteerToward(targetPosition) - transform.position;
                heading.y = 0f;
            }
            else
            {
                Vector3 radial = distance > 0.01f ? toTarget / distance : transform.forward;
                heading = Quaternion.Euler(0f, 90f, 0f) * radial * 0.6f + radial * Mathf.Clamp((distance - keepRange) / 3f, -1f, 1f);
            }
            Face(toTarget, turnSpeed);
            Move(heading.sqrMagnitude < 0.01f ? Vector3.zero : heading.normalized * (moveSpeed * SpeedScale));
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
