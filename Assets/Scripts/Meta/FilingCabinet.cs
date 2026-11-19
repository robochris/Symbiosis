using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The filing cabinet in the lab: one card for every kind of item the player has ever picked up, in any run.
    /// Walk up to read one; step away and back for the next. It says how many are on file out of how many there
    /// are, so there is always something still to find. It turns up with the first item.
    /// </summary>
    public class FilingCabinet : MonoBehaviour
    {
        [Tooltip("The drawers, bottom to top; more of them stand open as more is on file.")]
        public Transform[] drawers;
        public float readDistance = 2f;

        int _reading = -1;
        bool _near;

        /// <summary>Kinds of item on file.</summary>
        public static int Filed => SaveSystem.Data.meta.itemsFound.Count;

        /// <summary>Puts an item on file the first time it is picked up. Returns whether it was new.</summary>
        public static bool File(ItemDefinition item)
        {
            if (item == null) return false;
            List<int> found = SaveSystem.Data.meta.itemsFound;
            if (found.Contains((int)item.type)) return false;
            found.Add((int)item.type);
            return true;
        }

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Show();
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        // A drawer stands open for each share of the whole that is on file.
        void Show()
        {
            if (drawers == null || drawers.Length == 0) return;
            var library = GameAssets.Instance != null ? GameAssets.Instance.items : null;
            int total = library != null && library.items != null ? library.items.Length : 0;
            float share = total > 0 ? (float)Filed / total : 0f;
            for (int i = 0; i < drawers.Length; i++)
            {
                if (drawers[i] == null) continue;
                bool open = share >= (i + 1f) / drawers.Length;
                Vector3 at = drawers[i].localPosition;
                at.z = open ? 0.45f : 0.18f;
                drawers[i].localPosition = at;
            }
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            if (to.magnitude > readDistance)
            {
                _near = false;
                return null;
            }

            var library = GameAssets.Instance != null ? GameAssets.Instance.items : null;
            if (library == null || library.items == null) return null;

            // In the order they were first found, newest first each time the player walks up.
            var cards = new List<ItemDefinition>();
            foreach (int type in SaveSystem.Data.meta.itemsFound)
            {
                ItemDefinition item = library.Get((ItemType)type);
                if (item != null) cards.Add(item);
            }
            int total = library.items.Length;
            if (cards.Count == 0) return $"The filing cabinet: 0 of {total} items on file. The cards are blank, and alphabetised.";

            if (!_near)
            {
                _near = true;
                _reading = _reading < 0 ? cards.Count - 1 : (_reading + cards.Count - 1) % cards.Count;
            }
            _reading = Mathf.Clamp(_reading, 0, cards.Count - 1);
            ItemDefinition card = cards[_reading];

            string text = $"The filing cabinet: {cards.Count} of {total} items on file.  {card.displayName}: {card.Blurb}";
            if (cards.Count == total) text += "\nThat is all of them. Dr. Finch never got further than nine.";
            else if (cards.Count > 1) text += "\nStep away and back for the next card.";
            return text;
        }
    }
}
