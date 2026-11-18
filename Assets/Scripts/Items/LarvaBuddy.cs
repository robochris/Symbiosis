using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Larva Buddy item: a small larva that trails the player and bites nearby enemies.
    /// The player's shot items power up its bite too (fire sets its target alight, acid leaves a puddle).
    /// </summary>
    public class LarvaBuddy : MonoBehaviour
    {
        public PlayerItems owner;
        public float followSpeed = 7f;
        public float chaseSpeed = 9f;
        [Tooltip("It goes for enemies within this distance of the player.")]
        public float guardRadius = 7f;
        public float biteReach = 1.1f;
        public float biteInterval = 1f;

        public int Bites { get; private set; }

        float _nextBite;

        void Update()
        {
            if (owner == null) { Destroy(gameObject); return; }

            Vector3 home = owner.transform.position - owner.transform.forward * 1.3f + owner.transform.right * 0.9f;
            Enemy target = NearestEnemy(owner.transform.position);

            Vector3 goal = target != null ? target.transform.position : home;
            float speed = target != null ? chaseSpeed : followSpeed;
            Vector3 to = goal - transform.position;
            to.y = 0f;

            if (to.magnitude > (target != null ? biteReach * 0.8f : 0.2f))
            {
                transform.position += to.normalized * Mathf.Min(speed * Time.deltaTime, to.magnitude);
                transform.rotation = Quaternion.LookRotation(to.normalized);
            }
            transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);

            if (target != null && to.magnitude <= biteReach && Time.time >= _nextBite)
            {
                _nextBite = Time.time + biteInterval;
                Bite(target);
            }
        }

        void Bite(Enemy target)
        {
            int level = owner.Count(ItemType.LarvaBuddy);
            PlayerStats stats = owner.Stats;
            Bites++;

            target.Health.TakeDamage(Mathf.Max(1, level) * stats.damageMultiplier, transform.forward);
            if (stats.burn > 0) target.Ignite(stats.burn);
            if (stats.acid > 0 && GameAssets.Instance != null)
            {
                bool burning = stats.burn > 0;
                GroundHazard.SpawnFriendlyPuddle(target.transform.position, stats.acid, burning,
                    burning ? GameAssets.Instance.fire : GameAssets.Instance.playerAcid);
            }
        }

        Enemy NearestEnemy(Vector3 around)
        {
            Enemy best = null;
            float bestDistance = guardRadius * guardRadius;
            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead || !enemy.Targetable) continue;
                float distance = (enemy.transform.position - around).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }
            return best;
        }
    }
}
