using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Milestone 3: each ant behaviour and each mutation, tried one at a time in the empty arena.</summary>
    public class EnemyTests
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
            Assert.IsNotNull(_player);
            Assert.IsNotNull(_spawner);
            _biome = _spawner.biome;
            Assert.IsNotNull(_biome, "arena spawner has no biome");
            _player.Input.overrideFrame = new InputFrame();

            // Empty the arena and stop it restocking; the tests bring their own ants.
            _spawner.maxAlive = 0;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            yield return null;
            Assert.AreEqual(0, Enemy.Active.Count);
        }

        Enemy Spawn(string prefabName, Vector3 position, params MutationType[] mutations)
        {
            Enemy prefab = _biome.EnemyNamed(prefabName);
            Assert.IsNotNull(prefab, "no enemy called " + prefabName);
            var list = new List<MutationDefinition>();
            foreach (var type in mutations)
            {
                var mutation = _biome.Mutation(type);
                Assert.IsNotNull(mutation, "no mutation " + type);
                list.Add(mutation);
            }
            return _spawner.SpawnAt(prefab, position, list);
        }

        [UnityTest]
        public IEnumerator Enemies_only_leave_food_for_a_player_who_is_hurt()
        {
            Loot.CrumbChanceOverride = 1f;
            try
            {
                Enemy first = Spawn("WorkerAnt", new Vector3(0f, 0f, 14f));
                first.frozen = true;
                yield return null;
                first.Health.Kill();
                yield return null;
                int crumbs = 0;
                foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Health) crumbs++;
                Assert.AreEqual(0, crumbs, "food for somebody who did not need it");

                _player.Health.TakeDamage(1, Vector3.forward);
                Enemy second = Spawn("WorkerAnt", new Vector3(0f, 0f, 14f));
                second.frozen = true;
                yield return null;
                second.Health.Kill();
                yield return null;
                Pickup crumb = null;
                foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Health) crumb = pickup;
                Assert.IsNotNull(crumb, "nothing to eat for a hurt player");

                // Walking onto it puts the health back.
                int hurt = _player.Health.Current;
                Vector3 spot = crumb.transform.position;
                spot.y = 0.05f;
                _player.Teleport(spot);
                yield return WaitUntil(() => _player.Health.Current == hurt + 1, 1f, "the crumb to be eaten");
            }
            finally
            {
                Loot.CrumbChanceOverride = null;
            }
        }

        [UnityTest]
        public IEnumerator Nothing_can_climb_onto_cover()
        {
            // A ramp a good deal gentler than anything could not walk up by default (20 degrees), right behind an ant
            // that wants to back away from the player. It leads up to well above where shots fly.
            var boulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boulder.transform.SetPositionAndRotation(new Vector3(0f, 1.25f, 11.6f), Quaternion.Euler(-20f, 0f, 0f));
            boulder.transform.localScale = new Vector3(5f, 0.2f, 8f);
            Physics.SyncTransforms();
            var lobber = (LobberEnemy)Spawn("BombardierAnt", new Vector3(0f, 0f, 7.5f));
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(new Vector3(0f, 0.05f, 4f));

            float highest = 0f;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                highest = Mathf.Max(highest, lobber.transform.position.y);
                yield return null;
            }
            Object.Destroy(boulder);
            Assert.Less(highest, 0.4f, "an enemy walked up onto cover, out of reach of every shot");
        }

        [UnityTest]
        public IEnumerator A_mimic_is_coins_until_it_is_walked_up_to_or_shot_or_left_alone()
        {
            // With company in the room and the player far off: it sits there, and nothing aims at it.
            Enemy company = Spawn("WorkerAnt", new Vector3(10f, 0f, 14f));
            company.frozen = true;
            var mimic = (MimicEnemy)Spawn("Mimic", new Vector3(0f, 0f, 12f));
            _player.Health.SetInvulnerable(600f);
            Vector3 sat = mimic.transform.position;
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(mimic.Asleep);
            Assert.IsFalse(mimic.Targetable, "aim assist would give it away");
            Assert.IsTrue(mimic.disguise.activeSelf && !mimic.body.activeSelf);
            Assert.Less((mimic.transform.position - sat).magnitude, 0.3f, "the coins moved");

            // Walking up to it: it shakes, gets up, and comes for the player.
            _player.Teleport(mimic.transform.position + new Vector3(0f, 0.05f, -2.5f));
            yield return WaitUntil(() => mimic.Waking, 1f, "the coins to shake");
            Assert.IsTrue(mimic.Asleep, "it got up with no warning");
            yield return WaitUntil(() => !mimic.Asleep, 2f, "it to get up");
            Assert.IsTrue(mimic.Targetable);
            Assert.IsTrue(mimic.body.activeSelf && !mimic.disguise.activeSelf);
            mimic.Health.Kill();
            yield return new WaitForSeconds(0.4f);

            // Shooting the heap finds it out from a distance.
            _player.Teleport(new Vector3(0f, 0.05f, 0f));
            var second = (MimicEnemy)Spawn("Mimic", new Vector3(0f, 0f, 12f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(second.Asleep);
            second.Health.TakeDamage(1, Vector3.forward);
            yield return WaitUntil(() => !second.Asleep, 2f, "a shot mimic to get up");
            second.Health.Kill();
            company.Health.Kill();
            yield return new WaitForSeconds(0.4f);

            // Left as the only thing in the room, it gives itself away.
            var last = (MimicEnemy)Spawn("Mimic", new Vector3(0f, 0f, 13f));
            yield return WaitUntil(() => !last.Asleep, last.aloneTime + last.wakeTime + 1.5f, "the last mimic to give itself away");
        }

        [UnityTest]
        public IEnumerator Lobber_throws_at_where_the_player_is_going_and_keeps_its_distance()
        {
            var lobber = (LobberEnemy)Spawn("BombardierAnt", new Vector3(0f, 0f, 11f));
            _player.Health.SetInvulnerable(600f);

            // Standing still: it lands on the player, after a warning disc.
            yield return WaitUntil(() => lobber.Lobs == 1, 4f, "the first throw");
            Assert.Less((lobber.LastTarget - _player.transform.position).magnitude, 1f, "a throw at a still player should land on them");
            bool disc = false;
            foreach (var hazard in GroundHazard.All) disc |= hazard.isBlast && (hazard.transform.position - lobber.LastTarget).magnitude < 0.5f;
            Assert.IsTrue(disc, "a throw with no warning disc");
            Assert.IsNotNull(Object.FindAnyObjectByType<LobVisual>(), "nothing is seen in the air");

            // Running sideways: it lands ahead of them. (Set off late, so the run is not ended by the arena wall first.)
            yield return new WaitForSeconds(lobber.lobInterval - 1.2f);
            _player.Input.overrideFrame = new InputFrame { move = Vector3.right };
            yield return WaitUntil(() => lobber.Lobs == 2, 6f, "the second throw");
            Assert.Greater(lobber.LastTarget.x - _player.transform.position.x, 1.5f, "it did not aim ahead of a running player");
            _player.Input.overrideFrame = new InputFrame();

            // Walk up to it and it backs off.
            _player.Teleport(lobber.transform.position + new Vector3(0f, 0.05f, -3f));
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(DistanceToPlayer(lobber), 4.5f, "it stood its ground with the player in its face");
        }

        [UnityTest]
        public IEnumerator Support_speeds_up_its_neighbours_and_only_bites_when_alone()
        {
            var herald = (SupportEnemy)Spawn("HeraldAnt", new Vector3(0f, 0f, 12f));
            Enemy first = Spawn("WorkerAnt", new Vector3(-2f, 0f, 13f));
            Enemy second = Spawn("WorkerAnt", new Vector3(2f, 0f, 13f));
            Enemy far = Spawn("WorkerAnt", new Vector3(0f, 0f, -30f));
            first.frozen = second.frozen = far.frozen = true;
            int health = _player.Health.Current;

            yield return WaitUntil(() => herald.Pulses > 0, 5f, "a pulse");
            Assert.AreEqual(2, herald.LastReached, "the pulse should reach the two beside it and not the one across the arena");
            Assert.IsTrue(first.IsBuffed && second.IsBuffed);
            Assert.IsFalse(far.IsBuffed);
            Assert.IsFalse(herald.Alone);
            Assert.AreEqual(health, _player.Health.Current, "a support with company should hurt nobody");

            // The last one standing comes for the player.
            first.Health.Kill();
            second.Health.Kill();
            far.Health.Kill();
            yield return WaitUntil(() => herald.Alone, 2f, "it to notice it is alone");
            yield return WaitUntil(() => _player.Health.Current < health, 10f, "a panicked support to bite");
        }

        float DistanceToPlayer(Enemy enemy)
        {
            Vector3 to = enemy.transform.position - _player.transform.position;
            to.y = 0f;
            return to.magnitude;
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        bool PlayerHurt => _player.Health.Current < _player.Health.maxHealth;

        // ---------- arrows for what the camera cannot see ----------

        [UnityTest]
        public IEnumerator Unseen_enemies_nearby_get_an_arrow_and_seen_ones_do_not()
        {
            GameSettings.Reset();
            var arrows = EnemyArrows.Instance;
            Assert.IsNotNull(arrows, "the arena has no enemy arrows");
            _player.Health.SetInvulnerable(600f);
            _player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            CameraFollow.Instance.Snap();
            var view = Camera.main;
            Vector3 ahead = view.transform.forward, right = view.transform.right;
            ahead.y = right.y = 0f;
            Vector3 from = _player.transform.position;

            // In front and in view: nothing.
            var seen = Spawn("WorkerAnt", from + ahead.normalized * 7f);
            seen.frozen = true;
            yield return null;
            yield return null;
            Assert.AreEqual(0, arrows.Showing, "an enemy in plain view got an arrow");

            // Close behind: an arrow, pointing down the screen.
            var behind = Spawn("WorkerAnt", from - ahead.normalized * 6f);
            behind.frozen = true;
            yield return null;
            yield return null;
            Assert.AreEqual(1, arrows.Showing);
            Assert.IsTrue(arrows.Matters(behind, _player));

            // Far behind: too far to matter, until there is a Compound Eye to see it with.
            var far = Spawn("WorkerAnt", from - ahead.normalized * 16f + right.normalized * 3f);
            far.frozen = true;
            yield return null;
            yield return null;
            Assert.AreEqual(1, arrows.Showing, "a far-off enemy got an arrow");
            Assert.IsFalse(arrows.Matters(far, _player));
            _player.Items.Add(GameAssets.Instance.items.Get(ItemType.CompoundEye));
            yield return null;
            Assert.AreEqual(2, arrows.Showing, "the Compound Eye should show the far one too");

            // The setting turns them all off.
            GameSettings.EnemyArrows = false;
            yield return null;
            Assert.AreEqual(0, arrows.Showing);
            GameSettings.Reset();
        }

        // ---------- behaviours ----------

        [UnityTest]
        public IEnumerator Charger_winds_up_then_charges_into_a_still_player()
        {
            var soldier = (ChargerEnemy)Spawn("SoldierAnt", new Vector3(0f, 0f, 8f));
            yield return WaitUntil(() => soldier.Current == ChargerEnemy.State.Windup, 2f, "the wind-up");
            Assert.IsFalse(PlayerHurt, "hurt before the charge started");
            Vector3 before = soldier.transform.position;
            yield return WaitUntil(() => soldier.Current == ChargerEnemy.State.Charge, 2f, "the charge");
            yield return WaitUntil(() => PlayerHurt, 2f, "the charge to hit");
            Assert.Greater((soldier.transform.position - before).magnitude, 4f, "the charge barely moved");
        }

        [UnityTest]
        public IEnumerator Charger_shows_where_it_will_charge_before_it_goes()
        {
            var soldier = (ChargerEnemy)Spawn("SoldierAnt", new Vector3(0f, 0f, 7f));
            _player.Health.SetInvulnerable(600f);
            Assert.IsFalse(soldier.ChargeLineShowing);

            for (float t = 0f; t < 3f && soldier.Current != ChargerEnemy.State.Windup; t += Time.deltaTime) yield return null;
            Assert.AreEqual(ChargerEnemy.State.Windup, soldier.Current, "the soldier never wound up");
            yield return null;
            Assert.IsTrue(soldier.ChargeLineShowing, "no warning strip during the wind-up");
            float early = soldier.ChargeLineLength;

            float longest = early;
            while (soldier.Current == ChargerEnemy.State.Windup)
            {
                longest = Mathf.Max(longest, soldier.ChargeLineLength);
                yield return null;
            }
            Assert.Greater(longest, early + 3f, "the strip should grow as the charge gets closer");
            Assert.LessOrEqual(longest, soldier.chargeSpeed * soldier.chargeTime + 0.01f);
            Assert.IsFalse(soldier.ChargeLineShowing, "the strip is still there after the charge began");
        }

        [UnityTest]
        public IEnumerator Charger_misses_a_player_who_steps_aside()
        {
            var soldier = (ChargerEnemy)Spawn("SoldierAnt", new Vector3(0f, 0f, 8f));
            yield return WaitUntil(() => soldier.Current == ChargerEnemy.State.Charge, 4f, "the charge");
            _player.Input.overrideFrame = new InputFrame { move = Vector3.right };
            yield return WaitUntil(() => soldier.Current == ChargerEnemy.State.Recover, 3f, "the charge to end");
            Assert.IsFalse(PlayerHurt, "the charge turned to follow the player");
        }

        [UnityTest]
        public IEnumerator Shooter_backs_off_and_spits_at_the_player()
        {
            var spitter = (ShooterEnemy)Spawn("SpitterAnt", new Vector3(0f, 0f, 3f));
            yield return new WaitForSeconds(2f);
            Assert.Greater(DistanceToPlayer(spitter), 5f, "spitter stayed in the player's face");
            yield return WaitUntil(() => spitter.ShotsFired > 0, 4f, "a shot");
            yield return WaitUntil(() => PlayerHurt, 4f, "the spit to hit a still player");
        }

        [UnityTest]
        public IEnumerator Flyer_circles_then_dives_to_bite()
        {
            var flyer = (FlyerEnemy)Spawn("WingedAnt", new Vector3(0f, 0f, 9f));
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(flyer.visual.localPosition.y, 0.7f, "flyer is not off the ground");
            yield return WaitUntil(() => flyer.Current == FlyerEnemy.State.Dive, 8f, "a dive");
            yield return WaitUntil(() => PlayerHurt, 10f, "a dive to land a bite");
        }

        // ---------- mutations ----------

        [UnityTest]
        public IEnumerator Fast_ants_cover_more_ground()
        {
            var normal = Spawn("WorkerAnt", new Vector3(-6f, 0f, 14f));
            var fast = Spawn("WorkerAnt", new Vector3(6f, 0f, 14f), MutationType.Fast);
            float normalStart = DistanceToPlayer(normal), fastStart = DistanceToPlayer(fast);
            yield return new WaitForSeconds(1.5f);
            float normalMoved = normalStart - DistanceToPlayer(normal), fastMoved = fastStart - DistanceToPlayer(fast);
            Assert.Greater(fastMoved, normalMoved * 1.3f, $"fast moved {fastMoved:F1}, normal moved {normalMoved:F1}");
        }

        [UnityTest]
        public IEnumerator Armored_ants_take_half_damage()
        {
            var ant = Spawn("WorkerAnt", new Vector3(0f, 0f, 12f), MutationType.Armored);
            int max = ant.Health.maxHealth;
            for (int i = 0; i < 4; i++) ant.Health.TakeDamage(1, ant.transform.forward);
            Assert.AreEqual(max - 2, ant.Health.Current);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hardened_shell_blocks_the_front_but_not_the_back()
        {
            var ant = Spawn("WorkerAnt", new Vector3(0f, 0f, 12f), MutationType.HardenedShell);
            int max = ant.Health.maxHealth;

            // A shot from the player travels the opposite way to the ant's facing: it hits the shell.
            ant.Health.TakeDamage(1, -ant.transform.forward);
            Assert.AreEqual(max, ant.Health.Current, "shot from the front got through");

            ant.Health.TakeDamage(1, ant.transform.forward);
            Assert.AreEqual(max - 1, ant.Health.Current, "shot from behind was blocked");
            yield return null;

            // Enough shots to the face and the shell shatters, so nothing that keeps facing you is untouchable.
            int strength = ant.Mutations.shellStrength;
            Assert.AreEqual(strength - 1, ant.Mutations.ShellLeft);
            Transform plate = null;
            foreach (Transform child in ant.transform) if (child.name == "Shell plate") plate = child;
            Assert.IsNotNull(plate);
            for (int i = 0; i < strength - 1; i++)
            {
                Assert.IsTrue(plate.gameObject.activeSelf, "the shell broke early");
                ant.Health.TakeDamage(1, -ant.transform.forward);
            }
            Assert.AreEqual(max - 1, ant.Health.Current, "the shell let something through before it broke");
            Assert.AreEqual(0, ant.Mutations.ShellLeft);
            Assert.IsFalse(plate.gameObject.activeSelf, "the broken shell is still showing");
            ant.Health.TakeDamage(1, -ant.transform.forward);
            Assert.AreEqual(max - 2, ant.Health.Current, "a shattered shell still blocks");
        }

        [UnityTest]
        public IEnumerator Regenerating_ants_heal_when_left_alone()
        {
            var ant = Spawn("SoldierAnt", new Vector3(0f, 0f, 14f), MutationType.Regenerates);
            int max = ant.Health.maxHealth;
            ant.Health.TakeDamage(3, ant.transform.forward);
            Assert.AreEqual(max - 3, ant.Health.Current);

            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(max - 3, ant.Health.Current, "healed before 2 seconds without a hit");
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(max, ant.Health.Current, "did not heal back up");
        }

        [UnityTest]
        public IEnumerator Exploding_ants_burst_after_a_fuse()
        {
            var ant = Spawn("WorkerAnt", _player.transform.position + new Vector3(1.5f, 0f, 0f), MutationType.ExplodesOnDeath);
            ant.Health.Kill();
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(PlayerHurt, "blast went off with no fuse");
            yield return WaitUntil(() => PlayerHurt, 1f, "the blast");
        }

        [UnityTest]
        public IEnumerator Exploding_ants_can_be_outrun()
        {
            var ant = Spawn("WorkerAnt", _player.transform.position + new Vector3(1.5f, 0f, 0f), MutationType.ExplodesOnDeath);
            ant.Health.Kill();
            _player.Input.overrideFrame = new InputFrame { move = Vector3.left };
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(PlayerHurt, "walking away from the blast did not save the player");
            Assert.AreEqual(0, GroundHazard.All.Count, "the blast never cleaned itself up");
        }

        [UnityTest]
        public IEnumerator Acid_blood_leaves_a_pool_that_hurts()
        {
            var ant = Spawn("WorkerAnt", _player.transform.position + new Vector3(0.8f, 0f, 0f), MutationType.AcidBlood);
            ant.Health.Kill();
            yield return WaitUntil(() => PlayerHurt, 1f, "the acid to burn a player standing in it");
            Assert.AreEqual(1, GroundHazard.All.Count);
            yield return new WaitForSeconds(5.5f);
            Assert.AreEqual(0, GroundHazard.All.Count, "the pool never dried up");
        }

        [UnityTest]
        public IEnumerator Swarm_call_brings_three_workers_once()
        {
            var ant = Spawn("SoldierAnt", new Vector3(0f, 0f, 14f), MutationType.SwarmCall);
            ant.Health.TakeDamage(4, ant.transform.forward);
            Assert.AreEqual(1, Enemy.Active.Count, "called for help above half health");

            ant.Health.TakeDamage(1, ant.transform.forward);
            yield return null;
            Assert.AreEqual(4, Enemy.Active.Count, "half health did not bring three workers");

            ant.Health.TakeDamage(1, ant.transform.forward);
            yield return null;
            Assert.AreEqual(4, Enemy.Active.Count, "called for help twice");
        }

        [UnityTest]
        public IEnumerator Pheromone_trail_speeds_up_the_ant_behind()
        {
            var leader = Spawn("WorkerAnt", new Vector3(0f, 0f, 9f), MutationType.PheromoneTrail);
            var follower = Spawn("WorkerAnt", new Vector3(0f, 0f, 13f));
            _player.Health.SetInvulnerable(60f);

            bool followerBuffed = false, leaderBuffed = false;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                followerBuffed |= follower.IsBuffed;
                leaderBuffed |= leader.IsBuffed;
                yield return null;
            }
            Assert.IsTrue(followerBuffed, "the ant following the trail was never sped up");
            Assert.IsFalse(leaderBuffed, "the trail's own maker was sped up by it");
        }

        [UnityTest]
        public IEnumerator Ants_scuttle_when_they_move_flap_when_they_fly_and_burst_when_they_die()
        {
            var worker = Spawn("WorkerAnt", new Vector3(0f, 0f, 12f));
            var winged = Spawn("WingedAnt", new Vector3(6f, 0f, 12f));
            winged.frozen = true;
            var legs = worker.GetComponent<AntMotion>();
            Assert.IsNotNull(legs, "the worker has no leg animation");
            Assert.AreEqual(6, legs.legs.Length);

            // The worker runs at the player: its legs go, and opposite legs go opposite ways.
            Quaternion front = legs.legs[0].localRotation, middle = legs.legs[1].localRotation;
            float most = 0f;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                most = Mathf.Max(most, Quaternion.Angle(front, legs.legs[0].localRotation));
                yield return null;
            }
            Assert.Greater(legs.Stride01, 0.8f, "a running ant's legs are not moving");
            Assert.Greater(most, 8f);

            // The frozen flyer stays put, legs still, wings beating.
            var wings = winged.GetComponent<AntMotion>();
            Assert.AreEqual(2, wings.wings.Length);
            Assert.AreEqual(0f, wings.Stride01, 0.05f);
            Quaternion wing = wings.wings[0].localRotation;
            yield return null;
            yield return null;
            Assert.AreNotEqual(wing, wings.wings[0].localRotation, "the wings are not beating");

            Assert.IsNotNull(Fx.Instance, "no particle effects in the scene");
            int bursts = Fx.Bursts;
            worker.Health.Kill();
            Assert.Greater(Fx.Bursts, bursts, "nothing flew when the ant died");
        }

        [UnityTest]
        public IEnumerator Every_mutation_has_its_own_look_on_the_body()
        {
            var looks = new Dictionary<MutationType, string>
            {
                { MutationType.Fast, "Speed line" }, { MutationType.Armored, "Armour plate" }, { MutationType.Regenerates, "Regeneration pulse" },
                { MutationType.AcidBlood, "Acid drip" }, { MutationType.SwarmCall, "Antenna glow" }, { MutationType.HardenedShell, "Shell plate" },
            };
            int i = 0;
            foreach (var look in looks)
            {
                var ant = Spawn(i % 2 == 0 ? "WorkerAnt" : "WingedAnt", new Vector3(-8f + i * 3f, 0f, 8f), look.Key);
                ant.frozen = true;
                i++;
                Transform found = null;
                foreach (var part in ant.GetComponentsInChildren<Transform>()) if (part.name == look.Value) found = part;
                Assert.IsNotNull(found, $"{look.Key} has no \"{look.Value}\" on the ant");
                Assert.IsNull(found.GetComponent<Collider>() != null && found.GetComponent<Collider>().enabled ? found : null, "a look must not block shots");

                // The moving ones really move.
                if (look.Key == MutationType.Armored || look.Key == MutationType.HardenedShell) continue;
                Vector3 position = found.localPosition, scale = found.localScale;
                yield return new WaitForSeconds(0.5f);
                Assert.IsTrue(found.localPosition != position || found.localScale != scale, $"the {look.Value} is not animated");
            }
        }

        [UnityTest]
        public IEnumerator Mutated_ants_show_a_badge_for_each_mutation_and_lose_them_when_reused()
        {
            var ant = Spawn("WorkerAnt", new Vector3(0f, 0f, 12f), MutationType.Fast, MutationType.Armored, MutationType.AcidBlood);
            yield return null;
            Assert.AreEqual(3, ant.Mutations.Active.Count);
            Assert.AreEqual(4, ant.Mutations.TotalCost);

            int badges = 0;
            foreach (Transform child in ant.transform) if (child.name.EndsWith("badge")) badges++;
            Assert.AreEqual(3, badges);

            // Switching the ant off and on again (what the pool does) must leave it plain.
            ant.gameObject.SetActive(false);
            ant.gameObject.SetActive(true);
            yield return null;
            Assert.AreEqual(0, ant.Mutations.Active.Count);
            Assert.AreEqual(1f, ant.SpeedMultiplier);
            Assert.AreEqual(1f, ant.Health.damageMultiplier);
        }
    }
}
