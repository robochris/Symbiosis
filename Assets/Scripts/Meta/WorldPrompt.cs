using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Text shown at the bottom of the screen when the player is near something they can use.
    /// Anything in the world can offer a line; when more than one has something to say, the nearest wins, so two
    /// things standing close together each read as themselves.
    /// </summary>
    public static class WorldPrompt
    {
        struct Source
        {
            public Func<Vector3, string> ask;
            public Transform at;
        }

        static readonly List<Source> Sources = new List<Source>();

        /// <param name="at">Where the thing is, for choosing the nearest. Leave it out for something with no one place.</param>
        public static void Add(Func<Vector3, string> source, Transform at = null) => Sources.Add(new Source { ask = source, at = at });

        public static void Remove(Func<Vector3, string> source)
        {
            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                if (Sources[i].ask == source) Sources.RemoveAt(i);
            }
        }

        public static string For(Vector3 playerPosition)
        {
            string best = null;
            float bestDistance = float.MaxValue;
            // Every source is asked, not only until one answers: some of them keep count of the player walking up.
            for (int i = 0; i < Sources.Count; i++)
            {
                Source source = Sources[i];
                string text = source.ask(playerPosition);
                if (string.IsNullOrEmpty(text)) continue;

                float distance = 0f;
                if (source.at != null)
                {
                    Vector3 to = source.at.position - playerPosition;
                    to.y = 0f;
                    distance = to.sqrMagnitude;
                }
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = text;
                }
            }
            return best;
        }
    }

    /// <summary>
    /// Using things: the player stands at one and presses E (or F, or X on a pad, or taps USE on a phone). Walking over
    /// something never sets it off. With more than one thing in reach, the nearest is the one used.
    /// </summary>
    public static class Interact
    {
        /// <summary>How far past a pad's own radius it can still be used from.</summary>
        public const float Reach = 0.6f;
        /// <summary>Tests set this: stepping onto a pad counts as using it, as it did before there was a button.</summary>
        public static bool AutoUse;
        /// <summary>Set by the input reader: whether the player is on a touch screen, for the wording.</summary>
        public static bool OnTouch;

        /// <summary>What to tell the player to do, for prompts: "Press E" or "Tap USE".</summary>
        public static string Verb => OnTouch ? "Tap USE" : "Press E";

        /// <summary>True while something usable is in reach (the phone's USE button shows only then).</summary>
        public static bool Available => Time.frameCount - _availableFrame <= 1;

        static int _availableFrame = -10, _pressFrame = -10, _offerFrame = -10;
        static StepPad _best, _chosen;
        static float _bestDistance;

        /// <summary>The use button went down this frame.</summary>
        public static void Press()
        {
            _pressFrame = Time.frameCount;
            _chosen = null;
            _best = null;
        }

        // Every pad in reach offers itself for a frame or two after a press; then the nearest is chosen, and it alone
        // answers true. Pads update in no fixed order, so the choice waits until all of them have had their say.
        internal static bool Check(StepPad pad, float distance, float reach)
        {
            int frame = Time.frameCount;
            bool inReach = distance <= reach;
            if (inReach) _availableFrame = frame;

            if (_chosen == pad)
            {
                _chosen = null;
                return true;
            }

            bool pressLive = frame - _pressFrame <= 1;
            if (pressLive)
            {
                if (inReach && (_offerFrame != _pressFrame || distance < _bestDistance))
                {
                    _best = pad;
                    _bestDistance = distance;
                    _offerFrame = _pressFrame;
                }
                return false;
            }
            if (_best != null && _offerFrame == _pressFrame)
            {
                _chosen = _best;
                _best = null;
                if (_chosen == pad)
                {
                    _chosen = null;
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// A spot on the floor that does something when the player stands at it and presses use (see Interact). With
    /// Interact.AutoUse on (tests), stepping onto it is enough, once, and the player must step off before it goes again.
    /// </summary>
    [Serializable]
    public class StepPad
    {
        public float radius = 1.2f;
        bool _armed;

        public float Distance(Vector3 padPosition, Vector3 playerPosition)
        {
            Vector3 to = playerPosition - padPosition;
            to.y = 0f;
            return to.magnitude;
        }

        /// <summary>True on the frame the player uses the pad.</summary>
        public bool SteppedOn(Vector3 padPosition, Vector3 playerPosition)
        {
            float distance = Distance(padPosition, playerPosition);
            bool used = Interact.Check(this, distance, radius + Interact.Reach);
            if (!Interact.AutoUse) return used;
            if (distance > radius + 0.5f)
            {
                _armed = true;
                return false;
            }
            if (!_armed || distance > radius) return false;
            _armed = false;
            return true;
        }
    }
}
