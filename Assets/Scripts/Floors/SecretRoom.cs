using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A secret room: a dead end walled off behind a cracked wall where its doorway should be. Nothing says it is
    /// there but the cracks, and it is not on the map. Shoot the wall down and inside is an item, some samples and a
    /// few coins, free. Five found is an achievement.
    /// </summary>
    public class SecretWall : MonoBehaviour
    {
        public const int Toughness = 6;
        public const int Samples = 10, Coins = 6;
        [Tooltip("Chance a floor (from the second of a world on) has a secret room, where it has a dead end to spare.")]
        public static float Chance = 0.3f;
        /// <summary>Tests set this to make a secret room certain (1) or impossible (0).</summary>
        public static float? ChanceForAll;

        public Room room;
        public RoomNode node;
        public GameObject cracks;
        public bool Broken { get; private set; }

        Health _health;

        /// <summary>Seals a room's one doorway with a cracked wall, and lays the room's loot out ready.</summary>
        public static SecretWall Seal(Room room, RoomNode node, Material crackMaterial, Material wallMaterial)
        {
            GameObject door = null;
            for (int side = 0; side < 4 && door == null; side++) door = room.DoorObject(side);
            if (door == null) return null;

            door.name = "Secret Wall";
            door.SetActive(true);
            // The same stuff as the walls either side of it: only the cracks give it away.
            if (wallMaterial != null) door.GetComponent<Renderer>().sharedMaterial = wallMaterial;
            // Health first, so the wall hears about it when it is knocked down.
            var health = door.AddComponent<Health>();
            health.team = Team.Enemy;
            health.maxHealth = Toughness;
            var wall = door.AddComponent<SecretWall>();
            wall.room = room;
            wall.node = node;

            // A few dark cracks across the face, both sides, as their own pieces so the wall's stretch does not bend them.
            wall.cracks = new GameObject("Cracks");
            wall.cracks.transform.SetParent(room.transform, false);
            wall.cracks.transform.SetPositionAndRotation(door.transform.position, door.transform.rotation);
            Vector3 size = door.transform.lossyScale;
            bool alongX = size.x >= size.z;
            float thick = (alongX ? size.z : size.x) * 0.5f + 0.02f;
            for (int i = 0; i < 4; i++)
            {
                for (int face = -1; face <= 1; face += 2)
                {
                    var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(crack.GetComponent<Collider>());
                    crack.transform.SetParent(wall.cracks.transform, false);
                    float along = (i - 1.5f) * 0.7f;
                    float up = -0.6f + (i % 2) * 0.9f;
                    crack.transform.localPosition = alongX ? new Vector3(along, up, face * thick) : new Vector3(face * thick, up, along);
                    crack.transform.localScale = alongX ? new Vector3(0.9f, 0.06f, 0.02f) : new Vector3(0.02f, 0.06f, 0.9f);
                    if (alongX) crack.transform.localRotation = Quaternion.Euler(0f, 0f, 35f + i * 40f);
                    else crack.transform.localRotation = Quaternion.Euler(35f + i * 40f, 0f, 0f);
                    crack.GetComponent<Renderer>().sharedMaterial = crackMaterial;
                }
            }
            return wall;
        }

        void OnEnable()
        {
            if (_health == null) _health = GetComponent<Health>();
            if (_health != null) _health.Died += Break;
        }

        void OnDisable()
        {
            if (_health != null) _health.Died -= Break;
        }

        void Break()
        {
            if (Broken) return;
            Broken = true;
            if (cracks != null) Destroy(cracks);
            Fx.Burst(transform.position, new Color(0.7f, 0.65f, 0.6f), 26, 6f, 0.2f, 0.6f);
            Sfx.Play(SfxId.Blast);
            gameObject.SetActive(false);

            // On the map from now on.
            if (node != null) node.visited = true;
            var meta = SaveSystem.Data.meta;
            meta.secretsFound++;
            Jobs.Count(JobKind.Secrets);
            if (meta.secretsFound >= Achievements.SecretsNeeded) Achievements.Unlock(AchievementId.NosyParker);
            SaveSystem.Save();

            var assets = GameAssets.Instance;
            Vector3 middle = room.Centre;
            if (assets != null && assets.items != null)
            {
                var picks = assets.items.RandomCommon(new System.Random(Random.Range(0, int.MaxValue)), 1);
                if (picks.Count > 0) Pickup.Item(picks[0], middle + Vector3.up * 0.6f, 0);
            }
            Pickup.Sample(middle + new Vector3(1.6f, 0f, 0f), Samples);
            for (int i = 0; i < Coins; i++) Pickup.Coin(middle + new Vector3(-1.6f + (i % 3) * 0.4f, 0f, -0.4f + (i / 3) * 0.8f), 1);

            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("A secret room. Somebody walled this up on purpose.", 4f);
        }
    }
}
