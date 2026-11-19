using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Dr. Finch on the radio: what gets said when, without repeats, and the setting that turns it down.</summary>
    public class RadioTests
    {
        PlayerController _player;
        FloorManager _floors;
        Radio _radio;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            Radio.ChanceOverride = 0f; // no chatter unless a test asks for it
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            _radio = Radio.Instance;
            _player.Input.overrideFrame = new InputFrame();
        }

        [TearDown]
        public void CleanUp()
        {
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            Radio.ChanceOverride = null;
            Time.timeScale = 1f;
        }

        IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        [UnityTest]
        public IEnumerator Every_line_is_its_own_and_short_enough_to_read()
        {
            var seen = new HashSet<string>();
            var ids = new HashSet<int>();
            foreach (string line in RadioLines.All())
            {
                Assert.IsTrue(seen.Add(line), "said twice in the script: " + line);
                Assert.IsTrue(ids.Add(RadioLines.Id(line)), "two lines share an id: " + line);
                Assert.LessOrEqual(line.Length, 125, "too long for the panel: " + line);
            }
            Assert.Greater(seen.Count, 90, "the pool is too small to stay fresh");
            yield break;
        }

        [UnityTest]
        public IEnumerator No_line_repeats_until_the_rest_of_its_pool_has_been_said()
        {
            var rng = new System.Random(5);
            int size = RadioLines.PoolFor(RadioMoment.Death).Count;
            var heard = new HashSet<string>();
            for (int i = 0; i < size; i++) Assert.IsTrue(heard.Add(RadioLines.Pick(RadioMoment.Death, null, rng)), "a line came round early");
            // The pool starts again, and what was heard is in the save file.
            Assert.IsTrue(heard.Contains(RadioLines.Pick(RadioMoment.Death, null, rng)));
            SaveSystem.Save();
            SaveSystem.Load();
            Assert.Greater(SaveSystem.Data.meta.radioHeard.Count, 0);
            yield break;
        }

        [UnityTest]
        public IEnumerator A_run_opens_with_a_word_and_a_boss_gets_its_own()
        {
            Assert.IsNotNull(_radio, "the Run scene has no radio");
            Assert.AreEqual(RadioMoment.RunStart, _radio.LastMoment);
            Assert.IsTrue(_radio.Showing);
            Assert.That(RadioLines.PoolFor(RadioMoment.RunStart), Does.Contain(_radio.LastLine));

            // A plain next floor is only chatter, and chatter is off here.
            int said = _radio.SaidCount;
            _floors.BuildFloor(2);
            yield return null;
            Assert.AreEqual(said, _radio.SaidCount);

            // The Queen: the line about her in particular, then one for beating her.
            _floors.BuildFloor(10);
            yield return null;
            _player.Health.SetInvulnerable(600f);
            _player.Teleport(_floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            yield return WaitUntil(() => _floors.Boss != null, 2f, "the boss");
            yield return null;
            Assert.AreEqual(RadioMoment.Boss, _radio.LastMoment);
            Assert.That(_radio.LastLine, Does.StartWith("Her Majesty"));
            _floors.Boss.Health.Kill();
            yield return WaitUntil(() => !_floors.IsLocked, 2f, "the arena to open");
            Assert.AreEqual(RadioMoment.BossDown, _radio.LastMoment);

            // The text types itself out in full.
            yield return new WaitForSecondsRealtime(_radio.LastLine.Length / _radio.lettersPerSecond + 0.3f);
            var texts = _radio.GetComponentsInChildren<UnityEngine.UI.Text>();
            Assert.IsTrue(System.Array.Exists(texts, text => text.text == _radio.LastLine), "the line was not typed out in full");
        }

        [UnityTest]
        public IEnumerator Chatter_keeps_its_distance_and_the_setting_turns_it_down()
        {
            Radio.ChanceOverride = 1f;
            // A line is up (the run's opening one): chatter does not talk over it.
            Assert.IsFalse(_radio.Say(RadioMoment.Item));
            // Nor straight after it, even once it has gone.
            _radio.chatterGap = 0.2f;
            yield return WaitUntil(() => !_radio.Showing, 12f, "the opening line to go");
            Assert.IsTrue(_radio.Say(RadioMoment.Item));
            Assert.AreEqual(RadioMoment.Item, _radio.LastMoment);

            // Less: the big moments only.
            GameSettings.Radio = 1;
            yield return WaitUntil(() => !_radio.Showing, 12f, "the line to go");
            Assert.IsFalse(_radio.Say(RadioMoment.Floor));
            Assert.IsTrue(_radio.Say(RadioMoment.Boss));

            // Off: nothing at all.
            GameSettings.Radio = 0;
            int said = _radio.SaidCount;
            Assert.IsFalse(_radio.Say(RadioMoment.Boss));
            Assert.IsFalse(_radio.Say(RadioMoment.LowHealth));
            Assert.AreEqual(said, _radio.SaidCount);
        }

        [UnityTest]
        public IEnumerator Being_down_to_one_health_gets_a_remark()
        {
            Radio.ChanceOverride = 1f;
            _radio.chatterGap = 0f;
            yield return WaitUntil(() => !_radio.Showing, 12f, "the opening line to go");
            while (_player.Health.Current > 1)
            {
                _player.Health.TakeDamage(1, Vector3.forward);
                yield return WaitUntil(() => !_player.Health.Invulnerable, 5f, "the moment of safety after a hit to pass");
            }
            Assert.AreEqual(RadioMoment.LowHealth, _radio.LastMoment);
        }

        [UnityTest]
        public IEnumerator Falling_is_remarked_on_back_in_the_lab()
        {
            _player.Health.Kill();
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "Lab" && Radio.Instance != null && Radio.Instance.SaidCount > 0, 5f, "the lab and its radio");
            Assert.AreEqual(RadioMoment.Death, Radio.Instance.LastMoment);
            Assert.That(RadioLines.PoolFor(RadioMoment.Death), Does.Contain(Radio.Instance.LastLine));
        }

        [UnityTest]
        public IEnumerator A_new_player_is_introduced_in_the_lab()
        {
            SaveSystem.Wipe();
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(RadioMoment.Hello, Radio.Instance.LastMoment);
            Assert.That(Radio.Instance.LastLine, Does.Contain("Dr. Finch"));
        }
    }
}
