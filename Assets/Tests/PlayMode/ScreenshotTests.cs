using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>
    /// Not a real test: saves pictures of the game from each camera view, for looking at without opening Unity.
    /// Only runs when asked for by name:  runtests.ps1 -Filter Capture_camera_views
    /// Pictures land in the system temp folder under shrunk-tests\shots. The HUD is not included.
    /// </summary>
    public class ScreenshotTests
    {
        [UnityTest, Explicit]
        public IEnumerator Capture_camera_views()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;

            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            var follow = CameraFollow.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);

            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            // Start room, each view.
            foreach (CameraMode mode in new[] { CameraMode.Shoulder, CameraMode.FirstPerson, CameraMode.TopDown })
            {
                follow.SetMode(mode);
                yield return new WaitForSeconds(0.3f);
                Save(Path.Combine(folder, "start-" + mode + ".png"));
            }

            // A fight room with ants in it, each view.
            RoomNode fight = null;
            foreach (var node in floors.Layout.ordered)
            {
                if (node.type == RoomType.Normal) { fight = node; break; }
            }
            Assert.IsNotNull(fight);
            Room room = floors.RoomAt(fight.cell);
            player.Teleport(room.Centre + Vector3.up * 0.05f);
            yield return new WaitForSeconds(1.2f);

            foreach (CameraMode mode in new[] { CameraMode.Shoulder, CameraMode.FirstPerson, CameraMode.TopDown })
            {
                // Turn to face the nearest ant so the picture shows one.
                Enemy nearest = null;
                foreach (var enemy in Enemy.Active)
                {
                    if (nearest == null || (enemy.transform.position - player.transform.position).sqrMagnitude < (nearest.transform.position - player.transform.position).sqrMagnitude) nearest = enemy;
                }
                if (nearest != null)
                {
                    Vector3 to = nearest.transform.position - player.transform.position;
                    to.y = 0f;
                    player.transform.rotation = Quaternion.LookRotation(to);
                }
                follow.SetMode(mode);
                yield return null;
                yield return null;
                Save(Path.Combine(folder, "fight-" + mode + ".png"));
            }
        }

        [UnityTest, Explicit]
        public IEnumerator Capture_enemy_lineup()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;

            var player = PlayerController.Instance;
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            var follow = CameraFollow.Instance;
            var biome = spawner.biome;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            spawner.maxAlive = 0;
            foreach (var enemy in new System.Collections.Generic.List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            yield return null;

            // Back row: the four ants, plain. Front rows: a worker with each mutation.
            string[] ants = { "WorkerAnt", "SoldierAnt", "SpitterAnt", "WingedAnt" };
            for (int i = 0; i < ants.Length; i++)
            {
                Freeze(spawner.SpawnAt(biome.EnemyNamed(ants[i]), new Vector3(-6f + i * 4f, 0f, 10f), null));
            }
            for (int i = 0; i < biome.mutations.Length; i++)
            {
                var position = new Vector3(-6f + (i % 4) * 4f, 0f, 6f - (i / 4) * 3.5f);
                Freeze(spawner.SpawnAt(biome.EnemyNamed("WorkerAnt"), position, new[] { biome.mutations[i] }));
            }
            // And one carrying three at once.
            Freeze(spawner.SpawnAt(biome.EnemyNamed("SoldierAnt"), new Vector3(0f, 0f, -1.5f),
                new[] { biome.Mutation(MutationType.HardenedShell), biome.Mutation(MutationType.Fast), biome.Mutation(MutationType.ExplodesOnDeath) }));

            player.Teleport(new Vector3(0f, 0.05f, -7f));
            player.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(0.5f);

            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            follow.SetMode(CameraMode.Shoulder);
            yield return null;
            yield return null;
            Save(Path.Combine(folder, "lineup-Shoulder.png"));

            follow.SetMode(CameraMode.TopDown);
            follow.target = null;
            follow.enabled = false;
            Camera.main.transform.SetPositionAndRotation(new Vector3(0f, 13f, -6f), Quaternion.Euler(55f, 0f, 0f));
            yield return null;
            Save(Path.Combine(folder, "lineup-Above.png"));
        }

        // Stops an ant thinking so it stands still for its picture; its mutation tells keep animating.
        static void Freeze(Enemy enemy)
        {
            enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            enemy.enabled = false;
        }

        static void Save(string path)
        {
            var cam = Camera.main;
            var target = new RenderTexture(960, 540, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = target;
            cam.Render();
            cam.targetTexture = previous;

            RenderTexture.active = target;
            var picture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            picture.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(path, picture.EncodeToPNG());
            Object.Destroy(picture);
            target.Release();
            Object.Destroy(target);
            Debug.Log("[Shrunk] Saved " + path);
        }
    }
}
