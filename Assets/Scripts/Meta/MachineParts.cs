using System.Collections.Generic;

namespace Shrunk
{
    /// <summary>One part of the machine, and the boss that has it.</summary>
    public class MachinePart
    {
        /// <summary>The boss's name as its prefab has it: beating that boss is what gives the part.</summary>
        public string boss;
        public string name;
    }

    /// <summary>
    /// The machine in the lab (the portal, and everything bolted to it) is missing four parts. Each has ended up
    /// with the last boss of one of the worlds. Bring all four back and the machine can be switched on and fought;
    /// beating it restarts it, which starts the next cycle: every run a depth harder, and the parts gone again.
    /// </summary>
    public static class MachineParts
    {
        public static readonly MachinePart[] All =
        {
            new MachinePart { boss = "AntQueen", name = "the fuse" },
            new MachinePart { boss = "TrickCandle", name = "the coil" },
            new MachinePart { boss = "RobotVacuum", name = "the dial" },
            new MachinePart { boss = "IceMaker", name = "the lens" },
        };

        static List<string> Held => SaveSystem.Data.meta.parts;

        /// <summary>How many times the machine has been restarted. Every run starts this much deeper.</summary>
        public static int Cycle => SaveSystem.Data.meta.cycle;

        public static bool Has(MachinePart part) => Held.Contains(part.boss);

        public static int Count
        {
            get
            {
                int count = 0;
                foreach (var part in All) if (Has(part)) count++;
                return count;
            }
        }

        public static bool Complete => Count >= All.Length;

        /// <summary>The part this boss has, or null.</summary>
        public static MachinePart From(Enemy boss)
        {
            if (boss == null) return null;
            string key = boss.name.Replace("(Clone)", "").Trim();
            foreach (var part in All) if (part.boss == key) return part;
            return null;
        }

        /// <summary>Takes a part home. Returns false if it was already held.</summary>
        public static bool Take(MachinePart part)
        {
            if (part == null || Has(part)) return false;
            Held.Add(part.boss);
            SaveSystem.Save();
            return true;
        }

        /// <summary>The machine has been beaten: it restarts, a cycle on, and its parts are out in the worlds again.</summary>
        public static void Restart()
        {
            SaveSystem.Data.meta.cycle++;
            Held.Clear();
            SaveSystem.Save();
        }

        /// <summary>The names of the parts held, or of the ones still out there, for the panel to read out.</summary>
        public static string Names(bool held)
        {
            var names = new List<string>();
            foreach (var part in All) if (Has(part) == held) names.Add(part.name);
            return names.Count > 0 ? string.Join(", ", names) : "none";
        }
    }
}
