using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The evidence board in the lab: every page of Dr. Finch's notebook found so far, pinned up. Walk up to read one;
    /// step away and back for the next. It only appears once there is a page to pin to it.
    /// </summary>
    public class EvidenceBoard : MonoBehaviour
    {
        [Tooltip("One per page, in page order; they switch on as pages are found.")]
        public GameObject[] pages;
        public float readDistance = 3.2f;

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
            for (int i = 0; pages != null && i < pages.Length && i < FinchNotes.All.Length; i++)
            {
                if (pages[i] != null) pages[i].SetActive(FinchNotes.Has(FinchNotes.All[i].id));
            }
        }

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            bool near = to.magnitude <= readDistance;
            if (!near)
            {
                _near = false;
                return null;
            }

            var found = FinchNotes.Collected();
            int total = FinchNotes.All.Length;
            if (found.Count == 0) return "The evidence board: nothing pinned to it yet.";

            // Each walk up to the board turns to the next page, starting with the newest.
            if (!_near)
            {
                _near = true;
                _reading = _reading < 0 ? found.Count - 1 : (_reading + 1) % found.Count;
            }
            _reading = Mathf.Clamp(_reading, 0, found.Count - 1);
            FinchNote note = found[_reading];

            string text = $"The evidence board: {found.Count} of {total} pages.  Page {note.id}: \"{note.text}\"";
            if (found.Count == total) text += "\nThey all point the same way: down. The radio insists there is no door.";
            else if (found.Count > 1) text += "\nStep away and back for the next page.";
            return text;
        }
    }
}
