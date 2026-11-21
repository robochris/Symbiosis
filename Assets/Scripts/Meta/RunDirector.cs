using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shrunk
{
    /// <summary>
    /// Runs one expedition from the lab's portal to coming home: applies lab upgrades at the start, saves at the start
    /// of every floor, and settles up when the player extracts or dies.
    /// </summary>
    public class RunDirector : MonoBehaviour
    {
        public static RunDirector Instance { get; private set; }
        /// <summary>What happened on the last run, for the lab to show when the player arrives.</summary>
        public static string LastRunMessage;
        /// <summary>The player has cashed out, carrying this many samples (before the multiplier).</summary>
        public static event System.Action<int> Extracted;

        /// <summary>Set by the lab's hidden door: the next run is not a run but the trip to the bottom.</summary>
        public static bool Descent;
        /// <summary>Set with Descent by the machine's panel: the trip is to the machine room, not the bottom.</summary>
        public static bool ToMachine;
        /// <summary>True for a run that is only the fight with the machine.</summary>
        public bool IsMachine { get; private set; }
        /// <summary>The machine has just been beaten: the lab has a card to show and the radio a word to say.</summary>
        public static bool CycleCard, CycleRadio;

        public string labScene = "Lab";
        [Tooltip("Items handed over at the top of the way down, so the last fight is not fought bare.")]
        public int descentItems = 3;

        /// <summary>True on the trip to the bottom. Nothing about it is saved as a run, and a saved run is left alone.</summary>
        public bool IsDescent { get; private set; }
        public float deathDelay = 1.6f;

        public bool Ended { get; private set; }

        FloorManager _floors;
        PlayerController _player;

        /// <summary>Each extra biome raises what your samples are worth when you extract: ×1, ×1.5, ×2...</summary>
        public static float RewardMultiplier(int depth) => 1f + 0.5f * (Mathf.Max(1, depth) - 1);

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            FloorManager.FloorBuilt += OnFloorBuilt;
            FloorManager.ExtractChosen += Extract;
            Enemy.Killed += OnKilled;
            GameSettings.Changed += ApplyDifficulty;
        }

        void OnDisable()
        {
            FloorManager.FloorBuilt -= OnFloorBuilt;
            FloorManager.ExtractChosen -= Extract;
            Enemy.Killed -= OnKilled;
            GameSettings.Changed -= ApplyDifficulty;
        }

        bool _relaxed;

        // Relaxed mode is two more points of health. It can be switched on or off in the middle of a run.
        void ApplyDifficulty()
        {
            if (_player == null || GameSettings.Relaxed == _relaxed) return;
            _relaxed = GameSettings.Relaxed;
            Health health = _player.Health;
            if (_relaxed)
            {
                health.SetMaxHealth(health.maxHealth + GameSettings.RelaxedHealth, false);
                health.Heal(GameSettings.RelaxedHealth);
            }
            else
            {
                health.SetMaxHealth(Mathf.Max(1, health.maxHealth - GameSettings.RelaxedHealth), false);
            }
        }

        void OnKilled(Enemy enemy)
        {
            if (enemy is MachineBoss)
            {
                // Beaten, it restarts: the next cycle, with its parts out in the worlds again.
                MachineParts.Restart();
                CycleCard = CycleRadio = true;
                return;
            }
            if (!(enemy is CatBoss)) return;
            SaveSystem.Data.meta.finchBeaten++;
            SaveSystem.Save();
        }

        void Start()
        {
            _floors = FloorManager.Instance;
            _player = PlayerController.Instance;
            _player.Health.Died += OnPlayerDied;

            ApplyUpgrades();

            IsDescent = Descent && _floors.bottomBiome != null;
            IsMachine = IsDescent && ToMachine && _floors.machineBiome != null;
            Descent = false;
            ToMachine = false;

            RunSave run = SaveSystem.Data.run;
            if (IsDescent) BeginDescent();
            else if (run.active) Resume(run);
            else Begin();
        }

        // Straight to the bottom, with a few things from the lab's shelves.
        void BeginDescent()
        {
            var assets = GameAssets.Instance;
            if (descentItems > 0 && assets != null && assets.items != null && _player.Items != null)
            {
                var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), descentItems);
                foreach (var pick in picks) _player.Items.Add(pick);
            }
            _floors.BuildBottom(IsMachine);
        }

        void ApplyUpgrades()
        {
            _player.Health.SetMaxHealth(_player.Health.maxHealth + Upgrades.BonusHealth, true);
            _player.dodgeRechargeMultiplier = Upgrades.DodgeRechargeMultiplier;
            if (_player.Power != null) _player.Power.fillMultiplier = Upgrades.PowerFillMultiplier;
            _floors.treasureChoices = Upgrades.TreasureChoices;
            ApplyDifficulty();
        }

        void Begin()
        {
            SaveSystem.Data.meta.runsStarted++;
            Progress.StartRun();
            if (Upgrades.StartCoins > 0) RunState.AddCoins(Upgrades.StartCoins);
            if (Progress.Has(Perk.LooseChange)) RunState.AddCoins(Progress.LooseChangeCoins);
            // A lucky fortune, read in the lab before setting out.
            if (SaveSystem.Data.meta.luckyCoins > 0)
            {
                RunState.AddCoins(SaveSystem.Data.meta.luckyCoins);
                SaveSystem.Data.meta.luckyCoins = 0;
            }

            var assets = GameAssets.Instance;
            if (Upgrades.StartsWithItem && assets != null && assets.items != null && _player.Items != null)
            {
                var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
                if (picks.Count > 0) _player.Items.Add(picks[0]);
            }

            _floors.depth = TimeClock.StartDepth;
            _floors.BuildFloor(1);
        }

        // Puts the player back at the start of the floor they were on.
        void Resume(RunSave run)
        {
            var assets = GameAssets.Instance;
            if (assets != null && assets.items != null && _player.Items != null)
            {
                foreach (int type in run.items)
                {
                    var item = assets.items.Get((ItemType)type);
                    if (item != null) _player.Items.Add(item);
                }
            }

            // Items may have raised max health (Sugar Cube); health is set after they are back.
            _player.Health.Restore(run.health);
            if (_player.Power != null) _player.Power.SetMeter(run.powerMeter);
            RunState.Restore(run.kills, run.coins, run.samples);

            _floors.depth = Mathf.Max(1, run.depth);
            if (!string.IsNullOrEmpty(run.biome)) _floors.SetBiome(run.biome);
            _floors.BuildFloor(Mathf.Max(1, run.floorNumber), run.seed);
        }

        // "The run saves at the start of every floor."
        void OnFloorBuilt(FloorManager floors)
        {
            if (Ended || _player == null || IsDescent) return;

            RunSave run = SaveSystem.Data.run;
            // A floor further down than the run has been: that is progress. (A resumed floor is not.)
            if (run.active && floors.FloorNumber > run.floorNumber) Progress.Add(Progress.FloorXp);
            run.active = true;
            run.floorNumber = floors.FloorNumber;
            run.depth = floors.depth;
            run.biome = floors.biome != null ? floors.biome.id : "";
            run.seed = floors.Layout.seed;
            run.health = _player.Health.Current;
            run.coins = RunState.Coins;
            run.samples = RunState.Samples;
            run.kills = RunState.Kills;
            run.powerMeter = _player.Power != null ? _player.Power.Meter : 0f;
            run.items.Clear();
            if (_player.Items != null)
            {
                foreach (var item in _player.Items.Collected) run.items.Add((int)item.type);
            }

            var meta = SaveSystem.Data.meta;
            meta.deepestFloor = Mathf.Max(meta.deepestFloor, floors.FloorNumber);
            SaveSystem.Save();
        }

        /// <summary>Cash out: every sample, times the multiplier for how deep the run went.</summary>
        public void Extract()
        {
            if (Ended) return;
            Ended = true;

            if (IsDescent)
            {
                // Back up from the bottom: what was picked up comes home, and any saved run is where it was left.
                SaveSystem.Data.meta.samples += RunState.Samples;
                SaveSystem.Save();
                LastRunMessage = IsMachine
                    ? $"The machine has restarted. Cycle {MachineParts.Cycle + 1}: every run starts a depth harder, and the four parts are out there again."
                    : $"You came back up from the bottom with {RunState.Samples} samples. Dr. Finch is, on paper, still employed.";
                Fade.LoadScene(labScene);
                return;
            }

            int depth = _floors != null ? _floors.depth : 1;
            float multiplier = RewardMultiplier(depth);
            int banked = Mathf.RoundToInt(RunState.Samples * multiplier);

            Extracted?.Invoke(RunState.Samples);
            Progress.Add(Progress.ExtractXp);
            var meta = SaveSystem.Data.meta;
            meta.samples += banked;
            meta.rareMaterials += depth;
            meta.extractions++;
            SaveSystem.Data.run = new RunSave();
            SaveSystem.Save();

            LastRunMessage = (multiplier > 1f
                ? $"Extracted! {RunState.Samples} samples ×{multiplier:0.#} = {banked} brought home."
                : $"Extracted! {banked} samples brought home.") + Tally();
            Fade.LoadScene(labScene);
        }

        // A bad run still counts: a share of the samples makes it home.
        void OnPlayerDied()
        {
            if (Ended) return;
            Ended = true;

            float share = Upgrades.SamplesKeptOnDeath;
            int kept = Mathf.FloorToInt(RunState.Samples * share);
            int floor = _floors != null ? _floors.FloorNumber : 1;

            var meta = SaveSystem.Data.meta;
            meta.samples += kept;
            meta.deaths++;
            if (!IsDescent) SaveSystem.Data.run = new RunSave();
            SaveSystem.Save();

            LastRunMessage = IsDescent
                ? (IsMachine ? "The machine won that one. Its parts are still in. Step on the mat to go again." : "You fell at the bottom. The door is still there, whatever the radio says.")
                : $"You fell on floor {floor}. {Mathf.RoundToInt(share * 100f)}% of your samples made it home: {kept}." + Tally();
            StartCoroutine(GoHome());
        }

        // The rest of how it went, for the line the lab greets the player with.
        string Tally()
        {
            int items = _player != null && _player.Items != null ? _player.Items.Collected.Count : 0;
            string where = _floors != null && _floors.biome != null ? _floors.biome.displayName : "";
            string text = $"  {RunState.Kills} kills, {items} {(items == 1 ? "item" : "items")}";
            if (_floors != null && _floors.depth > 1) text += $", depth {_floors.depth}";
            if (Progress.RunXp > 0) text += $", {Progress.RunXp} XP";
            return where.Length > 0 ? text + $", last seen in {where}." : text + ".";
        }

        IEnumerator GoHome()
        {
            if (_player != null && _player.visual != null) _player.visual.gameObject.SetActive(false);
            yield return new WaitForSeconds(deathDelay);
            Fade.LoadScene(labScene);
        }

        // "...and whenever the app goes to the background."
        void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save();
        }

        void OnApplicationQuit()
        {
            SaveSystem.Save();
        }
    }
}
