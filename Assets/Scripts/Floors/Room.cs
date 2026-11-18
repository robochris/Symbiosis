using System;
using UnityEngine;

namespace Shrunk
{
    /// <summary>One side of a room: either a doorway to a neighbour, or solid wall.</summary>
    [Serializable]
    public class DoorSlot
    {
        [Tooltip("Wall piece that plugs the doorway when there is no room on this side.")]
        public GameObject wallFill;
        [Tooltip("Barrier that closes the doorway while the room is locked.")]
        public GameObject door;
    }

    /// <summary>
    /// A hand-built room that fills one grid square. Every room has the same footprint and a door slot on each side,
    /// so the generator can place any room anywhere.
    /// </summary>
    public class Room : MonoBehaviour
    {
        [Tooltip("North, East, South, West.")]
        public DoorSlot[] slots = new DoorSlot[4];
        [Tooltip("Places enemies can appear when the room locks.")]
        public Transform[] enemySpawns;
        [Tooltip("Exit rooms only: the way down, hidden until the room is cleared.")]
        public GameObject exitHole;
        [Tooltip("Half the width of the walkable inside of the room.")]
        public float interiorHalfSize = 11f;

        public Doors Doors { get; private set; }
        public bool Locked { get; private set; }

        /// <summary>Opens the sides that have a neighbour and walls up the rest.</summary>
        public void Configure(Doors doors)
        {
            Doors = doors;
            Locked = false;
            for (int i = 0; i < slots.Length; i++)
            {
                bool hasDoor = (doors & (Doors)(1 << i)) != 0;
                if (slots[i].wallFill != null) slots[i].wallFill.SetActive(!hasDoor);
                if (slots[i].door != null) slots[i].door.SetActive(false);
            }
            if (exitHole != null) exitHole.SetActive(false);
        }

        public void SetLocked(bool locked)
        {
            Locked = locked;
            for (int i = 0; i < slots.Length; i++)
            {
                bool hasDoor = (Doors & (Doors)(1 << i)) != 0;
                if (slots[i].door != null) slots[i].door.SetActive(locked && hasDoor);
            }
        }

        /// <summary>True when the point is inside the room and at least <paramref name="margin"/> away from the walls.</summary>
        public bool ContainsInside(Vector3 worldPoint, float margin)
        {
            Vector3 local = worldPoint - transform.position;
            float limit = interiorHalfSize - margin;
            return Mathf.Abs(local.x) <= limit && Mathf.Abs(local.z) <= limit;
        }
    }
}
