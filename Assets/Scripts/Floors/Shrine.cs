using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A shrine: now and then, in the first room of a floor, a little stone post with a glowing orb that offers one
    /// trade, always something for something (health for coins, coins for an item, a piece of your health for good
    /// for an item, samples for more health). Used once, with E or USE, or left alone. Made by the floor.
    /// </summary>
    public class Shrine : MonoBehaviour
    {
        public enum Deal { BloodForGold, GoldForGear, BloodForGear, SamplesForHeart }

        public const int GoldGained = 15, GoldPrice = 12, SamplePrice = 15;
        /// <summary>Tests pin it: 0 never, 1 always.</summary>
        public static float? ChanceForAll;
        public const float Chance = 0.14f;

        public Deal deal;
        public bool Used { get; private set; }
        readonly StepPad _pad = new StepPad { radius = 1f };
        Transform _orb;

        public static Shrine Create(Room room, Vector3 position, Deal deal)
        {
            var go = new GameObject("Shrine");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            var stone = new Color(0.55f, 0.55f, 0.6f);
            Part.Make(PrimitiveType.Cylinder, "Base", go.transform, new Vector3(0f, 0.1f, 0f), new Vector3(0.9f, 0.1f, 0.9f), stone);
            Part.Make(PrimitiveType.Cube, "Post", go.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.45f, 1.1f, 0.45f), stone);
            Part.Make(PrimitiveType.Cylinder, "Dish", go.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.7f, 0.06f, 0.7f), stone);
            var shrine = go.AddComponent<Shrine>();
            shrine.deal = deal;
            shrine._orb = Part.Make(PrimitiveType.Sphere, "Orb", go.transform, new Vector3(0f, 1.55f, 0f), Vector3.one * 0.38f, Colour(deal), true).transform;
            Part.Make(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f), Colour(deal));
            PortalTag.Create(go.transform, () => shrine.Used ? "" : "SHRINE", Colour(deal), 2.6f);
            UseMarkers.Rescan();
            return shrine;
        }

        static Color Colour(Deal deal)
        {
            switch (deal)
            {
                case Deal.BloodForGold: return new Color(1f, 0.82f, 0.25f);
                case Deal.GoldForGear: return new Color(0.4f, 0.8f, 1f);
                case Deal.BloodForGear: return new Color(0.9f, 0.25f, 0.35f);
                default: return new Color(0.5f, 1f, 0.55f);
            }
        }

        public static string Offer(Deal deal)
        {
            switch (deal)
            {
                case Deal.BloodForGold: return $"Give a point of health, take {GoldGained} coins.";
                case Deal.GoldForGear: return $"Give {GoldPrice} coins, take an item.";
                case Deal.BloodForGear: return "Give a point of your most health, for good this run, take an item.";
                default: return $"Give {SamplePrice} samples, take a point of most health, filled.";
            }
        }

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            if (_orb != null) _orb.localPosition = new Vector3(0f, 1.55f + Mathf.Sin(Time.time * 2f) * 0.08f, 0f);
            var player = PlayerController.Instance;
            if (Used || player == null) return;
            if (_pad.SteppedOn(transform.position, player.transform.position)) Take(player);
        }

        /// <summary>Can the player pay for it right now?</summary>
        public bool Affordable(PlayerController player)
        {
            if (player == null) return false;
            switch (deal)
            {
                case Deal.BloodForGold: return player.Health.Current > 1;
                case Deal.GoldForGear: return RunState.Coins >= GoldPrice;
                case Deal.BloodForGear: return player.Health.maxHealth > 2;
                default: return RunState.Samples >= SamplePrice;
            }
        }

        /// <summary>Makes the trade, if it can be paid for. Returns false if not.</summary>
        public bool Take(PlayerController player)
        {
            if (Used || !Affordable(player)) return false;
            var assets = GameAssets.Instance;
            switch (deal)
            {
                case Deal.BloodForGold:
                    player.Health.Restore(player.Health.Current - 1);
                    RunState.AddCoins(GoldGained);
                    break;
                case Deal.GoldForGear:
                    if (!RunState.TrySpendCoins(GoldPrice)) return false;
                    GiveItem(assets);
                    break;
                case Deal.BloodForGear:
                    player.Health.SetMaxHealth(player.Health.maxHealth - 1, false);
                    GiveItem(assets);
                    break;
                default:
                    RunState.AddSamples(-SamplePrice);
                    player.Health.SetMaxHealth(player.Health.maxHealth + 1, false);
                    player.Health.Heal(1);
                    break;
            }
            Used = true;
            if (_orb != null) _orb.gameObject.SetActive(false);
            Sfx.Play(SfxId.PowerOn, 0.6f);
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.15f);
            return true;
        }

        void GiveItem(GameAssets assets)
        {
            if (assets == null || assets.items == null) return;
            var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
            if (picks.Count > 0) Pickup.Item(picks[0], transform.position + new Vector3(0f, 0f, -1.6f), 0);
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > 2.2f) return null;
            if (Used) return "The shrine is quiet now.";
            var player = PlayerController.Instance;
            return Affordable(player)
                ? $"A shrine. {Offer(deal)}\n{Interact.Verb} to accept, or leave it be."
                : $"A shrine. {Offer(deal)}\nYou cannot pay that yet.";
        }
    }
}
