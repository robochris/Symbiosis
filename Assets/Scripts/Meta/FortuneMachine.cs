using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The fortune machine in the range room. Step on its mat and it reads out a fortune: one a visit to the lab.
    /// About one in three is a lucky one, and the next run then starts with a few coins in hand. It is mostly there
    /// to be read.
    /// </summary>
    public class FortuneMachine : MonoBehaviour
    {
        public const int LuckyCoins = 6;

        public static readonly string[] Fortunes =
        {
            "You will meet something with too many legs. This is not a prediction. It is a schedule.",
            "A great journey begins with a single step into a hole in the floor.",
            "Someone is thinking of you. It is the cat.",
            "Your lucky number is the one you are about to be hit for.",
            "Today is a good day to stand somewhere else.",
            "An old friend will get in touch. He is in the wall.",
            "You will come into money. Then you will find a claw machine.",
            "The answer you seek is behind the crates.",
            "Avoid large purchases, red circles, and librarians.",
            "Fortune favours the small. Nothing else does.",
            "A stranger will shush you. Do as they say.",
            "Good things come to those who extract.",
        };

        public StepPad pad = new StepPad { radius = 1f };

        /// <summary>True once a fortune has been read this visit.</summary>
        public bool Used { get; private set; }
        public string Last { get; private set; }
        /// <summary>Tests set this to decide whether the next fortune is a lucky one.</summary>
        [System.NonSerialized] public bool? rigged;

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Read();
        }

        /// <summary>Reads a fortune out. Returns false if this visit's has been read already.</summary>
        public bool Read()
        {
            if (Used) return false;
            Used = true;
            MetaSave meta = SaveSystem.Data.meta;
            Last = Fortunes[Mathf.Abs(meta.fortunesRead) % Fortunes.Length];
            meta.fortunesRead++;
            bool lucky = rigged ?? Random.value < 0.34f;
            if (lucky) meta.luckyCoins = LuckyCoins;
            SaveSystem.Save();

            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(lucky ? $"{Last}\nA lucky one: your next run starts with {LuckyCoins} coins." : Last, 6f);
            Sfx.Play(lucky ? SfxId.Item : SfxId.Coin);
            return true;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.2f) return null;
            return Used
                ? "The fortune machine: one a visit. Come back after a run."
                : "The fortune machine: from Dr. Finch's desk calendar. Step on the mat for today's.";
        }
    }
}
