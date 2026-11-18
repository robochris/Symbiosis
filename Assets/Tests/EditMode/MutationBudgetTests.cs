using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Shrunk.Tests
{
    /// <summary>The point budget that decides how mutated an enemy can be.</summary>
    public class MutationBudgetTests
    {
        static List<MutationDefinition> Pool()
        {
            var costs = new Dictionary<MutationType, int>
            {
                { MutationType.Fast, 1 }, { MutationType.Armored, 2 }, { MutationType.Regenerates, 2 },
                { MutationType.ExplodesOnDeath, 2 }, { MutationType.AcidBlood, 1 }, { MutationType.SwarmCall, 3 },
                { MutationType.HardenedShell, 2 }, { MutationType.PheromoneTrail, 2 },
            };
            var pool = new List<MutationDefinition>();
            foreach (var pair in costs)
            {
                var mutation = ScriptableObject.CreateInstance<MutationDefinition>();
                mutation.type = pair.Key;
                mutation.cost = pair.Value;
                pool.Add(mutation);
            }
            return pool;
        }

        [Test]
        public void Budget_follows_the_design_doc()
        {
            Assert.AreEqual(1, MutationBudget.MaxPoints(1, 1));
            Assert.AreEqual(3, MutationBudget.MaxPoints(3, 1));
            Assert.AreEqual(5, MutationBudget.MaxPoints(5, 1));
            Assert.AreEqual(5, MutationBudget.MaxPoints(9, 1), "depth past 5 keeps the same base budget");
            Assert.AreEqual(2, MutationBudget.MaxPoints(1, 6), "the harder half of a biome allows one more point");
        }

        [Test]
        public void Rolls_never_overspend_or_repeat()
        {
            var pool = Pool();
            var rng = new System.Random(42);

            for (int max = 0; max <= 6; max++)
            {
                bool sawEmpty = false, sawFull = false;
                for (int i = 0; i < 2000; i++)
                {
                    var roll = MutationBudget.Roll(rng, pool, max);
                    int cost = 0;
                    var seen = new HashSet<MutationType>();
                    foreach (var mutation in roll)
                    {
                        cost += mutation.cost;
                        Assert.IsTrue(seen.Add(mutation.type), "same mutation twice");
                    }
                    Assert.LessOrEqual(cost, max);
                    sawEmpty |= roll.Count == 0;
                    sawFull |= cost == max;
                }
                Assert.IsTrue(sawEmpty, $"budget {max}: every enemy was mutated");
                Assert.IsTrue(sawFull, $"budget {max}: the full budget was never spent");
            }
        }

        [Test]
        public void A_one_point_budget_only_buys_one_point_mutations()
        {
            var pool = Pool();
            var rng = new System.Random(7);
            var seen = new HashSet<MutationType>();
            for (int i = 0; i < 500; i++)
            {
                foreach (var mutation in MutationBudget.Roll(rng, pool, 1)) seen.Add(mutation.type);
            }
            CollectionAssert.AreEquivalent(new[] { MutationType.Fast, MutationType.AcidBlood }, seen);
        }
    }
}
