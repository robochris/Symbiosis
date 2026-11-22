using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>
    /// A bot plays whole floors on foot, the way a person would: out of the start room, down the tunnels, through
    /// every doorway, fighting whatever locks it in, until every room is cleared. Nothing is teleported, so this is
    /// what catches a doorway that can't be walked through, an ant that can't be reached, or a fight that never ends.
    /// </summary>
    public class SoakTests
    {
        PlayerController _player;
        FloorManager _floors;
        readonly NavMeshPath _path = new NavMeshPath();
        float _strafe = 1f, _nextSwap;

        [UnitySetUp]
        public IEnumerator LoadRun()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            _player = PlayerController.Instance;
            _floors = FloorManager.Instance;
            _player.Input.overrideFrame = new InputFrame();
            // Top-down input: the bot aims by direction, with no camera to turn.
            CameraFollow.Instance.SetMode(CameraMode.TopDown);
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            Achievements.EverythingUnlocked = false;
            GameSettings.Reset();
            SaveSystem.Wipe();
        }

        string Where(RoomNode node)
        {
            Room room = node != null ? _floors.RoomAt(node.cell) : null;
            string where = $"floor {_floors.FloorNumber} (seed {_floors.Layout.seed}), biome {_floors.biome.id}, locked {_floors.IsLocked}, exit open {_floors.ExitOpen}, " +
                           $"player at {_player.transform.position} in {(_floors.CurrentNode != null ? _floors.CurrentNode.type + " " + _floors.CurrentNode.cell : "no room")}\n";
            if (node == null) return where + "(no room given)\n" + _floors.Layout.ToAscii();
            bool current = _floors.Layout.rooms.TryGetValue(node.cell, out var now) && now == node;
            where += $"{node.type} room {node.cell}, still part of this floor: {current}\n";
            if (room == null) return where + "(that room is not built)\n" + _floors.Layout.ToAscii();
            return where + $"{RoomDressing.NameOf(room.Shape.feature)}\n{_floors.Layout.ToAscii()}\n{room.Shape.ToAscii()}";
        }

        // Who is still standing, and why the bot might not be able to finish them.
        string Survivors()
        {
            var text = new System.Text.StringBuilder();
            foreach (var enemy in Enemy.Active)
            {
                string mutations = "";
                if (enemy.Mutations != null) foreach (var mutation in enemy.Mutations.Active) mutations += " " + mutation.type;
                bool onMesh = NavMesh.SamplePosition(enemy.transform.position, out var hit, 1.5f, NavMesh.AllAreas);
                text.AppendLine($"  {enemy.name}{(enemy.IsElite ? " ELITE" : "")} health {enemy.Health.Current}/{enemy.Health.maxHealth} at {enemy.transform.position}" +
                                $" (player at {_player.transform.position}) mutations:{mutations} targetable={enemy.Targetable} active={enemy.gameObject.activeSelf}" +
                                $" onNavMesh={onMesh} inSight={CanSee(enemy)} sinceHit={Time.time - enemy.Health.LastDamageTime:F1}s");
            }
            return text.ToString();
        }

        static Vector3 OnNavMesh(Vector3 near) => NavMesh.SamplePosition(near, out var hit, 3f, NavMesh.AllAreas) ? hit.position : near;

        Vector3 NextStep(Vector3 destination)
        {
            Vector3 position = _player.transform.position;
            if (NavMesh.CalculatePath(position, destination, NavMesh.AllAreas, _path) && _path.corners.Length > 1) return _path.corners[1];
            return destination;
        }

        bool CanSee(Enemy enemy)
        {
            Vector3 from = _player.transform.position + Vector3.up * 0.7f;
            Vector3 to = enemy.transform.position + Vector3.up * 0.7f;
            foreach (var hit in Physics.RaycastAll(from, (to - from).normalized, (to - from).magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Health>() == null) return false;
            }
            return true;
        }

        // One frame of fighting: shoot the nearest ant in sight while circling it; walk round cover to one that isn't.
        void FightStep()
        {
            Vector3 position = _player.transform.position;
            Enemy nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead || !enemy.Targetable || !enemy.gameObject.activeSelf) continue;
                float d = (enemy.transform.position - position).magnitude;
                if (d < nearestDistance) { nearestDistance = d; nearest = enemy; }
            }
            if (nearest == null)
            {
                _player.Input.overrideFrame = new InputFrame();
                return;
            }

            Vector3 to = nearest.transform.position - position;
            to.y = 0f;
            Vector3 aim = to.normalized;
            Vector3 move;
            if (CanSee(nearest))
            {
                if (Time.time >= _nextSwap)
                {
                    _nextSwap = Time.time + Random.Range(1f, 2.5f);
                    _strafe = -_strafe;
                }
                move = Vector3.Cross(Vector3.up, aim) * _strafe;
                if (nearestDistance < 5f) move -= aim * 1.2f;
                else if (nearestDistance > Mathf.Min(10f, _player.weapon.Reach * 0.8f)) move += aim;
            }
            else
            {
                move = NextStep(OnNavMesh(nearest.transform.position)) - position;
                move.y = 0f;
            }
            // Something in its face that it can't hurt (a shell, or a wall at its back): roll out sideways, as a person would,
            // and come round behind it.
            bool pinned = nearestDistance < 3f && Time.time - nearest.Health.LastDamageTime > 2.5f;
            bool dodge = pinned && _player.DodgeCharges > 0 && Time.time >= _nextDodge;
            if (pinned) move = Vector3.Cross(Vector3.up, aim) * _strafe - aim * 0.3f;
            if (dodge)
            {
                _nextDodge = Time.time + 1.2f;
                _strafe = -_strafe;
            }
            _player.Input.overrideFrame = new InputFrame { move = move.normalized, aim = aim, fire = true, dodge = dodge };
        }

        float _nextDodge;

        // Walks to a room on foot, fighting through anything that locks on the way. Fails if it can't get there.
        IEnumerator GoTo(RoomNode node)
        {
            Room room = _floors.RoomAt(node.cell);
            Vector3 destination = OnNavMesh(room.Centre + new Vector3(RoomShape.TileSize, 0f, 0f));
            float walked = 0f, fought = 0f;
            Vector3 stuckAt = _player.transform.position;
            float stuckFor = 0f;

            while (true)
            {
                Vector3 position = _player.transform.position;
                if (_floors.IsLocked)
                {
                    fought += Time.deltaTime;
                    if (fought > 150f) Assert.Fail("a fight that never ends: " + Enemy.Active.Count + " left\n" + Survivors() + Where(_floors.CurrentNode));
                    FightStep();
                    yield return null;
                    continue;
                }

                Vector3 left = destination - position;
                left.y = 0f;
                if (node.cleared && (left.magnitude < 0.8f || node == _floors.Layout.exit)) break; // never walk on over the way down
                if (_floors.ExitOpen && _floors.Layout.InArena(node.cell)) break; // the boss fell on the way here: stop where we are

                // Standing on the spot in a room that should have locked: step further in.
                Vector3 next = left.magnitude < 0.8f ? room.Centre : NextStep(destination);
                Vector3 direction = next - position;
                direction.y = 0f;
                _player.Input.overrideFrame = new InputFrame { move = direction.normalized, aim = direction.normalized };

                walked += Time.deltaTime;
                if (walked > 90f) Assert.Fail("could not walk to the room\n" + Where(node));
                stuckFor += Time.deltaTime;
                if ((position - stuckAt).magnitude > 1f)
                {
                    stuckAt = position;
                    stuckFor = 0f;
                }
                if (stuckFor > 6f) Assert.Fail($"stuck at {position} on the way to the room\n" + Where(node));
                yield return null;
            }
            _player.Input.overrideFrame = new InputFrame();
        }

        IEnumerator PlayFloor(int floor, int seed, bool canBeHurt = false)
        {
            _floors.BuildFloor(floor, seed);
            yield return null;
            if (canBeHurt)
            {
                // For measuring: nothing protects the bot, and it has health enough never to fall.
                _player.Health.ClearInvulnerability();
                _player.Health.SetMaxHealth(9999, true);
            }
            else
            {
                _player.Health.SetInvulnerable(100000f);
            }

            // Nearest rooms first, so the walk is the one a player would take.
            var order = new List<RoomNode>(_floors.Layout.ordered);
            // The exit goes last: once it is cleared the way down opens, and walking over it would end the floor.
            RoomNode exit = _floors.Layout.exit;
            order.Sort((a, b) => a == b ? 0 : a == exit ? 1 : b == exit ? -1 : a.distance.CompareTo(b.distance));
            foreach (var node in order)
            {
                if (node == _floors.Layout.start) continue;
                // The boss can be met and beaten from any square of its arena. Once the way down is open the floor is
                // done: walking on across the arena would mean walking over the hole, which (rightly) leaves the floor.
                if (_floors.ExitOpen && (node == exit || _floors.Layout.InArena(node.cell))) continue;
                yield return GoTo(node);
                if (_floors.ExitOpen && (node == exit || _floors.Layout.InArena(node.cell))) continue;
                Assert.IsTrue(node.visited, "walked to a room without entering it\n" + Where(node));
                Assert.IsTrue(node.cleared, "left a room uncleared\n" + Where(node));
            }
            Assert.IsTrue(_floors.ExitOpen, $"floor {floor} (seed {seed}): every room cleared but the way down is shut");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator A_bot_clears_three_whole_floors_on_foot()
        {
            Time.timeScale = 4f;
            yield return PlayFloor(2, 1001);
            yield return PlayFloor(5, 1002);
            yield return PlayFloor(8, 1003);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator A_bot_clears_two_Birthday_Cake_floors_on_foot()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            Time.timeScale = 4f;
            yield return PlayFloor(4, 2001);
            yield return PlayFloor(5, 2002);
        }

        // Walks into the boss room of a floor and fights with no invulnerability, counting what lands.
        /// <summary>Items the bot carries into a boss, as a player would have picked up by then: 2 at a mini-boss, 5 at a boss. 0 for bare.</summary>
        public static int MiniBossItems = 2, BossItems = 5;

        // Tops the bot's items up to what a run would have found by this fight. The same seed gives the same items.
        void GiveRunItems(int floor, int seed)
        {
            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null || _player.Items == null) return;
            int want = FloorLayout.IsBossFloor(floor) && FloorLayout.FloorInBiome(floor) % 10 == 0 ? BossItems : MiniBossItems;
            int missing = want - _player.Items.Collected.Count;
            if (missing <= 0) return;
            // From every item there is, whatever happens to be unlocked by now, so the same seed always gives the same items.
            bool unlocked = Achievements.EverythingUnlocked;
            Achievements.EverythingUnlocked = true;
            var picks = assets.items.RandomCommon(new System.Random(seed), missing);
            Achievements.EverythingUnlocked = unlocked;
            var names = new System.Collections.Generic.List<string>();
            foreach (var pick in picks) { _player.Items.Add(pick); names.Add(pick.displayName); }
            Debug.Log("[Shrunk] MEASURE items: " + string.Join(", ", names));
        }

        IEnumerator MeasureBoss(int floor, int seed, string label)
        {
            GiveRunItems(floor, seed);
            _floors.BuildFloor(floor, seed);
            yield return null;
            // Start a little way from the middle, where the boss will appear.
            _player.Health.ClearInvulnerability();
            _player.Health.SetMaxHealth(999, true);
            _player.Teleport(OnNavMesh(_floors.ArenaCentre + new Vector3(0f, 0f, -10f)) + Vector3.up * 0.05f);

            int hits = 0, pools = 0;
            System.Action<int, Vector3> count = (amount, direction) => { hits += amount; if (direction == Vector3.zero) pools += amount; };
            _player.Health.Damaged += count;
            float started = Time.time;
            for (float t = 0f; t < 3f && !_floors.IsLocked; t += Time.deltaTime) yield return null;
            Assert.IsTrue(_floors.IsLocked, "the boss room did not lock");
            while (_floors.IsLocked && Time.time - started < 400f)
            {
                FightStep();
                yield return null;
            }
            _player.Health.Damaged -= count;
            _player.Input.overrideFrame = new InputFrame();
            // What the boss dropped stays on the floor: the bot only carries what GiveRunItems hands it.
            Pickup.ClearAll();
            Debug.Log($"[Shrunk] MEASURE {label}: {(_floors.IsLocked ? "NOT BEATEN" : "beaten")} in {Time.time - started:F0} s, {hits} hits taken ({pools} from pools and blasts)");
        }

        /// <summary>Not a test: how hard the two bosses are for a bot that strafes but never dodges. Read the log.</summary>
        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Measure_boss_difficulty()
        {
            // At normal speed: sped up, the bot misses far more and the numbers mean nothing.
            for (int i = 0; i < 2; i++) yield return MeasureBoss(5, 500 + i, "Brood Mother");
            for (int i = 0; i < 2; i++) yield return MeasureBoss(10, 900 + i, "Ant Queen");
        }

        /// <summary>Not a test: one fight with every boss in the game, for comparing them. Read the log for the MEASURE lines.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_every_boss()
        {
            yield return MeasureBoss(5, 500, "Brood Mother");
            yield return MeasureBoss(10, 900, "Ant Queen");
            Assert.IsTrue(_floors.SetBiome("cake"));
            yield return MeasureBoss(5, 501, "Jelly Mother");
            yield return MeasureBoss(10, 901, "Trick Candle");
            Assert.IsTrue(_floors.SetBiome("sofa"));
            yield return MeasureBoss(5, 502, "Grandmother Dust");
            yield return MeasureBoss(10, 902, "Robot Vacuum");
            _floors.biome = _floors.bottomBiome;
            yield return MeasureBoss(10, 903, "The Cat");
        }

        // Plays one whole floor with nothing protecting the bot, and says how long it took and how often it was hit.
        IEnumerator MeasureFloor(string biome, int floor, int seed)
        {
            Assert.IsTrue(_floors.SetBiome(biome));
            int hits = 0, rooms = 0, pools = 0;
            System.Action<int, Vector3> count = (amount, direction) => { hits += amount; if (direction == Vector3.zero) pools += amount; };
            _player.Health.Damaged += count;
            float started = Time.time;
            // Coins as the game hands them out, not as the tests pin them.
            float? pinned = Loot.CoinChanceOverride;
            Loot.CoinChanceOverride = null;
            int coinsBefore = RunState.Coins;
            yield return PlayFloor(floor, seed, true);
            Loot.CoinChanceOverride = pinned;
            int picked = RunState.Coins - coinsBefore, lying = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Coin) lying += pickup.amount;
            _player.Health.Damaged -= count;
            foreach (var node in _floors.Layout.ordered) if (node.type == RoomType.Normal || node.type == RoomType.Exit) rooms++;
            Debug.Log($"[Shrunk] MEASURE FLOOR {biome} {floor}: {Time.time - started:F0} s, {hits} hits over {rooms} fight rooms ({(float)hits / Mathf.Max(1, rooms):F1} a room), {pools} of them from pools and blasts; coins {picked} picked up and {lying} left lying");
        }

        /// <summary>
        /// Not a test: an early floor and a late floor of every biome, played with nothing protecting the bot. The
        /// late floors have the lobbers and supports. Read the log for the MEASURE FLOOR lines. Sped up, so the bot
        /// is clumsier than it would be: compare the lines with each other, not with a person.
        /// </summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_floors()
        {
            Achievements.EverythingUnlocked = true;
            Time.timeScale = 3f;
            foreach (string biome in new[] { "ants", "cake", "sofa", "fridge" })
            {
                yield return MeasureFloor(biome, 3, 7003);
                yield return MeasureFloor(biome, 8, 7008);
            }
        }

        /// <summary>Not a test: the same floor and the same boss with each of the three weapons. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_weapons()
        {
            foreach (var info in Weapons.All)
            {
                _player.weapon.kind = info.kind;
                Debug.Log($"[Shrunk] MEASURE WEAPON {info.name}");
                Time.timeScale = 3f;
                yield return MeasureFloor("ants", 6, 7106);
                Time.timeScale = 1f;
                yield return MeasureBoss(5, 500, "Brood Mother with the " + info.name);
            }
        }

        /// <summary>Not a test: the library's two, and one of its floors. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_library()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("library"));
            yield return MeasureBoss(5, 506, "The Trainee");
            yield return MeasureBoss(10, 906, "The Librarian");
            Time.timeScale = 3f;
            yield return MeasureFloor("library", 8, 7108);
        }

        /// <summary>Not a test: the sink's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_sink()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("sink"));
            yield return MeasureBoss(5, 516, "The Sponge");
            yield return MeasureBoss(10, 916, "The Tap");
        }

        /// <summary>Not a test: the bookshelf's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_bookshelf()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("bookshelf"));
            yield return MeasureBoss(5, 515, "The Bookend");
            yield return MeasureBoss(10, 915, "The Encyclopedia");
        }

        /// <summary>Not a test: the shed's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_shed()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("shed"));
            yield return MeasureBoss(5, 514, "The Hedge Trimmer");
            yield return MeasureBoss(10, 914, "The Lawnmower");
        }

        /// <summary>Not a test: the attic's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_attic()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("attic"));
            yield return MeasureBoss(5, 513, "The Bat");
            yield return MeasureBoss(10, 913, "The Spider");
        }

        /// <summary>Not a test: the bathroom's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_bathroom()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("bathroom"));
            yield return MeasureBoss(5, 512, "The Bar of Soap");
            yield return MeasureBoss(10, 912, "The Rubber Duck");
        }

        /// <summary>Not a test: the toy box's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_toybox()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("toybox"));
            yield return MeasureBoss(5, 511, "The Wind-up Mouse");
            yield return MeasureBoss(10, 911, "The Jack-in-the-Box");
        }

        /// <summary>Not a test: the aquarium's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_aquarium()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("aquarium"));
            yield return MeasureBoss(5, 510, "The Snail");
            yield return MeasureBoss(10, 910, "The Goldfish");
        }

        /// <summary>Not a test: the console's two. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_console()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("console"));
            yield return MeasureBoss(5, 509, "The Cursor");
            yield return MeasureBoss(10, 909, "The Glitch");
        }

        /// <summary>Not a test: the clouds' two, and one of their floors. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_clouds()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("clouds"));
            yield return MeasureBoss(5, 508, "The Kite");
            yield return MeasureBoss(10, 908, "The Thundercloud");
            Time.timeScale = 3f;
            yield return MeasureFloor("clouds", 8, 7110);
        }

        /// <summary>Not a test: the forest's two, and one of its floors. Read the log.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_forest()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("forest"));
            yield return MeasureBoss(5, 507, "The Froglet");
            yield return MeasureBoss(10, 907, "The Toad");
            Time.timeScale = 3f;
            yield return MeasureFloor("forest", 8, 7109);
        }

        /// <summary>Not a test: the machine, for a bot that strafes and never dodges. Read the log.</summary>
        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Measure_machine()
        {
            _floors.biome = _floors.machineBiome;
            yield return MeasureBoss(10, 905, "The Machine");
        }

        /// <summary>Not a test: three late floors of the ants and of the cake, to tell a hard biome from an unlucky floor.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_late_floors()
        {
            Achievements.EverythingUnlocked = true;
            Time.timeScale = 3f;
            foreach (int seed in new[] { 8101, 8102, 8103 })
            {
                yield return MeasureFloor("ants", 8, seed);
                yield return MeasureFloor("cake", 8, seed);
            }
        }

        /// <summary>Not a test: the fridge's two.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_fridge_bosses()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("fridge"));
            yield return MeasureBoss(5, 504, "The Leftovers");
            yield return MeasureBoss(10, 904, "The Ice Maker");
        }

        /// <summary>Not a test: the three that needed taming, again.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator Measure_tuned_bosses()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            yield return MeasureBoss(10, 901, "Trick Candle");
            Assert.IsTrue(_floors.SetBiome("sofa"));
            yield return MeasureBoss(5, 502, "Grandmother Dust");
            _floors.biome = _floors.bottomBiome;
            yield return MeasureBoss(10, 903, "The Cat");
        }

        /// <summary>Under the Sofa, on a floor late enough to have its lobbers and supports, and its mini boss.</summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator A_bot_clears_two_sofa_floors_on_foot()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("sofa"));
            Time.timeScale = 4f;
            yield return PlayFloor(6, 3001);
            yield return PlayFloor(5, 3002);
            Achievements.EverythingUnlocked = false;
        }

        /// <summary>By name only: the fridge, late enough for every kind of enemy, and its mini boss.</summary>
        [UnityTest, Explicit, Timeout(600000)]
        public IEnumerator A_bot_clears_two_fridge_floors_on_foot()
        {
            Achievements.EverythingUnlocked = true;
            Assert.IsTrue(_floors.SetBiome("fridge"));
            Time.timeScale = 4f;
            yield return PlayFloor(7, 4001);
            yield return PlayFloor(5, 4002);
        }

        /// <summary>The long version, by name only: ten floors in a row, new ones every time.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator A_bot_clears_a_whole_biome_on_foot()
        {
            Time.timeScale = 4f;
            for (int floor = 1; floor <= 10; floor++) yield return PlayFloor(floor, Random.Range(1, int.MaxValue));
        }

        /// <summary>
        /// By name only: the game as it ships. Six floors to a world, twists and exchanges rolling as they do for a
        /// player, and a bot on foot through every floor of all four worlds. Read the log for the WORLD lines.
        /// </summary>
        [UnityTest, Explicit, Timeout(7200000)]
        public IEnumerator A_bot_plays_all_four_six_floor_worlds()
        {
            FloorLayout.FloorsPerBiome = 6;
            FloorManager.TwistChanceForAll = null;
            FloorManager.ExchangeChanceForAll = null;
            try
            {
                foreach (string biome in new[] { "ants", "cake", "sofa", "fridge" })
                {
                    Achievements.EverythingUnlocked = true;
                    Assert.IsTrue(_floors.SetBiome(biome));
                    float started = Time.time;
                    Time.timeScale = 4f;
                    string twists = "";
                    for (int floor = 1; floor <= 6; floor++)
                    {
                        yield return PlayFloor(floor, Random.Range(1, int.MaxValue));
                        if (_floors.Twist != FloorTwist.None) twists += $" floor {floor} {FloorTwists.Name(_floors.Twist)};";
                        Assert.AreEqual(floor % 3 == 0, _floors.Layout.hasArena || FloorLayout.IsBossFloor(floor), "a boss floor in the wrong place");
                    }
                    Debug.Log($"[Shrunk] WORLD {biome}: six floors in {(Time.time - started) / 60f:F1} minutes of game time. Twists:{(twists.Length > 0 ? twists : " none")}");
                }
            }
            finally
            {
                FloorLayout.FloorsPerBiome = 10;
                FloorManager.TwistChanceForAll = 0f;
                FloorManager.ExchangeChanceForAll = 0f;
                Time.timeScale = 1f;
            }
        }

        /// <summary>The same in the Birthday Cake.</summary>
        [UnityTest, Explicit, Timeout(3600000)]
        public IEnumerator A_bot_clears_a_whole_cake_biome_on_foot()
        {
            Assert.IsTrue(_floors.SetBiome("cake"));
            Time.timeScale = 4f;
            for (int floor = 1; floor <= 10; floor++) yield return PlayFloor(floor, Random.Range(1, int.MaxValue));
        }
    }
}
