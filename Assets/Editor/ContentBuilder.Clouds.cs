using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The seventh world: The Clouds. Up above everything, on floors of cloud with banks of it for walls, in bright
    /// cold light. Seven enemies on the seven behaviours, the Kite half way and the Thundercloud at the end, whose
    /// fight is lightning you can see coming, gusts you can step out of, and rain that slows. Its portal opens once
    /// the Toad has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string CloudsPath = "Assets/Data/TheClouds.asset";

        public static BiomeDefinition BuildClouds(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var cloud = ArenaSceneBuilder.Lit("CloudFloor", new Color(0.7f, 0.76f, 0.86f), 0.05f);
            var bank = ArenaSceneBuilder.Lit("CloudBank", new Color(0.56f, 0.62f, 0.75f), 0.05f);
            var storm = ArenaSceneBuilder.Lit("StormGrey", new Color(0.32f, 0.34f, 0.42f), 0.1f);
            var rainBlue = ArenaSceneBuilder.Lit("Raindrop", new Color(0.35f, 0.6f, 1f), 0.9f);
            var hail = ArenaSceneBuilder.Lit("Hail", new Color(0.88f, 0.95f, 1f), 0.95f);
            var feather = ArenaSceneBuilder.Lit("SwallowBlue", new Color(0.12f, 0.18f, 0.4f), 0.3f);
            var breast = ArenaSceneBuilder.Lit("SwallowBreast", new Color(0.95f, 0.75f, 0.6f), 0.2f);
            var beak = ArenaSceneBuilder.Lit("Beak", new Color(1f, 0.75f, 0.2f), 0.4f);
            var flake = ArenaSceneBuilder.Unlit("Snowflake", new Color(0.9f, 0.97f, 1f));
            var rainbowRed = ArenaSceneBuilder.Unlit("RainbowRed", new Color(1f, 0.35f, 0.35f));
            var rainbowYellow = ArenaSceneBuilder.Unlit("RainbowYellow", new Color(1f, 0.9f, 0.35f));
            var rainbowBlue = ArenaSceneBuilder.Unlit("RainbowBlue", new Color(0.4f, 0.65f, 1f));
            var copper = ArenaSceneBuilder.Lit("RodCopper", new Color(0.75f, 0.45f, 0.25f), 0.85f);
            var spark = ArenaSceneBuilder.Unlit("Spark", new Color(1f, 0.95f, 0.5f));
            var kitePaper = ArenaSceneBuilder.Lit("KitePaper", new Color(0.95f, 0.3f, 0.35f), 0.2f);
            var kiteStripe = ArenaSceneBuilder.Lit("KiteStripe", new Color(1f, 0.9f, 0.3f), 0.2f);
            var puddle = ArenaSceneBuilder.Unlit("RainPuddle", new Color(0.4f, 0.6f, 0.95f));
            var bolt = ArenaSceneBuilder.Unlit("Lightning", new Color(1f, 1f, 0.55f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var drip = new GameObject("RainShot");
            Decor(PrimitiveType.Sphere, "Visual", drip.transform, Vector3.zero, new Vector3(0.28f, 0.28f, 0.45f), rainBlue);
            drip.AddComponent<Bullet>().radius = 0.2f;
            var dripPrefab = ArenaSceneBuilder.SavePrefab(drip, "Enemies/RainShot").GetComponent<Bullet>();

            var zap = new GameObject("Zap");
            Decor(PrimitiveType.Cube, "Visual", zap.transform, Vector3.zero, new Vector3(0.1f, 0.1f, 0.7f), bolt);
            zap.AddComponent<Bullet>().radius = 0.18f;
            var zapPrefab = ArenaSceneBuilder.SavePrefab(zap, "Enemies/Zap").GetComponent<Bullet>();

            // ----- the seven -----
            // Raindrop: the Chaser. It is going down, and it would like company.
            var drop = Creature<ChaserEnemy>("RaindropEnemy", 0.45f, 1.1f, 5, 1f, 1.9f, out var dropModel);
            Decor(PrimitiveType.Sphere, "Body", dropModel, new Vector3(0f, 0.45f, 0f), new Vector3(0.75f, 0.75f, 0.75f), rainBlue);
            Decor(PrimitiveType.Sphere, "Tip", dropModel, new Vector3(0f, 0.85f, 0f), new Vector3(0.35f, 0.5f, 0.35f), rainBlue);
            Decor(PrimitiveType.Sphere, "Eye L", dropModel, new Vector3(-0.15f, 0.55f, 0.32f), Vector3.one * 0.12f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", dropModel, new Vector3(0.15f, 0.55f, 0.32f), Vector3.one * 0.12f, dark);
            drop.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.IceCube };
            var dropPrefab = SaveEnemy(drop);

            // Hailstone: the Charger. Hard, cold, and in a hurry to land.
            var stone = Creature<ChargerEnemy>("Hailstone", 0.8f, 1.6f, 10, 1.5f, 2.4f, out var stoneModel);
            Decor(PrimitiveType.Sphere, "Ball", stoneModel, new Vector3(0f, 0.8f, 0f), Vector3.one * 1.5f, hail);
            Decor(PrimitiveType.Sphere, "Lump", stoneModel, new Vector3(0.45f, 1.2f, -0.2f), Vector3.one * 0.6f, hail);
            Decor(PrimitiveType.Sphere, "Eye L", stoneModel, new Vector3(-0.28f, 1f, 0.68f), Vector3.one * 0.18f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", stoneModel, new Vector3(0.28f, 1f, 0.68f), Vector3.one * 0.18f, dark);
            stone.knockbackOnHit = 1.5f;
            stone.telegraphMaterial = telegraph;
            stone.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.IceCube };
            var stonePrefab = SaveEnemy(stone);

            // Raincloud: the Shooter. A small grey one with a grudge.
            var puff = Creature<ShooterEnemy>("Raincloud", 0.5f, 1.5f, 4, 0.9f, 2.4f, out var puffModel);
            Decor(PrimitiveType.Sphere, "Puff A", puffModel, new Vector3(0f, 1.1f, 0f), new Vector3(1f, 0.7f, 0.8f), storm);
            Decor(PrimitiveType.Sphere, "Puff B", puffModel, new Vector3(-0.4f, 1f, 0f), Vector3.one * 0.6f, storm);
            Decor(PrimitiveType.Sphere, "Puff C", puffModel, new Vector3(0.4f, 1f, 0f), Vector3.one * 0.6f, storm);
            Decor(PrimitiveType.Sphere, "Eye L", puffModel, new Vector3(-0.15f, 1.15f, 0.38f), Vector3.one * 0.12f, cloud);
            Decor(PrimitiveType.Sphere, "Eye R", puffModel, new Vector3(0.15f, 1.15f, 0.38f), Vector3.one * 0.12f, cloud);
            var puffMuzzle = new GameObject("Muzzle").transform;
            puffMuzzle.SetParent(puff.transform, false);
            puffMuzzle.localPosition = new Vector3(0f, 0.8f, 0.6f);
            puff.muzzle = puffMuzzle;
            puff.projectilePrefab = dripPrefab;
            puff.favouredDrops = new[] { ItemType.AcidGland, ItemType.RainBoot };
            var puffPrefab = SaveEnemy(puff);

            // Swallow: the Flyer. It was here first, and it is very fast about it.
            var bird = Creature<FlyerEnemy>("Swallow", 0.45f, 2f, 3, 0.8f, 2.9f, out var birdModel);
            Decor(PrimitiveType.Sphere, "Body", birdModel, new Vector3(0f, 0.7f, 0f), new Vector3(0.4f, 0.35f, 0.9f), feather);
            Decor(PrimitiveType.Sphere, "Breast", birdModel, new Vector3(0f, 0.62f, 0.15f), new Vector3(0.32f, 0.28f, 0.5f), breast);
            Decor(PrimitiveType.Cube, "Beak", birdModel, new Vector3(0f, 0.72f, 0.5f), new Vector3(0.08f, 0.06f, 0.2f), beak);
            Decor(PrimitiveType.Cube, "Tail", birdModel, new Vector3(0f, 0.7f, -0.6f), new Vector3(0.3f, 0.03f, 0.4f), feather);
            var birdLeft = Decor(PrimitiveType.Cube, "Wing L", birdModel, new Vector3(-0.5f, 0.78f, 0f), new Vector3(0.8f, 0.03f, 0.35f), feather).transform;
            var birdRight = Decor(PrimitiveType.Cube, "Wing R", birdModel, new Vector3(0.5f, 0.78f, 0f), new Vector3(0.8f, 0.03f, 0.35f), feather).transform;
            var birdMotion = bird.gameObject.AddComponent<AntMotion>();
            birdMotion.legs = new Transform[0];
            birdMotion.wings = new[] { birdLeft, birdRight };
            bird.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            bird.favouredDrops = new[] { ItemType.WingBuds, ItemType.ColdCoffee };
            var birdPrefab = SaveEnemy(bird);

            // Snowflake: the Lobber. Every one different; every one thrown at you.
            var snow = Creature<LobberEnemy>("Snowflake", 0.5f, 1.4f, 6, 1f, 2.3f, out var snowModel);
            for (int i = 0; i < 3; i++)
                Decor(PrimitiveType.Cube, "Arm " + i, snowModel, new Vector3(0f, 0.8f, 0f), new Vector3(0.12f, 1.3f, 0.12f), flake).transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
            Decor(PrimitiveType.Sphere, "Middle", snowModel, new Vector3(0f, 0.8f, 0f), Vector3.one * 0.35f, flake);
            Decor(PrimitiveType.Sphere, "Eye L", snowModel, new Vector3(-0.08f, 0.85f, 0.16f), Vector3.one * 0.08f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", snowModel, new Vector3(0.08f, 0.85f, 0.16f), Vector3.one * 0.08f, dark);
            snow.blastMaterial = telegraph;
            snow.lobMaterial = flake;
            snow.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.IceCube };
            var snowPrefab = SaveEnemy(snow);

            // Rainbow: the Support. Cheerful, and it makes everything around it worse.
            var arc = Creature<SupportEnemy>("Rainbow", 0.5f, 1.6f, 4, 1f, 2.4f, out var arcModel);
            Material[] bands = { rainbowRed, rainbowYellow, rainbowBlue };
            for (int i = 0; i < bands.Length; i++)
            {
                float r = 0.75f - i * 0.18f;
                for (int k = 0; k < 5; k++)
                {
                    float a = Mathf.PI * k / 4f;
                    Decor(PrimitiveType.Sphere, "Band " + i + "." + k, arcModel, new Vector3(Mathf.Cos(a) * r, 0.3f + Mathf.Sin(a) * r, 0f), Vector3.one * 0.2f, bands[i]);
                }
            }
            Decor(PrimitiveType.Sphere, "Eye L", arcModel, new Vector3(-0.12f, 0.5f, 0.1f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", arcModel, new Vector3(0.12f, 0.5f, 0.1f), Vector3.one * 0.1f, dark);
            arc.pulseColor = new Color(1f, 0.9f, 0.5f);
            arc.favouredDrops = new[] { ItemType.SpareBattery, ItemType.LostMarble };
            var arcPrefab = SaveEnemy(arc);

            // Lightning rod: the Sniper. It draws the line, then it draws the lightning down it.
            var rod = Creature<SniperEnemy>("LightningRod", 0.45f, 1.6f, 5, 0.9f, 2.4f, out var rodModel);
            Decor(PrimitiveType.Cylinder, "Rod", rodModel, new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 0.9f, 0.12f), copper);
            Decor(PrimitiveType.Cylinder, "Base", rodModel, new Vector3(0f, 0.1f, 0f), new Vector3(0.6f, 0.1f, 0.6f), storm);
            Decor(PrimitiveType.Sphere, "Ball", rodModel, new Vector3(0f, 1.85f, 0f), Vector3.one * 0.3f, spark);
            Decor(PrimitiveType.Sphere, "Eye L", rodModel, new Vector3(-0.1f, 1.2f, 0.1f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", rodModel, new Vector3(0.1f, 1.2f, 0.1f), Vector3.one * 0.1f, dark);
            var rodMuzzle = new GameObject("Muzzle").transform;
            rodMuzzle.SetParent(rod.transform, false);
            rodMuzzle.localPosition = new Vector3(0f, 1f, 0.5f);
            rod.muzzle = rodMuzzle;
            rod.projectilePrefab = zapPrefab;
            rod.telegraphMaterial = telegraph;
            rod.favouredDrops = new[] { ItemType.NeedleTip, ItemType.StaticSock };
            var rodPrefab = SaveEnemy(rod);

            // Droplet: grows into a raindrop if it is left.
            var droplet = Creature<LarvaEnemy>("Droplet", 0.35f, 0.6f, 2, 0.6f, 1.2f, out var dropletModel);
            Decor(PrimitiveType.Sphere, "Body", dropletModel, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.45f, rainBlue);
            Decor(PrimitiveType.Sphere, "Eye L", dropletModel, new Vector3(-0.08f, 0.3f, 0.2f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", dropletModel, new Vector3(0.08f, 0.3f, 0.2f), Vector3.one * 0.07f, dark);
            droplet.growsInto = dropPrefab;
            droplet.dropsLoot = false;
            droplet.knockbackOnHit = 1f;
            Object.DestroyImmediate(droplet.GetComponent<EnemyMutations>());
            var dropletPrefab = (LarvaEnemy)SaveEnemy(droplet);

            // ----- bosses: a kite that got away, and the weather that took it -----
            var kite = Creature<StormBoss>("TheKite", 0.9f, 2.6f, 110, 1f, 3.6f, out var kiteModel);
            Decor(PrimitiveType.Cube, "Sail", kiteModel, new Vector3(0f, 1.6f, 0f), new Vector3(1.6f, 1.6f, 0.06f), kitePaper).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Decor(PrimitiveType.Cube, "Spar A", kiteModel, new Vector3(0f, 1.6f, -0.05f), new Vector3(0.06f, 2.2f, 0.06f), kiteStripe);
            Decor(PrimitiveType.Cube, "Spar B", kiteModel, new Vector3(0f, 1.6f, -0.05f), new Vector3(2.2f, 0.06f, 0.06f), kiteStripe);
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Cube, "Tail " + i, kiteModel, new Vector3(Mathf.Sin(i) * 0.15f, 0.4f - i * 0.25f, 0f), new Vector3(0.25f, 0.08f, 0.05f), i % 2 == 0 ? kiteStripe : kitePaper);
            Decor(PrimitiveType.Sphere, "Eye L", kiteModel, new Vector3(-0.25f, 1.8f, 0.06f), Vector3.one * 0.18f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", kiteModel, new Vector3(0.25f, 1.8f, 0.06f), Vector3.one * 0.18f, dark);
            kite.displayName = "The Kite";
            kite.subtitle = "Somebody let go of the string. It has not forgiven anyone.";
            kite.canLightning = false;
            kite.gustTime = 1.1f;
            kite.gustSpeed = 7.5f;
            kite.telegraphMaterial = telegraph;
            kite.rainMaterial = puddle;
            kite.friends = new[] { birdPrefab };
            kite.sampleReward = 12;
            kite.knockbackOnHit = 1f;
            Object.DestroyImmediate(kite.GetComponent<EnemyMutations>());
            var kitePrefab = (BossEnemy)SaveEnemy(kite);

            var thunder = Creature<StormBoss>("TheThundercloud", 1.6f, 3.6f, 380, 1f, 5f, out var thunderModel);
            Decor(PrimitiveType.Sphere, "Puff A", thunderModel, new Vector3(0f, 2.2f, 0f), new Vector3(3.4f, 2f, 2.6f), storm);
            Decor(PrimitiveType.Sphere, "Puff B", thunderModel, new Vector3(-1.4f, 2f, 0.2f), Vector3.one * 1.8f, storm);
            Decor(PrimitiveType.Sphere, "Puff C", thunderModel, new Vector3(1.4f, 2.1f, -0.1f), Vector3.one * 1.9f, storm);
            Decor(PrimitiveType.Sphere, "Puff D", thunderModel, new Vector3(0.3f, 3f, 0f), Vector3.one * 1.6f, storm);
            Decor(PrimitiveType.Sphere, "Eye L", thunderModel, new Vector3(-0.55f, 2.5f, 1.2f), new Vector3(0.45f, 0.3f, 0.2f), spark);
            Decor(PrimitiveType.Sphere, "Eye R", thunderModel, new Vector3(0.55f, 2.5f, 1.2f), new Vector3(0.45f, 0.3f, 0.2f), spark);
            Decor(PrimitiveType.Cube, "Frown", thunderModel, new Vector3(0f, 1.9f, 1.28f), new Vector3(0.9f, 0.08f, 0.05f), dark);
            thunder.displayName = "The Thundercloud";
            thunder.subtitle = "It was a perfectly nice day. Then you got here.";
            thunder.telegraphMaterial = telegraph;
            thunder.rainMaterial = puddle;
            thunder.friends = new[] { birdPrefab, arcPrefab };
            thunder.sampleReward = 30;
            thunder.knockbackOnHit = 0.3f;
            Object.DestroyImmediate(thunder.GetComponent<EnemyMutations>());
            var thunderPrefab = (BossEnemy)SaveEnemy(thunder);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(CloudsPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, CloudsPath);
            }

            biome.displayName = "The Clouds";
            biome.id = "clouds";
            biome.mutationTheme = "speed, static, things that fall";
            biome.isBottom = false;
            biome.floorMaterial = cloud;
            biome.wallMaterial = bank;
            biome.doorMaterial = rainbowYellow;
            biome.wallHeight = 5f;
            // Above everything: the sky, a hard bright sun, and a long way to see.
            biome.ceiling = false;
            biome.ceilingMaterial = null;
            biome.skyColor = new Color(0.5f, 0.72f, 1f);
            // Bright, but not so bright the white lettering on screen disappears into it.
            biome.ambientColor = new Color(0.55f, 0.62f, 0.78f);
            biome.sunIntensity = 1f;
            biome.fog = true;
            biome.fogColor = new Color(0.55f, 0.68f, 0.9f);
            biome.fogStart = 30f;
            biome.fogEnd = 110f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = storm;               // dark little clouds to hide behind
            biome.crumbMaterial = cloud;               // wisps
            biome.sugarMaterial = hail;
            biome.rootMaterial = copper;               // weathervane stands
            biome.podMaterial = rainBlue;              // drops waiting to fall
            biome.podHatchling = dropletPrefab;
            biome.dewMaterial = hail;
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = spark;                // ball lightning, held still
            biome.glowStemMaterial = copper;
            biome.glowLightRange = 10f;
            biome.glowLightIntensity = 1.6f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = dropPrefab;
            biome.miniBoss = kitePrefab;
            biome.boss = thunderPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = dropPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = puffPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = stonePrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = birdPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = snowPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = arcPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = rodPrefab, weight = 1, minFloor = 5 },
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
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("CloudsPortal", rainbowYellow, black, bank), "Props/CloudsPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
