using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The way into a boss's room, marked so it is never walked into by accident: a gold arch round the doorway, seen
    /// from the room before it, and a line of warning when the player comes close. Made by the floor as it is built;
    /// it has no collider, so nothing walks into it or paths round it.
    /// </summary>
    public class BossGate : MonoBehaviour
    {
        public static readonly List<BossGate> All = new List<BossGate>();

        public float noticeDistance = 5.5f;

        static readonly string[] Warnings =
        {
            "Something large is breathing on the other side.",
            "The floor past this arch is very clean. Nothing small has crossed it lately.",
            "Whatever is through here has a name. You will be told it shortly.",
        };

        string _warning;

        /// <summary>Stands an arch in one doorway of a room, on the outside of it.</summary>
        public static BossGate Create(Room room, int side, BiomeDefinition biome, int seed)
        {
            Vector2Int d = FloorLayout.Directions[side];
            float edge = (RoomShape.Size - 1) * 0.5f * RoomShape.TileSize;
            float half = RoomShape.TileSize * RoomShape.DoorWidth * 0.5f;
            bool alongX = d.x == 0;
            Material material = biome.eliteMaterial != null ? biome.eliteMaterial : biome.doorMaterial;

            var go = new GameObject("Boss Gate");
            go.transform.SetParent(room.transform, false);
            // A little outside the wall, so it is the first thing seen from the room before.
            go.transform.localPosition = new Vector3(d.x * (edge + 0.7f), 0f, d.y * (edge + 0.7f));

            Vector3 along = alongX ? Vector3.right : Vector3.forward;
            float height = Mathf.Min(biome.wallHeight, 6f);
            Part(go.transform, "Post", along * -(half + 0.2f) + Vector3.up * (height * 0.5f), new Vector3(0.55f, height, 0.55f), material);
            Part(go.transform, "Post", along * (half + 0.2f) + Vector3.up * (height * 0.5f), new Vector3(0.55f, height, 0.55f), material);
            Vector3 across = alongX ? new Vector3(half * 2f + 1f, 0.6f, 0.55f) : new Vector3(0.55f, 0.6f, half * 2f + 1f);
            Part(go.transform, "Lintel", Vector3.up * (height - 0.3f), across, material);
            // Three teeth hanging from the lintel: it is that kind of door.
            for (int i = -1; i <= 1; i++)
            {
                Part(go.transform, "Tooth", along * (i * half * 0.55f) + Vector3.up * (height - 1f), new Vector3(0.35f, 0.9f, 0.35f), material);
            }

            var gate = go.AddComponent<BossGate>();
            gate._warning = Warnings[Mathf.Abs(seed + side) % Warnings.Length];
            return gate;
        }

        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) renderer.sharedMaterial = material;
        }

        void OnEnable()
        {
            All.Add(this);
            WorldPrompt.Add(Prompt);
        }

        void OnDisable()
        {
            All.Remove(this);
            WorldPrompt.Remove(Prompt);
        }

        string Prompt(Vector3 playerPosition)
        {
            var floors = FloorManager.Instance;
            // Once the boss is met, or beaten, there is nothing left to warn about.
            if (floors == null || floors.Layout == null || floors.IsLocked || floors.Layout.exit.cleared) return null;
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            return to.magnitude <= noticeDistance ? _warning : null;
        }
    }
}
