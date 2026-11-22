using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Greets the player in the lab with how the last run went.</summary>
    public class LabDirector : MonoBehaviour
    {
        public const string CycleLine = "The machine has restarted. Everything out there is a depth harder, and worth more. The parts are gone again.";
        public static string CycleHeading(int cycle) => "CYCLE " + (cycle + 1);
        public const string EndingHeading = "END OF PROBATION";
        public const string EndingLine = "You are now permanent staff. Dr. Finch remains, technically, your manager.";

        /// <summary>Tests switch off the middle-of-the-night note.</summary>
        public static bool NightNoteOff;

        void Start()
        {
            // Home from the bottom with Finch beaten, for the first time: the ending, such as it is.
            MetaSave meta = SaveSystem.Data.meta;
            if (meta.finchBeaten > 0 && !meta.endingSeen)
            {
                meta.endingSeen = true;
                SaveSystem.Save();
                if (ScreenCards.Instance != null) ScreenCards.Instance.ShowCard(EndingHeading, EndingLine, 7f);
            }

            // Home from the machine: it has restarted, and so has everything else.
            if (RunDirector.CycleCard)
            {
                RunDirector.CycleCard = false;
                if (ScreenCards.Instance != null) ScreenCards.Instance.ShowCard(CycleHeading(MachineParts.Cycle), CycleLine, 6f);
            }

            // How the run went, on a card of its own, unless a bigger card has the screen.
            if (!string.IsNullOrEmpty(RunDirector.LastSummary) && ScreenCards.Instance != null && !ScreenCards.Instance.CardShowing)
            {
                ScreenCards.Instance.ShowCard(RunDirector.LastSummaryHeading, RunDirector.LastSummary, 4.5f);
            }
            RunDirector.LastSummary = null;

            // Today's parcel, beside where the player comes in.
            var player = PlayerController.Instance;
            if (player != null && Unlocks.Open(Unlocks.Feature.DailyParcel)) DailyParcel.Place(player.transform.position + new Vector3(2.6f, 0f, 1.6f));
            if (player != null && Unlocks.Open(Unlocks.Feature.HatStand)) HatStand.Place(player.transform.position + new Vector3(-2.6f, 0f, 1.6f));
            if (player != null && Unlocks.Open(Unlocks.Feature.PetJar)) PetJar.Place(player.transform.position + new Vector3(-2.6f, 0f, -1.4f));
            if (player != null && Unlocks.Open(Unlocks.Feature.PressureGauge)) PressureGauge.Place(player.transform.position + new Vector3(2.6f, 0f, -1.4f));
            if (player != null && Unlocks.Open(Unlocks.Feature.Mailbox)) Mailbox.Place(player.transform.position + new Vector3(4.6f, 0f, 1.2f));
            if (player != null && Unlocks.Open(Unlocks.Feature.Jukebox)) Jukebox.Place(player.transform.position + new Vector3(-4.8f, 0f, 1.2f));
            if (player != null && Unlocks.Open(Unlocks.Feature.FishTank)) FishTank.Place(player.transform.position + new Vector3(-4.8f, 0f, 4.2f));
            // The cat is asleep somewhere new each visit: one of a few spots out of the way.
            Vector3[] naps = { new Vector3(3.6f, 0f, 4.6f), new Vector3(-6.5f, 0f, -1.5f), new Vector3(6.5f, 0f, 3.5f), new Vector3(-3.6f, 0f, 5f) };
            if (player != null) LabCat.Place(player.transform.position + naps[UnityEngine.Random.Range(0, naps.Length)]);
            if (player != null && Unlocks.Open(Unlocks.Feature.Seasons)) SeasonBoard.Place(player.transform.position + new Vector3(2.6f, 0f, -4.4f), player.transform.position);
            if (player != null && Unlocks.Open(Unlocks.Feature.QuickRun)) QuickRunSign.Place(player.transform.position + new Vector3(-2.6f, 0f, -5.8f));
            var cabinet = FindAnyObjectByType<FilingCabinet>(FindObjectsInactive.Include);
            if (cabinet != null && Unlocks.Open(Unlocks.Feature.ItemWall)) ItemWall.Place(cabinet.transform.position + new Vector3(-2.8f, 0f, 0.4f), Quaternion.LookRotation(Vector3.back));
            Pressure.Apply(0);
            if (!NightNoteOff && System.DateTime.Now.Hour >= 2 && System.DateTime.Now.Hour < 5 && ScreenCards.Instance != null && !ScreenCards.Instance.CardShowing)
                ScreenCards.Instance.ShowCard("A NOTE FROM FINCH", "It is the middle of the night. Go to sleep. The ants will still be there in the morning.", 4f);
            if (SaveSystem.Data.meta.runsStarted == 0)
            {
                var portal = GameObject.Find("Portal");
                if (portal != null) StartHere.Place(portal.transform.position + Vector3.up * 4.2f);
            }
            RunRules.End();

            // Something new in the lab, or out there: one a visit, so it is never all at once.
            Unlocks.AnnounceNext();

            // A hat or a pet earned on the way: say so, and where to find it.
            var fresh = Hats.TakeNew();
            var pets = Pets.TakeNew();
            if ((fresh.Count > 0 || pets.Count > 0) && ScreenCards.Instance != null && !ScreenCards.Instance.CardShowing)
            {
                string line = "";
                if (fresh.Count > 0) line += (fresh.Count == 1 ? $"You earned the {fresh[0].name}." : $"You earned {fresh.Count} new hats.") + (fresh.Count == 1 ? " Try it on" : " Try them on") + " at the hat stand by the door. ";
                if (pets.Count > 0) line += (pets.Count == 1 ? $"The {pets[0].name} would like to come along." : $"{pets.Count} pets would like to come along.") + " Let them out at the pet jar.";
                string heading = fresh.Count > 0 && pets.Count > 0 ? "NEW THINGS" : fresh.Count > 0 ? (fresh.Count == 1 ? "NEW HAT" : "NEW HATS") : (pets.Count == 1 ? "NEW PET" : "NEW PETS");
                ScreenCards.Instance.ShowCard(heading, line.Trim(), 4.5f);
            }

            var hud = FindAnyObjectByType<Hud>();
            if (hud == null) return;

            if (!string.IsNullOrEmpty(RunDirector.LastRunMessage))
            {
                hud.Toast(RunDirector.LastRunMessage, 6f);
                RunDirector.LastRunMessage = null;
            }
            else if (SaveSystem.Data.meta.runsStarted == 0)
            {
                hud.Toast($"The lab. Stand at the portal and {Interact.Verb.ToLower()} to start your first run.", 6f);
            }
        }

        void OnEnable() => Upgrades.Purchased += OnPurchased;
        void OnDisable() => Upgrades.Purchased -= OnPurchased;

        void OnPurchased(UpgradeDefinition upgrade)
        {
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"Bought {upgrade.displayName}, level {Upgrades.Level(upgrade.type)}.", 3f);
        }
    }
}
