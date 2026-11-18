using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Greets the player in the lab with how the last run went.</summary>
    public class LabDirector : MonoBehaviour
    {
        void Start()
        {
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
