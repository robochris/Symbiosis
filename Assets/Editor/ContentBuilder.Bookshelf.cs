using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The fourteenth world: The Bookshelf, upstairs in the study. A second storey at last: the fight rooms have
    /// ledges in their corners, stacked books with a ramp up, and enemies stand on them as well as below. Seven
    /// enemies on the seven behaviours, the Bookend half way and the Encyclopedia at the end, whose fight is a long
    /// marked strip it falls flat along, and fans of loose pages. Its portal opens once the Lawnmower is beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string BookshelfPath = "Assets/Data/TheBookshelf.asset";

        public static BiomeDefinition BuildBookshelf(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var shelfWood = ArenaSceneBuilder.Lit("ShelfWood", new Color(0.42f, 0.26f, 0.14f), 0.25f);
            var spines = ArenaSceneBuilder.Lit("BookSpines", new Color(0.45f, 0.12f, 0.12f), 0.3f);
            var paper = ArenaSceneBuilder.Lit("ShelfPaper", new Color(0.93f, 0.9f, 0.8f), 0.1f);
            var silver = ArenaSceneBuilder.Lit("Silverfish", new Color(0.7f, 0.72f, 0.78f), 0.8f);
            var ink = ArenaSceneBuilder.Lit("InkPot", new Color(0.12f, 0.14f, 0.3f), 0.9f);
            var inkDrop = ArenaSceneBuilder.Unlit("InkDrop", new Color(0.15f, 0.2f, 0.55f));
            var punchGrey = ArenaSceneBuilder.Lit("HolePunch", new Color(0.35f, 0.38f, 0.42f), 0.7f);
            var stampRed = ArenaSceneBuilder.Lit("StampRed", new Color(0.75f, 0.15f, 0.15f), 0.4f);
            var stampWood = ArenaSceneBuilder.Lit("StampHandle", new Color(0.55f, 0.35f, 0.18f), 0.3f);
            var lampBrass = ArenaSceneBuilder.Lit("LampBrass", new Color(0.8f, 0.62f, 0.25f), 0.85f);
            var pencilYellow = ArenaSceneBuilder.Lit("PencilYellow", new Color(1f, 0.8f, 0.15f), 0.3f);
            var graphite = ArenaSceneBuilder.Unlit("Graphite", new Color(0.25f, 0.25f, 0.28f));
            var bookGreen = ArenaSceneBuilder.Lit("BookGreen", new Color(0.15f, 0.4f, 0.25f), 0.3f);
            var bookBlue = ArenaSceneBuilder.Lit("BookBlue", new Color(0.15f, 0.25f, 0.55f), 0.3f);
            var gilt = ArenaSceneBuilder.Lit("Gilt", new Color(0.95f, 0.78f, 0.3f), 0.9f);
            var lamp = ArenaSceneBuilder.Unlit("ReadingLamp", new Color(1f, 0.9f, 0.6f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var drop = new GameObject("InkBlot");
            Decor(PrimitiveType.Sphere, "Visual", drop.transform, Vector3.zero, new Vector3(0.28f, 0.28f, 0.4f), inkDrop);
            drop.AddComponent<Bullet>().radius = 0.18f;
            var dropPrefab = ArenaSceneBuilder.SavePrefab(drop, "Enemies/InkBlot").GetComponent<Bullet>();

            var lead = new GameObject("PencilLine");
            Decor(PrimitiveType.Cube, "Visual", lead.transform, Vector3.zero, new Vector3(0.1f, 0.1f, 1f), graphite);
            lead.AddComponent<Bullet>().radius = 0.16f;
            var leadPrefab = ArenaSceneBuilder.SavePrefab(lead, "Enemies/PencilLine").GetComponent<Bullet>();

            var page = new GameObject("LoosePage");
            Decor(PrimitiveType.Cube, "Visual", page.transform, Vector3.zero, new Vector3(0.5f, 0.04f, 0.65f), paper);
            page.AddComponent<Bullet>().radius = 0.3f;
            var pagePrefab = ArenaSceneBuilder.SavePrefab(page, "Enemies/LoosePage").GetComponent<Bullet>();

            var fish = Creature<ChaserEnemy>("Silverfish", 0.4f, 0.6f, 4, 1f, 1.5f, out var fishModel);
            Decor(PrimitiveType.Sphere, "Body", fishModel, new Vector3(0f, 0.2f, 0f), new Vector3(0.35f, 0.18f, 0.95f), silver);
            Decor(PrimitiveType.Cube, "Tail", fishModel, new Vector3(0f, 0.2f, -0.6f), new Vector3(0.25f, 0.03f, 0.3f), silver);
            Decor(PrimitiveType.Sphere, "Eye L", fishModel, new Vector3(-0.08f, 0.27f, 0.42f), Vector3.one * 0.06f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", fishModel, new Vector3(0.08f, 0.27f, 0.42f), Vector3.one * 0.06f, dark);
            fish.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.WingBuds };
            var fishPrefab = SaveEnemy(fish);

            var pot = Creature<ShooterEnemy>("InkPot", 0.5f, 1.1f, 5, 1f, 2f, out var potModel);
            Decor(PrimitiveType.Cylinder, "Pot", potModel, new Vector3(0f, 0.4f, 0f), new Vector3(0.75f, 0.4f, 0.75f), ink);
            Decor(PrimitiveType.Cylinder, "Neck", potModel, new Vector3(0f, 0.9f, 0f), new Vector3(0.35f, 0.12f, 0.35f), ink);
            Decor(PrimitiveType.Sphere, "Eye L", potModel, new Vector3(-0.15f, 0.55f, 0.35f), Vector3.one * 0.12f, paper);
            Decor(PrimitiveType.Sphere, "Eye R", potModel, new Vector3(0.15f, 0.55f, 0.35f), Vector3.one * 0.12f, paper);
            var potMuzzle = new GameObject("Muzzle").transform;
            potMuzzle.SetParent(pot.transform, false);
            potMuzzle.localPosition = new Vector3(0f, 1f, 0.4f);
            pot.muzzle = potMuzzle;
            pot.projectilePrefab = dropPrefab;
            pot.favouredDrops = new[] { ItemType.AcidGland, ItemType.CompoundEye };
            var potPrefab = SaveEnemy(pot);

            var punch = Creature<ChargerEnemy>("HolePunch", 0.65f, 1f, 10, 1.4f, 2f, out var punchModel);
            Decor(PrimitiveType.Cube, "Base", punchModel, new Vector3(0f, 0.25f, 0f), new Vector3(1.1f, 0.4f, 1.3f), punchGrey);
            Decor(PrimitiveType.Cube, "Lever", punchModel, new Vector3(0f, 0.65f, -0.1f), new Vector3(1f, 0.15f, 1.2f), punchGrey).transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", punchModel, new Vector3(-0.25f, 0.45f, 0.62f), Vector3.one * 0.14f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", punchModel, new Vector3(0.25f, 0.45f, 0.62f), Vector3.one * 0.14f, dark);
            punch.knockbackOnHit = 1.5f;
            punch.telegraphMaterial = telegraph;
            punch.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.SugarCube };
            var punchPrefab = SaveEnemy(punch);

            var plane = Creature<FlyerEnemy>("PaperPlane", 0.45f, 2f, 3, 0.8f, 2.8f, out var planeModel);
            Decor(PrimitiveType.Cube, "Body", planeModel, new Vector3(0f, 0.8f, 0f), new Vector3(0.12f, 0.12f, 0.9f), paper);
            var planeL = Decor(PrimitiveType.Cube, "Wing L", planeModel, new Vector3(-0.3f, 0.82f, -0.05f), new Vector3(0.5f, 0.02f, 0.6f), paper).transform;
            var planeR = Decor(PrimitiveType.Cube, "Wing R", planeModel, new Vector3(0.3f, 0.82f, -0.05f), new Vector3(0.5f, 0.02f, 0.6f), paper).transform;
            var planeMotion = plane.gameObject.AddComponent<AntMotion>();
            planeMotion.legs = new Transform[0];
            planeMotion.wings = new[] { planeL, planeR };
            plane.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            plane.favouredDrops = new[] { ItemType.WingBuds, ItemType.KiteString };
            var planePrefab = SaveEnemy(plane);

            var stamp = Creature<LobberEnemy>("RubberStamp", 0.5f, 1.4f, 6, 1f, 2.3f, out var stampModel);
            Decor(PrimitiveType.Cube, "Pad", stampModel, new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.25f, 0.6f), stampRed);
            Decor(PrimitiveType.Cylinder, "Stem", stampModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.2f, 0.3f, 0.2f), stampWood);
            Decor(PrimitiveType.Sphere, "Knob", stampModel, new Vector3(0f, 1.05f, 0f), Vector3.one * 0.4f, stampWood);
            Decor(PrimitiveType.Sphere, "Eye L", stampModel, new Vector3(-0.15f, 1.1f, 0.17f), Vector3.one * 0.08f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", stampModel, new Vector3(0.15f, 1.1f, 0.17f), Vector3.one * 0.08f, dark);
            stamp.blastMaterial = telegraph;
            stamp.lobMaterial = stampRed;
            stamp.favouredDrops = new[] { ItemType.Lunchbox, ItemType.LostMarble };
            var stampPrefab = SaveEnemy(stamp);

            var deskLamp = Creature<SupportEnemy>("DeskLamp", 0.45f, 1.7f, 4, 1f, 2.5f, out var lampModel);
            Decor(PrimitiveType.Cylinder, "Foot", lampModel, new Vector3(0f, 0.05f, 0f), new Vector3(0.6f, 0.05f, 0.6f), lampBrass);
            Decor(PrimitiveType.Cylinder, "Arm", lampModel, new Vector3(0f, 0.7f, 0f), new Vector3(0.07f, 0.65f, 0.07f), lampBrass);
            Decor(PrimitiveType.Sphere, "Shade", lampModel, new Vector3(0f, 1.4f, 0.15f), new Vector3(0.55f, 0.35f, 0.55f), lampBrass);
            Decor(PrimitiveType.Sphere, "Bulb", lampModel, new Vector3(0f, 1.3f, 0.2f), Vector3.one * 0.2f, lamp);
            deskLamp.pulseColor = new Color(1f, 0.9f, 0.6f);
            deskLamp.favouredDrops = new[] { ItemType.FireflyLantern, ItemType.SpareBattery };
            var lampPrefab = SaveEnemy(deskLamp);

            var pencil = Creature<SniperEnemy>("Pencil", 0.35f, 1.6f, 5, 0.9f, 2.4f, out var pencilModel);
            Decor(PrimitiveType.Cylinder, "Shaft", pencilModel, new Vector3(0f, 0.8f, 0f), new Vector3(0.22f, 0.7f, 0.22f), pencilYellow);
            Decor(PrimitiveType.Cylinder, "Eraser", pencilModel, new Vector3(0f, 1.55f, 0f), new Vector3(0.23f, 0.1f, 0.23f), stampRed);
            Decor(PrimitiveType.Cylinder, "Tip", pencilModel, new Vector3(0f, 0.08f, 0f), new Vector3(0.12f, 0.08f, 0.12f), graphite);
            Decor(PrimitiveType.Sphere, "Eye L", pencilModel, new Vector3(-0.06f, 1.2f, 0.1f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", pencilModel, new Vector3(0.06f, 1.2f, 0.1f), Vector3.one * 0.07f, dark);
            var pencilMuzzle = new GameObject("Muzzle").transform;
            pencilMuzzle.SetParent(pencil.transform, false);
            pencilMuzzle.localPosition = new Vector3(0f, 0.9f, 0.4f);
            pencil.muzzle = pencilMuzzle;
            pencil.projectilePrefab = leadPrefab;
            pencil.telegraphMaterial = telegraph;
            pencil.favouredDrops = new[] { ItemType.NeedleTip, ItemType.MagnifyingGlass };
            var pencilPrefab = SaveEnemy(pencil);

            var egg = Creature<LarvaEnemy>("SilverfishEgg", 0.3f, 0.5f, 2, 0.6f, 1.1f, out var eggModel);
            Decor(PrimitiveType.Sphere, "Egg", eggModel, new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.25f, 0.4f), silver);
            egg.growsInto = fishPrefab;
            egg.dropsLoot = false;
            egg.knockbackOnHit = 1f;
            Object.DestroyImmediate(egg.GetComponent<EnemyMutations>());
            var eggPrefab = (LarvaEnemy)SaveEnemy(egg);

            var bookend = Creature<BookBoss>("TheBookend", 0.9f, 2.6f, 240, 1f, 3.2f, out var bookendModel);
            Decor(PrimitiveType.Cube, "Upright", bookendModel, new Vector3(0f, 1.3f, 0f), new Vector3(1.4f, 2.6f, 0.35f), punchGrey);
            Decor(PrimitiveType.Cube, "Foot", bookendModel, new Vector3(0f, 0.1f, 0.6f), new Vector3(1.4f, 0.2f, 1.2f), punchGrey);
            Decor(PrimitiveType.Sphere, "Eye L", bookendModel, new Vector3(-0.3f, 2f, 0.2f), Vector3.one * 0.22f, lamp);
            Decor(PrimitiveType.Sphere, "Eye R", bookendModel, new Vector3(0.3f, 2f, 0.2f), Vector3.one * 0.22f, lamp);
            bookend.displayName = "The Bookend";
            bookend.subtitle = "Holds everything up. Has had enough of it.";
            bookend.canThrowPages = false;
            bookend.bookLength = 7f;
            bookend.restBetween = 1.2f;
            bookend.telegraphMaterial = telegraph;
            bookend.friends = new[] { fishPrefab };
            bookend.sampleReward = 12;
            bookend.knockbackOnHit = 0.4f;
            Object.DestroyImmediate(bookend.GetComponent<EnemyMutations>());
            var bookendPrefab = (BossEnemy)SaveEnemy(bookend);

            var tome = Creature<BookBoss>("TheEncyclopedia", 1.4f, 4.4f, 520, 1f, 5f, out var tomeModel);
            Decor(PrimitiveType.Cube, "Cover", tomeModel, new Vector3(0f, 2.2f, 0f), new Vector3(3f, 4.4f, 1.1f), bookGreen);
            Decor(PrimitiveType.Cube, "Pages", tomeModel, new Vector3(0.12f, 2.2f, 0f), new Vector3(2.8f, 4.2f, 1f), paper);
            Decor(PrimitiveType.Cube, "Spine", tomeModel, new Vector3(-1.5f, 2.2f, 0f), new Vector3(0.12f, 4.4f, 1.15f), bookBlue);
            for (int i = 0; i < 3; i++)
                Decor(PrimitiveType.Cube, "Band " + i, tomeModel, new Vector3(-1.52f, 1f + i * 1.3f, 0f), new Vector3(0.06f, 0.12f, 1.17f), gilt);
            Decor(PrimitiveType.Sphere, "Eye L", tomeModel, new Vector3(-0.5f, 3.2f, 0.58f), new Vector3(0.45f, 0.35f, 0.1f), lamp);
            Decor(PrimitiveType.Sphere, "Eye R", tomeModel, new Vector3(0.5f, 3.2f, 0.58f), new Vector3(0.45f, 0.35f, 0.1f), lamp);
            tome.displayName = "The Encyclopedia";
            tome.subtitle = "Volume one of one. It knows everything, and it is falling on you.";
            tome.projectilePrefab = pagePrefab;
            tome.telegraphMaterial = telegraph;
            tome.friends = new[] { potPrefab, stampPrefab };
            tome.sampleReward = 30;
            tome.knockbackOnHit = 0.1f;
            Object.DestroyImmediate(tome.GetComponent<EnemyMutations>());
            var tomePrefab = (BossEnemy)SaveEnemy(tome);

            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BookshelfPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, BookshelfPath);
            }

            biome.displayName = "The Bookshelf";
            biome.id = "bookshelf";
            biome.mutationTheme = "paper cuts, ink, a great deal of dust";
            biome.isBottom = false;
            biome.ledges = true;
            biome.ledgeTopMaterial = paper;
            biome.floorMaterial = shelfWood;
            biome.wallMaterial = spines;
            biome.doorMaterial = gilt;
            biome.wallHeight = 6f;
            biome.ceiling = true;
            biome.ceilingMaterial = shelfWood;
            biome.skyColor = new Color(0.1f, 0.07f, 0.05f);
            biome.ambientColor = new Color(0.62f, 0.55f, 0.45f);
            biome.sunIntensity = 0.85f;
            biome.fog = true;
            biome.fogColor = new Color(0.2f, 0.15f, 0.1f);
            biome.fogStart = 24f;
            biome.fogEnd = 80f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = bookBlue;            // books lying flat
            biome.crumbMaterial = paper;
            biome.sugarMaterial = gilt;
            biome.rootMaterial = spines;
            biome.podMaterial = silver;
            biome.podHatchling = eggPrefab;
            biome.dewMaterial = inkDrop;
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = lamp;
            biome.glowStemMaterial = lampBrass;
            biome.glowLightRange = 11f;
            biome.glowLightIntensity = 2f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = fishPrefab;
            biome.miniBoss = bookendPrefab;
            biome.boss = tomePrefab;
            // More that shoot than usual: with ledges about, it is a world for fighting up and down.
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = fishPrefab, weight = 4, minFloor = 1 },
                new EnemySpawnEntry { prefab = potPrefab, weight = 3, minFloor = 1 },
                new EnemySpawnEntry { prefab = punchPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = planePrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = stampPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = lampPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = pencilPrefab, weight = 2, minFloor = 5 },
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
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("BookshelfPortal", lamp, black, spines), "Props/BookshelfPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
