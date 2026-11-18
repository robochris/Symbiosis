using UnityEngine;

namespace Shrunk
{
    /// <summary>A pooled shot. Moves in a straight line and sweeps a sphere each frame so fast shots can't skip through thin targets.</summary>
    public class Bullet : MonoBehaviour
    {
        public float radius = 0.15f;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        Vector3 _direction;
        float _speed;
        float _range;
        float _travelled;
        int _damage;
        Team _team;
        Pool<Bullet> _pool;

        public void Launch(Vector3 position, Vector3 direction, float speed, int damage, float range, Team team, Pool<Bullet> pool)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            _direction = direction;
            _speed = speed;
            _damage = damage;
            _range = range;
            _team = team;
            _pool = pool;
            _travelled = 0f;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, _direction, Hits, step, ~0, QueryTriggerInteraction.Ignore);

            bool hitSomething = false;
            float nearest = float.MaxValue;
            Health nearestHealth = null;

            for (int i = 0; i < count; i++)
            {
                var health = Hits[i].collider.GetComponentInParent<Health>();
                if (health != null && (health.team == _team || health.IsDead)) continue; // pass through friends and corpses
                if (Hits[i].distance < nearest)
                {
                    nearest = Hits[i].distance;
                    nearestHealth = health;
                    hitSomething = true;
                }
            }

            if (hitSomething)
            {
                if (nearestHealth != null) nearestHealth.TakeDamage(_damage, _direction);
                Despawn();
                return;
            }

            transform.position += _direction * step;
            _travelled += step;
            if (_travelled >= _range) Despawn();
        }

        void Despawn()
        {
            if (_pool != null) _pool.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
