using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The third biome: Under the Sofa. Dark, dusty, and full of everything anybody ever lost. Like the cake it is
    /// built from the same parts for its four enemies, one per base behaviour. Its two bosses, Grandmother Dust and
    /// the Robot Vacuum, have fights of their own. Its portal opens once the Trick Candle has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string SofaPath = "Assets/Data/UnderTheSofa.asset";

        public static BiomeDefinition BuildSofa(BiomeDefinition ants)
        {
            var dust = ArenaSceneBuilder.Lit("Dust", new Color(0.62f, 0.6f, 0.58f), 0.05f);
            var dustDark = ArenaSceneBuilder.Lit("DustDark", new Color(0.4f, 0.38f, 0.37f), 0.05f);
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var white = ArenaSceneBuilder.Lit("Sugar", new Color(0.95f, 0.95f, 0.92f), 0.6f);
            var glassBlue = ArenaSceneBuilder.Lit("MarbleBlue", new Color(0.15f, 0.45f, 0.9f), 0.97f);
            var glassSwirl = ArenaSceneBuilder.Lit("MarbleSwirl", new Color(0.95f, 0.9f, 0.4f), 0.97f);
            var army = ArenaSceneBuilder.Lit("ArmyGreen", new Color(0.2f, 0.42f, 0.18f), 0.35f);
            var mothBody = ArenaSceneBuilder.Lit("MothBody", new Color(0.5f, 0.4f, 0.3f), 0.1f);
            var mothWing = ArenaSceneBuilder.Lit("MothWing", new Color(0.78f, 0.7f, 0.58f), 0.1f);
            var plastic = ArenaSceneBuilder.Lit("VacuumBody", new Color(0.13f, 0.14f, 0.16f), 0.75f);
            var plasticLight = ArenaSceneBuilder.Lit("VacuumTop", new Color(0.55f, 0.57f, 0.6f), 0.8f);
            var redEye = ArenaSceneBuilder.Unlit("VacuumEye", new Color(1f, 0.2f, 0.15f));
            var spark = ArenaSceneBuilder.Unlit("StaticSpark", new Color(1f, 0.95f, 0.4f));
            var pellet = ArenaSceneBuilder.Unlit("Pellet", new Color(0.55f, 0.95f, 0.4f));
            var carpet = ArenaSceneBuilder.Lit("Carpet", new Color(0.3f, 0.2f, 0.32f), 0.02f);
            var underside = ArenaSceneBuilder.Lit("SofaUnderside", new Color(0.23f, 0.18f, 0.18f), 0.05f);
            var wood = ArenaSceneBuilder.Lit("SofaWood", new Color(0.32f, 0.2f, 0.12f), 0.3f);
            var brick = ArenaSceneBuilder.Lit("ToyBrick", new Color(0.85f, 0.15f, 0.12f), 0.7f);
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            // ----- shots -----
            var shot = new GameObject("PelletShot");
            Decor(PrimitiveType.Sphere, "Visual", shot.transform, Vector3.zero, Vector3.one * 0.3f, pellet);
            shot.AddComponent<Bullet>().radius = 0.2f;
            var pelletPrefab = ArenaSceneBuilder.SavePrefab(shot, "Enemies/PelletShot").GetComponent<Bullet>();

            var dustShot = new GameObject("DustShot");
            Decor(PrimitiveType.Sphere, "Visual", dustShot.transform, Vector3.zero, Vector3.one * 0.6f, spark);
            dustShot.AddComponent<Bullet>().radius = 0.28f;
            var dustShotPrefab = ArenaSceneBuilder.SavePrefab(dustShot, "Enemies/DustShot").GetComponent<Bullet>();

            // ----- the four, one per base behaviour -----
            // Dust bunny: the Chaser. Soft, weak, and there is always another.
            var bunny = Creature<ChaserEnemy>("DustBunny", 0.5f, 1.3f, 5, 1f, 2.3f, out var bunnyModel);
            BunnyParts(bunnyModel, dust, dustDark, dark);
            bunny.favouredDrops = new[] { ItemType.StaticSock, ItemType.ExtraLegs };
            var bunnyPrefab = SaveEnemy(bunny);

            // Marble: the Charger. Somebody lost it in 1994 and it has been angry since.
            var marble = Creature<ChargerEnemy>("Marble", 0.8f, 1.6f, 10, 1.5f, 2.5f, out var marbleModel);
            Decor(PrimitiveType.Sphere, "Glass", marbleModel, new Vector3(0f, 0.8f, 0f), Vector3.one * 1.6f, glassBlue);
            Decor(PrimitiveType.Cylinder, "Swirl A", marbleModel, new Vector3(0f, 0.8f, 0f), new Vector3(1.62f, 0.1f, 1.62f), glassSwirl).transform.localRotation = Quaternion.Euler(35f, 20f, 0f);
            Decor(PrimitiveType.Cylinder, "Swirl B", marbleModel, new Vector3(0f, 0.8f, 0f), new Vector3(1.62f, 0.06f, 1.62f), white).transform.localRotation = Quaternion.Euler(-40f, 70f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", marbleModel, new Vector3(-0.28f, 1f, 0.7f), Vector3.one * 0.2f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", marbleModel, new Vector3(0.28f, 1f, 0.7f), Vector3.one * 0.2f, dark);
            marble.knockbackOnHit = 2f;
            marble.favouredDrops = new[] { ItemType.LostMarble, ItemType.RubberBand };
            marble.telegraphMaterial = telegraph;
            var marblePrefab = SaveEnemy(marble);

            // Toy soldier: the Shooter. Has been holding this position since it was dropped.
            var soldier = Creature<ShooterEnemy>("ToySoldier", 0.45f, 1.7f, 4, 0.9f, 2.4f, out var soldierModel);
            Decor(PrimitiveType.Cylinder, "Base", soldierModel, new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.04f, 0.6f), army);
            Decor(PrimitiveType.Cube, "Leg L", soldierModel, new Vector3(-0.15f, 0.4f, 0f), new Vector3(0.18f, 0.7f, 0.2f), army);
            Decor(PrimitiveType.Cube, "Leg R", soldierModel, new Vector3(0.15f, 0.4f, 0.1f), new Vector3(0.18f, 0.7f, 0.2f), army);
            Decor(PrimitiveType.Cube, "Torso", soldierModel, new Vector3(0f, 1f, 0f), new Vector3(0.5f, 0.6f, 0.3f), army);
            Decor(PrimitiveType.Sphere, "Head", soldierModel, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.32f, army);
            Decor(PrimitiveType.Sphere, "Helmet", soldierModel, new Vector3(0f, 1.53f, 0f), new Vector3(0.42f, 0.24f, 0.42f), army);
            Decor(PrimitiveType.Cube, "Rifle", soldierModel, new Vector3(0.2f, 1.1f, 0.45f), new Vector3(0.07f, 0.09f, 0.95f), army);
            var soldierMuzzle = new GameObject("Muzzle").transform;
            soldierMuzzle.SetParent(soldier.transform, false);
            soldierMuzzle.localPosition = new Vector3(0.2f, 1f, 0.95f);
            soldier.muzzle = soldierMuzzle;
            soldier.projectilePrefab = pelletPrefab;
            soldier.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.NeedleTip, ItemType.CompoundEye };
            var soldierPrefab = SaveEnemy(soldier);

            // Moth: the Flyer. Here for the sofa, staying for you.
            var moth = Creature<FlyerEnemy>("Moth", 0.45f, 2f, 3, 0.8f, 2.9f, out var mothModel);
            Decor(PrimitiveType.Sphere, "Body", mothModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.38f, 0.38f, 0.85f), mothBody);
            Decor(PrimitiveType.Sphere, "Head", mothModel, new Vector3(0f, 0.65f, 0.45f), Vector3.one * 0.3f, mothBody);
            Decor(PrimitiveType.Cube, "Feeler L", mothModel, new Vector3(-0.1f, 0.85f, 0.6f), new Vector3(0.03f, 0.03f, 0.35f), dark).transform.localRotation = Quaternion.Euler(-30f, -20f, 0f);
            Decor(PrimitiveType.Cube, "Feeler R", mothModel, new Vector3(0.1f, 0.85f, 0.6f), new Vector3(0.03f, 0.03f, 0.35f), dark).transform.localRotation = Quaternion.Euler(-30f, 20f, 0f);
            var mothWingLeft = Decor(PrimitiveType.Cube, "Wing L", mothModel, new Vector3(-0.65f, 0.8f, -0.05f), new Vector3(1.1f, 0.03f, 0.9f), mothWing).transform;
            mothWingLeft.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var mothWingRight = Decor(PrimitiveType.Cube, "Wing R", mothModel, new Vector3(0.65f, 0.8f, -0.05f), new Vector3(1.1f, 0.03f, 0.9f), mothWing).transform;
            mothWingRight.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var mothMotion = moth.gameObject.AddComponent<AntMotion>();
            mothMotion.legs = new Transform[0];
            mothMotion.wings = new[] { mothWingLeft, mothWingRight };
            moth.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            moth.favouredDrops = new[] { ItemType.WingBuds, ItemType.FireflyLantern };
            var mothPrefab = SaveEnemy(moth);

            // ----- two more, from floor 5 on -----
            // Popcorn: the Lobber. One piece that got away, and has been throwing kernels since.
            var kernel = ArenaSceneBuilder.Unlit("PopcornKernel", new Color(1f, 0.85f, 0.35f));
            var popcorn = Creature<LobberEnemy>("Popcorn", 0.5f, 1.4f, 6, 1f, 2.3f, out var popcornModel);
            Vector3[] puffs = { new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, 0.8f, 0.1f), new Vector3(-0.28f, 0.82f, -0.05f), new Vector3(0.05f, 1.05f, -0.1f), new Vector3(-0.1f, 0.7f, 0.3f) };
            for (int i = 0; i < puffs.Length; i++)
            {
                Decor(PrimitiveType.Sphere, "Puff " + i, popcornModel, puffs[i], Vector3.one * (0.62f - 0.05f * i), white);
            }
            Decor(PrimitiveType.Sphere, "Kernel", popcornModel, new Vector3(0f, 0.3f, -0.2f), new Vector3(0.4f, 0.3f, 0.4f), kernel);
            Decor(PrimitiveType.Sphere, "Eye L", popcornModel, new Vector3(-0.15f, 0.75f, 0.5f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", popcornModel, new Vector3(0.12f, 0.72f, 0.52f), Vector3.one * 0.1f, dark);
            popcorn.blastMaterial = telegraph;
            popcorn.lobMaterial = kernel;
            popcorn.favouredDrops = new[] { ItemType.BirthdayCandle, ItemType.FireflyLantern };
            var popcornPrefab = SaveEnemy(popcorn);

            // Remote control: the Support. It turns everything up.
            var remote = Creature<SupportEnemy>("RemoteControl", 0.5f, 1.6f, 4, 1f, 2.5f, out var remoteModel);
            Decor(PrimitiveType.Cube, "Case", remoteModel, new Vector3(0f, 0.8f, 0f), new Vector3(0.55f, 1.5f, 0.2f), plastic);
            Material[] buttons = { redEye, spark, pellet, glassBlue };
            for (int i = 0; i < 8; i++)
            {
                Decor(PrimitiveType.Sphere, "Button " + i, remoteModel, new Vector3(-0.13f + (i % 2) * 0.26f, 0.35f + (i / 2) * 0.24f, 0.11f), new Vector3(0.13f, 0.13f, 0.06f), buttons[i % buttons.Length]);
            }
            Decor(PrimitiveType.Sphere, "Light", remoteModel, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.16f, redEye);
            Decor(PrimitiveType.Sphere, "Eye L", remoteModel, new Vector3(-0.13f, 1.3f, 0.11f), Vector3.one * 0.12f, white);
            Decor(PrimitiveType.Sphere, "Eye R", remoteModel, new Vector3(0.13f, 1.3f, 0.11f), Vector3.one * 0.12f, white);
            remote.pulseColor = new Color(1f, 0.25f, 0.2f);
            remote.favouredDrops = new[] { ItemType.SpareBattery, ItemType.CompoundEye };
            var remotePrefab = SaveEnemy(remote);

            // ----- bosses and what they bring -----
            // Fluff: a clump that grows into a dust bunny if it is left alone. (The Brood Mother's larva.)
            var fluff = Creature<LarvaEnemy>("Fluff", 0.4f, 0.8f, 2, 0.6f, 1.4f, out var fluffModel);
            Decor(PrimitiveType.Sphere, "Clump", fluffModel, new Vector3(0f, 0.28f, 0f), new Vector3(0.6f, 0.5f, 0.6f), dust);
            Decor(PrimitiveType.Sphere, "Clump 2", fluffModel, new Vector3(0.18f, 0.22f, 0.12f), Vector3.one * 0.35f, dustDark);
            fluff.growsInto = bunnyPrefab;
            fluff.dropsLoot = false;
            fluff.knockbackOnHit = 1f;
            Object.DestroyImmediate(fluff.GetComponent<EnemyMutations>());
            var fluffPrefab = (LarvaEnemy)SaveEnemy(fluff);

            // Lint pile: the Robot Vacuum's "eggs". Leave one long enough and something climbs out.
            var lint = Creature<EggEnemy>("LintPile", 0.9f, 1.8f, 6, 1f, 2.4f, out var lintModel);
            Vector3[] heap = { new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.35f, 0.2f), new Vector3(-0.5f, 0.35f, 0.15f), new Vector3(0.05f, 0.35f, -0.5f), new Vector3(0f, 0.95f, 0f) };
            for (int i = 0; i < heap.Length; i++)
            {
                Decor(PrimitiveType.Sphere, "Lint " + i, lintModel, heap[i], new Vector3(0.85f, 0.75f, 0.85f), i % 2 == 0 ? dust : dustDark);
            }
            lint.dropsLoot = false;
            lint.knockbackOnHit = 0f;
            Object.DestroyImmediate(lint.GetComponent<EnemyMutations>());
            var lintPrefab = (EggEnemy)SaveEnemy(lint);

            // Grandmother Dust (floor 5): a dust bunny the size of a car, with a soft white tail. The tail is the weak point.
            var fluffShot = new GameObject("FluffShot");
            Decor(PrimitiveType.Sphere, "Visual", fluffShot.transform, Vector3.zero, Vector3.one * 0.55f, dust);
            fluffShot.AddComponent<Bullet>().radius = 0.27f;
            var fluffShotPrefab = ArenaSceneBuilder.SavePrefab(fluffShot, "Enemies/FluffShot").GetComponent<Bullet>();

            var granny = Creature<GrandmotherDust>("GrandmotherDust", 1.65f, 3.6f, 150, 3f, 4.8f, out var grannyModel);
            grannyModel.localScale = Vector3.one * 2.7f;
            BunnyParts(grannyModel, dustDark, dust, dark);
            Decor(PrimitiveType.Sphere, "Tail", grannyModel, new Vector3(0f, 0.55f, -0.6f), Vector3.one * 0.5f, white);
            granny.displayName = "Grandmother Dust";
            granny.subtitle = "Has been under here since the sofa was new.";
            granny.fluffPrefab = fluffPrefab;
            granny.projectilePrefab = fluffShotPrefab;
            granny.stickyMaterial = ArenaSceneBuilder.Unlit("LintPatch", new Color(0.55f, 0.53f, 0.5f));
            granny.telegraphMaterial = telegraph;
            granny.sampleReward = 12;
            granny.knockbackOnHit = 0f;
            var grannyPrefab = (BossEnemy)SaveEnemy(granny);

            // The Robot Vacuum (floor 10): its own fight. Drives, sprays sparks, sucks, and has to stop to empty its bin.
            var vacuum = Creature<RobotVacuum>("RobotVacuum", 1.9f, 4f, 280, 3.4f, 4.6f, out var vacuumModel);
            Decor(PrimitiveType.Cylinder, "Body", vacuumModel, new Vector3(0f, 0.6f, 0f), new Vector3(4.2f, 0.5f, 4.2f), plastic);
            // The lid and everything standing on it lift together when the bin opens.
            var lid = new GameObject("Lid").transform;
            lid.SetParent(vacuumModel, false);
            lid.localPosition = new Vector3(0f, 1.14f, 0f);
            vacuum.lid = lid;
            Decor(PrimitiveType.Cylinder, "Plate", lid, Vector3.zero, new Vector3(3.5f, 0.05f, 3.5f), plasticLight);
            // What shows when the lid is up: the full bin.
            Decor(PrimitiveType.Cylinder, "Bin", vacuumModel, new Vector3(0f, 1.1f, 0f), new Vector3(3f, 0.04f, 3f), dustDark);
            Decor(PrimitiveType.Cube, "Bumper", vacuumModel, new Vector3(0f, 0.6f, 1.85f), new Vector3(2.8f, 0.7f, 0.5f), plasticLight);
            Decor(PrimitiveType.Cylinder, "Tower", lid, new Vector3(0f, 0.56f, 0f), new Vector3(1.3f, 0.55f, 1.3f), plastic);
            Decor(PrimitiveType.Sphere, "Eye", lid, new Vector3(0f, 0.61f, 0.6f), Vector3.one * 0.5f, redEye);
            Decor(PrimitiveType.Cylinder, "Button", lid, new Vector3(0f, 1.14f, 0f), new Vector3(0.6f, 0.04f, 0.6f), spark);
            Decor(PrimitiveType.Cube, "Brush L", vacuumModel, new Vector3(-1.5f, 0.15f, 1.5f), new Vector3(1.1f, 0.06f, 0.12f), white).transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
            Decor(PrimitiveType.Cube, "Brush R", vacuumModel, new Vector3(1.5f, 0.15f, 1.5f), new Vector3(1.1f, 0.06f, 0.12f), white).transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
            vacuum.displayName = "The Robot Vacuum";
            vacuum.subtitle = "Scheduled for 9 a.m. It is 9 a.m.";
            vacuum.projectilePrefab = dustShotPrefab;
            vacuum.lintPrefab = lintPrefab;
            vacuum.telegraphMaterial = telegraph;
            vacuum.sparkMaterial = spark;
            vacuum.sampleReward = 30;
            vacuum.knockbackOnHit = 0f;
            var vacuumPrefab = (BossEnemy)SaveEnemy(vacuum);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(SofaPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, SofaPath);
            }

            biome.displayName = "Under the Sofa";
            biome.id = "sofa";
            biome.mutationTheme = "static, sticky lint";
            biome.isBottom = false;
            biome.floorMaterial = carpet;
            biome.wallMaterial = underside;
            biome.doorMaterial = wood;
            biome.wallHeight = 6f;
            // The sofa is the roof. What light there is comes in low and yellow from the room outside.
            biome.ceiling = true;
            biome.ceilingMaterial = underside;
            biome.skyColor = new Color(0.03f, 0.025f, 0.03f);
            biome.ambientColor = new Color(0.62f, 0.55f, 0.5f);
            biome.sunIntensity = 0.7f;
            biome.fog = true;
            biome.fogColor = new Color(0.03f, 0.025f, 0.03f);
            biome.fogStart = 20f;
            biome.fogEnd = 80f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = brick;            // lost toy bricks
            biome.crumbMaterial = ants.crumbMaterial; // crumbs get everywhere
            biome.sugarMaterial = white;            // one lost die
            biome.rootMaterial = wood;              // the sofa's legs and slats
            biome.podMaterial = dust;
            biome.podHatchling = fluffPrefab;
            biome.dewMaterial = ArenaSceneBuilder.Lit("SpiltJuice", new Color(0.6f, 0.2f, 0.7f), 0.9f);
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = ArenaSceneBuilder.Unlit("LostTorch", new Color(1f, 0.9f, 0.55f));
            biome.glowStemMaterial = plastic;
            biome.glowLightRange = 12f;
            biome.glowLightIntensity = 4f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = bunnyPrefab;
            biome.miniBoss = grannyPrefab;
            biome.boss = vacuumPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = bunnyPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = soldierPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = marblePrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = mothPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = popcornPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = remotePrefab, weight = 1, minFloor = 6 },
                // The mimic is the ants' own prefab: coins are coins everywhere.
                new EnemySpawnEntry { prefab = ants.EnemyNamed("Mimic"), weight = 1, minFloor = 4 },
            };
            var sticky = AssetDatabase.LoadAssetAtPath<MutationDefinition>("Assets/Data/Mutations/" + MutationType.Sticky + ".asset");
            var mutations = new System.Collections.Generic.List<MutationDefinition>
            {
                ants.Mutation(MutationType.Fast), ants.Mutation(MutationType.Armored), ants.Mutation(MutationType.Regenerates),
                ants.Mutation(MutationType.ExplodesOnDeath), ants.Mutation(MutationType.SwarmCall), ants.Mutation(MutationType.HardenedShell),
            };
            if (sticky != null) mutations.Add(sticky);
            mutations.Add(Mutation(MutationType.Static, "Static", 2, "Every few seconds it shocks anything standing close.", new Color(1f, 0.95f, 0.4f)));
            biome.mutations = mutations.ToArray();

            biome.startProp = ants.startProp;
            biome.exitProp = ants.exitProp;
            biome.treasureProp = ants.treasureProp;
            biome.shopProp = ants.shopProp;
            biome.extractPortalProp = ants.extractPortalProp;
            biome.deeperPortalProp = ants.deeperPortalProp;
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("SofaPortal", ArenaSceneBuilder.Unlit("LostTorch", new Color(1f, 0.9f, 0.55f)), black, wood), "Props/SofaPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }

        // A dust bunny, about 1.2 m tall: a ball of fluff with ears. Used small for the bunnies and large for Grandmother Dust.
        static void BunnyParts(Transform model, Material fluff, Material other, Material dark)
        {
            Decor(PrimitiveType.Sphere, "Fluff", model, new Vector3(0f, 0.5f, 0f), new Vector3(1f, 0.9f, 1.05f), fluff);
            Decor(PrimitiveType.Sphere, "Fluff Back", model, new Vector3(0.12f, 0.62f, -0.3f), Vector3.one * 0.7f, other);
            Decor(PrimitiveType.Sphere, "Fluff Side", model, new Vector3(-0.3f, 0.4f, -0.1f), Vector3.one * 0.6f, other);
            Decor(PrimitiveType.Sphere, "Fluff Top", model, new Vector3(0f, 0.85f, 0.1f), Vector3.one * 0.55f, fluff);
            Decor(PrimitiveType.Capsule, "Ear L", model, new Vector3(-0.17f, 1.2f, 0.05f), new Vector3(0.16f, 0.3f, 0.1f), fluff).transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            Decor(PrimitiveType.Capsule, "Ear R", model, new Vector3(0.17f, 1.2f, 0.05f), new Vector3(0.16f, 0.3f, 0.1f), fluff).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Decor(PrimitiveType.Sphere, "Eye L", model, new Vector3(-0.17f, 0.68f, 0.44f), Vector3.one * 0.11f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", model, new Vector3(0.17f, 0.68f, 0.44f), Vector3.one * 0.11f, dark);
        }
    }
}
