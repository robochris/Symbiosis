using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Under the Sofa's two bosses, which have fights of their own: Grandmother Dust and the Robot Vacuum.</summary>
    public class SofaBossTests
    {
        PlayerController _player;
        FloorManager _floors;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            Radio.ChanceOverride = 0f;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            _player.Input.overrideFrame = new InputFrame();
            _player.Health.SetInvulnerable(6000f);
            Assert.IsTrue(_floors.SetBiome("sofa"));
        }

        [TearDown]
        public void CleanUp()
        {
            SaveSystem.Wipe();
            Radio.ChanceOverride = null;
            Time.timeScale = 1f;
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator MeetBoss(int floor, Vector3 offset)
        {
            _floors.BuildFloor(floor);
            yield return null;
            _player.Health.SetInvulnerable(6000f);
            _player.Teleport(_floors.ArenaCentre + offset + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.Boss != null, 5f, "the boss");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
            Time.timeScale = 3f;
        }

        float Apart(Enemy enemy)
        {
            Vector3 to = enemy.transform.position - _player.transform.position;
            to.y = 0f;
            return to.magnitude;
        }

        [UnityTest]
        public IEnumerator Grandmother_Dust_hops_sheds_sneezes_and_has_a_soft_tail()
        {
            yield return MeetBoss(5, new Vector3(0f, 0f, -9f));
            var granny = (GrandmotherDust)_floors.Boss;
            Vector3 began = granny.transform.position;

            // The hop is warned of by a disc where she will come down.
            yield return WaitUntil(() => granny.Current == GrandmotherDust.State.Crouch, 5f, "the crouch");
            bool warned = false;
            foreach (var hazard in GroundHazard.All) warned |= hazard.isBlast;
            Assert.IsTrue(warned, "no warning of where she will land");
            yield return WaitUntil(() => granny.Hops > 0 && granny.Current == GrandmotherDust.State.Rest, 5f, "the landing");
            Assert.Greater((granny.transform.position - began).magnitude, 2f, "she hopped on the spot");
            Assert.Less(granny.transform.position.y, 0.5f, "she did not come back down");
            Assert.Greater(granny.FluffShed, 0, "nothing was shed on landing");
            Assert.Greater(_floors.LiveEnemies.Count, 1);

            // The tail takes double.
            int before = granny.Health.Current;
            granny.Health.TakeDamage(3, -granny.transform.forward);
            Assert.AreEqual(before - 3, granny.Health.Current);
            granny.Health.TakeDamage(3, granny.transform.forward);
            Assert.AreEqual(before - 9, granny.Health.Current, "the tail did not take double damage");

            // Every third turn is a sneeze.
            yield return WaitUntil(() => granny.Sneezes > 0, 20f, "a sneeze");
            Assert.GreaterOrEqual(granny.Hops, 2);

            // Below half: every landing leaves sticky lint.
            granny.Health.TakeDamage(granny.Health.Current - granny.Health.maxHealth / 2 + 5, -granny.transform.forward);
            int hops = granny.Hops;
            yield return WaitUntil(() => granny.Hops > hops && granny.Current == GrandmotherDust.State.Rest, 15f, "a hop while shedding");
            Assert.AreEqual(2, granny.Phase);
            bool sticky = false;
            foreach (var hazard in GroundHazard.All) sticky |= hazard.slowTo > 0f;
            Assert.IsTrue(sticky, "no sticky lint after the landing");

            granny.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 6f, "the arena to open");
        }

        [UnityTest]
        public IEnumerator The_Jelly_Mother_flops_lobs_and_tears_into_pieces()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            yield return MeetBoss(5, new Vector3(0f, 0f, -12f));
            var mother = (JellyMother)_floors.Boss;

            // Gumdrops come over first.
            yield return WaitUntil(() => mother.Lobbed > 0, 10f, "gumdrops");
            Assert.Greater(_floors.LiveEnemies.Count, 1);

            // Close in: she flops, with a disc round her as the warning, then lies there.
            yield return WaitUntil(() => mother.Current == JellyMother.State.FlopWindup, 20f, "the belly flop");
            bool warned = false;
            foreach (var hazard in GroundHazard.All) warned |= hazard.isBlast;
            Assert.IsTrue(warned, "a flop with no warning");
            yield return WaitUntil(() => mother.Current == JellyMother.State.Wobble, 5f, "her to land");
            Assert.GreaterOrEqual(mother.Flops, 1);

            // The jelly takes double.
            int before = mother.Health.Current;
            mother.Health.TakeDamage(3, mother.transform.forward);
            Assert.AreEqual(before - 6, mother.Health.Current, "the jelly did not take double damage");

            // Two thirds: a piece tears off as two bears, and she is smaller and quicker.
            int bears = 0;
            foreach (var enemy in _floors.LiveEnemies) if (enemy is ChaserEnemy && enemy.gameObject.activeSelf) bears++;
            mother.Health.TakeDamage(mother.Health.Current - Mathf.FloorToInt(mother.Health.maxHealth * 0.6f), -mother.transform.forward);
            yield return WaitUntil(() => mother.Splits == 1, 8f, "the first split");
            Assert.AreEqual(2, mother.Phase);
            Assert.Less(mother.BaseScale, 0.9f);
            Assert.Greater(mother.SpeedMultiplier, 1.1f);
            int after = 0;
            foreach (var enemy in _floors.LiveEnemies) if (enemy is ChaserEnemy && enemy.gameObject.activeSelf) after++;
            Assert.GreaterOrEqual(after, bears + 2, "nothing tore off");

            mother.Health.TakeDamage(mother.Health.Current - Mathf.FloorToInt(mother.Health.maxHealth * 0.3f), -mother.transform.forward);
            yield return WaitUntil(() => mother.Splits == 2, 8f, "the second split");
            Assert.Less(mother.BaseScale, 0.75f);

            mother.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 6f, "the arena to open");
            Assert.IsFalse(Achievements.Has(AchievementId.MothersDay), "the Jelly Mother was mistaken for the Brood Mother");
        }

        [UnityTest]
        public IEnumerator Static_enemies_shock_whatever_stands_close()
        {
            MutationDefinition charge = _floors.biome.Mutation(MutationType.Static);
            Assert.IsNotNull(charge, "the sofa has no Static mutation");

            _floors.BuildFloor(4);
            yield return null;
            _player.Health.SetInvulnerable(6000f);
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                _player.Teleport(_floors.RoomAt(node.cell).Centre + new Vector3(RoomShape.TileSize, 0.05f, 0f));
                break;
            }
            yield return WaitUntil(() => _floors.IsLocked && _floors.LiveEnemies.Count > 0, 3f, "a fight");
            Enemy enemy = _floors.LiveEnemies[0];
            enemy.frozen = true;
            foreach (var other in new System.Collections.Generic.List<Enemy>(_floors.LiveEnemies)) if (other != enemy) other.Vanish();
            enemy.Mutations.Apply(new System.Collections.Generic.List<MutationDefinition> { charge });
            Assert.AreEqual(0, enemy.Mutations.Discharges, "it went off the moment it arrived");

            yield return WaitUntil(() => enemy.Mutations.Discharges > 0, 6f, "a discharge");
            bool disc = false;
            foreach (var hazard in GroundHazard.All) disc |= hazard.isBlast && (hazard.transform.position - enemy.transform.position).magnitude < 1f;
            Assert.IsTrue(disc, "a discharge with no warning disc");

            // Standing in it when it goes off hurts.
            _player.Teleport(enemy.transform.position + new Vector3(1.5f, 0.05f, 0f));
            _player.Health.ClearInvulnerability();
            int health = _player.Health.Current;
            yield return WaitUntil(() => enemy.Mutations.Discharges > 1, 8f, "the next discharge");
            yield return new WaitForSeconds(EnemyMutations.StaticFuse + 0.3f);
            Assert.Less(_player.Health.Current, health, "standing next to a Static enemy did not hurt");
        }

        [UnityTest]
        public IEnumerator The_Trick_Candle_drips_wax_lights_sparklers_and_spins()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            yield return MeetBoss(10, new Vector3(0f, 0f, -10f));
            var candle = (TrickCandle)_floors.Boss;
            yield return null;
            Assert.AreEqual(candle.sparklerCount, EggEnemy.All.Count);

            // Wax: a warning first, then a pool that stays.
            yield return WaitUntil(() => candle.WaxDropped > 0, 8f, "wax");
            bool warned = false;
            foreach (var hazard in GroundHazard.All) warned |= hazard.isBlast;
            Assert.IsTrue(warned, "wax with no warning");
            yield return new WaitForSeconds(candle.waxFuse + 0.3f);
            bool pool = false;
            foreach (var hazard in GroundHazard.All) pool |= !hazard.isBlast && hazard.hurts == Team.Player;
            Assert.IsTrue(pool, "the wax left no pool");

            // It does not charge about: it slides.
            Vector3 was = candle.transform.position;
            yield return new WaitForSeconds(2f);
            Assert.Less((candle.transform.position - was).magnitude, 2f * candle.slideSpeed * 1.5f);

            // The boxes are lit one at a time.
            yield return WaitUntil(() => candle.SparklersLit > 0, 30f, "a sparkler box to be lit");
            Assert.AreEqual(candle.sparklerCount - 1, EggEnemy.All.Count);

            // Below 60%: the spiral.
            Assert.AreEqual(0, candle.Spirals);
            candle.Health.TakeDamage(candle.Health.Current - Mathf.FloorToInt(candle.Health.maxHealth * 0.55f), Vector3.forward);
            yield return WaitUntil(() => candle.Current == TrickCandle.State.Spiral, 10f, "the spiral");
            Assert.AreEqual(2, candle.Phase);
            yield return WaitUntil(() => candle.Current == TrickCandle.State.Burn, 10f, "the spiral to end");

            // The trick still works, and it is quicker afterwards.
            candle.Health.TakeDamage(candle.Health.Current - Mathf.FloorToInt(candle.Health.maxHealth * 0.3f), Vector3.forward);
            yield return WaitUntil(() => candle.IsOut, 3f, "it to go out");
            yield return WaitUntil(() => !candle.IsOut, 10f, "it to relight");
            Assert.AreEqual(3, candle.Phase);
            Assert.Greater(candle.SpeedMultiplier, 1.2f);

            candle.Health.Kill();
            yield return WaitUntil(() => _floors.ExtractPortal != null, 8f, "the portals");
            Assert.IsTrue(Achievements.Has(AchievementId.SweetTooth));
            Assert.IsFalse(Achievements.Has(AchievementId.Regicide), "the candle was mistaken for the Ant Queen");
        }

        [UnityTest]
        public IEnumerator The_Robot_Vacuum_sprays_sucks_and_has_to_empty_its_bin()
        {
            yield return MeetBoss(10, new Vector3(0f, 0f, -9f));
            var vacuum = (RobotVacuum)_floors.Boss;
            yield return null;
            Assert.AreEqual(vacuum.startLint, EggEnemy.All.Count, "the arena did not start dusty");

            yield return WaitUntil(() => vacuum.Sprays > 0, 8f, "a ring of sparks");

            // It stops to suck, and the player is dragged towards it.
            yield return WaitUntil(() => vacuum.Current == RobotVacuum.State.Suck, 20f, "the suction");
            _player.Teleport(vacuum.transform.position + new Vector3(0f, 0.05f, -10f));
            yield return null;
            float far = Apart(vacuum);
            yield return new WaitForSeconds(1f);
            Assert.Less(Apart(vacuum), far - 1.5f, "the suction did not pull the player in");

            // Then the bin is full: lid open, double damage, new lint left behind, and the old lint hatched.
            yield return WaitUntil(() => vacuum.BinOpen, 10f, "the bin to open");
            Assert.Greater(vacuum.LintDropped, vacuum.startLint);
            Assert.Greater(_floors.LiveEnemies.Count, 1 + vacuum.lintPerEmpty, "the old lint did not hatch");
            int before = vacuum.Health.Current;
            vacuum.Health.TakeDamage(10, Vector3.forward);
            Assert.AreEqual(before - 20, vacuum.Health.Current, "an open bin should take double");
            yield return WaitUntil(() => !vacuum.BinOpen, 10f, "the bin to shut");
            before = vacuum.Health.Current;
            vacuum.Health.TakeDamage(10, Vector3.forward);
            Assert.AreEqual(before - 10, vacuum.Health.Current);

            // Turbo: it charges, with a warning line first.
            vacuum.Health.TakeDamage(vacuum.Health.Current - Mathf.FloorToInt(vacuum.Health.maxHealth * 0.5f), Vector3.forward);
            yield return WaitUntil(() => vacuum.Current == RobotVacuum.State.ChargeWindup, 25f, "a charge to wind up");
            Assert.AreEqual(2, vacuum.Phase);
            yield return null;
            Assert.IsTrue(vacuum.ChargeLineShowing, "a charge with no warning");
            yield return WaitUntil(() => vacuum.Charges > 0, 5f, "the charge");

            // Low battery: it stops and recharges; enough damage makes that fail and leaves it stunned and open.
            yield return WaitUntil(() => vacuum.Current != RobotVacuum.State.Charge && vacuum.Current != RobotVacuum.State.Empty, 10f, "the charge to end");
            vacuum.Health.TakeDamage(vacuum.Health.Current - Mathf.FloorToInt(vacuum.Health.maxHealth * 0.2f), Vector3.forward);
            yield return WaitUntil(() => vacuum.Current == RobotVacuum.State.Dock, 5f, "the recharge");
            Assert.IsTrue(vacuum.Docked);
            int low = vacuum.Health.Current;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(vacuum.Health.Current, low, "it did not recharge");
            vacuum.Health.TakeDamage(vacuum.dockInterrupt, Vector3.forward);
            Assert.IsTrue(vacuum.DockFailed);
            Assert.IsTrue(vacuum.BinOpen, "a failed recharge should leave it open");

            // It only has the one battery.
            yield return WaitUntil(() => !vacuum.BinOpen, 15f, "it to get going again");
            yield return new WaitForSeconds(1f);
            Assert.AreNotEqual(RobotVacuum.State.Dock, vacuum.Current);

            vacuum.Health.Kill();
            yield return WaitUntil(() => _floors.ExtractPortal != null, 8f, "the portals");
            Assert.IsFalse(Achievements.Has(AchievementId.Regicide), "the vacuum was mistaken for the Ant Queen");
            Assert.IsTrue(Achievements.Has(AchievementId.SpringClean));
            Assert.IsTrue(Achievements.Has(AchievementId.Unplugged));
            Assert.IsTrue(Achievements.StyleUnlocked("Lint"));
        }
    }
}
