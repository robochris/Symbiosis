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
            FinchNotes.Found += OnNote;
            FloorManager.DareWon += OnDare;
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
            FinchNotes.Found -= OnNote;
            FloorManager.DareWon -= OnDare;
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
            if (enemy is MimicEnemy)
            {
                var meta = SaveSystem.Data.meta;
                meta.mimicsKilled++;
                if (meta.mimicsKilled >= Achievements.MimicsNeeded) Achievements.Unlock(AchievementId.LooseChange);
            }

            if (enemy is MachineBoss) Achievements.Unlock(AchievementId.Reboot);

            if (enemy is CatBoss)
            {
                Achievements.Unlock(AchievementId.PerformanceReview);
            }
            else if (enemy is IceMaker)
            {
                Achievements.Unlock(AchievementId.Defrosted);
            }
            else if (enemy is RobotVacuum vacuum)
            {
                Achievements.Unlock(AchievementId.SpringClean);
                if (vacuum.DockFailed) Achievements.Unlock(AchievementId.Unplugged);
            }
            else if (enemy is TrickCandle)
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
            var library = GameAssets.Instance != null ? GameAssets.Instance.items : null;
            if (library != null && library.items != null && FilingCabinet.Filed >= library.items.Length) Achievements.Unlock(AchievementId.FiledAway);
        }

        void OnDare(FloorManager floors)
        {
            var meta = SaveSystem.Data.meta;
            meta.daresWon++;
            SaveSystem.Save();
            if (meta.daresWon >= Achievements.DaresNeeded) Achievements.Unlock(AchievementId.AskedForIt);
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

        void OnNote(FinchNote note)
        {
            if (FinchNotes.Count >= FinchNotes.All.Length) Achievements.Unlock(AchievementId.PaperTrail);
        }

        void OnExtracted(int samples)
        {
            if (samples >= Achievements.SamplesNeeded) Achievements.Unlock(AchievementId.FullPockets);
            if (TimeClock.On) Achievements.Unlock(AchievementId.TimeAndAHalf);
        }
    }
}
