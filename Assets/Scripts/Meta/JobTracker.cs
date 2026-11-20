using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Watches the game for the things the noticeboard's jobs count, and says so on screen when one is finished.
    /// One per scene, on the Game object.
    /// </summary>
    public class JobTracker : MonoBehaviour
    {
        void OnEnable()
        {
            Enemy.Killed += OnKilled;
            FloorManager.FloorBuilt += OnFloorBuilt;
            FloorManager.DareWon += OnDare;
            Pickup.ItemCollected += OnItem;
            RunDirector.Extracted += OnExtracted;
            Jobs.Done += OnDone;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnKilled;
            FloorManager.FloorBuilt -= OnFloorBuilt;
            FloorManager.DareWon -= OnDare;
            Pickup.ItemCollected -= OnItem;
            RunDirector.Extracted -= OnExtracted;
            Jobs.Done -= OnDone;
        }

        void OnKilled(Enemy enemy)
        {
            if (!enemy.dropsLoot) return;
            Jobs.Count(JobKind.Kills);
            if (enemy.IsElite) Jobs.Count(JobKind.Elites);
            if (enemy is MimicEnemy) Jobs.Count(JobKind.Mimics);
            if (enemy is BossEnemy) Jobs.Count(JobKind.Bosses);
        }

        // Arriving on a floor past the first one of the scene is having gone down one.
        void OnFloorBuilt(FloorManager floors)
        {
            if (floors.FloorsBuilt > 1 && floors.biome != null && !floors.biome.isBottom) Jobs.Count(JobKind.Floors);
        }

        void OnDare(FloorManager floors) => Jobs.Count(JobKind.Dares);
        void OnItem(ItemDefinition item) => Jobs.Count(JobKind.Items);
        void OnExtracted(int samples) => Jobs.Count(JobKind.Extractions);

        void OnDone(Job job)
        {
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"Job done: {job.what} {job.pay} samples paid into the lab.", 5f);
            Sfx.Play(SfxId.Buy);
        }
    }
}
