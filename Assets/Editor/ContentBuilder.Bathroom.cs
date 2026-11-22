using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The eleventh world: The Bathroom. White tiles underfoot, pale blue tiles up the walls, bright and echoing.
    /// Seven enemies on the seven behaviours, the Bar of Soap half way and the Rubber Duck at the end, whose fight is
    /// rings spreading across the floor to dodge through, and a bounce that comes down where it is marked. Its portal
    /// opens once the Jack-in-the-Box has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string BathroomPath = "Assets/Data/TheBathroom.asset";

        public static BiomeDefinition BuildBathroom(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var tile = ArenaSceneBuilder.Lit("BathTile", new Color(0.88f, 0.9f, 0.92f), 0.8f);
            var wallTile = ArenaSceneBuilder.Lit("BathWallTile", new Color(0.62f, 0.78f, 0.85f), 0.8f);
            var duckYellow = ArenaSceneBuilder.Lit("DuckYellow", new Color(1f, 0.85f, 0.15f), 0.6f);
            var beakOrange = ArenaSceneBuilder.Lit("BeakOrange", new Color(1f, 0.5f, 0.1f), 0.5f);
            var bristle = ArenaSceneBuilder.Lit("Bristle", new Color(0.95f, 0.95f, 1f), 0.2f);
            var brushHandle = ArenaSceneBuilder.Lit("BrushHandle", new Color(0.3f, 0.6f, 0.9f), 0.6f);
            var shampoo = ArenaSceneBuilder.Lit("Shampoo", new Color(0.55f, 0.3f, 0.75f), 0.6f);
            var bubble = ArenaSceneBuilder.Lit("Bubble", new Color(0.75f, 0.95f, 1f), 0.95f);
            var loofah = ArenaSceneBuilder.Lit("Loofah", new Color(0.9f, 0.75f, 0.55f), 0.1f);
            var freshener = ArenaSceneBuilder.Lit("Freshener", new Color(0.5f, 0.85f, 0.6f), 0.5f);
            var chrome = ArenaSceneBuilder.Lit("TapChrome", new Color(0.75f, 0.78f, 0.82f), 0.95f);
            var soap = ArenaSceneBuilder.Lit("SoapPink", new Color(1f, 0.75f, 0.8f), 0.7f);
            var suds = ArenaSceneBuilder.Unlit("Suds", new Color(0.92f, 0.97f, 1f));
            var water = ArenaSceneBuilder.Unlit("WaterJet", new Color(0.5f, 0.75f, 1f));
            var ring = ArenaSceneBuilder.Unlit("SqueakRing", new Color(1f, 0.9f, 0.3f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var squirt = new GameObject("ShampooSquirt");
            Decor(PrimitiveType.Sphere, "Visual", squirt.transform, Vector3.zero, new Vector3(0.28f, 0.28f, 0.45f), shampoo);
            squirt.AddComponent<Bullet>().radius = 0.2f;
            var squirtPrefab = ArenaSceneBuilder.SavePrefab(squirt, "Enemies/ShampooSquirt").GetComponent<Bullet>();

            var jet = new GameObject("WaterShot");
            Decor(PrimitiveType.Cube, "Visual", jet.transform, Vector3.zero, new Vector3(0.1f, 0.1f, 0.8f), water);
            jet.AddComponent<Bullet>().radius = 0.16f;
            var jetPrefab = ArenaSceneBuilder.SavePrefab(jet, "Enemies/WaterShot").GetComponent<Bullet>();

            // ----- the seven -----
            // Duckling: the Chaser. Squeaks as it runs. It is running at you.
            var duckling = Creature<ChaserEnemy>("Duckling", 0.45f, 0.9f, 5, 1f, 1.8f, out var ducklingModel);
            Decor(PrimitiveType.Sphere, "Body", ducklingModel, new Vector3(0f, 0.35f, -0.05f), new Vector3(0.6f, 0.5f, 0.75f), duckYellow);
            Decor(PrimitiveType.Sphere, "Head", ducklingModel, new Vector3(0f, 0.7f, 0.2f), Vector3.one * 0.4f, duckYellow);
            Decor(PrimitiveType.Cube, "Beak", ducklingModel, new Vector3(0f, 0.66f, 0.45f), new Vector3(0.2f, 0.06f, 0.15f), beakOrange);
            Decor(PrimitiveType.Sphere, "Eye L", ducklingModel, new Vector3(-0.1f, 0.78f, 0.35f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", ducklingModel, new Vector3(0.1f, 0.78f, 0.35f), Vector3.one * 0.07f, dark);
            duckling.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.RainBoot };
            var ducklingPrefab = SaveEnemy(duckling);

            // Toothbrush: the Charger. Bristles first, always.
            var brush = Creature<ChargerEnemy>("Toothbrush", 0.6f, 1.2f, 10, 1.5f, 2.1f, out var brushModel);
            Decor(PrimitiveType.Cube, "Handle", brushModel, new Vector3(0f, 0.5f, -0.3f), new Vector3(0.25f, 0.25f, 1.6f), brushHandle);
            Decor(PrimitiveType.Cube, "Bristles", brushModel, new Vector3(0f, 0.8f, 0.45f), new Vector3(0.35f, 0.4f, 0.5f), bristle);
            Decor(PrimitiveType.Sphere, "Eye L", brushModel, new Vector3(-0.1f, 0.6f, -0.05f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", brushModel, new Vector3(0.1f, 0.6f, -0.05f), Vector3.one * 0.1f, dark);
            brush.knockbackOnHit = 1.5f;
            brush.telegraphMaterial = telegraph;
            brush.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.NeedleTip };
            var brushPrefab = SaveEnemy(brush);

            // Shampoo bottle: the Shooter. Lather, rinse, aim, repeat.
            var bottle = Creature<ShooterEnemy>("ShampooBottle", 0.5f, 1.6f, 4, 0.9f, 2.4f, out var bottleModel);
            Decor(PrimitiveType.Cylinder, "Bottle", bottleModel, new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 0.7f, 0.45f), shampoo);
            Decor(PrimitiveType.Cylinder, "Cap", bottleModel, new Vector3(0f, 1.5f, 0f), new Vector3(0.3f, 0.12f, 0.3f), bristle);
            Decor(PrimitiveType.Sphere, "Eye L", bottleModel, new Vector3(-0.12f, 1f, 0.24f), Vector3.one * 0.1f, bristle);
            Decor(PrimitiveType.Sphere, "Eye R", bottleModel, new Vector3(0.12f, 1f, 0.24f), Vector3.one * 0.1f, bristle);
            var bottleMuzzle = new GameObject("Muzzle").transform;
            bottleMuzzle.SetParent(bottle.transform, false);
            bottleMuzzle.localPosition = new Vector3(0f, 1.3f, 0.45f);
            bottle.muzzle = bottleMuzzle;
            bottle.projectilePrefab = squirtPrefab;
            bottle.favouredDrops = new[] { ItemType.AcidGland, ItemType.BubbleWrap };
            var bottlePrefab = SaveEnemy(bottle);

            // Soap bubble: the Flyer. Drifts, wobbles, and then very suddenly does not.
            var floater = Creature<FlyerEnemy>("SoapBubble", 0.45f, 2f, 3, 0.8f, 2.9f, out var floaterModel);
            Decor(PrimitiveType.Sphere, "Bubble", floaterModel, new Vector3(0f, 0.75f, 0f), Vector3.one * 0.75f, bubble);
            var floatL = Decor(PrimitiveType.Sphere, "Wobble L", floaterModel, new Vector3(-0.3f, 0.85f, 0f), Vector3.one * 0.25f, bubble).transform;
            var floatR = Decor(PrimitiveType.Sphere, "Wobble R", floaterModel, new Vector3(0.3f, 0.85f, 0f), Vector3.one * 0.25f, bubble).transform;
            Decor(PrimitiveType.Sphere, "Eye L", floaterModel, new Vector3(-0.12f, 0.85f, 0.33f), Vector3.one * 0.08f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", floaterModel, new Vector3(0.12f, 0.85f, 0.33f), Vector3.one * 0.08f, dark);
            var floaterMotion = floater.gameObject.AddComponent<AntMotion>();
            floaterMotion.legs = new Transform[0];
            floaterMotion.wings = new[] { floatL, floatR };
            floater.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            floater.favouredDrops = new[] { ItemType.WingBuds, ItemType.BubbleWrap };
            var floaterPrefab = SaveEnemy(floater);

            // Loofah: the Lobber. Throws suds over the bath mat at where you are going.
            var scrub = Creature<LobberEnemy>("Loofah", 0.55f, 1.1f, 6, 1f, 2f, out var scrubModel);
            Decor(PrimitiveType.Sphere, "Loofah", scrubModel, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.85f, 0.9f), loofah);
            Decor(PrimitiveType.Cylinder, "Cord", scrubModel, new Vector3(0f, 1f, 0f), new Vector3(0.05f, 0.15f, 0.05f), bristle);
            Decor(PrimitiveType.Sphere, "Eye L", scrubModel, new Vector3(-0.15f, 0.6f, 0.4f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", scrubModel, new Vector3(0.15f, 0.6f, 0.4f), Vector3.one * 0.1f, dark);
            scrub.blastMaterial = telegraph;
            scrub.lobMaterial = suds;
            scrub.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.Lunchbox };
            var scrubPrefab = SaveEnemy(scrub);

            // Air freshener: the Support. Pine-fresh, and everybody near it is suddenly in a hurry.
            var spray = Creature<SupportEnemy>("AirFreshener", 0.45f, 1.4f, 4, 1f, 2.3f, out var sprayModel);
            Decor(PrimitiveType.Cylinder, "Can", sprayModel, new Vector3(0f, 0.65f, 0f), new Vector3(0.45f, 0.65f, 0.45f), freshener);
            Decor(PrimitiveType.Cube, "Nozzle", sprayModel, new Vector3(0f, 1.35f, 0.1f), new Vector3(0.15f, 0.1f, 0.15f), bristle);
            spray.pulseColor = new Color(0.5f, 0.85f, 0.6f);
            spray.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.SpareBattery };
            var sprayPrefab = SaveEnemy(spray);

            // Tap: the Sniper. Drips first, a line of it. Then the rest of the bath.
            var tap = Creature<SniperEnemy>("Tap", 0.45f, 1.3f, 5, 0.9f, 2.2f, out var tapModel);
            Decor(PrimitiveType.Cylinder, "Body", tapModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 0.6f, 0.35f), chrome);
            Decor(PrimitiveType.Cube, "Spout", tapModel, new Vector3(0f, 1.1f, 0.3f), new Vector3(0.15f, 0.15f, 0.6f), chrome);
            Decor(PrimitiveType.Cube, "Handle", tapModel, new Vector3(0f, 1.3f, 0f), new Vector3(0.6f, 0.08f, 0.08f), chrome);
            var tapMuzzle = new GameObject("Muzzle").transform;
            tapMuzzle.SetParent(tap.transform, false);
            tapMuzzle.localPosition = new Vector3(0f, 1.05f, 0.7f);
            tap.muzzle = tapMuzzle;
            tap.projectilePrefab = jetPrefab;
            tap.telegraphMaterial = telegraph;
            tap.favouredDrops = new[] { ItemType.NeedleTip, ItemType.MagnifyingGlass };
            var tapPrefab = SaveEnemy(tap);

            // Suds: grows into a duckling if it is left. Bath logic.
            var foam = Creature<LarvaEnemy>("Suds", 0.35f, 0.6f, 2, 0.6f, 1.2f, out var foamModel);
            Decor(PrimitiveType.Sphere, "Foam", foamModel, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.35f, 0.5f), suds);
            Decor(PrimitiveType.Sphere, "Eye L", foamModel, new Vector3(-0.08f, 0.32f, 0.2f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", foamModel, new Vector3(0.08f, 0.32f, 0.2f), Vector3.one * 0.07f, dark);
            foam.growsInto = ducklingPrefab;
            foam.dropsLoot = false;
            foam.knockbackOnHit = 1f;
            Object.DestroyImmediate(foam.GetComponent<EnemyMutations>());
            var foamPrefab = (LarvaEnemy)SaveEnemy(foam);

            // ----- bosses -----
            var bar = Creature<BathBoss>("TheBarOfSoap", 1f, 1.2f, 160, 1f, 2.6f, out var barModel);
            Decor(PrimitiveType.Cube, "Bar", barModel, new Vector3(0f, 0.55f, 0f), new Vector3(1.8f, 1f, 2.6f), soap);
            Decor(PrimitiveType.Cube, "Stamp", barModel, new Vector3(0f, 1.06f, 0f), new Vector3(1f, 0.04f, 1.5f), bristle);
            Decor(PrimitiveType.Sphere, "Eye L", barModel, new Vector3(-0.35f, 0.75f, 1.3f), Vector3.one * 0.2f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", barModel, new Vector3(0.35f, 0.75f, 1.3f), Vector3.one * 0.2f, dark);
            bar.displayName = "The Bar of Soap";
            bar.subtitle = "Slippery. Very slippery. Mind where it has been.";
            bar.canSqueak = false;
            bar.canSlide = true;
            bar.sudsMaterial = suds;
            bar.telegraphMaterial = telegraph;
            bar.friends = new[] { ducklingPrefab };
            bar.sampleReward = 12;
            bar.knockbackOnHit = 0.5f;
            Object.DestroyImmediate(bar.GetComponent<EnemyMutations>());
            var barPrefab = (BossEnemy)SaveEnemy(bar);

            var duck = Creature<BathBoss>("TheRubberDuck", 1.3f, 3f, 440, 1f, 4.6f, out var duckModel);
            Decor(PrimitiveType.Sphere, "Body", duckModel, new Vector3(0f, 1.1f, -0.2f), new Vector3(2.4f, 1.8f, 3f), duckYellow);
            Decor(PrimitiveType.Sphere, "Head", duckModel, new Vector3(0f, 2.4f, 0.7f), Vector3.one * 1.5f, duckYellow);
            Decor(PrimitiveType.Cube, "Beak", duckModel, new Vector3(0f, 2.25f, 1.55f), new Vector3(0.8f, 0.25f, 0.6f), beakOrange);
            Decor(PrimitiveType.Sphere, "Eye L", duckModel, new Vector3(-0.4f, 2.65f, 1.25f), Vector3.one * 0.25f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", duckModel, new Vector3(0.4f, 2.65f, 1.25f), Vector3.one * 0.25f, dark);
            Decor(PrimitiveType.Cube, "Tail", duckModel, new Vector3(0f, 1.8f, -1.7f), new Vector3(0.8f, 0.6f, 0.3f), duckYellow).transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            duck.displayName = "The Rubber Duck";
            duck.subtitle = "Squeaks when squeezed. Nobody has squeezed it. It squeaks anyway.";
            duck.ringMaterial = ring;
            duck.telegraphMaterial = telegraph;
            duck.friends = new[] { ducklingPrefab, floaterPrefab };
            duck.sampleReward = 30;
            duck.knockbackOnHit = 0.3f;
            Object.DestroyImmediate(duck.GetComponent<EnemyMutations>());
            var duckPrefab = (BossEnemy)SaveEnemy(duck);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BathroomPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, BathroomPath);
            }

            biome.displayName = "The Bathroom";
            biome.id = "bathroom";
            biome.mutationTheme = "speed, shells, things that slip";
            biome.isBottom = false;
            biome.floorMaterial = tile;
            biome.wallMaterial = wallTile;
            biome.doorMaterial = duckYellow;
            biome.wallHeight = 6f;
            biome.ceiling = true;
            biome.ceilingMaterial = tile;
            biome.skyColor = new Color(0.6f, 0.68f, 0.72f);
            biome.ambientColor = new Color(0.68f, 0.74f, 0.78f);
            biome.sunIntensity = 0.95f;
            biome.fog = true;
            biome.fogColor = new Color(0.7f, 0.76f, 0.8f);
            biome.fogStart = 22f;
            biome.fogEnd = 75f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = chrome;               // taps and fittings
            biome.crumbMaterial = suds;                 // foam
            biome.sugarMaterial = soap;
            biome.rootMaterial = chrome;                // pipes
            biome.podMaterial = bubble;                 // bubbles
            biome.podHatchling = foamPrefab;
            biome.dewMaterial = water;                  // drips
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = ring;                  // night lights
            biome.glowStemMaterial = chrome;
            biome.glowLightRange = 10f;
            biome.glowLightIntensity = 1.6f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = ducklingPrefab;
            biome.miniBoss = barPrefab;
            biome.boss = duckPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = ducklingPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = bottlePrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = brushPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = floaterPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = scrubPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = sprayPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = tapPrefab, weight = 1, minFloor = 5 },
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
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("BathroomPortal", ring, black, chrome), "Props/BathroomPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
