using System.Collections;
using System.Collections.Generic;
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
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
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
            foreach (CameraMode mode in CameraFollow.Order)
            {
                follow.SetMode(mode);
                yield return new WaitForSeconds(0.3f);
                Save(Path.Combine(folder, "start-" + mode + ".png"));
            }

            // A fight room with ants in it, each view.
            // The biggest fight room on a deep floor, so the pictures show how much room there is.
            floors.BuildFloor(8, 4242);
            yield return null;
            RoomNode fight = null;
            foreach (var node in floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                if (fight == null || floors.RoomAt(node.cell).Shape.interior.Count > floors.RoomAt(fight.cell).Shape.interior.Count) fight = node;
            }
            Assert.IsNotNull(fight);
            Room room = floors.RoomAt(fight.cell);
            player.Teleport(room.Centre + Vector3.up * 0.05f);
            yield return new WaitForSeconds(1.2f);

            foreach (CameraMode mode in CameraFollow.Order)
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

        // One picture of each kind of fight room, from the wide view, looking at its cover.
        [UnityTest, Explicit]
        public IEnumerator Capture_room_features()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;

            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            var follow = CameraFollow.Instance;
            player.Input.overrideFrame = new InputFrame();
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            foreach (RoomFeature feature in System.Enum.GetValues(typeof(RoomFeature)))
            {
                RoomShape.ForceFeature = feature;
                floors.BuildFloor(7, 9001);
                RoomShape.ForceFeature = null;
                yield return null;
                player.Health.SetInvulnerable(600f);

                Room room = null;
                foreach (var node in floors.Layout.ordered)
                {
                    Room candidate = floors.RoomAt(node.cell);
                    if (node.type != RoomType.Normal || candidate.Shape.feature != feature) continue;
                    if (room == null || candidate.Shape.cover.Count + candidate.Shape.spots.Count > room.Shape.cover.Count + room.Shape.spots.Count) room = candidate;
                }
                // Bends only happen where the dice put them, so a floor may have none.
                if (room == null) continue;

                // Look at whatever makes the room what it is: the nearest dew spot, or the nearest cover.
                var tiles = feature == RoomFeature.DewRoom ? room.Shape.spots : room.Shape.cover.Count > 0 ? room.Shape.cover : room.Shape.glows;
                Vector3 look = room.TileWorld(tiles[0]);
                foreach (var tile in tiles)
                {
                    if ((room.TileWorld(tile) - room.Centre).sqrMagnitude < (look - room.Centre).sqrMagnitude) look = room.TileWorld(tile);
                }
                Vector3 to = look - room.Centre;
                player.Teleport(room.Centre - to.normalized * 2f + Vector3.up * 0.05f);
                player.transform.rotation = Quaternion.LookRotation(to);
                foreach (var drip in room.GetComponentsInChildren<DewDrip>()) drip.interval = 1.6f;
                follow.SetMode(CameraMode.Wide);
                yield return new WaitForSeconds(1.3f);
                follow.SetMode(CameraMode.Wide);
                yield return null;
                Save(Path.Combine(folder, "feature-" + feature + ".png"));
            }
        }

        // The Void in each character style, from the front.
        [UnityTest, Explicit]
        public IEnumerator Capture_styles()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            var follow = CameraFollow.Instance;
            follow.enabled = false;
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            var style = player.GetComponent<PlayerStyle>();
            Vector3 at = player.transform.position;
            Camera.main.transform.SetPositionAndRotation(at + player.transform.forward * 3.4f + player.transform.right * 1.2f + Vector3.up * 1.5f,
                Quaternion.LookRotation(-player.transform.forward * 3.4f - player.transform.right * 1.2f - Vector3.up * 0.5f));
            for (int i = 0; i < style.styles.Length; i++)
            {
                style.Apply(i);
                yield return null;
                Save(Path.Combine(folder, "style-" + i + "-" + style.styles[i].displayName + ".png"));
            }
            SaveSystem.Wipe();
        }

        // The title, the arrival and floor cards, a boss card and the end-of-biome summary.
        [UnityTest, Explicit]
        public IEnumerator Capture_cards()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            ScreenCards.ShowInTests = true;
            ScreenCards.TitleSeen = false;
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            PlayerController.Instance.Input.overrideFrame = new InputFrame();
            yield return new WaitForSeconds(0.5f);
            yield return Screen(Path.Combine(folder, "card-title.png"));
            ScreenCards.Instance.DismissTitle();

            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            yield return new WaitForSeconds(0.4f);
            yield return Screen(Path.Combine(folder, "card-biome.png"));

            floors.BuildFloor(4, 4242);
            yield return new WaitForSeconds(0.5f);
            yield return Screen(Path.Combine(folder, "card-floor.png"));

            floors.BuildFloor(10, 99);
            yield return null;
            player.Health.SetInvulnerable(600f);
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -11f));
            player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.SetMode(CameraMode.Auto);
            for (float t = 0f; t < 3f && floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Screen(Path.Combine(folder, "card-boss.png"));
            yield return new WaitForSecondsRealtime(2.5f);

            floors.Boss.Health.Kill();
            for (float t = 0f; t < 3f && floors.ExtractPortal == null; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.6f);
            yield return Screen(Path.Combine(folder, "card-summary.png"));
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Screen(Path.Combine(folder, "card-radio.png"));

            ScreenCards.ShowInTests = false;
            SaveSystem.Wipe();
        }

        [UnityTest, Explicit]
        public IEnumerator Capture_sofa()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            GameSettings.Radio = 0;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);
            Assert.IsTrue(floors.SetBiome("sofa"));

            // A fight room.
            floors.BuildFloor(8, 4242);
            yield return null;
            RoomNode fight = null;
            foreach (var node in floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                if (fight == null || floors.RoomAt(node.cell).Shape.interior.Count > floors.RoomAt(fight.cell).Shape.interior.Count) fight = node;
            }
            player.Teleport(floors.RoomAt(fight.cell).Centre + Vector3.up * 0.05f);
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            CameraFollow.Instance.Snap();
            for (float t = 0f; t < 6f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSeconds(1.6f);
            if (floors.LiveEnemies.Count > 0)
            {
                player.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(floors.LiveEnemies[0].transform.position - player.transform.position, Vector3.up));
                CameraFollow.Instance.Snap();
                yield return null;
            }
            yield return Screen(Path.Combine(folder, "sofa-room.png"));

            // The two bosses.
            foreach (int number in new[] { 5, 10 })
            {
                floors.BuildFloor(number);
                yield return null;
                player.Health.SetInvulnerable(600f);
                player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
                player.transform.rotation = Quaternion.identity;
                CameraFollow.Instance.Snap();
                for (float t = 0f; t < 6f && floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
                for (float t = 0f; t < 8f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForSeconds(1.5f);
                player.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(floors.Boss.transform.position - player.transform.position, Vector3.up));
                CameraFollow.Instance.Snap();
                yield return null;
                yield return Screen(Path.Combine(folder, "sofa-boss-" + number + ".png"));

                // And each one's signature moment: the vacuum with its bin open, Grandmother Dust mid-hop.
                var bossNow = floors.Boss;
                System.Func<bool> moment = () => bossNow is RobotVacuum vacuum ? vacuum.BinOpen
                    : bossNow is GrandmotherDust granny && granny.Current == GrandmotherDust.State.Air;
                Time.timeScale = 3f;
                for (float t = 0f; t < 40f && !moment(); t += Time.deltaTime) yield return null;
                Time.timeScale = 1f;
                yield return new WaitForSeconds(bossNow is RobotVacuum ? 0.3f : 0.3f);
                player.Teleport(bossNow.transform.position + new Vector3(0f, 0.05f, -10f));
                player.transform.rotation = Quaternion.identity;
                CameraFollow.Instance.Snap();
                yield return null;
                yield return Screen(Path.Combine(folder, "sofa-boss-" + number + "-moment.png"));
                floors.Boss.Health.Kill();
                yield return new WaitForSeconds(0.5f);
            }
            GameSettings.Reset();
            SaveSystem.Wipe();
        }

        [UnityTest, Explicit]
        public IEnumerator Capture_portals()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            GameSettings.Radio = 0;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            floors.BuildFloor(10);
            yield return null;
            player.Teleport(floors.ArenaCentre + Vector3.up * 0.05f);
            for (float t = 0f; t < 6f && floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0f; t < 8f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;
            RunState.AddSamples(23);
            floors.Boss.Health.Kill();
            for (float t = 0f; t < 8f && floors.ExtractPortal == null; t += Time.unscaledDeltaTime) yield return null;
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            CameraFollow.Instance.Snap();
            yield return new WaitForSecondsRealtime(13f); // the summary goes away
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);
            yield return Screen(Path.Combine(folder, "portals.png"));
            GameSettings.Reset();
            SaveSystem.Wipe();
        }

        [UnityTest, Explicit]
        public IEnumerator Capture_finch()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            SaveSystem.Data.meta.runsStarted = 3;
            foreach (var note in FinchNotes.All) FinchNotes.Collect(note);
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            // The door, in the lab.
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            var door = Object.FindAnyObjectByType<HiddenDoor>();
            player.Teleport(door.transform.position + new Vector3(3.2f, 0.05f, 0f));
            player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            CameraFollow.Instance.Snap();
            yield return new WaitForSeconds(0.6f);
            yield return Screen(Path.Combine(folder, "door.png"));

            // The bottom, and Finch.
            RunDirector.Descent = true;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            yield return null;
            player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            player.Teleport(floors.ArenaCentre + new Vector3(0f, 0.05f, -5f));
            player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.Snap();
            for (float t = 0f; t < 6f && floors.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(floors.Boss);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Screen(Path.Combine(folder, "finch-card.png"));
            for (float t = 0f; t < 8f && Time.timeScale <= 0f; t += Time.unscaledDeltaTime) yield return null;
            var finch = (FinchBoss)floors.Boss;
            for (float t = 0f; t < 8f && finch.Bursts == 0; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.25f);
            player.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(finch.transform.position - player.transform.position, Vector3.up));
            CameraFollow.Instance.Snap();
            yield return null;
            yield return Screen(Path.Combine(folder, "finch-fight.png"));

            finch.Health.TakeDamage(Mathf.CeilToInt(finch.Health.maxHealth * 0.7f), Vector3.forward);
            for (float t = 0f; t < 12f && finch.FlasksThrown == 0; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.4f);
            player.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(finch.transform.position - player.transform.position, Vector3.up));
            CameraFollow.Instance.Snap();
            yield return null;
            yield return Screen(Path.Combine(folder, "finch-small.png"));
            SaveSystem.Wipe();
        }

        [UnityTest, Explicit]
        public IEnumerator Capture_evidence()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            SaveSystem.Data.meta.runsStarted = 3;
            foreach (int id in new[] { 1, 2, 4 }) FinchNotes.Collect(FinchNotes.Get(id));
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Input.overrideFrame = new InputFrame();
            var board = Object.FindAnyObjectByType<EvidenceBoard>();
            player.Teleport(board.transform.position + new Vector3(-1.5f, 0.05f, 0f));
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            CameraFollow.Instance.Snap();
            yield return new WaitForSeconds(0.6f);
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);
            yield return Screen(Path.Combine(folder, "evidence.png"));
            SaveSystem.Wipe();
        }

        [UnityTest, Explicit]
        public IEnumerator Capture_arrows()
        {
            GameSettings.Reset();
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            spawner.maxAlive = 0;
            foreach (var enemy in new List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            yield return null;
            foreach (var at in new[] { new Vector3(-7f, 0f, -3f), new Vector3(6f, 0f, -5f), new Vector3(0.5f, 0f, -7f), new Vector3(2f, 0f, 7f) })
            {
                spawner.SpawnAt(spawner.biome.EnemyNamed("WorkerAnt"), at, null).frozen = true;
            }
            yield return new WaitForSeconds(0.4f);
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);
            yield return Screen(Path.Combine(folder, "arrows.png"));
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

        [UnityTest, Explicit]
        public IEnumerator Capture_item_looks()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;

            var player = PlayerController.Instance;
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            var follow = CameraFollow.Instance;
            player.Input.overrideFrame = new InputFrame();
            spawner.maxAlive = 0;
            foreach (var enemy in new System.Collections.Generic.List<Enemy>(Enemy.Active)) Object.Destroy(enemy.gameObject);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);
            follow.SetMode(CameraMode.TopDown);
            follow.target = null;
            follow.enabled = false;

            // One of each slot, then the rest, so every look gets seen across the two pictures.
            var library = GameAssets.Instance.items;
            ItemType[][] sets =
            {
                new[] { ItemType.NeedleTip, ItemType.SplitterMandible, ItemType.CompoundEye, ItemType.WingBuds, ItemType.ChitinPlate, ItemType.PheromoneCloud, ItemType.LarvaBuddy },
                new[] { ItemType.AcidGland, ItemType.FireflyLantern, ItemType.SugarCube, ItemType.QueensJelly },
                new[] { ItemType.ExtraLegs },
            };
            for (int i = 0; i < sets.Length; i++)
            {
                foreach (var type in sets[i]) player.Items.Add(library.Get(type));
                yield return new WaitForSeconds(0.3f);
                Camera.main.transform.SetPositionAndRotation(new Vector3(1.6f, 1.6f, -3.2f), Quaternion.Euler(12f, -27f, 0f));
                yield return null;
                Save(Path.Combine(folder, "items-front-" + (i + 1) + ".png"));
                Camera.main.transform.SetPositionAndRotation(new Vector3(-1.8f, 1.8f, 3f), Quaternion.Euler(15f, 150f, 0f));
                yield return null;
                Save(Path.Combine(folder, "items-back-" + (i + 1) + ".png"));
            }
        }

        [UnityTest, Explicit]
        public IEnumerator Capture_lab_and_portal_room()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            Upgrades.SetLevel(UpgradeType.ThickerShadow, 2);
            Upgrades.SetLevel(UpgradeType.FieldKit, 1);
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            PlayerController.Instance.Input.overrideFrame = new InputFrame();
            var follow = CameraFollow.Instance;
            yield return new WaitForSeconds(0.3f);
            Save(Path.Combine(folder, "lab-Shoulder.png"));
            follow.enabled = false;
            Camera.main.transform.SetPositionAndRotation(new Vector3(0f, 15f, -9f), Quaternion.Euler(58f, 0f, 0f));
            yield return null;
            Save(Path.Combine(folder, "lab-Above.png"));

            SaveSystem.Wipe();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            floors.BuildFloor(10);
            yield return null;
            Room arena = floors.RoomAt(floors.Layout.exit.cell);
            player.Teleport(arena.Centre + new Vector3(0f, 0.05f, -7f));
            player.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(2.5f);
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            yield return null;
            yield return null;
            Save(Path.Combine(folder, "queen-fight.png"));

            floors.Boss.Health.Kill();
            yield return new WaitForSeconds(0.6f);
            player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            yield return null;
            yield return null;
            Save(Path.Combine(folder, "portal-room.png"));
            SaveSystem.Wipe();
        }

        // The whole screen including the HUD, as the player sees it.
        [UnityTest, Explicit]
        public IEnumerator Capture_hud()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
            string folder = Path.Combine(Path.GetTempPath(), "shrunk-tests", "shots");
            Directory.CreateDirectory(folder);

            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            player.Input.overrideFrame = new InputFrame();
            player.Health.SetInvulnerable(600f);
            RunState.AddCoins(23);
            RunState.AddSamples(41);
            player.Power.SetMeter(0.7f);
            player.Items.Add(GameAssets.Instance.items.Get(ItemType.SugarCube));

            // A fight room, so the minimap, arrows and enemies are all on screen.
            RoomNode fight = null;
            foreach (var node in floors.Layout.ordered) if (node.type == RoomType.Normal) { fight = node; break; }
            player.Teleport(floors.RoomAt(fight.cell).Centre + Vector3.up * 0.05f);
            yield return new WaitForSeconds(1f);
            yield return Screen(Path.Combine(folder, "hud-run.png"));

            floors.BuildFloor(5);
            yield return null;
            Room arena = floors.RoomAt(floors.Layout.exit.cell);
            player.Teleport(arena.Centre + new Vector3(0f, 0.05f, -6f));
            player.transform.rotation = Quaternion.identity;
            CameraFollow.Instance.SetMode(CameraMode.Shoulder);
            yield return new WaitForSeconds(1.5f);
            yield return Screen(Path.Combine(folder, "hud-boss.png"));

            PauseMenu.Instance.Open();
            yield return null;
            yield return Screen(Path.Combine(folder, "hud-pause.png"));
            PauseMenu.Instance.Close();

            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            PlayerController.Instance.Input.overrideFrame = new InputFrame();
            SaveSystem.Data.meta.samples = 180;
            var pad = Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)[0];
            PlayerController.Instance.Teleport(pad.transform.position + new Vector3(0f, 0.05f, -1.8f));
            yield return new WaitForSeconds(0.4f);
            yield return Screen(Path.Combine(folder, "hud-lab.png"));
            SaveSystem.Wipe();
        }

        // Draws the HUD into the picture too. Headless Unity can't grab the real screen, so for the moment of the
        // picture the HUD canvas is attached to the camera instead of sitting on top of the screen.
        static IEnumerator Screen(string path)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var target = new RenderTexture(1280, 720, 24);
            cam.targetTexture = target;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 0.5f;
            }
            yield return null;
            yield return null; // a second frame, so anything placed from the canvas size has the new size
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = target;
            var picture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            picture.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, picture.EncodeToPNG());
            Object.Destroy(picture);

            foreach (var canvas in canvases)
            {
                if (canvas != null) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            cam.targetTexture = null;
            target.Release();
            Object.Destroy(target);
            Debug.Log("[Shrunk] Saved " + path);
        }

        // Stops an ant thinking so it stands still for its picture; its mutation tells keep animating.
        static void Freeze(Enemy enemy)
        {
            enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            enemy.frozen = true;
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
