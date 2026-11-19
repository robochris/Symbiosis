using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The equipment cupboard in the lab, where the Void's weapon is chosen. Step onto the mat in front of it to take
    /// the next one the lab has issued; the choice is remembered between runs. It also says what the next one is and
    /// what it takes to be issued it.
    /// </summary>
    public class WeaponRack : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1f };
        [Tooltip("One per weapon, in order; the one being carried is lifted off the shelf.")]
        public Transform[] pieces;

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

        /// <summary>Takes the next weapon that has been issued, and remembers it. Returns the one now carried.</summary>
        public WeaponKind Next()
        {
            int count = Weapons.All.Length;
            int current = (int)Weapons.Chosen;
            int next = current;
            for (int i = 0; i < count; i++)
            {
                next = (next + 1) % count;
                if (Weapons.Unlocked((WeaponKind)next)) break;
            }

            SaveSystem.Data.meta.weapon = next;
            SaveSystem.Save();
            var player = PlayerController.Instance;
            if (player != null && player.weapon != null) player.weapon.kind = (WeaponKind)next;
            if (next != current)
            {
                Sfx.Play(SfxId.Item);
                if (Radio.Instance != null) Radio.Instance.Say(RadioMoment.Weapon);
            }
            Show();
            return (WeaponKind)next;
        }

        void Show()
        {
            if (pieces == null) return;
            if (_homes == null || _homes.Length != pieces.Length)
            {
                _homes = new Vector3[pieces.Length];
                for (int i = 0; i < pieces.Length; i++) if (pieces[i] != null) _homes[i] = pieces[i].localPosition;
            }
            int chosen = (int)Weapons.Chosen;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null) continue;
                pieces[i].localPosition = _homes[i] + (i == chosen ? Vector3.up * 0.45f : Vector3.zero);
                pieces[i].gameObject.SetActive(i < Weapons.All.Length && Weapons.Unlocked((WeaponKind)i));
            }
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;

            WeaponInfo carried = Weapons.Info(Weapons.Chosen);
            int issued = 0;
            foreach (var info in Weapons.All) if (Weapons.Unlocked(info.kind)) issued++;

            string text = $"The equipment cupboard: you carry the {carried.name}. {carried.does}";
            if (issued > 1) text += " Step off and on again for the next.";
            foreach (var info in Weapons.All)
            {
                if (Weapons.Unlocked(info.kind)) continue;
                text += $"\nThe {info.name}: {info.needs}";
                break;
            }
            return text;
        }
    }
}
