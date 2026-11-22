using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Shrunk.Tests
{
    /// <summary>For new players: the daily parcel, beginner's luck, the floating use markers, and what to go back for.</summary>
    public class WelcomeTests
    {
        [SetUp]
        public void Fresh()
        {
            SaveSystem.Profile = "tests";
            SaveSystem.Wipe();
            GameSettings.Reset();
        }

        [TearDown]
        public void CleanUp()
        {
            DailyParcel.Off = true;
            DailyParcel.TodayOverride = null;
            RunDirector.BeginnersLuckOff = true;
            SaveSystem.Wipe();
        }

        [UnityTest]
        public IEnumerator A_parcel_waits_in_the_lab_each_day_and_grows_with_the_streak()
        {
            DailyParcel.Off = false;
            DailyParcel.TodayOverride = new DateTime(2026, 3, 1);
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var parcel = DailyParcel.Current;
            Assert.IsNotNull(parcel, "no parcel in the lab");
            int before = SaveSystem.Data.meta.samples;
            Assert.AreEqual(DailyParcel.BaseSamples, parcel.Open());
            Assert.AreEqual(before + DailyParcel.BaseSamples, SaveSystem.Data.meta.samples);
            Assert.IsFalse(DailyParcel.Waiting, "a second parcel the same day");
            Assert.IsNull(DailyParcel.Place(Vector3.zero));

            // The next day: a bigger one. A day missed: back to the start.
            DailyParcel.TodayOverride = new DateTime(2026, 3, 2);
            Assert.IsTrue(DailyParcel.Waiting);
            Assert.AreEqual(2, DailyParcel.StreakIfOpened);
            Assert.AreEqual(DailyParcel.BaseSamples + DailyParcel.PerDay, DailyParcel.SamplesFor(2));
            DailyParcel.TodayOverride = new DateTime(2026, 3, 4);
            Assert.AreEqual(1, DailyParcel.StreakIfOpened);

            // A whole week: lucky coins as well.
            SaveSystem.Data.meta.parcelStreak = DailyParcel.MaxStreak - 1;
            SaveSystem.Data.meta.parcelDate = "2026-03-03";
            int coins = SaveSystem.Data.meta.luckyCoins;
            var weekly = DailyParcel.Place(Vector3.zero);
            yield return null;
            weekly.Open();
            Assert.AreEqual(coins + DailyParcel.LuckyCoins, SaveSystem.Data.meta.luckyCoins);
        }

        [UnityTest]
        public IEnumerator The_first_runs_start_with_an_item()
        {
            RunDirector.BeginnersLuckOff = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            Assert.AreEqual(1, player.Items.Collected.Count, "no beginner's luck on the first run");
            int? pinned = Progress.LevelOverride;
            Progress.LevelOverride = 1;
            string goal = RunDirector.NextGoal();
            Progress.LevelOverride = pinned;
            StringAssert.StartsWith("Next: level", goal);
        }

        [UnityTest]
        public IEnumerator Usable_things_have_a_floating_marker_instead_of_a_mat()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            yield return null;
            Assert.Greater(UseMarker.All.Count, 5, "few markers in the lab");
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.name == "Mat") Assert.IsFalse(renderer.enabled, "a mat still showing under " + renderer.transform.parent.name);
            }

            // Walk up to one and it shows the key.
            var marker = UseMarker.All[0];
            var player = PlayerController.Instance;
            // Only standing there, not using it (the tests' step-on-it shortcut would start whatever it is).
            Interact.AutoUse = false;
            player.Teleport(marker.transform.position + Vector3.up * 0.05f);
            yield return null;
            yield return null;
            bool inReach = marker.InReach;
            Interact.AutoUse = true;
            Assert.IsTrue(inReach, "standing at it and it is not in reach");
        }
            [UnityTest]
        public IEnumerator Hats_are_earned_by_playing_and_tried_on_at_the_stand()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            var stand = HatStand.Current;
            Assert.IsNotNull(stand, "no hat stand in the lab");
            var player = PlayerController.Instance;
            Assert.IsNotNull(player.GetComponent<PlayerHat>(), "the player has no head for hats");

            // A new player has the beanie, and nothing else yet.
            Assert.AreEqual(2, Hats.EarnedCount);
            Assert.AreEqual("Beanie", Hats.WearNext().name);
            Assert.AreEqual("No hat", Hats.WearNext().name);
            Assert.AreEqual("Party Hat", Hats.NextToEarn().name);

            // Three runs started: the party hat, announced once.
            SaveSystem.Data.meta.runsStarted = 3;
            var fresh = Hats.TakeNew();
            Assert.AreEqual(1, fresh.Count);
            Assert.AreEqual("Party Hat", fresh[0].name);
            Assert.AreEqual(0, Hats.TakeNew().Count, "the same hat announced twice");
            Assert.AreEqual("Beanie", Hats.WearNext().name);
            Assert.AreEqual("Party Hat", Hats.WearNext().name);
            Assert.AreEqual("Party Hat", Hats.All[Hats.Worn].name);
            yield return null;
            Assert.IsNotNull(player.visual.Find("Hat/Pom"), "the party hat is not on the head");
        }

        [UnityTest]
        public IEnumerator First_time_tips_are_said_once_each()
        {
            FirstTips.Off = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.IsFalse(FirstTips.Seen(FirstTips.Tip.Coins));
            RunState.AddCoins(3);
            for (float t = 0f; t < 10f && !FirstTips.Seen(FirstTips.Tip.Coins); t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(FirstTips.Seen(FirstTips.Tip.Coins), "no tip about coins");
            foreach (FirstTips.Tip tip in System.Enum.GetValues(typeof(FirstTips.Tip))) Assert.IsNotEmpty(FirstTips.Line(tip));
            FirstTips.Off = true;
        }

        [UnityTest]
        public IEnumerator Quick_kills_chain_into_a_combo_that_pays_coins()
        {
            KillCombo.Off = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            KillCombo.StartRun();
            int pickups = Pickup.All.Count;
            for (int i = 0; i < KillCombo.Milestone; i++) KillCombo.Register();
            Assert.AreEqual(KillCombo.Milestone, KillCombo.Chain);
            Assert.AreEqual(KillCombo.Milestone, KillCombo.RunBest);
            Assert.Greater(Pickup.All.Count, pickups, "no coins for a chain of five");
            Assert.GreaterOrEqual(SaveSystem.Data.meta.bestCombo, KillCombo.Milestone);

            // A pause longer than the window breaks it.
            yield return new WaitForSeconds(KillCombo.Window + 0.2f);
            KillCombo.Register();
            Assert.AreEqual(1, KillCombo.Chain);
            KillCombo.Off = true;
        }

        [UnityTest]
        public IEnumerator A_shrine_offers_one_trade_and_keeps_its_word()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            Shrine.ChanceForAll = 1f;
            floors.BuildFloor(2);
            yield return null;
            Shrine.ChanceForAll = 0f;
            var shrine = floors.ShrineHere;
            Assert.IsNotNull(shrine, "no shrine with the chance pinned to always");

            // Each deal, made directly: something given, something got.
            shrine.deal = Shrine.Deal.GoldForGear;
            Assert.IsFalse(shrine.Take(player), "an item for coins nobody had");
            RunState.AddCoins(Shrine.GoldPrice);
            int items = Pickup.All.Count;
            Assert.IsTrue(shrine.Take(player));
            Assert.AreEqual(0, RunState.Coins);
            Assert.Greater(Pickup.All.Count, items, "no item for the coins");
            Assert.IsFalse(shrine.Take(player), "a second trade");

            floors.BuildFloor(3);
            yield return null;
            Assert.IsNull(floors.ShrineHere, "a shrine with the chance pinned to never");
            var made = Shrine.Create(floors.RoomAt(floors.Layout.start.cell), player.transform.position + Vector3.forward * 3f, Shrine.Deal.BloodForGold);
            int health = player.Health.Current;
            Assert.IsTrue(made.Take(player));
            Assert.AreEqual(health - 1, player.Health.Current);
            Assert.AreEqual(Shrine.GoldGained, RunState.Coins);
        }

        [UnityTest]
        public IEnumerator Pets_follow_and_hits_buzz()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.IsNotNull(PetJar.Current, "no pet jar in the lab");
            Assert.IsNull(PetFollower.Body, "a pet before any were earned");
            SaveSystem.Data.meta.extractions = 1;
            Assert.AreEqual("Ladybird", Pets.ChooseNext().name);
            yield return null;
            Assert.IsNotNull(PetFollower.Body, "the ladybird did not come out");
            var player = PlayerController.Instance;
            for (int i = 0; i < 40; i++) yield return null;
            Assert.Less(Vector3.Distance(PetFollower.Body.position, player.transform.position), 4f, "the pet is not keeping up");

            int buzzes = Haptics.Buzzes;
            player.Health.TakeDamage(1, Vector3.forward);
            Assert.AreEqual(buzzes + 1, Haptics.Buzzes, "a hit with no buzz");
        }

        [UnityTest]
        public IEnumerator Pressure_makes_enemies_tougher_and_samples_worth_more()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.IsNotNull(PressureGauge.Current, "no pressure gauge in the lab");
            Assert.AreEqual(0, Pressure.Unlocked, "pressure before any boss");
            SaveSystem.Data.meta.bossesBeaten = 1;
            Assert.AreEqual(1, Pressure.Unlocked);
            Assert.AreEqual(1, Pressure.Next());
            Assert.AreEqual(0, Pressure.Next(), "the dial went past what is open");
            Pressure.Next();

            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.AreEqual(1, Pressure.Active);
            Assert.AreEqual(1.25f, RunDirector.RewardMultiplier(1), 0.001f);
            var prefab = FloorManager.Instance.biome.spawnTable[0].prefab;
            var enemy = UnityEngine.Object.Instantiate(prefab, PlayerController.Instance.transform.position + Vector3.forward * 12f, Quaternion.identity);
            yield return null;
            Assert.AreEqual(Mathf.CeilToInt(prefab.GetComponent<Health>().maxHealth * 1.2f), enemy.Health.maxHealth, "no extra health under pressure");
            UnityEngine.Object.Destroy(enemy.gameObject);

            // A world's boss beaten at it opens the next level; three, the hard hat.
            Pressure.BossBeaten();
            Assert.AreEqual(2, Pressure.Unlocked);
            SaveSystem.Data.meta.pressureBest = 3;
            Assert.IsTrue(Hats.Earned(Hats.All.Length - 1), "no hard hat at pressure 3");
            Pressure.Apply(0);
            Assert.AreEqual(1f, Enemy.HealthScale);
        }

        [UnityTest]
        public IEnumerator A_boss_leaves_its_own_item_the_first_time_and_then_it_joins_the_pool()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            Assert.IsTrue(floors.SetBiome("forest"));
            BossEnemy toadPrefab = floors.biome.boss;
            Assert.AreEqual(15, BossEnemy.Trophies.Count, "a world boss without an item");
            foreach (var pair in BossEnemy.Trophies) Assert.IsNotNull(GameAssets.Instance.items.Get(pair.Value), pair.Key + "'s item is missing");
            Assert.AreEqual(0, BossEnemy.Pool().Count, "a boss item loose before any boss was beaten");

            var toad = UnityEngine.Object.Instantiate(toadPrefab, PlayerController.Instance.transform.position + Vector3.forward * 15f, Quaternion.identity);
            yield return null;
            int before = Pickup.All.Count;
            toad.DropRewards();
            ItemDefinition dropped = null;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item) dropped = pickup.item;
            Assert.IsNotNull(dropped);
            Assert.AreEqual(ItemType.Croak, dropped.type, "the Toad's first win did not give the Croak");
            Assert.Contains(ItemType.Croak, BossEnemy.Pool(), "the Croak did not join the pool");
            UnityEngine.Object.Destroy(toad.gameObject);
        }

        [UnityTest]
        public IEnumerator A_first_run_is_caught_and_new_players_have_goals()
        {
            SafetyNet.Off = false;
            StarterGoals.Off = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            player.Health.ClearInvulnerability();
            player.Health.Restore(1);
            player.Health.TakeDamage(1, Vector3.forward);
            Assert.IsFalse(player.Health.IsDead, "the first run's safety net did not catch the hit");
            Assert.AreEqual(1, SafetyNet.Used);

            int samples = SaveSystem.Data.meta.samples;
            StarterGoals.Tick(StarterGoals.Goal.Shop);
            StarterGoals.Tick(StarterGoals.Goal.Shop);
            Assert.AreEqual(samples + StarterGoals.Pay, SaveSystem.Data.meta.samples, "a goal paid twice, or not at all");
            Assert.IsTrue(StarterGoals.Done(StarterGoals.Goal.Shop));
            SafetyNet.Off = true;
            StarterGoals.Off = true;
        }

        [UnityTest]
        public IEnumerator A_shot_past_a_dodge_is_a_close_call()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            int before = CloseCalls.Count;
            player.Input.overrideFrame = new InputFrame { dodge = true, move = Vector3.right };
            yield return null;
            player.Input.overrideFrame = new InputFrame();
            Assert.IsTrue(player.IsDodging, "no dodge");
            var prefab = FloorManager.Instance.biome.spawnTable[0].prefab;
            // A spitter's shot, made by hand, right beside the player.
            Bullet shot = null;
            foreach (var entry in FloorManager.Instance.biome.spawnTable)
            {
                var shooter = entry.prefab != null ? entry.prefab.GetComponent<ShooterEnemy>() : null;
                if (shooter != null && shooter.projectilePrefab != null) { shot = UnityEngine.Object.Instantiate(shooter.projectilePrefab); break; }
            }
            Assert.IsNotNull(shot, "no shooter to borrow a shot from");
            shot.Launch(player.transform.position + Vector3.right * 0.8f + Vector3.up * 0.6f, Vector3.forward, 0.1f, new Shot { damage = 1 }, 5f, Team.Enemy, null);
            yield return null;
            yield return null;
            Assert.Greater(CloseCalls.Count, before, "a shot right past a dodge was not a close call");
            Assert.Greater(CloseCalls.BoostUntil, Time.time);
            UnityEngine.Object.Destroy(shot.gameObject);
        }

        [UnityTest]
        public IEnumerator A_vault_opens_to_its_code_and_its_trial_pays()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Health.SetInvulnerable(600f);
            CodeVault.ChanceForAll = 1f;
            CodeVault vault = null;
            for (int floor = 2; floor <= 5 && vault == null; floor++)
            {
                floors.BuildFloor(floor);
                yield return null;
                vault = floors.Vault;
            }
            CodeVault.ChanceForAll = 0f;
            Assert.IsNotNull(vault, "no vault with the chance pinned to always");
            Assert.IsNotNull(vault.Clue, "no slate with the code");
            Assert.That(WorldPrompt.For(vault.Clue.transform.position), Does.Contain(CodeVault.ColourNames[vault.code[0]]));

            // Wrong, then right.
            Assert.IsFalse(vault.Open);
            vault.Turn(0);
            if (vault.code[0] == 1 && vault.code[1] == 0 && vault.code[2] == 0) Assert.IsTrue(vault.Open);
            else Assert.IsFalse(vault.Open, "one turn of one dial opened it");
            // Round to the start, then to the code.
            for (int t = 1; t < CodeVault.Colours.Length && !vault.Open; t++) vault.Turn(0);
            for (int dial = 0; dial < 3 && !vault.Open; dial++)
                for (int t = 0; t < vault.code[dial] && !vault.Open; t++) vault.Turn(dial);
            Assert.IsTrue(vault.Open, "the right code did not open the vault");

            // The trial: in, watch, step.
            player.Teleport(vault.room.Centre + Vector3.up * 0.05f);
            for (float t = 0f; t < 2f && vault.Round == 0; t += Time.deltaTime) yield return null;
            Assert.AreEqual(1, vault.Round, "the trial did not start");
            yield return new WaitForSeconds(0.6f + vault.Order.Count * 0.8f);
            int wrong = (vault.Order[0] + 1) % 3;
            vault.StepOn(wrong);
            Assert.AreEqual(1, vault.Mistakes);
            for (int round = 1; round <= CodeVault.Rounds; round++)
            {
                yield return new WaitForSeconds(0.6f + vault.Order.Count * 0.8f);
                var order = new System.Collections.Generic.List<int>(vault.Order);
                foreach (int plate in order) vault.StepOn(plate);
            }
            Assert.IsTrue(vault.Cleared, "three rounds right and the vault is not cleared");
            Assert.AreEqual(1, SaveSystem.Data.meta.vaultsCleared);
        }

        [UnityTest]
        public IEnumerator The_lab_has_mail_an_item_wall_and_a_quick_run()
        {
            SaveSystem.Data.meta.runsStarted = 1;
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.IsNotNull(Mailbox.Current, "no mailbox");
            Assert.IsNotNull(ItemWall.Current, "no item wall");
            Assert.IsNotNull(QuickRunSign.Current, "no quick-run sign");
            Assert.AreEqual(1, Mailbox.Waiting().Count, "the first letter has not come");
            int samples = SaveSystem.Data.meta.samples;
            var letter = Mailbox.Current.ReadNext();
            Assert.AreEqual("welcome", letter.id);
            Assert.AreEqual(samples + letter.gift, SaveSystem.Data.meta.samples);
            Assert.AreEqual(0, Mailbox.Waiting().Count, "a letter read twice");

            Assert.IsTrue(QuickRunSign.Current.Begin());
            yield return new WaitForSeconds(1.5f);
            yield return null;
            Assert.IsNotNull(RunDirector.Instance, "the quick run did not start");
            Assert.IsTrue(RunDirector.Instance.IsQuick);
            var floors = FloorManager.Instance;
            floors.BuildFloor(2);
            yield return null;
            Assert.IsTrue(FloorLayout.IsBossFloor(floors.NextFloorNumber()), "a quick run's second floor does not lead to the boss");
        }

        [UnityTest]
        public IEnumerator The_rival_bolts_at_half_health_and_stays_the_third_time()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var prefab = GameAssets.Instance.rivalPrefab;
            Assert.IsNotNull(prefab, "no rival");
            var player = PlayerController.Instance;
            player.Health.SetInvulnerable(600f);
            var rival = (RivalVoid)UnityEngine.Object.Instantiate(prefab, player.transform.position + Vector3.forward * 9f, Quaternion.identity);
            yield return null;
            Assert.IsFalse(rival.Final);
            rival.Health.TakeDamage(rival.Health.maxHealth / 2 + 1, Vector3.forward);
            yield return null;
            yield return null;
            Assert.IsTrue(rival.Escaped, "it did not bolt at half health");
            Assert.AreEqual(1, SaveSystem.Data.meta.rivalMet);

            SaveSystem.Data.meta.rivalMet = RivalVoid.Meetings - 1;
            var last = (RivalVoid)UnityEngine.Object.Instantiate(prefab, player.transform.position + Vector3.forward * 9f, Quaternion.identity);
            yield return null;
            Assert.IsTrue(last.Final);
            last.Health.TakeDamage(last.Health.maxHealth / 2 + 1, Vector3.forward);
            yield return null;
            Assert.IsFalse(last.Escaped, "it ran the third time");
            last.Health.Kill();
            yield return null;
            Assert.AreEqual(1, SaveSystem.Data.meta.rivalBeaten);
            bool bandana = false;
            for (int i = 0; i < Hats.All.Length; i++) if (Hats.All[i].name == "Rival's Bandana") bandana = Hats.Earned(i);
            Assert.IsTrue(bandana, "no bandana for beating the rival");
        }

        [UnityTest]
        public IEnumerator A_caged_larva_comes_along_and_the_rule_of_the_day_counts()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            RescueCage.ChanceForAll = 1f;
            floors.BuildFloor(2);
            yield return null;
            RescueCage.ChanceForAll = 0f;
            var cage = floors.Cage;
            Assert.IsNotNull(cage, "no cage with the chance pinned to always");
            cage.Open();
            Assert.IsNotNull(cage.Freed, "nothing came out of the cage");
            var freed = cage.Freed;
            floors.BuildFloor(3);
            yield return null;
            yield return null;
            Assert.IsTrue(freed == null, "the larva stayed on into the next floor");

            RunRules.Off = false;
            RunRules.Begin(true);
            Assert.AreNotEqual(RunRules.Rule.None, RunRules.Active);
            RunRules.TodayOverride = new System.DateTime(2026, 1, 2);
            var a = RunRules.Today;
            RunRules.TodayOverride = new System.DateTime(2026, 1, 3);
            Assert.AreNotEqual(a, RunRules.Today, "the same rule two days running");
            RunRules.TodayOverride = null;
            RunRules.End();
            RunRules.Off = true;
        }

        [Test]
        public void Every_world_has_music_of_its_own_in_two_layers()
        {
            float[] lab = Music.Render("lab", false);
            float[] forest = Music.Render("forest", false);
            float[] fight = Music.Render("forest", true);
            Assert.Greater(lab.Length, Music.Rate * 4, "a loop under four seconds");
            Assert.AreEqual(forest.Length, fight.Length, "the two layers would drift apart");
            float Loudest(float[] data) { float most = 0f; foreach (float v in data) most = Mathf.Max(most, Mathf.Abs(v)); return most; }
            Assert.Greater(Loudest(forest), 0.05f, "silent");
            Assert.Greater(Loudest(fight), 0.05f, "a silent fight layer");
            Assert.LessOrEqual(Loudest(fight), 1f);
            bool differs = false;
            for (int i = 0; i < Mathf.Min(lab.Length, forest.Length) && !differs; i += 997) differs = Mathf.Abs(lab[i] - forest[i]) > 0.01f;
            Assert.IsTrue(differs, "the lab and the forest play the same tune");
        }

        [UnityTest]
        public IEnumerator The_music_follows_the_world_and_the_fights()
        {
            Music.Off = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            Assert.AreEqual(floors.biome.id, Music.Playing, "the music is not the world's own");
            Assert.IsTrue(floors.SetBiome("forest"));
            floors.BuildFloor(2);
            yield return null;
            Assert.AreEqual("forest", Music.Playing, "the music did not change with the world");
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.AreEqual("lab", Music.Playing);
            Music.Off = true;
        }

        [UnityTest]
        public IEnumerator The_season_track_pays_as_it_goes_and_starts_again_each_season()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.IsNotNull(SeasonBoard.Current, "no season board");
            Season.Off = false;
            Season.NowOverride = new System.DateTime(2026, 3, 2);
            int samples = SaveSystem.Data.meta.samples;
            Season.Add(Season.XpPerLevel * 2);
            Assert.AreEqual(2, Season.Level);
            Assert.AreEqual(samples + 2 * Season.Samples, SaveSystem.Data.meta.samples);
            Season.Add(Season.XpPerLevel * 3);
            Assert.AreEqual(Season.Coins, SaveSystem.Data.meta.luckyCoins, "level five paid no lucky coins");
            Season.Add(Season.XpPerLevel * 40);
            Assert.AreEqual(Season.Levels, Season.Level);
            Assert.AreEqual(1, SaveSystem.Data.meta.seasonStars);
            Season.NowOverride = new System.DateTime(2026, 3, 2).AddDays(Season.Days);
            Assert.AreEqual(0, Season.Level, "the track did not start again with a new season");
            Season.NowOverride = null;
            Season.Off = true;
        }

        [UnityTest]
        public IEnumerator The_first_floor_ever_is_gentle_and_the_second_weapon_comes_after_one_run()
        {
            SafetyNet.Off = false;
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            Assert.IsTrue(floors.FirstFloorEver);
            bool waiting = false;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item && pickup.price == 0) waiting = true;
            Assert.IsTrue(waiting, "no item waiting on the first floor ever");
            SafetyNet.Off = true;

            Assert.IsFalse(Weapons.Unlocked(WeaponKind.Sprinkler));
            SaveSystem.Data.meta.deaths = 1;
            Assert.IsTrue(Weapons.Unlocked(WeaponKind.Sprinkler), "no second weapon after a first run");
        }

        [UnityTest]
        public IEnumerator Clearing_a_room_is_celebrated_and_the_hole_takes_the_coins()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            Pickup.Coin(player.transform.position + Vector3.forward * 5f, 3);
            Pickup.Coin(player.transform.position + Vector3.forward * 30f, 3);
            int coins = RunState.Coins;
            Assert.AreEqual(1, Pickup.SweepUp(player.transform.position, 9f));
            Assert.AreEqual(coins + 3, RunState.Coins);
        }

        [UnityTest]
        public IEnumerator New_weapons_a_parry_and_a_last_stand()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            Assert.IsFalse(Weapons.Unlocked(WeaponKind.Boomerang));
            SaveSystem.Data.meta.specimens.Add("TheJackInTheBox");
            Assert.IsTrue(Weapons.Unlocked(WeaponKind.Boomerang));
            Assert.AreEqual(1, Weapons.Info(WeaponKind.Boomerang).returns);
            Assert.AreEqual(2, Weapons.Info(WeaponKind.MarbleLauncher).bounce);

            // A shot turned round is the player's.
            var player = PlayerController.Instance;
            Bullet shot = null;
            foreach (var entry in FloorManager.Instance.biome.spawnTable)
            {
                var shooter = entry.prefab != null ? entry.prefab.GetComponent<ShooterEnemy>() : null;
                if (shooter != null && shooter.projectilePrefab != null) { shot = UnityEngine.Object.Instantiate(shooter.projectilePrefab); break; }
            }
            shot.Launch(player.transform.position + Vector3.forward * 6f, Vector3.back, 0.1f, new Shot { damage = 1 }, 5f, Team.Enemy, null);
            shot.Reflect();
            Assert.AreEqual(Team.Player, shot.Team);
            UnityEngine.Object.Destroy(shot.gameObject);

            // The last stand: at one health, a kill gives one back.
            Juice.Off = false;
            player.Health.SetMaxHealth(4, true);
            player.Health.Restore(1);
            var prefab = FloorManager.Instance.biome.spawnTable[0].prefab;
            var enemy = UnityEngine.Object.Instantiate(prefab, player.transform.position + Vector3.forward * 12f, Quaternion.identity);
            yield return null;
            enemy.Health.Kill();
            yield return null;
            Assert.AreEqual(2, player.Health.Current, "no last stand");
            Juice.Off = true;
        }

        [UnityTest]
        public IEnumerator Synergies_and_the_glass_heart()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var items = player.Items;
            var library = GameAssets.Instance.items;
            int bounce = items.Stats.bounce;
            items.Add(library.Get(ItemType.RubberBand));
            items.Add(library.Get(ItemType.YoYo));
            Assert.IsTrue(Synergies.Has("Elastic", items));
            Assert.AreEqual(bounce + 2, items.Stats.bounce, "Elastic gave no extra bounce");
            Assert.Contains("Elastic", SaveSystem.Data.meta.synergiesFound);

            int damage = items.Stats.damageBonus;
            items.Add(library.Get(ItemType.GlassHeart));
            Assert.AreEqual(damage + 2, items.Stats.damageBonus);
            player.Health.ClearInvulnerability();
            player.Health.SetMaxHealth(5, true);
            player.Health.TakeDamage(1, Vector3.forward);
            Assert.IsTrue(items.GlassBroken);
            Assert.AreEqual(damage, items.Stats.damageBonus, "the heart still counts once broken");
        }

        [UnityTest]
        public IEnumerator The_new_powers_hop_stomp_hoover_and_shrink()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var power = player.Power;
            Assert.IsFalse(HeroPower.Unlocked(PowerKind.Hop));
            SaveSystem.Data.meta.secretsFound = 1;
            Assert.IsTrue(HeroPower.Unlocked(PowerKind.Hop));

            power.SetKind(PowerKind.Hop);
            power.SetMeter(1f);
            Vector3 from = player.transform.position;
            Assert.IsTrue(power.TryActivate());
            yield return null;
            Assert.Greater(Vector3.Distance(from, player.transform.position), 1f, "the hop went nowhere");

            power.SetKind(PowerKind.Tiny);
            float speed = player.Items.Stats.moveSpeedMultiplier;
            power.SetMeter(1f);
            Assert.IsTrue(power.TryActivate());
            Assert.Greater(player.Items.Stats.moveSpeedMultiplier, speed + 0.4f, "tiny is not faster");
        }

        [UnityTest]
        public IEnumerator Sleepers_wake_shinies_pay_and_bosses_keep_records()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var player = PlayerController.Instance;
            var prefab = FloorManager.Instance.biome.spawnTable[0].prefab;
            var sleeper = UnityEngine.Object.Instantiate(prefab, player.transform.position + Vector3.forward * 10f, Quaternion.identity);
            yield return null;
            sleeper.FallAsleep();
            yield return null;
            Assert.IsTrue(sleeper.Asleep);
            player.Teleport(sleeper.transform.position + Vector3.back * 1.5f);
            yield return null;
            yield return null;
            Assert.IsFalse(sleeper.Asleep, "walking right up to it did not wake it");

            var shiny = UnityEngine.Object.Instantiate(prefab, player.transform.position + Vector3.forward * 14f, Quaternion.identity);
            yield return null;
            shiny.MakeShiny();
            int samples = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Sample) samples++;
            shiny.Health.Kill();
            yield return null;
            int after = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Sample) after++;
            Assert.GreaterOrEqual(after - samples, 6, "a shiny paid no more than anything else");
            Assert.AreEqual(1, SaveSystem.Data.meta.shiniesFound);

            Assert.IsTrue(FloorManager.Instance.SetBiome("forest"));
            FloorManager.Instance.BuildFloor(5);
            yield return null;
            player.Teleport(FloorManager.Instance.ArenaCentre + new Vector3(0f, 0.05f, -9f));
            for (float t = 0f; t < 4f && FloorManager.Instance.Boss == null; t += Time.unscaledDeltaTime) yield return null;
            var boss = FloorManager.Instance.Boss;
            Assert.IsNotNull(boss);
            yield return null;
            yield return null;
            boss.Health.Kill();
            yield return null;
            Assert.IsNotNull(BossRecords.Best("TheFroglet"), "no best time kept");
            Assert.IsTrue(BossRecords.Medal("TheFroglet"), "no medal for a fight without a hit");
        }

        [UnityTest]
        public IEnumerator Giants_and_low_gravity_twists()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            floors.twistOverride = FloorTwist.Giants;
            floors.BuildFloor(3);
            yield return null;
            var prefab = floors.biome.spawnTable[0].prefab;
            var giant = UnityEngine.Object.Instantiate(prefab, player.transform.position + Vector3.forward * 12f, Quaternion.identity);
            yield return null;
            Assert.AreEqual(Mathf.CeilToInt(prefab.GetComponent<Health>().maxHealth * FloorTwists.GiantsHealth), giant.Health.maxHealth);
            Assert.AreEqual(FloorTwists.GiantsSize, giant.BaseScale, 0.01f);
            UnityEngine.Object.Destroy(giant.gameObject);

            floors.twistOverride = FloorTwist.LowGravity;
            floors.BuildFloor(4);
            yield return null;
            Assert.IsTrue(player.Items.Stats.hoverDodge, "no floating dodge in low gravity");
            floors.twistOverride = null;
            floors.BuildFloor(5);
            yield return null;
            Assert.AreEqual(1f, Enemy.TwistHealth);
        }

        [UnityTest]
        public IEnumerator The_lab_has_a_jukebox_a_fish_tank_and_a_cat()
        {
            SceneManager.LoadScene("Lab");
            yield return null;
            yield return null;
            Assert.IsNotNull(Jukebox.Current, "no jukebox");
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<FishTank>(), "no fish tank");
            Assert.IsNotNull(UnityEngine.Object.FindAnyObjectByType<LabCat>(), "no cat");
            Assert.AreEqual("ants", Jukebox.Current.Next(), "the first world's tune is not next after the lab's");
            Assert.AreEqual("lab", Jukebox.Current.Next(), "a tune from a world not yet visited");
        }

        [UnityTest]
        public IEnumerator A_timed_room_pays_if_it_is_cleared_in_time()
        {
            SceneManager.LoadScene("Run");
            yield return null;
            yield return null;
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            player.Health.SetInvulnerable(600f);
            RoomChallenge.ChanceForAll = 1f;
            floors.BuildFloor(2);
            yield return null;
            foreach (var node in floors.Layout.ordered)
            {
                if (node.type != RoomType.Normal) continue;
                player.Teleport(floors.RoomAt(node.cell).Centre + Vector3.up * 0.05f);
                break;
            }
            for (float t = 0f; t < 3f && !floors.IsLocked; t += Time.deltaTime) yield return null;
            Assert.IsTrue(RoomChallenge.Timed, "no clock on the fight");
            int items = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item) items++;
            foreach (var enemy in new System.Collections.Generic.List<Enemy>(floors.LiveEnemies)) enemy.Health.Kill();
            for (float t = 0f; t < 3f && floors.IsLocked; t += Time.deltaTime) yield return null;
            int after = 0;
            foreach (var pickup in Pickup.All) if (pickup.kind == PickupKind.Item) after++;
            Assert.Greater(after, items, "nothing for clearing in time");
            RoomChallenge.ChanceForAll = 0f;
        }
    }
}
