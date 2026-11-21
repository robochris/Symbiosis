using System.Collections.Generic;

namespace Shrunk
{
    /// <summary>What the radio has something to say about.</summary>
    public enum RadioMoment
    {
        /// <summary>The very first time the lab is opened.</summary>
        Hello,
        /// <summary>Opening the game in the lab, any time after that.</summary>
        Lab,
        RunStart,
        /// <summary>Picking a saved run back up.</summary>
        Resume,
        /// <summary>Back in the lab after falling.</summary>
        Death,
        /// <summary>Back in the lab after extracting.</summary>
        Extract,
        Biome,
        Boss,
        BossDown,
        /// <summary>A page of the notebook has been picked up.</summary>
        Note,
        /// <summary>In the lab with the whole notebook found: the door has turned up.</summary>
        Door,
        /// <summary>Arriving at the bottom.</summary>
        Bottom,
        /// <summary>The fight with Finch starts.</summary>
        FinchFight,
        /// <summary>The cat sits down to wash mid-fight: the moment to hurt it.</summary>
        FinchDose,
        FinchDown,
        /// <summary>Back in the lab having beaten Finch.</summary>
        FinchHome,
        /// <summary>Opening the game in the lab any time after Finch has been beaten.</summary>
        After,
        /// <summary>The player has picked up what was left in Finch's flask.</summary>
        Dose,
        /// <summary>Clocking in for overtime at the lab's time clock.</summary>
        Overtime,
        /// <summary>The player has rung a dare bell.</summary>
        Dare,
        /// <summary>Arriving on a floor with a twist.</summary>
        Twist,
        /// <summary>A different weapon taken from the lab's cupboard.</summary>
        Weapon,
        /// <summary>Samples traded at the exchange.</summary>
        Exchange,
        /// <summary>Arriving in the machine room, with all four parts in.</summary>
        MachineRoom,
        /// <summary>Home after beating the machine: it has restarted, and a new cycle has begun.</summary>
        Cycle,
        /// <summary>The player has shot the radio set in the lab.</summary>
        ShotRadio,
        // From here down is chatter: said only now and then, and not at all on the Less setting.
        LowHealth,
        Item,
        Floor,
        /// <summary>A new floor at depth 2 or more. The jokes get thinner the further in you are.</summary>
        Deep,
        /// <summary>A gold bug has turned up in a fight.</summary>
        GoldBug,
        /// <summary>The claw machine has dropped it again.</summary>
        ClawLost
    }

    /// <summary>
    /// Everything Dr. Finch says. One voice throughout: calm, polite, your boss, already inside and treating all of
    /// this as an ordinary working day. Dry, never random. Lines are short because they are read, not heard, and a
    /// line is not said again until the rest of its pool has been.
    /// </summary>
    public static class RadioLines
    {
        public const string Speaker = "DR. FINCH";

