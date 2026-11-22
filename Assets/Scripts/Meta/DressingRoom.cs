using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The dressing room, through the door at the back of the office: the hat stand, the mirror for styles and the
    /// pet jar, all in one place. Its door stays shut until the first run is over; the jar comes with the first boss.
    /// </summary>
    public class DressingRoom : MonoBehaviour
    {
        [Tooltip("Shut until the room opens.")]
        public GameObject door;

        /// <summary>Where the hat stand and the pet jar go, in the room.</summary>
        public static readonly Vector3 HatStandAt = new Vector3(-17f, 0f, 16f), PetJarAt = new Vector3(-14f, 0f, 12f);

        public static DressingRoom Current { get; private set; }
        public bool IsOpen { get; private set; }

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            IsOpen = Unlocks.Open(Unlocks.Feature.HatStand);
            if (door != null) door.SetActive(!IsOpen);
            if (!IsOpen || PlayerController.Instance == null) return;
            HatStand.Place(HatStandAt);
            if (Unlocks.Open(Unlocks.Feature.PetJar)) PetJar.Place(PetJarAt);
        }
    }
}
