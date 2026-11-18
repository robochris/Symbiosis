using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// Text shown at the bottom of the screen when the player is near something they can use.
    /// Anything in the world can offer a line; the nearest one that has something to say wins.
    /// </summary>
    public static class WorldPrompt
    {
        static readonly List<Func<Vector3, string>> Sources = new List<Func<Vector3, string>>();

        public static void Add(Func<Vector3, string> source) => Sources.Add(source);
        public static void Remove(Func<Vector3, string> source) => Sources.Remove(source);

        public static string For(Vector3 playerPosition)
        {
            foreach (var source in Sources)
            {
                string text = source(playerPosition);
                if (!string.IsNullOrEmpty(text)) return text;
            }
            return null;
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
