using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Runs a floor: generates the flat layout, carves a random shape for every room, locks the doors when the player walks into
    /// a room with enemies, opens them when the room is cleared, and moves on to the next floor at the exit.
    /// </summary>
    public class FloorManager : MonoBehaviour
    {
        public static FloorManager Instance { get; private set; }
        /// <summary>A new floor has been built.</summary>
        public static event Action<FloorManager> FloorBuilt;
        /// <summary>The player walked into another room, or the current room's state changed.</summary>
        public static event Action<FloorManager> RoomChanged;
        /// <summary>The player stepped into the extract portal after a biome boss.</summary>
        public static event Action ExtractChosen;

        public BiomeDefinition biome;
        [Tooltip("The other biomes a portal can lead to after a biome boss. The current one is always offered again.")]
        public BiomeDefinition[] otherBiomes;
        [Tooltip("How many portals to other biomes are offered, on top of the one back into this biome.")]
        public int otherPortals = 2;
        [Tooltip("Where the lab's hidden door leads: one floor, and Dr. Finch. Never offered by a portal.")]
        public BiomeDefinition bottomBiome;

        /// <summary>Builds the bottom: the one floor behind the lab's hidden door.</summary>
        public void BuildBottom()
        {
            if (bottomBiome == null) return;
            biome = bottomBiome;
            depth = 1;
            BuildFloor(10);
        }

        /// <summary>One way onward in the portal room: a gate, and the biome it leads to.</summary>
        public class BiomePortal
        {
            public GameObject gate;
            public BiomeDefinition leadsTo;
            public readonly StepPad pad = new StepPad { radius = 1.5f };
        }

        /// <summary>After a biome boss: every way onward. The first is always this biome again.</summary>
        public IReadOnlyList<BiomePortal> Portals => _portals;
        readonly List<BiomePortal> _portals = new List<BiomePortal>();

        /// <summary>Every biome this run can visit: the one it started in, then the others.</summary>
        public List<BiomeDefinition> AllBiomes()
        {
            var all = new List<BiomeDefinition>();
            if (_firstBiome != null) all.Add(_firstBiome);
            if (otherBiomes != null) foreach (var other in otherBiomes) if (other != null && !all.Contains(other)) all.Add(other);
            if (biome != null && !all.Contains(biome)) all.Add(biome);
            return all;
        }

        /// <summary>Switches to the biome with this id, e.g. when a saved run is loaded. False if there is no such biome.</summary>
        public bool SetBiome(string id)
        {
            foreach (var candidate in AllBiomes())
            {
                if (candidate.id != id) continue;
                biome = candidate;
                return true;
            }
            return false;
        }

        BiomeDefinition _firstBiome;
        Light _sun;
        /// <summary>Distance between room centres: one room's full footprint.</summary>
        public float cellSize => RoomShape.Size * RoomShape.TileSize;
        [Tooltip("0 = a different floor every time. Any other number repeats the same run of floors.")]
        public int fixedSeed = 0;
        [Tooltip("How far past the doorway the player must be before the doors lock.")]
        public float lockMargin = 1.5f;
        [Tooltip("Enemies never appear closer to the player than this.")]
        public float minSpawnDistance = 8f;
        public float exitRadius = 1.4f;
        [Tooltip("How many biomes into the run this is. Raises the mutation budget: 1 point at depth 1, up to 5 at depth 5.")]
        public int depth = 1;

        public int FloorNumber { get; private set; }
        /// <summary>1-10: which floor of the current biome this is.</summary>
        public int FloorInBiome => FloorLayout.FloorInBiome(FloorNumber);
        /// <summary>The boss of the current floor once its fight has started, or null.</summary>
        public BossEnemy Boss { get; private set; }
        public FloorLayout Layout { get; private set; }
        public RoomNode CurrentNode { get; private set; }
        public Room CurrentRoom => CurrentNode != null ? _rooms[CurrentNode.cell] : null;
        public bool IsLocked { get; private set; }
        public bool ExitOpen { get; private set; }
        /// <summary>After a biome boss: the way home. Null until then.</summary>
        public GameObject ExtractPortal { get; private set; }
        /// <summary>After a biome boss: the way on to the next biome, one depth deeper. Null until then.</summary>
        public GameObject DeeperPortal => _portals.Count > 0 ? _portals[0].gate : null;
        public IReadOnlyList<Enemy> LiveEnemies => _live;
        /// <summary>For tests: 0 to 1 replaces the usual chance of an elite in a fight room. Negative = the usual chance.</summary>
        [System.NonSerialized] public float eliteChanceOverride = -1f;
        public int FloorsBuilt { get; private set; }

        readonly Dictionary<Vector2Int, Room> _rooms = new Dictionary<Vector2Int, Room>();
        readonly Dictionary<Enemy, Pool<Enemy>> _pools = new Dictionary<Enemy, Pool<Enemy>>();
        readonly List<Enemy> _live = new List<Enemy>();
        readonly List<Pool<Enemy>> _livePools = new List<Pool<Enemy>>();
        Transform _roomsRoot;
        Transform _enemiesRoot;
        NavMeshSurface _surface;
        bool _exitArmed;
        readonly StepPad _extractPad = new StepPad { radius = 1.5f };

        void Awake()
        {
            Instance = this;
            _firstBiome = biome;

            _roomsRoot = new GameObject("Rooms").transform;
            _roomsRoot.SetParent(transform, false);
            _enemiesRoot = new GameObject("Enemies").transform;
            _enemiesRoot.SetParent(transform, false);

            // The navmesh is baked from the rooms' colliders each time a floor is built, so enemies path around cover.
            _surface = _roomsRoot.gameObject.AddComponent<NavMeshSurface>();
            _surface.collectObjects = CollectObjects.Children;
            _surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        }

        void OnEnable()
        {
            WorldPrompt.Add(PortalPrompt);
            Enemy.SummonRequested += OnSummon;
            Enemy.MinionRequested += OnMinion;
            FinchNotes.Found += OnNoteFound;
        }

        void OnDisable()
        {
            WorldPrompt.Remove(PortalPrompt);
            Enemy.SummonRequested -= OnSummon;
            Enemy.MinionRequested -= OnMinion;
            FinchNotes.Found -= OnNoteFound;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Loot.CoinBonus = 0;
            Loot.SampleBonus = 0;
        }

        void Start()
        {
            // In a real run the RunDirector decides which floor to build (a new run, or a saved one).
            if (GetComponent<RunDirector>() == null) BuildFloor(1);
        }

        public Room RoomAt(Vector2Int cell) => _rooms.TryGetValue(cell, out var room) ? room : null;

        public Vector3 CellCentre(Vector2Int cell) => new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

        public Vector2Int CellOf(Vector3 position) =>
            new Vector2Int(Mathf.RoundToInt(position.x / cellSize), Mathf.RoundToInt(position.z / cellSize));

        /// <param name="seedOverride">Non-zero rebuilds one exact floor, e.g. when a saved run is loaded.</param>
        public void BuildFloor(int number, int seedOverride = 0)
        {
            ClearFloor();
            if (_firstBiome == null) _firstBiome = biome;

            FloorNumber = number;
            int seed = seedOverride != 0 ? seedOverride : fixedSeed != 0 ? fixedSeed + number : UnityEngine.Random.Range(1, int.MaxValue);
            Twist = twistOverride ?? FloorTwists.Roll(seed, number, biome, TwistChanceForAll ?? twistChance);
            Loot.CoinBonus = Twist == FloorTwist.Crowded ? FloorTwists.CrowdedCoins : 0;
            Loot.SampleBonus = Twist == FloorTwist.PowerCut ? FloorTwists.PowerCutSamples : 0;
            ApplyLook();
            Layout = FloorLayout.Generate(seed, FloorLayout.RoomCountForFloor(number), FloorLayout.IsBossFloor(number));

            foreach (var node in Layout.ordered)
            {
                RoomShape shape = RoomShape.GenerateFor(Layout, node, number);
                Room room = Room.Create(shape, biome, _roomsRoot, CellCentre(node.cell));
                room.name = $"{node.type} {node.cell.x},{node.cell.y}";
                _rooms.Add(node.cell, room);

                // Only fight rooms need clearing.
                node.cleared = node.type != RoomType.Normal && node.type != RoomType.Exit;
                // At the bottom nothing is waiting in the rooms on the way. Only at the end.
                if (biome.isBottom && node.type == RoomType.Normal) node.cleared = true;
            }

            // Every way into a boss's room gets an arch, so it is walked into on purpose.
            if (FloorLayout.IsBossFloor(number))
            {
                foreach (var node in Layout.ordered)
                {
                    if (node != Layout.exit && !Layout.InArena(node.cell)) continue;
                    Room room = _rooms[node.cell];
                    for (int side = 0; side < 4; side++)
                    {
                        if ((room.Doors & (Doors)(1 << side)) != 0) BossGate.Create(room, side, biome, seed);
                    }
                }
            }

            Physics.SyncTransforms();
            _surface.BuildNavMesh();
            PlaceItems(seed);
            PlaceNote(seed);

            var player = PlayerController.Instance;
            if (player != null)
            {
                player.Teleport(_rooms[Layout.start.cell].Centre + Vector3.up * 0.05f);
                if (CameraFollow.Instance != null) CameraFollow.Instance.Snap();
            }

            FloorsBuilt++;
            if (FloorsBuilt > 1)
            {
                Sfx.Play(SfxId.NextFloor);
                Fade.In();
            }
            FloorBuilt?.Invoke(this);
            Enter(Layout.start);
        }

        // The sky, the light and the haze belong to the biome: the dark of the earth, or a bright afternoon.
        void ApplyLook()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            bool dark = Twist == FloorTwist.PowerCut;
            RenderSettings.ambientLight = dark ? biome.ambientColor * FloorTwists.PowerCutAmbient : biome.ambientColor;
            RenderSettings.fog = biome.fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = biome.fogColor;
            RenderSettings.fogStartDistance = biome.fogStart;
            RenderSettings.fogEndDistance = biome.fogEnd;

            var view = Camera.main;
            if (view != null) view.backgroundColor = biome.skyColor;

            if (_sun == null)
            {
                foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional) { _sun = light; break; }
                }
            }
            if (_sun != null) _sun.intensity = dark ? biome.sunIntensity * FloorTwists.PowerCutSun : biome.sunIntensity;
        }

        /// <summary>What is different about this floor, if anything.</summary>
        public FloorTwist Twist { get; private set; }
        [Tooltip("Chance that a floor has a twist. Never the first two floors, a biome's first floor or a boss's.")]
        public float twistChance = 0.3f;
        /// <summary>Tests and clips set this to get one particular twist (or none) on the next floor built.</summary>
        [System.NonSerialized] public FloorTwist? twistOverride;

        /// <summary>Set by the tests, for every floor manager: the chance of a twist and of a dare where a test has not asked for one.</summary>
        public static float? TwistChanceForAll, DareChanceForAll;

        /// <summary>The dare left in a cleared room on this floor, until it is rung.</summary>
        public DareBell Dare { get; private set; }
        [Tooltip("Chance that clearing an ordinary room leaves a dare behind. One a floor at most, and never on floor 1.")]
        public float dareChance = 0.25f;
        /// <summary>Tests set this to make the roll certain (1) or impossible (0).</summary>
        [System.NonSerialized] public float dareChanceOverride = -1f;
        /// <summary>How many more enemies a dare brings than the room had the first time.</summary>
        public const int DareExtra = 3, DareCoins = 5;
        /// <summary>True while the fight going on is one the player asked for.</summary>
        public bool DareFight { get; private set; }
        bool _dareOffered;

        public static event Action<FloorManager> DareRung;
        public static event Action<FloorManager> DareWon;

        /// <summary>Shuts a cleared room again on a second, worse wave. Called by the bell when it is rung.</summary>
        public bool StartDare(DareBell bell)
        {
            if (IsLocked || bell == null || bell.room == null || Layout == null) return false;
            Room room = bell.room;
            RoomNode node = null;
            foreach (var pair in _rooms) if (pair.Value == room) node = Layout.rooms[pair.Key];
            if (node == null) return false;

            var player = PlayerController.Instance;
            Vector3 at = player != null ? player.transform.position : room.Centre;
            IsLocked = true;
            DareFight = true;
            _fightNode = node;
            _fightRoom = room;
            SetFightLocked(true);
            SpawnWave(room, at, DareExtra + (Twist == FloorTwist.Crowded ? FloorTwists.CrowdedExtra : 0), true, 1);

            Destroy(bell.gameObject);
            Dare = null;
            Sfx.Play(SfxId.Item);
            DareRung?.Invoke(this);
            RoomChanged?.Invoke(this);
            return true;
        }

        /// <summary>Lab upgrade: how many items a treasure room offers.</summary>
        [System.NonSerialized] public int treasureChoices = 1;

        // Treasure rooms hold a free item on the pedestal; the shop lays three out in front of the counter, for coins.
        void PlaceItems(int seed)
        {
            var assets = GameAssets.Instance;
            if (assets == null || assets.items == null) return;
            var rng = new System.Random(seed ^ 0x2545F491);

            foreach (var node in Layout.ordered)
            {
                Room room = _rooms[node.cell];
                if (node.type == RoomType.Treasure)
                {
                    var picks = assets.items.RandomCommon(rng, Mathf.Max(1, treasureChoices));
                    for (int i = 0; i < picks.Count; i++)
                    {
                        // One sits on the pedestal; a second (Lucky Find) sits beside it. Taking one removes the other.
                        Vector3 offset = i == 0 ? Vector3.up * 0.6f : new Vector3(0f, 0f, -2.2f);
                        Pickup.Item(picks[i], room.Centre + offset, 0).treasureRoom = room;
                    }
                }
                else if (node.type == RoomType.Shop)
                {
                    var picks = assets.items.RandomCommon(rng, 3);
                    Vector3[] spots = { new Vector3(-2f, 0f, -1.7f), new Vector3(0f, 0f, -2.3f), new Vector3(2f, 0f, -1.7f) };
                    bool sale = Twist == FloorTwist.Sale;
                    for (int i = 0; i < picks.Count; i++) Pickup.Item(picks[i], room.Centre + spots[i], sale ? FloorTwists.Discount(picks[i].price) : picks[i].price);
                    // And something to eat, beside the counter. Both stay inside the square every room keeps clear round its middle.
                    Pickup.Health(room.Centre + new Vector3(2.6f, 0f, 0.9f), sale ? FloorTwists.Discount(Pickup.CrumbPrice) : Pickup.CrumbPrice);
                    // On the other side, the bell: a few coins for three different things.
                    ShopReroll.Create(room, room.Centre + new Vector3(-2.6f, 0f, 0.9f), spots, assets.coin, assets.note).sale = sale;
                }
            }
        }

        /// <summary>The page of Dr. Finch's notebook hidden on this floor, if there is one and it has not been taken.</summary>
        public NotePickup Note { get; private set; }
        [Tooltip("Chance that a floor in the middle of a biome hides a page. Floor 9 always does, if the biome has one left.")]
        public float noteChance = 0.25f;
        // The biome visit a page was last found in: one page a visit, so the trail takes a few trips.
        string _noteVisit;

        // One page at most, tucked into the far corner of a fight room away from the way in.
        void PlaceNote(int seed)
        {
            Note = null;
            if (biome == null || FloorLayout.IsBossFloor(FloorNumber) || FloorInBiome < 2) return;
            FinchNote next = FinchNotes.NextFor(biome.id);
            if (next == null || _noteVisit == biome.id + depth) return;
            var rng = new System.Random(seed ^ 0x51ED270B);
            if (FloorInBiome != 9 && rng.NextDouble() >= noteChance) return;

            var rooms = new List<Room>();
            foreach (var node in Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                Room room = _rooms[node.cell];
                if (!room.Shape.isArenaPart && room.Shape.free.Count > 0) rooms.Add(room);
            }
            if (rooms.Count == 0) return;

            Room chosen = rooms[rng.Next(rooms.Count)];
            Vector2Int best = chosen.Shape.free[0];
            float farthest = -1f;
            foreach (var tile in chosen.Shape.free)
            {
                float away = (tile - chosen.Shape.centre).sqrMagnitude;
                if (away > farthest) { farthest = away; best = tile; }
            }
            Note = NotePickup.Create(next, chosen.TileWorld(best), chosen.transform);
        }

        void OnNoteFound(FinchNote note)
        {
            if (biome != null) _noteVisit = biome.id + depth;
        }

        void ClearFloor()
        {
            for (int i = 0; i < _live.Count; i++) _livePools[i].Release(_live[i]);
            _live.Clear();
            _livePools.Clear();
            GroundHazard.ClearAll();
            PheromoneTrail.ClearAll();
            Pickup.ClearAll();

            // Destroy only happens at the end of the frame; switching the old rooms off keeps them out of the new navmesh.
            foreach (var room in _rooms.Values)
            {
                room.gameObject.SetActive(false);
                Destroy(room.gameObject);
            }
            _rooms.Clear();

            CurrentNode = null;
            _fightNode = null;
            _fightRoom = null;
            Boss = null;
            Dare = null;
            DareFight = false;
            _dareOffered = false;
            ExtractPortal = null;
            _portals.Clear();
            IsLocked = false;
            ExitOpen = false;
            _exitArmed = false;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (Layout == null || player == null) return;

            Vector3 position = player.transform.position;
            if (Layout.rooms.TryGetValue(CellOf(position), out var node) && node != CurrentNode) Enter(node);
            if (CurrentNode == null) return;

            Room room = CurrentRoom;

            // A big arena is four squares that fight as one room: the exit room's.
            bool inArena = Layout.InArena(CurrentNode.cell);
            RoomNode fightNode = inArena ? Layout.exit : CurrentNode;
            Room fightRoom = inArena ? _rooms[Layout.exit.cell] : room;
            bool inside = inArena ? InsideArena(position, lockMargin) : room.ContainsInside(position, lockMargin);
            if (!IsLocked && !fightNode.cleared && inside) Lock(fightNode, fightRoom, position);
            // When the boss falls, the fight is over: whatever it brought with it goes too.
            if (IsLocked && Boss != null && Boss.Health.IsDead)
            {
                foreach (var enemy in _live)
                {
                    if (enemy != Boss && enemy.gameObject.activeSelf && !enemy.Health.IsDead) enemy.Vanish();
                }
            }
            if (IsLocked && AllDead()) Clear();

            // The portal room: after a biome boss the player chooses to go home or push on.
            if (ExtractPortal != null && _extractPad.SteppedOn(ExtractPortal.transform.position, position))
            {
                ExtractChosen?.Invoke();
                return;
            }
            foreach (var portal in _portals)
            {
                if (!portal.pad.SteppedOn(portal.gate.transform.position, position)) continue;
                biome = portal.leadsTo;
                depth++;
                BuildFloor(FloorNumber + 1);
                return;
            }

            if (ExitOpen && ExtractPortal == null && CurrentNode.type == RoomType.Exit && room.ExitHole != null)
            {
                Vector3 toExit = room.ExitHole.transform.position - position;
                toExit.y = 0f;
                float distance = toExit.magnitude;

                // If the hole opens under the player's feet, they must step off it first. Going down is always a choice.
                if (distance > exitRadius + 0.5f) _exitArmed = true;
                else if (_exitArmed && distance <= exitRadius) BuildFloor(FloorNumber + 1);
            }
        }

        [Tooltip("Rooms further than this many squares from the player are not drawn. Two covers the longest view through two doorways in a line.")]
        public int drawDistance = 2;

        void Enter(RoomNode node)
        {
            CurrentNode = node;
            node.visited = true;

            // Every room is a closed box, so far ones can't be seen: only the ones near the player are drawn.
            foreach (var pair in _rooms)
            {
                Vector2Int away = pair.Key - node.cell;
                pair.Value.SetDrawn(Mathf.Abs(away.x) + Mathf.Abs(away.y) <= drawDistance);
            }
            RoomChanged?.Invoke(this);
        }

        RoomNode _fightNode;
        Room _fightRoom;

        /// <summary>The middle of the room a boss fights in: the middle of all four squares for a big arena.</summary>
        public Vector3 ArenaCentre
        {
            get
            {
                if (Layout == null) return Vector3.zero;
                if (!Layout.hasArena) return _rooms[Layout.exit.cell].Centre;
                return CellCentre(Layout.arenaOrigin) + new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);
            }
        }

        // Past the arena's own doorways and a little way in, measured on the whole two-by-two block.
        bool InsideArena(Vector3 position, float margin)
        {
            Vector3 local = position - ArenaCentre;
            float limit = cellSize - RoomShape.TileSize - margin;
            return Mathf.Abs(local.x) <= limit && Mathf.Abs(local.z) <= limit;
        }

        // Every room that shuts for this fight: the one room, or all four squares of an arena.
        void SetFightLocked(bool locked)
        {
            if (_fightNode != null && Layout.InArena(_fightNode.cell))
            {
                foreach (var pair in _rooms) if (Layout.InArena(pair.Key)) pair.Value.SetLocked(locked);
            }
            else if (_fightRoom != null)
            {
                _fightRoom.SetLocked(locked);
            }
        }

        void Lock(RoomNode node, Room room, Vector3 playerPosition)
        {
            IsLocked = true;
            _fightNode = node;
            _fightRoom = room;
            SetFightLocked(true);

            // Boss floors: the exit room holds the boss, alone. It brings its own company.
            BossEnemy bossPrefab = node.type == RoomType.Exit ? BossForFloor() : null;
            if (bossPrefab != null)
            {
                Vector3 centre = ArenaCentre;
                Vector3 away = centre - playerPosition;
                away.y = 0f;
                // Not on top of the player if they ran to the middle.
                Vector3 spot = away.magnitude < 6f ? centre + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * 6f : centre;
                Boss = (BossEnemy)Spawn(bossPrefab, spot, playerPosition);
                if (Boss.Mutations != null && depth > 1)
                {
                    var bossRng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
                    Boss.Mutations.Apply(MutationBudget.Roll(bossRng, biome.mutations, depth - 1));
                }
                RoomChanged?.Invoke(this);
                return;
            }

            SpawnWave(room, playerPosition, Twist == FloorTwist.Crowded ? FloorTwists.CrowdedExtra : 0, Twist == FloorTwist.Promotions, 0);
            RoomChanged?.Invoke(this);
        }

        // One room's worth of enemies, put down away from the player.
        void SpawnWave(Room room, Vector3 playerPosition, int extra, bool forceElite, int extraBudget)
        {
            int count = biome.EnemiesFor(FloorInBiome, room.Shape.interior.Count);
            count += UnityEngine.Random.Range(0, 2) + extra;

            // Any clear tile far enough from the player can hold an enemy. In a tiny room, fall back to the farthest tiles.
            var points = new List<Vector3>();
            foreach (var tile in room.Shape.free)
            {
                Vector3 point = room.TileWorld(tile);
                if ((point - playerPosition).sqrMagnitude >= minSpawnDistance * minSpawnDistance) points.Add(point);
            }
            if (points.Count < count)
            {
                points.Clear();
                foreach (var tile in room.Shape.free) points.Add(room.TileWorld(tile));
                points.Sort((a, b) => (b - playerPosition).sqrMagnitude.CompareTo((a - playerPosition).sqrMagnitude));
                if (points.Count > count) points.RemoveRange(count, points.Count - count);
            }

            var rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
            int budget = MutationBudget.MaxPoints(depth, FloorInBiome) + extraBudget;
            // Some rooms have an elite: the first ant in gets a full load of mutations, more health and a crown.
            float eliteChance = eliteChanceOverride >= 0f ? eliteChanceOverride : MutationBudget.EliteChance(depth, FloorInBiome);
            bool elite = forceElite || rng.NextDouble() < eliteChance;
            for (int i = 0; i < count && points.Count > 0; i++)
            {
                int pick = UnityEngine.Random.Range(0, points.Count);
                Enemy enemy = Spawn(biome.PickEnemy(FloorInBiome, rng), points[pick], playerPosition);
                if (elite && i == 0)
                {
                    if (enemy.Mutations != null) enemy.Mutations.Apply(MutationBudget.RollFull(rng, biome.mutations, budget + 1));
                    enemy.MakeElite(biome.eliteMaterial);
                }
                else if (enemy.Mutations != null)
                {
                    enemy.Mutations.Apply(MutationBudget.Roll(rng, biome.mutations, budget));
                }
                points.RemoveAt(pick);
            }
        }

        string PortalPrompt(Vector3 playerPosition)
        {
            if (ExtractPortal != null && _extractPad.Distance(ExtractPortal.transform.position, playerPosition) < 3.5f)
            {
                if (biome != null && biome.isBottom) return "The way back up. Step in to go home.";
                float multiplier = RunDirector.RewardMultiplier(depth);
                int worth = Mathf.RoundToInt(RunState.Samples * multiplier);
                return multiplier > 1f
                    ? $"Extract: go home with {RunState.Samples} samples ×{multiplier:0.#} = {worth}. Step in to leave."
                    : $"Extract: go home with {worth} samples. Step in to leave.";
            }
            foreach (var portal in _portals)
            {
                if (portal.pad.Distance(portal.gate.transform.position, playerPosition) >= 3.5f) continue;
                string where = portal.leadsTo == biome ? $"{biome.displayName} again" : portal.leadsTo.displayName;
                string theme = string.IsNullOrEmpty(portal.leadsTo.mutationTheme) ? "" : $" ({portal.leadsTo.mutationTheme})";
                return $"Go deeper: {where}{theme}, depth {depth + 1}. Samples will be worth ×{RunDirector.RewardMultiplier(depth + 1):0.#}; " +
                       $"enemies carry up to {MutationBudget.MaxPoints(depth + 1, 1)} mutation points. Step in to push on.";
            }
            return null;
        }

        BossEnemy BossForFloor()
        {
            if (!FloorLayout.IsBossFloor(FloorNumber)) return null;
            return FloorInBiome == 10 ? biome.boss : biome.miniBoss;
        }

        // Larvae, eggs, hatchlings and guards: anything an enemy brings into the fight joins it, so the room stays
        // locked until they are dealt with too.
        void OnMinion(Enemy source, Enemy prefab, Vector3 position, bool mutate) => AddToFight(prefab, position, mutate, source.transform.position);

        /// <summary>
        /// Brings one more enemy into the fight going on in the current room; the doors stay shut until it is dead too.
        /// A null prefab means a random ant from the biome. Returns null if there is no fight to join.
        /// </summary>
        public Enemy AddToFight(Enemy prefab, Vector3 position, bool mutate, Vector3? fallback = null)
        {
            if (!IsLocked || CurrentNode == null) return null;
            var rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
            if (prefab == null) prefab = biome.PickEnemy(FloorInBiome, rng);
            if (prefab == null) return null;

            // Keep it on the floor: the nearest walkable spot to where it was asked for.
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out var hit, 4f, UnityEngine.AI.NavMesh.AllAreas) && hit.position.y < 0.5f) position = hit.position;
            else if (fallback.HasValue) position = fallback.Value;

            var player = PlayerController.Instance;
            Enemy minion = Spawn(prefab, position, player != null ? player.transform.position : position);
            if (mutate && minion.Mutations != null)
            {
                minion.Mutations.Apply(MutationBudget.Roll(rng, biome.mutations, MutationBudget.MaxPoints(depth, FloorInBiome)));
            }
            return minion;
        }

        // Swarm Call: bring in the biome's swarm enemy on the open tiles nearest the caller. They join the fight,
        // so the room stays locked until they are dead too.
        void OnSummon(Enemy source, int count)
        {
            if (!IsLocked || CurrentNode == null || biome.swarmEnemy == null) return;
            Room room = CurrentRoom;
            Vector3 origin = source.transform.position;

            var tiles = new List<Vector3>();
            foreach (var tile in room.Shape.free) tiles.Add(room.TileWorld(tile));
            tiles.Sort((a, b) => (a - origin).sqrMagnitude.CompareTo((b - origin).sqrMagnitude));

            var player = PlayerController.Instance;
            Vector3 lookAt = player != null ? player.transform.position : origin;
            for (int i = 0; i < count && i < tiles.Count; i++) Spawn(biome.swarmEnemy, tiles[i], lookAt);
        }

        Enemy Spawn(Enemy prefab, Vector3 position, Vector3 lookAt)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new Pool<Enemy>(prefab, _enemiesRoot);
                _pools.Add(prefab, pool);
            }

            Enemy enemy = pool.Get();
            Vector3 facing = lookAt - position;
            facing.y = 0f;
            enemy.transform.SetPositionAndRotation(position + Vector3.up * 0.05f,
                facing.sqrMagnitude > 0.01f ? Quaternion.LookRotation(facing) : Quaternion.identity);
            enemy.gameObject.SetActive(true);
            _live.Add(enemy);
            _livePools.Add(pool);
            return enemy;
        }

        void AddPortal(Room room, Vector3 middle, BiomeDefinition leadsTo, GameObject prop, Vector3 offset)
        {
            var gate = Instantiate(prop, middle + offset, Quaternion.identity, room.transform);
            _portals.Add(new BiomePortal { gate = gate, leadsTo = leadsTo });
            string where = leadsTo == biome ? "DEEPER" : leadsTo.displayName.ToUpperInvariant();
            string worth = $"depth {depth + 1}  ·  ×{RunDirector.RewardMultiplier(depth + 1):0.#}";
            PortalTag.Create(gate.transform, () => where + "\n" + worth, DeeperColor);
        }

        static readonly Color HomeColor = new Color(0.55f, 0.95f, 1f);
        static readonly Color DeeperColor = new Color(1f, 0.6f, 0.45f);

        // What going home now would bank, kept up to date as the boss's samples are picked up.
        string HomeTag()
        {
            int worth = Mathf.RoundToInt(RunState.Samples * RunDirector.RewardMultiplier(depth));
            return "GO HOME\n" + worth + (worth == 1 ? " sample" : " samples");
        }

        bool AllDead()
        {
            foreach (var enemy in _live)
            {
                // Something that vanished (a hatched egg, a grown larva) counts as gone.
                if (enemy.gameObject.activeSelf && !enemy.Health.IsDead) return false;
            }
            return true;
        }

        void Clear()
        {
            Room room = _fightRoom;
            RoomNode node = _fightNode;
            for (int i = 0; i < _live.Count; i++) _livePools[i].Release(_live[i]);
            _live.Clear();
            _livePools.Clear();

            IsLocked = false;
            node.cleared = true;
            SetFightLocked(false);
            Vector3 middle = node.type == RoomType.Exit && FloorLayout.IsBossFloor(FloorNumber) ? ArenaCentre : room.Centre;

            var assets = GameAssets.Instance;
            if (DareFight)
            {
                // The prize for asking for it: an item where the bell stood, and a handful of coins.
                DareFight = false;
                if (assets != null && assets.items != null)
                {
                    var picks = assets.items.RandomCommon(new System.Random(UnityEngine.Random.Range(0, int.MaxValue)), 1);
                    if (picks.Count > 0) Pickup.Item(picks[0], room.Centre, 0);
                }
                for (int i = 0; i < DareCoins; i++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.4f;
                    Pickup.Coin(room.Centre + new Vector3(offset.x, 0f, offset.y), 1);
                }
                DareWon?.Invoke(this);
            }
            else if (node.type == RoomType.Normal && !_dareOffered && FloorNumber >= 2 && !room.Shape.isArenaPart && assets != null
                     && UnityEngine.Random.value < (dareChanceOverride >= 0f ? dareChanceOverride : DareChanceForAll ?? dareChance))
            {
                _dareOffered = true;
                Dare = DareBell.Create(room, room.Centre, biome.eliteMaterial != null ? biome.eliteMaterial : assets.coin, assets.coin);
            }

            if (node.type == RoomType.Exit)
            {
                ExitOpen = true;
                if (biome.isBottom && biome.extractPortalProp != null)
                {
                    // There is nothing under the bottom: the only way on is back up.
                    ExtractPortal = Instantiate(biome.extractPortalProp, middle, Quaternion.identity, room.transform);
                    PortalTag.Create(ExtractPortal.transform, () => "BACK UP", HomeColor);
                }
                else if (FloorInBiome == 10 && biome.extractPortalProp != null && biome.deeperPortalProp != null)
                {
                    // The biome boss is dead: two portals instead of a hole in the floor.
                    // The way home on the left, this biome again on the right, and the other biomes along the far side.
                    ExtractPortal = Instantiate(biome.extractPortalProp, middle + new Vector3(-4f, 0f, 0f), Quaternion.identity, room.transform);
                    PortalTag.Create(ExtractPortal.transform, HomeTag, HomeColor);
                    AddPortal(room, middle, biome, biome.deeperPortalProp, new Vector3(4f, 0f, 0f));

                    var others = AllBiomes();
                    others.Remove(biome);
                    others.RemoveAll(other => !Achievements.BiomeUnlocked(other.id));
                    for (int i = others.Count - 1; i > 0; i--)
                    {
                        int j = UnityEngine.Random.Range(0, i + 1);
                        (others[i], others[j]) = (others[j], others[i]);
                    }
                    int offered = Mathf.Min(otherPortals, others.Count);
                    for (int i = 0; i < offered; i++)
                    {
                        float x = (i - (offered - 1) * 0.5f) * 7f;
                        GameObject prop = others[i].portalProp != null ? others[i].portalProp : biome.deeperPortalProp;
                        AddPortal(room, middle, others[i], prop, new Vector3(x, 0f, 7f));
                    }
                }
                else if (room.ExitHole != null)
                {
                    room.ExitHole.SetActive(true);
                }
            }

            RoomChanged?.Invoke(this);
        }
    }
}
