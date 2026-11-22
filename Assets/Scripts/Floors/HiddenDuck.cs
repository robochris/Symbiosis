using UnityEngine;

namespace Shrunk
{
    /// <summary>
    /// A rubber duck, hidden somewhere in every world: now and then, on a floor of a world whose duck has not been
    /// found, one sits in the corner of a room. Walk over it to take it. Ten found is a hat.
    /// </summary>
    public class HiddenDuck : MonoBehaviour
    {
        public const float Chance = 0.15f;
        public const int ForTheHat = 10;
        public static float? ChanceForAll;

        public string world;
        public static HiddenDuck Current { get; private set; }

        public static int Found => SaveSystem.Data.meta.ducksFound.Count;

        public static HiddenDuck Place(Room room, Vector3 at, string world)
        {
            var go = new GameObject("Hidden Duck");
            go.transform.SetParent(room.transform, false);
            go.transform.position = at;
            var yellow = new Color(1f, 0.85f, 0.15f);
            Part.Make(PrimitiveType.Sphere, "Body", go.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.35f, 0.55f), yellow);
            Part.Make(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0f, 0.45f, 0.18f), Vector3.one * 0.28f, yellow);
            Part.Make(PrimitiveType.Cube, "Beak", go.transform, new Vector3(0f, 0.43f, 0.34f), new Vector3(0.12f, 0.05f, 0.12f), new Color(1f, 0.5f, 0.1f));
            var duck = go.AddComponent<HiddenDuck>();
            duck.world = world;
            return duck;
        }

        void OnEnable() => Current = this;
        void OnDisable() { if (Current == this) Current = null; }

        void Update()
        {
            transform.Rotate(0f, 40f * Time.deltaTime, 0f);
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude > 1.1f) return;
            Take();
        }

        public void Take()
        {
            var found = SaveSystem.Data.meta.ducksFound;
            if (!found.Contains(world)) found.Add(world);
            SaveSystem.Save();
            Sfx.Play(SfxId.Item, 0.8f);
            Fx.Burst(transform.position + Vector3.up * 0.4f, new Color(1f, 0.85f, 0.15f), 20, 5f, 0.12f, 0.5f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast($"A rubber duck! Squeak. {Found} found." + (Found == ForTheHat ? " That is ten: there is a hat for this at the stand." : " There is one hidden in every world."), 4f);
            Destroy(gameObject);
        }
    }
}
