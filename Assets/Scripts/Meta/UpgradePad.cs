using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>One upgrade at the lab's workbench. Step onto its pad to buy the next level with banked samples.</summary>
    public class UpgradePad : MonoBehaviour
    {
        public UpgradeDefinition upgrade;
        [Tooltip("One object per level; they switch on as levels are bought, so progress shows in the lab itself.")]
        public GameObject[] levelMarkers;
        public StepPad pad = new StepPad { radius = 1f };

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Refresh();
        }

        void OnDisable()
        {
            WorldPrompt.Remove(Prompt);
        }

        public void Refresh()
        {
            if (upgrade == null || levelMarkers == null) return;
            int level = Upgrades.Level(upgrade.type);
            for (int i = 0; i < levelMarkers.Length; i++)
            {
                if (levelMarkers[i] != null) levelMarkers[i].SetActive(i < level);
            }
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || upgrade == null) return;
            if (pad.SteppedOn(transform.position, player.transform.position) && Upgrades.TryBuy(upgrade)) Refresh();
        }

        string Prompt(Vector3 playerPosition)
        {
            if (upgrade == null || pad.Distance(transform.position, playerPosition) > 2f) return null;

            int level = Upgrades.Level(upgrade.type);
            string text = $"{upgrade.displayName} ({level}/{upgrade.MaxLevel}): {upgrade.description}";
            int cost = Upgrades.NextCost(upgrade);
            if (cost < 0) return text + "   ·   fully upgraded";

            int have = SaveSystem.Data.meta.samples;
            return have >= cost
                ? text + $"   ·   {cost} samples ({Interact.Verb.ToLower()} to buy)"
                : text + $"   ·   {cost} samples (you have {have})";
        }
    }
}
