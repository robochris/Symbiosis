using UnityEngine;
using UnityEngine.AI;

namespace Shrunk
{
    /// <summary>
    /// The Spider. Three moves in turn. Web: strips of web laid across the floor, slowing whoever walks on them.
    /// Drop: it climbs out of sight (it cannot be hurt up there), a circle is marked under the player, and it comes
    /// down on it. Bite: when close, it rears and lunges. At two thirds and one third it speeds up and its spiderlings
    /// come.
    /// </summary>
    public class SpiderBoss : BossEnemy
    {
        public enum Act { Prowl, Web, Climbing, Dropping, Rear, Lunge }

        [Header("Web")]
        [Tooltip("Strips laid at once, in each phase.")]
        public int[] stripsPerWeb = { 2, 3, 4 };
        public float stripLength = 12f;
        public Material webMaterial;

        [Header("Drop")]
        public float climbTime = 1f;
        public float dropFuse = 1f;
        public float dropRadius = 2.2f;

        [Header("Bite")]
        public float biteRange = 3f;
        public float rearTime = 0.5f;
        public float lungeSpeed = 13f;
        public float lungeTime = 0.3f;
        public float biteReach = 1.8f;

        [Header("Moving")]
        public float moveSpeed = 3f;
        public float turnSpeed = 300f;
        public float restBetween = 1.8f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Webs { get; private set; }
        public int Drops { get; private set; }
        public int Bites { get; private set; }
        public bool Up => Current == Act.Climbing || Current == Act.Dropping;

        float _timer;
        int _turn;
        bool _bit, _bitSince;
        Vector3 _dropAt;

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = Act.Prowl;
            _timer = 1.5f;
            Webs = Drops = Bites = _turn = 0;
            Show(true);
        }

        protected override void OnDisable()
        {
            Show(true);
            base.OnDisable();
        }

        void Show(bool shown)
        {
            if (visual != null) visual.gameObject.SetActive(shown);
            Targetable = shown;
            if (Controller != null) Controller.detectCollisions = shown;
            if (Health == null) return;
            if (shown) Health.ClearInvulnerability();
            else Health.SetInvulnerable(climbTime + dropFuse + 1f);
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                Move(Vector3.zero);
                return;
            }

            float health = Health01;
            if (Current == Act.Prowl && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            _timer -= Time.deltaTime * (Current == Act.Lunge ? 1f : SpeedScale);

            switch (Current)
            {
                case Act.Climbing:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    _dropAt = targetPosition;
                    if (NavMesh.SamplePosition(_dropAt, out var hit, 1.5f, NavMesh.AllAreas)) _dropAt = hit.position;
                    _dropAt.y = transform.position.y;
                    GroundHazard.SpawnBlast(new Vector3(_dropAt.x, 0f, _dropAt.z), dropRadius, dropFuse, telegraphMaterial);
                    Current = Act.Dropping;
                    _timer = dropFuse;
                    break;

                case Act.Dropping:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    Controller.enabled = false;
                    transform.position = _dropAt;
                    Controller.enabled = true;
                    Show(true);
                    Drops++;
                    Sfx.Play(SfxId.Blast, 0.7f);
                    Rest();
                    break;

                case Act.Rear:
                    Move(Vector3.zero);
                    Face(toTarget, turnSpeed);
                    SetVisualScale(1.12f);
                    if (_timer > 0f) break;
                    SetVisualScale(1f);
                    Current = Act.Lunge;
                    _timer = lungeTime;
                    _bit = false;
                    Bites++;
                    break;

                case Act.Lunge:
                    Move(transform.forward * lungeSpeed);
                    if (!_bit && targetHealth != null && distance <= biteReach) _bit = targetHealth.TakeDamage(1, transform.forward);
                    if (_timer <= 0f) Rest();
                    break;

                default:
                    Face(toTarget, turnSpeed);
                    Move(distance > 2.5f ? (SteerToward(targetPosition) - transform.position).normalized * (moveSpeed * SpeedScale) : Vector3.zero);
                    // One bite between moves, so standing close does not keep it from webbing and climbing.
                    if (distance <= biteRange && _timer < restBetween * 0.5f && !_bitSince)
                    {
                        _bitSince = true;
                        Current = Act.Rear;
                        _timer = rearTime;
                        break;
                    }
                    if (_timer > 0f) break;
                    _turn++;
                    _bitSince = false;
                    if (_turn % 2 == 1) LayWeb(targetPosition);
                    else
                    {
                        Show(false);
                        Current = Act.Climbing;
                        _timer = climbTime;
                    }
                    break;
            }
        }

        // Strips of web across the floor, through and around the player: slow going, all of it.
        void LayWeb(Vector3 targetPosition)
        {
            int strips = stripsPerWeb != null && stripsPerWeb.Length >= Phase ? stripsPerWeb[Phase - 1] : 2;
            float start = Random.value * 180f;
            for (int i = 0; i < strips; i++)
            {
                Vector3 along = Quaternion.Euler(0f, start + i * 180f / strips, 0f) * Vector3.forward;
                Vector3 offset = Quaternion.Euler(0f, 90f, 0f) * along * ((i - (strips - 1) * 0.5f) * 3f);
                for (float d = -stripLength * 0.5f; d <= stripLength * 0.5f; d += 2.3f)
                {
                    Vector3 at = targetPosition + offset + along * d;
                    if (!NavMesh.SamplePosition(at, out var hit, 0.8f, NavMesh.AllAreas)) continue;
                    GroundHazard.SpawnStickyPatch(hit.position, webMaterial);
                }
            }
            Webs++;
            Rest();
        }

        void Rest()
        {
            Current = Act.Prowl;
            _timer = restBetween;
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