        static readonly Dictionary<RadioMoment, string[]> Pools = new Dictionary<RadioMoment, string[]>
        {
            [RadioMoment.Hello] = new[]
            {
                "Ah, the new intern. I'm Dr. Finch. I'd show you round, but I went in some time ago. The portal is the glowing thing.",
            },
            [RadioMoment.Lab] = new[]
            {
                "Good. You're in. The portal's where I left it.",
                "The lab's yours while I'm away. Please don't rearrange it.",
                "Whenever you're ready. I'm not going anywhere. Well. I am, but slowly.",
                "There's a kettle somewhere. It's the size of a cathedral now, so, no tea.",
                "Good morning. Or evening. I've rather lost track, down here.",
                "You're early. Or late. Either way, the portal is open.",
            },
            [RadioMoment.RunStart] = new[]
            {
                "Good morning. Same as yesterday, please, but with fewer mistakes.",
                "You're on the clock. I checked, and you weren't before.",
                "The portal is perfectly safe. I've been through it once.",
                "Try to bring back samples. Bringing back yourself is a bonus.",
                "I've left the lights on down there. That was a joke. There are no lights.",
                "Off you go. I'd come and help, but I'm a great deal further in.",
                "Remember your training. If you recall any, do tell me what it was.",
                "Another expedition. I've marked it down as voluntary.",
                "Do be careful with the ants. I have an understanding with some of them.",
                "Today's goal is the same as always: further than last time.",
                "I had a look at your contract. It doesn't mention any of this, which works in my favour.",
                "In you go. I'll be on the radio, and nowhere else you can reach.",
                "Clock's running. So, I imagine, are you.",
                "Do stretch first. I never did, and look where I am.",
                "Bring back more than you take in. That's the whole of economics.",
                "Mind the first step. It is the same height as you are.",
            },
            [RadioMoment.Resume] = new[]
            {
                "Welcome back. Nothing down here has improved while you were away.",
                "There you are. I kept your place. The ants tried to take it.",
                "Picking up where you left off. A brave choice, considering where that was.",
                "Ah, you returned. I had started the paperwork.",
            },
            [RadioMoment.Death] = new[]
            {
                "You're back. Smaller in spirit, the same in size.",
                "That went about as well as the form said it might.",
                "Don't be discouraged. The data from that was excellent.",
                "I've written it up as a learning experience. That's the box with the least paperwork.",
                "You kept some of the samples. I'd call that some of a success.",
                "For what it's worth, it looked surprised too.",
                "No need to apologise. You weren't going to, but there's no need.",
                "I went back over the recording. The mistake was early, and then also late.",
                "Take a moment. Then please go again; I'm on a schedule.",
                "The lab put you back together. It's good at that. It has had practice.",
                "Technically you didn't die. Technically is a word I lean on a great deal.",
                "I had hoped you'd get further. I had planned for this, though.",
                "Back so soon. I've left your tea where it was. It was never there.",
                "The lab says you're fine. The lab says that about everybody.",
                "Lessons learned, I hope. I've stopped keeping a list.",
                "It happens to the best of us. It also happens to you.",
            },
            [RadioMoment.Extract] = new[]
            {
                "Samples received. You may consider yourself adequate.",
                "A clean exit. I'm told people enjoy praise, so: well done.",
                "You came home on purpose. That's rarer than you'd think.",
                "These are good samples. I'll take the credit, as agreed.",
                "Back in one piece. I've updated your file from 'pending'.",
                "Sensible. Leaving is the one skill I never picked up.",
                "Good haul. I'd shake your hand, but I'm rather a long way down.",
                "Home safe. The ants will be relieved. I shan't speculate about you.",
            },
            [RadioMoment.Biome] = new[]
            {
                "Somewhere new. I've been, of course. I've been everywhere you're going.",
                "A fresh world. Small enough, everything is one. Do wipe your feet.",
                "New surroundings. Same instructions: further in, and try to come back.",
            },
            [RadioMoment.Boss] = new[]
            {
                "Ah. That one. I'd stay out of its way, which I realise is the opposite of your job.",
                "I walked past this one. Politely. You may need a different approach.",
                "Large, isn't it. Everything is, of course, but that especially.",
                "I'd offer advice, but what I did was be somewhere else.",
                "Do try to win. I've already told people you would.",
                "This is the difficult part. The earlier parts were also difficult; I didn't want to worry you.",
            },
            [RadioMoment.BossDown] = new[]
            {
                "Well. I've stopped telling people you wouldn't manage it.",
                "That's that. I'd have gone round, personally, but your way works.",
                "Impressive. I'm making a note, and it's a kind one.",
                "Dead, then. I'll let the others know. They won't take it well.",
                "Good. That one had been in my way for some time. In yours, I mean.",
                "You've cleared the way. Thank you. That's more useful to me than you know.",
            },
            [RadioMoment.Note] = new[]
            {
                "Ah. You found a page. That's mine, and I'd rather you didn't read it. You're reading it.",
                "Where did you get that? Don't answer. Put it on the board if you must.",
                "My handwriting was better then. So was my judgement.",
                "That page is out of context. The context is worse.",
                "You're collecting those. I see. I had hoped you were less thorough.",
                "Reading other people's notes is rude. Pinning them to a board is a hobby.",
            },
            [RadioMoment.Door] = new[]
            {
                "There is no door. If there were one, it would be locked. It isn't locked. There is no door.",
                "I see you've found the door there isn't. Do leave it alone.",
                "That door goes nowhere you're paid to go.",
                "You read every page. I had hoped you would skim.",
            },
            [RadioMoment.Bottom] = new[]
            {
                "Ah. You came down. Keep your voice low. It can hear a crumb land.",
                "Welcome to the bottom. I built it to look like the lab. Then something moved in.",
                "There you are. I'm in the wall. No, the other wall. Don't wave.",
            },
            [RadioMoment.FinchFight] = new[]
            {
                "That's the cat. I'd have mentioned the cat, but I've been busy hiding from the cat.",
                "Don't try to kill it. You can't, and I'd feel bad. Just be less interesting than it hoped.",
                "It isn't angry with you. It isn't anything with you. That's what makes it a cat.",
            },
            [RadioMoment.FinchDose] = new[]
            {
                "It's washing. It does that when it's losing interest. Now would be the time.",
                "Look, it's stopped to wash. It wants you to think it meant to do that.",
                "It's sitting down. Hit it while it's pretending not to care.",
                "Washing again. Good sign. Bad sign afterwards, in my experience.",
            },
            [RadioMoment.FinchDown] = new[]
            {
                "It's leaving. It's decided you were a waste of its afternoon. Well done.",
                "It's gone? It's gone. I'm coming out of the wall. Give me a week.",
            },
            [RadioMoment.FinchHome] = new[]
            {
                "You bored a cat into leaving. I'd put that on your review, if I could reach the form.",
                "You're home. I'm out of the wall. I'd like it noted that I was never technically stuck.",
            },
            [RadioMoment.After] = new[]
            {
                "Good morning. The cat hasn't been back. I've stopped holding my breath, mostly.",
                "The portal's open. So is the door. I've stopped pretending about the door.",
                "I had time to think, in the wall. I'm told that is what walls are for.",
                "You're still my intern, technically. I checked. Nobody else did.",
            },
            [RadioMoment.Dose] = new[]
            {
                "That was mine. I pushed it out of the wall as a thank-you. Do enjoy being shorter.",
                "You drank it. Of course you did. Welcome to the size I was last month.",
                "Smaller, are we? It doesn't suit you. It didn't suit me either.",
            },
            [RadioMoment.Overtime] = new[]
            {
                "You've clocked in. I didn't think that clock still worked. I didn't think you did, either.",
                "Overtime. It's paid at time and a half. Half of nothing is still very reasonable.",
                "Extra hours. I'd have approved them, if anybody had asked, and if I could reach the form.",
            },
            [RadioMoment.Dare] = new[]
            {
                "You rang a bell with DARE written over it. I'd like that noted somewhere, with the time.",
                "That bell calls more of them. I don't know who put it there. I suspect it was them.",
                "A dare. From a bell. And you took it. We'll discuss this when you're back. If.",
            },
            [RadioMoment.Twist] = new[]
            {
                "Something's different about this floor. I'd say be careful, but you've heard that one.",
                "This floor has a twist. I didn't order it. Nobody down there takes orders.",
                "Conditions have changed. The notice is on your screen. I read it. I didn't like it.",
            },
            [RadioMoment.Weapon] = new[]
            {
                "A different one. Good. I was running out of ways to describe the first one in the incident reports.",
                "That one is on loan. From me. I'd like it back in the condition you'd like to be in.",
                "Changed your mind. That's allowed. It's about the only thing in here that is.",
            },
            [RadioMoment.Exchange] = new[]
            {
                "You've sold my samples. To a hatch. I'd be angrier, but it's the most business we've done all year.",
                "Those samples were for science. Now they're for whatever that is. Carry on.",
                "A fair trade, says the hatch. The hatch would say that.",
            },
            [RadioMoment.MachineRoom] = new[]
            {
                "You've switched it on. I'd have advised against it, which is why you didn't ask.",
                "That's the machine with everything plugged back in. It was quieter in pieces.",
                "All four parts. It hasn't run like this since the day I got smaller.",
            },
            [RadioMoment.Cycle] = new[]
            {
                "It's restarted. Everything out there has too. The parts are gone again; don't look at me.",
                "You turned it off and on again. That's all anybody has ever managed with it.",
                "Good news: it works. Bad news: it works. The worlds are a size harder now.",
            },
            [RadioMoment.ShotRadio] = new[]
            {
                "Ow. That's the radio. I'm not in the radio. It still stings, somehow.",
                "You've shot the radio. It was a gift. From me, to me.",
                "Please stop shooting the radio. It's the only thing in there that agrees with me.",
                "That's coming out of your samples. Everything is. This too.",
                "I felt that. I shouldn't have. We'll both pretend I didn't.",
            },
            [RadioMoment.LowHealth] = new[]
            {
                "You appear to be mostly damage. Do something about that.",
                "One more of those and I have to fill in a form.",
                "I'd suggest not getting hit. I appreciate that's the general idea already.",
                "Your readings are doing something I'd describe as 'brief'.",
                "Careful. I've only the one intern.",
                "Deep breath. Small targets are hard to hit, and you are very small.",
                "That is your last one. I'm not being dramatic. I counted.",
                "If you see a crumb, eat it. I don't say that about most things down here.",
                "Somewhere nearby there is a shop. I mention it for no reason.",
            },
            [RadioMoment.Item] = new[]
            {
                "Oh, you found one of those. Mine's better, but well done.",
                "Put that on. Or in. I forget how that one attaches.",
                "That's not standard issue. Nothing is. Carry on.",
                "Interesting. I left that there, I think. Or something left it.",
                "Keep it. The previous owner has no further use for it.",
                "You're becoming quite unusual. That's the job going well.",
                "I wouldn't ask where that came from. I didn't.",
                "A fine specimen. So is the thing you picked up.",
                "That one's good. I say that about all of them, so do check.",
                "Ah, I wondered where that had got to. Keep it. I've changed.",
                "It suits you. Most things do, at that size.",
                "Careful how you hold that. I'm not certain which end is the end.",
            },
            [RadioMoment.Floor] = new[]
            {
                "Further down, please. I'm still below you. Quite a lot below you.",
                "I came this way once. It was tidier.",
                "You're doing well. I say that to the equipment too.",
                "Mind the floor. And the walls. And anything that looks back.",
                "I can hear you walking. Could you do it more quietly? Not for me. For them.",
                "Still with me? Good. It gets lonely between interns.",
                "If you find a notebook down here, it's mine, and it's private.",
                "Lovely tunnels. Structurally they shouldn't work. I admire that.",
                "You're making good time. I've no idea what time it is, mind.",
                "I left a sandwich down here once. Do not look for the sandwich.",
                "If something offers to sell you things, that's the beetle. It's honest. For a beetle.",
                "The warning marks on the floor are real. I used to think they were decorative.",
                "Chin up. Or whatever it is you have.",
                "If one of them hangs back and hums, shoot that one first. Trust me.",
            },
            [RadioMoment.GoldBug] = new[]
            {
                "That's a gold bug. Catch it. I'd explain why, but it will have gone.",
                "Gold bug. It owes me money. Long story. Shoot it.",
                "There, the shiny one. It leaves when it's bored, like everybody.",
            },
            [RadioMoment.ClawLost] = new[]
            {
                "It dropped it. It's meant to. I designed it. I'm not proud.",
                "The claw has never held anything in its life. Nor have I, professionally.",
                "Keep feeding it. It's the only thing down there on a salary.",
            },
            [RadioMoment.Deep] = new[]
            {
                "You're deeper than most of my interns got. I should say that more carefully. Deeper than the others.",
                "It's quieter this far in. I'd almost forgotten there was a way back up.",
                "You're getting closer. I'd rather you didn't, and I'd rather you did. It's complicated.",
                "I'm not lost, you understand. I know exactly how far down I am.",
                "Keep going. I need the way kept clear behind me.",
                "If I stop answering, carry on as normal. It only means I've gone a little further.",
                "Nobody comes down this far by accident. I'd like to know which of us is which.",
                "I can hear you from here now. That's new. I'm not sure I like it.",
                "You'd tell me if you found a door, wouldn't you. No. Nor would I.",
                "It is very quiet where I am. I've started narrating. You may have noticed.",
            },
        };

