using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The fifth world, and the first that is not in a house: The Library. Warm, dim and brown, floored in wood and
    /// walled in books. Six enemies on the six behaviours, the Trainee half way and the Librarian at the end, whose
    /// fight is about when not to shoot. Its portal opens once the Ice Maker has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string LibraryPath = "Assets/Data/TheLibrary.asset";

        public static BiomeDefinition BuildLibrary(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var paper = ArenaSceneBuilder.Lit("LibraryPaper", new Color(0.93f, 0.88f, 0.74f), 0.05f);
            var silver = ArenaSceneBuilder.Lit("Silverfish", new Color(0.72f, 0.76f, 0.8f), 0.85f);
            var ink = ArenaSceneBuilder.Lit("InkGlass", new Color(0.12f, 0.14f, 0.3f), 0.9f);
            var inkShot = ArenaSceneBuilder.Unlit("InkBlot", new Color(0.2f, 0.25f, 0.75f));
            var leatherRed = ArenaSceneBuilder.Lit("LeatherRed", new Color(0.55f, 0.14f, 0.12f), 0.35f);
            var leatherGreen = ArenaSceneBuilder.Lit("LeatherGreen", new Color(0.15f, 0.36f, 0.24f), 0.35f);
            var gilt = ArenaSceneBuilder.Unlit("Gilt", new Color(1f, 0.82f, 0.3f));
            var steel = ArenaSceneBuilder.Lit("StaplerSteel", new Color(0.3f, 0.32f, 0.36f), 0.8f);
            var staple = ArenaSceneBuilder.Unlit("Staple", new Color(0.85f, 0.88f, 0.92f));
            var ribbon = ArenaSceneBuilder.Lit("BookmarkRibbon", new Color(0.75f, 0.2f, 0.5f), 0.3f);
            var cardigan = ArenaSceneBuilder.Lit("Cardigan", new Color(0.4f, 0.3f, 0.45f), 0.1f);
            var skin = ArenaSceneBuilder.Lit("LibrarianSkin", new Color(0.86f, 0.7f, 0.58f), 0.3f);
            var hair = ArenaSceneBuilder.Lit("LibrarianHair", new Color(0.8f, 0.8f, 0.82f), 0.2f);
            var glasses = ArenaSceneBuilder.Unlit("LibrarianGlasses", new Color(1f, 0.9f, 0.6f));
            var wood = ArenaSceneBuilder.Lit("LibraryFloor", new Color(0.42f, 0.27f, 0.15f), 0.45f);
            var shelves = ArenaSceneBuilder.Lit("LibraryShelves", new Color(0.3f, 0.17f, 0.1f), 0.25f);
            var lamp = ArenaSceneBuilder.Unlit("ReadingLamp", new Color(0.6f, 1f, 0.55f));
            var brass = ArenaSceneBuilder.Lit("LampBrass", new Color(0.7f, 0.55f, 0.25f), 0.8f);
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var blot = new GameObject("InkBlot");
            Decor(PrimitiveType.Sphere, "Visual", blot.transform, Vector3.zero, new Vector3(0.34f, 0.34f, 0.5f), inkShot);
            blot.AddComponent<Bullet>().radius = 0.22f;
            var blotPrefab = ArenaSceneBuilder.SavePrefab(blot, "Enemies/InkBlot").GetComponent<Bullet>();

            // ----- the six -----
            // Silverfish: the Chaser. It has read everything in here, by eating it.
            var fish = Creature<ChaserEnemy>("Silverfish", 0.5f, 1f, 5, 1f, 1.9f, out var fishModel);
            Decor(PrimitiveType.Sphere, "Body", fishModel, new Vector3(0f, 0.35f, -0.2f), new Vector3(0.6f, 0.4f, 1.6f), silver);
            Decor(PrimitiveType.Sphere, "Head", fishModel, new Vector3(0f, 0.4f, 0.6f), new Vector3(0.5f, 0.38f, 0.5f), silver);
            Decor(PrimitiveType.Cube, "Feeler L", fishModel, new Vector3(-0.2f, 0.5f, 1f), new Vector3(0.03f, 0.03f, 0.6f), dark).transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
            Decor(PrimitiveType.Cube, "Feeler R", fishModel, new Vector3(0.2f, 0.5f, 1f), new Vector3(0.03f, 0.03f, 0.6f), dark).transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", fishModel, new Vector3(-0.15f, 0.5f, 0.8f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", fishModel, new Vector3(0.15f, 0.5f, 0.8f), Vector3.one * 0.1f, dark);
            fish.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.RubberBand };
            var fishPrefab = SaveEnemy(fish);

            // Encyclopedia: the Charger. Volume nine, and it means to be consulted.
            var tome = Creature<ChargerEnemy>("Encyclopedia", 0.8f, 1.7f, 10, 1.5f, 2.6f, out var tomeModel);
            Decor(PrimitiveType.Cube, "Cover", tomeModel, new Vector3(0f, 0.85f, 0f), new Vector3(1.3f, 1.6f, 0.6f), leatherRed);
            Decor(PrimitiveType.Cube, "Pages", tomeModel, new Vector3(0f, 0.85f, 0.04f), new Vector3(1.15f, 1.45f, 0.5f), paper);
            Decor(PrimitiveType.Cube, "Band", tomeModel, new Vector3(0f, 1.25f, -0.31f), new Vector3(1.3f, 0.12f, 0.02f), gilt);
            Decor(PrimitiveType.Sphere, "Eye L", tomeModel, new Vector3(-0.28f, 1.1f, 0.31f), Vector3.one * 0.2f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", tomeModel, new Vector3(0.28f, 1.1f, 0.31f), Vector3.one * 0.2f, dark);
            tome.knockbackOnHit = 2f;
            tome.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.Lunchbox };
            tome.telegraphMaterial = telegraph;
            var tomePrefab = SaveEnemy(tome);

            // Ink pot: the Shooter. Blue-black, and it does not wash out.
            var pot = Creature<ShooterEnemy>("InkPot", 0.45f, 1.3f, 4, 0.9f, 2.1f, out var potModel);
            Decor(PrimitiveType.Cylinder, "Glass", potModel, new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.45f, 0.8f), ink);
            Decor(PrimitiveType.Cylinder, "Neck", potModel, new Vector3(0f, 1f, 0f), new Vector3(0.4f, 0.12f, 0.4f), ink);
            Decor(PrimitiveType.Cube, "Label", potModel, new Vector3(0f, 0.45f, 0.4f), new Vector3(0.5f, 0.4f, 0.03f), paper);
            Decor(PrimitiveType.Sphere, "Eye L", potModel, new Vector3(-0.15f, 0.75f, 0.36f), Vector3.one * 0.13f, paper);
            Decor(PrimitiveType.Sphere, "Eye R", potModel, new Vector3(0.15f, 0.75f, 0.36f), Vector3.one * 0.13f, paper);
            var potMuzzle = new GameObject("Muzzle").transform;
            potMuzzle.SetParent(pot.transform, false);
            potMuzzle.localPosition = new Vector3(0f, 1f, 0.7f);
            pot.muzzle = potMuzzle;
            pot.projectilePrefab = blotPrefab;
            pot.favouredDrops = new[] { ItemType.NeedleTip, ItemType.AcidGland };
            var potPrefab = SaveEnemy(pot);

            // Paper plane: the Flyer. Somebody's homework, folded in anger.
            var plane = Creature<FlyerEnemy>("PaperPlane", 0.45f, 2f, 3, 0.8f, 2.9f, out var planeModel);
            Decor(PrimitiveType.Cube, "Keel", planeModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.08f, 0.25f, 1.2f), paper);
            var planeLeft = Decor(PrimitiveType.Cube, "Wing L", planeModel, new Vector3(-0.38f, 0.75f, -0.1f), new Vector3(0.75f, 0.03f, 1.1f), paper).transform;
            planeLeft.localRotation = Quaternion.Euler(0f, 0f, -12f);
            var planeRight = Decor(PrimitiveType.Cube, "Wing R", planeModel, new Vector3(0.38f, 0.75f, -0.1f), new Vector3(0.75f, 0.03f, 1.1f), paper).transform;
            planeRight.localRotation = Quaternion.Euler(0f, 0f, 12f);
            Decor(PrimitiveType.Sphere, "Eye L", planeModel, new Vector3(-0.12f, 0.8f, 0.5f), Vector3.one * 0.12f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", planeModel, new Vector3(0.12f, 0.8f, 0.5f), Vector3.one * 0.12f, dark);
            var planeMotion = plane.gameObject.AddComponent<AntMotion>();
            planeMotion.legs = new Transform[0];
            planeMotion.wings = new[] { planeLeft, planeRight };
            plane.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            plane.favouredDrops = new[] { ItemType.WingBuds, ItemType.StaticSock };
            var planePrefab = SaveEnemy(plane);

            // Stapler: the Lobber. It throws staples over the shelves, at where you are going.
            var stapler = Creature<LobberEnemy>("Stapler", 0.5f, 1.2f, 6, 1f, 2.1f, out var staplerModel);
            Decor(PrimitiveType.Cube, "Base", staplerModel, new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.25f, 1.5f), steel);
            Decor(PrimitiveType.Cube, "Arm", staplerModel, new Vector3(0f, 0.7f, 0.1f), new Vector3(0.55f, 0.3f, 1.4f), leatherRed).transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", staplerModel, new Vector3(-0.17f, 0.95f, 0.6f), Vector3.one * 0.14f, paper);
            Decor(PrimitiveType.Sphere, "Eye R", staplerModel, new Vector3(0.17f, 0.95f, 0.6f), Vector3.one * 0.14f, paper);
            stapler.blastMaterial = telegraph;
            stapler.lobMaterial = staple;
            stapler.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.SpareBattery };
            var staplerPrefab = SaveEnemy(stapler);

            // Bookmark: the Support. It keeps everybody's place, and hurries them along from it.
            var mark = Creature<SupportEnemy>("Bookmark", 0.5f, 1.6f, 4, 1f, 2.5f, out var markModel);
            Decor(PrimitiveType.Cube, "Ribbon", markModel, new Vector3(0f, 0.85f, 0f), new Vector3(0.5f, 1.5f, 0.08f), ribbon);
            Decor(PrimitiveType.Cube, "Tassel", markModel, new Vector3(0f, 1.7f, 0f), new Vector3(0.12f, 0.3f, 0.12f), gilt);
            Decor(PrimitiveType.Sphere, "Eye L", markModel, new Vector3(-0.12f, 1.2f, 0.06f), Vector3.one * 0.12f, paper);
            Decor(PrimitiveType.Sphere, "Eye R", markModel, new Vector3(0.12f, 1.2f, 0.06f), Vector3.one * 0.12f, paper);
            mark.pulseColor = new Color(1f, 0.6f, 0.85f);
            mark.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.LostMarble };
            var markPrefab = SaveEnemy(mark);

            // Fountain pen: the Sniper. It rules a line to you first, and then signs it.
            var pen = Creature<SniperEnemy>("FountainPen", 0.45f, 1.4f, 5, 0.9f, 2.2f, out var penModel);
            Decor(PrimitiveType.Cylinder, "Barrel", penModel, new Vector3(0f, 0.75f, -0.1f), new Vector3(0.28f, 0.7f, 0.28f), ink).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Decor(PrimitiveType.Cylinder, "Band", penModel, new Vector3(0f, 0.75f, 0.35f), new Vector3(0.3f, 0.05f, 0.3f), gilt).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Decor(PrimitiveType.Cube, "Nib", penModel, new Vector3(0f, 0.75f, 0.8f), new Vector3(0.12f, 0.04f, 0.4f), gilt);
            Decor(PrimitiveType.Cube, "Foot", penModel, new Vector3(0f, 0.2f, -0.1f), new Vector3(0.3f, 0.4f, 0.5f), steel);
            var penMuzzle = new GameObject("Muzzle").transform;
            penMuzzle.SetParent(pen.transform, false);
            penMuzzle.localPosition = new Vector3(0f, 0.75f, 1.1f);
            pen.muzzle = penMuzzle;
            pen.projectilePrefab = blotPrefab;
            pen.telegraphMaterial = telegraph;
            pen.favouredDrops = new[] { ItemType.NeedleTip, ItemType.CompoundEye };
            var penPrefab = SaveEnemy(pen);

            // Bookworm: grows into a silverfish if it is left. (What the pods hatch.)
            var worm = Creature<LarvaEnemy>("Bookworm", 0.4f, 0.7f, 2, 0.6f, 1.3f, out var wormModel);
            Decor(PrimitiveType.Sphere, "Body", wormModel, new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.35f, 0.9f), paper);
            Decor(PrimitiveType.Sphere, "Head", wormModel, new Vector3(0f, 0.3f, 0.45f), Vector3.one * 0.32f, leatherGreen);
            worm.growsInto = fishPrefab;
            worm.dropsLoot = false;
            worm.knockbackOnHit = 1f;
            Object.DestroyImmediate(worm.GetComponent<EnemyMutations>());
            var wormPrefab = (LarvaEnemy)SaveEnemy(worm);

            // ----- bosses: the same person at two points in a career -----
            var trainee = Person("TheTrainee", 0.6f, 2.2f, 70, 3.1f, 1.2f, leatherGreen, skin, dark, glasses, paper, leatherGreen);
            trainee.displayName = "The Trainee";
            trainee.subtitle = "Started Monday. Has been told the books go back on the shelf, or at you.";
            trainee.canShush = false;
            // New, and not quick on their feet: closer in and slower than the Librarian, so they can be hit.
            trainee.keepRange = 6.5f;
            trainee.moveSpeed = 1.8f;
            trainee.booksPerRun = new[] { 2, 2, 3 };
            trainee.telegraphMaterial = telegraph;
            trainee.friends = new[] { fishPrefab };
            trainee.sampleReward = 12;
            trainee.knockbackOnHit = 1f;
            Object.DestroyImmediate(trainee.GetComponent<EnemyMutations>());
            var traineePrefab = (BossEnemy)SaveEnemy(trainee);

            var librarian = Person("TheLibrarian", 0.8f, 3.2f, 260, 4.4f, 1.75f, cardigan, skin, hair, glasses, paper, leatherRed);
            librarian.displayName = "The Librarian";
            librarian.subtitle = "This is a library. You will find that out when you make a noise.";
            librarian.telegraphMaterial = telegraph;
            librarian.friends = new[] { markPrefab, planePrefab };
            librarian.sampleReward = 30;
            librarian.knockbackOnHit = 0.5f;
            Object.DestroyImmediate(librarian.GetComponent<EnemyMutations>());
            var librarianPrefab = (BossEnemy)SaveEnemy(librarian);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(LibraryPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, LibraryPath);
            }

            biome.displayName = "The Library";
            biome.id = "library";
            biome.mutationTheme = "speed, shells, things long overdue";
            biome.isBottom = false;
            biome.floorMaterial = wood;
            biome.wallMaterial = shelves;
            biome.doorMaterial = leatherGreen;
            biome.wallHeight = 6f;
            // Closed in, warm and dim, lit by green reading lamps.
            biome.ceiling = true;
            biome.ceilingMaterial = shelves;
            biome.skyColor = new Color(0.08f, 0.05f, 0.03f);
            biome.ambientColor = new Color(0.62f, 0.5f, 0.38f);
            biome.sunIntensity = 0.75f;
            biome.fog = true;
            biome.fogColor = new Color(0.1f, 0.07f, 0.04f);
            biome.fogStart = 24f;
            biome.fogEnd = 85f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = leatherRed;          // books somebody left out
            biome.crumbMaterial = paper;               // loose pages
            biome.sugarMaterial = leatherGreen;
            biome.rootMaterial = brass;                // lamp stands
            biome.podMaterial = paper;                 // returned parcels
            biome.podHatchling = wormPrefab;
            biome.dewMaterial = ink;                   // ink drips
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = lamp;
            biome.glowStemMaterial = brass;
            biome.glowLightRange = 12f;
            biome.glowLightIntensity = 2.2f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = fishPrefab;
            biome.miniBoss = traineePrefab;
            biome.boss = librarianPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = fishPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = potPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = tomePrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = planePrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = staplerPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = markPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = penPrefab, weight = 1, minFloor = 5 },
                new EnemySpawnEntry { prefab = ants.EnemyNamed("Mimic"), weight = 1, minFloor = 4 },
            };
            var mutations = new System.Collections.Generic.List<MutationDefinition>
            {
                ants.Mutation(MutationType.Fast), ants.Mutation(MutationType.Armored), ants.Mutation(MutationType.Regenerates),
                ants.Mutation(MutationType.SwarmCall), ants.Mutation(MutationType.HardenedShell), ants.Mutation(MutationType.AcidBlood),
            };
            var borrowed = AssetDatabase.LoadAssetAtPath<MutationDefinition>("Assets/Data/Mutations/" + MutationType.Static + ".asset");
            if (borrowed != null) mutations.Add(borrowed);
            mutations.Add(Mutation(MutationType.Sequel, "Sequel", 2, "Put it down and two silverfish turn up in its place.", new Color(0.93f, 0.88f, 0.74f)));
            mutations.RemoveAll(mutation => mutation == null);
            biome.mutations = mutations.ToArray();

            biome.startProp = ants.startProp;
            biome.exitProp = ants.exitProp;
            biome.treasureProp = ants.treasureProp;
            biome.shopProp = ants.shopProp;
            biome.extractPortalProp = ants.extractPortalProp;
            biome.deeperPortalProp = ants.deeperPortalProp;
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("LibraryPortal", lamp, black, brass), "Props/LibraryPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }

        // Somebody who works here: legs, a cardigan, a head, glasses, and a book held ready.
        static LibrarianBoss Person(string name, float radius, float height, int health, float badge, float size,
            Material top, Material skin, Material hair, Material glasses, Material paper, Material book)
        {
            var person = Creature<LibrarianBoss>(name, radius, height, health, 1f, badge, out var model);
            model.localScale = Vector3.one * size;
            Decor(PrimitiveType.Cube, "Skirt", model, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.4f), hair);
            Decor(PrimitiveType.Cube, "Top", model, new Vector3(0f, 1.15f, 0f), new Vector3(0.7f, 0.6f, 0.42f), top);
            Decor(PrimitiveType.Cube, "Arm L", model, new Vector3(-0.45f, 1.1f, 0.05f), new Vector3(0.18f, 0.6f, 0.2f), top);
            Decor(PrimitiveType.Cube, "Arm R", model, new Vector3(0.45f, 1.25f, 0.25f), new Vector3(0.18f, 0.2f, 0.6f), top);
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 1.65f, 0f), Vector3.one * 0.42f, skin);
            Decor(PrimitiveType.Sphere, "Bun", model, new Vector3(0f, 1.85f, -0.12f), Vector3.one * 0.26f, hair);
            Decor(PrimitiveType.Cube, "Glasses", model, new Vector3(0f, 1.68f, 0.2f), new Vector3(0.36f, 0.09f, 0.04f), glasses);
            Decor(PrimitiveType.Cube, "Book", model, new Vector3(0.45f, 1.3f, 0.62f), new Vector3(0.3f, 0.4f, 0.1f), book);
            Decor(PrimitiveType.Cube, "Book Pages", model, new Vector3(0.45f, 1.3f, 0.63f), new Vector3(0.26f, 0.36f, 0.1f), paper);
            return person;
        }
    }
}
