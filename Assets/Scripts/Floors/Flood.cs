using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The sink's floods. During a fight in a world that floods, every so often the water rises across the room: first
    /// a shimmer and a gurgle (time to get up on a ledge), then the room fills for a few seconds. Anyone left on the
    /// floor is soaked: slowed while it lasts, and a point of health the moment it comes in. Then it drains away.
    /// Ledges are the dry places. Added to the floor manager on a flooding world.
    /// </summary>
    public class Flood : MonoBehaviour
    {
        public enum Phase { Dry, Rising, Flooded, Draining }

        public float between = 15f, warning = 2.5f, flooded = 4f, draining = 1.5f;
        public float depth = 0.45f;
        public float slow = 0.55f;
        /// <summary>Tests pin the wait between floods.</summary>
        public static float? BetweenForAll;

        public Phase Current { get; private set; }
        public int Floods { get; private set; }
        public int Soaked { get; private set; }

        FloorManager _floors;
        GameObject _water;
        Material _material;
        float _timer;
        bool _hurtThisFlood;
        static bool _told;

        void Awake()
        {
            _floors = GetComponent<FloorManager>();
            Current = Phase.Dry;
            _timer = BetweenForAll ?? between;
        }

        void OnDisable() => Hide();

        void Update()
        {
            // Only where there is somewhere dry to get to: rooms with ledges. A boss's arena has none, so it never floods.
            bool fighting = _floors != null && _floors.biome != null && _floors.biome.floods && _floors.IsLocked && _floors.CurrentRoom != null
                && Ledges.SpotsIn(_floors.CurrentRoom).Count > 0;
            if (!fighting)
            {
                if (Current != Phase.Dry) Hide();
                Current = Phase.Dry;
                _timer = BetweenForAll ?? between;
                return;
            }

            _timer -= Time.deltaTime;
            switch (Current)
            {
                case Phase.Dry:
                    if (_timer > 0f) break;
                    Current = Phase.Rising;
                    _timer = warning;
                    Show(_floors.CurrentRoom);
                    Sfx.Play(SfxId.Blocked, 0.5f);
                    if (!_told)
                    {
                        _told = true;
                        var hud = FindAnyObjectByType<Hud>();
                        if (hud != null) hud.Toast("The water is rising! Get up on a ledge.", 3f);
                    }
                    break;
                case Phase.Rising:
                    SetLevel(Mathf.Lerp(-0.05f, depth * 0.3f, 1f - _timer / warning), 0.35f);
                    if (_timer > 0f) break;
                    Current = Phase.Flooded;
                    _timer = flooded;
                    _hurtThisFlood = false;
                    Floods++;
                    break;
                case Phase.Flooded:
                    SetLevel(depth, 0.55f);
                    SoakPlayer();
                    if (_timer > 0f) break;
                    Current = Phase.Draining;
                    _timer = draining;
                    break;
                case Phase.Draining:
                    SetLevel(Mathf.Lerp(depth, -0.05f, 1f - _timer / draining), 0.45f);
                    if (_timer > 0f) break;
                    Hide();
                    Current = Phase.Dry;
                    _timer = BetweenForAll ?? between;
                    break;
            }
        }

        void SoakPlayer()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            // Up on a ledge, or in the air mid-dodge: dry.
            if (player.transform.position.y > depth + 0.2f || player.IsHovering) return;
            player.Slow(slow, 0.3f);
            if (_hurtThisFlood) return;
            _hurtThisFlood = true;
            Soaked++;
            player.Health.TakeDamage(1, Vector3.zero);
        }

        void Show(Room room)
        {
            if (_water == null)
            {
                _water = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _water.name = "Flood";
                Destroy(_water.GetComponent<Collider>());
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                _material = new Material(shader != null ? shader : Shader.Find("Standard"));
                // See-through, so the floor and anything on it show.
                _material.SetFloat("_Surface", 1f);
                _material.SetFloat("_Blend", 0f);
                _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _material.SetInt("_ZWrite", 0);
                _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                _water.GetComponent<Renderer>().sharedMaterial = _material;
                _water.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            float size = RoomShape.Size * RoomShape.TileSize;
            _water.transform.position = new Vector3(room.Centre.x, -0.05f, room.Centre.z);
            _water.transform.localScale = new Vector3(size, 0.02f, size);
            _water.SetActive(true);
        }

        void SetLevel(float y, float alpha)
        {
            if (_water == null) return;
            Vector3 at = _water.transform.position;
            _water.transform.position = new Vector3(at.x, y, at.z);
            var colour = new Color(0.35f, 0.6f, 0.95f, alpha);
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", colour);
            _material.color = colour;
        }

        void Hide()
        {
            if (_water != null) _water.SetActive(false);
        }
    }
}
