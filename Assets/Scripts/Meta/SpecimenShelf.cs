using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>One jar on the shelf: a boss, and what Dr. Finch wrote on the label.</summary>
    public class Specimen
    {
        /// <summary>The boss's prefab name, which is what the save file remembers.</summary>
        public string key;
        public string title;
        public string label;
    }

    /// <summary>Every boss that can end up in a jar, in shelf order, and which of them the player has beaten.</summary>
    public static class Specimens
    {
        public static readonly Specimen[] All =
        {
            new Specimen { key = "BroodMother", title = "Brood Mother", label = "Excellent parent, by volume. I had hoped to interview her. You had other ideas." },
            new Specimen { key = "AntQueen", title = "Ant Queen", label = "We had an arrangement. The colony has not been told about this jar and I would thank you to keep it that way." },
            new Specimen { key = "JellyMother", title = "Jelly Mother", label = "Technically confectionery. The jar is mostly to keep the smell in." },
            new Specimen { key = "TrickCandle", title = "The Trick Candle", label = "It keeps relighting. The lid is on tight and I check it more often than I would like." },
            new Specimen { key = "GrandmotherDust", title = "Grandmother Dust", label = "Older than the sofa. Do not open. She gets everywhere." },
            new Specimen { key = "RobotVacuum", title = "The Robot Vacuum", label = "The battery is in a separate jar. I am not an amateur." },
            new Specimen { key = "Leftovers", title = "The Leftovers", label = "Do not open. I mean it more for this one than for the others." },
            new Specimen { key = "IceMaker", title = "The Ice Maker", label = "Unplugged. It is still cold to the touch, which I put down to spite." },
            new Specimen { key = "TheTrainee", title = "The Trainee", label = "Handed in their notice from inside the jar. I have not accepted it." },
            new Specimen { key = "TheLibrarian", title = "The Librarian", label = "Silence, at last. The label is in pencil, at their request." },
            new Specimen { key = "TheFroglet", title = "The Froglet", label = "Still growing. I have punched air holes in the lid, and it has punched them back." },
            new Specimen { key = "TheToad", title = "The Toad", label = "It has not blinked since it went in. Neither, I find, have I." },
            new Specimen { key = "TheKite", title = "The Kite", label = "The string is still attached. I am not pulling on it. You pull on it." },
            new Specimen { key = "TheThundercloud", title = "The Thundercloud", label = "The jar fogs up from the inside. Now and then it rumbles. I have stopped jumping." },
            new Specimen { key = "TheCat", title = "The Cat", label = "An empty jar. It would not get in. Dr. Finch asks that you do not tap the glass anyway." },
        };

        static List<string> Kept => SaveSystem.Data.meta.specimens;

        public static bool Has(string key) => Kept.Contains(key);
        public static int Count
        {
            get
            {
                int count = 0;
                foreach (var specimen in All) if (Has(specimen.key)) count++;
                return count;
            }
        }

        /// <summary>The ones on the shelf so far, in shelf order.</summary>
        public static List<Specimen> Collected()
        {
            var list = new List<Specimen>();
            foreach (var specimen in All) if (Has(specimen.key)) list.Add(specimen);
            return list;
        }

        /// <summary>A boss has been beaten: it goes in a jar, if it is not in one already. Returns whether it was new.</summary>
        public static bool Keep(Enemy boss)
        {
            if (boss == null) return false;
            string key = boss.name.Replace("(Clone)", "").Trim();
            foreach (var specimen in All)
            {
                if (specimen.key != key) continue;
                if (Has(key)) return false;
                Kept.Add(key);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The specimen shelf in the lab: a jar for every boss the player has beaten, each with Dr. Finch's label. Walk
    /// up to read one; step away and back for the next. It turns up with the first boss.
    /// </summary>
    public class SpecimenShelf : MonoBehaviour
    {
        [Tooltip("One per specimen, in shelf order; they are switched on as bosses are beaten.")]
        public GameObject[] jars;
        public float readDistance = 2.6f;

        int _reading = -1;
        bool _near;

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Refresh();
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        public void Refresh()
        {
            for (int i = 0; jars != null && i < jars.Length && i < Specimens.All.Length; i++)
            {
                if (jars[i] != null) jars[i].SetActive(Specimens.Has(Specimens.All[i].key));
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

            var kept = Specimens.Collected();
            if (kept.Count == 0) return "The specimen shelf: empty jars, labelled in advance.";

            // Each walk up to the shelf turns to the next jar, starting with the newest.
            if (!_near)
            {
                _near = true;
                _reading = _reading < 0 ? kept.Count - 1 : (_reading + 1) % kept.Count;
            }
            _reading = Mathf.Clamp(_reading, 0, kept.Count - 1);
            Specimen jar = kept[_reading];

            string text = $"The specimen shelf: {kept.Count} of {Specimens.All.Length} jars.  {jar.title}: \"{jar.label}\"";
            if (kept.Count > 1) text += "\nStep away and back for the next jar.";
            return text;
        }
    }
}
