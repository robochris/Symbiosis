using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>What kind of fight room this is. Decides what stands on its cover tiles and what else is in it.</summary>
    public enum RoomFeature
    {
        /// <summary>Open chamber: pebbles for cover, room to move.</summary>
        Open,
        /// <summary>Food store: giant crumbs and a sugar cube to hide behind.</summary>
        FoodStore,
        /// <summary>Root hall: thick roots from floor to ceiling, which block sight completely.</summary>
        RootHall,
        /// <summary>Nursery: larva pods. Pop them for coins, or some hatch during the fight.</summary>
        Nursery,
        /// <summary>Dew room: droplets fall on marked spots and slow whoever is under them.</summary>
        DewRoom
    }

    /// <summary>Puts the things that stand in a room on its floor: cover on the cover tiles, and whatever its feature adds.</summary>
    public static class RoomDressing
    {
        public static string NameOf(RoomFeature feature)
        {
            switch (feature)
            {
                case RoomFeature.FoodStore: return "Food store";
                case RoomFeature.RootHall: return "Root hall";
                case RoomFeature.Nursery: return "Nursery";
                case RoomFeature.DewRoom: return "Dew room";
                default: return "Open chamber";
            }
        }

        public static void Build(Room room, BiomeDefinition biome)
        {
            RoomShape shape = room.Shape;
            var rng = new System.Random(shape.seed ^ 0x5bd1e995);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            bool roots = shape.feature == RoomFeature.RootHall && biome.rootMaterial != null;
            bool pods = shape.feature == RoomFeature.Nursery && biome.podMaterial != null;
            bool food = shape.feature == RoomFeature.FoodStore && biome.crumbMaterial != null;
            bool sugarPlaced = biome.sugarMaterial == null;

            foreach (var tile in shape.cover)
            {
                Vector3 at = RoomShape.TileLocal(tile) + new Vector3(Range(-0.15f, 0.15f), 0f, Range(-0.15f, 0.15f));

                if (roots)
                {
                    // Floor to ceiling. The swelling at the foot is only for looks.
                    float width = Range(1f, 1.3f);
                    var root = Piece(PrimitiveType.Cylinder, "Root", room, at + Vector3.up * biome.wallHeight * 0.5f,
                        new Vector3(width, biome.wallHeight * 0.5f, width), biome.rootMaterial, true);
                    root.transform.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                    Piece(PrimitiveType.Sphere, "Root Foot", room, at, new Vector3(width * 1.7f, 0.9f, width * 1.7f), biome.rootMaterial, false);
                    Piece(PrimitiveType.Sphere, "Root Top", room, at + Vector3.up * biome.wallHeight, new Vector3(width * 1.9f, 1.2f, width * 1.9f), biome.rootMaterial, false);
                }
                else if (pods)
                {
                    float width = Range(1.15f, 1.45f);
                    var pod = Piece(PrimitiveType.Sphere, "Larva Pod", room, at + Vector3.up * width * 0.38f, Vector3.one * width, biome.podMaterial, true);
                    var health = pod.AddComponent<Health>();
                    health.team = Team.Enemy;
                    health.SetMaxHealth(2, true);
                    var larvaPod = pod.AddComponent<LarvaPod>();
                    larvaPod.room = room;
                    larvaPod.hatchling = biome.podHatchling;
                    // About two in five are alive, and hatch part-way through the fight if nobody pops them first.
                    larvaPod.hatches = biome.podHatchling != null && rng.Next(5) < 2;
                    larvaPod.hatchAfter = Range(5f, 16f);
                }
                else if (food && !sugarPlaced)
                {
                    sugarPlaced = true;
                    float side = Range(1.5f, 1.8f);
                    var cube = Piece(PrimitiveType.Cube, "Sugar Cube", room, at + Vector3.up * side * 0.45f, Vector3.one * side, biome.sugarMaterial, true);
                    cube.transform.localRotation = Quaternion.Euler(0f, Range(0f, 90f), 0f);
                }
                else
                {
                    // A half-buried lump about chest high. Crumbs are bigger and knobbly; pebbles are smooth.
                    Material material = food ? biome.crumbMaterial : biome.coverMaterial;
                    float width = food ? Range(1.7f, 2f) : Range(1.5f, 1.9f);
                    Piece(PrimitiveType.Sphere, food ? "Crumb" : "Pebble", room, at + Vector3.up * width * Range(0.2f, 0.3f), Vector3.one * width, material, true);

                    int bits = food ? 3 : rng.Next(1, 4);
                    for (int i = 0; i < bits; i++)
                    {
                        float angle = Range(0f, Mathf.PI * 2f);
                        if (food)
                        {
                            // Knobs stuck to the crumb itself.
                            float size = width * Range(0.45f, 0.65f), reach = width * 0.33f;
                            Piece(PrimitiveType.Sphere, "Crumb Knob", room,
                                at + new Vector3(Mathf.Cos(angle) * reach, width * Range(0.25f, 0.55f), Mathf.Sin(angle) * reach), Vector3.one * size, material, false);
                        }
                        else
                        {
                            float size = Range(0.25f, 0.55f), reach = width * 0.5f + Range(0.15f, 0.5f);
                            Piece(PrimitiveType.Sphere, "Grit", room,
                                at + new Vector3(Mathf.Cos(angle) * reach, size * 0.2f, Mathf.Sin(angle) * reach), new Vector3(size, size * 0.7f, size), material, false);
                        }
                    }
                }
            }

            if (shape.feature == RoomFeature.DewRoom && biome.dewMaterial != null)
            {
                foreach (var tile in shape.spots)
                {
                    var spot = new GameObject("Dew Spot");
                    spot.transform.SetParent(room.transform, false);
                    spot.transform.localPosition = RoomShape.TileLocal(tile);
                    var drip = spot.AddComponent<DewDrip>();
                    drip.room = room;
                    drip.height = biome.wallHeight - 0.4f;
                    drip.interval = Range(3.5f, 5.5f);
                    drip.Build(biome.dewMaterial, Range(0f, 3f));
                }
            }
        }

        internal static GameObject Piece(PrimitiveType type, string name, Room room, Vector3 localPosition, Vector3 scale, Material material, bool solid)
        {
            var piece = GameObject.CreatePrimitive(type);
            piece.name = name;
            // Straight away, not at the end of the frame: the navmesh is baked this same frame.
            if (!solid) Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.transform.SetParent(room.transform, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            return piece;
        }
    }

    /// <summary>
    /// A larva pod in a nursery. Shoot it and it pops, usually leaving a coin or two. Some are alive: if the fight
    /// goes on long enough they swell and hatch a larva, which grows into a worker ant unless it is dealt with.
    /// </summary>
    public class LarvaPod : MonoBehaviour
    {
        public static readonly List<LarvaPod> All = new List<LarvaPod>();
        /// <summary>Counts for the tests: pods shot open, and pods that hatched by themselves.</summary>
        public static int Popped, Hatched;

        public Room room;
        public LarvaEnemy hatchling;
        public bool hatches;
        [Tooltip("Seconds of fighting in this room before it hatches.")]
        public float hatchAfter = 10f;
        [Range(0f, 1f)] public float coinChance = 0.65f;
        [Range(0f, 1f)] public float sampleChance = 0.25f;

        Health _health;
        Vector3 _scale;
        float _fought;

        void Awake()
        {
            _health = GetComponent<Health>();
            _health.Died += Pop;
            _scale = transform.localScale;
        }

        void Start() => _scale = transform.localScale;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            var floors = FloorManager.Instance;
            bool fighting = hatches && floors != null && floors.IsLocked && floors.CurrentRoom == room;
            if (fighting) _fought += Time.deltaTime;

            // All of them breathe a little. A live one swells and shakes in its last three seconds.
            float swell = 1f + 0.04f * Mathf.Sin(Time.time * 2.5f + transform.position.x);
            if (hatches)
            {
                float close = Mathf.InverseLerp(hatchAfter - 3f, hatchAfter, _fought);
                swell += 0.3f * close + 0.05f * close * Mathf.Sin(Time.time * 30f);
            }
            transform.localScale = _scale * swell;

            if (fighting && _fought >= hatchAfter)
            {
                Hatched++;
                floors.AddToFight(hatchling, transform.position, false);
                Destroy(gameObject);
            }
        }

        void Pop()
        {
            Popped++;
            Sfx.Play(SfxId.EnemyDie, 0.6f);
            Vector3 at = transform.position;
            at.y = 0f;
            if (Random.value < coinChance) Pickup.Coin(at, Random.Range(1, 3));
            if (Random.value < sampleChance) Pickup.Sample(at + new Vector3(0.4f, 0f, 0.2f), 1);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// A marked spot in a dew room. Every few seconds a droplet falls on it from the ceiling; if the player is under
    /// it when it lands they are soaked and slowed for a moment. It only runs while the player is in the room.
    /// </summary>
    public class DewDrip : MonoBehaviour
    {
        public Room room;
        public float radius = 1.5f;
        [Tooltip("Seconds from one droplet landing to the next.")]
        public float interval = 4.5f;
        [Tooltip("Seconds the droplet takes to fall: the warning.")]
        public float fallTime = 1f;
        public float height = 5.6f;
        [Tooltip("The player's speed while soaked: 0.5 = half.")]
        public float slowTo = 0.5f;
        public float slowSeconds = 2.5f;

        public int Landed { get; private set; }

        Transform _drop;
        Material _material;
        float _clock;

        public void Build(Material material, float startAt)
        {
            _material = material;
            _clock = startAt;

            var mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mark.name = "Wet Patch";
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            mark.transform.SetParent(transform, false);
            mark.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            mark.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            mark.GetComponent<Renderer>().sharedMaterial = material;

            var drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = "Droplet";
            Object.DestroyImmediate(drop.GetComponent<Collider>());
            drop.transform.SetParent(transform, false);
            drop.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            drop.GetComponent<Renderer>().sharedMaterial = material;
            drop.SetActive(false);
            _drop = drop.transform;
        }

        void Update()
        {
            var floors = FloorManager.Instance;
            if (_drop == null || floors == null || floors.CurrentRoom != room)
            {
                if (_drop != null && _drop.gameObject.activeSelf) _drop.gameObject.SetActive(false);
                return;
            }

            _clock += Time.deltaTime;
            float falling = _clock - (interval - fallTime);
            if (falling < 0f)
            {
                if (_drop.gameObject.activeSelf) _drop.gameObject.SetActive(false);
                return;
            }

            if (_clock >= interval)
            {
                _clock = 0f;
                _drop.gameObject.SetActive(false);
                Land();
                return;
            }

            // Hangs for a moment, then speeds up, like a real drip.
            float t = falling / fallTime;
            _drop.gameObject.SetActive(true);
            _drop.localPosition = new Vector3(0f, Mathf.Lerp(height, 0.4f, t * t), 0f);
        }

        void Land()
        {
            Landed++;
            var player = PlayerController.Instance;
            if (player == null || player.Health.IsDead || player.IsHovering) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > radius * radius) return;
            player.Slow(slowTo, slowSeconds, _material);
            Sfx.Play(SfxId.Blocked, 0.6f);
        }
    }
}
