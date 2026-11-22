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
            if (player != null) DailyParcel.Place(player.transform.position + new Vector3(2.6f, 0f, 1.6f));
            if (player != null) HatStand.Place(player.transform.position + new Vector3(-2.6f, 0f, 1.6f));
            if (player != null) PetJar.Place(player.transform.position + new Vector3(-2.6f, 0f, -1.4f));
            if (player != null) PressureGauge.Place(player.transform.position + new Vector3(2.6f, 0f, -1.4f));
            if (player != null) Mailbox.Place(player.transform.position + new Vector3(4.6f, 0f, 1.2f));
            if (player != null) SeasonBoard.Place(player.transform.position + new Vector3(2.6f, 0f, -4.4f), player.transform.position);
            if (player != null) QuickRunSign.Place(player.transform.position + new Vector3(-2.6f, 0f, -5.8f));
            var cabinet = FindAnyObjectByType<FilingCabinet>(FindObjectsInactive.Include);
            if (cabinet != null) ItemWall.Place(cabinet.transform.position + new Vector3(-2.8f, 0f, 0.4f), Quaternion.LookRotation(Vector3.back));
            Pressure.Apply(0);
            if (SaveSystem.Data.meta.runsStarted == 0)
            {
                var portal = GameObject.Find("Portal");
                if (portal != null) StartHere.Place(portal.transform.position + Vector3.up * 4.2f);
            }
            RunRules.End();

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
