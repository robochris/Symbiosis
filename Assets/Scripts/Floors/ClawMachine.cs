using UnityEngine;

namespace Shrunk
{
    public enum ClawPrize { Nothing, Crumb, Jackpot, Item }

    /// <summary>
    /// The claw machine: it sometimes turns up in the first room of a floor. Stand on its mat for a moment and it
    /// takes some coins and has a go. Mostly it drops something small or nothing at all; now and then it pays out
    /// more coins than it took, or an item. Every go on the same floor costs more than the last. Made by the floor.
    /// </summary>
    public class ClawMachine : MonoBehaviour
    {
        public const int FirstPrice = 6, PriceStep = 3;
        public const float HoldTime = 1.1f;
        /// <summary>The odds, in order: an item, a jackpot, a crumb. Whatever is left is nothing.</summary>
        public const float ItemOdds = 0.15f, JackpotOdds = 0.2f, CrumbOdds = 0.3f;

        public float radius = 1f;
        public int price = FirstPrice;
        public int Goes { get; private set; }
        public ClawPrize Last { get; private set; }
        /// <summary>Tests set this to decide what the next go wins.</summary>
        [System.NonSerialized] public ClawPrize? rigged;

        float _held;
        Transform _claw;

        public static ClawMachine Create(Room room, Vector3 position, Material matMaterial, Material boxMaterial, Material glassMaterial)
        {
            var go = new GameObject("Claw Machine");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            Part(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.9f, 0.02f, 1.9f), matMaterial);
            Part(PrimitiveType.Cube, "Base", go.transform, new Vector3(0f, 0.5f, 1.3f), new Vector3(1.4f, 1f, 0.9f), boxMaterial);
            Part(PrimitiveType.Cube, "Glass", go.transform, new Vector3(0f, 1.6f, 1.3f), new Vector3(1.3f, 1.2f, 0.8f), glassMaterial);
            Part(PrimitiveType.Cube, "Top", go.transform, new Vector3(0f, 2.3f, 1.3f), new Vector3(1.4f, 0.2f, 0.9f), boxMaterial);
            var machine = go.AddComponent<ClawMachine>();
            machine._claw = Part(PrimitiveType.Sphere, "Claw", go.transform, new Vector3(0f, 2.05f, 0.85f), Vector3.one * 0.22f, matMaterial);
            PortalTag.Create(go.transform, () => "CLAW\n" + machine.price + " coins", new Color(1f, 0.82f, 0.2f), 3.1f);
            return machine;
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
            if (player == null) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            bool on = to.magnitude <= radius && RunState.Coins >= price;
            _held = on ? _held + Time.deltaTime : 0f;
            // The claw comes down as the go gets nearer.
            if (_claw != null) _claw.localPosition = new Vector3(Mathf.Sin(Time.time * 2f) * 0.35f, 2.05f - 0.6f * Mathf.Clamp01(_held / HoldTime), 0.85f);
            if (_held >= HoldTime) Play();
        }

        /// <summary>Has a go, if the coins are there. Returns false if they are not.</summary>
        public bool Play()
        {
            _held = 0f;
            if (!RunState.TrySpendCoins(price)) return false;
            int paid = price;
            Goes++;
            price += PriceStep;

            float roll = Random.value;
            ClawPrize prize = rigged ?? (roll < ItemOdds ? ClawPrize.Item
                : roll < ItemOdds + JackpotOdds ? ClawPrize.Jackpot
                : roll < ItemOdds + JackpotOdds + CrumbOdds ? ClawPrize.Crumb : ClawPrize.Nothing);
            var assets = GameAssets.Instance;
            Vector3 chute = transform.position + new Vector3(0f, 0f, -1.8f);
            string said;
            switch (prize)
            {
                case ClawPrize.Item when assets != null && assets.items != null:
                    var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
                    if (picks.Count > 0) Pickup.Item(picks[0], chute, 0);
                    said = "The claw held on. That never happens.";
                    break;
                case ClawPrize.Jackpot:
                    int coins = Mathf.CeilToInt(paid * 2.5f);
                    for (int i = 0; i < coins; i++)
                    {
                        Vector2 offset = Random.insideUnitCircle * 1.2f;
                        Pickup.Coin(chute + new Vector3(offset.x, 0f, offset.y), 1);
                    }
                    said = $"Jackpot: {coins} coins. The machine is embarrassed.";
                    break;
                case ClawPrize.Crumb:
                    Pickup.Health(chute);
                    said = "A crumb. It counts as winning.";
                    break;
                default:
                    prize = ClawPrize.Nothing;
                    said = "It dropped it. It always drops it.";
                    break;
            }
            Last = prize;
            if (prize != ClawPrize.Nothing) Jobs.Count(JobKind.ClawWins);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(said, 3f);
            Sfx.Play(prize == ClawPrize.Nothing ? SfxId.Blocked : SfxId.Item);
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 2f) return null;
            return RunState.Coins >= price
                ? $"The claw machine: {price} coins a go (you have {RunState.Coins}). It usually drops it.\nStand still on the mat to play."
                : $"The claw machine: {price} coins a go. You have {RunState.Coins}.";
        }
    }
}
