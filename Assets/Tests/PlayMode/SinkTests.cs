using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The Kitchen Sink, behind the Encyclopedia: ledges, floods that only the ledges escape, the Sponge and the Tap.</summary>
    public class SinkTests
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
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            _player.Input.overrideFrame = new InputFrame();
            _player.Health.SetInvulnerable(600f);
        }

        [TearDown]
        public void CleanUp()
        {
            Time.timeScale = 1f;
            SaveSystem.Wipe();
        }

        static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        IEnumerator MeetBoss(int floor)
        {
            _floors.BuildFloor(floor);
            yield return null;
            _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => _floors.Boss != null, 4f, "the boss");
            yield return WaitUntil(() => Time.timeScale > 0f, 8f, "the title card to pass");
        }

        [UnityTest]
        public IEnumerator The_sink_is_behind_the_encyclopedia_and_floods_in_a_fight()
        {
            BiomeDefinition sink = null;
            foreach (var biome in _floors.AllBiomes()) if (biome.id == "sink") sink = biome;
            Assert.IsNotNull(sink, "there is no sink");
            Assert.IsTrue(sink.ledges && sink.floods);
            Assert.IsFalse(Achievements.BiomeUnlocked("sink"));
            Achievements.Unlock(AchievementId.Shelved);
            Assert.IsTrue(Achievements.BiomeUnlocked("sink"));
            Assert.IsTrue(_floors.SetBiome("sink"));
            Assert.That(ScreenCards.SinkLines, Does.Contain(ScreenCards.TipFor("sink", 4)));

            // In a fight in a room with ledges, on the floor, the water comes in.
            Flood.BetweenForAll = 0.5f;
            Room ledgeRoom = null;
            for (int floor = 2; floor <= 8 && ledgeRoom == null; floor++)
            {
                _floors.BuildFloor(floor);
                yield return null;
                foreach (var node in _floors.Layout.ordered)
                {
                    if (node.type != RoomType.Normal || ledgeRoom != null) continue;
                    var room = _floors.RoomAt(node.cell);
                    if (Ledges.SpotsIn(room).Count > 0) ledgeRoom = room;
                }
            }
            Assert.IsNotNull(ledgeRoom, "no room with ledges");
            var flood = _floors.GetComponent<Flood>();
            Assert.IsNotNull(flood, "a flooding world with no floods");
            _player.Health.ClearInvulnerability();
            _player.Health.SetMaxHealth(50, true);
            _player.Teleport(ledgeRoom.Centre + Vector3.up * 0.05f);
            yield return WaitUntil(() => _floors.IsLocked, 3f, "the fight to start");
            yield return WaitUntil(() => flood.Soaked >= 1, 10f, "a soaking");
            Assert.GreaterOrEqual(flood.Floods, 1);
            Flood.BetweenForAll = null;

            yield return MeetBoss(10);
            var tap = (StormBoss)_floors.Boss;
            Assert.AreEqual("The Tap", tap.displayName);
            tap.Health.Kill();
            yield return null;
            Assert.IsTrue(Achievements.Has(AchievementId.PlugPulled));
            Assert.IsFalse(Achievements.Has(AchievementId.ClearSkies), "the Tap gave out the Thundercloud's achievement");
        }
    }
}
