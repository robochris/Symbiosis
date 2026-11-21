using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Dr. Finch's field guide: every kind of creature, what to do about it, and a line of his on it. The first time
    /// one is met in a run it is named on screen with the tip; the book in the office keeps all the ones met so far.
    /// </summary>
    public static class FieldGuide
    {
        /// <summary>His note on each, by the creature's name.</summary>
        public static readonly Dictionary<string, string> Notes = new Dictionary<string, string>
        {
            // The ants
            ["WorkerAnt"] = "Works hard. Mostly at you.",
            ["SoldierAnt"] = "All jaw. It warns you before it comes, which is more than I did.",
            ["SpitterAnt"] = "Acid, spat. Do not stand in what it leaves.",
            ["WingedAnt"] = "It was a queen once, or meant to be. It took it badly.",
            ["BombardierAnt"] = "Throws over things. Has no sense of personal space.",
            ["HeraldAnt"] = "Announces you. Then everyone else is in a hurry.",
            ["MarksmanAnt"] = "It lines you up. Respect the line.",
            ["Mimic"] = "A coin with teeth. I have stopped picking up coins.",
            ["GoldBug"] = "Pure profit, with legs. Catch it before it goes.",
            // The cake
            ["GummyBear"] = "Cheerful. Determined. Mostly sugar.",
            ["Jawbreaker"] = "Lives up to the name.",
            ["Cupcake"] = "Shoots sprinkles. I did not know they could.",
            ["SugarFly"] = "Attracted to the cake, and then to you.",
            ["PipingBag"] = "Ices a line to you. Then fills it in.",
            ["SodaBottle"] = "Shaken. Very shaken.",
            ["PartyHorn"] = "Gets everyone going. Silence it.",
            ["Gumdrop"] = "Small, soft, and about to be a gummy bear.",
            ["SparklerBox"] = "Do not let them all go off at once.",
            // Under the sofa
            ["DustBunny"] = "Breeds, as the name suggests.",
            ["Marble"] = "Lost in 1994. Still rolling.",
            ["ToySoldier"] = "Still following orders. Somebody's.",
            ["Moth"] = "Came for the light. Stayed for you.",
            ["LaserPointer"] = "The red dot. You are the cat now.",
            ["Popcorn"] = "Popped once. Throws the rest.",
            ["RemoteControl"] = "Fast forward, for everybody but you.",
            ["Fluff"] = "Will be a dust bunny. Give it time. Do not give it time.",
            ["LintPile"] = "Things hatch from lint. I wish they did not.",
            // The fridge
            ["MouldyBerry"] = "Past its best. Still keen.",
            ["FrozenPea"] = "Only goes straight. So should you, sideways.",
            ["KetchupPacket"] = "Squirts. Stains.",
            ["FruitFly"] = "There is always one. There are never just one.",
            ["CocktailStick"] = "Points first, stabs second.",
            ["YogurtPot"] = "Lobs where you will be. Be somewhere else.",
            ["BakingSoda"] = "Keeps the others fresh. Open it first.",
            ["Spore"] = "Mould with ambitions.",
            // The library
            ["Silverfish"] = "Has read everything in here, by eating it.",
            ["Encyclopedia"] = "Volume nine. It means to be consulted.",
            ["InkPot"] = "Blue-black. Does not wash out.",
            ["PaperPlane"] = "Somebody's homework, folded in anger.",
            ["Stapler"] = "Throws staples over the shelves.",
            ["Bookmark"] = "Keeps everybody's place, and hurries them from it.",
            ["FountainPen"] = "Rules a line to you, then signs it.",
            ["Bookworm"] = "Grows into a silverfish if left.",
            // The forest
            ["Woodlouse"] = "Fourteen legs, every one in a hurry.",
            ["StagBeetle"] = "Has antlers, and has had them out before.",
            ["Toadstool"] = "Puffs. You would rather it did not.",
            ["Mosquito"] = "Only small to everybody else.",
            ["AcornWeevil"] = "A nose for acorns, and an arm for them.",
            ["Firefly"] = "Lights the way for the others. The way is toward you.",
            ["Hornet"] = "Lines you up from across the clearing.",
            ["Caterpillar"] = "Grows into a woodlouse. Biology is not this forest's strength.",
            // The clouds
            ["RaindropEnemy"] = "Going down, and it would like company.",
            ["Hailstone"] = "Hard, cold, in a hurry to land.",
            ["Raincloud"] = "A small grey one with a grudge.",
            ["Swallow"] = "Was here first. Very fast about it.",
            ["Snowflake"] = "Every one different. Every one thrown at you.",
            ["Rainbow"] = "Cheerful, and it makes everything round it worse.",
            ["LightningRod"] = "Draws the line, then the lightning down it.",
            ["Droplet"] = "Grows into a raindrop if left.",
            // The console
            ["Electron"] = "Negatively charged, and it takes it out on you.",
            ["Capacitor"] = "Stores it all up, then lets go at once.",
            ["Sprite"] = "Eight pixels by eight, waiting since 1985.",
            ["Bug"] = "Somebody's mistake, with wings.",
            ["SolderBlob"] = "Hot, silver, sticky about where it lands.",
            ["CoolingFan"] = "Keeps tempers down and speeds up.",
            ["LaserDiode"] = "A thin line first, then the rest of the light.",
            ["Bit"] = "A one, or possibly a zero. Grows into an electron.",
        };

        static MetaSave Meta => SaveSystem.Data.meta;

        /// <summary>The name a creature is filed under: its prefab's name.</summary>
        public static string Key(Enemy enemy) => enemy == null ? "" : enemy.name.Replace("(Clone)", "").Trim();

        public static bool Seen(string key) => Meta.creaturesSeen.Contains(key);
        public static int SeenCount => Meta.creaturesSeen.Count;
        /// <summary>Every kind there is to meet, bosses not counted.</summary>
        public static int Total => Notes.Count;

        /// <summary>"StagBeetle" → "Stag Beetle". Bosses go by their own names.</summary>
        public static string NameOf(string key, Enemy enemy = null)
        {
            if (enemy is BossEnemy boss) return boss.displayName;
            if (key == "RaindropEnemy") return "Raindrop";
            var name = new StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key[i]) && !char.IsUpper(key[i - 1])) name.Append(' ');
                name.Append(key[i]);
            }
            return name.ToString();
        }

        /// <summary>What to do about one, from how it behaves.</summary>
        public static string Tip(Enemy enemy)
        {
            switch (enemy)
            {
                case MimicEnemy _: return "looks like a coin. It is not.";
                case GoldBug _: return "runs off with money. Catch it first.";
                case ChargerEnemy _: return "winds up, then charges in a straight line. Step aside.";
                case ShooterEnemy _: return "keeps its distance and shoots.";
                case FlyerEnemy _: return "circles, then dives. Dodge as it comes in.";
                case LobberEnemy _: return "throws over cover at where you are going. Keep moving.";
                case SupportEnemy _: return "makes the others quicker. Put it down first.";
                case SniperEnemy _: return "draws a line first; the shot follows it. Step off the line.";
                case LarvaEnemy _: return "grows into something worse if left.";
                case EggEnemy _: return "hatches if it is not broken.";
                case ChaserEnemy _: return "runs straight at you.";
                default: return "best avoided.";
            }
        }

        /// <summary>Notes a creature as met. True if it is the first time.</summary>
        public static bool Record(Enemy enemy)
        {
            if (enemy == null || enemy is BossEnemy) return false;
            string key = Key(enemy);
            if (string.IsNullOrEmpty(key) || Seen(key)) return false;
            Meta.creaturesSeen.Add(key);
            return true;
        }

        /// <summary>One page of the book: the name, the tip, and his note.</summary>
        public static string Page(string key)
        {
            Notes.TryGetValue(key, out string note);
            return string.IsNullOrEmpty(note) ? NameOf(key) : $"{NameOf(key)}: \"{note}\"";
        }
    }

    /// <summary>
    /// In a run: the first time each kind of creature turns up, it is named on screen with what to do about it. A
    /// room full of new ones takes its turn, one line every few seconds.
    /// </summary>
    public class FieldGuideWatcher : MonoBehaviour
    {
        public float gap = 3.2f;
        /// <summary>Tests switch the announcements off, so they do not talk over what the tests are reading.</summary>
        public static bool Quiet;

        readonly Queue<string> _waiting = new Queue<string>();
        float _next;

        void OnEnable() => Enemy.Appeared += OnAppeared;
        void OnDisable() => Enemy.Appeared -= OnAppeared;

        void OnAppeared(Enemy enemy)
        {
            if (Quiet || FloorManager.Instance == null || !FieldGuide.Record(enemy)) return;
            _waiting.Enqueue($"New: {FieldGuide.NameOf(FieldGuide.Key(enemy), enemy)}. It {FieldGuide.Tip(enemy)}");
        }

        void Update()
        {
            if (_waiting.Count == 0 || Time.unscaledTime < _next) return;
            var hud = FindAnyObjectByType<Hud>();
            if (hud == null) return;
            hud.Toast(_waiting.Dequeue(), gap);
            _next = Time.unscaledTime + gap;
        }
    }

    /// <summary>The field guide in the office: how many have been met, and one page at a time to read.</summary>
    public class FieldGuideBook : MonoBehaviour
    {
        public StepPad pad = new StepPad { radius = 1f };
        int _page = -1;

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !pad.SteppedOn(transform.position, player.transform.position)) return;
            Turn();
        }

        /// <summary>The next page among those met. Returns what it says, or null if nothing has been met yet.</summary>
        public string Turn()
        {
            var seen = SaveSystem.Data.meta.creaturesSeen;
            if (seen.Count == 0) return null;
            _page = (_page + 1) % seen.Count;
            string page = FieldGuide.Page(seen[_page]);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"{_page + 1} of {seen.Count}. {page}", 5f);
            Sfx.Play(SfxId.Coin, 0.5f);
            return page;
        }

        string Prompt(Vector3 playerPosition)
        {
            if (pad.Distance(transform.position, playerPosition) > 2.2f) return null;
            int seen = FieldGuide.SeenCount;
            return seen == 0
                ? "Dr. Finch's field guide. Blank so far: meet something first."
                : $"Dr. Finch's field guide: {seen} of {FieldGuide.Total} creatures met. {Interact.Verb} to turn a page.";
        }
    }
}
