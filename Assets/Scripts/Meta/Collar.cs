using System.Collections.Generic;

namespace Shrunk
{
    /// <summary>One piece of the cat's collar, and the boss that has been carrying it about.</summary>
    public class CollarPiece
    {
        /// <summary>The boss's specimen key: beating that boss is what gives the piece.</summary>
        public string boss;
        public string name;
    }

    /// <summary>
    /// The cat's collar, in four pieces, one with the last boss of each biome. The door in the lab is a cat flap that
    /// only opens for the whole collar, so the way to the cat is through all four. Nothing new is saved: a piece is
    /// held once its boss is in a jar on the shelf.
    /// </summary>
    public static class Collar
    {
        public static readonly CollarPiece[] Pieces =
        {
            new CollarPiece { boss = "AntQueen", name = "the bell" },
            new CollarPiece { boss = "TrickCandle", name = "the buckle" },
            new CollarPiece { boss = "RobotVacuum", name = "the name tag" },
            new CollarPiece { boss = "IceMaker", name = "the strap" },
        };

        static List<string> Jars => SaveSystem.Data.meta.specimens;

        public static bool Has(CollarPiece piece) => Jars.Contains(piece.boss);

        public static int Count
        {
            get
            {
                int count = 0;
                foreach (var piece in Pieces) if (Has(piece)) count++;
                return count;
            }
        }

        public static bool Complete => Count >= Pieces.Length;

        /// <summary>The piece this boss carries, or null. Takes the boss's name as its prefab has it.</summary>
        public static CollarPiece From(Enemy boss)
        {
            if (boss == null) return null;
            string key = boss.name.Replace("(Clone)", "").Trim();
            foreach (var piece in Pieces) if (piece.boss == key) return piece;
            return null;
        }

        /// <summary>The names of the pieces held, or of the ones still out there, for the door to read out.</summary>
        public static string Names(bool held)
        {
            var names = new List<string>();
            foreach (var piece in Pieces) if (Has(piece) == held) names.Add(piece.name);
            return string.Join(", ", names);
        }
    }
}
