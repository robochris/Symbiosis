using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>The antlion: shot only while it is up, and it comes up where it marked.</summary>
    public class BurrowerTests
    {
        [UnityTest]
        public IEnumerator The_antlion_sinks_marks_a_circle_under_the_player_and_comes_up_there()
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

            var antlion = (BurrowerEnemy)spawner.SpawnAt(spawner.biome.EnemyNamed("Antlion"), new Vector3(0f, 0f, 8f), null);
            Assert.IsNotNull(antlion, "no antlion");
            Time.timeScale = 2f;
            for (float t = 0f; t < 6f && antlion.Current != BurrowerEnemy.State.Under; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(BurrowerEnemy.State.Under, antlion.Current, "it never went under");
            Assert.IsFalse(antlion.Targetable, "it can be aimed at underground");
            Assert.IsFalse(antlion.Health.TakeDamage(3, Vector3.forward), "it can be hurt underground");

            for (float t = 0f; t < 6f && antlion.Rises == 0; t += Time.unscaledDeltaTime) yield return null;
            Time.timeScale = 1f;
            Assert.AreEqual(1, antlion.Rises);
            Assert.Less(Vector3.Distance(antlion.transform.position, player.transform.position), 2.5f, "it came up somewhere else");
            Assert.IsTrue(antlion.Targetable);
            Assert.IsTrue(antlion.Health.TakeDamage(1, Vector3.forward), "it cannot be hurt once it is up");
        }
    }
}
