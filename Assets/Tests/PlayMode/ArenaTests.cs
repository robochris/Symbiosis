using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>Plays the Arena scene with scripted input to check the Milestone 1 basics still work.</summary>
    public class ArenaTests
    {
        PlayerController _player;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            Assert.IsNotNull(_player, "Arena scene has no player");
            _player.Input.overrideFrame = new InputFrame();
        }

        [UnityTest]
        public IEnumerator The_Void_swings_its_legs_when_walking_and_its_arm_kicks_when_shooting()
        {
            var body = _player.GetComponent<VoidBody>();
            Assert.IsNotNull(body, "the player has no body animation");
            Assert.IsNotNull(body.legLeft);
            Assert.IsNotNull(body.armRight);
            _player.Health.SetInvulnerable(600f);

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(0f, body.Stride01, 0.05f, "legs moving while standing still");
            Quaternion standing = body.legLeft.localRotation;

            _player.Input.overrideFrame = new InputFrame { move = Vector3.right };
            float most = 0f;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                most = Mathf.Max(most, Quaternion.Angle(standing, body.legLeft.localRotation));
                yield return null;
            }
            Assert.Greater(body.Stride01, 0.8f, "not striding at full speed");
            Assert.Greater(most, 20f, "the legs barely moved");
            Assert.Greater(Quaternion.Angle(body.legLeft.localRotation, body.legRight.localRotation), 0.5f, "both legs swing the same way");

            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, body.Stride01, 0.05f, "legs still going after stopping");
            Assert.Less(Quaternion.Angle(standing, body.legLeft.localRotation), 1f, "legs did not come back to rest");

            Assert.AreEqual(0f, body.Kick, 0.01f);
            _player.Input.overrideFrame = new InputFrame { aim = Vector3.forward, fire = true };
            float kick = 0f;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                kick = Mathf.Max(kick, body.Kick);
                yield return null;
            }
            _player.Input.overrideFrame = new InputFrame();
            Assert.Greater(kick, 0.5f, "the arm did not kick on a shot");

            // Every part of the body is inside the space the collider covers, so items made for it still fit.
            foreach (var part in _player.visual.GetComponentsInChildren<Renderer>())
            {
                Vector3 local = _player.transform.InverseTransformPoint(part.bounds.center);
                Assert.Less(Mathf.Abs(local.x), 0.6f, part.name + " sticks out sideways");
                Assert.That(local.y, Is.InRange(-0.05f, 1.9f), part.name + " is outside the body's height");
            }
        }

        static Enemy FirstEnemy()
        {
            foreach (var enemy in Enemy.Active)
            {
                if (!enemy.Health.IsDead) return enemy;
            }
            return null;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        [UnityTest]
        public IEnumerator Player_moves_and_is_stopped_by_walls()
        {
            _player.Input.overrideFrame = new InputFrame { move = Vector3.right };
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(_player.transform.position.x, 2f, "player did not move right");

            yield return new WaitForSeconds(4f);
            Assert.Less(_player.transform.position.x, 15f, "player walked through the wall");
            Assert.Greater(_player.transform.position.y, -0.5f, "player fell through the floor");
        }

        [UnityTest]
        public IEnumerator Ant_chases_the_player()
        {
            var ant = FirstEnemy();
            Assert.IsNotNull(ant, "no ant spawned");
            float before = Flat(ant.transform.position - _player.transform.position).magnitude;
            yield return new WaitForSeconds(1f);
            float after = Flat(ant.transform.position - _player.transform.position).magnitude;
            Assert.Less(after, before - 2f, "ant did not close the distance");
        }

        [UnityTest]
        public IEnumerator Ant_bites_a_player_who_stands_still()
        {
            int max = _player.Health.maxHealth;
            for (float t = 0f; t < 8f && _player.Health.Current == max; t += Time.deltaTime) yield return null;
            Assert.Less(_player.Health.Current, max, "ant never landed a bite");
        }

        [UnityTest]
        public IEnumerator Shooting_kills_the_ant_and_another_arrives()
        {
            for (float t = 0f; t < 6f && GameLoop.Kills == 0; t += Time.deltaTime)
            {
                var ant = FirstEnemy();
                var aim = ant != null ? Flat(ant.transform.position - _player.transform.position).normalized : Vector3.forward;
                _player.Input.overrideFrame = new InputFrame { aim = aim, fire = true };
                yield return null;
            }
            Assert.GreaterOrEqual(GameLoop.Kills, 1, "the ant survived 6 seconds of fire");
            Assert.Greater(_player.weapon.ShotsFired, 0);

            _player.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(2f);
            Assert.IsNotNull(FirstEnemy(), "no replacement ant arrived");
        }

        [UnityTest]
        public IEnumerator Aim_assist_bends_a_near_miss_onto_the_ant()
        {
            var ant = FirstEnemy();
            Assert.IsNotNull(ant);
            int before = ant.Health.Current;

            // Aim 12 degrees off target: inside the 20 degree assist cone, but a clear miss without assist.
            for (float t = 0f; t < 1.5f && ant.Health.Current == before; t += Time.deltaTime)
            {
                var exact = Flat(ant.transform.position - _player.transform.position).normalized;
                _player.Input.overrideFrame = new InputFrame { aim = Quaternion.Euler(0f, 12f, 0f) * exact, fire = true };
                yield return null;
            }
            Assert.Less(ant.Health.Current, before, "off-target shots were not pulled onto the ant");
        }

        [UnityTest]
        public IEnumerator Dodge_moves_fast_blocks_damage_and_has_a_cooldown()
        {
            Vector3 start = _player.transform.position;
            _player.Input.overrideFrame = new InputFrame { move = Vector3.back, dodge = true };
            yield return null;
            _player.Input.overrideFrame = new InputFrame();

            Assert.IsTrue(_player.IsDodging, "dodge did not start");
            Assert.IsFalse(_player.Health.TakeDamage(1, Vector3.forward), "took damage during the roll");
            Assert.Less(_player.DodgeReady01, 0.5f, "cooldown did not start");

            yield return new WaitForSeconds(0.4f);
            float travelled = Flat(_player.transform.position - start).magnitude;
            Assert.Greater(travelled, 2.5f, "roll was too short");
            Assert.IsFalse(_player.IsDodging);

            yield return new WaitForSeconds(1f);
            Assert.AreEqual(1f, _player.DodgeReady01, 0.001f, "dodge never came back");
        }
    }
}
