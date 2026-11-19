using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Milestone 4: every item, the combinations the design doc promises, looks on the Void, the hero power and drops.</summary>
    public class ItemTests
    {
        PlayerController _player;
        PlayerItems _items;
        EnemySpawner _spawner;
        BiomeDefinition _biome;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _items = _player.Items;
            _spawner = Object.FindAnyObjectByType<EnemySpawner>();
            _biome = _spawner.biome;
            Assert.IsNotNull(_items, "player has no PlayerItems");
            Assert.IsNotNull(GameAssets.Instance, "scene has no GameAssets");
            _player.Input.overrideFrame = new InputFrame();
            _player.transform.rotation = Quaternion.identity;

            _spawner.maxAlive = 0;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            yield return null;
        }

        void Give(ItemType type, int count = 1)
        {
            var item = GameAssets.Instance.items.Get(type);
            Assert.IsNotNull(item, "no item " + type);
            for (int i = 0; i < count; i++) _items.Add(item);
        }

        // A target that stands still and never fights back.
        Enemy Dummy(Vector3 position, string prefabName = "WorkerAnt")
        {
            var enemy = _spawner.SpawnAt(_biome.EnemyNamed(prefabName), position, null);
            enemy.frozen = true;
            return enemy;
        }

        static int Lost(Enemy enemy) => enemy.Health.maxHealth - enemy.Health.Current;

        // Fires exactly one shot straight ahead, then lets it fly.
        IEnumerator FireOnce()
        {
            _player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            Assert.AreEqual(1, _player.weapon.ShotsFired, "expected exactly one shot");
            yield return new WaitForSeconds(0.9f);
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        // ---------- shot items ----------

        [UnityTest]
        public IEnumerator A_plain_shot_stops_at_the_first_enemy()
        {
            var front = Dummy(new Vector3(0f, 0f, 5f));
            var behind = Dummy(new Vector3(0f, 0f, 8f));
            yield return FireOnce();
            Assert.AreEqual(1, Lost(front));
            Assert.AreEqual(0, Lost(behind));
        }

        [UnityTest]
        public IEnumerator Needle_Tip_pierces_and_a_duplicate_pierces_further()
        {
            Give(ItemType.NeedleTip);
            Assert.AreEqual(2, _items.Stats.pierce);
            var a = Dummy(new Vector3(0f, 0f, 4f));
            var b = Dummy(new Vector3(0f, 0f, 6.5f));
            var c = Dummy(new Vector3(0f, 0f, 9f));
            var d = Dummy(new Vector3(0f, 0f, 11.5f));
            yield return FireOnce();
            Assert.AreEqual(1, Lost(a));
            Assert.AreEqual(1, Lost(b));
            Assert.AreEqual(1, Lost(c));
            Assert.AreEqual(0, Lost(d), "one Needle Tip went through more than two enemies");

            Give(ItemType.NeedleTip);
            Assert.AreEqual(4, _items.Stats.pierce, "a duplicate should make the effect stronger");
        }

        [UnityTest]
        public IEnumerator Splitter_Mandible_splits_into_three_on_hit()
        {
            Give(ItemType.SplitterMandible);
            var front = Dummy(new Vector3(0f, 0f, 5f));
            var left = Dummy(new Vector3(-2.4f, 0f, 8f));
            var right = Dummy(new Vector3(2.4f, 0f, 8f));
            var straight = Dummy(new Vector3(0f, 0f, 8.5f));
            yield return FireOnce();
            Assert.AreEqual(1, Lost(front));
            Assert.AreEqual(1, Lost(left), "left split shot missed");
            Assert.AreEqual(1, Lost(right), "right split shot missed");
            Assert.AreEqual(1, Lost(straight), "middle split shot missed");
        }

        [UnityTest]
        public IEnumerator Pierce_split_and_acid_combine_on_one_shot()
        {
            // The doc's table: pierce + split, and split + acid = three puddles.
            Give(ItemType.NeedleTip);
            Give(ItemType.SplitterMandible);
            Give(ItemType.AcidGland);

            var front = Dummy(new Vector3(0f, 0f, 5f), "SoldierAnt");
            var left = Dummy(new Vector3(-2.6f, 0f, 8.2f), "SoldierAnt");
            var right = Dummy(new Vector3(2.6f, 0f, 8.2f), "SoldierAnt");
            var behind = Dummy(new Vector3(0f, 0f, 12f), "SoldierAnt");

            _player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.45f);

            Assert.GreaterOrEqual(Lost(front), 1, "front enemy not hit");
            Assert.GreaterOrEqual(Lost(left), 1, "split shot missed left");
            Assert.GreaterOrEqual(Lost(right), 1, "split shot missed right");

            int puddles = 0;
            foreach (var hazard in GroundHazard.All) if (hazard.hurts == Team.Enemy) puddles++;
            Assert.GreaterOrEqual(puddles, 3, "split + acid should leave at least three puddles");

            yield return new WaitForSeconds(0.5f);
            Assert.GreaterOrEqual(Lost(behind), 1, "the shot did not pierce through to the enemy behind");
        }

        [UnityTest]
        public IEnumerator Acid_Gland_puddles_keep_hurting_enemies_but_not_the_player()
        {
            Give(ItemType.AcidGland);
            var target = Dummy(new Vector3(0f, 0f, 5f), "SoldierAnt");
            yield return FireOnce();
            yield return new WaitForSeconds(1.5f);
            Assert.GreaterOrEqual(Lost(target), 3, "the puddle did not keep hurting the enemy standing in it");

            // The player's own puddle is safe to stand in.
            _player.Teleport(target.transform.position + new Vector3(0.9f, 0.05f, 0f));
            GroundHazard.SpawnFriendlyPuddle(_player.transform.position, 1, false, GameAssets.Instance.playerAcid);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(_player.Health.maxHealth, _player.Health.Current);
        }

        [UnityTest]
        public IEnumerator Firefly_Lantern_sets_enemies_on_fire()
        {
            Give(ItemType.FireflyLantern);
            var target = Dummy(new Vector3(0f, 0f, 5f), "SoldierAnt");
            yield return FireOnce();
            Assert.IsTrue(target.IsBurning, "enemy is not burning");
            yield return new WaitForSeconds(2.5f);
            Assert.GreaterOrEqual(Lost(target), 4, "fire did little damage over time");
            Assert.IsFalse(target.IsBurning, "fire never went out");
        }

        [UnityTest]
        public IEnumerator Fire_and_acid_make_burning_puddles()
        {
            Give(ItemType.FireflyLantern);
            Give(ItemType.AcidGland);
            Dummy(new Vector3(0f, 0f, 5f), "SoldierAnt");
            _player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.4f);

            GroundHazard puddle = null;
            foreach (var hazard in GroundHazard.All) if (hazard.hurts == Team.Enemy) puddle = hazard;
            Assert.IsNotNull(puddle, "no puddle");
            Assert.IsTrue(puddle.burning, "puddle is not burning");
        }

        [UnityTest]
        public IEnumerator Compound_Eye_widens_aim_assist_and_adds_crits()
        {
            _player.weapon.aimAssistStrength = 1f; // this is about full assist, whatever a new player starts on
            // 24 degrees off target (a little more as seen from the hand): outside the normal 20 degree cone, inside the widened 30.
            var target = Dummy(new Vector3(0f, 0f, 8f), "SoldierAnt");
            Vector3 offTarget = Quaternion.Euler(0f, 24f, 0f) * Vector3.forward;

            _player.Input.overrideFrame = new InputFrame { aim = offTarget, fire = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(0, Lost(target), "aim assist reached too far without the item");

            Give(ItemType.CompoundEye);
            Assert.AreEqual(0.15f, _items.Stats.critChance, 0.001f);
            _player.Input.overrideFrame = new InputFrame { aim = offTarget, fire = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.7f);
            Assert.GreaterOrEqual(Lost(target), 1, "the wider cone did not pull the shot in");
        }

        // ---------- body items ----------

        [UnityTest]
        public IEnumerator Extra_Legs_speed_up_walking_and_the_dodge_recharge()
        {
            _player.Input.overrideFrame = new InputFrame { move = Vector3.right };
            yield return new WaitForSeconds(0.5f);
            float plain = _player.transform.position.x;

            _player.Teleport(new Vector3(0f, 0.05f, 0f));
            Give(ItemType.ExtraLegs);
            yield return new WaitForSeconds(0.5f);
            float withLegs = _player.transform.position.x;
            Assert.Greater(withLegs, plain * 1.1f);
            Assert.Less(_items.Stats.dodgeCooldownMultiplier, 0.8f);
        }

        [UnityTest]
        public IEnumerator Wing_Buds_hover_over_acid()
        {
            // A pool right in the player's path.
            var mutation = _biome.Mutation(MutationType.AcidBlood);
            GroundHazard.SpawnAcidPool(new Vector3(0f, 0f, 2.2f), mutation.effectMaterial);

            Give(ItemType.WingBuds);
            _player.Input.overrideFrame = new InputFrame { move = Vector3.forward, dodge = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame { move = Vector3.forward };
            Assert.IsTrue(_player.IsHovering, "dodge did not become a hover");

            bool wasOverPool = false;
            while (_player.IsDodging)
            {
                wasOverPool |= Mathf.Abs(_player.transform.position.z - 2.2f) < 1.4f;
                yield return null;
            }
            Assert.IsTrue(wasOverPool, "the hover never crossed the pool");
            Assert.Greater(_player.transform.position.z, 3.9f, "the hover did not clear the pool");
            Assert.AreEqual(_player.Health.maxHealth, _player.Health.Current, "hurt while hovering");
        }

        [UnityTest]
        public IEnumerator Wing_Buds_with_Extra_Legs_give_two_hovers_in_a_row()
        {
            Give(ItemType.WingBuds);
            yield return null;
            Assert.AreEqual(1, _player.DodgeCharges);

            Give(ItemType.ExtraLegs);
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(2, _items.Stats.dodgeCharges);
            yield return WaitUntil(() => _player.DodgeCharges == 2, 2f, "the second dodge to charge");

            for (int hover = 1; hover <= 2; hover++)
            {
                _player.Input.overrideFrame = new InputFrame { move = Vector3.forward, dodge = true };
                yield return null;
                _player.Input.overrideFrame = new InputFrame();
                Assert.IsTrue(_player.IsHovering, $"hover {hover} did not start");
                yield return WaitUntil(() => !_player.IsDodging, 1f, "the hover to end");
            }
            Assert.Less(_player.DodgeCharges, 2, "both hovers should have used a charge");
        }

        [UnityTest]
        public IEnumerator Chitin_Plate_blocks_one_hit_until_recharged()
        {
            Give(ItemType.ChitinPlate);
            yield return null;
            int max = _player.Health.maxHealth;

            Assert.IsTrue(_player.Health.TakeDamage(1, Vector3.forward));
            Assert.AreEqual(max, _player.Health.Current, "the plate did not block the first hit");
            Assert.AreEqual(0, _items.ShieldCharges);

            yield return new WaitForSeconds(0.6f);
            _player.Health.TakeDamage(1, Vector3.forward);
            Assert.AreEqual(max - 1, _player.Health.Current, "the plate blocked a second hit in the same room");

            _items.RechargeShield(); // what entering a new room does
            yield return new WaitForSeconds(0.9f);
            _player.Health.TakeDamage(1, Vector3.forward);
            Assert.AreEqual(max - 1, _player.Health.Current, "the plate did not come back");
        }

        [UnityTest]
        public IEnumerator Sugar_Cube_adds_a_health_point_and_heals()
        {
            var hud = Object.FindAnyObjectByType<Hud>();
            _player.Health.TakeDamage(2, Vector3.forward);
            yield return null;
            Assert.AreEqual(1, hud.LitPips);

            Give(ItemType.SugarCube);
            yield return null;
            Assert.AreEqual(4, _player.Health.maxHealth);
            Assert.AreEqual(4, _player.Health.Current);
            Assert.AreEqual(4, hud.LitPips, "the HUD did not grow a fourth health pip");
        }

        [UnityTest]
        public IEnumerator Queens_Jelly_raises_every_stat_a_little()
        {
            PlayerStats before = _items.Stats;
            Give(ItemType.QueensJelly);
            PlayerStats after = _items.Stats;
            Assert.Greater(after.moveSpeedMultiplier, before.moveSpeedMultiplier);
            Assert.Less(after.dodgeCooldownMultiplier, before.dodgeCooldownMultiplier);
            Assert.Less(after.fireIntervalMultiplier, before.fireIntervalMultiplier);
            Assert.Greater(after.critChance, before.critChance);
            Assert.AreEqual(4, _player.Health.maxHealth);
            Assert.IsTrue(GameAssets.Instance.items.Get(ItemType.QueensJelly).rare);
            yield return null;
        }

        // ---------- companions and crowd control ----------

        [UnityTest]
        public IEnumerator Confused_ants_attack_each_other()
        {
            _player.Health.SetInvulnerable(60f);
            var attacker = _spawner.SpawnAt(_biome.EnemyNamed("WorkerAnt"), new Vector3(0f, 0f, 10f), null);
            var victim = Dummy(new Vector3(3f, 0f, 10f), "SoldierAnt");
            attacker.Confuse(6f);
            yield return WaitUntil(() => Lost(victim) > 0, 5f, "the confused ant to bite its neighbour");
        }

        [UnityTest]
        public IEnumerator Pheromone_Cloud_confuses_enemies_near_the_player()
        {
            _player.Health.SetInvulnerable(60f);
            Give(ItemType.PheromoneCloud);
            _items.pheromoneChance = 1f; // certain, so the test isn't down to luck
            _items.pheromoneInterval = 0.2f;

            var near = Dummy(new Vector3(0f, 0f, 4f));
            var far = Dummy(new Vector3(0f, 0f, 13f));
            yield return WaitUntil(() => near.IsConfused, 1.5f, "the nearby ant to be confused");
            Assert.IsFalse(far.IsConfused, "an ant outside the cloud was confused");
        }

        [UnityTest]
        public IEnumerator Larva_Buddy_follows_and_bites()
        {
            Give(ItemType.LarvaBuddy);
            yield return null;
            Assert.IsNotNull(_items.Buddy, "no larva appeared");

            var target = Dummy(new Vector3(0f, 0f, 5f), "SoldierAnt");
            yield return WaitUntil(() => Lost(target) >= 2, 5f, "the larva to bite twice");
            Assert.AreEqual(0, _player.weapon.ShotsFired, "the player was not meant to shoot in this test");

            // Shot items power up its bite.
            Give(ItemType.FireflyLantern);
            yield return WaitUntil(() => target.IsBurning, 3f, "the larva's bite to set the enemy on fire");
        }

        // ---------- looks ----------

        [UnityTest]
        public IEnumerator Items_show_on_the_Void_and_the_newest_wins_a_slot()
        {
            Give(ItemType.NeedleTip);
            Give(ItemType.SplitterMandible);
            yield return null;

            // Two items in different slots: both visible on the body.
            Assert.IsNotNull(_items.LookIn(ItemSlot.Hands));
            Assert.IsNotNull(_items.LookIn(ItemSlot.Head));
            Assert.IsTrue(_items.LookIn(ItemSlot.Hands).transform.IsChildOf(_player.visual));
            Assert.Greater(_items.LookIn(ItemSlot.Hands).GetComponentsInChildren<Renderer>().Length, 0);
            Assert.Greater(_items.LookIn(ItemSlot.Head).GetComponentsInChildren<Renderer>().Length, 0);

            // Two items in the same slot: the newer one shows.
            Give(ItemType.FireflyLantern);
            yield return null;
            Assert.AreEqual("Firefly Lantern", _items.LookIn(ItemSlot.Back).name);
            Give(ItemType.ExtraLegs);
            yield return null;
            Assert.AreEqual("Extra Legs", _items.LookIn(ItemSlot.Back).name);

            int backLooks = 0;
            foreach (Transform child in _player.visual)
            {
                if (child.name == "Firefly Lantern" || child.name == "Extra Legs") backLooks++;
            }
            Assert.AreEqual(1, backLooks, "the old look in the slot was not removed");

            // Both still work even though only one shows.
            Assert.AreEqual(1, _items.Stats.burn);
        }

        [UnityTest]
        public IEnumerator Freeze_Frame_slows_enemies_and_their_shots_but_not_the_player()
        {
            var power = _player.Power;
            power.SetKind(PowerKind.FreezeFrame);
            Assert.AreEqual("Freeze Frame", power.displayName);
            Enemy runner = _spawner.SpawnAt(_biome.EnemyNamed("WorkerAnt"), new Vector3(0f, 0f, 16f), null);
            runner.transform.position += Vector3.up * 0.05f;
            _player.Health.SetInvulnerable(600f);
            yield return new WaitForSeconds(0.3f);

            // How far it gets in half a second at full speed, then in a second of Freeze Frame.
            Vector3 from = runner.transform.position;
            yield return new WaitForSeconds(0.5f);
            float usual = Vector3.ProjectOnPlane(runner.transform.position - from, Vector3.up).magnitude * 2f;
            Assert.Greater(usual, 2f, "the ant was not running to begin with");

            power.SetMeter(1f);
            Assert.IsTrue(power.TryActivate());
            Assert.IsTrue(power.IsFreezing);
            Assert.IsFalse(power.IsOvercharged, "Freeze Frame should not give the damage Overcharge does");
            Assert.AreEqual(1, _items.Stats.damageMultiplier);
            from = runner.transform.position;
            Vector3 playerFrom = _player.transform.position;
            _player.Input.overrideFrame = new InputFrame { move = Vector3.left };
            yield return new WaitForSeconds(1f);
            _player.Input.overrideFrame = new InputFrame();
            float slowed = Vector3.ProjectOnPlane(runner.transform.position - from, Vector3.up).magnitude;
            Assert.Less(slowed, usual * 0.4f, "the enemy was not slowed");
            Assert.Greater((_player.transform.position - playerFrom).magnitude, 4f, "the player was slowed too");

            // It wears off, and the world is put back.
            yield return new WaitForSeconds(power.duration);
            Assert.IsFalse(power.IsActive);
            Assert.AreEqual(1f, Enemy.WorldSlow);
        }

        [UnityTest]
        public IEnumerator Second_Wind_heals_clears_shots_and_keeps_the_player_safe()
        {
            var power = _player.Power;
            power.SetKind(PowerKind.SecondWind);
            _player.Health.TakeDamage(1, Vector3.forward);
            yield return WaitUntil(() => !_player.Health.Invulnerable, 3f, "the hit to wear off");
            int hurt = _player.Health.Current;

            // A shot from a spitter is in the air when it goes off.
            var spitter = (ShooterEnemy)_spawner.SpawnAt(_biome.EnemyNamed("SpitterAnt"), new Vector3(0f, 0f, 9f), null);
            yield return WaitUntil(() => spitter.ShotsFired > 0, 6f, "a shot to be in the air");
            yield return null;

            power.SetMeter(1f);
            Assert.IsTrue(power.TryActivate());
            Assert.AreEqual(hurt + 1, _player.Health.Current, "no health back");
            Assert.GreaterOrEqual(power.LastShotsCleared, 1, "the shot in the air was left there");
            Assert.IsTrue(_player.Health.Invulnerable);
            Assert.IsFalse(_player.Health.TakeDamage(1, Vector3.forward), "hurt during Second Wind");
            Assert.AreEqual(hurt + 1, _player.Health.Current);
        }

        [UnityTest]
        public IEnumerator The_marble_hits_harder_and_slower_and_sprinkles_fire_faster()
        {
            Enemy dummy = Dummy(new Vector3(0f, 0f, 6f), "SoldierAnt");
            yield return null;
            yield return FireOnce();
            Assert.AreEqual(1, Lost(dummy));

            Give(ItemType.LostMarble);
            Assert.Greater(_items.Stats.fireIntervalMultiplier, 1.3f);
            _player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            Assert.AreEqual(2, _player.weapon.ShotsFired);
            yield return new WaitForSeconds(0.9f);
            Assert.AreEqual(3, Lost(dummy), "a marble shot should do 2");

            Give(ItemType.Sprinkles, 2);
            Assert.Less(_items.Stats.fireIntervalMultiplier, 1f, "two lots of sprinkles should more than make up for one marble");
        }

        [UnityTest]
        public IEnumerator The_sock_charges_every_fifth_shot()
        {
            Give(ItemType.StaticSock);
            Assert.AreEqual(5, _items.Stats.chargedEvery);
            Enemy dummy = Dummy(new Vector3(0f, 0f, 6f), "SoldierAnt");
            dummy.Health.SetMaxHealth(50, true);
            yield return null;

            _player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            for (float t = 0f; t < 4f && _player.weapon.ShotsFired < 5; t += Time.deltaTime) yield return null;
            _player.Input.overrideFrame = new InputFrame();
            Assert.AreEqual(5, _player.weapon.ShotsFired);
            Assert.AreEqual(1, _player.weapon.ChargedShots);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(6, Lost(dummy), "four plain shots and one doubled");

            Give(ItemType.StaticSock, 2);
            Assert.AreEqual(3, _items.Stats.chargedEvery);
        }

        [UnityTest]
        public IEnumerator The_candle_leaves_fire_behind_a_dodge_and_the_dose_shrinks_the_Void()
        {
            Give(ItemType.BirthdayCandle);
            _player.Input.overrideFrame = new InputFrame { move = Vector3.forward, dodge = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            bool fire = false;
            foreach (var hazard in GroundHazard.All) fire |= hazard.burning && hazard.hurts == Team.Enemy;
            Assert.IsTrue(fire, "no fire where the dodge started");

            float speed = _items.Stats.moveSpeedMultiplier;
            Give(ItemType.FinchsDose);
            Assert.AreEqual(PlayerItems.DoseSize, _player.transform.localScale.x, 0.001f);
            Assert.Greater(_items.Stats.moveSpeedMultiplier, speed);
            Assert.IsTrue(GameAssets.Instance.items.Get(ItemType.FinchsDose).rare, "the dose should only come from Finch");
            Give(ItemType.FinchsDose, 5);
            Assert.GreaterOrEqual(_player.transform.localScale.x, 0.55f, "the Void shrank away to nothing");
        }

        [UnityTest]
        public IEnumerator Every_item_has_a_name_a_description_and_a_look()
        {
            var library = GameAssets.Instance.items;
            Assert.AreEqual(17, library.items.Length);
            foreach (var item in library.items)
            {
                Assert.IsNotEmpty(item.displayName);
                Assert.IsNotEmpty(item.description);
                Assert.IsNotNull(item.material, item.displayName + " has no pickup colour");
                if (item.slot != ItemSlot.Companion) Assert.IsNotNull(item.visual, item.displayName + " has no look");
                _items.Add(item); // must not throw
            }
            yield return null;
            Assert.AreEqual(17, _items.Collected.Count);
        }

        // ---------- hero power ----------

        [UnityTest]
        public IEnumerator Overcharge_fills_with_kills_then_doubles_damage_and_pierces()
        {
            var power = _player.Power;
            Assert.IsNotNull(power);
            Assert.IsFalse(power.TryActivate(), "went off with an empty meter");

            for (int i = 0; i < 25; i++) _spawner.SpawnAt(_biome.EnemyNamed("WorkerAnt"), new Vector3(10f, 0f, 10f), null).Health.Kill();
            yield return null;
            Assert.IsTrue(power.IsReady, $"25 kills did not fill the meter ({power.Meter:F2})");

            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            yield return null;

            // The button press goes through the same input as everything else.
            _player.Input.overrideFrame = new InputFrame { power = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            Assert.IsTrue(power.IsActive, "the power button did nothing");
            Assert.AreEqual(0f, power.Meter);
            Assert.AreEqual(2, _items.Stats.damageMultiplier);

            var front = Dummy(new Vector3(0f, 0f, 5f), "SoldierAnt");
            var behind = Dummy(new Vector3(0f, 0f, 8f), "SoldierAnt");
            _player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(2, Lost(front), "damage was not doubled");
            Assert.AreEqual(2, Lost(behind), "the shot did not pierce");

            // And it runs out.
            power.duration = 0.3f;
            power.SetMeter(1f);
            Assert.IsFalse(power.TryActivate(), "could be set off again while already running");
        }

        [UnityTest]
        public IEnumerator Every_item_has_its_one_line()
        {
            var lines = new HashSet<string>();
            foreach (var item in GameAssets.Instance.items.items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.flavour), item.displayName + " has no line");
                Assert.IsTrue(lines.Add(item.flavour), "two items share a line");
                Assert.That(item.Blurb, Does.StartWith(item.description).And.EndWith(item.flavour));
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator Overcharge_starts_with_a_surge_and_makes_the_player_faster()
        {
            var power = _player.Power;
            var near = Dummy(new Vector3(0f, 0f, 4f));
            var far = Dummy(new Vector3(0f, 0f, 12f));
            yield return null;
            float fieldOfView = Camera.main.fieldOfView;

            power.SetMeter(1f);
            Assert.IsTrue(power.TryActivate());
            Assert.AreEqual(1, power.LastSurgeHits, "the surge should catch the near ant and not the far one");
            Assert.AreEqual(near.Health.maxHealth - power.surgeDamage, near.Health.Current);
            Assert.AreEqual(far.Health.maxHealth, far.Health.Current);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(near.transform.position.z, 4.3f, "the near ant was not thrown back");
            Assert.Greater(Camera.main.fieldOfView, fieldOfView + 5f, "the view did not open up");

            Vector3 from = _player.transform.position;
            _player.Input.overrideFrame = new InputFrame { move = Vector3.left };
            yield return new WaitForSeconds(0.5f);
            _player.Input.overrideFrame = new InputFrame();
            float covered = (_player.transform.position - from).magnitude;
            Assert.Greater(covered, _player.moveSpeed * 0.5f * 1.1f, "no faster with the power on");
        }

        [UnityTest]
        public IEnumerator Overcharge_ends_after_its_time()
        {
            var power = _player.Power;
            power.duration = 0.4f;
            power.SetMeter(1f);
            Assert.IsTrue(power.TryActivate());
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(power.IsActive);
            Assert.AreEqual(1, _items.Stats.damageMultiplier);
        }

        // ---------- drops ----------

        [UnityTest]
        public IEnumerator Dead_ants_drop_coins_and_samples_that_fly_to_the_player()
        {
            Assert.AreEqual(0, RunState.Coins);
            var ant = Dummy(new Vector3(0f, 0f, 2.5f));
            ant.Health.Kill();
            yield return null;

            int coins = 0, samples = 0;
            foreach (var pickup in Pickup.All)
            {
                if (pickup.kind == PickupKind.Coin) coins++;
                if (pickup.kind == PickupKind.Sample) samples++;
            }
            Assert.Greater(coins, 0, "no coins dropped");
            Assert.AreEqual(1, samples, "no sample dropped");

            yield return WaitUntil(() => RunState.Coins == coins && RunState.Samples == Loot.SamplesPerEnemy, 3f, "the drops to be collected");
            Assert.AreEqual(1, RunState.Kills);
        }

        [UnityTest]
        public IEnumerator Mutated_ants_drop_more_samples()
        {
            var plain = Dummy(new Vector3(0f, 0f, 2f));
            plain.Health.Kill();
            yield return WaitUntil(() => RunState.Samples == Loot.SamplesPerEnemy, 3f, "the plain ant's sample");

            var mutated = _spawner.SpawnAt(_biome.EnemyNamed("WorkerAnt"), new Vector3(0f, 0f, 2f),
                new[] { _biome.Mutation(MutationType.Armored), _biome.Mutation(MutationType.Fast) });
            mutated.frozen = true;
            mutated.Health.Kill();
            yield return WaitUntil(() => RunState.Samples == 2 * Loot.SamplesPerEnemy + 3, 3f, "the mutated ant's samples (the usual amount + 3 mutation points)");
        }
    }
}
