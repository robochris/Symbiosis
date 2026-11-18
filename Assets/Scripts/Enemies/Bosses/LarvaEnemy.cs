using UnityEngine;

namespace Shrunk
{
    /// <summary>A larva laid by the Brood Mother. Harmless, but if it isn't killed in time it grows into a full ant.</summary>
    public class LarvaEnemy : Enemy
    {
        [Tooltip("What it becomes.")]
        public Enemy growsInto;
        [Tooltip("Seconds until it grows up.")]
        public float growTime = 10f;

        public float Age { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            Age = 0f;
        }

        protected override void Think()
        {
            Age += Time.deltaTime;

            // Swells as it gets close to hatching, so the player can see which ones are urgent.
            SetVisualScale(Mathf.Lerp(0.7f, 1.4f, Age / growTime) + 0.06f * Mathf.Sin(Time.time * 9f));
            Move(Vector3.zero);

            if (Age >= growTime)
            {
                if (growsInto != null) RequestMinion(growsInto, transform.position, false);
                Vanish();
            }
        }
    }
}
