using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// What opens up as the game is played, so a new player is not met with everything at once. Each feature has a
    /// moment it arrives (a first run, a first boss, a first world beaten...), and arriving it gets a card of its own
    /// from Finch in the lab, one a visit. Read in order, the cards are the story of the lab filling up.
    /// </summary>
    public static class Unlocks
    {
        public enum Feature
        {
            HatStand, Mailbox, DailyParcel, ItemWall,
            PetJar, PressureGauge, Shrines, RuleOfTheDay, Sleepers,
            QuickRun, Seasons, Jukebox, RescueCages, Challenges,
            Vaults, WorldSecrets, Ducks, FishTank,
            Rival
        }

        class Gate
        {
            public Feature feature;
            public Func<MetaSave, bool> open;
            public string heading, line;
        }

        /// <summary>Tests open everything, so each can try what it is about.</summary>
        public static bool AllOpen;

        static int WorldBosses(MetaSave meta)
        {
            int count = 0;
            foreach (var key in meta.specimens) if (BossEnemy.Trophies.ContainsKey(key)) count++;
            return count;
        }

        static int Runs(MetaSave meta) => meta.deaths + meta.extractions;

        static readonly Gate[] Gates =
        {
            new Gate { feature = Feature.HatStand, open = m => Runs(m) >= 1, heading = "SOMETHING TO WEAR", line = "I found a hat stand in a cupboard. Hats turn up as you do things. Try the beanie: it is on the house." },
            new Gate { feature = Feature.Mailbox, open = m => Runs(m) >= 1, heading = "YOU HAVE MAIL", line = "There is a mailbox by the door now. Things you meet out there write to you. I don't know how either." },
            new Gate { feature = Feature.DailyParcel, open = m => Runs(m) >= 2, heading = "DELIVERIES", line = "A parcel will be left by the door once a day. Come back tomorrow and it will be bigger." },
            new Gate { feature = Feature.ItemWall, open = m => m.itemsFound.Count >= 3, heading = "THE ITEM WALL", line = "I put up a wall by the filing cabinet: a plaque for every item there is. The question marks are the ones still out there." },
            new Gate { feature = Feature.PetJar, open = m => m.bossesBeaten >= 1, heading = "COMPANY", line = "Something small has been following you home. There is a jar for pets by the door now. Earn their trust and they will come along." },
            new Gate { feature = Feature.RuleOfTheDay, open = m => m.bossesBeaten >= 1, heading = "THE RULE OF THE DAY", line = "From now on every day has a small rule of its own. Bouncy shots, gold rushes. A new one tomorrow." },
            new Gate { feature = Feature.Shrines, open = m => m.bossesBeaten >= 1, heading = "SHRINES", line = "You may see little shrines out there now. They offer a trade. Read it before you take it." },
            new Gate { feature = Feature.Sleepers, open = m => Runs(m) >= 3, heading = "QUIET, PLEASE", line = "Some of them sleep. Sneak up, and catch them napping: they drop a little more." },
            new Gate { feature = Feature.PressureGauge, open = m => m.bossesBeaten >= 2, heading = "PRESSURE", line = "The pressure gauge is connected. Turn it up and everything is tougher, and pays more. Only if you want to." },
            new Gate { feature = Feature.QuickRun, open = m => Runs(m) >= 4, heading = "QUICK RUNS", line = "For a short sitting: the sign by the portal. Two floors a world, then its boss." },
            new Gate { feature = Feature.Seasons, open = m => Runs(m) >= 5, heading = "SEASONS", line = "The board by the portal counts seasons now. Everything you do fills its track, and there is a star at the end." },
            new Gate { feature = Feature.Jukebox, open = m => Runs(m) >= 6, heading = "MUSIC", line = "I mended the jukebox. It plays any world's tune you have been to." },
            new Gate { feature = Feature.Challenges, open = m => Runs(m) >= 6, heading = "AGAINST THE CLOCK", line = "Sometimes a fight now comes with a clock. Beat it and something is left for you." },
            new Gate { feature = Feature.RescueCages, open = m => Runs(m) >= 8, heading = "SOMEBODY CAGED THEM", line = "There are larvae in cages out there. Clear the room and let one out: it will fight with you for a floor." },
            new Gate { feature = Feature.Vaults, open = m => WorldBosses(m) >= 1, heading = "THE VAULTS", line = "Beyond the first world there are vaults: coded doors, with the code chalked somewhere on the same floor. Inside is a trial, and something worth it." },
            new Gate { feature = Feature.WorldSecrets, open = m => WorldBosses(m) >= 1, heading = "EVERY WORLD HAS A SECRET", line = "Every world hides something of its own. A candle nobody lit. A remote under the sofa. They are rare. Keep your eyes open." },
            new Gate { feature = Feature.Ducks, open = m => WorldBosses(m) >= 2, heading = "DUCKS", line = "Somebody has hidden a rubber duck in every world. I cannot imagine who. Bring me ten." },
            new Gate { feature = Feature.FishTank, open = m => m.achievements.Count >= 5, heading = "FISH", line = "I got a fish tank. A fish for every five things you have done that I am proud of. Do not tell them that." },
            new Gate { feature = Feature.Rival, open = m => WorldBosses(m) >= 2, heading = "THE OTHER ONE", line = "Something else came through from another universe when you did. Another you. It is not friendly. You will meet it." },
        };

        public static bool Open(Feature feature)
        {
            if (AllOpen || Achievements.EverythingUnlocked) return true;
            var meta = SaveSystem.Data.meta;
            foreach (var gate in Gates) if (gate.feature == feature) return gate.open(meta);
            return true;
        }

        /// <summary>The first feature that has opened but not yet been announced; marks it announced and shows its card.</summary>
        public static bool AnnounceNext()
        {
            if (AllOpen || ScreenCards.Instance == null || ScreenCards.Instance.CardShowing) return false;
            var meta = SaveSystem.Data.meta;
            foreach (var gate in Gates)
            {
                string key = gate.feature.ToString();
                if (meta.featuresAnnounced.Contains(key) || !gate.open(meta)) continue;
                meta.featuresAnnounced.Add(key);
                SaveSystem.Save();
                ScreenCards.Instance.ShowCard(gate.heading, gate.line, 5.5f);
                return true;
            }
            return false;
        }
    }
}
