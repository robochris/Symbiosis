using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The nurse ant: it mends the hurt ones round it, and hurries nobody.</summary>
    public class NurseTests
    {
        [UnityTest]
        public IEnumerator The_nurse_mends_a_hurt_friend_and_does_not_hurry_anyone()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            spawner.maxAlive = 0;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            yield return null;

            var nurse = (SupportEnemy)spawner.SpawnAt(spawner.biome.EnemyNamed("NurseAnt"), new Vector3(0f, 0f, 14f), null);
            var patient = spawner.SpawnAt(spawner.biome.EnemyNamed("SoldierAnt"), new Vector3(2f, 0f, 14f), null);
            patient.frozen = true;
            nurse.frozen = false;
            patient.Health.TakeDamage(6, Vector3.forward);
            int hurt = patient.Health.Current;
            Time.timeScale = 2f;
            for (float t = 0f; t < 8f && nurse.Healed == 0; t += Time.unscaledDeltaTime) yield return null;
            Time.timeScale = 1f;
            Assert.Greater(nurse.Healed, 0, "the nurse never mended anybody");
            Assert.Greater(patient.Health.Current, hurt);
            Assert.IsFalse(patient.IsBuffed, "the nurse hurried its patient along");
        }
    }
}
