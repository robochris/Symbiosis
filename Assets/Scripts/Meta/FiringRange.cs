using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The firing range, in its own room off the lab. Shooting the targets is free and counts for nothing. Stepping
    /// on the mat at the firing line starts a timed round: as many as can be knocked down before the clock runs out,
    /// with the best round kept on the board.
    /// </summary>
    public class FiringRange : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1f };
        [Tooltip("Where the score is written up.")]
        public TextMesh board;
        public float roundSeconds = 20f;
        /// <summary>The round the noticeboard's job asks for.</summary>
        public const int JobScore = 8;
        [Tooltip("The player is in the range once they are south of this.")]
        public float roomStartsAtZ = -9f;

        /// <summary>Targets knocked down since the scene opened, round or no round.</summary>
        public int Hits { get; private set; }
        public int RoundHits { get; private set; }
        public bool Running { get; private set; }
        public static int Best => SaveSystem.Data.meta.rangeBest;

        float _ends;

        void OnEnable()
        {
            RangeTarget.Popped += OnPopped;
            WorldPrompt.Add(Prompt, transform);
            Write();
        }

        void OnDisable()
        {
            RangeTarget.Popped -= OnPopped;
            WorldPrompt.Remove(Prompt);
        }

        void Update()
        {
            var player = PlayerController.Instance;
            // The board's lettering shows through walls, so it is only up while somebody is in the room to read it.
            if (board != null && player != null)
            {
                bool inRoom = player.transform.position.z < roomStartsAtZ;
                if (board.gameObject.activeSelf != inRoom) board.gameObject.SetActive(inRoom);
            }
            if (player != null && !Running && pad.SteppedOn(transform.position, player.transform.position)) StartRound();
            if (Running && Time.time >= _ends) EndRound();
            if (Running) Write();
        }

        public void StartRound()
        {
            Running = true;
            RoundHits = 0;
            _ends = Time.time + roundSeconds;
            Sfx.Play(SfxId.PowerOn);
            Write();
        }

        void EndRound()
        {
            Running = false;
            MetaSave meta = SaveSystem.Data.meta;
            if (RoundHits >= JobScore) Jobs.Count(JobKind.RangeRounds);
            bool record = RoundHits > meta.rangeBest;
            if (record)
            {
                meta.rangeBest = RoundHits;
                SaveSystem.Save();
            }
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast(record ? $"Time. {RoundHits} down: a new best." : $"Time. {RoundHits} down. The best is {meta.rangeBest}.", 4f);
            Sfx.Play(SfxId.Item);
            Write();
        }

        RangeTarget[] _targets;

        void OnPopped(RangeTarget target)
        {
            Hits += target.worth;
            if (Running) RoundHits += target.worth;
            Write();

            // All of them down at the same moment: quick hands, and somebody noticed.
            if (_targets == null) _targets = FindObjectsByType<RangeTarget>(FindObjectsSortMode.None);
            int standing = 0, real = 0;
            foreach (var each in _targets)
            {
                // The gold one comes and goes: the sweep is of the ones that stand there.
                if (each == null || each.isRadio || each.appearsEvery > 0f) continue;
                real++;
                if (!each.IsDown) standing++;
            }
            if (real > 1 && standing == 0) Achievements.Unlock(AchievementId.CleanSweep);
        }

        void Write()
        {
            if (board == null) return;
            board.text = Running
                ? $"{RoundHits}\n{Mathf.CeilToInt(Mathf.Max(0f, _ends - Time.time))} s"
                : $"BEST {Best}\nmat starts the clock";
        }

        string Prompt(Vector3 playerPosition)
        {
            if (Running || pad.Distance(transform.position, playerPosition) > 2.2f) return null;
            return $"The range: {roundSeconds:0} seconds, as many targets as you can. Your best is {Best}.\nStep on the mat to start the clock. Nothing here aims for you.";
        }
    }
}
