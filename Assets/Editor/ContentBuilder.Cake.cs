using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The second biome, as a test of how different a biome can be made from the same parts: the Birthday Cake.
    /// Open to the sky, sponge floors and frosting walls, and candy for enemies. Every enemy reuses one of the four
    /// base behaviours with a new body. Its two bosses, the Jelly Mother and the Trick Candle, have fights of their own.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string CakePath = "Assets/Data/BirthdayCake.asset";

        public static BiomeDefinition BuildCake(BiomeDefinition ants)
        {
            var gummyRed = ArenaSceneBuilder.Lit("GummyRed", new Color(0.9f, 0.15f, 0.2f), 0.9f);
            var gummyGreen = ArenaSceneBuilder.Lit("GummyGreen", new Color(0.3f, 0.8f, 0.25f), 0.9f);
            var gummyPale = ArenaSceneBuilder.Lit("GummyPale", new Color(1f, 0.75f, 0.75f), 0.9f);
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var white = ArenaSceneBuilder.Lit("Sugar", new Color(0.95f, 0.95f, 0.92f), 0.6f);
            var blue = ArenaSceneBuilder.Lit("CandyBlue", new Color(0.25f, 0.55f, 0.95f), 0.8f);
            var yellow = ArenaSceneBuilder.Lit("CandyYellow", new Color(1f, 0.85f, 0.25f), 0.8f);
            var sponge = ArenaSceneBuilder.Lit("Sponge", new Color(0.8f, 0.62f, 0.36f), 0.05f);
            var frosting = ArenaSceneBuilder.Lit("Frosting", new Color(0.85f, 0.45f, 0.6f), 0.3f);
            var chocolate = ArenaSceneBuilder.Lit("Chocolate", new Color(0.33f, 0.18f, 0.1f), 0.4f);
            var cupcakeCase = ArenaSceneBuilder.Lit("CupcakeCase", new Color(0.55f, 0.35f, 0.2f), 0.2f);
            var cherry = ArenaSceneBuilder.Lit("Cherry", new Color(0.8f, 0.05f, 0.1f), 0.9f);
            var wax = ArenaSceneBuilder.Lit("CandleWax", new Color(0.55f, 0.8f, 0.95f), 0.5f);
            var flame = ArenaSceneBuilder.Unlit("Fire", new Color(1f, 0.55f, 0.1f));
            var hotWax = ArenaSceneBuilder.Unlit("HotWax", new Color(1f, 0.6f, 0.25f));
            var sprinkle = ArenaSceneBuilder.Unlit("Sprinkle", new Color(1f, 0.35f, 0.75f));
            var wing = ArenaSceneBuilder.Lit("Wing", new Color(0.85f, 0.9f, 0.95f), 0.8f);
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            // ----- shots -----
            var shot = new GameObject("SprinkleShot");
            Decor(PrimitiveType.Capsule, "Visual", shot.transform, Vector3.zero, new Vector3(0.22f, 0.3f, 0.22f), sprinkle).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shot.AddComponent<Bullet>().radius = 0.22f;
            var sprinklePrefab = ArenaSceneBuilder.SavePrefab(shot, "Enemies/SprinkleShot").GetComponent<Bullet>();

            var waxShot = new GameObject("WaxShot");
            Decor(PrimitiveType.Sphere, "Visual", waxShot.transform, Vector3.zero, Vector3.one * 0.55f, hotWax);
            waxShot.AddComponent<Bullet>().radius = 0.27f;
            var waxPrefab = ArenaSceneBuilder.SavePrefab(waxShot, "Enemies/WaxShot").GetComponent<Bullet>();

            // ----- the four candies, one per base behaviour -----
            // Gummy bear: the Chaser. Weak, and there are a lot of them.
            var gummy = Creature<ChaserEnemy>("GummyBear", 0.5f, 1.6f, 5, 1f, 2.4f, out var gummyModel);
            GummyParts(gummyModel, gummyRed, gummyPale, dark);
            gummy.favouredDrops = new[] { ItemType.Sprinkles, ItemType.SugarCube };
            var gummyPrefab = SaveEnemy(gummy);

            // Jawbreaker: the Charger. A hard ball with a face that rolls you flat.
            var breaker = Creature<ChargerEnemy>("Jawbreaker", 0.85f, 1.7f, 10, 1.5f, 2.6f, out var breakerModel);
            Decor(PrimitiveType.Sphere, "Ball", breakerModel, new Vector3(0f, 0.85f, 0f), Vector3.one * 1.7f, white);
            Material[] bands = { blue, gummyRed, yellow };
            for (int i = 0; i < 3; i++)
            {
                Decor(PrimitiveType.Cylinder, "Band " + i, breakerModel, new Vector3(0f, 0.85f, 0f), new Vector3(1.72f, 0.07f, 1.72f), bands[i]).transform.localRotation = Quaternion.Euler(25f + i * 55f, i * 40f, 0f);
            }
            Decor(PrimitiveType.Sphere, "Eye L", breakerModel, new Vector3(-0.3f, 1.05f, 0.75f), Vector3.one * 0.22f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", breakerModel, new Vector3(0.3f, 1.05f, 0.75f), Vector3.one * 0.22f, dark);
            breaker.knockbackOnHit = 2f;
            breaker.favouredDrops = new[] { ItemType.ChitinPlate };
            breaker.telegraphMaterial = telegraph;
            var breakerPrefab = SaveEnemy(breaker);

            // Cupcake: the Shooter. Keeps its distance and flicks sprinkles.
            var cupcake = Creature<ShooterEnemy>("Cupcake", 0.5f, 1.5f, 4, 0.9f, 2.3f, out var cupcakeModel);
            Decor(PrimitiveType.Cylinder, "Case", cupcakeModel, new Vector3(0f, 0.32f, 0f), new Vector3(0.85f, 0.32f, 0.85f), cupcakeCase);
            Decor(PrimitiveType.Sphere, "Icing", cupcakeModel, new Vector3(0f, 0.8f, 0f), new Vector3(1.05f, 0.7f, 1.05f), frosting);
            Decor(PrimitiveType.Sphere, "Icing Top", cupcakeModel, new Vector3(0f, 1.1f, 0f), new Vector3(0.65f, 0.5f, 0.65f), frosting);
            Decor(PrimitiveType.Sphere, "Cherry", cupcakeModel, new Vector3(0f, 1.42f, 0f), Vector3.one * 0.26f, cherry);
            Decor(PrimitiveType.Sphere, "Eye L", cupcakeModel, new Vector3(-0.2f, 0.85f, 0.48f), Vector3.one * 0.13f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", cupcakeModel, new Vector3(0.2f, 0.85f, 0.48f), Vector3.one * 0.13f, dark);
            var cupcakeMuzzle = new GameObject("Muzzle").transform;
            cupcakeMuzzle.SetParent(cupcake.transform, false);
            cupcakeMuzzle.localPosition = new Vector3(0f, 0.8f, 0.9f);
            cupcake.muzzle = cupcakeMuzzle;
            cupcake.projectilePrefab = sprinklePrefab;
            cupcake.favouredDrops = new[] { ItemType.BirthdayCandle, ItemType.SplitterMandible };
            var cupcakePrefab = SaveEnemy(cupcake);

            // Sugar fly: the Flyer. A wrapped sweet with wings.
            var fly = Creature<FlyerEnemy>("SugarFly", 0.45f, 2f, 3, 0.8f, 2.9f, out var flyModel);
            Decor(PrimitiveType.Sphere, "Sweet", flyModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.5f, 0.75f), yellow);
            Decor(PrimitiveType.Cube, "Twist Front", flyModel, new Vector3(0f, 0.6f, 0.5f), new Vector3(0.4f, 0.4f, 0.08f), blue).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Decor(PrimitiveType.Cube, "Twist Back", flyModel, new Vector3(0f, 0.6f, -0.5f), new Vector3(0.4f, 0.4f, 0.08f), blue).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var flyWingLeft = Decor(PrimitiveType.Cube, "Wing L", flyModel, new Vector3(-0.55f, 0.9f, 0f), new Vector3(0.9f, 0.03f, 0.45f), wing).transform;
            flyWingLeft.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var flyWingRight = Decor(PrimitiveType.Cube, "Wing R", flyModel, new Vector3(0.55f, 0.9f, 0f), new Vector3(0.9f, 0.03f, 0.45f), wing).transform;
            flyWingRight.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var flyMotion = fly.gameObject.AddComponent<AntMotion>();
            flyMotion.legs = new Transform[0];
            flyMotion.wings = new[] { flyWingLeft, flyWingRight };
            fly.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            fly.favouredDrops = new[] { ItemType.WingBuds };
            var flyPrefab = SaveEnemy(fly);

            // ----- two more, from floor 5 on -----
            // Soda bottle: the Lobber. Shaken, and it knows it.
            var glassBrown = ArenaSceneBuilder.Lit("SodaGlass", new Color(0.3f, 0.16f, 0.08f), 0.95f);
            var fizz = ArenaSceneBuilder.Unlit("SodaFizz", new Color(0.85f, 0.6f, 0.35f));
            var soda = Creature<LobberEnemy>("SodaBottle", 0.45f, 1.9f, 6, 1f, 2.6f, out var sodaModel);
            Decor(PrimitiveType.Cylinder, "Bottle", sodaModel, new Vector3(0f, 0.65f, 0f), new Vector3(0.75f, 0.65f, 0.75f), glassBrown);
            Decor(PrimitiveType.Cylinder, "Label", sodaModel, new Vector3(0f, 0.7f, 0f), new Vector3(0.78f, 0.22f, 0.78f), gummyRed);
            Decor(PrimitiveType.Sphere, "Shoulder", sodaModel, new Vector3(0f, 1.3f, 0f), new Vector3(0.75f, 0.5f, 0.75f), glassBrown);
            Decor(PrimitiveType.Cylinder, "Neck", sodaModel, new Vector3(0f, 1.62f, 0f), new Vector3(0.28f, 0.2f, 0.28f), glassBrown);
            Decor(PrimitiveType.Cylinder, "Cap", sodaModel, new Vector3(0f, 1.85f, 0f), new Vector3(0.34f, 0.05f, 0.34f), yellow);
            Decor(PrimitiveType.Sphere, "Eye L", sodaModel, new Vector3(-0.16f, 1.05f, 0.36f), Vector3.one * 0.12f, white);
            Decor(PrimitiveType.Sphere, "Eye R", sodaModel, new Vector3(0.16f, 1.05f, 0.36f), Vector3.one * 0.12f, white);
            soda.blastMaterial = telegraph;
            soda.lobMaterial = fizz;
            soda.favouredDrops = new[] { ItemType.AcidGland, ItemType.Sprinkles };
            var sodaPrefab = SaveEnemy(soda);

            // Party horn: the Support. Every time it toots, everything near it gets quicker.
            var horn = Creature<SupportEnemy>("PartyHorn", 0.45f, 1.4f, 4, 0.9f, 2.2f, out var hornModel);
            var hornTube = Decor(PrimitiveType.Cylinder, "Tube", hornModel, new Vector3(0f, 0.6f, 0f), new Vector3(0.4f, 0.55f, 0.4f), blue);
            hornTube.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 3; i++)
            {
                Decor(PrimitiveType.Cylinder, "Stripe " + i, hornModel, new Vector3(0f, 0.6f, -0.35f + i * 0.35f), new Vector3(0.42f, 0.05f, 0.42f), yellow).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Decor(PrimitiveType.Sphere, "Bell", hornModel, new Vector3(0f, 0.6f, 0.65f), new Vector3(0.75f, 0.75f, 0.3f), gummyRed);
            Decor(PrimitiveType.Sphere, "Eye L", hornModel, new Vector3(-0.14f, 0.92f, 0.2f), Vector3.one * 0.13f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", hornModel, new Vector3(0.14f, 0.92f, 0.2f), Vector3.one * 0.13f, dark);
            horn.pulseColor = new Color(1f, 0.85f, 0.25f);
            horn.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.Sprinkles };
            var hornPrefab = SaveEnemy(horn);

            // ----- bosses and what they bring -----
            // Gumdrop: a small dome that grows into a gummy bear if it is left alone. (The Brood Mother's larva.)
            var gumdrop = Creature<LarvaEnemy>("Gumdrop", 0.4f, 0.8f, 2, 0.6f, 1.4f, out var gumdropModel);
            Decor(PrimitiveType.Sphere, "Dome", gumdropModel, new Vector3(0f, 0.25f, 0f), new Vector3(0.7f, 0.8f, 0.7f), gummyGreen);
            gumdrop.growsInto = gummyPrefab;
            gumdrop.dropsLoot = false;
            gumdrop.knockbackOnHit = 1f;
            Object.DestroyImmediate(gumdrop.GetComponent<EnemyMutations>());
            var gumdropPrefab = (LarvaEnemy)SaveEnemy(gumdrop);

            // Sparkler box: the Trick Candle's "eggs". Each one lit lets something out.
            var sparkler = Creature<EggEnemy>("SparklerBox", 0.9f, 1.8f, 6, 1f, 2.4f, out var sparklerModel);
            Decor(PrimitiveType.Cube, "Box", sparklerModel, new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.6f, 0.9f), blue);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.45f + i * 0.3f;
                Decor(PrimitiveType.Cylinder, "Stick " + i, sparklerModel, new Vector3(x, 1f, 0f), new Vector3(0.05f, 0.5f, 0.05f), dark).transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1.5f) * 9f);
                Decor(PrimitiveType.Sphere, "Spark " + i, sparklerModel, new Vector3(x - (i - 1.5f) * 0.08f, 1.5f, 0f), Vector3.one * 0.22f, flame);
            }
            sparkler.dropsLoot = false;
            sparkler.knockbackOnHit = 0f;
            Object.DestroyImmediate(sparkler.GetComponent<EnemyMutations>());
            var sparklerPrefab = (EggEnemy)SaveEnemy(sparkler);

            // Jelly Mother (floor 5): her own fight. Belly-flops, lobs gumdrops, and tears into smaller, quicker pieces.
            // The wobbling jelly on her back is the weak point.
            var mother = Creature<JellyMother>("JellyMother", 1.65f, 3.6f, 160, 3f, 4.8f, out var motherModel);
            motherModel.localScale = Vector3.one * 2.6f;
            GummyParts(motherModel, gummyGreen, gummyPale, dark);
            Decor(PrimitiveType.Sphere, "Jelly", motherModel, new Vector3(0f, 0.75f, -0.45f), new Vector3(0.7f, 0.7f, 0.6f), gummyPale);
            mother.displayName = "Jelly Mother";
            mother.subtitle = "97% sugar. 3% spite.";
            mother.gumdropPrefab = gumdropPrefab;
            mother.splitInto = gummyPrefab;
            mother.telegraphMaterial = telegraph;
            mother.sampleReward = 12;
            mother.knockbackOnHit = 0f;
            var motherPrefab = (BossEnemy)SaveEnemy(mother);

            // The Trick Candle (floor 10): its own fight. Drips wax where you are going, lights its sparkler boxes,
            // spins out a spiral of wax, and once goes out and relights.
            var candle = Creature<TrickCandle>("TrickCandle", 1.85f, 4f, 300, 3.4f, 6.2f, out var candleModel);
            Decor(PrimitiveType.Cylinder, "Plate", candleModel, new Vector3(0f, 0.12f, 0f), new Vector3(4f, 0.12f, 4f), white);
            Decor(PrimitiveType.Cylinder, "Candle", candleModel, new Vector3(0f, 2.2f, 0f), new Vector3(2.4f, 2f, 2.4f), wax);
            for (int i = 0; i < 4; i++)
            {
                Decor(PrimitiveType.Cylinder, "Stripe " + i, candleModel, new Vector3(0f, 0.9f + i * 0.9f, 0f), new Vector3(2.44f, 0.12f, 2.44f), white);
            }
            Decor(PrimitiveType.Cylinder, "Wick", candleModel, new Vector3(0f, 4.4f, 0f), new Vector3(0.12f, 0.25f, 0.12f), dark);
            candle.flame = Decor(PrimitiveType.Sphere, "Flame", candleModel, new Vector3(0f, 5f, 0f), new Vector3(0.8f, 1.2f, 0.8f), flame).transform;
            Decor(PrimitiveType.Sphere, "Eye L", candleModel, new Vector3(-0.45f, 3f, 1.12f), Vector3.one * 0.36f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", candleModel, new Vector3(0.45f, 3f, 1.12f), Vector3.one * 0.36f, dark);
            Decor(PrimitiveType.Cube, "Mouth", candleModel, new Vector3(0f, 2.4f, 1.18f), new Vector3(0.8f, 0.14f, 0.1f), dark);
            candle.displayName = "The Trick Candle";
            candle.subtitle = "Make a wish. It won't help.";
            candle.projectilePrefab = waxPrefab;
            candle.sparklerPrefab = sparklerPrefab;
            candle.telegraphMaterial = telegraph;
            candle.waxMaterial = hotWax;
            candle.sampleReward = 30;
            candle.knockbackOnHit = 0f;
            var candlePrefab = (BossEnemy)SaveEnemy(candle);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(CakePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, CakePath);
            }

            biome.displayName = "Birthday Cake";
            biome.id = "cake";
            biome.mutationTheme = "sugar rush, sticky";
            biome.floorMaterial = sponge;
            biome.wallMaterial = frosting;
            biome.doorMaterial = chocolate;
            biome.wallHeight = 6f;
            // Outdoors: no roof, a blue sky, bright even light and no haze.
            biome.ceiling = false;
            biome.ceilingMaterial = null;
            biome.skyColor = new Color(0.55f, 0.8f, 0.97f);
            biome.ambientColor = new Color(0.52f, 0.5f, 0.55f);
            biome.sunIntensity = 0.95f;
            biome.fog = false;
            biome.roomCastsShadows = false;

            biome.coverMaterial = gummyGreen;       // gumdrops lying about
            biome.crumbMaterial = sponge;           // lumps of cake
            biome.sugarMaterial = white;
            biome.rootMaterial = ArenaSceneBuilder.Lit("CandyCane", new Color(0.92f, 0.2f, 0.25f), 0.7f);
            biome.podMaterial = ArenaSceneBuilder.Lit("JellyBean", new Color(0.7f, 0.4f, 0.95f), 0.9f);
            biome.podHatchling = gumdropPrefab;
            biome.dewMaterial = ArenaSceneBuilder.Lit("IcingDrip", new Color(1f, 1f, 0.98f), 0.9f);
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = ArenaSceneBuilder.Unlit("CandleGlow", new Color(1f, 0.85f, 0.45f));
            biome.glowStemMaterial = wax;
            biome.glowLightIntensity = 1.5f;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = gummyPrefab;
            biome.miniBoss = motherPrefab;
            biome.boss = candlePrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = gummyPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = cupcakePrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = breakerPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = flyPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = sodaPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = hornPrefab, weight = 1, minFloor = 6 },
                // The mimic is the ants' own prefab: coins are coins everywhere.
                new EnemySpawnEntry { prefab = ants.EnemyNamed("Mimic"), weight = 1, minFloor = 4 },
            };
            biome.mutations = new[]
            {
                ants.Mutation(MutationType.Fast), ants.Mutation(MutationType.Armored), ants.Mutation(MutationType.Regenerates),
                ants.Mutation(MutationType.ExplodesOnDeath), ants.Mutation(MutationType.SwarmCall),
                Mutation(MutationType.Sticky, "Sticky", 1, "Leaves a sticky patch on death that slows you.", new Color(0.85f, 0.55f, 0.15f)),
                Mutation(MutationType.SugarRush, "Sugar rush", 2, "Below half health it moves much faster.", new Color(1f, 0.35f, 0.75f)),
            };

            // The same special-room props as everywhere, and its own coloured portal in other biomes' portal rooms.
            biome.startProp = ants.startProp;
            biome.exitProp = ants.exitProp;
            biome.treasureProp = ants.treasureProp;
            biome.shopProp = ants.shopProp;
            biome.extractPortalProp = ants.extractPortalProp;
            biome.deeperPortalProp = ants.deeperPortalProp;
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("CakePortal", sprinkle, black, frosting), "Props/CakePortal");

            // And the way back to the ants from here.
            ants.portalProp = ArenaSceneBuilder.SavePrefab(Portal("AntPortal", ArenaSceneBuilder.Unlit("MutationAcidBlood", new Color(0.7f, 1f, 0.2f)), black, ants.wallMaterial), "Props/AntPortal");
            EditorUtility.SetDirty(ants);

            EditorUtility.SetDirty(biome);
            return biome;
        }

        // A plain creature with no body yet: the collider, health, the Visual and Model holders, and the parts every
        // enemy has. Bodies are sized as if a worker ant were 1, so mutation looks land in sensible places.
        static T Creature<T>(string name, float radius, float height, int health, float bodySize, float badgeHeight, out Transform model) where T : Enemy
        {
            var root = new GameObject(name);

            var controller = root.AddComponent<CharacterController>();
            controller.radius = radius;
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);

            var hp = root.AddComponent<Health>();
            hp.team = Team.Enemy;
            hp.maxHealth = health;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            model = new GameObject("Model").transform;
            model.SetParent(visual, false);

            var enemy = root.AddComponent<T>();
            enemy.visual = visual;
            root.AddComponent<HitFlash>();
            var mutations = root.AddComponent<EnemyMutations>();
            mutations.badgeHeight = badgeHeight;
            mutations.shellOffset = radius + 0.5f;
            mutations.bodySize = bodySize;
            return enemy;
        }

        // A gummy bear, 1.6 m tall: used small for the bears and large for the Jelly Mother.
        static void GummyParts(Transform model, Material body, Material pale, Material dark)
        {
            Decor(PrimitiveType.Sphere, "Belly", model, new Vector3(0f, 0.6f, 0f), new Vector3(0.85f, 0.95f, 0.75f), body);
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 1.22f, 0.05f), Vector3.one * 0.66f, body);
            Decor(PrimitiveType.Sphere, "Ear L", model, new Vector3(-0.25f, 1.52f, 0.02f), Vector3.one * 0.24f, body);
            Decor(PrimitiveType.Sphere, "Ear R", model, new Vector3(0.25f, 1.52f, 0.02f), Vector3.one * 0.24f, body);
            Decor(PrimitiveType.Sphere, "Snout", model, new Vector3(0f, 1.15f, 0.34f), new Vector3(0.3f, 0.24f, 0.2f), pale);
            Decor(PrimitiveType.Sphere, "Eye L", model, new Vector3(-0.14f, 1.32f, 0.33f), Vector3.one * 0.09f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", model, new Vector3(0.14f, 1.32f, 0.33f), Vector3.one * 0.09f, dark);
            Decor(PrimitiveType.Sphere, "Arm L", model, new Vector3(-0.46f, 0.8f, 0.14f), new Vector3(0.3f, 0.3f, 0.4f), body);
            Decor(PrimitiveType.Sphere, "Arm R", model, new Vector3(0.46f, 0.8f, 0.14f), new Vector3(0.3f, 0.3f, 0.4f), body);
            Decor(PrimitiveType.Sphere, "Leg L", model, new Vector3(-0.24f, 0.17f, 0.08f), new Vector3(0.36f, 0.34f, 0.44f), body);
            Decor(PrimitiveType.Sphere, "Leg R", model, new Vector3(0.24f, 0.17f, 0.08f), new Vector3(0.36f, 0.34f, 0.44f), body);
        }
    }
}
