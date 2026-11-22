using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The thirteenth world: The Garden Shed. Concrete underfoot, creosoted planks for walls, cobwebs and tools. Seven
    /// enemies on the seven behaviours, the Hedge Trimmer half way and the Lawnmower at the end, whose fight is a band
    /// of marks across the room with a gap or two to stand in, and a charge along a marked line. Its portal opens once
    /// the Spider has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string ShedPath = "Assets/Data/TheShed.asset";

        public static BiomeDefinition BuildShed(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var concrete = ArenaSceneBuilder.Lit("ShedConcrete", new Color(0.5f, 0.5f, 0.48f), 0.15f);
            var planks = ArenaSceneBuilder.Lit("ShedPlanks", new Color(0.35f, 0.25f, 0.16f), 0.2f);
            var earwigBrown = ArenaSceneBuilder.Lit("Earwig", new Color(0.4f, 0.22f, 0.12f), 0.6f);
            var terracotta = ArenaSceneBuilder.Lit("Terracotta", new Color(0.8f, 0.42f, 0.25f), 0.2f);
            var canGreen = ArenaSceneBuilder.Lit("CanGreen", new Color(0.25f, 0.55f, 0.3f), 0.6f);
            var dragonBlue = ArenaSceneBuilder.Lit("Dragonfly", new Color(0.2f, 0.5f, 0.8f), 0.8f);
            var gnomeRed = ArenaSceneBuilder.Lit("GnomeHat", new Color(0.85f, 0.15f, 0.15f), 0.4f);
            var gnomeBlue = ArenaSceneBuilder.Lit("GnomeCoat", new Color(0.2f, 0.3f, 0.7f), 0.4f);
            var skin = ArenaSceneBuilder.Lit("GnomeSkin", new Color(0.95f, 0.75f, 0.6f), 0.3f);
            var beard = ArenaSceneBuilder.Lit("GnomeBeard", new Color(0.95f, 0.95f, 0.95f), 0.2f);
            var chimeMetal = ArenaSceneBuilder.Lit("ChimeMetal", new Color(0.75f, 0.75f, 0.7f), 0.9f);
            var hose = ArenaSceneBuilder.Lit("Hosepipe", new Color(0.2f, 0.6f, 0.25f), 0.5f);
            var mowerRed = ArenaSceneBuilder.Lit("MowerRed", new Color(0.8f, 0.1f, 0.1f), 0.6f);
            var steel = ArenaSceneBuilder.Lit("StaplerSteel", new Color(0.3f, 0.32f, 0.36f), 0.8f);
            var seed = ArenaSceneBuilder.Lit("Seed", new Color(0.55f, 0.45f, 0.25f), 0.3f);
            var waterJet = ArenaSceneBuilder.Unlit("WaterJet", new Color(0.5f, 0.75f, 1f));
            var lamp = ArenaSceneBuilder.Unlit("ShedLamp", new Color(1f, 0.85f, 0.55f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var drop = new GameObject("CanDrop");
            Decor(PrimitiveType.Sphere, "Visual", drop.transform, Vector3.zero, new Vector3(0.25f, 0.25f, 0.4f), waterJet);
            drop.AddComponent<Bullet>().radius = 0.18f;
            var dropPrefab = ArenaSceneBuilder.SavePrefab(drop, "Enemies/CanDrop").GetComponent<Bullet>();

            var jet = new GameObject("HoseJet");
            Decor(PrimitiveType.Cube, "Visual", jet.transform, Vector3.zero, new Vector3(0.12f, 0.12f, 0.9f), waterJet);
            jet.AddComponent<Bullet>().radius = 0.17f;
            var jetPrefab = ArenaSceneBuilder.SavePrefab(jet, "Enemies/HoseJet").GetComponent<Bullet>();

            var earwig = Creature<ChaserEnemy>("Earwig", 0.45f, 0.8f, 5, 1f, 1.7f, out var earwigModel);
            Decor(PrimitiveType.Sphere, "Body", earwigModel, new Vector3(0f, 0.3f, 0f), new Vector3(0.4f, 0.3f, 1.1f), earwigBrown);
            Decor(PrimitiveType.Cube, "Pincer L", earwigModel, new Vector3(-0.1f, 0.3f, -0.7f), new Vector3(0.05f, 0.05f, 0.4f), earwigBrown).transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            Decor(PrimitiveType.Cube, "Pincer R", earwigModel, new Vector3(0.1f, 0.3f, -0.7f), new Vector3(0.05f, 0.05f, 0.4f), earwigBrown).transform.localRotation = Quaternion.Euler(0f, -15f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", earwigModel, new Vector3(-0.08f, 0.38f, 0.5f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", earwigModel, new Vector3(0.08f, 0.38f, 0.5f), Vector3.one * 0.07f, dark);
            earwig.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.SplitterMandible };
            var earwigPrefab = SaveEnemy(earwig);

            var pot = Creature<ChargerEnemy>("FlowerPot", 0.7f, 1.4f, 10, 1.5f, 2.2f, out var potModel);
            Decor(PrimitiveType.Cylinder, "Pot", potModel, new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 0.6f, 1.1f), terracotta);
            Decor(PrimitiveType.Cylinder, "Soil", potModel, new Vector3(0f, 1.2f, 0f), new Vector3(1f, 0.04f, 1f), dark);
            Decor(PrimitiveType.Sphere, "Eye L", potModel, new Vector3(-0.25f, 0.8f, 0.5f), Vector3.one * 0.15f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", potModel, new Vector3(0.25f, 0.8f, 0.5f), Vector3.one * 0.15f, dark);
            pot.knockbackOnHit = 1.5f;
            pot.telegraphMaterial = telegraph;
            pot.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.SugarCube };
            var potPrefab = SaveEnemy(pot);

            var can = Creature<ShooterEnemy>("WateringCan", 0.55f, 1.3f, 4, 0.9f, 2.2f, out var canModel);
            Decor(PrimitiveType.Cylinder, "Can", canModel, new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 0.5f, 0.75f), canGreen);
            Decor(PrimitiveType.Cube, "Spout", canModel, new Vector3(0f, 0.75f, 0.55f), new Vector3(0.1f, 0.1f, 0.7f), canGreen).transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", canModel, new Vector3(-0.15f, 0.7f, 0.36f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", canModel, new Vector3(0.15f, 0.7f, 0.36f), Vector3.one * 0.1f, dark);
            var canMuzzle = new GameObject("Muzzle").transform;
            canMuzzle.SetParent(can.transform, false);
            canMuzzle.localPosition = new Vector3(0f, 1f, 0.9f);
            can.muzzle = canMuzzle;
            can.projectilePrefab = dropPrefab;
            can.favouredDrops = new[] { ItemType.RainBoot, ItemType.AcidGland };
            var canPrefab = SaveEnemy(can);

            var fly = Creature<FlyerEnemy>("Dragonfly", 0.45f, 2f, 3, 0.8f, 2.9f, out var flyModel);
            Decor(PrimitiveType.Cylinder, "Body", flyModel, new Vector3(0f, 0.75f, 0f), new Vector3(0.12f, 0.6f, 0.12f), dragonBlue).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var flyL = Decor(PrimitiveType.Cube, "Wing L", flyModel, new Vector3(-0.4f, 0.8f, 0.1f), new Vector3(0.7f, 0.02f, 0.2f), ArenaSceneBuilder.Lit("WingGlass", new Color(0.75f, 0.85f, 0.9f), 0.9f)).transform;
            var flyR = Decor(PrimitiveType.Cube, "Wing R", flyModel, new Vector3(0.4f, 0.8f, 0.1f), new Vector3(0.7f, 0.02f, 0.2f), ArenaSceneBuilder.Lit("WingGlass", new Color(0.75f, 0.85f, 0.9f), 0.9f)).transform;
            var flyMotion = fly.gameObject.AddComponent<AntMotion>();
            flyMotion.legs = new Transform[0];
            flyMotion.wings = new[] { flyL, flyR };
            fly.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            fly.favouredDrops = new[] { ItemType.WingBuds, ItemType.KiteString };
            var flyPrefab = SaveEnemy(fly);

            var gnome = Creature<LobberEnemy>("GardenGnome", 0.5f, 1.5f, 6, 1f, 2.4f, out var gnomeModel);
            Decor(PrimitiveType.Cylinder, "Coat", gnomeModel, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.45f, 0.6f), gnomeBlue);
            Decor(PrimitiveType.Sphere, "Face", gnomeModel, new Vector3(0f, 1f, 0f), Vector3.one * 0.4f, skin);
            Decor(PrimitiveType.Sphere, "Beard", gnomeModel, new Vector3(0f, 0.85f, 0.15f), new Vector3(0.4f, 0.35f, 0.2f), beard);
            Decor(PrimitiveType.Cylinder, "Hat", gnomeModel, new Vector3(0f, 1.35f, 0f), new Vector3(0.35f, 0.3f, 0.35f), gnomeRed);
            gnome.blastMaterial = telegraph;
            gnome.lobMaterial = seed;
            gnome.favouredDrops = new[] { ItemType.Lunchbox, ItemType.LostMarble };
            var gnomePrefab = SaveEnemy(gnome);

            var chime = Creature<SupportEnemy>("WindChime", 0.45f, 1.6f, 4, 1f, 2.5f, out var chimeModel);
            Decor(PrimitiveType.Cylinder, "Top", chimeModel, new Vector3(0f, 1.5f, 0f), new Vector3(0.6f, 0.04f, 0.6f), planks);
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Cylinder, "Tube " + i, chimeModel, new Vector3(Mathf.Cos(i * 1.57f) * 0.2f, 1f - i * 0.05f, Mathf.Sin(i * 1.57f) * 0.2f), new Vector3(0.07f, 0.4f, 0.07f), chimeMetal);
            chime.pulseColor = new Color(0.85f, 0.9f, 1f);
            chime.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.SpareBattery };
            var chimePrefab = SaveEnemy(chime);

            var hosePipe = Creature<SniperEnemy>("Hosepipe", 0.45f, 1.2f, 5, 0.9f, 2.1f, out var hoseModel);
            Decor(PrimitiveType.Cylinder, "Reel", hoseModel, new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 0.15f, 0.8f), hose).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Decor(PrimitiveType.Cube, "Nozzle", hoseModel, new Vector3(0f, 0.75f, 0.45f), new Vector3(0.12f, 0.12f, 0.5f), steel);
            var hoseMuzzle = new GameObject("Muzzle").transform;
            hoseMuzzle.SetParent(hosePipe.transform, false);
            hoseMuzzle.localPosition = new Vector3(0f, 0.75f, 0.8f);
            hosePipe.muzzle = hoseMuzzle;
            hosePipe.projectilePrefab = jetPrefab;
            hosePipe.telegraphMaterial = telegraph;
            hosePipe.favouredDrops = new[] { ItemType.NeedleTip, ItemType.Umbrella };
            var hosePrefab = SaveEnemy(hosePipe);

            var seedling = Creature<LarvaEnemy>("Seedling", 0.35f, 0.6f, 2, 0.6f, 1.2f, out var seedModel);
            Decor(PrimitiveType.Sphere, "Seed", seedModel, new Vector3(0f, 0.2f, 0f), new Vector3(0.35f, 0.3f, 0.45f), seed);
            Decor(PrimitiveType.Cube, "Shoot", seedModel, new Vector3(0f, 0.45f, 0f), new Vector3(0.04f, 0.3f, 0.04f), canGreen);
            seedling.growsInto = earwigPrefab;
            seedling.dropsLoot = false;
            seedling.knockbackOnHit = 1f;
            Object.DestroyImmediate(seedling.GetComponent<EnemyMutations>());
            var seedlingPrefab = (LarvaEnemy)SaveEnemy(seedling);

            var trimmer = Creature<MowerBoss>("TheHedgeTrimmer", 0.9f, 1.4f, 190, 1f, 2.8f, out var trimmerModel);
            Decor(PrimitiveType.Cube, "Body", trimmerModel, new Vector3(0f, 0.7f, -0.3f), new Vector3(0.8f, 0.7f, 1.2f), canGreen);
            Decor(PrimitiveType.Cube, "Blade", trimmerModel, new Vector3(0f, 0.7f, 1.1f), new Vector3(0.25f, 0.12f, 1.8f), steel);
            for (int i = 0; i < 6; i++)
                Decor(PrimitiveType.Cube, "Tooth " + i, trimmerModel, new Vector3(0f, 0.7f, 0.4f + i * 0.28f), new Vector3(0.45f, 0.06f, 0.06f), steel);
            Decor(PrimitiveType.Sphere, "Eye L", trimmerModel, new Vector3(-0.2f, 0.95f, 0.3f), Vector3.one * 0.12f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", trimmerModel, new Vector3(0.2f, 0.95f, 0.3f), Vector3.one * 0.12f, dark);
            trimmer.displayName = "The Hedge Trimmer";
            trimmer.subtitle = "Keeps things neat. You are untidy.";
            trimmer.canSweep = false;
            trimmer.restBetween = 1.2f;
            trimmer.telegraphMaterial = telegraph;
            trimmer.friends = new[] { earwigPrefab };
            trimmer.sampleReward = 12;
            trimmer.knockbackOnHit = 0.5f;
            Object.DestroyImmediate(trimmer.GetComponent<EnemyMutations>());
            var trimmerPrefab = (BossEnemy)SaveEnemy(trimmer);

            var mower = Creature<MowerBoss>("TheLawnmower", 1.5f, 2.2f, 520, 1f, 3.8f, out var mowerModel);
            Decor(PrimitiveType.Cube, "Deck", mowerModel, new Vector3(0f, 0.7f, 0f), new Vector3(2.6f, 0.9f, 2.8f), mowerRed);
            Decor(PrimitiveType.Cube, "Engine", mowerModel, new Vector3(0f, 1.4f, 0.2f), new Vector3(1.2f, 0.7f, 1.2f), steel);
            Decor(PrimitiveType.Cube, "Handle", mowerModel, new Vector3(0f, 1.9f, -1.6f), new Vector3(1.8f, 0.1f, 0.1f), steel);
            Decor(PrimitiveType.Cube, "Bar L", mowerModel, new Vector3(-0.85f, 1.4f, -1.4f), new Vector3(0.08f, 1f, 0.08f), steel).transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            Decor(PrimitiveType.Cube, "Bar R", mowerModel, new Vector3(0.85f, 1.4f, -1.4f), new Vector3(0.08f, 1f, 0.08f), steel).transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Cylinder, "Wheel " + i, mowerModel, new Vector3(i < 2 ? -1.35f : 1.35f, 0.3f, i % 2 == 0 ? 1f : -1f), new Vector3(0.6f, 0.1f, 0.6f), dark).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Decor(PrimitiveType.Sphere, "Eye L", mowerModel, new Vector3(-0.5f, 1.05f, 1.42f), Vector3.one * 0.3f, lamp);
            Decor(PrimitiveType.Sphere, "Eye R", mowerModel, new Vector3(0.5f, 1.05f, 1.42f), Vector3.one * 0.3f, lamp);
            mower.displayName = "The Lawnmower";
            mower.subtitle = "Every Sunday. Today is Sunday.";
            mower.telegraphMaterial = telegraph;
            mower.friends = new[] { gnomePrefab, chimePrefab };
            mower.sampleReward = 30;
            mower.knockbackOnHit = 0.2f;
            Object.DestroyImmediate(mower.GetComponent<EnemyMutations>());
            var mowerPrefab = (BossEnemy)SaveEnemy(mower);

            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(ShedPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, ShedPath);
            }

            biome.displayName = "The Garden Shed";
            biome.id = "shed";
            biome.mutationTheme = "shells, speed, things with blades";
            biome.isBottom = false;
            biome.floorMaterial = concrete;
            biome.wallMaterial = planks;
            biome.doorMaterial = terracotta;
            biome.wallHeight = 5f;
            biome.ceiling = true;
            biome.ceilingMaterial = planks;
            biome.skyColor = new Color(0.12f, 0.1f, 0.08f);
            biome.ambientColor = new Color(0.55f, 0.52f, 0.46f);
            biome.sunIntensity = 0.75f;
            biome.fog = true;
            biome.fogColor = new Color(0.18f, 0.15f, 0.12f);
            biome.fogStart = 20f;
            biome.fogEnd = 70f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = terracotta;          // stacked pots
            biome.crumbMaterial = seed;
            biome.sugarMaterial = canGreen;
            biome.rootMaterial = hose;
            biome.podMaterial = seed;
            biome.podHatchling = seedlingPrefab;
            biome.dewMaterial = waterJet;
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = lamp;
            biome.glowStemMaterial = steel;
            biome.glowLightRange = 11f;
            biome.glowLightIntensity = 2f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = earwigPrefab;
            biome.miniBoss = trimmerPrefab;
            biome.boss = mowerPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = earwigPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = canPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = potPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = flyPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = gnomePrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = chimePrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = hosePrefab, weight = 1, minFloor = 5 },
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
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("ShedPortal", lamp, black, planks), "Props/ShedPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
