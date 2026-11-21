using UnityEngine;
using UnityEngine.AI;

namespace Shrunk
{
    /// <summary>
    /// The Jack-in-the-Box, and (with the box switched off and the charges switched on) the Wind-up Mouse before it.
    /// Jack: hides, then marks several circles round the player and springs up out of one of them; every circle goes
    /// off. Up, it can be hurt, and throws toy jacks that land on marks; then it hides again.
    /// Mouse: charges along a marked line, again and again, then winds down and sits still, open, until wound up.
    /// At two thirds and one third both speed up and send for friends.
    /// </summary>
    public class ToyBoss : BossEnemy
    {
        public enum Act { Up, Hidden, Springing, Windup, Charge, WoundDown }

        [Header("Jack")]
        public bool canHide = true;
        [Tooltip("Circles marked when it springs, in each phase.")]
        public int[] circlesPerSpring = { 3, 4, 5 };
        public float circleRadius = 1.5f;
        public float springFuse = 1.1f;
        public float hideTime = 0.8f;
        public float upTime = 3.2f;
        public int jacksPerThrow = 3;
        public float jackRadius = 1.1f;
        public float jackFuse = 1f;

        [Header("Mouse")]
        public bool canCharge;
        public int chargesInARow = 3;
        public float chargeWindup = 0.8f;
        public float chargeSpeed = 15f;
        public float chargeTime = 0.6f;
        public float chargeWidth = 1.8f;
        public float chargeReach = 1.6f;
        public float woundDownTime = 3f;

        [Header("Moving")]
        public float moveSpeed = 2f;
        public float turnSpeed = 280f;

        [Header("Phases")]
        [Range(0f, 1f)] public float phase2At = 0.66f;
        [Range(0f, 1f)] public float phase3At = 0.33f;
        public float[] phaseSpeed = { 1f, 1.15f, 1.3f };
        public Enemy[] friends;
        public int friendsPerPhase = 2;

        public Act Current { get; private set; }
        public int Springs { get; private set; }
        public int Throws { get; private set; }
        public int Charges { get; private set; }
        public int WindDowns { get; private set; }
        public bool Hidden => Current == Act.Hidden || Current == Act.Springing;

        float _timer;
        int _chargesLeft;
        bool _hit;
        Vector3 _springTo;

        protected override void OnEnable()
        {
            base.OnEnable();
            Springs = Throws = Charges = WindDowns = 0;
            Current = Act.Up;
            _timer = 1.5f;
            Show(true);
        }

        protected override void OnDisable()
        {
            HideChargeLine();
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
            else Health.SetInvulnerable(hideTime + springFuse + 1f);
        }

        protected override void Think()
        {
            if (!TryGetTarget(out Vector3 targetPosition, out Health targetHealth))
            {
                HideChargeLine();
                Move(Vector3.zero);
                return;
            }

            float health = Health01;
            if (!Hidden && Current != Act.Charge && ((Phase == 1 && health <= phase2At) || (Phase == 2 && health <= phase3At))) NextPhase();

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _timer -= Time.deltaTime * (Current == Act.Charge ? 1f : SpeedScale);

            switch (Current)
            {
                case Act.Hidden:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    Spring(targetPosition);
                    break;

                case Act.Springing:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    Controller.enabled = false;
                    transform.position = _springTo;
                    Controller.enabled = true;
                    Show(true);
                    Springs++;
                    Sfx.Play(SfxId.BossAppear, 0.6f);
                    Current = Act.Up;
                    _timer = upTime;
                    _thrown = false;
                    break;

                case Act.Windup:
                    Move(Vector3.zero);
                    if (_timer > chargeWindup * 0.3f) Face(toTarget, turnSpeed * 1.5f);
                    ShowChargeLine(1f - Mathf.Clamp01(_timer / chargeWindup), chargeSpeed * chargeTime, chargeWidth, telegraphMaterial);
                    if (_timer > 0f) break;
                    HideChargeLine();
                    Current = Act.Charge;
                    _timer = chargeTime;
                    _hit = false;
                    Charges++;
                    break;

                case Act.Charge:
                    Move(transform.forward * chargeSpeed);
                    if (!_hit && targetHealth != null && toTarget.magnitude <= chargeReach) _hit = targetHealth.TakeDamage(1, transform.forward);
                    if (_timer > 0f) break;
                    _chargesLeft--;
                    if (_chargesLeft > 0)
                    {
                        Current = Act.Windup;
                        _timer = chargeWindup;
                    }
                    else
                    {
                        // Run down: it sits there, ticking, and can be hit as much as anybody likes.
                        Current = Act.WoundDown;
                        _timer = woundDownTime;
                        WindDowns++;
                        SetVisualScale(0.92f);
                    }
                    break;

                case Act.WoundDown:
                    Move(Vector3.zero);
                    if (_timer > 0f) break;
                    SetVisualScale(1f);
                    Current = Act.Up;
                    _timer = 1.2f;
                    break;

                default:
                    Face(toTarget, turnSpeed);
                    Move(toTarget.magnitude > 6f ? (SteerToward(targetPosition) - transform.position).normalized * (moveSpeed * SpeedScale) : Vector3.zero);
                    if (canHide && !_thrown && _timer < upTime * 0.5f) ThrowJacks(targetPosition);
                    if (_timer > 0f) break;
                    if (canCharge)
                    {
                        _chargesLeft = chargesInARow + Phase - 1;
                        Current = Act.Windup;
                        _timer = chargeWindup;
                    }
                    else if (canHide)
                    {
                        Show(false);
                        Current = Act.Hidden;
                        _timer = hideTime;
                    }
                    else _timer = upTime;
                    break;
            }
        }

        bool _thrown;

        // Toy jacks over the top: each one marked where it will land, round the player.
        void ThrowJacks(Vector3 targetPosition)
        {
            _thrown = true;
            Throws++;
            for (int i = 0; i < jacksPerThrow + Phase - 1; i++)
            {
                Vector2 scatter = i == 0 ? Vector2.zero : Random.insideUnitCircle * 3.5f;
                GroundHazard.SpawnBlast(new Vector3(targetPosition.x + scatter.x, 0f, targetPosition.z + scatter.y), jackRadius, jackFuse, telegraphMaterial);
            }
        }

        static NavMeshPath _springPath;
        static NavMeshPath SpringPath => _springPath ?? (_springPath = new NavMeshPath());

        // Circles round the player, and it comes up out of one of them. Every one of them goes off.
        void Spring(Vector3 targetPosition)
        {
            int circles = circlesPerSpring != null && circlesPerSpring.Length >= Phase ? circlesPerSpring[Phase - 1] : 3;
            int real = Random.Range(0, circles);
            float start = Random.value * 360f;
            for (int i = 0; i < circles; i++)
            {
                Vector3 at = targetPosition + Quaternion.Euler(0f, start + i * 360f / circles, 0f) * Vector3.forward * (i == 0 ? 0f : 3.5f);
                // Only ever on the floor, inside the room: where there is none, the circle goes under the player instead.
                if (NavMesh.SamplePosition(at, out var hit, 1.5f, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, SpringPath) && SpringPath.status == NavMeshPathStatus.PathComplete)
                    at = hit.position;
                else at = targetPosition;
                GroundHazard.SpawnBlast(new Vector3(at.x, 0f, at.z), circleRadius, springFuse, telegraphMaterial);
                if (i == real) _springTo = new Vector3(at.x, transform.position.y, at.z);
            }
            Current = Act.Springing;
            _timer = springFuse;
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
