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

            var hud = FindAnyObjectByType<Hud>();
            if (hud == null) return;

            if (!string.IsNullOrEmpty(RunDirector.LastRunMessage))
            {
                hud.Toast(RunDirector.LastRunMessage, 6f);
                RunDirector.LastRunMessage = null;
            }
            else if (SaveSystem.Data.meta.runsStarted == 0)
            {
                hud.Toast("The lab. Walk into the portal to start your first run.", 6f);
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
