using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The time clock in the lab, for after the ending. Clocking in puts the Void on overtime: every new run starts
    /// one depth further down, so enemies carry more mutations from the first floor, elites turn up sooner, and
    /// samples are worth half as much again. Step on the mat to clock in or out. It appears once Dr. Finch has been
    /// beaten.
    /// </summary>
    public class TimeClock : MonoBehaviour
    {
        /// <summary>The depth a run on overtime starts at.</summary>
        public const int OvertimeDepth = 2;

        public StepPad pad = new StepPad { radius = 1f };
        [Tooltip("Lit while clocked in.")]
        public GameObject lamp;

        /// <summary>True when new runs start on overtime: clocked in, and entitled to be.</summary>
        public static bool On => SaveSystem.Data.meta.overtime && SaveSystem.Data.meta.finchBeaten > 0;

        /// <summary>The depth a new run starts at.</summary>
        public static int StartDepth => 1 + MachineParts.Cycle + (On ? OvertimeDepth - 1 : 0);

        void OnEnable()
        {
            WorldPrompt.Add(Prompt, transform);
            Show();
        }

        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Toggle();
        }

        /// <summary>Clocks in, or out. Returns whether the Void is now on overtime.</summary>
        public bool Toggle()
        {
            MetaSave meta = SaveSystem.Data.meta;
            meta.overtime = !meta.overtime;
            SaveSystem.Save();
            Sfx.Play(SfxId.Item);
            Show();
            if (On && Radio.Instance != null) Radio.Instance.Say(RadioMoment.Overtime);
            return On;
        }

        void Show()
        {
            if (lamp != null) lamp.SetActive(On);
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.4f) return null;
            int depth = MachineParts.Cycle + OvertimeDepth;
            float worth = RunDirector.RewardMultiplier(depth);
            return On
                ? $"The time clock: you are on overtime. Runs start at depth {depth}: tougher from the first floor, samples worth ×{worth:0.#}. {Interact.Verb} to clock out."
                : $"The time clock: you are on ordinary hours. {Interact.Verb} to clock in for overtime: runs start at depth {depth}, samples worth ×{worth:0.#}.";
        }
    }
}
