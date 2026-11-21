using System.Collections.Generic;

namespace Shrunk
{
    public enum JobKind { Kills, Elites, Mimics, Bosses, Dares, Extractions, Floors, Items, Streaks, GoldBugs, ClawWins, RangeRounds, Fortunes, Secrets, Dives, Dailies }

    /// <summary>One job off the lab's noticeboard: something to do a number of times, and what the lab pays for it.</summary>
    public class Job
    {
        public int id;
        public JobKind kind;
        public int needed;
        public int pay;
        public string what;
    }

    /// <summary>
    /// The noticeboard's jobs. Three are pinned up at a time; they are worked on across runs, pay samples straight
    /// into the lab's stock the moment they are done, and each finished one is replaced by the next off the pile.
    /// The pile never runs out: it goes round.
    /// </summary>
    public static class Jobs
    {
        public const int Pinned = 3;

        public static readonly Job[] All =
        {
            new Job { id = 0, kind = JobKind.Kills, needed = 40, pay = 25, what = "Put down 40 of anything." },
            new Job { id = 1, kind = JobKind.Elites, needed = 4, pay = 35, what = "Put down 4 crowned ones." },
            new Job { id = 2, kind = JobKind.Floors, needed = 5, pay = 30, what = "Go down 5 floors." },
            new Job { id = 3, kind = JobKind.Items, needed = 6, pay = 30, what = "Pick up 6 items." },
            new Job { id = 4, kind = JobKind.Bosses, needed = 2, pay = 50, what = "Beat 2 bosses." },
            new Job { id = 5, kind = JobKind.Dares, needed = 2, pay = 45, what = "Ring 2 dare bells and live." },
            new Job { id = 6, kind = JobKind.Extractions, needed = 1, pay = 40, what = "Come home through a portal, on purpose." },
            new Job { id = 7, kind = JobKind.Mimics, needed = 3, pay = 35, what = "Put down 3 mimics." },
            new Job { id = 8, kind = JobKind.Kills, needed = 100, pay = 60, what = "Put down 100 of anything." },
            new Job { id = 9, kind = JobKind.Floors, needed = 12, pay = 70, what = "Go down 12 floors." },
            new Job { id = 10, kind = JobKind.Streaks, needed = 2, pay = 40, what = "Put down 10 in a row without a pause, twice." },
            new Job { id = 11, kind = JobKind.GoldBugs, needed = 2, pay = 45, what = "Catch 2 gold bugs before they leave." },
            new Job { id = 13, kind = JobKind.RangeRounds, needed = 1, pay = 20, what = "Knock down 8 or more in one round on the range." },
            new Job { id = 14, kind = JobKind.Fortunes, needed = 3, pay = 20, what = "Read 3 fortunes. One a visit." },
            new Job { id = 15, kind = JobKind.Secrets, needed = 1, pay = 35, what = "Break into a secret room." },
            new Job { id = 16, kind = JobKind.Dives, needed = 1, pay = 30, what = "Dive to the bottom of a pond in the forest." },
            new Job { id = 17, kind = JobKind.Dailies, needed = 2, pay = 40, what = "Play the Daily Experiment on 2 days." },
            new Job { id = 12, kind = JobKind.ClawWins, needed = 3, pay = 35, what = "Get anything at all out of a claw machine, 3 times." },
        };

        /// <summary>A job has just been finished and paid.</summary>
        public static event System.Action<Job> Done;

        static MetaSave Meta => SaveSystem.Data.meta;

        public static Job Get(int id)
        {
            foreach (var job in All) if (job.id == id) return job;
            return All[0];
        }

        /// <summary>The jobs pinned up now. The first look at the board pins the first three.</summary>
        public static List<int> Current
        {
            get
            {
                MetaSave meta = Meta;
                while (meta.jobs.Count < Pinned && meta.jobs.Count < All.Length)
                {
                    meta.jobs.Add(NextId());
                    meta.jobProgress.Add(0);
                }
                while (meta.jobProgress.Count < meta.jobs.Count) meta.jobProgress.Add(0);
                return meta.jobs;
            }
        }

        public static int Progress(int slot) => slot < Meta.jobProgress.Count ? Meta.jobProgress[slot] : 0;

        // The next job off the pile that is not already pinned up.
        static int NextId()
        {
            MetaSave meta = Meta;
            for (int i = 0; i < All.Length; i++)
            {
                int id = All[(meta.jobsDealt + i) % All.Length].id;
                if (meta.jobs.Contains(id)) continue;
                meta.jobsDealt = (meta.jobsDealt + i + 1) % All.Length;
                return id;
            }
            return All[0].id;
        }

        /// <summary>Something countable has happened. Every pinned job of that kind moves on; finished ones pay and are replaced.</summary>
        public static void Count(JobKind kind, int amount = 1)
        {
            if (amount <= 0) return;
            List<int> current = Current;
            MetaSave meta = Meta;
            bool changed = false;
            for (int slot = 0; slot < current.Count; slot++)
            {
                Job job = Get(current[slot]);
                if (job.kind != kind) continue;
                meta.jobProgress[slot] += amount;
                changed = true;
                if (meta.jobProgress[slot] < job.needed) continue;

                meta.samples += job.pay;
                meta.jobsDone++;
                Shrunk.Progress.Add(Shrunk.Progress.JobXp);
                // Off the board first, so the pile cannot hand the same one straight back.
                current[slot] = -1;
                current[slot] = NextId();
                meta.jobProgress[slot] = 0;
                Done?.Invoke(job);
            }
            if (changed && kind != JobKind.Kills) SaveSystem.Save();
        }

        /// <summary>The board, as the player reads it.</summary>
        public static string Describe()
        {
            List<int> current = Current;
            // Three lines, one a job: the prompt has room for no more.
            var text = new System.Text.StringBuilder();
            for (int slot = 0; slot < current.Count; slot++)
            {
                Job job = Get(current[slot]);
                text.Append(slot == 0 ? "The noticeboard. " : "\n");
                text.Append($"{job.what} {Progress(slot)} of {job.needed}. Pays {job.pay} samples.");
            }
            return text.ToString();
        }
    }
}
