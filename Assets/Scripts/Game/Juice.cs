using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Small things that make it feel good: confetti and a chime when a room is cleared, puffs of dust from the feet
    /// while running (in the floor's own colour), and a beam of light over rare items lying about so they are seen
    /// from across the room.
    /// </summary>
    public class Juice : MonoBehaviour
    {
        static Juice _instance;
        public static int Confetti { get; private set; }
        float _nextStep, _nextBeams;
        Vector3 _lastFeet;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("Juice");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Juice>();
        }

        void OnEnable() => FloorManager.RoomCleared += OnRoomCleared;
        void OnDisable() => FloorManager.RoomCleared -= OnRoomCleared;

        void OnRoomCleared(FloorManager floors)
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            Confetti++;
            Vector3 at = player.transform.position + Vector3.up * 2.2f;
            Color[] colours = { new Color(1f, 0.4f, 0.5f), new Color(0.4f, 0.8f, 1f), new Color(1f, 0.85f, 0.3f), new Color(0.5f, 1f, 0.5f) };
            foreach (var colour in colours) Fx.Burst(at, colour, 10, 6f, 0.12f, 0.9f);
            Sfx.Play(SfxId.Item, 0.45f);
        }

        void Update()
        {
            var player = PlayerController.Instance;
            var floors = FloorManager.Instance;
            if (player == null) return;

            // Dust from the feet, now and then, while running.
            if (Time.time >= _nextStep)
            {
                float elapsed = Time.time - (_nextStep - 0.22f);
                _nextStep = Time.time + 0.22f;
                Vector3 velocity = (player.transform.position - _lastFeet) / Mathf.Max(0.05f, elapsed);
                _lastFeet = player.transform.position;
                velocity.y = 0f;
                if (velocity.magnitude > 2f && floors != null && floors.biome != null)
                {
                    Color dust = floors.biome.floorMaterial != null ? floors.biome.floorMaterial.color * 1.2f : new Color(0.7f, 0.65f, 0.6f);
                    Fx.Burst(player.transform.position + Vector3.up * 0.05f - velocity.normalized * 0.3f, dust, 3, 1.2f, 0.08f, 0.35f);
                }
            }

            // Rare items get a beam.
            if (Time.time >= _nextBeams)
            {
                _nextBeams = Time.time + 0.5f;
                foreach (var pickup in Pickup.All)
                {
                    if (pickup == null || pickup.kind != PickupKind.Item || pickup.item == null || !pickup.item.rare) continue;
                    if (pickup.transform.Find("Beam") != null) continue;
                    var beam = Part.Make(PrimitiveType.Cylinder, "Beam", pickup.transform, Vector3.zero, new Vector3(0.25f, 4f, 0.25f), new Color(1f, 0.9f, 0.5f), true);
                    // Upright whatever the pickup's own turn, and not spinning with it.
                    beam.AddComponent<StayUpright>();
                }
            }
        }
    }

    /// <summary>Keeps a part pointing straight up, standing on its parent, whatever the parent does.</summary>
    public class StayUpright : MonoBehaviour
    {
        void LateUpdate()
        {
            if (transform.parent == null) return;
            transform.rotation = Quaternion.identity;
            transform.position = transform.parent.position + Vector3.up * 4f;
        }
    }
}

namespace Shrunk
{
    /// <summary>A big bouncing arrow with START on it, over the portal, until the first run is started.</summary>
    public class StartHere : UnityEngine.MonoBehaviour
    {
        UnityEngine.Vector3 _home;

        public static StartHere Place(UnityEngine.Vector3 at)
        {
            var go = new UnityEngine.GameObject("Start Here");
            go.transform.position = at;
            var yellow = new UnityEngine.Color(1f, 0.85f, 0.2f);
            Part.Make(UnityEngine.PrimitiveType.Cube, "Shaft", go.transform, new UnityEngine.Vector3(0f, 0.6f, 0f), new UnityEngine.Vector3(0.25f, 0.9f, 0.25f), yellow, true);
            var head = Part.Make(UnityEngine.PrimitiveType.Cube, "Head", go.transform, new UnityEngine.Vector3(0f, 0f, 0f), new UnityEngine.Vector3(0.6f, 0.6f, 0.2f), yellow, true);
            head.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 0f, 45f);
            var label = new UnityEngine.GameObject("Label");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new UnityEngine.Vector3(0f, 1.5f, 0f);
            var text = label.AddComponent<UnityEngine.TextMesh>();
            var font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
            text.font = font;
            label.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = font.material;
            text.text = "START";
            text.fontSize = 64;
            text.characterSize = 0.08f;
            text.fontStyle = UnityEngine.FontStyle.Bold;
            text.anchor = UnityEngine.TextAnchor.MiddleCenter;
            text.color = yellow;
            label.AddComponent<FaceCamera>();
            go.AddComponent<FaceCamera>();
            var arrow = go.AddComponent<StartHere>();
            arrow._home = at;
            return arrow;
        }

        void Update()
        {
            transform.position = _home + UnityEngine.Vector3.up * UnityEngine.Mathf.Abs(UnityEngine.Mathf.Sin(UnityEngine.Time.time * 3f)) * 0.5f;
        }
    }
}
