using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The tenth world: The Toy Box. Painted wood underfoot, walls of stacked bricks in primary colours, lit like a
    /// nursery. Seven enemies on the seven behaviours, the Wind-up Mouse half way and the Jack-in-the-Box at the end,
    /// whose fight is circles to get out of and a box that could be any of them. Its portal opens once the Goldfish
    /// has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string ToyBoxPath = "Assets/Data/TheToyBox.asset";

        public static BiomeDefinition BuildToyBox(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var painted = ArenaSceneBuilder.Lit("PaintedWood", new Color(0.85f, 0.72f, 0.5f), 0.4f);
            var brickRed = ArenaSceneBuilder.Lit("ToyRed", new Color(0.85f, 0.15f, 0.15f), 0.5f);
            var brickBlue = ArenaSceneBuilder.Lit("ToyBlue", new Color(0.15f, 0.35f, 0.85f), 0.5f);
            var brickYellow = ArenaSceneBuilder.Lit("ToyYellow", new Color(0.95f, 0.8f, 0.15f), 0.5f);
            var brickGreen = ArenaSceneBuilder.Lit("ToyGreen", new Color(0.2f, 0.7f, 0.3f), 0.5f);
            var tin = ArenaSceneBuilder.Lit("Tin", new Color(0.6f, 0.62f, 0.66f), 0.85f);
            var felt = ArenaSceneBuilder.Lit("Felt", new Color(0.55f, 0.55f, 0.58f), 0.05f);
            var pink = ArenaSceneBuilder.Lit("ToyPink", new Color(1f, 0.6f, 0.75f), 0.4f);
            var white = ArenaSceneBuilder.Lit("ToyWhite", new Color(0.95f, 0.95f, 0.92f), 0.4f);
            var key = ArenaSceneBuilder.Lit("WindingKey", new Color(0.8f, 0.65f, 0.3f), 0.85f);
            var dart = ArenaSceneBuilder.Unlit("DartOrange", new Color(1f, 0.5f, 0.1f));
            var ball = ArenaSceneBuilder.Unlit("ToyBall", new Color(1f, 0.3f, 0.5f));
            var glow = ArenaSceneBuilder.Unlit("NightLight", new Color(1f, 0.9f, 0.6f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var dartShot = new GameObject("DartShot");
            Decor(PrimitiveType.Cube, "Visual", dartShot.transform, Vector3.zero, new Vector3(0.12f, 0.12f, 0.55f), dart);
            dartShot.AddComponent<Bullet>().radius = 0.18f;
            var dartPrefab = ArenaSceneBuilder.SavePrefab(dartShot, "Enemies/DartShot").GetComponent<Bullet>();

            var pellet = new GameObject("ToyPellet");
            Decor(PrimitiveType.Sphere, "Visual", pellet.transform, Vector3.zero, Vector3.one * 0.3f, ball);
            pellet.AddComponent<Bullet>().radius = 0.2f;
            var pelletPrefab = ArenaSceneBuilder.SavePrefab(pellet, "Enemies/ToyPellet").GetComponent<Bullet>();

            // ----- the seven -----
            // Toy car: the Chaser. Pull it back, let it go.
            var car = Creature<ChaserEnemy>("ToyCar", 0.5f, 0.9f, 5, 1f, 1.8f, out var carModel);
            Decor(PrimitiveType.Cube, "Body", carModel, new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.35f, 1.1f), brickRed);
            Decor(PrimitiveType.Cube, "Cab", carModel, new Vector3(0f, 0.65f, -0.1f), new Vector3(0.55f, 0.3f, 0.5f), white);
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Cylinder, "Wheel " + i, carModel, new Vector3(i < 2 ? -0.38f : 0.38f, 0.18f, i % 2 == 0 ? 0.35f : -0.35f), new Vector3(0.28f, 0.06f, 0.28f), dark).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Decor(PrimitiveType.Sphere, "Eye L", carModel, new Vector3(-0.15f, 0.45f, 0.56f), Vector3.one * 0.12f, white);
            Decor(PrimitiveType.Sphere, "Eye R", carModel, new Vector3(0.15f, 0.45f, 0.56f), Vector3.one * 0.12f, white);
            car.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.ColdCoffee };
            var carPrefab = SaveEnemy(car);

            // Building block: the Charger. Has one face for every side, and every side is hard.
            var block = Creature<ChargerEnemy>("BuildingBlock", 0.75f, 1.5f, 10, 1.5f, 2.3f, out var blockModel);
            Decor(PrimitiveType.Cube, "Block", blockModel, new Vector3(0f, 0.75f, 0f), Vector3.one * 1.4f, brickBlue);
            Decor(PrimitiveType.Cube, "Letter", blockModel, new Vector3(0f, 0.75f, 0.71f), new Vector3(0.6f, 0.8f, 0.02f), brickYellow);
            Decor(PrimitiveType.Sphere, "Eye L", blockModel, new Vector3(-0.3f, 1.15f, 0.72f), Vector3.one * 0.16f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", blockModel, new Vector3(0.3f, 1.15f, 0.72f), Vector3.one * 0.16f, dark);
            block.knockbackOnHit = 1.5f;
            block.telegraphMaterial = telegraph;
            block.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.Lunchbox };
            var blockPrefab = SaveEnemy(block);

            // Tin robot: the Shooter. Batteries not included; it found some.
            var robot = Creature<ShooterEnemy>("TinRobot", 0.5f, 1.6f, 4, 0.9f, 2.4f, out var robotModel);
            Decor(PrimitiveType.Cube, "Body", robotModel, new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 0.8f, 0.5f), tin);
            Decor(PrimitiveType.Cube, "Head", robotModel, new Vector3(0f, 1.35f, 0f), new Vector3(0.5f, 0.4f, 0.45f), tin);
            Decor(PrimitiveType.Cube, "Antenna", robotModel, new Vector3(0f, 1.65f, 0f), new Vector3(0.04f, 0.25f, 0.04f), brickRed);
            Decor(PrimitiveType.Sphere, "Eye L", robotModel, new Vector3(-0.12f, 1.38f, 0.23f), Vector3.one * 0.1f, glow);
            Decor(PrimitiveType.Sphere, "Eye R", robotModel, new Vector3(0.12f, 1.38f, 0.23f), Vector3.one * 0.1f, glow);
            var robotMuzzle = new GameObject("Muzzle").transform;
            robotMuzzle.SetParent(robot.transform, false);
            robotMuzzle.localPosition = new Vector3(0f, 0.9f, 0.5f);
            robot.muzzle = robotMuzzle;
            robot.projectilePrefab = pelletPrefab;
            robot.favouredDrops = new[] { ItemType.SpareBattery, ItemType.CompoundEye };
            var robotPrefab = SaveEnemy(robot);

            // Toy plane: the Flyer. Somebody's going "neeeow". It is.
            var plane = Creature<FlyerEnemy>("ToyPlane", 0.45f, 2f, 3, 0.8f, 2.9f, out var planeModel);
            Decor(PrimitiveType.Cylinder, "Fuselage", planeModel, new Vector3(0f, 0.7f, 0f), new Vector3(0.25f, 0.5f, 0.25f), brickYellow).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var planeL = Decor(PrimitiveType.Cube, "Wing L", planeModel, new Vector3(-0.45f, 0.72f, 0.05f), new Vector3(0.7f, 0.04f, 0.25f), brickRed).transform;
            var planeR = Decor(PrimitiveType.Cube, "Wing R", planeModel, new Vector3(0.45f, 0.72f, 0.05f), new Vector3(0.7f, 0.04f, 0.25f), brickRed).transform;
            Decor(PrimitiveType.Cube, "Propeller", planeModel, new Vector3(0f, 0.7f, 0.52f), new Vector3(0.5f, 0.06f, 0.02f), dark);
            var planeMotion = plane.gameObject.AddComponent<AntMotion>();
            planeMotion.legs = new Transform[0];
            planeMotion.wings = new[] { planeL, planeR };
            plane.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            plane.favouredDrops = new[] { ItemType.WingBuds, ItemType.KiteString };
            var planePrefab = SaveEnemy(plane);

            // Clown: the Lobber. Juggles, badly, in your direction.
            var clown = Creature<LobberEnemy>("Clown", 0.5f, 1.5f, 6, 1f, 2.4f, out var clownModel);
            Decor(PrimitiveType.Sphere, "Body", clownModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.75f, 0.9f, 0.6f), brickGreen);
            Decor(PrimitiveType.Sphere, "Head", clownModel, new Vector3(0f, 1.25f, 0f), Vector3.one * 0.5f, white);
            Decor(PrimitiveType.Sphere, "Nose", clownModel, new Vector3(0f, 1.25f, 0.27f), Vector3.one * 0.14f, brickRed);
            Decor(PrimitiveType.Cylinder, "Hat", clownModel, new Vector3(0f, 1.6f, 0f), new Vector3(0.3f, 0.2f, 0.3f), brickYellow);
            Decor(PrimitiveType.Sphere, "Eye L", clownModel, new Vector3(-0.1f, 1.32f, 0.22f), Vector3.one * 0.08f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", clownModel, new Vector3(0.1f, 1.32f, 0.22f), Vector3.one * 0.08f, dark);
            clown.blastMaterial = telegraph;
            clown.lobMaterial = ball;
            clown.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.Sprinkles };
            var clownPrefab = SaveEnemy(clown);

            // Music box: the Support. Plays the same tune forever. Everybody near it hurries.
            var musicBox = Creature<SupportEnemy>("MusicBox", 0.55f, 1.2f, 4, 1f, 2.1f, out var musicModel);
            Decor(PrimitiveType.Cube, "Box", musicModel, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.6f, 0.7f), pink);
            Decor(PrimitiveType.Cube, "Lid", musicModel, new Vector3(0f, 0.85f, -0.3f), new Vector3(0.9f, 0.05f, 0.7f), pink).transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            Decor(PrimitiveType.Cylinder, "Dancer", musicModel, new Vector3(0f, 0.85f, 0f), new Vector3(0.12f, 0.2f, 0.12f), white);
            musicBox.pulseColor = new Color(1f, 0.6f, 0.75f);
            musicBox.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.LostMarble };
            var musicPrefab = SaveEnemy(musicBox);

            // Dart gun: the Sniper. Suction cup on the end. Somehow, still.
            var gun = Creature<SniperEnemy>("DartGun", 0.45f, 1.3f, 5, 0.9f, 2.2f, out var gunModel);
            Decor(PrimitiveType.Cube, "Barrel", gunModel, new Vector3(0f, 0.85f, 0.15f), new Vector3(0.25f, 0.25f, 0.9f), brickYellow);
            Decor(PrimitiveType.Cube, "Grip", gunModel, new Vector3(0f, 0.5f, -0.15f), new Vector3(0.2f, 0.5f, 0.25f), brickBlue);
            Decor(PrimitiveType.Sphere, "Eye L", gunModel, new Vector3(-0.08f, 0.95f, 0.5f), Vector3.one * 0.08f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", gunModel, new Vector3(0.08f, 0.95f, 0.5f), Vector3.one * 0.08f, dark);
            var gunMuzzle = new GameObject("Muzzle").transform;
            gunMuzzle.SetParent(gun.transform, false);
            gunMuzzle.localPosition = new Vector3(0f, 0.85f, 0.75f);
            gun.muzzle = gunMuzzle;
            gun.projectilePrefab = dartPrefab;
            gun.telegraphMaterial = telegraph;
            gun.favouredDrops = new[] { ItemType.NeedleTip, ItemType.MagnifyingGlass };
            var gunPrefab = SaveEnemy(gun);

            // Bead: grows into a toy car if left. Nobody knows why. Toys are like that.
            var bead = Creature<LarvaEnemy>("Bead", 0.35f, 0.6f, 2, 0.6f, 1.2f, out var beadModel);
            Decor(PrimitiveType.Sphere, "Bead", beadModel, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.45f, brickRed);
            Decor(PrimitiveType.Sphere, "Eye L", beadModel, new Vector3(-0.08f, 0.3f, 0.2f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", beadModel, new Vector3(0.08f, 0.3f, 0.2f), Vector3.one * 0.07f, dark);
            bead.growsInto = carPrefab;
            bead.dropsLoot = false;
            bead.knockbackOnHit = 1f;
            Object.DestroyImmediate(bead.GetComponent<EnemyMutations>());
            var beadPrefab = (LarvaEnemy)SaveEnemy(bead);

            // ----- bosses: something that has to be wound up, and something that is always wound up -----
            var mouse = Creature<ToyBoss>("TheWindupMouse", 0.9f, 1.6f, 160, 1f, 3f, out var mouseModel);
            mouseModel.localScale = Vector3.one * 2f;
            Decor(PrimitiveType.Sphere, "Body", mouseModel, new Vector3(0f, 0.4f, 0f), new Vector3(0.7f, 0.6f, 1f), felt);
            Decor(PrimitiveType.Sphere, "Ear L", mouseModel, new Vector3(-0.22f, 0.75f, 0.3f), new Vector3(0.25f, 0.25f, 0.05f), pink);
            Decor(PrimitiveType.Sphere, "Ear R", mouseModel, new Vector3(0.22f, 0.75f, 0.3f), new Vector3(0.25f, 0.25f, 0.05f), pink);
            Decor(PrimitiveType.Sphere, "Nose", mouseModel, new Vector3(0f, 0.38f, 0.52f), Vector3.one * 0.1f, pink);
            Decor(PrimitiveType.Sphere, "Eye L", mouseModel, new Vector3(-0.12f, 0.52f, 0.42f), Vector3.one * 0.08f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", mouseModel, new Vector3(0.12f, 0.52f, 0.42f), Vector3.one * 0.08f, dark);
            Decor(PrimitiveType.Cube, "Key", mouseModel, new Vector3(0f, 0.7f, -0.35f), new Vector3(0.35f, 0.25f, 0.04f), key);
            Decor(PrimitiveType.Cube, "Tail", mouseModel, new Vector3(0f, 0.2f, -0.8f), new Vector3(0.03f, 0.03f, 0.6f), pink);
            mouse.displayName = "The Wind-up Mouse";
            mouse.subtitle = "Wound to the stop. It will not stop until it does.";
            mouse.canHide = false;
            mouse.canCharge = true;
            mouse.upTime = 1.6f;
            mouse.telegraphMaterial = telegraph;
            mouse.friends = new[] { carPrefab };
            mouse.sampleReward = 12;
            mouse.knockbackOnHit = 0.5f;
            Object.DestroyImmediate(mouse.GetComponent<EnemyMutations>());
            var mousePrefab = (BossEnemy)SaveEnemy(mouse);

            var jack = Creature<ToyBoss>("TheJackInTheBox", 1.2f, 3.2f, 380, 1f, 4.6f, out var jackModel);
            Decor(PrimitiveType.Cube, "Box", jackModel, new Vector3(0f, 0.7f, 0f), Vector3.one * 1.4f, brickRed);
            Decor(PrimitiveType.Cube, "Trim", jackModel, new Vector3(0f, 1.42f, 0f), new Vector3(1.5f, 0.08f, 1.5f), brickYellow);
            Decor(PrimitiveType.Cylinder, "Spring", jackModel, new Vector3(0f, 1.9f, 0f), new Vector3(0.35f, 0.5f, 0.35f), tin);
            Decor(PrimitiveType.Sphere, "Head", jackModel, new Vector3(0f, 2.65f, 0f), Vector3.one * 0.9f, white);
            Decor(PrimitiveType.Sphere, "Nose", jackModel, new Vector3(0f, 2.65f, 0.45f), Vector3.one * 0.2f, brickRed);
            Decor(PrimitiveType.Cylinder, "Hat", jackModel, new Vector3(0f, 3.2f, 0f), new Vector3(0.6f, 0.25f, 0.6f), brickBlue);
            Decor(PrimitiveType.Sphere, "Eye L", jackModel, new Vector3(-0.18f, 2.8f, 0.38f), Vector3.one * 0.14f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", jackModel, new Vector3(0.18f, 2.8f, 0.38f), Vector3.one * 0.14f, dark);
            jack.displayName = "The Jack-in-the-Box";
            jack.subtitle = "It has been waiting in there a very long time for exactly this.";
            // Every circle and every jack is something to get out of: fewer of them, and longer up top between.
            jack.jacksPerThrow = 2;
            jack.circlesPerSpring = new[] { 3, 3, 4 };
            jack.upTime = 4.5f;
            jack.telegraphMaterial = telegraph;
            jack.friends = new[] { clownPrefab, musicPrefab };
            jack.sampleReward = 30;
            jack.knockbackOnHit = 0.3f;
            Object.DestroyImmediate(jack.GetComponent<EnemyMutations>());
            var jackPrefab = (BossEnemy)SaveEnemy(jack);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(ToyBoxPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, ToyBoxPath);
            }

            biome.displayName = "The Toy Box";
            biome.id = "toybox";
            biome.mutationTheme = "speed, shells, things wound too tight";
            biome.isBottom = false;
            biome.floorMaterial = painted;
            biome.wallMaterial = brickBlue;
            biome.doorMaterial = brickYellow;
            biome.wallHeight = 6f;
            biome.ceiling = true;
            biome.ceilingMaterial = brickRed;
            biome.skyColor = new Color(0.25f, 0.2f, 0.3f);
            biome.ambientColor = new Color(0.7f, 0.62f, 0.58f);
            biome.sunIntensity = 0.85f;
            biome.fog = true;
            biome.fogColor = new Color(0.3f, 0.22f, 0.3f);
            biome.fogStart = 24f;
            biome.fogEnd = 85f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = brickGreen;          // blocks left out
            biome.crumbMaterial = brickYellow;         // bits of jigsaw
            biome.sugarMaterial = pink;
            biome.rootMaterial = tin;                  // tin soldiers' stands
            biome.podMaterial = ball;                  // bouncy balls
            biome.podHatchling = beadPrefab;
            biome.dewMaterial = white;
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = glow;                 // night lights
            biome.glowStemMaterial = tin;
            biome.glowLightRange = 11f;
            biome.glowLightIntensity = 2f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = carPrefab;
            biome.miniBoss = mousePrefab;
            biome.boss = jackPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = carPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = robotPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = blockPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = planePrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = clownPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = musicPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = gunPrefab, weight = 1, minFloor = 5 },
                new EnemySpawnEntry { prefab = ants.EnemyNamed("Mimic"), weight = 1, minFloor = 4 },
            };
            var mutations = new System.Collections.Generic.List<MutationDefinition>
            {
                ants.Mutation(MutationType.Fast), ants.Mutation(MutationType.Armored), ants.Mutation(MutationType.Regenerates),
                ants.Mutation(MutationType.SwarmCall), ants.Mutation(MutationType.HardenedShell), ants.Mutation(MutationType.AcidBlood),
            };
            var staticMutation = AssetDatabase.LoadAssetAtPath<MutationDefinition>("Assets/Data/Mutations/" + MutationType.Static + ".asset");
            if (staticMutation != null) mutations.Add(staticMutation);
            mutations.RemoveAll(mutation => mutation == null);
            biome.mutations = mutations.ToArray();

            biome.startProp = ants.startProp;
            biome.exitProp = ants.exitProp;
            biome.treasureProp = ants.treasureProp;
            biome.shopProp = ants.shopProp;
            biome.extractPortalProp = ants.extractPortalProp;
            biome.deeperPortalProp = ants.deeperPortalProp;
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("ToyBoxPortal", brickYellow, black, brickRed), "Props/ToyBoxPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
