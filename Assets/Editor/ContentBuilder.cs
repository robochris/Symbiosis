using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the placeholder game content both scenes share: the bullet, the four ants, the eight mutations,
    /// the special-room props and the Ant Colony biome data file that ties them together.
    /// </summary>
    public static class ContentBuilder
    {
        const string BiomePath = "Assets/Data/AntColony.asset";

        public static BiomeDefinition Build(out Bullet playerBullet)
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Materials");
            ArenaSceneBuilder.EnsureFolder("Assets", "Prefabs");
            ArenaSceneBuilder.EnsureFolder("Assets/Prefabs", "Props");
            ArenaSceneBuilder.EnsureFolder("Assets/Prefabs", "Enemies");
            ArenaSceneBuilder.EnsureFolder("Assets", "Data");
            ArenaSceneBuilder.EnsureFolder("Assets/Data", "Mutations");

            var antBody = ArenaSceneBuilder.Lit("AntBody", new Color(0.55f, 0.16f, 0.10f), 0.4f);
            var antDark = ArenaSceneBuilder.Lit("AntDark", new Color(0.16f, 0.06f, 0.05f), 0.4f);
            var soldierBody = ArenaSceneBuilder.Lit("SoldierBody", new Color(0.3f, 0.08f, 0.06f), 0.5f);
            var spitterBody = ArenaSceneBuilder.Lit("SpitterBody", new Color(0.45f, 0.5f, 0.12f), 0.4f);
            var wingedBody = ArenaSceneBuilder.Lit("WingedBody", new Color(0.7f, 0.4f, 0.12f), 0.4f);
            var wing = ArenaSceneBuilder.Lit("Wing", new Color(0.85f, 0.9f, 0.95f), 0.8f);
            var acid = ArenaSceneBuilder.Unlit("AcidSpit", new Color(0.7f, 1f, 0.2f));
            var bulletGlow = ArenaSceneBuilder.Unlit("BulletGlow", new Color(0.6f, 1f, 0.95f));

            // ----- shots -----
            var bullet = new GameObject("Bullet");
            Decor(PrimitiveType.Sphere, "Visual", bullet.transform, Vector3.zero, new Vector3(0.22f, 0.22f, 0.5f), bulletGlow);
            bullet.AddComponent<Bullet>();
            playerBullet = ArenaSceneBuilder.SavePrefab(bullet, "Bullet").GetComponent<Bullet>();

            var spit = new GameObject("AcidSpit");
            Decor(PrimitiveType.Sphere, "Visual", spit.transform, Vector3.zero, Vector3.one * 0.5f, acid);
            spit.AddComponent<Bullet>().radius = 0.25f;
            var spitPrefab = ArenaSceneBuilder.SavePrefab(spit, "Enemies/AcidSpit").GetComponent<Bullet>();

            // ----- the four ants, one per base behaviour -----
            var worker = Ant<ChaserEnemy>("WorkerAnt", 1f, 5, antBody, antDark, false, out _);
            var workerPrefab = SaveEnemy(worker);

            var soldier = Ant<ChargerEnemy>("SoldierAnt", 1.45f, 10, soldierBody, antDark, false, out _);
            soldier.knockbackOnHit = 2f;
            var soldierPrefab = SaveEnemy(soldier);

            var spitter = Ant<ShooterEnemy>("SpitterAnt", 0.9f, 4, spitterBody, antDark, false, out _);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(spitter.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.7f, 1f);
            spitter.muzzle = muzzle;
            spitter.projectilePrefab = spitPrefab;
            var spitterPrefab = SaveEnemy(spitter);

            var winged = Ant<FlyerEnemy>("WingedAnt", 0.8f, 3, wingedBody, antDark, true, out var wingedModel);
            Decor(PrimitiveType.Cube, "Wing L", wingedModel, new Vector3(-0.7f, 0.95f, -0.15f), new Vector3(1.2f, 0.04f, 0.55f), wing).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Decor(PrimitiveType.Cube, "Wing R", wingedModel, new Vector3(0.7f, 0.95f, -0.15f), new Vector3(1.2f, 0.04f, 0.55f), wing).transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var wingedController = winged.GetComponent<CharacterController>();
            wingedController.height = 2f;
            wingedController.center = new Vector3(0f, 1f, 0f);
            winged.GetComponent<EnemyMutations>().badgeHeight = 2.9f;
            var wingedPrefab = SaveEnemy(winged);

            // ----- biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, BiomePath);
            }

            biome.displayName = "Backyard Ant Colony";
            biome.floorMaterial = ArenaSceneBuilder.Lit("Soil", new Color(0.36f, 0.25f, 0.16f), 0.05f);
            biome.wallMaterial = ArenaSceneBuilder.Lit("Wall", new Color(0.23f, 0.15f, 0.10f), 0.05f);
            biome.doorMaterial = ArenaSceneBuilder.Lit("DoorLocked", new Color(0.85f, 0.3f, 0.15f), 0.3f);
            biome.swarmEnemy = workerPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = workerPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = spitterPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = soldierPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = wingedPrefab, weight = 2, minFloor = 4 },
            };
            biome.mutations = new[]
            {
                Mutation(MutationType.Fast, "Fast", 1, "Moves and attacks quicker.", new Color(0.3f, 0.6f, 1f)),
                Mutation(MutationType.Armored, "Armored", 2, "Takes half damage.", new Color(0.6f, 0.62f, 0.66f)),
                Mutation(MutationType.Regenerates, "Regenerates", 2, "Heals if not hit for 2 seconds.", new Color(0.3f, 0.9f, 0.4f)),
                Mutation(MutationType.ExplodesOnDeath, "Explodes on death", 2, "Bursts after a short fuse when killed.", new Color(1f, 0.2f, 0.15f)),
                Mutation(MutationType.AcidBlood, "Acid blood", 1, "Leaves an acid pool on death.", new Color(0.7f, 1f, 0.2f)),
                Mutation(MutationType.SwarmCall, "Swarm call", 3, "Summons 3 workers once, at half health.", new Color(1f, 0.85f, 0.2f)),
                Mutation(MutationType.HardenedShell, "Hardened shell", 2, "Blocks shots from the front.", new Color(0.85f, 0.9f, 1f)),
                Mutation(MutationType.PheromoneTrail, "Pheromone trail", 2, "Ants crossing its trail get faster.", new Color(1f, 0.45f, 0.75f)),
            };

            BuildProps(biome);
            EditorUtility.SetDirty(biome);
            return biome;
        }

        // ---------- ants ----------

        static T Ant<T>(string name, float size, int health, Material body, Material dark, bool flying, out Transform model) where T : Enemy
        {
            var root = new GameObject(name);

            var controller = root.AddComponent<CharacterController>();
            controller.radius = 0.55f * size;
            controller.height = 1.2f * size;
            controller.center = new Vector3(0f, 0.6f * size, 0f);

            var hp = root.AddComponent<Health>();
            hp.team = Team.Enemy;
            hp.maxHealth = health;

            // "Visual" is resized by code (telegraphs, death pop); "Model" inside it holds the ant's own size.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            model = new GameObject("Model").transform;
            model.SetParent(visual, false);
            model.localScale = Vector3.one * size;

            Decor(PrimitiveType.Sphere, "Abdomen", model, new Vector3(0f, 0.6f, -0.65f), new Vector3(0.85f, 0.75f, 1.05f), body);
            Decor(PrimitiveType.Sphere, "Thorax", model, new Vector3(0f, 0.55f, 0f), new Vector3(0.55f, 0.5f, 0.65f), dark);
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 0.65f, 0.5f), new Vector3(0.6f, 0.5f, 0.55f), body);
            Decor(PrimitiveType.Cube, "Mandible L", model, new Vector3(-0.14f, 0.55f, 0.85f), new Vector3(0.08f, 0.08f, 0.3f), dark).transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            Decor(PrimitiveType.Cube, "Mandible R", model, new Vector3(0.14f, 0.55f, 0.85f), new Vector3(0.08f, 0.08f, 0.3f), dark).transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
            for (int i = 0; i < 3; i++)
            {
                float z = -0.25f + i * 0.25f;
                float droop = flying ? 60f : 35f; // legs hang down in flight
                Decor(PrimitiveType.Cube, "Leg L" + i, model, new Vector3(-0.5f, 0.3f, z), new Vector3(0.7f, 0.07f, 0.07f), dark).transform.localRotation = Quaternion.Euler(0f, 0f, droop);
                Decor(PrimitiveType.Cube, "Leg R" + i, model, new Vector3(0.5f, 0.3f, z), new Vector3(0.7f, 0.07f, 0.07f), dark).transform.localRotation = Quaternion.Euler(0f, 0f, -droop);
            }

            var enemy = root.AddComponent<T>();
            enemy.visual = visual;
            root.AddComponent<HitFlash>();
            var mutations = root.AddComponent<EnemyMutations>();
            mutations.badgeHeight = 1.3f * size + 0.9f;
            mutations.shellOffset = 1.1f * size;
            return enemy;
        }

        static Enemy SaveEnemy(Enemy enemy) =>
            ArenaSceneBuilder.SavePrefab(enemy.gameObject, "Enemies/" + enemy.name).GetComponent<Enemy>();

        // ---------- mutations ----------

        static MutationDefinition Mutation(MutationType type, string name, int cost, string effect, Color color)
        {
            string path = "Assets/Data/Mutations/" + type + ".asset";
            var mutation = AssetDatabase.LoadAssetAtPath<MutationDefinition>(path);
            if (mutation == null)
            {
                mutation = ScriptableObject.CreateInstance<MutationDefinition>();
                AssetDatabase.CreateAsset(mutation, path);
            }
            mutation.type = type;
            mutation.displayName = name;
            mutation.cost = cost;
            mutation.effect = effect;
            mutation.color = color;
            mutation.effectMaterial = ArenaSceneBuilder.Unlit("Mutation" + type, color);
            EditorUtility.SetDirty(mutation);
            return mutation;
        }

        // ---------- special room props ----------

        // Each prop fits inside the 3×3 tiles (6 m square) that every room keeps clear around its centre.
        static void BuildProps(BiomeDefinition biome)
        {
            var wall = biome.wallMaterial;
            var root = ArenaSceneBuilder.Lit("Root", new Color(0.3f, 0.2f, 0.12f), 0.1f);
            var gold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));
            var pad = ArenaSceneBuilder.Unlit("StartPad", new Color(0.2f, 0.5f, 0.55f));
            var beetle = ArenaSceneBuilder.Lit("Beetle", new Color(0.1f, 0.25f, 0.2f), 0.7f);

            var startProp = new GameObject("StartPad");
            Decor(PrimitiveType.Cylinder, "Pad", startProp.transform, new Vector3(0f, 0.01f, 0f), new Vector3(4f, 0.01f, 4f), pad);
            biome.startProp = ArenaSceneBuilder.SavePrefab(startProp, "Props/StartPad");

            var exit = new GameObject("ExitHole");
            Decor(PrimitiveType.Cylinder, "Rim", exit.transform, new Vector3(0f, 0.01f, 0f), new Vector3(3.6f, 0.01f, 3.6f), gold);
            Decor(PrimitiveType.Cylinder, "Hole", exit.transform, new Vector3(0f, 0.02f, 0f), new Vector3(3f, 0.02f, 3f), black);
            biome.exitProp = ArenaSceneBuilder.SavePrefab(exit, "Props/ExitHole");

            var treasure = new GameObject("TreasurePedestal");
            Solid(PrimitiveType.Cylinder, "Pedestal", treasure.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.5f, 1.4f), wall);
            Decor(PrimitiveType.Sphere, "Item Placeholder", treasure.transform, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.8f, gold);
            biome.treasureProp = ArenaSceneBuilder.SavePrefab(treasure, "Props/TreasurePedestal");

            var shop = new GameObject("ShopStall");
            Solid(PrimitiveType.Cube, "Counter", shop.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 1.2f, 1f), root);
            Solid(PrimitiveType.Sphere, "Beetle Merchant", shop.transform, new Vector3(0f, 1f, 1.7f), new Vector3(1.6f, 1.6f, 1.6f), beetle);
            Decor(PrimitiveType.Sphere, "Ware 1", shop.transform, new Vector3(-0.5f, 1.45f, 0f), Vector3.one * 0.45f, gold);
            Decor(PrimitiveType.Sphere, "Ware 2", shop.transform, new Vector3(0.5f, 1.45f, 0f), Vector3.one * 0.45f, gold);
            biome.shopProp = ArenaSceneBuilder.SavePrefab(shop, "Props/ShopStall");
        }

        static GameObject Solid(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, true);

        static GameObject Decor(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, false);
    }
}
