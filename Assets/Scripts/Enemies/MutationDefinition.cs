using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    public enum MutationType
    {
        Fast,
        Armored,
        Regenerates,
        ExplodesOnDeath,
        AcidBlood,
        SwarmCall,
        HardenedShell,
        PheromoneTrail,
        /// <summary>Birthday Cake: leaves a sticky patch on death that slows the player.</summary>
        Sticky,
        /// <summary>Birthday Cake: below half health it moves and attacks much faster.</summary>
        SugarRush,
        /// <summary>Under the Sofa: every few seconds it discharges, shocking anything standing close.</summary>
        Static,
        /// <summary>The Library: it has a sequel. When it is put down, two of the world's smallest turn up in its place.</summary>
        Sequel
    }

    /// <summary>A mutation as data: what it is, what it costs from the point budget, and the colour players read it by.</summary>
    [CreateAssetMenu(menuName = "Shrunk/Mutation", fileName = "NewMutation")]
    public class MutationDefinition : ScriptableObject
    {
        public MutationType type;
        public string displayName = "Mutation";
        [Range(1, 3)] public int cost = 1;
        [TextArea] public string effect;
        [Tooltip("The colour of its tell: body tint, the badge above the enemy, and any pool, blast or trail it leaves.")]
        public Color color = Color.white;
        [Tooltip("Unlit material in that colour, used for the badge and effects.")]
        public Material effectMaterial;
    }

    /// <summary>The point budget: how many mutation points an enemy may roll, and which mutations it gets.</summary>
    public static class MutationBudget
    {
        /// <summary>
        /// Depth is how many biomes into the run you are. Depth 1 allows up to 1 point, depth 3 up to 3, depth 5 and
        /// beyond up to 5. The harder half of a biome (floors 6-9) allows one more.
        /// </summary>
        public static int MaxPoints(int depth, int floor)
        {
            int points = Mathf.Clamp(depth, 1, 5);
            if (floor >= 6) points++;
            return points;
        }

        /// <summary>Rolls a point total from 0 to the maximum, then spends it on random mutations with no repeats.</summary>
        public static List<MutationDefinition> Roll(System.Random rng, IList<MutationDefinition> pool, int maxPoints)
        {
            var chosen = new List<MutationDefinition>();
            if (pool == null || pool.Count == 0 || maxPoints <= 0) return chosen;

            Spend(rng, pool, rng.Next(0, maxPoints + 1), chosen);
            return chosen;
        }

        /// <summary>Spends exactly this many points (or as near as the pool allows), with no dice on the total. For elites.</summary>
        public static List<MutationDefinition> RollFull(System.Random rng, IList<MutationDefinition> pool, int points)
        {
            var chosen = new List<MutationDefinition>();
            if (pool != null && pool.Count > 0) Spend(rng, pool, points, chosen);
            return chosen;
        }

        /// <summary>
        /// The chance that a fight room has an elite. None on the first half of the first biome; one room in four on
        /// floors 6-9; and every biome deeper adds a fifth, up to nine rooms in ten.
        /// </summary>
        public static float EliteChance(int depth, int floor)
        {
            float chance = (floor >= 6 ? 0.25f : 0f) + 0.2f * (Mathf.Max(1, depth) - 1);
            return Mathf.Clamp(chance, 0f, 0.9f);
        }

        static void Spend(System.Random rng, IList<MutationDefinition> pool, int points, List<MutationDefinition> chosen)
        {
            var affordable = new List<MutationDefinition>();
            while (points > 0)
            {
                affordable.Clear();
                foreach (var mutation in pool)
                {
                    if (mutation != null && mutation.cost <= points && !chosen.Contains(mutation)) affordable.Add(mutation);
                }
                if (affordable.Count == 0) break;

                var pick = affordable[rng.Next(affordable.Count)];
                chosen.Add(pick);
                points -= pick.cost;
            }
        }
    }
}
