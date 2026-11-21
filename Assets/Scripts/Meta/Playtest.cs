using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// Shortcuts for trying the game out, not for playing it: they write straight into the save file. In the lab,
    /// F9 unlocks everything there is to unlock and F10 adds 500 samples. The same two are in the Shrunk menu in
    /// Unity. Delete Saved Progress (or the title's erase button) undoes them.
    /// </summary>
    public class Playtest : MonoBehaviour
    {
        /// <summary>
        /// Every achievement (so every style, item and biome), every page of the notebook (so the door), every jar,
        /// and enough extractions and falls for all three hero powers.
        /// </summary>
        public static void UnlockEverything()
        {
            MetaSave meta = SaveSystem.Data.meta;
            foreach (var achievement in Achievements.All)
            {
                if (!meta.achievements.Contains((int)achievement.id)) meta.achievements.Add((int)achievement.id);
            }
            foreach (var note in FinchNotes.All) if (!meta.notes.Contains(note.id)) meta.notes.Add(note.id);
            foreach (var specimen in Specimens.All) if (!meta.specimens.Contains(specimen.key)) meta.specimens.Add(specimen.key);
            foreach (var part in MachineParts.All) if (!meta.parts.Contains(part.boss)) meta.parts.Add(part.boss);
            // Not the items: the filing cabinet is for what was really found.
            meta.runsStarted = Mathf.Max(meta.runsStarted, 1);
            meta.extractions = Mathf.Max(meta.extractions, 1);
            meta.deaths = Mathf.Max(meta.deaths, 3);
            meta.jobsDone = Mathf.Max(meta.jobsDone, 3);
            meta.bossesBeaten = Mathf.Max(meta.bossesBeaten, Specimens.All.Length);
            meta.xp = Mathf.Max(meta.xp, Progress.XpFor(Progress.TopLevel));
            meta.dailyBestStreak = Mathf.Max(meta.dailyBestStreak, 3);
            SaveSystem.Save();
        }

        public static void AddSamples(int amount)
        {
            SaveSystem.Data.meta.samples += amount;
            SaveSystem.Save();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || ScreenCards.Blocking) return;

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                UnlockEverything();
                RunDirector.LastRunMessage = "Playtest: everything is unlocked. Erase the save to undo it.";
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else if (keyboard.f10Key.wasPressedThisFrame)
            {
                AddSamples(500);
                var hud = FindAnyObjectByType<Hud>();
                if (hud != null) hud.Toast("Playtest: 500 samples added.", 3f);
            }
        }
    }
}
