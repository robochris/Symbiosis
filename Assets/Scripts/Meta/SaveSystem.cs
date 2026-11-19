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
        static string _profile = "player";
        static SaveFile _data;

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
