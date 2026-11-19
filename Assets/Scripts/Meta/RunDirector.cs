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

        public string labScene = "Lab";
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
        }

        void OnDisable()
        {
            FloorManager.FloorBuilt -= OnFloorBuilt;
            FloorManager.ExtractChosen -= Extract;
        }

        void Start()
        {
            _floors = FloorManager.Instance;
            _player = PlayerController.Instance;
            _player.Health.Died += OnPlayerDied;

            ApplyUpgrades();

            RunSave run = SaveSystem.Data.run;
            if (run.active) Resume(run);
            else Begin();
        }

        void ApplyUpgrades()
        {
            _player.Health.SetMaxHealth(_player.Health.maxHealth + Upgrades.BonusHealth, true);
            _player.dodgeRechargeMultiplier = Upgrades.DodgeRechargeMultiplier;
            if (_player.Power != null) _player.Power.fillMultiplier = Upgrades.PowerFillMultiplier;
            _floors.treasureChoices = Upgrades.TreasureChoices;
        }

        void Begin()
        {
            SaveSystem.Data.meta.runsStarted++;

            var assets = GameAssets.Instance;
            if (Upgrades.StartsWithItem && assets != null && assets.items != null && _player.Items != null)
            {
                var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
                if (picks.Count > 0) _player.Items.Add(picks[0]);
            }

            _floors.depth = 1;
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
            if (Ended || _player == null) return;

            RunSave run = SaveSystem.Data.run;
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

            int depth = _floors != null ? _floors.depth : 1;
            float multiplier = RewardMultiplier(depth);
            int banked = Mathf.RoundToInt(RunState.Samples * multiplier);

            Extracted?.Invoke(RunState.Samples);
            var meta = SaveSystem.Data.meta;
            meta.samples += banked;
            meta.rareMaterials += depth;
            meta.extractions++;
            SaveSystem.Data.run = new RunSave();
            SaveSystem.Save();

            LastRunMessage = multiplier > 1f
                ? $"Extracted! {RunState.Samples} samples ×{multiplier:0.#} = {banked} brought home."
                : $"Extracted! {banked} samples brought home.";
            SceneManager.LoadScene(labScene);
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
            SaveSystem.Data.run = new RunSave();
            SaveSystem.Save();

            LastRunMessage = $"You fell on floor {floor}. {Mathf.RoundToInt(share * 100f)}% of your samples made it home: {kept}.";
            StartCoroutine(GoHome());
        }

        IEnumerator GoHome()
        {
            if (_player != null && _player.visual != null) _player.visual.gameObject.SetActive(false);
            yield return new WaitForSeconds(deathDelay);
            SceneManager.LoadScene(labScene);
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
