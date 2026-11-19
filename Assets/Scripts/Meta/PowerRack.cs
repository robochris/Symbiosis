using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The rack in the lab where the Void's hero power is chosen. Step onto the mat in front of it to take the next
    /// one the lab has issued; the choice is remembered between runs. It also says what the others are and what it
    /// takes to be issued them.
    /// </summary>
    public class PowerRack : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1f };
        [Tooltip("One per power, in order; the one being carried is lifted off the rack.")]
        public Transform[] canisters;

        Vector3[] _homes;

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Show();
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Next();
        }

        /// <summary>Takes the next power that has been issued, and remembers it. Returns the one now carried.</summary>
        public PowerKind Next()
        {
            int count = HeroPower.All.Length;
            int current = (int)HeroPower.Chosen;
            int next = current;
            for (int i = 0; i < count; i++)
            {
                next = (next + 1) % count;
                if (HeroPower.Unlocked((PowerKind)next)) break;
            }

            SaveSystem.Data.meta.power = next;
            SaveSystem.Save();
            var player = PlayerController.Instance;
            if (player != null && player.Power != null) player.Power.SetKind((PowerKind)next);
            if (next != current) Sfx.Play(SfxId.Item);
            Show();
            return (PowerKind)next;
        }

        // The one being carried stands proud of the rest.
        void Show()
        {
            if (canisters == null) return;
            if (_homes == null || _homes.Length != canisters.Length)
            {
                _homes = new Vector3[canisters.Length];
                for (int i = 0; i < canisters.Length; i++) if (canisters[i] != null) _homes[i] = canisters[i].localPosition;
            }
            int chosen = (int)HeroPower.Chosen;
            for (int i = 0; i < canisters.Length; i++)
            {
                if (canisters[i] == null) continue;
                canisters[i].localPosition = _homes[i] + (i == chosen ? Vector3.up * 0.45f : Vector3.zero);
                canisters[i].gameObject.SetActive(i < HeroPower.All.Length && HeroPower.Unlocked((PowerKind)i));
            }
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.6f) return null;

            PowerInfo carried = HeroPower.Info(HeroPower.Chosen);
            int issued = 0;
            foreach (var info in HeroPower.All) if (HeroPower.Unlocked(info.kind)) issued++;

            string text = $"The power rack: you carry {carried.name}. {carried.does}";
            if (issued > 1) text += " Step off and on again for the next.";
            // Say how to be issued the next one that is still locked.
            foreach (var info in HeroPower.All)
            {
                if (HeroPower.Unlocked(info.kind)) continue;
                text += $"\n{info.name}: {info.needs}";
                break;
            }
            return text;
        }
    }
}
