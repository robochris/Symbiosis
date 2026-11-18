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

        public BiomeDefinition biome;
        /// <summary>Distance between room centres: one room's full footprint.</summary>
        public float cellSize => RoomShape.Size * RoomShape.TileSize;
        [Tooltip("0 = a different floor every time. Any other number repeats the same run of floors.")]
        public int fixedSeed = 0;
        [Tooltip("How far past the doorway the player must be before the doors lock.")]
        public float lockMargin = 1.5f;
        [Tooltip("Enemies never appear closer to the player than this.")]
        public float minSpawnDistance = 6f;
        public float exitRadius = 1.4f;
        [Tooltip("How many biomes into the run this is. Raises the mutation budget: 1 point at depth 1, up to 5 at depth 5.")]
        public int depth = 1;

        public int FloorNumber { get; private set; }
        public FloorLayout Layout { get; private set; }
        public RoomNode CurrentNode { get; private set; }
        public Room CurrentRoom => CurrentNode != null ? _rooms[CurrentNode.cell] : null;
        public bool IsLocked { get; private set; }
        public bool ExitOpen { get; private set; }
        public IReadOnlyList<Enemy> LiveEnemies => _live;
        public int FloorsBuilt { get; private set; }

        readonly Dictionary<Vector2Int, Room> _rooms = new Dictionary<Vector2Int, Room>();
        readonly Dictionary<Enemy, Pool<Enemy>> _pools = new Dictionary<Enemy, Pool<Enemy>>();
        readonly List<Enemy> _live = new List<Enemy>();
        readonly List<Pool<Enemy>> _livePools = new List<Pool<Enemy>>();
        Transform _roomsRoot;
        Transform _enemiesRoot;
        NavMeshSurface _surface;
        bool _exitArmed;

        void Awake()
        {
            Instance = this;

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
            Enemy.SummonRequested += OnSummon;
        }

        void OnDisable()
        {
            Enemy.SummonRequested -= OnSummon;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            BuildFloor(1);
        }

        public Room RoomAt(Vector2Int cell) => _rooms.TryGetValue(cell, out var room) ? room : null;

        public Vector3 CellCentre(Vector2Int cell) => new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

        public Vector2Int CellOf(Vector3 position) =>
            new Vector2Int(Mathf.RoundToInt(position.x / cellSize), Mathf.RoundToInt(position.z / cellSize));

        public void BuildFloor(int number)
        {
            ClearFloor();

            FloorNumber = number;
            int seed = fixedSeed != 0 ? fixedSeed + number : UnityEngine.Random.Range(1, int.MaxValue);
            Layout = FloorLayout.Generate(seed, FloorLayout.RoomCountForFloor(number));

            foreach (var node in Layout.ordered)
            {
                RoomShape shape = RoomShape.GenerateFor(Layout, node, number);
                Room room = Room.Create(shape, biome, _roomsRoot, CellCentre(node.cell));
                room.name = $"{node.type} {node.cell.x},{node.cell.y}";
                _rooms.Add(node.cell, room);

                // Only fight rooms need clearing.
                node.cleared = node.type != RoomType.Normal && node.type != RoomType.Exit;
            }

            Physics.SyncTransforms();
            _surface.BuildNavMesh();

            var player = PlayerController.Instance;
            if (player != null)
            {
                player.Teleport(_rooms[Layout.start.cell].Centre + Vector3.up * 0.05f);
                if (CameraFollow.Instance != null) CameraFollow.Instance.Snap();
            }

            FloorsBuilt++;
            FloorBuilt?.Invoke(this);
            Enter(Layout.start);
        }

        void ClearFloor()
        {
            for (int i = 0; i < _live.Count; i++) _livePools[i].Release(_live[i]);
            _live.Clear();
            _livePools.Clear();
            GroundHazard.ClearAll();
            PheromoneTrail.ClearAll();

            // Destroy only happens at the end of the frame; switching the old rooms off keeps them out of the new navmesh.
            foreach (var room in _rooms.Values)
            {
                room.gameObject.SetActive(false);
                Destroy(room.gameObject);
            }
            _rooms.Clear();

            CurrentNode = null;
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

            if (!IsLocked && !CurrentNode.cleared && room.ContainsInside(position, lockMargin)) Lock(room, position);
            if (IsLocked && AllDead()) Clear(room);

            if (ExitOpen && CurrentNode.type == RoomType.Exit && room.ExitHole != null)
            {
                Vector3 toExit = room.ExitHole.transform.position - position;
                toExit.y = 0f;
                float distance = toExit.magnitude;

                // If the hole opens under the player's feet, they must step off it first. Going down is always a choice.
                if (distance > exitRadius + 0.5f) _exitArmed = true;
                else if (_exitArmed && distance <= exitRadius) BuildFloor(FloorNumber + 1);
            }
        }

        void Enter(RoomNode node)
        {
            CurrentNode = node;
            node.visited = true;
            RoomChanged?.Invoke(this);
        }

        void Lock(Room room, Vector3 playerPosition)
        {
            IsLocked = true;
            room.SetLocked(true);

            int count = biome.baseEnemiesPerRoom + (FloorNumber - 1) / Mathf.Max(1, biome.floorsPerExtraEnemy);
            count += UnityEngine.Random.Range(0, 2);

            // Any open tile far enough from the player can hold an enemy. In a tiny room, fall back to the farthest tiles.
            var points = new List<Vector3>();
            foreach (var tile in room.Shape.interior)
            {
                Vector3 point = room.TileWorld(tile);
                if ((point - playerPosition).sqrMagnitude >= minSpawnDistance * minSpawnDistance) points.Add(point);
            }
            if (points.Count < count)
            {
                points.Clear();
                foreach (var tile in room.Shape.interior) points.Add(room.TileWorld(tile));
                points.Sort((a, b) => (b - playerPosition).sqrMagnitude.CompareTo((a - playerPosition).sqrMagnitude));
                if (points.Count > count) points.RemoveRange(count, points.Count - count);
            }

            var rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
            int budget = MutationBudget.MaxPoints(depth, FloorNumber);
            for (int i = 0; i < count && points.Count > 0; i++)
            {
                int pick = UnityEngine.Random.Range(0, points.Count);
                Enemy enemy = Spawn(biome.PickEnemy(FloorNumber, rng), points[pick], playerPosition);
                if (enemy.Mutations != null) enemy.Mutations.Apply(MutationBudget.Roll(rng, biome.mutations, budget));
                points.RemoveAt(pick);
            }

            RoomChanged?.Invoke(this);
        }

        // Swarm Call: bring in the biome's swarm enemy on the open tiles nearest the caller. They join the fight,
        // so the room stays locked until they are dead too.
        void OnSummon(Enemy source, int count)
        {
            if (!IsLocked || CurrentNode == null || biome.swarmEnemy == null) return;
            Room room = CurrentRoom;
            Vector3 origin = source.transform.position;

            var tiles = new List<Vector3>();
            foreach (var tile in room.Shape.interior) tiles.Add(room.TileWorld(tile));
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

        bool AllDead()
        {
            foreach (var enemy in _live)
            {
                if (!enemy.Health.IsDead) return false;
            }
            return true;
        }

        void Clear(Room room)
        {
            for (int i = 0; i < _live.Count; i++) _livePools[i].Release(_live[i]);
            _live.Clear();
            _livePools.Clear();

            IsLocked = false;
            CurrentNode.cleared = true;
            room.SetLocked(false);

            if (CurrentNode.type == RoomType.Exit)
            {
                ExitOpen = true;
                if (room.ExitHole != null) room.ExitHole.SetActive(true);
            }

            RoomChanged?.Invoke(this);
        }
    }
}
