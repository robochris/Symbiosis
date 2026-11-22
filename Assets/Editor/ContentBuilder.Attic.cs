using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The twelfth world: The Attic. Bare boards underfoot, rafters and dust, one bulb somewhere. Seven enemies on the
    /// seven behaviours, the Bat half way and the Spider at the end, whose fight is web that slows, a drop from out of
    /// sight onto a marked circle, and a bite when too close. Its portal opens once the Rubber Duck has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string AtticPath = "Assets/Data/TheAttic.asset";

        public static BiomeDefinition BuildAttic(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var boards = ArenaSceneBuilder.Lit("AtticBoards", new Color(0.4f, 0.3f, 0.2f), 0.2f);
            var rafters = ArenaSceneBuilder.Lit("AtticRafters", new Color(0.3f, 0.22f, 0.15f), 0.15f);
            var dust = ArenaSceneBuilder.Lit("AtticDust", new Color(0.6f, 0.58f, 0.55f), 0.05f);
            var spiderBody = ArenaSceneBuilder.Lit("SpiderBody", new Color(0.12f, 0.1f, 0.1f), 0.5f);
            var spiderMark = ArenaSceneBuilder.Unlit("SpiderMark", new Color(0.9f, 0.2f, 0.2f));
            var leather = ArenaSceneBuilder.Lit("OldLeather", new Color(0.35f, 0.22f, 0.12f), 0.3f);
            var brass = ArenaSceneBuilder.Lit("AtticBrass", new Color(0.7f, 0.55f, 0.25f), 0.8f);
            var batWing = ArenaSceneBuilder.Lit("BatWing", new Color(0.2f, 0.15f, 0.18f), 0.2f);
            var mothball = ArenaSceneBuilder.Lit("Mothball", new Color(0.95f, 0.95f, 0.92f), 0.3f);
            var glass = ArenaSceneBuilder.Lit("JarGlass", new Color(0.6f, 0.8f, 0.85f), 0.95f);
            var wood = ArenaSceneBuilder.Lit("Slingshot", new Color(0.55f, 0.35f, 0.18f), 0.2f);
            var web = ArenaSceneBuilder.Unlit("Web", new Color(0.88f, 0.9f, 0.92f));
            var bulb = ArenaSceneBuilder.Unlit("Bulb", new Color(1f, 0.9f, 0.6f));
            var eyeRed = ArenaSceneBuilder.Unlit("EyeRed", new Color(1f, 0.25f, 0.2f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var pebble = new GameObject("SlingStone");
            Decor(PrimitiveType.Sphere, "Visual", pebble.transform, Vector3.zero, Vector3.one * 0.28f, dust);
            pebble.AddComponent<Bullet>().radius = 0.2f;
            var pebblePrefab = ArenaSceneBuilder.SavePrefab(pebble, "Enemies/SlingStone").GetComponent<Bullet>();

            var beam = new GameObject("StarShot");
            Decor(PrimitiveType.Cube, "Visual", beam.transform, Vector3.zero, new Vector3(0.1f, 0.1f, 0.7f), bulb);
            beam.AddComponent<Bullet>().radius = 0.16f;
            var beamPrefab = ArenaSceneBuilder.SavePrefab(beam, "Enemies/StarShot").GetComponent<Bullet>();

            // ----- the seven -----
            var spiderling = Creature<ChaserEnemy>("Spiderling", 0.45f, 0.7f, 5, 1f, 1.6f, out var lingModel);
            Decor(PrimitiveType.Sphere, "Body", lingModel, new Vector3(0f, 0.35f, -0.1f), new Vector3(0.5f, 0.4f, 0.6f), spiderBody);
            for (int i = 0; i < 4; i++)
            {
                Decor(PrimitiveType.Cube, "Leg L" + i, lingModel, new Vector3(-0.35f, 0.2f, 0.25f - i * 0.15f), new Vector3(0.4f, 0.04f, 0.04f), spiderBody).transform.localRotation = Quaternion.Euler(0f, -20f + i * 13f, -25f);
                Decor(PrimitiveType.Cube, "Leg R" + i, lingModel, new Vector3(0.35f, 0.2f, 0.25f - i * 0.15f), new Vector3(0.4f, 0.04f, 0.04f), spiderBody).transform.localRotation = Quaternion.Euler(0f, 20f - i * 13f, 25f);
            }
            Decor(PrimitiveType.Sphere, "Eye L", lingModel, new Vector3(-0.08f, 0.42f, 0.2f), Vector3.one * 0.07f, eyeRed);
            Decor(PrimitiveType.Sphere, "Eye R", lingModel, new Vector3(0.08f, 0.42f, 0.2f), Vector3.one * 0.07f, eyeRed);
            spiderling.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.NeedleTip };
            var spiderlingPrefab = SaveEnemy(spiderling);

            var boot = Creature<ChargerEnemy>("OldBoot", 0.75f, 1.4f, 10, 1.5f, 2.2f, out var bootModel);
            Decor(PrimitiveType.Cube, "Leg", bootModel, new Vector3(0f, 0.9f, -0.2f), new Vector3(0.6f, 1f, 0.6f), leather);
            Decor(PrimitiveType.Cube, "Foot", bootModel, new Vector3(0f, 0.25f, 0.2f), new Vector3(0.65f, 0.5f, 1.3f), leather);
            Decor(PrimitiveType.Cube, "Tongue", bootModel, new Vector3(0f, 1.35f, 0.08f), new Vector3(0.35f, 0.25f, 0.05f), leather);
            Decor(PrimitiveType.Sphere, "Eye L", bootModel, new Vector3(-0.15f, 1.1f, 0.12f), Vector3.one * 0.12f, mothball);
            Decor(PrimitiveType.Sphere, "Eye R", bootModel, new Vector3(0.15f, 1.1f, 0.12f), Vector3.one * 0.12f, mothball);
            boot.knockbackOnHit = 1.5f;
            boot.telegraphMaterial = telegraph;
            boot.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.KiteString };
            var bootPrefab = SaveEnemy(boot);

            var sling = Creature<ShooterEnemy>("Slingshot", 0.5f, 1.4f, 4, 0.9f, 2.2f, out var slingModel);
            Decor(PrimitiveType.Cube, "Handle", slingModel, new Vector3(0f, 0.45f, 0f), new Vector3(0.15f, 0.9f, 0.15f), wood);
            Decor(PrimitiveType.Cube, "Fork L", slingModel, new Vector3(-0.2f, 1.05f, 0f), new Vector3(0.1f, 0.45f, 0.1f), wood).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            Decor(PrimitiveType.Cube, "Fork R", slingModel, new Vector3(0.2f, 1.05f, 0f), new Vector3(0.1f, 0.45f, 0.1f), wood).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            Decor(PrimitiveType.Sphere, "Eye L", slingModel, new Vector3(-0.06f, 0.75f, 0.09f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", slingModel, new Vector3(0.06f, 0.75f, 0.09f), Vector3.one * 0.07f, dark);
            var slingMuzzle = new GameObject("Muzzle").transform;
            slingMuzzle.SetParent(sling.transform, false);
            slingMuzzle.localPosition = new Vector3(0f, 1.1f, 0.4f);
            sling.muzzle = slingMuzzle;
            sling.projectilePrefab = pebblePrefab;
            sling.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.CompoundEye };
            var slingPrefab = SaveEnemy(sling);

            var bat = Creature<FlyerEnemy>("Bat", 0.45f, 2f, 3, 0.8f, 2.9f, out var batModel);
            Decor(PrimitiveType.Sphere, "Body", batModel, new Vector3(0f, 0.75f, 0f), new Vector3(0.35f, 0.35f, 0.45f), batWing);
            var batL = Decor(PrimitiveType.Cube, "Wing L", batModel, new Vector3(-0.45f, 0.8f, 0f), new Vector3(0.7f, 0.03f, 0.35f), batWing).transform;
            var batR = Decor(PrimitiveType.Cube, "Wing R", batModel, new Vector3(0.45f, 0.8f, 0f), new Vector3(0.7f, 0.03f, 0.35f), batWing).transform;
            Decor(PrimitiveType.Sphere, "Eye L", batModel, new Vector3(-0.08f, 0.85f, 0.2f), Vector3.one * 0.06f, eyeRed);
            Decor(PrimitiveType.Sphere, "Eye R", batModel, new Vector3(0.08f, 0.85f, 0.2f), Vector3.one * 0.06f, eyeRed);
            var batMotion = bat.gameObject.AddComponent<AntMotion>();
            batMotion.legs = new Transform[0];
            batMotion.wings = new[] { batL, batR };
            bat.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            bat.favouredDrops = new[] { ItemType.WingBuds, ItemType.ColdCoffee };
            var batPrefab = SaveEnemy(bat);

            var jar = Creature<LobberEnemy>("MothballJar", 0.5f, 1.2f, 6, 1f, 2.1f, out var jarModel);
            Decor(PrimitiveType.Cylinder, "Jar", jarModel, new Vector3(0f, 0.5f, 0f), new Vector3(0.65f, 0.5f, 0.65f), glass);
            Decor(PrimitiveType.Cylinder, "Lid", jarModel, new Vector3(0f, 1.02f, 0f), new Vector3(0.7f, 0.05f, 0.7f), brass);
            Decor(PrimitiveType.Sphere, "Ball", jarModel, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.35f, mothball);
            jar.blastMaterial = telegraph;
            jar.lobMaterial = mothball;
            jar.favouredDrops = new[] { ItemType.AcidGland, ItemType.Lunchbox };
            var jarPrefab = SaveEnemy(jar);

            var gram = Creature<SupportEnemy>("Gramophone", 0.55f, 1.6f, 4, 1f, 2.5f, out var gramModel);
            Decor(PrimitiveType.Cube, "Box", gramModel, new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.5f, 0.8f), rafters);
            Decor(PrimitiveType.Cylinder, "Horn", gramModel, new Vector3(0f, 1.05f, 0.25f), new Vector3(0.6f, 0.35f, 0.6f), brass).transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            gram.pulseColor = new Color(0.9f, 0.75f, 0.4f);
            gram.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.LostMarble };
            var gramPrefab = SaveEnemy(gram);

            var scope = Creature<SniperEnemy>("Telescope", 0.45f, 1.4f, 5, 0.9f, 2.2f, out var scopeModel);
            Decor(PrimitiveType.Cylinder, "Tube", scopeModel, new Vector3(0f, 1f, 0.1f), new Vector3(0.25f, 0.55f, 0.25f), brass).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Decor(PrimitiveType.Cube, "Leg A", scopeModel, new Vector3(-0.15f, 0.45f, 0f), new Vector3(0.05f, 0.9f, 0.05f), wood);
            Decor(PrimitiveType.Cube, "Leg B", scopeModel, new Vector3(0.15f, 0.45f, 0f), new Vector3(0.05f, 0.9f, 0.05f), wood);
            var scopeMuzzle = new GameObject("Muzzle").transform;
            scopeMuzzle.SetParent(scope.transform, false);
            scopeMuzzle.localPosition = new Vector3(0f, 1f, 0.7f);
            scope.muzzle = scopeMuzzle;
            scope.projectilePrefab = beamPrefab;
            scope.telegraphMaterial = telegraph;
            scope.favouredDrops = new[] { ItemType.NeedleTip, ItemType.MagnifyingGlass };
            var scopePrefab = SaveEnemy(scope);

            var egg = Creature<LarvaEnemy>("SpiderEgg", 0.35f, 0.6f, 2, 0.6f, 1.2f, out var eggModel);
            Decor(PrimitiveType.Sphere, "Sac", eggModel, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.45f, web);
            egg.growsInto = spiderlingPrefab;
            egg.dropsLoot = false;
            egg.knockbackOnHit = 1f;
            Object.DestroyImmediate(egg.GetComponent<EnemyMutations>());
            var eggPrefab = (LarvaEnemy)SaveEnemy(egg);

            // ----- bosses -----
            var bigBat = Creature<FishBoss>("TheBat", 0.9f, 2.4f, 140, 1f, 3.4f, out var bigBatModel);
            bigBat.GetComponent<CharacterController>().center = new Vector3(0f, 1.2f, 0f);
            Decor(PrimitiveType.Sphere, "Body", bigBatModel, new Vector3(0f, 1.5f, 0f), new Vector3(0.9f, 0.9f, 1.1f), batWing);
            Decor(PrimitiveType.Cube, "Wing L", bigBatModel, new Vector3(-1.2f, 1.6f, 0f), new Vector3(1.8f, 0.05f, 0.9f), batWing);
            Decor(PrimitiveType.Cube, "Wing R", bigBatModel, new Vector3(1.2f, 1.6f, 0f), new Vector3(1.8f, 0.05f, 0.9f), batWing);
            Decor(PrimitiveType.Sphere, "Ear L", bigBatModel, new Vector3(-0.25f, 2.05f, 0.2f), new Vector3(0.18f, 0.35f, 0.1f), batWing);
            Decor(PrimitiveType.Sphere, "Ear R", bigBatModel, new Vector3(0.25f, 2.05f, 0.2f), new Vector3(0.18f, 0.35f, 0.1f), batWing);
            Decor(PrimitiveType.Sphere, "Eye L", bigBatModel, new Vector3(-0.15f, 1.65f, 0.5f), Vector3.one * 0.12f, eyeRed);
            Decor(PrimitiveType.Sphere, "Eye R", bigBatModel, new Vector3(0.15f, 1.65f, 0.5f), Vector3.one * 0.12f, eyeRed);
            bigBat.displayName = "The Bat";
            bigBat.subtitle = "Hangs about all day. Tonight it has plans, and you are in them.";
            bigBat.canDash = true;
            bigBat.bubblePrefab = null;
            bigBat.restBetween = 1.1f;
            bigBat.telegraphMaterial = telegraph;
            bigBat.friends = new[] { batPrefab };
            bigBat.sampleReward = 12;
            bigBat.knockbackOnHit = 0.5f;
            Object.DestroyImmediate(bigBat.GetComponent<EnemyMutations>());
            var bigBatPrefab = (BossEnemy)SaveEnemy(bigBat);

            var spider = Creature<SpiderBoss>("TheSpider", 1.3f, 2f, 460, 1f, 3.6f, out var spiderModel);
            Decor(PrimitiveType.Sphere, "Abdomen", spiderModel, new Vector3(0f, 1.2f, -0.9f), new Vector3(2f, 1.7f, 2.2f), spiderBody);
            Decor(PrimitiveType.Sphere, "Head", spiderModel, new Vector3(0f, 1.05f, 0.6f), new Vector3(1.2f, 0.9f, 1.1f), spiderBody);
            Decor(PrimitiveType.Cube, "Mark", spiderModel, new Vector3(0f, 2.05f, -0.9f), new Vector3(0.5f, 0.05f, 0.7f), spiderMark);
            for (int i = 0; i < 4; i++)
            {
                Decor(PrimitiveType.Cube, "Leg L" + i, spiderModel, new Vector3(-1.2f, 0.7f, 0.7f - i * 0.45f), new Vector3(1.8f, 0.12f, 0.12f), spiderBody).transform.localRotation = Quaternion.Euler(0f, -25f + i * 17f, -35f);
                Decor(PrimitiveType.Cube, "Leg R" + i, spiderModel, new Vector3(1.2f, 0.7f, 0.7f - i * 0.45f), new Vector3(1.8f, 0.12f, 0.12f), spiderBody).transform.localRotation = Quaternion.Euler(0f, 25f - i * 17f, 35f);
            }
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Sphere, "Eye " + i, spiderModel, new Vector3(-0.3f + i * 0.2f, 1.3f + (i % 2) * 0.12f, 1.1f), Vector3.one * 0.14f, eyeRed);
            spider.displayName = "The Spider";
            spider.subtitle = "Has been up here longer than the house. Considers it hers.";
            spider.webMaterial = web;
            spider.telegraphMaterial = telegraph;
            spider.friends = new[] { spiderlingPrefab, jarPrefab };
            spider.sampleReward = 30;
            spider.knockbackOnHit = 0.3f;
            Object.DestroyImmediate(spider.GetComponent<EnemyMutations>());
            var spiderPrefab = (BossEnemy)SaveEnemy(spider);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(AtticPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, AtticPath);
            }

            biome.displayName = "The Attic";
            biome.id = "attic";
            biome.mutationTheme = "shells, webs, things that wait";
            biome.isBottom = false;
            biome.floorMaterial = boards;
            biome.wallMaterial = rafters;
            biome.doorMaterial = dust;
            biome.wallHeight = 5f;
            biome.ceiling = true;
            biome.ceilingMaterial = rafters;
            biome.skyColor = new Color(0.06f, 0.05f, 0.05f);
            biome.ambientColor = new Color(0.45f, 0.4f, 0.36f);
            biome.sunIntensity = 0.6f;
            biome.fog = true;
            biome.fogColor = new Color(0.12f, 0.1f, 0.08f);
            biome.fogStart = 18f;
            biome.fogEnd = 65f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = leather;             // old trunks
            biome.crumbMaterial = dust;
            biome.sugarMaterial = mothball;
            biome.rootMaterial = rafters;
            biome.podMaterial = web;                   // egg sacs
            biome.podHatchling = eggPrefab;
            biome.dewMaterial = web;
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = bulb;                 // bare bulbs
            biome.glowStemMaterial = dark;
            biome.glowLightRange = 11f;
            biome.glowLightIntensity = 2.2f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = spiderlingPrefab;
            biome.miniBoss = bigBatPrefab;
            biome.boss = spiderPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = spiderlingPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = slingPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = bootPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = batPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = jarPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = gramPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = scopePrefab, weight = 1, minFloor = 5 },
                new EnemySpawnEntry { prefab = ants.EnemyNamed("Mimic"), weight = 1, minFloor = 4 },
            };
            var mutations = new System.Collections.Generic.List<MutationDefinition>
            {
                ants.Mutation(MutationType.Fast), ants.Mutation(MutationType.Armored), ants.Mutation(MutationType.Regenerates),
                ants.Mutation(MutationType.SwarmCall), ants.Mutation(MutationType.HardenedShell), ants.Mutation(MutationType.AcidBlood),
            };
            mutations.RemoveAll(mutation => mutation == null);
            biome.mutations = mutations.ToArray();

            biome.startProp = ants.startProp;
            biome.exitProp = ants.exitProp;
            biome.treasureProp = ants.treasureProp;
            biome.shopProp = ants.shopProp;
            biome.extractPortalProp = ants.extractPortalProp;
            biome.deeperPortalProp = ants.deeperPortalProp;
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("AtticPortal", bulb, black, rafters), "Props/AtticPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
