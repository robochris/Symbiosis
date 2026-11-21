using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// The Decoy power: a cardboard cut-out of the Void, left where the player stood. While it stands every enemy goes
    /// for it instead of the player, bosses included. It falls over when its time is up or it has taken enough.
    /// </summary>
    public class Decoy : MonoBehaviour
    {
        public const int Toughness = 8;

        /// <summary>The one standing now, if any. Enemies look here before they look for the player.</summary>
        public static Decoy Current { get; private set; }
        public Health Health { get; private set; }

        float _until;

        public static Decoy Place(Vector3 position, Quaternion facing, float seconds)
        {
            if (Current != null) Destroy(Current.gameObject);
            var go = new GameObject("Decoy");
            go.transform.SetPositionAndRotation(position, facing);
            var decoy = go.AddComponent<Decoy>();
            decoy._until = Time.time + seconds;

            var collider = go.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.4f;
            decoy.Health = go.AddComponent<Health>();
            decoy.Health.team = Team.Player;
            decoy.Health.maxHealth = Toughness;
            decoy.Health.Died += decoy.FallOver;

            // Flat cardboard, cut roughly to shape, on a little stand.
            var cardboard = MachineRoom.Lit(new Color(0.72f, 0.55f, 0.35f), 0.05f);
            var ink = MachineRoom.Unlit(new Color(0.05f, 0.05f, 0.07f));
            MachineRoom.Piece(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 1.1f, 0.04f), cardboard);
            MachineRoom.Piece(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 1.6f, 0f), new Vector3(0.55f, 0.02f, 0.55f), cardboard).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MachineRoom.Piece(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 1.6f, 0.03f), new Vector3(0.45f, 0.02f, 0.45f), ink).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MachineRoom.Piece(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.05f, -0.2f), new Vector3(0.5f, 0.1f, 0.4f), cardboard);
            Current = decoy;
            return decoy;
        }

        void Update()
        {
            if (Time.time >= _until) FallOver();
        }

        void FallOver()
        {
            if (Current == this) Current = null;
            Fx.Burst(transform.position + Vector3.up * 0.8f, new Color(0.72f, 0.55f, 0.35f), 16, 5f, 0.15f, 0.5f);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
