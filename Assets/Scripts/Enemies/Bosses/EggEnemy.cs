using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// An egg cluster in the Ant Queen's chamber. It does nothing by itself; the Queen hatches them into ants.
    /// The player can shoot them first to make the fight easier.
    /// </summary>
    public class EggEnemy : Enemy
    {
        public static readonly List<EggEnemy> All = new List<EggEnemy>();

        protected override void OnEnable()
        {
            base.OnEnable();
            All.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            All.Remove(this);
        }

        protected override void Think()
        {
            SetVisualScale(1f + 0.05f * Mathf.Sin(Time.time * 3f + transform.position.x));
            Move(Vector3.zero);
        }

        /// <summary>Turns into a random ant from the biome (mutated, deeper in a run) and disappears.</summary>
        public void Hatch()
        {
            if (Health.IsDead) return;
            RequestMinion(null, transform.position, true);
            Vanish();
        }
    }
}