        /// <summary>Lines about one particular place or boss, said before the general ones. Keyed by a word in its name.</summary>
        static readonly Dictionary<string, string[]> Particular = new Dictionary<string, string[]>
        {
            ["ants"] = new[]
            {
                "The ant colony. They've been here a hundred million years and have opinions about visitors.",
                "Ants again. They're very organised. You'll find that upsetting.",
            },
            ["cake"] = new[]
            {
                "A birthday cake. Someone's party is going ahead without them. Don't touch the candles.",
                "Everything in here is edible, and feels the same way about you.",
            },
            ["sofa"] = new[]
            {
                "Under the sofa. Everything anybody ever lost is here. Including, for a while, me.",
                "Dark, dusty, and the coins are the size of dinner tables. Don't get attached to one.",
            },
            // What Finch lets slip after each restart of the machine, in order: the story, a cycle at a time.
            ["restart one"] = new[] { "Between us: I'm not in the building, exactly. I'm near it. Let's leave it at near." },
            ["restart two"] = new[] { "I built that machine to make things smaller. It worked once. I was standing too close." },
            ["restart three"] = new[] { "Something large lives here. It has a bell it doesn't wear. I listen for it anyway." },
            ["restart four"] = new[] { "I kept notes. They got away from me, a page at a time. You may have trodden on some." },
            ["restart five"] = new[] { "If you ever find all of my notebook, don't come looking. That is not reverse psychology." },
            ["library"] = new[]
            {
                "A library. That isn't in the house. That isn't in any house I know of. Interesting.",
                "Books, the size of buildings. Somebody is going to want those back.",
            },
            ["forest"] = new[]
            {
                "Trees. Real ones, with a sky on top. I had forgotten what that looked like. Describe it. No, don't, keep moving.",
                "Mind the leaf litter. Some of it is leaves.",
            },
            ["froglet"] = new[]
            {
                "A froglet. Young, keen, and it lands where you were going. Go somewhere else.",
            },
            ["toad"] = new[]
            {
                "The Toad. When it stops turning, it's about to lash. Step off the line. I never did learn to.",
            },
            ["librarian"] = new[]
            {
                "The Librarian. When they say quiet, be quiet. I never managed it myself.",
            },
            ["trainee"] = new[]
            {
                "A trainee. Be kind. No, don't be kind, they're throwing books.",
            },
            ["machine"] = new[]
            {
                "That's my machine. Be gentle. No, be thorough. I can't decide.",
                "It pulls. Walk the other way. I learned that one in person.",
            },
            ["vacuum"] = new[]
            {
                "The robot vacuum. It keeps a schedule. You are on it.",
            },
            ["grandmother"] = new[]
            {
                "Mostly hair and regret. Go round the back. I didn't tell you that.",
            },
            ["fridge"] = new[]
            {
                "The fridge. Somebody keeps opening the door to look, then closing it. That is not weather. That is a person.",
                "Cold in here. Things keep, down here. Not well, but for a very long time.",
            },
            ["leftovers"] = new[]
            {
                "The Leftovers. I don't know what it was. It doesn't either, and it is angry about that.",
            },
            ["ice maker"] = new[]
            {
                "The Ice Maker. It was fitted for convenience. Nobody has asked whose.",
            },
            ["queen"] = new[]
            {
                "Her Majesty. We had an arrangement once. Don't mention my name.",
            },
            ["brood"] = new[]
            {
                "The Brood Mother. Protective. I'd compliment the children, but there are a great many and they bite.",
            },
            ["candle"] = new[]
            {
                "It's a candle. I'm aware. Please take it seriously anyway; it takes itself very seriously.",
            },
            ["jelly"] = new[]
            {
                "A jelly. It wobbles, but it does not fall down. I've checked.",
            },
        };

