using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The lab fills in as the player gets further, so progress is something they walk past: a station is not there
    /// until it has a reason to be. The first time a new one is there, the lab says so.
    /// </summary>
    public class LabGrowth : MonoBehaviour
    {
        public enum Needs
        {
            /// <summary>The record board: once there is a run to record.</summary>
            ARunStarted,
            /// <summary>The mirror: once there is a second style to change into.</summary>
            AStyleUnlocked,
            /// <summary>Once any boss has been beaten.</summary>
            ABossBeaten,
            /// <summary>The evidence board: once a page of the notebook has been found.</summary>
            ANoteFound,
            /// <summary>The hidden door: once the whole notebook has been found.</summary>
            AllNotesFound,
            /// <summary>The filing cabinet: once any item has been picked up.</summary>
            AnItemFound,
            /// <summary>The time clock: once Dr. Finch has been beaten.</summary>
            FinchBeaten,
            /// <summary>The door: once a piece of the collar has been taken from a boss.</summary>
            APieceFound
        }

        [Serializable]
        public class Station
        {
            public string name;
            public GameObject thing;
            public Needs needs;
        }

        public Station[] stations;

        public static bool Met(Needs needs)
        {
            MetaSave meta = SaveSystem.Data.meta;
            switch (needs)
            {
                case Needs.ARunStarted: return meta.runsStarted > 0;
                case Needs.ABossBeaten: return meta.bossesBeaten > 0;
                case Needs.ANoteFound: return meta.notes.Count > 0;
                case Needs.AllNotesFound: return meta.notes.Count >= FinchNotes.All.Length;
                case Needs.AnItemFound: return meta.itemsFound.Count > 0;
                case Needs.FinchBeaten: return meta.finchBeaten > 0;
                case Needs.APieceFound: return Collar.Count > 0;
                default:
                    if (Achievements.EverythingUnlocked) return true;
                    foreach (var achievement in Achievements.All)
                    {
                        if (achievement.style != null && Achievements.Has(achievement.id)) return true;
                    }
                    return false;
            }
        }

        void Start()
        {
            MetaSave meta = SaveSystem.Data.meta;
            string arrived = null;
            for (int i = 0; stations != null && i < stations.Length; i++)
            {
                Station station = stations[i];
                if (station.thing == null) continue;
                bool here = Met(station.needs);
                station.thing.SetActive(here);

                // Bit i of labSeen: the player has already been told about this one.
                if (here && (meta.labSeen & (1 << i)) == 0)
                {
                    meta.labSeen |= 1 << i;
                    arrived = arrived == null ? station.name : arrived + " and " + station.name;
                }
            }

            if (arrived == null) return;
            SaveSystem.Save();
            StartCoroutine(Say($"New in the lab: {arrived}."));
        }

        // After whatever the lab is already saying (how the last run went), not over the top of it.
        System.Collections.IEnumerator Say(string message)
        {
            yield return null;
            var hud = FindAnyObjectByType<Hud>();
            if (hud == null) yield break;
            while (hud.ToastShowing) yield return null;
            hud.Toast(message, 6f);
        }
    }
}
