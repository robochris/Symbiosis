using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The fifteenth world: The Kitchen Sink. Steel underfoot, tiles for walls, a dripping tap somewhere above. It has
    /// the bookshelf's ledges (upturned mugs and stacked plates) and floods: during a fight the water rises across the
    /// room every so often, and only the ledges stay dry. The Sponge half way, the Tap at the end. Its portal opens
    /// once the Encyclopedia has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string SinkPath = "Assets/Data/TheSink.asset";

        public static BiomeDefinition BuildSink(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var steel = ArenaSceneBuilder.Lit("SinkSteel", new Color(0.42f, 0.45f, 0.5f), 0.85f);
            var tiles = ArenaSceneBuilder.Lit("SinkTiles", new Color(0.62f, 0.72f, 0.76f), 0.6f);
            var mug = ArenaSceneBuilder.Lit("Mug", new Color(0.85f, 0.45f, 0.3f), 0.5f);
            var slug = ArenaSceneBuilder.Lit("SinkSlug", new Color(0.45f, 0.4f, 0.3f), 0.8f);
            var brush = ArenaSceneBuilder.Lit("BrushBlue", new Color(0.2f, 0.45f, 0.85f), 0.4f);
            var bristle = ArenaSceneBuilder.Lit("Bristle", new Color(0.95f, 0.9f, 0.7f), 0.2f);
            var bottle = ArenaSceneBuilder.Lit("SquirtBottle", new Color(0.4f, 0.85f, 0.4f), 0.7f);
            var fly = ArenaSceneBuilder.Lit("DrainFly", new Color(0.3f, 0.28f, 0.25f), 0.4f);
            var sponge = ArenaSceneBuilder.Lit("Sponge", new Color(1f, 0.85f, 0.3f), 0.05f);
            var scourer = ArenaSceneBuilder.Lit("Scourer", new Color(0.2f, 0.55f, 0.25f), 0.1f);
            var plug = ArenaSceneBuilder.Lit("Plug", new Color(0.12f, 0.12f, 0.14f), 0.7f);
            var chrome = ArenaSceneBuilder.Lit("Chrome", new Color(0.85f, 0.88f, 0.92f), 0.97f);
            var suds = ArenaSceneBuilder.Unlit("Suds", new Color(0.92f, 0.96f, 1f));
            var water = ArenaSceneBuilder.Unlit("TapWater", new Color(0.5f, 0.75f, 1f));
            var lamp = ArenaSceneBuilder.Unlit("SinkLamp", new Color(0.9f, 0.97f, 1f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var squirt = new GameObject("Squirt");
            Decor(PrimitiveType.Sphere, "Visual", squirt.transform, Vector3.zero, new Vector3(0.22f, 0.22f, 0.4f), bottle);
            squirt.AddComponent<Bullet>().radius = 0.17f;
            var squirtPrefab = ArenaSceneBuilder.SavePrefab(squirt, "Enemies/Squirt").GetComponent<Bullet>();

            var jet = new GameObject("SinkJet");
            Decor(PrimitiveType.Cube, "Visual", jet.transform, Vector3.zero, new Vector3(0.12f, 0.12f, 0.9f), water);
            jet.AddComponent<Bullet>().radius = 0.17f;
            var jetPrefab = ArenaSceneBuilder.SavePrefab(jet, "Enemies/SinkJet").GetComponent<Bullet>();

            var sinkSlug = Creature<ChaserEnemy>("SinkSlug", 0.45f, 0.6f, 5, 1f, 1.5f, out var slugModel);
            Decor(PrimitiveType.Sphere, "Body", slugModel, new Vector3(0f, 0.22f, 0f), new Vector3(0.45f, 0.3f, 1.1f), slug);
            Decor(PrimitiveType.Cylinder, "Stalk L", slugModel, new Vector3(-0.1f, 0.45f, 0.45f), new Vector3(0.04f, 0.15f, 0.04f), slug);
            Decor(PrimitiveType.Cylinder, "Stalk R", slugModel, new Vector3(0.1f, 0.45f, 0.45f), new Vector3(0.04f, 0.15f, 0.04f), slug);
            sinkSlug.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.AcidGland };
            var slugPrefab = SaveEnemy(sinkSlug);

            var scrub = Creature<ChargerEnemy>("ScrubbingBrush", 0.65f, 0.9f, 10, 1.4f, 2f, out var scrubModel);
            Decor(PrimitiveType.Cube, "Back", scrubModel, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.3f, 1.3f), brush);
            Decor(PrimitiveType.Cube, "Bristles", scrubModel, new Vector3(0f, 0.15f, 0f), new Vector3(0.85f, 0.3f, 1.25f), bristle);
            Decor(PrimitiveType.Sphere, "Eye L", scrubModel, new Vector3(-0.2f, 0.6f, 0.55f), Vector3.one * 0.12f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", scrubModel, new Vector3(0.2f, 0.6f, 0.55f), Vector3.one * 0.12f, dark);
            scrub.knockbackOnHit = 1.5f;
            scrub.telegraphMaterial = telegraph;
            scrub.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.SugarCube };
            var scrubPrefab = SaveEnemy(scrub);

            var squirter = Creature<ShooterEnemy>("SquirtyBottle", 0.45f, 1.5f, 5, 1f, 2.3f, out var bottleModel);
            Decor(PrimitiveType.Cylinder, "Bottle", bottleModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.6f, 0.6f, 0.6f), bottle);
            Decor(PrimitiveType.Cube, "Nozzle", bottleModel, new Vector3(0f, 1.3f, 0.15f), new Vector3(0.15f, 0.2f, 0.4f), chrome);
            Decor(PrimitiveType.Sphere, "Eye L", bottleModel, new Vector3(-0.13f, 0.8f, 0.29f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", bottleModel, new Vector3(0.13f, 0.8f, 0.29f), Vector3.one * 0.1f, dark);
            var bottleMuzzle = new GameObject("Muzzle").transform;
            bottleMuzzle.SetParent(squirter.transform, false);
            bottleMuzzle.localPosition = new Vector3(0f, 1.3f, 0.5f);
            squirter.muzzle = bottleMuzzle;
            squirter.projectilePrefab = squirtPrefab;
            squirter.favouredDrops = new[] { ItemType.RainBoot, ItemType.Umbrella };
            var squirterPrefab = SaveEnemy(squirter);

            var drainFly = Creature<FlyerEnemy>("DrainFly", 0.4f, 2f, 3, 0.8f, 2.6f, out var flyModel);
            Decor(PrimitiveType.Sphere, "Body", flyModel, new Vector3(0f, 0.85f, 0f), new Vector3(0.3f, 0.25f, 0.4f), fly);
            var wingL = Decor(PrimitiveType.Sphere, "Wing L", flyModel, new Vector3(-0.3f, 0.9f, 0f), new Vector3(0.45f, 0.02f, 0.3f), fly).transform;
            var wingR = Decor(PrimitiveType.Sphere, "Wing R", flyModel, new Vector3(0.3f, 0.9f, 0f), new Vector3(0.45f, 0.02f, 0.3f), fly).transform;
            var flyMotion = drainFly.gameObject.AddComponent<AntMotion>();
            flyMotion.legs = new Transform[0];
            flyMotion.wings = new[] { wingL, wingR };
            drainFly.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            drainFly.favouredDrops = new[] { ItemType.WingBuds, ItemType.KiteString };
            var drainFlyPrefab = SaveEnemy(drainFly);

            var soap = Creature<LobberEnemy>("SoapDispenser", 0.5f, 1.5f, 6, 1f, 2.4f, out var soapModel);
            Decor(PrimitiveType.Cylinder, "Body", soapModel, new Vector3(0f, 0.55f, 0f), new Vector3(0.6f, 0.55f, 0.6f), mug);
            Decor(PrimitiveType.Cube, "Pump", soapModel, new Vector3(0f, 1.25f, 0.1f), new Vector3(0.12f, 0.25f, 0.35f), chrome);
            soap.blastMaterial = telegraph;
            soap.lobMaterial = suds;
            soap.favouredDrops = new[] { ItemType.Lunchbox, ItemType.BubbleWrap };
            var soapPrefab = SaveEnemy(soap);

            var plugEnemy = Creature<SupportEnemy>("ThePlug", 0.45f, 1.2f, 4, 1f, 2f, out var plugModel);
            Decor(PrimitiveType.Cylinder, "Plug", plugModel, new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.25f, 0.8f), plug);
            Decor(PrimitiveType.Cylinder, "Chain", plugModel, new Vector3(0f, 0.8f, 0f), new Vector3(0.05f, 0.4f, 0.05f), chrome);
            plugEnemy.pulseColor = new Color(0.7f, 0.85f, 1f);
            plugEnemy.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.SpareBattery };
            var plugPrefab = SaveEnemy(plugEnemy);

            var dripper = Creature<SniperEnemy>("LeakyPipe", 0.45f, 1.3f, 5, 0.9f, 2.2f, out var pipeModel);
            Decor(PrimitiveType.Cylinder, "Pipe", pipeModel, new Vector3(0f, 0.55f, 0f), new Vector3(0.4f, 0.55f, 0.4f), chrome);
            Decor(PrimitiveType.Cylinder, "Elbow", pipeModel, new Vector3(0f, 1.05f, 0.25f), new Vector3(0.35f, 0.25f, 0.35f), chrome).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var pipeMuzzle = new GameObject("Muzzle").transform;
            pipeMuzzle.SetParent(dripper.transform, false);
            pipeMuzzle.localPosition = new Vector3(0f, 1f, 0.6f);
            dripper.muzzle = pipeMuzzle;
            dripper.projectilePrefab = jetPrefab;
            dripper.telegraphMaterial = telegraph;
            dripper.favouredDrops = new[] { ItemType.NeedleTip, ItemType.IceCube };
            var dripperPrefab = SaveEnemy(dripper);

            var egg = Creature<LarvaEnemy>("FlyEgg", 0.3f, 0.5f, 2, 0.6f, 1.1f, out var eggModel);
            Decor(PrimitiveType.Sphere, "Egg", eggModel, new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.25f, 0.4f), fly);
            egg.growsInto = drainFlyPrefab;
            egg.dropsLoot = false;
            egg.knockbackOnHit = 1f;
            Object.DestroyImmediate(egg.GetComponent<EnemyMutations>());
            var eggPrefab = (LarvaEnemy)SaveEnemy(egg);

            // The Sponge: the toad's hops, without the tongue. It lands where you are going.
            var spongeBoss = Creature<ToadBoss>("TheSponge", 1f, 1.6f, 240, 1f, 2.8f, out var spongeModel);
            Decor(PrimitiveType.Cube, "Sponge", spongeModel, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.9f, 1.3f), sponge);
            Decor(PrimitiveType.Cube, "Scourer", spongeModel, new Vector3(0f, 1.12f, 0f), new Vector3(1.82f, 0.18f, 1.32f), scourer);
            Decor(PrimitiveType.Sphere, "Eye L", spongeModel, new Vector3(-0.35f, 0.75f, 0.66f), Vector3.one * 0.2f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", spongeModel, new Vector3(0.35f, 0.75f, 0.66f), Vector3.one * 0.2f, dark);
            spongeBoss.displayName = "The Sponge";
            spongeBoss.subtitle = "Soaks everything up. Squeezes it out on you.";
            spongeBoss.canTongue = false;
            spongeBoss.telegraphMaterial = telegraph;
            spongeBoss.tongueMaterial = suds;
            spongeBoss.friends = new[] { slugPrefab };
            spongeBoss.sampleReward = 12;
            spongeBoss.knockbackOnHit = 0.4f;
            Object.DestroyImmediate(spongeBoss.GetComponent<EnemyMutations>());
            var spongePrefab = (BossEnemy)SaveEnemy(spongeBoss);

            // The Tap: the cloud's moves, in chrome. Drips land where you are going, the jet blows you along, the suds
            // stick.
            var tap = Creature<StormBoss>("TheTap", 1.4f, 3.6f, 480, 1f, 5f, out var tapModel);
            Decor(PrimitiveType.Cylinder, "Neck", tapModel, new Vector3(0f, 2.4f, 0f), new Vector3(0.6f, 1.2f, 0.6f), chrome);
            Decor(PrimitiveType.Cylinder, "Spout", tapModel, new Vector3(0f, 3.4f, 0.6f), new Vector3(0.45f, 0.7f, 0.45f), chrome).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Decor(PrimitiveType.Cube, "Handle", tapModel, new Vector3(0f, 3.75f, 0f), new Vector3(1.6f, 0.15f, 0.2f), chrome);
            Decor(PrimitiveType.Sphere, "Hot", tapModel, new Vector3(-0.8f, 3.85f, 0f), Vector3.one * 0.25f, ArenaSceneBuilder.Unlit("HotRed", new Color(1f, 0.3f, 0.25f)));
            Decor(PrimitiveType.Sphere, "Cold", tapModel, new Vector3(0.8f, 3.85f, 0f), Vector3.one * 0.25f, ArenaSceneBuilder.Unlit("ColdBlue", new Color(0.3f, 0.6f, 1f)));
            Decor(PrimitiveType.Sphere, "Eye L", tapModel, new Vector3(-0.2f, 2.9f, 0.31f), Vector3.one * 0.18f, lamp);
            Decor(PrimitiveType.Sphere, "Eye R", tapModel, new Vector3(0.2f, 2.9f, 0.31f), Vector3.one * 0.18f, lamp);
            tap.displayName = "The Tap";
            tap.subtitle = "Drip. Drip. Drip. It has been waiting all night for you.";
            tap.isWorldBoss = false;
            tap.telegraphMaterial = telegraph;
            tap.rainMaterial = suds;
            tap.friends = new[] { squirterPrefab, drainFlyPrefab };
            tap.sampleReward = 30;
            tap.knockbackOnHit = 0.2f;
            Object.DestroyImmediate(tap.GetComponent<EnemyMutations>());
            var tapPrefab = (BossEnemy)SaveEnemy(tap);

            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(SinkPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, SinkPath);
            }

            biome.displayName = "The Kitchen Sink";
            biome.id = "sink";
            biome.mutationTheme = "slime, suds, things that drip";
            biome.isBottom = false;
            biome.ledges = true;
            biome.floods = true;
            biome.ledgeTopMaterial = mug;
            biome.floorMaterial = steel;
            biome.wallMaterial = tiles;
            biome.doorMaterial = chrome;
            biome.wallHeight = 5f;
            biome.ceiling = false;
            biome.skyColor = new Color(0.25f, 0.32f, 0.4f);
            biome.ambientColor = new Color(0.5f, 0.56f, 0.62f);
            biome.sunIntensity = 0.85f;
            biome.fog = true;
            biome.fogColor = new Color(0.3f, 0.37f, 0.45f);
            biome.fogStart = 35f;
            biome.fogEnd = 120f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = mug;              // upturned mugs
            biome.crumbMaterial = suds;
            biome.sugarMaterial = sponge;
            biome.rootMaterial = chrome;
            biome.podMaterial = fly;
            biome.podHatchling = eggPrefab;
            biome.dewMaterial = water;
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = lamp;
            biome.glowStemMaterial = chrome;
            biome.glowLightRange = 11f;
            biome.glowLightIntensity = 1.6f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = slugPrefab;
            biome.miniBoss = spongePrefab;
            biome.boss = tapPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = slugPrefab, weight = 4, minFloor = 1 },
                new EnemySpawnEntry { prefab = squirterPrefab, weight = 3, minFloor = 1 },
                new EnemySpawnEntry { prefab = scrubPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = drainFlyPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = soapPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = plugPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = dripperPrefab, weight = 2, minFloor = 5 },
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
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("SinkPortal", lamp, black, chrome), "Props/SinkPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
