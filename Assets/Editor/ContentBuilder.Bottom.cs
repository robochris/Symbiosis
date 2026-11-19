using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The bottom: where the lab's hidden door leads. One quiet floor that looks like a copy of the lab, because
    /// Dr. Finch built it from memory, and Dr. Finch at the end of it.
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

            // ----- the shot: the same thing the intern was issued, in the colour of the dose -----
            var shot = new GameObject("FinchShot");
            Decor(PrimitiveType.Sphere, "Visual", shot.transform, Vector3.zero, new Vector3(0.26f, 0.26f, 0.55f), dose);
            shot.AddComponent<Bullet>().radius = 0.22f;
            var shotPrefab = ArenaSceneBuilder.SavePrefab(shot, "Enemies/FinchShot").GetComponent<Bullet>();

            // ----- Dr. Finch: a person in a lab coat, 1.8 m tall at the start of the fight -----
            var finch = Creature<FinchBoss>("DrFinch", 0.5f, 1.8f, 150, 1f, 2.6f, out var model);
            Decor(PrimitiveType.Cube, "Leg L", model, new Vector3(-0.16f, 0.35f, 0f), new Vector3(0.2f, 0.7f, 0.22f), dark);
            Decor(PrimitiveType.Cube, "Leg R", model, new Vector3(0.16f, 0.35f, 0f), new Vector3(0.2f, 0.7f, 0.22f), dark);
            Decor(PrimitiveType.Cube, "Coat", model, new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.95f, 0.42f), coat);
            Decor(PrimitiveType.Cube, "Coat Tail", model, new Vector3(0f, 0.5f, -0.12f), new Vector3(0.66f, 0.35f, 0.2f), coat);
            Decor(PrimitiveType.Cube, "Tie", model, new Vector3(0f, 1.1f, 0.22f), new Vector3(0.1f, 0.45f, 0.02f), dark);
            Decor(PrimitiveType.Cube, "Arm L", model, new Vector3(-0.45f, 1f, 0.05f), new Vector3(0.18f, 0.7f, 0.2f), coat);
            Decor(PrimitiveType.Cube, "Arm R", model, new Vector3(0.45f, 1.15f, 0.3f), new Vector3(0.18f, 0.2f, 0.7f), coat);
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.42f, skin);
            Decor(PrimitiveType.Sphere, "Hair", model, new Vector3(0f, 1.74f, -0.05f), new Vector3(0.44f, 0.26f, 0.42f), hair);
            Decor(PrimitiveType.Cube, "Glasses", model, new Vector3(0f, 1.65f, 0.2f), new Vector3(0.36f, 0.09f, 0.04f), glass);
            // The flask in the raised hand: what is left of the dose.
            Decor(PrimitiveType.Cylinder, "Flask Neck", model, new Vector3(0.45f, 1.32f, 0.62f), new Vector3(0.07f, 0.08f, 0.07f), glass);
            Decor(PrimitiveType.Sphere, "Flask", model, new Vector3(0.45f, 1.2f, 0.62f), Vector3.one * 0.2f, dose);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(finch.transform, false);
            muzzle.localPosition = new Vector3(0.3f, 0.9f, 0.8f);
            finch.muzzle = muzzle;
            finch.projectilePrefab = shotPrefab;
            finch.displayName = "Dr. Finch";
            finch.subtitle = "Your boss. Technically still on the clock.";
            finch.telegraphMaterial = telegraph;
            finch.sampleReward = 60;
            finch.knockbackOnHit = 1.5f;
            var friends = new System.Collections.Generic.List<Enemy>();
            foreach (string name in new[] { "WorkerAnt", "SpitterAnt" })
            {
                Enemy friend = ants.EnemyNamed(name);
                if (friend != null) friends.Add(friend);
            }
            finch.friends = friends.ToArray();
            // Nothing about Finch is an accident of the biome: no mutations.
            Object.DestroyImmediate(finch.GetComponent<EnemyMutations>());
            var finchPrefab = (BossEnemy)SaveEnemy(finch);

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