        public static bool IsChatter(RadioMoment moment) => moment >= RadioMoment.LowHealth;

        public static IReadOnlyList<string> PoolFor(RadioMoment moment) => Pools[moment];

        public static IEnumerable<string> All()
        {
            foreach (var pool in Pools.Values) foreach (string line in pool) yield return line;
            foreach (var pool in Particular.Values) foreach (string line in pool) yield return line;
        }

        static readonly string[] RestartWords = { "one", "two", "three", "four", "five" };

        /// <summary>What to ask about after the machine's nth restart, so that restart's part of the story is told. Null once it is all out.</summary>
        public static string RestartKey(int cycle) => cycle >= 1 && cycle <= RestartWords.Length ? "restart " + RestartWords[cycle - 1] : null;

        /// <summary>
        /// The next thing to say: a line about this particular place or boss if there is one not yet heard, otherwise
        /// one from the moment's pool that has not been heard since the pool last ran out.
        /// </summary>
        public static string Pick(RadioMoment moment, string about, System.Random rng)
        {
            List<int> heard = SaveSystem.Data.meta.radioHeard;

            if (!string.IsNullOrEmpty(about))
            {
                string lower = about.ToLowerInvariant();
                foreach (var pair in Particular)
                {
                    if (!lower.Contains(pair.Key)) continue;
                    foreach (string line in pair.Value)
                    {
                        if (heard.Contains(Id(line))) continue;
                        heard.Add(Id(line));
                        return line;
                    }
                }
            }

            string[] pool = Pools[moment];
            var fresh = new List<string>();
            foreach (string line in pool) if (!heard.Contains(Id(line))) fresh.Add(line);
            if (fresh.Count == 0)
            {
                // All heard: start the pool again.
                foreach (string line in pool) heard.Remove(Id(line));
                fresh.AddRange(pool);
            }

            string pick = fresh[rng.Next(fresh.Count)];
            heard.Add(Id(pick));
            return pick;
        }

        // The same number for the same line every time the game runs, which string.GetHashCode does not promise.
        public static int Id(string line)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in line) hash = (hash ^ c) * 16777619;
                return (int)hash;
            }
        }
    }
}
