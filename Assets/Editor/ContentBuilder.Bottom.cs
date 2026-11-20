using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The bottom: where the lab's hidden door leads. One quiet floor that looks like a copy of the lab, because
    /// Dr. Finch built it from memory, and at the end of it the cat that has kept him in the wall ever since.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string BottomPath = "Assets/Data/TheBottom.asset";

        public static BiomeDefinition BuildBottom(BiomeDefinition ants)
        {
            var coat = ArenaSceneBuilder.Lit("LabCoat", new Color(0.93f, 0.94f, 0.96f), 0.3f);
            var skin = ArenaSceneBuilder.Lit("FinchSkin", new Color(0.85f, 0.68f, 0.56f), 0.3f);
            var hair = ArenaSceneBuilder.Lit("FinchHair", new Color(0.7f, 0.7f, 0.72f), 0.2f);
            var dark = ArenaSceneBuilder.Lit("FinchDark", new Color(0.12f, 0.13f, 0.17f), 0.4f);
            var glass = ArenaSceneBuilder.Unlit("FinchGlasses", new Color(0.55f, 0.9f, 1f));
            var dose = ArenaSceneBuilder.Unlit("FinchDose", new Color(0.45f, 1f, 0.6f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));

            // ----- the cat: far too big for the room, and not much interested in it -----
            var fur = ArenaSceneBuilder.Lit("CatFur", new Color(0.9f, 0.55f, 0.2f), 0.15f);
            var cream = ArenaSceneBuilder.Lit("CatCream", new Color(0.97f, 0.9f, 0.78f), 0.15f);
            var stripe = ArenaSceneBuilder.Lit("CatStripe", new Color(0.6f, 0.32f, 0.1f), 0.15f);
            var eye = ArenaSceneBuilder.Unlit("CatEye", new Color(0.55f, 1f, 0.35f));
            var pink = ArenaSceneBuilder.Unlit("CatNose", new Color(1f, 0.6f, 0.65f));
            var cat = Creature<CatBoss>("TheCat", 1.7f, 3.6f, 750, 1f, 5.6f, out var model);
            Decor(PrimitiveType.Sphere, "Body", model, new Vector3(0f, 2.1f, -0.9f), new Vector3(2.5f, 2.3f, 4.6f), fur);
            Decor(PrimitiveType.Sphere, "Belly", model, new Vector3(0f, 1.75f, -0.7f), new Vector3(2.1f, 1.7f, 3.8f), cream);
            Decor(PrimitiveType.Sphere, "Chest", model, new Vector3(0f, 2.3f, 0.9f), new Vector3(2.1f, 2.2f, 1.9f), cream);
            for (int i = 0; i < 3; i++)
            {
                Decor(PrimitiveType.Cube, "Stripe " + i, model, new Vector3(0f, 3.12f - i * 0.03f, -0.4f - i * 0.8f), new Vector3(1.6f, 0.25f, 0.28f), stripe);
            }
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 3.5f, 1.9f), new Vector3(2.1f, 1.85f, 1.9f), fur);
            Decor(PrimitiveType.Sphere, "Muzzle", model, new Vector3(0f, 3.2f, 2.7f), new Vector3(1f, 0.7f, 0.6f), cream);
            Decor(PrimitiveType.Sphere, "Nose", model, new Vector3(0f, 3.38f, 2.98f), new Vector3(0.24f, 0.18f, 0.14f), pink);
            Decor(PrimitiveType.Sphere, "Eye L", model, new Vector3(-0.5f, 3.75f, 2.68f), new Vector3(0.42f, 0.5f, 0.16f), eye);
            Decor(PrimitiveType.Sphere, "Eye R", model, new Vector3(0.5f, 3.75f, 2.68f), new Vector3(0.42f, 0.5f, 0.16f), eye);
            Decor(PrimitiveType.Cube, "Pupil L", model, new Vector3(-0.5f, 3.75f, 2.77f), new Vector3(0.08f, 0.42f, 0.04f), stripe);
            Decor(PrimitiveType.Cube, "Pupil R", model, new Vector3(0.5f, 3.75f, 2.77f), new Vector3(0.08f, 0.42f, 0.04f), stripe);
            Decor(PrimitiveType.Cube, "Ear L", model, new Vector3(-0.68f, 4.45f, 1.8f), new Vector3(0.6f, 0.75f, 0.2f), fur).transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            Decor(PrimitiveType.Cube, "Ear R", model, new Vector3(0.68f, 4.45f, 1.8f), new Vector3(0.6f, 0.75f, 0.2f), fur).transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Decor(PrimitiveType.Cube, "Whiskers", model, new Vector3(0f, 3.22f, 2.9f), new Vector3(2.4f, 0.03f, 0.03f), cream);
            Decor(PrimitiveType.Cube, "Whiskers Low", model, new Vector3(0f, 3.1f, 2.88f), new Vector3(2.1f, 0.03f, 0.03f), cream).transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            foreach (var leg in new[] { new Vector3(-0.8f, 0.7f, 1f), new Vector3(0.8f, 0.7f, 1f), new Vector3(-0.85f, 0.7f, -2.3f), new Vector3(0.85f, 0.7f, -2.3f) })
            {
                Decor(PrimitiveType.Cylinder, "Leg", model, leg, new Vector3(0.62f, 0.7f, 0.62f), fur);
                Decor(PrimitiveType.Sphere, "Paw", model, new Vector3(leg.x, 0.16f, leg.z + 0.18f), new Vector3(0.8f, 0.32f, 0.95f), cream);
            }
            Decor(PrimitiveType.Cylinder, "Tail", model, new Vector3(0f, 3.4f, -3.3f), new Vector3(0.36f, 1.5f, 0.36f), fur).transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Tail Tip", model, new Vector3(0f, 4.9f, -3.78f), Vector3.one * 0.5f, cream);

            cat.displayName = "The Cat";
            cat.subtitle = "Not angry. Not anything. You cannot win. You can only bore it.";
            cat.telegraphMaterial = telegraph;
            cat.sampleReward = 60;
            cat.knockbackOnHit = 0.2f;
            // Nothing about the cat is an accident of the biome: no mutations.
            Object.DestroyImmediate(cat.GetComponent<EnemyMutations>());
            var finchPrefab = (BossEnemy)SaveEnemy(cat);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BottomPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, BottomPath);
            }

            // Everything the ants have (the special-room props, the sizes), then its own look on top.
            EditorUtility.CopySerialized(ants, biome);
            biome.name = "TheBottom";
            biome.displayName = "The Bottom";
            biome.id = "bottom";
            biome.mutationTheme = "there is no further down";
            biome.isBottom = true;

            // A copy of the lab, built from memory by somebody very small.
            biome.floorMaterial = ArenaSceneBuilder.Lit("LabFloor", new Color(0.32f, 0.36f, 0.42f), 0.5f);
            biome.wallMaterial = ArenaSceneBuilder.Lit("LabWall", new Color(0.18f, 0.2f, 0.26f), 0.3f);
            biome.ceilingMaterial = biome.wallMaterial;
            biome.doorMaterial = ArenaSceneBuilder.Lit("LabBench", new Color(0.5f, 0.52f, 0.56f), 0.7f);
            biome.coverMaterial = biome.doorMaterial;
            biome.ceiling = true;
            biome.skyColor = new Color(0.02f, 0.02f, 0.04f);
            biome.ambientColor = new Color(0.8f, 0.88f, 0.95f);
            biome.sunIntensity = 0.8f;
            biome.fog = true;
            biome.fogColor = new Color(0.02f, 0.02f, 0.04f);
            biome.glowMaterial = glass;
            biome.glowStemMaterial = biome.doorMaterial;

            // No crumbs, roots, pods or dew: nothing grows here.
            biome.crumbMaterial = null;
            biome.sugarMaterial = null;
            biome.rootMaterial = null;
            biome.podMaterial = null;
            biome.podHatchling = null;
            biome.dewMaterial = null;

            biome.spawnTable = new EnemySpawnEntry[0];
            biome.swarmEnemy = ants.swarmEnemy;
            biome.miniBoss = finchPrefab;
            biome.boss = finchPrefab;
            biome.mutations = new MutationDefinition[0];
            biome.deeperPortalProp = null;
            biome.portalProp = null;

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
