using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Milestone 7: the pause menu and its options, placeholder sound, and the off-screen enemy markers.</summary>
    public class DemoPassTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
        }

        [TearDown]
        public void CleanUp()
        {
            GameSettings.Reset();
            SaveSystem.Wipe();
            Time.timeScale = 1f;
        }

        static PlayerController Player => PlayerController.Instance;

        IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            Assert.IsNotNull(Player);
            Player.Input.overrideFrame = new InputFrame();
            Assert.IsNotNull(PauseMenu.Instance, scene + " has no pause menu");
        }

        static void UseRow(string label)
        {
            int row = PauseMenu.Instance.RowNamed(label);
            Assert.GreaterOrEqual(row, 0, "no menu row called " + label);
            PauseMenu.Instance.Use(row);
        }

        static string Row(string label) => PauseMenu.Instance.RowText(PauseMenu.Instance.RowNamed(label));

        [UnityTest]
        public IEnumerator Pausing_stops_the_game_and_blocks_input()
        {
            yield return Load("Arena");
            Assert.IsFalse(PauseMenu.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);

            PauseMenu.Instance.Open();
            Assert.IsTrue(PauseMenu.IsOpen);
            Assert.AreEqual(0f, Time.timeScale);

            // With the menu up, real input is ignored: the reader hands back an empty frame.
            Player.Input.overrideFrame = null;
            InputFrame frame = Player.Input.Read();
            Assert.AreEqual(Vector3.zero, frame.move);
            Assert.IsFalse(frame.fire || frame.dodge || frame.power);
            Player.Input.overrideFrame = new InputFrame();

            Vector3 before = Player.transform.position;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(before, Player.transform.position);

            UseRow("Resume");
            Assert.IsFalse(PauseMenu.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Options_change_the_game_straight_away()
        {
            yield return Load("Arena");
            var follow = CameraFollow.Instance;
            var touch = Object.FindAnyObjectByType<TouchControls>();
            PauseMenu.Instance.Open();

            Assert.AreEqual(CameraMode.Shoulder, follow.mode);
            UseRow("Camera");
            Assert.AreEqual(CameraMode.Wide, follow.mode);
            Assert.That(Row("Camera"), Does.Contain("Wide"));
            UseRow("Camera");
            Assert.AreEqual(CameraMode.FirstPerson, follow.mode);
            Assert.That(Row("Camera"), Does.Contain("First person"));
            UseRow("Camera");
            Assert.AreEqual(CameraMode.TopDown, follow.mode);
            UseRow("Camera");
            Assert.AreEqual(CameraMode.Shoulder, follow.mode, "the camera row should come back round to Shoulder");
            GameSettings.Camera = CameraMode.TopDown;

            Assert.AreEqual(1f, Player.weapon.aimAssistStrength);
            Assert.That(Row("Aim assist"), Does.Contain("Full"));
            UseRow("Aim assist");
            Assert.AreEqual(0f, Player.weapon.aimAssistStrength, "Full should wrap round to Off");
            Assert.That(Row("Aim assist"), Does.Contain("Off"));
            UseRow("Aim assist");
            Assert.AreEqual(0.35f, Player.weapon.aimAssistStrength, 0.001f);

            Assert.IsTrue(follow.lockVertical);
            UseRow("Looking up and down");
            Assert.IsFalse(follow.lockVertical);

            Assert.IsFalse(touch.leftHanded);
            UseRow("Touch controls");
            Assert.IsTrue(touch.leftHanded);

            UseRow("Look speed");
            Assert.AreEqual(1.5f, GameSettings.LookSpeed, 0.001f);

            UseRow("Sound");
            Assert.AreEqual(0f, AudioListener.volume, "Full should wrap round to Off");
            UseRow("Sound");
            Assert.AreEqual(0.5f, AudioListener.volume, 0.001f);
        }

        [UnityTest]
        public IEnumerator Options_are_remembered_in_the_next_scene()
        {
            yield return Load("Arena");
            PauseMenu.Instance.Open();
            UseRow("Camera");
            UseRow("Camera");
            UseRow("Camera");
            UseRow("Touch controls");
            UseRow("Resume");

            yield return Load("Lab");
            Assert.AreEqual(CameraMode.TopDown, CameraFollow.Instance.mode);
            Assert.IsTrue(Object.FindAnyObjectByType<TouchControls>().leftHanded);
            Assert.GreaterOrEqual(PauseMenu.Instance.RowNamed("Quit game"), 0, "the lab's menu should offer quitting");
            Assert.AreEqual(-1, PauseMenu.Instance.RowNamed("Save and go to the lab"));
        }

        [UnityTest]
        public IEnumerator Leaving_a_run_from_the_menu_keeps_it_saved()
        {
            yield return Load("Run");
            var floors = FloorManager.Instance;
            floors.BuildFloor(4);
            yield return null;

            PauseMenu.Instance.Open();
            UseRow("Save and go to the lab");
            yield return new WaitForSecondsRealtime(0.1f);
            yield return null;
            yield return null;

            Assert.AreEqual("Lab", SceneManager.GetActiveScene().name);
            Assert.AreEqual(1f, Time.timeScale, "the lab loaded frozen");
            Assert.IsTrue(SaveSystem.Data.run.active, "the run was thrown away");
            Assert.AreEqual(4, SaveSystem.Data.run.floorNumber);
            Assert.AreEqual(0, SaveSystem.Data.meta.deaths, "leaving counted as a death");
        }

        [UnityTest]
        public IEnumerator Shooting_and_killing_make_sounds()
        {
            yield return Load("Arena");
            int before = Sfx.Played;
            Player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            yield return new WaitForSeconds(0.5f);
            Player.Input.overrideFrame = new InputFrame();
            Assert.Greater(Sfx.Played, before + 1, "no shooting sounds");

            before = Sfx.Played;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) enemy.Health.Kill();
            yield return null;
            Assert.Greater(Sfx.Played, before, "no death sound");

            var sources = Object.FindAnyObjectByType<Sfx>().GetComponents<AudioSource>();
            bool hasClip = false;
            foreach (var source in sources) hasClip |= source.clip != null && source.clip.length > 0.01f;
            Assert.IsTrue(hasClip, "sounds were played without any audio in them");
        }

        [UnityTest]
        public IEnumerator Markers_point_at_enemies_behind_the_player()
        {
            yield return Load("Arena");
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            var arrows = Object.FindAnyObjectByType<EnemyArrows>();
            Assert.IsNotNull(arrows);
            spawner.maxAlive = 0;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            Player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            yield return null;
            yield return null;
            Assert.AreEqual(0, arrows.Showing);

            // In front of the player and in view: no marker.
            var front = spawner.SpawnAt(spawner.biome.EnemyNamed("WorkerAnt"), new Vector3(0f, 0f, 7f), null);
            front.frozen = true;
            yield return null;
            yield return null;
            Assert.AreEqual(0, arrows.Showing, "marker shown for an enemy in plain view");

            // Behind the player, out of view: one marker, at the bottom of the screen.
            var behind = spawner.SpawnAt(spawner.biome.EnemyNamed("WorkerAnt"), new Vector3(0f, 0f, -9f), null);
            behind.frozen = true;
            yield return null;
            yield return null;
            Assert.AreEqual(1, arrows.Showing, "no marker for the enemy behind");

            RectTransform marker = null;
            foreach (Transform child in arrows.transform)
            {
                if (child.name == "Enemy Arrow" && child.gameObject.activeSelf) marker = (RectTransform)child;
            }
            Assert.IsNotNull(marker);
            Assert.Less(marker.anchoredPosition.y, -100f, "the marker for an enemy behind should sit at the bottom edge");
        }
    }
}
