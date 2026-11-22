using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The hat stand, just inside the lab: use it to try on the next hat you have earned. It says what the next hat
    /// to find is and how to earn it. Put down by the lab when it opens, beside where the player comes in.
    /// </summary>
    public class HatStand : MonoBehaviour
    {
        public readonly StepPad pad = new StepPad { radius = 1f };
        public static HatStand Current { get; private set; }

        public static HatStand Place(Vector3 position)
        {
            var go = new GameObject("Hat Stand");
            go.transform.position = position;
            var wood = new Color(0.45f, 0.3f, 0.18f);
            Part.Make(PrimitiveType.Cylinder, "Foot", go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.7f, 0.05f, 0.7f), wood);
            Part.Make(PrimitiveType.Cylinder, "Pole", go.transform, new Vector3(0f, 1f, 0f), new Vector3(0.1f, 1f, 0.1f), wood);
            for (int i = 0; i < 3; i++)
            {
                var peg = Part.Make(PrimitiveType.Cylinder, "Peg " + i, go.transform, new Vector3(0f, 1.75f, 0f), new Vector3(0.05f, 0.25f, 0.05f), wood);
                peg.transform.localRotation = Quaternion.Euler(55f, i * 120f, 0f);
                peg.transform.localPosition += peg.transform.up * 0.2f;
            }
            // A hat hung up on it, so it reads as a hat stand from across the room.
            var shown = new GameObject("Hung Hat").transform;
            shown.SetParent(go.transform, false);
            shown.localPosition = new Vector3(0f, 1.15f, 0f);
            shown.localScale = Vector3.one * 1.1f;
            Hats.All[5].build(shown);
            Part.Make(PrimitiveType.Cylinder, "Mat", go.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f), new Color(0.85f, 0.65f, 1f));
            var stand = go.AddComponent<HatStand>();
            UseMarkers.Rescan();
            return stand;
        }

        void OnEnable()
        {
            Current = this;
            WorldPrompt.Add(Prompt, transform);
        }

        void OnDisable()
        {
            if (Current == this) Current = null;
            WorldPrompt.Remove(Prompt);
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Hat hat = Hats.WearNext();
            Sfx.Play(SfxId.Item, 0.6f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(Hats.Worn == 0 ? "Hat off." : $"Wearing the {hat.name}.", 2.5f);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2f) return null;
            Hat next = Hats.NextToEarn();
            string still = next != null ? $" Still to find: the {next.name}. {next.how}" : " Every hat found.";
            return $"The hat stand: {Hats.EarnedCount - 1} of {Hats.All.Length - 1} hats.{still}\n{Interact.Verb} to try the next one.";
        }
    }
}
