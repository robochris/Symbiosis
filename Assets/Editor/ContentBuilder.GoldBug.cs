using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>The gold bug: the one thing in the game that runs away.</summary>
    public static partial class ContentBuilder
    {
        public static Enemy BuildGoldBug()
        {
            var gold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var bug = Creature<GoldBug>("GoldBug", 0.45f, 0.9f, 6, 0.8f, 1.6f, out var model);
            Decor(PrimitiveType.Sphere, "Shell", model, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.6f, 1f), gold);
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 0.35f, 0.5f), Vector3.one * 0.4f, dark);
            Decor(PrimitiveType.Cube, "Seam", model, new Vector3(0f, 0.7f, -0.05f), new Vector3(0.03f, 0.03f, 0.8f), dark);
            Decor(PrimitiveType.Sphere, "Eye L", model, new Vector3(-0.12f, 0.42f, 0.66f), Vector3.one * 0.1f, gold);
            Decor(PrimitiveType.Sphere, "Eye R", model, new Vector3(0.12f, 0.42f, 0.66f), Vector3.one * 0.1f, gold);
            bug.knockbackOnHit = 2f;
            // It is what it is in every world: no mutations.
            Object.DestroyImmediate(bug.GetComponent<EnemyMutations>());
            return SaveEnemy(bug);
        }
    }
}
