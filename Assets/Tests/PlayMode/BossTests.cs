using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Milestone 5: the Brood Mother and the Ant Queen, phase by phase, in the empty arena. Time is sped up so the slow clocks don't make the tests crawl.</summary>
    public class BossTests
    {
        PlayerController _player;
        EnemySpawner _spawner;
        BiomeDefinition _biome;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _spawner = Object.FindAnyObjectByType<EnemySpawner>();
            _biome = _spawner.biome;
            _player.Input.overrideFrame = new InputFrame();
            _player.Health.SetInvulnerable(600f);

            _spawner.maxAlive = 0;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            yield return null;
            Time.timeScale = 3f;
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        T SpawnBoss<T>(BossEnemy prefab, Vector3 position) where T : BossEnemy
        {
            Assert.IsNotNull(prefab, "biome has no such boss");
            return (T)_spawner.SpawnAt(prefab, position, null);
        }

        static int Count<T>() where T : Enemy
        {
            int count = 0;
            foreach (var enemy in Enemy.Active) if (enemy is T && !enemy.Health.IsDead) count++;
            return count;
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        // ---------- Brood Mother ----------

        [UnityTest]
        public IEnumerator Brood_Mother_lays_three_larvae_that_grow_into_workers()
        {
            var mother = SpawnBoss<BroodMother>(_biome.miniBoss, new Vector3(0f, 0f, 10f));
            Assert.AreEqual(mother, BossEnemy.InPlay);

            yield return WaitUntil(() => mother.LarvaeLaid >= 3, 6f, "the first clutch");
            Assert.AreEqual(3, Count<LarvaEnemy>());
            Assert.AreEqual(0, Count<ChaserEnemy>());

            // Kill one; the other two are left alone and grow up.
            foreach (var enemy in Enemy.Active) if (enemy is LarvaEnemy) { enemy.Health.Kill(); break; }
            yield return WaitUntil(() => Count<ChaserEnemy>() >= 2, 12f, "the surviving larvae to grow into workers");
            Assert.AreEqual(2, Count<ChaserEnemy>(), "the killed larva grew up anyway");
        }

        [UnityTest]
        public IEnumerator Brood_Mother_takes_double_damage_on_the_egg_sac()
        {
            var mother = SpawnBoss<BroodMother>(_biome.miniBoss, new Vector3(0f, 0f, 10f));
            yield return null;
            int max = mother.Health.maxHealth;

            mother.Health.TakeDamage(3, -mother.transform.forward); // from the front
            Assert.AreEqual(max - 3, mother.Health.Current);
            mother.Health.TakeDamage(3, mother.transform.forward); // from behind
            Assert.AreEqual(max - 9, mother.Health.Current, "the egg sac did not take double damage");
        }

        [UnityTest]
        public IEnumerator Brood_Mother_slams_her_tail_when_the_player_is_behind_her()
        {
            var mother = SpawnBoss<BroodMother>(_biome.miniBoss, new Vector3(0f, 0f, 8f));
            mother.frozen = true; // hold her still so "behind" stays behind
            yield return null;
            mother.frozen = false;

            // In front: no slam.
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0, mother.TailSlams, "slammed with the player in front");

            _player.Teleport(mother.transform.position - mother.transform.forward * 4f + Vector3.up * 0.05f);
            _player.Health.ClearInvulnerability();
            yield return WaitUntil(() => mother.TailSlams > 0, 2f, "the tail slam");

            // The warning circle comes first; standing in it hurts only when it lands.
            Assert.AreEqual(_player.Health.maxHealth, _player.Health.Current, "tail slam hit with no warning");
            bool sawWarning = false;
            foreach (var hazard in GroundHazard.All) sawWarning |= hazard.isBlast;
            Assert.IsTrue(sawWarning, "no warning circle");
        }

        [UnityTest]
        public IEnumerator Brood_Mother_burrows_below_half_and_comes_up_under_the_player()
        {
            var mother = SpawnBoss<BroodMother>(_biome.miniBoss, new Vector3(0f, 0f, 12f));
            yield return null;
            mother.Health.TakeDamage(mother.Health.maxHealth / 2 + 1, -mother.transform.forward);
            yield return null;
            Assert.AreEqual(2, mother.Phase);

            yield return WaitUntil(() => mother.Current == BroodMother.State.Burrowed, 4f, "the burrow");
            Assert.IsFalse(mother.Targetable, "could still be targeted underground");
            Vector3 playerAt = _player.transform.position;
            int larvaeBefore = mother.LarvaeLaid;

            yield return WaitUntil(() => mother.Current != BroodMother.State.Burrowed, 3f, "her to come back up");
            Vector3 offset = mother.transform.position - playerAt;
            offset.y = 0f;
            Assert.Less(offset.magnitude, 1f, "did not come up where the player was standing");
            Assert.IsTrue(mother.Targetable);
            Assert.AreEqual(larvaeBefore + mother.ringLarvae, mother.LarvaeLaid, "no ring of larvae after surfacing");
        }

        [UnityTest]
        public IEnumerator Brood_Mother_drops_an_item_and_samples()
        {
            var mother = SpawnBoss<BroodMother>(_biome.miniBoss, new Vector3(0f, 0f, 12f));
            yield return null;
            mother.Health.Kill();
            yield return null;

            int items = 0, samples = 0;
            foreach (var pickup in Pickup.All)
            {
                if (pickup.kind == PickupKind.Item) items++;
                if (pickup.kind == PickupKind.Sample) samples += pickup.amount;
            }
            Assert.AreEqual(1, items, "the Brood Mother should drop exactly one item");
            Assert.GreaterOrEqual(samples, mother.sampleReward);
            Assert.IsNull(BossEnemy.InPlay == mother && !mother.Health.IsDead ? mother : null);
        }

        // ---------- Ant Queen ----------

        [UnityTest]
        public IEnumerator Queen_starts_with_eggs_round_the_walls_and_two_soldiers()
        {
            var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 6f));
            yield return null;
            yield return null;
            Assert.AreEqual(queen.eggCount, queen.EggsPlaced);
            Assert.AreEqual(queen.eggCount, EggEnemy.All.Count);
            Assert.AreEqual(2, Count<ChargerEnemy>(), "she should call two Soldiers");

            foreach (var egg in EggEnemy.All)
            {
                float fromQueen = (egg.transform.position - queen.transform.position).magnitude;
                Assert.Greater(fromQueen, 2.5f, "an egg cluster is under the Queen");
                Assert.Less(Mathf.Abs(egg.transform.position.x), 15f, "an egg cluster is outside the walls");
                Assert.Less(Mathf.Abs(egg.transform.position.z), 15f, "an egg cluster is outside the walls");
            }
        }

        [UnityTest]
        public IEnumerator Queen_phase_one_stays_put_and_spits_a_fan_of_three()
        {
            var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 9f));
            Vector3 throne = queen.transform.position;
            _player.Health.ClearInvulnerability();
            yield return WaitUntil(() => queen.FansFired >= 1, 4f, "the first fan");
            yield return null;

            int shots = 0;
            foreach (var bullet in Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None)) if (bullet.gameObject.activeSelf) shots++;
            Assert.AreEqual(3, shots, "a fan should be three shots");

            yield return WaitUntil(() => _player.Health.Current < _player.Health.maxHealth, 3f, "the fan to hit a player standing still");
            Vector3 moved = queen.transform.position - throne;
            moved.y = 0f;
            Assert.Less(moved.magnitude, 0.5f, "she left her throne in phase 1");
            Assert.AreEqual(1, queen.Phase);
        }

        [UnityTest]
        public IEnumerator Queen_eggs_hatch_one_at_a_time_and_can_be_shot_first()
        {
            var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 6f));
            queen.hatchInterval = 3f;
            yield return null;
            yield return null;

            // Shoot two clusters before they hatch.
            EggEnemy.All[0].Health.Kill();
            EggEnemy.All[1].Health.Kill();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, queen.EggsDestroyed);
            Assert.AreEqual(queen.eggCount - 2, queen.EggsRemaining);

            int antsBefore = Enemy.Active.Count;
            yield return WaitUntil(() => queen.EggsHatched >= 1, 5f, "an egg to hatch");
            yield return null;
            Assert.AreEqual(queen.eggCount - 3, queen.EggsRemaining);
            Assert.AreEqual(antsBefore, Enemy.Active.Count, "a hatched egg should be replaced by exactly one ant");
        }

        [UnityTest]
        public IEnumerator Queen_phase_two_charges_and_leaves_an_acid_trail()
        {
            var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 10f));
            yield return null;
            queen.Health.TakeDamage(Mathf.CeilToInt(queen.Health.maxHealth * 0.45f), Vector3.forward);
            yield return null;
            Assert.AreEqual(2, queen.Phase);

            Vector3 before = queen.transform.position;
            yield return WaitUntil(() => queen.Current == AntQueen.State.Windup, 4f, "the charge wind-up");
            yield return WaitUntil(() => queen.Charges >= 1 && queen.Current == AntQueen.State.Rest, 5f, "a charge to finish");

            Vector3 moved = queen.transform.position - before;
            moved.y = 0f;
            Assert.Greater(moved.magnitude, 5f, "the charge barely moved");

            int pools = 0;
            foreach (var hazard in GroundHazard.All) if (!hazard.isBlast && hazard.hurts == Team.Player) pools++;
            Assert.GreaterOrEqual(pools, 2, "no acid trail behind the charge");
        }

        [UnityTest]
        public IEnumerator Queen_phase_three_screams_hatches_every_egg_and_speeds_up()
        {
            var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 10f));
            yield return null;
            yield return null;
            int eggs = queen.EggsRemaining;
            Assert.Greater(eggs, 0);
            float speedBefore = queen.SpeedScale;

            queen.Health.TakeDamage(Mathf.CeilToInt(queen.Health.maxHealth * 0.8f), Vector3.forward);
            yield return WaitUntil(() => queen.Current == AntQueen.State.Scream, 1f, "the scream");
            Assert.AreEqual(3, queen.Phase);
            Assert.AreEqual(eggs, queen.EggsRemaining, "eggs hatched before the scream finished");

            yield return WaitUntil(() => queen.Current != AntQueen.State.Scream, 3f, "the scream to end");
            yield return null;
            Assert.AreEqual(0, queen.EggsRemaining, "eggs left after the scream");
            Assert.AreEqual(eggs, queen.EggsHatched);
            Assert.Greater(queen.SpeedScale, speedBefore * 1.2f, "she did not speed up");
        }

        [UnityTest]
        public IEnumerator Queen_death_gives_a_sample_pile_and_knows_if_every_egg_was_spared()
        {
            var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 10f));
            queen.jellyChance = 1f;
            yield return null;
            yield return null;

            // Killed outright, so no egg ever hatched or was shot: the secret-boss condition.
            queen.Health.Kill();
            Assert.AreEqual(queen.EggsPlaced, queen.EggsRemaining);
            Assert.AreEqual(0, queen.EggsDestroyed);
            yield return null;

            int samples = 0;
            bool jelly = false;
            foreach (var pickup in Pickup.All)
            {
                if (pickup.kind == PickupKind.Sample) samples += pickup.amount;
                if (pickup.kind == PickupKind.Item && pickup.item.type == ItemType.QueensJelly) jelly = true;
            }
            Assert.GreaterOrEqual(samples, queen.sampleReward);
            Assert.IsTrue(jelly, "no Queen's Jelly at 100% chance");
        }

        // ---------- can a player actually win? ----------

        // A plain but sensible player: shoots whatever is closest, backs away from anything near, circles the boss
        // otherwise. No dodging and no items, so a real player should do better.
        IEnumerator BotFight(BossEnemy boss, float timeLimit)
        {
            _player.Health.ClearInvulnerability();
            _player.Health.SetMaxHealth(999, true);
            int hits = 0;
            System.Action<int, Vector3> count = (amount, direction) => hits += amount;
            _player.Health.Damaged += count;

            float started = Time.time;
            while (!boss.Health.IsDead && Time.time - started < timeLimit)
            {
                Vector3 position = _player.transform.position;

                // Nearest thing that can be shot: deal with ants in your face before the boss.
                Enemy nearest = null;
                float nearestDistance = float.MaxValue;
                foreach (var enemy in Enemy.Active)
                {
                    if (enemy.Health.IsDead || !enemy.Targetable || enemy is EggEnemy) continue;
                    float d = (enemy.transform.position - position).magnitude;
                    if (enemy == boss) d -= 3f; // measure to her body, not her middle
                    if (d < nearestDistance) { nearestDistance = d; nearest = enemy; }
                }
                Enemy aimAt = nearest != null && nearestDistance < 8f ? nearest : boss;

                Vector3 toBoss = boss.transform.position - position;
                toBoss.y = 0f;
                Vector3 move = Vector3.Cross(Vector3.up, toBoss.normalized);
                if (toBoss.magnitude > 10f) move += toBoss.normalized * 0.6f;
                if (nearest != null && nearestDistance < 5f)
                {
                    Vector3 away = position - nearest.transform.position;
                    away.y = 0f;
                    move += away.normalized * 1.5f;
                }

                // Stay off the walls.
                Vector3 centre = -position;
                centre.y = 0f;
                if (centre.magnitude > 10f) move += centre.normalized * 2f;

                Vector3 aim = aimAt.transform.position - position;
                aim.y = 0f;
                _player.Input.overrideFrame = new InputFrame { move = move.normalized, aim = aim.normalized, fire = true };
                yield return null;
            }

            _player.Health.Damaged -= count;
            _player.Input.overrideFrame = new InputFrame();
            LastBotHits = hits;
            Debug.Log($"[Shrunk] Bot vs {boss.displayName}: {(boss.Health.IsDead ? "won" : "ran out of time")} after {Time.time - started:F0}s of game time, took {hits} hits, boss health left {boss.Health.Current}/{boss.Health.maxHealth}");
            Assert.IsTrue(boss.Health.IsDead, $"a circling, shooting player could not kill {boss.displayName} in {timeLimit}s");
        }

        int LastBotHits;

        // Tuning aid, run by name: how hard the Queen hits the plain bot at different settings.
        [UnityTest, Explicit]
        public IEnumerator Measure_queen_difficulty()
        {
            Time.timeScale = 5f;
            foreach (float lead in new[] { 0f, 0.4f, 0.85f })
            {
                foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
                GroundHazard.ClearAll();
                yield return null;
                _player.Teleport(new Vector3(0f, 0.05f, 0f));

                var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 8f));
                queen.fanLead = lead;
                int poolHits = 0, otherHits = 0;
                System.Action<int, Vector3> count = (amount, direction) => { if (direction == Vector3.zero) poolHits++; else otherHits++; };
                _player.Health.Damaged += count;
                Debug.Log($"[Shrunk] --- fan lead {lead} ---");
                yield return BotFight(queen, 300f);
                _player.Health.Damaged -= count;
                Debug.Log($"[Shrunk] lead {lead}: {poolHits} hits from acid pools, {otherHits} from shots, charges and ants");
            }
        }

        [UnityTest]
        public IEnumerator Brood_Mother_scuttles_after_a_player_who_keeps_away()
        {
            var mother = SpawnBoss<BroodMother>(_biome.miniBoss, new Vector3(0f, 0f, 9f));
            Assert.AreEqual(mother.crawlSpeed, mother.SpeedAt(3f), 0.001f, "up close she should crawl");
            Assert.AreEqual(mother.scuttleSpeed, mother.SpeedAt(20f), 0.001f, "far off she should scuttle");
            Assert.Greater(mother.SpeedAt(10f), mother.crawlSpeed);

            _player.Health.SetInvulnerable(600f);
            _player.Teleport(new Vector3(0f, 0.05f, -9f));
            yield return new WaitForSeconds(0.2f);
            float before = (mother.transform.position - _player.transform.position).magnitude;
            yield return new WaitForSeconds(1.5f);
            float closed = before - (mother.transform.position - _player.transform.position).magnitude;
            Assert.Greater(closed, mother.crawlSpeed * 1.5f * 1.4f, "she closed no faster than a crawl");
        }

        [UnityTest]
        public IEnumerator Brood_Mother_can_be_beaten_by_circling_and_shooting()
        {
            Time.timeScale = 5f;
            var mother = SpawnBoss<BroodMother>(_biome.miniBoss, new Vector3(0f, 0f, 10f));
            yield return BotFight(mother, 180f);
        }

        [UnityTest]
        public IEnumerator Ant_Queen_can_be_beaten_by_circling_and_shooting()
        {
            Time.timeScale = 5f;
            var queen = SpawnBoss<AntQueen>(_biome.boss, new Vector3(0f, 0f, 8f));
            yield return BotFight(queen, 300f);
        }
    }
}
