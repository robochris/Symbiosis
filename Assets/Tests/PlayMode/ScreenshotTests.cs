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
