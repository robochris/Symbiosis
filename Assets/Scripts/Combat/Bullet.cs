using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Everything a shot carries. Items add to these numbers, so effects stack and combine on their own:
    /// a piercing shot that also splits and carries acid does all three.
    /// </summary>
    public class Shot
    {
        public int damage = 1;
        /// <summary>How many more enemies it can pass through.</summary>
        public int pierce;
        /// <summary>Stacks of split: on hitting an enemy it throws off 3 smaller shots (5 with two stacks, and so on).</summary>
        public int split;
        /// <summary>Leaves an acid puddle where it lands.</summary>
        public int acid;
        /// <summary>Sets what it hits on fire.</summary>
        public int burn;
        public bool crit;
        /// <summary>Split-off shots keep every effect except splitting again.</summary>
        public bool isChild;
        /// <summary>Who fired it; it never hits them.</summary>
        public Health owner;

        public Shot Child() => new Shot
        {
            damage = damage, pierce = pierce, split = 0, acid = acid, burn = burn, crit = crit, isChild = true, owner = owner
        };
    }

    /// <summary>A pooled shot. Moves in a straight line and sweeps a sphere each frame so fast shots can't skip through thin targets.</summary>
    public class Bullet : MonoBehaviour
    {
        public float radius = 0.15f;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        readonly List<Health> _alreadyHit = new List<Health>();
        Shot _shot;
        Vector3 _direction;
        float _speed;
        float _range;
        float _travelled;
        Team _team;
        Pool<Bullet> _pool;

        public void Launch(Vector3 position, Vector3 direction, float speed, Shot shot, float range, Team team, Pool<Bullet> pool, Health alreadyHit = null)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            transform.localScale = Vector3.one * (shot.isChild ? 0.7f : 1f);
            _direction = direction;
            _speed = speed;
            _shot = shot;
            _range = range;
            _team = team;
            _pool = pool;
            _travelled = 0f;
            _alreadyHit.Clear();
            if (alreadyHit != null) _alreadyHit.Add(alreadyHit);
            gameObject.SetActive(true);
        }

        void Update()
        {
            float step = _speed * (_team == Team.Enemy ? Enemy.WorldSlow : 1f) * Time.deltaTime;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, _direction, Hits, step, ~0, QueryTriggerInteraction.Ignore);

            bool hitSomething = false;
            float nearest = float.MaxValue;
            Health nearestHealth = null;
            Vector3 nearestPoint = transform.position;

            for (int i = 0; i < count; i++)
            {
                var health = Hits[i].collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    // Pass through friends, corpses, the shooter, and anything this shot has already been through.
                    if (health.team == _team || health.IsDead || health == _shot.owner || _alreadyHit.Contains(health)) continue;
                }
                if (Hits[i].distance < nearest)
                {
                    nearest = Hits[i].distance;
                    nearestHealth = health;
                    // A cast that starts inside something reports distance 0 and no point.
                    nearestPoint = Hits[i].distance > 0f ? Hits[i].point : transform.position;
                    hitSomething = true;
                }
            }

            if (hitSomething)
            {
                Fx.Burst(nearestPoint, Colour, nearestHealth != null ? 5 : 3, 3f, 0.12f, 0.3f);
                bool keepFlying = nearestHealth != null && HitCharacter(nearestHealth, nearestPoint);
                if (nearestHealth == null && _shot.acid > 0) LeavePuddle(nearestPoint);
                if (!keepFlying)
                {
                    Despawn();
                    return;
                }
            }

            transform.position += _direction * step;
            _travelled += step;
            if (_travelled >= _range) Despawn();
        }

        // Order of effects: damage, then element, then split, then pierce. Returns true if the shot carries on.
        bool HitCharacter(Health health, Vector3 point)
        {
            health.TakeDamage(_shot.damage * (_shot.crit ? 2 : 1), _direction);

            if (_shot.burn > 0)
            {
                var enemy = health.GetComponent<Enemy>();
                if (enemy != null) enemy.Ignite(_shot.burn);
            }
            if (_shot.acid > 0) LeavePuddle(point);

            if (_shot.split > 0 && !_shot.isChild && _pool != null)
            {
                int children = 1 + 2 * _shot.split;
                float spread = 70f + 20f * (_shot.split - 1);
                for (int i = 0; i < children; i++)
                {
                    float angle = -spread * 0.5f + spread * i / (children - 1);
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * _direction;
                    _pool.Get().Launch(transform.position, direction, _speed, _shot.Child(), 9f, _team, _pool, health);
                }
            }

            if (_shot.pierce <= 0) return false;
            _shot.pierce--;
            _alreadyHit.Add(health);
            return true;
        }

        Color? _colour;
        Color Colour => _colour ?? (_colour = Fx.ColourOf(this)).Value;

        void LeavePuddle(Vector3 point)
        {
            var assets = GameAssets.Instance;
            if (assets == null) return;
            bool burning = _shot.burn > 0;
            GroundHazard.SpawnFriendlyPuddle(point, _shot.acid, burning, burning ? assets.fire : assets.playerAcid);
        }

        void OnEnable() => Live.Add(this);
        void OnDisable() => Live.Remove(this);

        /// <summary>Every shot in the air.</summary>
        public static readonly List<Bullet> Live = new List<Bullet>();

        /// <summary>Takes every enemy shot out of the air. Returns how many there were.</summary>
        public static int ClearEnemyShots()
        {
            int cleared = 0;
            foreach (var bullet in Live.ToArray())
            {
                if (bullet._team != Team.Enemy) continue;
                Fx.Burst(bullet.transform.position, bullet.Colour, 3, 3f, 0.12f, 0.3f);
                bullet.Despawn();
                cleared++;
            }
            return cleared;
        }

        void Despawn()
        {
            if (_pool != null) _pool.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
