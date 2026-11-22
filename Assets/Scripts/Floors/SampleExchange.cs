using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The exchange: a hatch that sometimes turns up in the first room of a floor. Stand on its mat for a moment and
    /// it takes a number of the samples being carried and puts an item out. Samples spent here never go home, so it
    /// is a trade of what the run is worth for a better chance of finishing it. One trade a floor. Made by the floor.
    /// </summary>
    public class SampleExchange : MonoBehaviour
    {
        public const int Price = 15;
        public const float HoldTime = 1.1f;

        public float radius = 1f;
        /// <summary>True once it has made its one trade.</summary>
        public bool Shut { get; private set; }

        float _held;
        readonly StepPad _pad = new StepPad { radius = 1.1f };
        Transform _hatch;

        public static SampleExchange Create(Room room, Vector3 position, Material matMaterial, Material boxMaterial)
        {
            var go = new GameObject("Sample Exchange");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            Part(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.9f, 0.02f, 1.9f), matMaterial);
            Part(PrimitiveType.Cube, "Box", go.transform, new Vector3(0f, 0.6f, 1.2f), new Vector3(1.5f, 1.2f, 0.7f), boxMaterial);
            var exchange = go.AddComponent<SampleExchange>();
            exchange._hatch = Part(PrimitiveType.Cube, "Hatch", go.transform, new Vector3(0f, 0.7f, 0.83f), new Vector3(0.9f, 0.5f, 0.06f), matMaterial);
            PortalTag.Create(go.transform, () => exchange.Shut ? "SHUT" : "EXCHANGE\n" + Price + " samples", new Color(0.45f, 1f, 0.6f), 2.6f);
            return exchange;
        }

        static Transform Part(PrimitiveType shape, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
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
            return part.transform;
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || Shut) return;
            // Used like everything else: E, or USE.
            if (_pad.SteppedOn(transform.position, player.transform.position) && RunState.Samples >= Price) Trade();
        }

        /// <summary>Takes the samples and puts an item out. Returns false if there are not enough, or it has shut.</summary>
        public bool Trade()
        {
            _held = 0f;
            var assets = GameAssets.Instance;
            if (Shut || RunState.Samples < Price || assets == null || assets.items == null) return false;
            var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            if (picks.Count == 0) return false;

            RunState.AddSamples(-Price);
            // Out of the hatch, on the side away from the box.
            Pickup.Item(picks[0], transform.position + new Vector3(0f, 0f, -1.8f), 0);
            Shut = true;
            if (_hatch != null) _hatch.gameObject.SetActive(false);
            Sfx.Play(SfxId.Item);
            if (Radio.Instance != null) Radio.Instance.Say(RadioMoment.Exchange);
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 2f) return null;
            if (Shut) return "The exchange: shut. One trade a floor.";
            return RunState.Samples >= Price
                ? $"The exchange: {Price} samples for an item (you have {RunState.Samples}).\nSpent samples do not go home. {Interact.Verb} to trade."
                : $"The exchange: {Price} samples for an item. You have {RunState.Samples}.\nIt does not do credit.";
        }
    }
}
