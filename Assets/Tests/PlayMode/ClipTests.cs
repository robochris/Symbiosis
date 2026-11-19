using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>
    /// Not a real test: a bot plays a short stretch of the game while every frame is saved as a picture, HUD and all,
    /// so the result can be joined into a video and watched without opening Unity.
    /// Only runs when asked for by name:  runtests.ps1 -Filter Capture_clip_fight
    /// Frames land in the system temp folder under shrunk-tests\clips\[name].
    /// </summary>
    public class ClipTests
    {
        const int FramesPerSecond = 24;
        const int Width = 960, Height = 540;

        PlayerController _player;
        FloorManager _floors;
        CameraFollow _follow;
        Camera _camera;
        RenderTexture _target;
        Texture2D _picture;
        string _folder;
        int _frame;
        float _strafe = 1f, _nextSwap;

        IEnumerator Begin(string clip, bool descent = false)
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            Radio.Forget();
            RunDirector.Descent = descent;
            GameLoop.SlowMotionOverride = true;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;

            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            _follow = CameraFollow.Instance;
            _camera = Camera.main;
            _player.Input.overrideFrame = new InputFrame();

            _folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "clips", clip);
            if (Directory.Exists(_folder)) foreach (string old in Directory.GetFiles(_folder, "*.jpg")) File.Delete(old);
            Directory.CreateDirectory(_folder);
            _frame = 0;

            // Headless Unity can't grab the real screen, so the HUD is drawn by the game camera for the length of the clip.
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _camera;
                canvas.planeDistance = 0.5f;
            }
            _target = new RenderTexture(Width, Height, 24);
            _picture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            Time.captureFramerate = FramesPerSecond;
        }

        void End()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            GameLoop.SlowMotionOverride = null;
            _target.Release();
            Object.Destroy(_target);
            Object.Destroy(_picture);
            Debug.Log($"[Shrunk] Saved {_frame} frames to {_folder}");
            SaveSystem.Wipe();
        }

        // One frame of the game, then its picture.
        IEnumerator Shoot()
        {
            yield return null;
            _camera.targetTexture = _target;
            _camera.Render();
            _camera.targetTexture = null;
            RenderTexture.active = _target;
            _picture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            _picture.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(_folder, $"f{_frame++:D5}.jpg"), _picture.EncodeToJPG(88));
        }

        // Turns the view toward a point the way a thumb would: quickly, but not instantly.
        void LookAt(Vector3 point, float degreesPerSecond)
        {
            Vector3 to = point - _player.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            float wanted = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(_follow.LookYaw, wanted);
            _follow.AddLook(Mathf.Clamp(delta, -degreesPerSecond * Time.deltaTime, degreesPerSecond * Time.deltaTime), 0f);
        }

        void Steer(Vector3 move, bool fire)
        {
            Vector3 forward = Quaternion.Euler(0f, _follow.LookYaw, 0f) * Vector3.forward;
            _player.Input.overrideFrame = new InputFrame { move = Vector3.ClampMagnitude(move, 1f), aim = forward, fire = fire, faceYaw = _follow.LookYaw };
        }

        IEnumerator Stand(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Steer(Vector3.zero, false);
                yield return Shoot();
            }
        }

        // Walks the navmesh path to a point, looking where it is going.
        IEnumerator Walk(Vector3 destination, float timeout, System.Func<bool> until)
        {
            var path = new NavMeshPath();
            for (float t = 0f; t < timeout && !until(); t += Time.deltaTime)
            {
                Vector3 position = _player.transform.position;
                Vector3 next = destination;
                if (NavMesh.CalculatePath(position, destination, NavMesh.AllAreas, path) && path.corners.Length > 1) next = path.corners[1];
                Vector3 direction = next - position;
                direction.y = 0f;
                if (direction.magnitude < 0.5f && next == destination) break;
                LookAt(next, 200f);
                Steer(direction.normalized, false);
                yield return Shoot();
            }
        }

        // Fights whatever is in the room: faces the nearest enemy, circles it, backs off when it gets close, pops pods on the way.
        IEnumerator Fight(float timeout)
        {
            for (float t = 0f; t < timeout && _floors.IsLocked; t += Time.deltaTime)
            {
                Vector3 position = _player.transform.position;
                Enemy nearest = null;
                float nearestDistance = float.MaxValue;
                foreach (var enemy in Enemy.Active)
                {
                    if (enemy.Health.IsDead || !enemy.Targetable || enemy is EggEnemy) continue;
                    float d = (enemy.transform.position - position).magnitude;
                    if (d < nearestDistance) { nearestDistance = d; nearest = enemy; }
                }
                if (nearest == null)
                {
                    Steer(Vector3.zero, false);
                    yield return Shoot();
                    continue;
                }

                if (Time.time >= _nextSwap)
                {
                    _nextSwap = Time.time + Random.Range(1.2f, 2.6f);
                    _strafe = -_strafe;
                }
                LookAt(nearest.transform.position, 260f);
                Quaternion look = Quaternion.Euler(0f, _follow.LookYaw, 0f);
                Vector3 move = look * Vector3.right * _strafe;
                if (nearestDistance < 6f) move += look * Vector3.back * 1.2f;
                else if (nearestDistance > 13f) move += look * Vector3.forward;

                // Stay in the room: lean back toward its middle when near the edge.
                Vector3 home = _floors.CurrentRoom.Centre - position;
                home.y = 0f;
                if (home.magnitude > 9f) move += home.normalized * 1.5f;

                Vector3 aim = look * Vector3.forward;
                Steer(move, _player.weapon.HasTarget(aim));
                yield return Shoot();
            }
        }

        RoomNode NearestFightRoom()
        {
            RoomNode best = null;
            foreach (var node in _floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                if (best == null || node.distance < best.distance) best = node;
            }
            return best;
        }

        static Vector3 OnNavMesh(Vector3 near) => NavMesh.SamplePosition(near, out var hit, 3f, NavMesh.AllAreas) ? hit.position : near;

        /// <summary>Start room, down the tunnel, into a fight room, the fight, and the quiet after: the Auto camera doing all of its moves.</summary>
        [UnityTest, Explicit]
        public IEnumerator Capture_clip_fight()
        {
            yield return Begin("fight");
            _floors.BuildFloor(6, 31337);
            yield return null;
            _player.Health.SetInvulnerable(600f);

            RoomNode fight = NearestFightRoom();
            Assert.IsNotNull(fight);
            Room room = _floors.RoomAt(fight.cell);
            Debug.Log($"[Shrunk] Clip room: {RoomDressing.NameOf(room.Shape.feature)}\n{room.Shape.ToAscii()}");

            yield return Stand(1f);
            yield return Walk(OnNavMesh(room.Centre), 25f, () => _floors.IsLocked);
            yield return Fight(40f);
            yield return Stand(2.5f);
            End();
        }

        /// <summary>The same walk and fight in the Birthday Cake, the second biome.</summary>
        [UnityTest, Explicit]
        public IEnumerator Capture_clip_cake()
        {
            yield return Begin("cake");
            Assert.IsTrue(_floors.SetBiome("cake"));
            _floors.BuildFloor(6, 4711);
            yield return null;
            _player.Health.SetInvulnerable(600f);

            RoomNode fight = NearestFightRoom();
            Room room = _floors.RoomAt(fight.cell);
            yield return Stand(1f);
            yield return Walk(OnNavMesh(room.Centre), 25f, () => _floors.IsLocked);
            yield return Fight(40f);
            yield return Stand(2f);
            End();
        }

        // A boss fight from the edge of its arena: walk in, fight for a while, finish it, and stay for the ending.
        IEnumerator BossClip(float fightSeconds)
        {
            _player.Health.SetInvulnerable(600f);
            Vector3 middle = _floors.ArenaCentre;
            _player.Teleport(OnNavMesh(middle + new Vector3(0f, 0f, -20f)) + Vector3.up * 0.05f);
            _follow.SetMode(CameraMode.Auto);
            LookAt(middle, 100000f);
            yield return Stand(0.8f);
            yield return Walk(OnNavMesh(middle + new Vector3(0f, 0f, -9f)), 12f, () => false);
            yield return Fight(fightSeconds);
            if (_floors.Boss != null && !_floors.Boss.Health.IsDead) _floors.Boss.Health.Kill();
            yield return Stand(3.5f);
            End();
        }

        /// <summary>The secret ending: Dr. Finch at the bottom, through both doses.</summary>
        [UnityTest, Explicit]
        public IEnumerator Capture_clip_finch()
        {
            yield return Begin("finch", true);
            yield return null;
            Assert.IsTrue(_floors.biome.isBottom);
            yield return BossClip(55f);
        }

        /// <summary>Under the Sofa's boss, the Robot Vacuum.</summary>
        [UnityTest, Explicit]
        public IEnumerator Capture_clip_vacuum()
        {
            yield return Begin("vacuum");
            Assert.IsTrue(_floors.SetBiome("sofa"));
            _floors.BuildFloor(10, 2024);
            yield return null;
            yield return BossClip(50f);
        }

        /// <summary>Under the Sofa's mini boss, Grandmother Dust.</summary>
        [UnityTest, Explicit]
        public IEnumerator Capture_clip_granny()
        {
            yield return Begin("granny");
            Assert.IsTrue(_floors.SetBiome("sofa"));
            _floors.BuildFloor(5, 2025);
            yield return null;
            yield return BossClip(30f);
        }

        /// <summary>The Birthday Cake's mini boss, the Jelly Mother.</summary>
        [UnityTest, Explicit]
        public IEnumerator Capture_clip_jelly()
        {
            yield return Begin("jelly");
            Assert.IsTrue(_floors.SetBiome("cake"));
            _floors.BuildFloor(5, 2026);
            yield return null;
            yield return BossClip(35f);
        }

        /// <summary>The Brood Mother on floor 5, for the boss view.</summary>
        [UnityTest, Explicit]
        public IEnumerator Capture_clip_boss()
        {
            yield return Begin("boss");
            _floors.BuildFloor(5, 777);
            yield return null;
            _player.Health.SetInvulnerable(600f);

            // Start at the edge of the arena, looking in.
            Vector3 middle = _floors.ArenaCentre;
            _player.Teleport(OnNavMesh(middle + new Vector3(0f, 0f, -20f)) + Vector3.up * 0.05f);
            _follow.SetMode(CameraMode.Auto);
            LookAt(middle, 100000f);
            yield return Stand(0.8f);
            yield return Walk(OnNavMesh(middle + new Vector3(0f, 0f, -9f)), 12f, () => false);
            yield return Fight(22f);
            if (_floors.Boss != null && !_floors.Boss.Health.IsDead) _floors.Boss.Health.Kill();
            yield return Stand(2.5f);
            End();
        }
    }
}
