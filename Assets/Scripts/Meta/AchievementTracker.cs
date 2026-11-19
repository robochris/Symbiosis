using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Watches the game for the things achievements are given for, and says so on screen when one is earned.
    /// One per scene, on the Game object.
    /// </summary>
    public class AchievementTracker : MonoBehaviour
    {
        int _hitsThisFloor;
        bool _floorCounted;
        PlayerController _watched;

        void OnEnable()
        {
            Achievements.Unlocked += Announce;
            Enemy.Killed += OnKilled;
            FloorManager.FloorBuilt += OnFloorBuilt;
            FloorManager.RoomChanged += OnRoomChanged;
            Pickup.ItemCollected += OnItem;
            EnemyMutations.ShellShattered += OnShell;
            LarvaPod.PodPopped += OnPod;
            RunDirector.Extracted += OnExtracted;
        }

        void OnDisable()
        {
            Achievements.Unlocked -= Announce;
            Enemy.Killed -= OnKilled;
            FloorManager.FloorBuilt -= OnFloorBuilt;
            FloorManager.RoomChanged -= OnRoomChanged;
            Pickup.ItemCollected -= OnItem;
            EnemyMutations.ShellShattered -= OnShell;
            LarvaPod.PodPopped -= OnPod;
            RunDirector.Extracted -= OnExtracted;
            if (_watched != null) _watched.Health.Damaged -= OnHit;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == _watched) return;
            if (_watched != null) _watched.Health.Damaged -= OnHit;
            _watched = player;
            if (_watched != null) _watched.Health.Damaged += OnHit;
        }

        void Announce(Achievement achievement)
        {
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"Achievement: {achievement.name}. Unlocked: {achievement.reward}.", 5f);
            Sfx.Play(SfxId.Item);
            foreach (var board in FindObjectsByType<TrophyBoard>(FindObjectsSortMode.None)) board.Refresh();
        }

        void OnHit(int amount, Vector3 direction)
        {
            if (amount > 0) _hitsThisFloor++;
        }

        void OnFloorBuilt(FloorManager floors)
        {
            _hitsThisFloor = 0;
            _floorCounted = false;
            if (floors.depth >= 2) Achievements.Unlock(AchievementId.Deeper);
        }

        // The way down (or the portals) opening is what "cleared the floor" means.
        void OnRoomChanged(FloorManager floors)
        {
            if (!floors.ExitOpen || _floorCounted) return;
            _floorCounted = true;
            Achievements.Unlock(AchievementId.FirstSteps);
            if (_hitsThisFloor == 0) Achievements.Unlock(AchievementId.Untouchable);
        }

        void OnKilled(Enemy enemy)
        {
            // The candle is a queen underneath, so it is asked about first.
            if (enemy is TrickCandle)
            {
                Achievements.Unlock(AchievementId.SweetTooth);
            }
            else if (enemy is AntQueen queen)
            {
                Achievements.Unlock(AchievementId.Regicide);
                if (queen.EggsPlaced > 0 && queen.EggsDestroyed == 0 && queen.EggsHatched == 0) Achievements.Unlock(AchievementId.HandsOff);
                else if (queen.EggsPlaced > 0 && queen.EggsDestroyed == queen.EggsPlaced) Achievements.Unlock(AchievementId.Omelette);
            }
            else if (enemy is BroodMother)
            {
                Achievements.Unlock(AchievementId.MothersDay);
            }
        }

        void OnItem(ItemDefinition item)
        {
            var player = PlayerController.Instance;
            if (player != null && player.Items != null && player.Items.Collected.Count >= Achievements.ItemsNeeded) Achievements.Unlock(AchievementId.Hoarder);
        }

        void OnShell()
        {
            var meta = SaveSystem.Data.meta;
            meta.shellsShattered++;
            if (meta.shellsShattered >= Achievements.ShellsNeeded) Achievements.Unlock(AchievementId.ShellShock);
        }

        void OnPod()
        {
            var meta = SaveSystem.Data.meta;
            meta.podsPopped++;
            if (meta.podsPopped >= Achievements.PodsNeeded) Achievements.Unlock(AchievementId.BubbleWrap);
        }

        void OnExtracted(int samples)
        {
            if (samples >= Achievements.SamplesNeeded) Achievements.Unlock(AchievementId.FullPockets);
        }
    }
}
