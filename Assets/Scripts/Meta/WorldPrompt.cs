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
    /// A spot on the floor that does something when the player steps onto it. If the player is already standing on it
    /// when it appears (or has just used it), they must step off first, so nothing ever triggers by surprise or twice.
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

        /// <summary>True on the frame the player steps onto the pad.</summary>
        public bool SteppedOn(Vector3 padPosition, Vector3 playerPosition)
        {
            float distance = Distance(padPosition, playerPosition);
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
