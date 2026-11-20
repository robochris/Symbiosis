using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Shrunk
{
    /// <summary>What is kept between runs: samples in the bank and lab upgrades.</summary>
    [Serializable]
    public class MetaSave
    {
        public int samples;
        public int rareMaterials;
        /// <summary>Level of each upgrade, indexed by UpgradeType.</summary>
        public int[] upgradeLevels = new int[0];
        public int runsStarted;
        public int extractions;
        public int deaths;
        public int deepestFloor;
        public int bossesBeaten;
        /// <summary>Times Dr. Finch has been beaten at the bottom.</summary>
        public int finchBeaten;
        /// <summary>The ending card has been shown: it is only shown the once.</summary>
        public bool endingSeen;
        /// <summary>Clocked in at the lab's time clock: new runs start a depth further down.</summary>
        public bool overtime;
        /// <summary>Achievements earned, as AchievementId numbers.</summary>
        public List<int> achievements = new List<int>();
        public int shellsShattered;
        public int podsPopped;
        public int mimicsKilled;
        public int daresWon;
        /// <summary>The weapon chosen at the equipment cupboard.</summary>
        public int weapon;
        /// <summary>Parts of the machine brought home this cycle, by the name of the boss each came from.</summary>
        public List<string> parts = new List<string>();
        /// <summary>Times the machine has been beaten and has restarted. Every run starts this much deeper.</summary>
        public int cycle;
        /// <summary>The jobs pinned to the noticeboard, how far along each is, how many have been done, and where the pile is up to.</summary>
        public List<int> jobs = new List<int>();
        public List<int> jobProgress = new List<int>();
        public int jobsDone;
        public int jobsDealt;
        /// <summary>The best timed round on the lab's firing range, and whether the vent behind it has been emptied.</summary>
        public int rangeBest;
        public bool rangeSecret;
        /// <summary>Which lab stations the player has been told about, one bit each.</summary>
        public int labSeen;
        /// <summary>Pages of Dr. Finch's notebook found, by id.</summary>
        public List<int> notes = new List<int>();
        /// <summary>Bosses beaten at least once, by prefab name: the jars on the lab's specimen shelf.</summary>
        public List<string> specimens = new List<string>();
        /// <summary>Every kind of item ever picked up, as ItemType numbers, in the order first found: the lab's filing cabinet.</summary>
        public List<int> itemsFound = new List<int>();
        /// <summary>Radio lines already heard, so none repeats until the rest of its pool has been said.</summary>
        public List<int> radioHeard = new List<int>();
        /// <summary>Which character style the Void is wearing.</summary>
        public int style;
        /// <summary>Which hero power the Void carries, as a PowerKind number.</summary>
        public int power;
    }

    /// <summary>A run in progress, as it stood at the start of its current floor.</summary>
    [Serializable]
    public class RunSave
    {
        public bool active;
        public int floorNumber;
        public int depth;
        /// <summary>Which biome the run is in. Empty in saves from before there was more than one.</summary>
        public string biome = "";
        /// <summary>The floor's seed, so the same floor is rebuilt.</summary>
        public int seed;
        public int health;
        public int coins;
        public int samples;
        public int kills;
        public float powerMeter;
        public List<int> items = new List<int>();
    }

    [Serializable]
    public class SaveFile
    {
        public MetaSave meta = new MetaSave();
        public RunSave run = new RunSave();
    }

    /// <summary>Loads and saves progress as one small file on the device.</summary>
    public static class SaveSystem
    {
        static string _profile = StartingProfile();
        static SaveFile _data;

        // "Shrunk.exe -saveProfile name" plays on a save file of its own, leaving the usual one alone.
        static string StartingProfile()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-saveProfile" && !string.IsNullOrWhiteSpace(args[i + 1])) return args[i + 1];
                }
            }
            catch (Exception)
            {
                // No command line on this platform: the usual save.
            }
            return "player";
        }

        /// <summary>Which save file is in use. Tests use their own so they never touch real progress.</summary>
        public static string Profile
        {
            get => _profile;
            set { _profile = value; _data = null; }
        }

        public static string FilePath => Path.Combine(Application.persistentDataPath, "shrunk-" + _profile + ".json");

        public static SaveFile Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static void Load()
        {
            _data = null;
            try
            {
                if (File.Exists(FilePath)) _data = JsonUtility.FromJson<SaveFile>(File.ReadAllText(FilePath));
            }
            catch (Exception problem)
            {
                Debug.LogWarning("[Shrunk] Could not read the save file, starting fresh: " + problem.Message);
            }

            if (_data == null) _data = new SaveFile();
            if (_data.meta == null) _data.meta = new MetaSave();
            if (_data.run == null) _data.run = new RunSave();
            if (_data.run.items == null) _data.run.items = new List<int>();
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception problem)
            {
                Debug.LogWarning("[Shrunk] Could not write the save file: " + problem.Message);
            }
        }

        /// <summary>Deletes all progress in the current profile.</summary>
        public static void Wipe()
        {
            _data = new SaveFile();
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception problem)
            {
                Debug.LogWarning("[Shrunk] Could not delete the save file: " + problem.Message);
            }
        }
    }
}
