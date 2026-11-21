using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the placeholder game content both scenes share: the bullet, the four ants, the eight mutations,
    /// the special-room props and the Ant Colony biome data file that ties them together.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string BiomePath = "Assets/Data/AntColony.asset";

        public static BiomeDefinition Build(out Bullet playerBullet)
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Materials");
            ArenaSceneBuilder.EnsureFolder("Assets", "Prefabs");
            ArenaSceneBuilder.EnsureFolder("Assets/Prefabs", "Props");
            ArenaSceneBuilder.EnsureFolder("Assets/Prefabs", "Enemies");
            ArenaSceneBuilder.EnsureFolder("Assets", "Data");
            ArenaSceneBuilder.EnsureFolder("Assets/Data", "Mutations");

            var antBody = ArenaSceneBuilder.Lit("AntBody", new Color(0.55f, 0.16f, 0.10f), 0.4f);
            var antDark = ArenaSceneBuilder.Lit("AntDark", new Color(0.16f, 0.06f, 0.05f), 0.4f);
            var soldierBody = ArenaSceneBuilder.Lit("SoldierBody", new Color(0.3f, 0.08f, 0.06f), 0.5f);
            var spitterBody = ArenaSceneBuilder.Lit("SpitterBody", new Color(0.45f, 0.5f, 0.12f), 0.4f);
            var wingedBody = ArenaSceneBuilder.Lit("WingedBody", new Color(0.7f, 0.4f, 0.12f), 0.4f);
            var wing = ArenaSceneBuilder.Lit("Wing", new Color(0.85f, 0.9f, 0.95f), 0.8f);
            var acid = ArenaSceneBuilder.Unlit("AcidSpit", new Color(0.7f, 1f, 0.2f));
            var bulletGlow = ArenaSceneBuilder.Unlit("BulletGlow", new Color(0.6f, 1f, 0.95f));

            // ----- shots -----
            var bullet = new GameObject("Bullet");
            Decor(PrimitiveType.Sphere, "Visual", bullet.transform, Vector3.zero, new Vector3(0.22f, 0.22f, 0.5f), bulletGlow);
            bullet.AddComponent<Bullet>();
            playerBullet = ArenaSceneBuilder.SavePrefab(bullet, "Bullet").GetComponent<Bullet>();

            var spit = new GameObject("AcidSpit");
            Decor(PrimitiveType.Sphere, "Visual", spit.transform, Vector3.zero, Vector3.one * 0.5f, acid);
            spit.AddComponent<Bullet>().radius = 0.25f;
            var spitPrefab = ArenaSceneBuilder.SavePrefab(spit, "Enemies/AcidSpit").GetComponent<Bullet>();

            // ----- the four ants, one per base behaviour -----
            var worker = Ant<ChaserEnemy>("WorkerAnt", 1f, 5, antBody, antDark, false, out _);
            worker.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.PheromoneCloud };
            var workerPrefab = SaveEnemy(worker);

            var soldier = Ant<ChargerEnemy>("SoldierAnt", 1.45f, 10, soldierBody, antDark, false, out _);
            soldier.knockbackOnHit = 2f;
            soldier.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.NeedleTip };
            soldier.telegraphMaterial = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var soldierPrefab = SaveEnemy(soldier);

            var spitter = Ant<ShooterEnemy>("SpitterAnt", 0.9f, 4, spitterBody, antDark, false, out _);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(spitter.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.7f, 1f);
            spitter.muzzle = muzzle;
            spitter.projectilePrefab = spitPrefab;
            spitter.favouredDrops = new[] { ItemType.AcidGland, ItemType.CompoundEye };
            var spitterPrefab = SaveEnemy(spitter);

            // The marksman: a spitter that has learned patience. One shot, along a line it shows you first.
            var marksmanBody = ArenaSceneBuilder.Lit("MarksmanBody", new Color(0.3f, 0.2f, 0.4f), 0.5f);
            var marksman = Ant<SniperEnemy>("MarksmanAnt", 0.9f, 5, marksmanBody, antDark, false, out var marksmanModel);
            Decor(PrimitiveType.Cube, "Barrel", marksmanModel, new Vector3(0f, 0.75f, 0.9f), new Vector3(0.12f, 0.12f, 1.4f), antDark);
            var marksmanMuzzle = new GameObject("Muzzle").transform;
            marksmanMuzzle.SetParent(marksman.transform, false);
            marksmanMuzzle.localPosition = new Vector3(0f, 0.75f, 1.7f);
            marksman.muzzle = marksmanMuzzle;
            marksman.projectilePrefab = spitPrefab;
            marksman.telegraphMaterial = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            marksman.favouredDrops = new[] { ItemType.NeedleTip, ItemType.CompoundEye };
            var marksmanPrefab = SaveEnemy(marksman);

            var winged = Ant<FlyerEnemy>("WingedAnt", 0.8f, 3, wingedBody, antDark, true, out var wingedModel);
            var wingLeft = Decor(PrimitiveType.Cube, "Wing L", wingedModel, new Vector3(-0.7f, 0.95f, -0.15f), new Vector3(1.2f, 0.04f, 0.55f), wing).transform;
            wingLeft.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var wingRight = Decor(PrimitiveType.Cube, "Wing R", wingedModel, new Vector3(0.7f, 0.95f, -0.15f), new Vector3(1.2f, 0.04f, 0.55f), wing).transform;
            wingRight.localRotation = Quaternion.Euler(0f, 0f, 18f);
            winged.GetComponent<AntMotion>().wings = new[] { wingLeft, wingRight };
            var wingedController = winged.GetComponent<CharacterController>();
            wingedController.height = 2f;
            wingedController.center = new Vector3(0f, 1f, 0f);
            winged.GetComponent<EnemyMutations>().badgeHeight = 2.9f;
            winged.favouredDrops = new[] { ItemType.WingBuds, ItemType.FireflyLantern };
            var wingedPrefab = SaveEnemy(winged);

            // ----- two more, from floor 5 on: one that throws and one that helps -----
            // Bombardier ant: the Lobber. Throws acid over everything, at where you are going.
            var bombardierBody = ArenaSceneBuilder.Lit("BombardierBody", new Color(0.75f, 0.4f, 0.08f), 0.4f);
            var bombardier = Ant<LobberEnemy>("BombardierAnt", 1.15f, 6, bombardierBody, antDark, false, out var bombardierModel);
            Decor(PrimitiveType.Sphere, "Gland", bombardierModel, new Vector3(0f, 0.95f, -0.8f), Vector3.one * 0.55f, acid);
            bombardier.blastMaterial = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            bombardier.lobMaterial = acid;
            bombardier.favouredDrops = new[] { ItemType.AcidGland, ItemType.FireflyLantern };
            var bombardierPrefab = SaveEnemy(bombardier);

            // Herald ant: the Support. Small, pink, and makes every ant near it quicker.
            var heraldBody = ArenaSceneBuilder.Lit("HeraldBody", new Color(0.8f, 0.35f, 0.55f), 0.4f);
            var herald = Ant<SupportEnemy>("HeraldAnt", 0.85f, 4, heraldBody, antDark, false, out var heraldModel);
            Decor(PrimitiveType.Sphere, "Scent", heraldModel, new Vector3(0f, 1.05f, -0.6f), Vector3.one * 0.45f, ArenaSceneBuilder.Unlit("Confused", new Color(1f, 0.45f, 0.75f)));
            herald.pulseColor = new Color(1f, 0.45f, 0.75f);
            herald.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.ExtraLegs };
            var heraldPrefab = SaveEnemy(herald);

            // Mimic: a heap of coins that is not. The same one turns up in every biome, from floor 4.
            var coinGold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            var mimic = Creature<MimicEnemy>("Mimic", 0.5f, 1.2f, 9, 1f, 2.2f, out var mimicModel);
            var mimicDisguise = new GameObject("Disguise").transform;
            mimicDisguise.SetParent(mimicModel, false);
            Decor(PrimitiveType.Cylinder, "Coin A", mimicDisguise, new Vector3(0f, 0.04f, 0f), new Vector3(0.7f, 0.04f, 0.7f), coinGold);
            Decor(PrimitiveType.Cylinder, "Coin B", mimicDisguise, new Vector3(0.12f, 0.12f, 0.08f), new Vector3(0.7f, 0.04f, 0.7f), coinGold);
            Decor(PrimitiveType.Cylinder, "Coin C", mimicDisguise, new Vector3(-0.3f, 0.3f, -0.1f), new Vector3(0.7f, 0.04f, 0.7f), coinGold).transform.localRotation = Quaternion.Euler(0f, 0f, 62f);
            var mimicBody = new GameObject("Body").transform;
            mimicBody.SetParent(mimicModel, false);
            Decor(PrimitiveType.Cylinder, "Coin", mimicBody, new Vector3(0f, 0.75f, 0f), new Vector3(1.1f, 0.07f, 1.1f), coinGold).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", mimicBody, new Vector3(-0.2f, 0.95f, 0.09f), Vector3.one * 0.16f, antDark);
            Decor(PrimitiveType.Sphere, "Eye R", mimicBody, new Vector3(0.2f, 0.95f, 0.09f), Vector3.one * 0.16f, antDark);
            for (int i = 0; i < 4; i++)
            {
                Decor(PrimitiveType.Cube, "Tooth " + i, mimicBody, new Vector3(-0.24f + i * 0.16f, 0.6f, 0.09f), new Vector3(0.1f, 0.16f, 0.05f), ArenaSceneBuilder.Lit("Sugar", new Color(0.95f, 0.95f, 0.92f), 0.6f));
            }
            Decor(PrimitiveType.Cube, "Leg L", mimicBody, new Vector3(-0.22f, 0.12f, 0f), new Vector3(0.08f, 0.28f, 0.08f), antDark);
            Decor(PrimitiveType.Cube, "Leg R", mimicBody, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.08f, 0.28f, 0.08f), antDark);
            mimicBody.gameObject.SetActive(false);
            mimic.disguise = mimicDisguise.gameObject;
            mimic.body = mimicBody.gameObject;
            mimic.moveSpeed = 4.8f;
            mimic.favouredDrops = new[] { ItemType.FridgeMagnet, ItemType.Lunchbox };
            var mimicPrefab = SaveEnemy(mimic);

            // ----- bosses and what they bring -----
            var pale = ArenaSceneBuilder.Lit("LarvaPod", new Color(0.9f, 0.85f, 0.6f), 0.5f);
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var enemyAcid = ArenaSceneBuilder.Unlit("MutationAcidBlood", new Color(0.7f, 1f, 0.2f));
            var royal = ArenaSceneBuilder.Lit("QueenBody", new Color(0.45f, 0.1f, 0.35f), 0.6f);
            var gold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));

            // Larva: a few pale segments on the floor. Grows into a Worker.
            var larvaRoot = new GameObject("Larva");
            var larvaController = larvaRoot.AddComponent<CharacterController>();
            larvaController.radius = 0.4f;
            larvaController.height = 0.8f;
            larvaController.center = new Vector3(0f, 0.4f, 0f);
            var larvaHealth = larvaRoot.AddComponent<Health>();
            larvaHealth.team = Team.Enemy;
            larvaHealth.maxHealth = 2;
            var larvaVisual = new GameObject("Visual").transform;
            larvaVisual.SetParent(larvaRoot.transform, false);
            for (int i = 0; i < 3; i++)
            {
                Decor(PrimitiveType.Sphere, "Segment " + i, larvaVisual, new Vector3(0f, 0.3f, 0.3f - i * 0.3f), Vector3.one * (0.5f - i * 0.06f), pale);
            }
            var larva = larvaRoot.AddComponent<LarvaEnemy>();
            larva.visual = larvaVisual;
            larva.growsInto = workerPrefab;
            larva.dropsLoot = false;
            larva.knockbackOnHit = 1f;
            larvaRoot.AddComponent<HitFlash>();
            var larvaPrefab = (LarvaEnemy)SaveEnemy(larva);

            // Egg cluster: a clump of eggs against the wall.
            var eggRoot = new GameObject("EggCluster");
            var eggController = eggRoot.AddComponent<CharacterController>();
            eggController.radius = 0.9f;
            eggController.height = 1.8f;
            eggController.center = new Vector3(0f, 0.9f, 0f);
            var eggHealth = eggRoot.AddComponent<Health>();
            eggHealth.team = Team.Enemy;
            eggHealth.maxHealth = 6;
            var eggVisual = new GameObject("Visual").transform;
            eggVisual.SetParent(eggRoot.transform, false);
            Vector3[] eggSpots = { new Vector3(0f, 0.5f, 0f), new Vector3(0.55f, 0.4f, 0.2f), new Vector3(-0.5f, 0.4f, 0.25f), new Vector3(0.1f, 0.4f, -0.55f), new Vector3(0.1f, 1.05f, 0f) };
            for (int i = 0; i < eggSpots.Length; i++)
            {
                Decor(PrimitiveType.Sphere, "Egg " + i, eggVisual, eggSpots[i], new Vector3(0.7f, 0.95f, 0.7f), pale);
            }
            var egg = eggRoot.AddComponent<EggEnemy>();
            egg.visual = eggVisual;
            egg.dropsLoot = false;
            egg.knockbackOnHit = 0f;
            eggRoot.AddComponent<HitFlash>();
            var eggPrefab = (EggEnemy)SaveEnemy(egg);

            // Brood Mother: a giant ant with a swollen egg sac on her back (the weak point).
            var mother = Ant<BroodMother>("BroodMother", 3f, 140, soldierBody, antDark, false, out var motherModel);
            Decor(PrimitiveType.Sphere, "Egg Sac", motherModel, new Vector3(0f, 0.95f, -0.85f), new Vector3(0.95f, 0.8f, 1.1f), pale);
            mother.displayName = "Brood Mother";
            mother.subtitle = "4,000 children. Knows none of their names.";
            mother.larvaPrefab = larvaPrefab;
            mother.telegraphMaterial = telegraph;
            mother.sampleReward = 12;
            mother.knockbackOnHit = 0f;
            var motherPrefab = (BossEnemy)SaveEnemy(mother);

            // Ant Queen: bigger still, royal purple, with a crown.
            var queen = Ant<AntQueen>("AntQueen", 3.4f, 300, royal, antDark, false, out var queenModel);
            Decor(PrimitiveType.Cylinder, "Crown", queenModel, new Vector3(0f, 0.98f, 0.5f), new Vector3(0.4f, 0.06f, 0.4f), gold);
            for (int i = 0; i < 5; i++)
            {
                Decor(PrimitiveType.Cube, "Crown Point " + i, queenModel, new Vector3(0f, 1.08f, 0.5f) + Quaternion.Euler(0f, i * 72f, 0f) * new Vector3(0.17f, 0f, 0f), Vector3.one * 0.08f, gold);
            }
            var queenMuzzle = new GameObject("Muzzle").transform;
            queenMuzzle.SetParent(queen.transform, false);
            queenMuzzle.localPosition = new Vector3(0f, 0.7f, 3.6f);
            queen.displayName = "Ant Queen";
            queen.subtitle = "Has never once opened a door for herself.";
            queen.muzzle = queenMuzzle;
            queen.projectilePrefab = spitPrefab;
            queen.eggPrefab = eggPrefab;
            queen.soldierPrefab = soldierPrefab;
            queen.telegraphMaterial = telegraph;
            queen.acidMaterial = enemyAcid;
            queen.sampleReward = 30;
            queen.knockbackOnHit = 0f;
            var queenPrefab = (BossEnemy)SaveEnemy(queen);

            // ----- biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, BiomePath);
            }

            biome.displayName = "Backyard Ant Colony";
            biome.id = "ants";
            biome.mutationTheme = "acid, armour, swarming";
            biome.floorMaterial = ArenaSceneBuilder.Lit("Soil", new Color(0.36f, 0.25f, 0.16f), 0.05f);
            biome.wallMaterial = ArenaSceneBuilder.Lit("Wall", new Color(0.23f, 0.15f, 0.10f), 0.05f);
            biome.doorMaterial = ArenaSceneBuilder.Lit("DoorLocked", new Color(0.85f, 0.3f, 0.15f), 0.3f);
            biome.ceilingMaterial = ArenaSceneBuilder.Lit("Ceiling", new Color(0.3f, 0.2f, 0.13f), 0.05f);
            biome.ceiling = true;
            biome.coverMaterial = ArenaSceneBuilder.Lit("Pebble", new Color(0.5f, 0.47f, 0.42f), 0.25f);
            biome.crumbMaterial = ArenaSceneBuilder.Lit("Crumb", new Color(0.82f, 0.62f, 0.34f), 0.1f);
            biome.sugarMaterial = ArenaSceneBuilder.Lit("Sugar", new Color(0.95f, 0.95f, 0.92f), 0.6f);
            biome.rootMaterial = ArenaSceneBuilder.Lit("RootColumn", new Color(0.42f, 0.3f, 0.2f), 0.15f);
            biome.podMaterial = ArenaSceneBuilder.Lit("LarvaPod", new Color(0.9f, 0.85f, 0.6f), 0.5f);
            biome.podHatchling = larvaPrefab;
            biome.dewMaterial = ArenaSceneBuilder.Lit("Dew", new Color(0.45f, 0.75f, 0.95f), 0.9f);
            biome.eliteMaterial = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            biome.glowMaterial = ArenaSceneBuilder.Unlit("GlowCap", new Color(0.35f, 1f, 0.85f));
            biome.glowStemMaterial = ArenaSceneBuilder.Lit("GlowStem", new Color(0.8f, 0.85f, 0.75f), 0.2f);
            biome.tilesPerExtraEnemy = 60;
            biome.maxExtraEnemiesForSize = 4;
            biome.wallHeight = 6f;
            biome.swarmEnemy = workerPrefab;
            biome.miniBoss = motherPrefab;
            biome.boss = queenPrefab;
            // Antlion: the Burrower. Sinks, then comes up under you from a marked circle.
            var antlionBody = ArenaSceneBuilder.Lit("AntlionBody", new Color(0.55f, 0.45f, 0.3f), 0.3f);
            var antlion = Ant<BurrowerEnemy>("Antlion", 1.15f, 6, antlionBody, antDark, false, out var antlionModel);
            Decor(PrimitiveType.Cube, "Jaw L", antlionModel, new Vector3(-0.2f, 0.45f, 0.75f), new Vector3(0.08f, 0.08f, 0.5f), antDark).transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            Decor(PrimitiveType.Cube, "Jaw R", antlionModel, new Vector3(0.2f, 0.45f, 0.75f), new Vector3(0.08f, 0.08f, 0.5f), antDark).transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
            antlion.telegraphMaterial = telegraph;
            antlion.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.ExtraLegs };
            var antlionPrefab = SaveEnemy(antlion);

            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = antlionPrefab, weight = 1, minFloor = 3 },
                new EnemySpawnEntry { prefab = workerPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = spitterPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = soldierPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = wingedPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = bombardierPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = heraldPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = mimicPrefab, weight = 1, minFloor = 4 },
                new EnemySpawnEntry { prefab = marksmanPrefab, weight = 1, minFloor = 5 },
            };
            biome.mutations = new[]
            {
                Mutation(MutationType.Fast, "Fast", 1, "Moves and attacks quicker.", new Color(0.3f, 0.6f, 1f)),
                Mutation(MutationType.Armored, "Armored", 2, "Takes half damage.", new Color(0.6f, 0.62f, 0.66f)),
                Mutation(MutationType.Regenerates, "Regenerates", 2, "Heals if not hit for 2 seconds.", new Color(0.3f, 0.9f, 0.4f)),
                Mutation(MutationType.ExplodesOnDeath, "Explodes on death", 2, "Bursts after a short fuse when killed.", new Color(1f, 0.2f, 0.15f)),
                Mutation(MutationType.AcidBlood, "Acid blood", 1, "Leaves an acid pool on death.", new Color(0.7f, 1f, 0.2f)),
                Mutation(MutationType.SwarmCall, "Swarm call", 3, "Summons 3 workers once, at half health.", new Color(1f, 0.85f, 0.2f)),
                Mutation(MutationType.HardenedShell, "Hardened shell", 2, "Blocks shots from the front.", new Color(0.85f, 0.9f, 1f)),
                Mutation(MutationType.PheromoneTrail, "Pheromone trail", 2, "Ants crossing its trail get faster.", new Color(1f, 0.45f, 0.75f)),
            };

            BuildProps(biome);
            EditorUtility.SetDirty(biome);
            return biome;
        }

        // ---------- items ----------

        /// <summary>Puts the shared asset holder on a scene's Game object.</summary>
        public static void AddGameAssets(GameObject game)
        {
            var assets = game.AddComponent<GameAssets>();
            game.AddComponent<ScreenCards>();
            game.AddComponent<AchievementTracker>();
            game.AddComponent<JobTracker>();
            game.AddComponent<KillStreak>();
            game.AddComponent<Radio>();
            game.AddComponent<DamageNumbers>();
            game.AddComponent<Fx>().material = ArenaSceneBuilder.LoadOrCreate("Particle", "Universal Render Pipeline/Particles/Unlit");
            assets.items = BuildItems(out var larva);
            assets.larvaPrefab = larva;
            assets.note = ArenaSceneBuilder.Unlit("FinchNote", new Color(1f, 0.96f, 0.8f));
            assets.health = ArenaSceneBuilder.Unlit("HealthCrumb", new Color(1f, 0.45f, 0.5f));
            assets.playerAcid = ArenaSceneBuilder.Unlit("PlayerAcid", new Color(0.35f, 0.95f, 0.75f));
            assets.faded = ArenaSceneBuilder.Faded();
            assets.fire = ArenaSceneBuilder.Unlit("Fire", new Color(1f, 0.55f, 0.1f));
            assets.confused = ArenaSceneBuilder.Unlit("Confused", new Color(1f, 0.45f, 0.75f));
            assets.chilled = ArenaSceneBuilder.Unlit("IceGlow", new Color(0.55f, 0.85f, 1f));
            assets.coin = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            assets.sample = ArenaSceneBuilder.Unlit("Sample", new Color(0.45f, 0.95f, 1f));
        }

        // Item looks are built around the middle of the Void's body: the capsule is 1.8 tall and 0.8 wide,
        // its eyes are at height 0.45 on the front, and its feet are at -0.9.
        static ItemLibrary BuildItems(out LarvaBuddy larvaPrefab)
        {
            ArenaSceneBuilder.EnsureFolder("Assets/Data", "Items");
            ArenaSceneBuilder.EnsureFolder("Assets/Prefabs", "Items");

            var silver = ArenaSceneBuilder.Lit("ItemSilver", new Color(0.8f, 0.82f, 0.88f), 0.8f);
            var chitin = ArenaSceneBuilder.Lit("ItemChitin", new Color(0.35f, 0.3f, 0.28f), 0.6f);
            var white = ArenaSceneBuilder.Lit("Sugar", new Color(0.95f, 0.95f, 0.92f), 0.6f);
            var pale = ArenaSceneBuilder.Lit("LarvaPod", new Color(0.9f, 0.85f, 0.6f), 0.5f);

            var items = new System.Collections.Generic.List<ItemDefinition>();

            // Needle Tip: a spike on the hand.
            var look = new GameObject("NeedleTip");
            Decor(PrimitiveType.Cylinder, "Spike", look.transform, new Vector3(0.45f, 0f, 0.5f), new Vector3(0.07f, 0.4f, 0.07f), silver).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            items.Add(Item(ItemType.NeedleTip, "Needle Tip", "Shots pierce through enemies.", ItemSlot.Hands, false, 20, new Color(0.8f, 0.82f, 0.88f), look));

            // Splitter Mandible: mandibles on the face.
            look = new GameObject("SplitterMandible");
            Decor(PrimitiveType.Cube, "Mandible L", look.transform, new Vector3(-0.14f, 0.25f, 0.5f), new Vector3(0.07f, 0.07f, 0.35f), chitin).transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            Decor(PrimitiveType.Cube, "Mandible R", look.transform, new Vector3(0.14f, 0.25f, 0.5f), new Vector3(0.07f, 0.07f, 0.35f), chitin).transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
            items.Add(Item(ItemType.SplitterMandible, "Splitter Mandible", "Shots split into 3 on hit.", ItemSlot.Head, false, 25, new Color(0.75f, 0.45f, 0.25f), look));

            // Acid Gland: a green drip from the hands.
            look = new GameObject("AcidGland");
            var acidMat = ArenaSceneBuilder.Unlit("PlayerAcid", new Color(0.35f, 0.95f, 0.75f));
            Decor(PrimitiveType.Sphere, "Gland", look.transform, new Vector3(-0.45f, -0.05f, 0.3f), Vector3.one * 0.26f, acidMat);
            Decor(PrimitiveType.Sphere, "Drip", look.transform, new Vector3(-0.45f, -0.3f, 0.3f), new Vector3(0.1f, 0.18f, 0.1f), acidMat);
            items.Add(Item(ItemType.AcidGland, "Acid Gland", "Shots leave a small acid puddle.", ItemSlot.Hands, false, 20, new Color(0.35f, 0.95f, 0.75f), look));

            // Firefly Lantern: a glowing tail light.
            look = new GameObject("FireflyLantern");
            Decor(PrimitiveType.Sphere, "Lantern", look.transform, new Vector3(0f, -0.25f, -0.5f), Vector3.one * 0.4f, ArenaSceneBuilder.Unlit("Fire", new Color(1f, 0.55f, 0.1f)));
            items.Add(Item(ItemType.FireflyLantern, "Firefly Lantern", "Shots set enemies on fire.", ItemSlot.Back, false, 25, new Color(1f, 0.55f, 0.1f), look));

            // Compound Eye: clusters of extra eyes.
            look = new GameObject("CompoundEye");
            var eyeMat = ArenaSceneBuilder.Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f;
                int n = i % 3;
                Decor(PrimitiveType.Sphere, "Eye " + i, look.transform, new Vector3(side * (0.26f + 0.06f * n), 0.5f + 0.1f * (n - 1), 0.3f - 0.05f * n), Vector3.one * 0.09f, eyeMat);
            }
            items.Add(Item(ItemType.CompoundEye, "Compound Eye", "Stronger aim assist, +15% critical hits. Arrows for every unseen enemy.", ItemSlot.Eyes, false, 20, new Color(0.45f, 0.95f, 1f), look));

            // Extra Legs: four little legs on the back.
            look = new GameObject("ExtraLegs");
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float y = i < 2 ? 0.15f : -0.2f;
                Decor(PrimitiveType.Cube, "Leg " + i, look.transform, new Vector3(side * 0.45f, y, -0.3f), new Vector3(0.5f, 0.06f, 0.06f), chitin).transform.localRotation = Quaternion.Euler(0f, side * 30f, side * -35f);
            }
            items.Add(Item(ItemType.ExtraLegs, "Extra Legs", "Faster movement, dodge recharges quicker.", ItemSlot.Back, false, 20, new Color(0.5f, 0.4f, 0.35f), look));

            // Wing Buds: small wings.
            look = new GameObject("WingBuds");
            var wingMat = ArenaSceneBuilder.Lit("Wing", new Color(0.85f, 0.9f, 0.95f), 0.8f);
            Decor(PrimitiveType.Cube, "Wing L", look.transform, new Vector3(-0.4f, 0.3f, -0.4f), new Vector3(0.6f, 0.03f, 0.3f), wingMat).transform.localRotation = Quaternion.Euler(0f, 20f, 25f);
            Decor(PrimitiveType.Cube, "Wing R", look.transform, new Vector3(0.4f, 0.3f, -0.4f), new Vector3(0.6f, 0.03f, 0.3f), wingMat).transform.localRotation = Quaternion.Euler(0f, -20f, -25f);
            items.Add(Item(ItemType.WingBuds, "Wing Buds", "Dodge becomes a short hover over hazards.", ItemSlot.Back, false, 20, new Color(0.85f, 0.9f, 0.95f), look));

            // Chitin Plate: shell plates on the body.
            look = new GameObject("ChitinPlate");
            Decor(PrimitiveType.Sphere, "Chest", look.transform, new Vector3(0f, 0.05f, 0.3f), new Vector3(0.6f, 0.5f, 0.3f), chitin);
            Decor(PrimitiveType.Sphere, "Belly", look.transform, new Vector3(0f, -0.4f, 0.28f), new Vector3(0.55f, 0.4f, 0.3f), chitin);
            Decor(PrimitiveType.Sphere, "Back", look.transform, new Vector3(0f, -0.1f, -0.3f), new Vector3(0.6f, 0.8f, 0.3f), chitin);
            items.Add(Item(ItemType.ChitinPlate, "Chitin Plate", "Blocks the first hit in every room.", ItemSlot.Body, false, 25, new Color(0.45f, 0.4f, 0.38f), look));

            // Sugar Cube: sugar crystals on the shoulders.
            look = new GameObject("SugarCube");
            Decor(PrimitiveType.Cube, "Crystal L", look.transform, new Vector3(-0.38f, 0.6f, 0f), Vector3.one * 0.2f, white).transform.localRotation = Quaternion.Euler(20f, 30f, 10f);
            Decor(PrimitiveType.Cube, "Crystal R", look.transform, new Vector3(0.38f, 0.6f, 0f), Vector3.one * 0.2f, white).transform.localRotation = Quaternion.Euler(-15f, 50f, 25f);
            Decor(PrimitiveType.Cube, "Crystal R2", look.transform, new Vector3(0.3f, 0.72f, -0.1f), Vector3.one * 0.12f, white).transform.localRotation = Quaternion.Euler(40f, 10f, 5f);
            items.Add(Item(ItemType.SugarCube, "Sugar Cube", "+1 max health and a full heal.", ItemSlot.Body, false, 25, new Color(0.95f, 0.95f, 0.92f), look));

            // Pheromone Cloud: a pink haze around the feet.
            look = new GameObject("PheromoneCloud");
            Decor(PrimitiveType.Cylinder, "Haze", look.transform, new Vector3(0f, -0.87f, 0f), new Vector3(2.4f, 0.01f, 2.4f), ArenaSceneBuilder.Unlit("Confused", new Color(1f, 0.45f, 0.75f)));
            items.Add(Item(ItemType.PheromoneCloud, "Pheromone Cloud", "Enemies near you sometimes fight each other.", ItemSlot.Aura, false, 25, new Color(1f, 0.45f, 0.75f), look));

            // Larva Buddy: not worn; it follows the player around.
            var larva = new GameObject("LarvaBuddy");
            for (int i = 0; i < 4; i++)
            {
                Decor(PrimitiveType.Sphere, "Segment " + i, larva.transform, new Vector3(0f, 0.25f, 0.3f - i * 0.25f), Vector3.one * (0.42f - i * 0.05f), pale);
            }
            larva.AddComponent<LarvaBuddy>();
            larvaPrefab = ArenaSceneBuilder.SavePrefab(larva, "Items/LarvaBuddy").GetComponent<LarvaBuddy>();
            items.Add(Item(ItemType.LarvaBuddy, "Larva Buddy", "A small larva follows you and bites enemies.", ItemSlot.Companion, false, 25, new Color(0.9f, 0.85f, 0.6f), null));

            // Queen's Jelly: a golden glow and a tiny crown.
            look = new GameObject("QueensJelly");
            var gold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            Decor(PrimitiveType.Cylinder, "Glow", look.transform, new Vector3(0f, -0.86f, 0f), new Vector3(1.8f, 0.01f, 1.8f), gold);
            Decor(PrimitiveType.Cylinder, "Crown", look.transform, new Vector3(0f, 0.98f, 0f), new Vector3(0.3f, 0.08f, 0.3f), gold);
            for (int i = 0; i < 4; i++)
            {
                var point = Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(0.13f, 1.1f, 0f);
                Decor(PrimitiveType.Cube, "Point " + i, look.transform, point, Vector3.one * 0.07f, gold);
            }
            items.Add(Item(ItemType.QueensJelly, "Queen's Jelly", "Every stat up a little.", ItemSlot.Aura, true, 0, new Color(1f, 0.82f, 0.2f), look));

            // Static Sock: a striped sock worn as a scarf, crackling.
            look = new GameObject("StaticSock");
            var sockMat = ArenaSceneBuilder.Lit("ItemSock", new Color(0.3f, 0.45f, 0.85f), 0.1f);
            var sparkMat = ArenaSceneBuilder.Unlit("StaticSpark", new Color(1f, 0.95f, 0.4f));
            Decor(PrimitiveType.Cylinder, "Sock", look.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 0.07f, 0.6f), sockMat);
            Decor(PrimitiveType.Cube, "Toe", look.transform, new Vector3(-0.2f, 0.3f, -0.34f), new Vector3(0.18f, 0.4f, 0.1f), sockMat).transform.localRotation = Quaternion.Euler(-15f, 0f, 10f);
            Decor(PrimitiveType.Sphere, "Spark A", look.transform, new Vector3(0.32f, 0.62f, 0.1f), Vector3.one * 0.08f, sparkMat);
            Decor(PrimitiveType.Sphere, "Spark B", look.transform, new Vector3(-0.3f, 0.6f, 0.2f), Vector3.one * 0.06f, sparkMat);
            items.Add(Item(ItemType.StaticSock, "Static Sock", "Every 5th shot is charged: double damage, and it goes through.", ItemSlot.Head, false, 25, new Color(0.3f, 0.45f, 0.85f), look));

            // Birthday Candle: a lit candle on the head.
            look = new GameObject("BirthdayCandle");
            Decor(PrimitiveType.Cylinder, "Candle", look.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.1f, 0.16f, 0.1f), ArenaSceneBuilder.Lit("CandleWax", new Color(0.55f, 0.8f, 0.95f), 0.5f));
            Decor(PrimitiveType.Sphere, "Flame", look.transform, new Vector3(0f, 1.28f, 0f), new Vector3(0.1f, 0.16f, 0.1f), ArenaSceneBuilder.Unlit("Fire", new Color(1f, 0.55f, 0.1f)));
            items.Add(Item(ItemType.BirthdayCandle, "Birthday Candle", "Dodging leaves a patch of fire where you were.", ItemSlot.Head, false, 25, new Color(1f, 0.7f, 0.3f), look));

            // Lost Marble: a glass marble carried in the off hand.
            look = new GameObject("LostMarble");
            var marbleMat = ArenaSceneBuilder.Lit("MarbleBlue", new Color(0.15f, 0.45f, 0.9f), 0.97f);
            Decor(PrimitiveType.Sphere, "Marble", look.transform, new Vector3(-0.5f, -0.05f, 0.25f), Vector3.one * 0.3f, marbleMat);
            items.Add(Item(ItemType.LostMarble, "Lost Marble", "Shots hit twice as hard, and come a third slower.", ItemSlot.Hands, false, 25, new Color(0.15f, 0.45f, 0.9f), look));

            // Sprinkles: scattered over the shoulders.
            look = new GameObject("Sprinkles");
            Color[] sprinkleColours = { new Color(1f, 0.35f, 0.75f), new Color(0.3f, 0.8f, 1f), new Color(1f, 0.9f, 0.3f), new Color(0.5f, 1f, 0.5f) };
            for (int i = 0; i < 8; i++)
            {
                var at = new Vector3(Mathf.Sin(i * 2.4f) * 0.34f, 0.45f + 0.05f * (i % 3), Mathf.Cos(i * 2.4f) * 0.3f);
                var bit = Decor(PrimitiveType.Capsule, "Sprinkle " + i, look.transform, at, new Vector3(0.05f, 0.07f, 0.05f), ArenaSceneBuilder.Unlit("Sprinkle" + (i % 4), sprinkleColours[i % 4]));
                bit.transform.localRotation = Quaternion.Euler(i * 47f, i * 83f, 90f);
            }
            items.Add(Item(ItemType.Sprinkles, "Sprinkles", "Fire faster.", ItemSlot.Body, false, 20, new Color(1f, 0.35f, 0.75f), look));

            // Finch's Dose: what was left in the flask. Rare: only Finch has any.
            look = new GameObject("FinchsDose");
            var doseMat = ArenaSceneBuilder.Unlit("FinchDose", new Color(0.45f, 1f, 0.6f));
            Decor(PrimitiveType.Sphere, "Flask", look.transform, new Vector3(0.3f, -0.35f, 0.3f), Vector3.one * 0.2f, doseMat);
            Decor(PrimitiveType.Cylinder, "Neck", look.transform, new Vector3(0.3f, -0.22f, 0.3f), new Vector3(0.07f, 0.06f, 0.07f), silver);
            items.Add(Item(ItemType.FinchsDose, "Finch's Dose", "Smaller again: less of you to hit, quicker on your feet, dodge back sooner.", ItemSlot.Aura, true, 0, new Color(0.45f, 1f, 0.6f), look));

            // Rubber Band: wound round one wrist.
            look = new GameObject("RubberBand");
            var rubber = ArenaSceneBuilder.Lit("ItemRubber", new Color(0.8f, 0.55f, 0.3f), 0.4f);
            Decor(PrimitiveType.Cylinder, "Band", look.transform, new Vector3(0.45f, -0.05f, 0.25f), new Vector3(0.3f, 0.03f, 0.3f), rubber).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Decor(PrimitiveType.Cylinder, "Band 2", look.transform, new Vector3(0.47f, -0.05f, 0.25f), new Vector3(0.26f, 0.03f, 0.26f), rubber).transform.localRotation = Quaternion.Euler(20f, 0f, 90f);
            items.Add(Item(ItemType.RubberBand, "Rubber Band", "Shots come back off the first wall they hit.", ItemSlot.Hands, false, 25, new Color(0.8f, 0.55f, 0.3f), look));

            // Ice Cube: balanced on the head, not melting.
            look = new GameObject("IceCube");
            Decor(PrimitiveType.Cube, "Cube", look.transform, new Vector3(0f, 1.08f, 0f), Vector3.one * 0.32f, ArenaSceneBuilder.Lit("IceCube", new Color(0.7f, 0.88f, 1f), 0.95f)).transform.localRotation = Quaternion.Euler(8f, 25f, 6f);
            items.Add(Item(ItemType.IceCube, "Ice Cube", "Shots chill what they hit: it does everything slower for a while.", ItemSlot.Head, false, 25, new Color(0.7f, 0.88f, 1f), look));

            // Fridge Magnet: a horseshoe on the back.
            look = new GameObject("FridgeMagnet");
            var magnetRed = ArenaSceneBuilder.Lit("ItemMagnetRed", new Color(0.85f, 0.12f, 0.12f), 0.6f);
            Decor(PrimitiveType.Cube, "Bar", look.transform, new Vector3(0f, 0.25f, -0.42f), new Vector3(0.5f, 0.14f, 0.12f), magnetRed);
            Decor(PrimitiveType.Cube, "Arm L", look.transform, new Vector3(-0.19f, 0.02f, -0.42f), new Vector3(0.12f, 0.36f, 0.12f), magnetRed);
            Decor(PrimitiveType.Cube, "Arm R", look.transform, new Vector3(0.19f, 0.02f, -0.42f), new Vector3(0.12f, 0.36f, 0.12f), magnetRed);
            Decor(PrimitiveType.Cube, "Tip L", look.transform, new Vector3(-0.19f, -0.2f, -0.42f), new Vector3(0.13f, 0.1f, 0.13f), silver);
            Decor(PrimitiveType.Cube, "Tip R", look.transform, new Vector3(0.19f, -0.2f, -0.42f), new Vector3(0.13f, 0.1f, 0.13f), silver);
            items.Add(Item(ItemType.FridgeMagnet, "Fridge Magnet", "Coins and samples fly to you from much further off.", ItemSlot.Back, false, 15, new Color(0.85f, 0.12f, 0.12f), look));

            // Spare Battery: strapped to the hip.
            look = new GameObject("SpareBattery");
            Decor(PrimitiveType.Cylinder, "Cell", look.transform, new Vector3(-0.42f, -0.35f, 0f), new Vector3(0.16f, 0.2f, 0.16f), ArenaSceneBuilder.Lit("ItemBattery", new Color(0.15f, 0.15f, 0.18f), 0.6f));
            Decor(PrimitiveType.Cylinder, "Band", look.transform, new Vector3(-0.42f, -0.22f, 0f), new Vector3(0.165f, 0.06f, 0.165f), ArenaSceneBuilder.Unlit("StaticSpark", new Color(1f, 0.95f, 0.4f)));
            Decor(PrimitiveType.Cylinder, "Nub", look.transform, new Vector3(-0.42f, -0.13f, 0f), new Vector3(0.06f, 0.03f, 0.06f), silver);
            items.Add(Item(ItemType.SpareBattery, "Spare Battery", "Your hero power fills a third faster.", ItemSlot.Body, false, 25, new Color(1f, 0.95f, 0.4f), look));

            // Lunchbox: carried on the back.
            look = new GameObject("Lunchbox");
            Decor(PrimitiveType.Cube, "Box", look.transform, new Vector3(0f, -0.25f, -0.45f), new Vector3(0.5f, 0.36f, 0.2f), ArenaSceneBuilder.Lit("ItemLunchbox", new Color(0.3f, 0.65f, 0.85f), 0.5f));
            Decor(PrimitiveType.Cube, "Handle", look.transform, new Vector3(0f, -0.03f, -0.45f), new Vector3(0.22f, 0.05f, 0.05f), silver);
            items.Add(Item(ItemType.Lunchbox, "Lunchbox", "Enemies leave crumbs twice as often when you are hurt.", ItemSlot.Back, false, 20, new Color(0.3f, 0.65f, 0.85f), look));

            // Paper Hat: folded from a page, worn at an angle.
            look = new GameObject("PaperHat");
            var hatPaper = ArenaSceneBuilder.Lit("ItemPaperHat", new Color(0.95f, 0.93f, 0.85f), 0.05f);
            Decor(PrimitiveType.Cube, "Crown", look.transform, new Vector3(0f, 1.12f, 0f), new Vector3(0.5f, 0.3f, 0.06f), hatPaper).transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            Decor(PrimitiveType.Cube, "Brim", look.transform, new Vector3(0f, 0.98f, 0f), new Vector3(0.7f, 0.06f, 0.3f), hatPaper).transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            items.Add(Item(ItemType.PaperHat, "Paper Hat", "Shots hit harder. Costs a point of health, for good.", ItemSlot.Head, false, 15, new Color(0.95f, 0.93f, 0.85f), look));

            // Piggy Bank: on the back, slot upward.
            look = new GameObject("PiggyBank");
            var piggyPink = ArenaSceneBuilder.Lit("ItemPiggy", new Color(1f, 0.6f, 0.7f), 0.5f);
            Decor(PrimitiveType.Sphere, "Pig", look.transform, new Vector3(0f, 0.3f, -0.42f), new Vector3(0.42f, 0.36f, 0.5f), piggyPink);
            Decor(PrimitiveType.Sphere, "Snout", look.transform, new Vector3(0f, 0.3f, -0.7f), Vector3.one * 0.16f, piggyPink);
            Decor(PrimitiveType.Cube, "Slot", look.transform, new Vector3(0f, 0.49f, -0.42f), new Vector3(0.04f, 0.02f, 0.2f), ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f)));
            items.Add(Item(ItemType.PiggyBank, "Piggy Bank", "Everything you put down drops one more coin.", ItemSlot.Back, false, 20, new Color(1f, 0.6f, 0.7f), look));

            // Cold Coffee: a paper cup in the hand, long since given up on.
            look = new GameObject("ColdCoffee");
            var cup = ArenaSceneBuilder.Lit("ItemCup", new Color(0.9f, 0.88f, 0.82f), 0.2f);
            Decor(PrimitiveType.Cylinder, "Cup", look.transform, new Vector3(-0.45f, 0.05f, 0.25f), new Vector3(0.2f, 0.14f, 0.2f), cup);
            Decor(PrimitiveType.Cylinder, "Coffee", look.transform, new Vector3(-0.45f, 0.19f, 0.25f), new Vector3(0.17f, 0.01f, 0.17f), ArenaSceneBuilder.Lit("ItemCoffee", new Color(0.25f, 0.15f, 0.08f), 0.6f));
            items.Add(Item(ItemType.ColdCoffee, "Cold Coffee", "Faster on your feet and the trigger. The dodge takes longer to come back.", ItemSlot.Hands, false, 20, new Color(0.45f, 0.28f, 0.15f), look));

            // Rain Boot: a pair of yellow wellingtons. Whatever is on the floor goes up as well as out.
            look = new GameObject("RainBoot");
            var boot = ArenaSceneBuilder.Lit("ItemBoot", new Color(1f, 0.82f, 0.15f), 0.6f);
            Decor(PrimitiveType.Cylinder, "Boot L", look.transform, new Vector3(-0.2f, 0.12f, 0f), new Vector3(0.24f, 0.12f, 0.24f), boot);
            Decor(PrimitiveType.Cylinder, "Boot R", look.transform, new Vector3(0.2f, 0.12f, 0f), new Vector3(0.24f, 0.12f, 0.24f), boot);
            items.Add(Item(ItemType.RainBoot, "Rain Boot", "Your puddles splash up: they hurt fliers too.", ItemSlot.Body, false, 15, new Color(1f, 0.82f, 0.15f), look));

            // Umbrella: a small one, up, over the back.
            look = new GameObject("Umbrella");
            var canopy = ArenaSceneBuilder.Lit("ItemUmbrella", new Color(0.25f, 0.4f, 0.85f), 0.5f);
            Decor(PrimitiveType.Sphere, "Canopy", look.transform, new Vector3(0f, 2.05f, -0.15f), new Vector3(1.1f, 0.3f, 1.1f), canopy);
            Decor(PrimitiveType.Cylinder, "Shaft", look.transform, new Vector3(0f, 1.6f, -0.3f), new Vector3(0.04f, 0.45f, 0.04f), ArenaSceneBuilder.Lit("ItemCup", new Color(0.9f, 0.88f, 0.82f), 0.2f));
            items.Add(Item(ItemType.Umbrella, "Umbrella", "The first blast or puddle that would hurt you in each room does not.", ItemSlot.Head, false, 25, new Color(0.25f, 0.4f, 0.85f), look));

            // Save State: a cartridge on the back, with a little red light.
            look = new GameObject("SaveState");
            Decor(PrimitiveType.Cube, "Cartridge", look.transform, new Vector3(0f, 1.05f, -0.45f), new Vector3(0.45f, 0.5f, 0.12f), ArenaSceneBuilder.Lit("ItemCartridge", new Color(0.3f, 0.3f, 0.34f), 0.5f));
            Decor(PrimitiveType.Sphere, "Light", look.transform, new Vector3(0.12f, 1.2f, -0.52f), Vector3.one * 0.07f, ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f)));
            items.Add(Item(ItemType.SaveState, "Save State", "Once a run, a hit that would finish you does not.", ItemSlot.Back, false, 30, new Color(0.45f, 0.45f, 0.5f), look));

            // Kite String: a loop of it round the middle, trailing.
            look = new GameObject("KiteString");
            var twine = ArenaSceneBuilder.Lit("ItemTwine", new Color(0.85f, 0.78f, 0.6f), 0.1f);
            Decor(PrimitiveType.Cylinder, "Loop", look.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.75f, 0.03f, 0.75f), twine);
            Decor(PrimitiveType.Cube, "Trail", look.transform, new Vector3(0f, 0.7f, -0.6f), new Vector3(0.03f, 0.03f, 0.6f), twine);
            items.Add(Item(ItemType.KiteString, "Kite String", "Your dodge goes much further.", ItemSlot.Body, false, 20, new Color(0.85f, 0.78f, 0.6f), look));

            const string libraryPath = "Assets/Data/Items/ItemLibrary.asset";
            var library = AssetDatabase.LoadAssetAtPath<ItemLibrary>(libraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<ItemLibrary>();
                AssetDatabase.CreateAsset(library, libraryPath);
            }
            library.items = items.ToArray();
            EditorUtility.SetDirty(library);
            return library;
        }

        static ItemDefinition Item(ItemType type, string name, string description, ItemSlot slot, bool rare, int price, Color color, GameObject look)
        {
            string path = "Assets/Data/Items/" + type + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.type = type;
            item.displayName = name;
            item.description = description;
            item.flavour = Flavour(type);
            item.slot = slot;
            item.rare = rare;
            item.price = price;
            item.material = ArenaSceneBuilder.Unlit("Item" + type, color);
            item.visual = look != null ? ArenaSceneBuilder.SavePrefab(look, "Items/" + look.name) : null;
            EditorUtility.SetDirty(item);
            return item;
        }

        // One dry line per item. Same voice throughout: deadpan, never random.
        static string Flavour(ItemType type)
        {
            switch (type)
            {
                case ItemType.NeedleTip: return "It was a splinter. Now it's a career.";
                case ItemType.SplitterMandible: return "Previously owned. The owner objected.";
                case ItemType.AcidGland: return "Do not lick. This is your only warning and it is late.";
                case ItemType.FireflyLantern: return "The firefly is fine. Probably.";
                case ItemType.CompoundEye: return "You can now see everything. You will wish you couldn't.";
                case ItemType.ExtraLegs: return "Four more reasons to leave quickly.";
                case ItemType.WingBuds: return "Not flying. Falling with ambition.";
                case ItemType.ChitinPlate: return "Somebody's back. Yours now.";
                case ItemType.SugarCube: return "A week of food, if you were still a normal size.";
                case ItemType.PheromoneCloud: return "You smell like an argument.";
                case ItemType.LarvaBuddy: return "It thinks you're its mother. Don't correct it.";
                case ItemType.QueensJelly: return "Tastes like being in charge.";
                case ItemType.StaticSock: return "The other one is still missing. It always will be.";
                case ItemType.BirthdayCandle: return "Someone made a wish on this. You're using it as arson.";
                case ItemType.LostMarble: return "You have, technically, found your marbles.";
                case ItemType.Sprinkles: return "Hundreds and thousands. You took eight.";
                case ItemType.FinchsDose: return "Not approved for interns. Nothing was.";
                case ItemType.RubberBand: return "Holds nothing together. Sends everything back.";
                case ItemType.IceCube: return "It should have melted by now. It knows.";
                case ItemType.FridgeMagnet: return "Says 'World's Best Intern'. Somebody had it made as a joke.";
                case ItemType.SpareBattery: return "Found down the back of something. Still has a bit in it.";
                case ItemType.Lunchbox: return "Packed by somebody who assumed you would be back for tea.";
                case ItemType.PaperHat: return "Makes you look like you mean it. That costs something.";
                case ItemType.PiggyBank: return "It was full when you found it. It is yours now, which is different.";
                case ItemType.ColdCoffee: return "Somebody made this on Tuesday. It still works.";
                case ItemType.RainBoot: return "There is no puddle so small it cannot be jumped in.";
                case ItemType.Umbrella: return "Borrowed from the stand by the door. It will not be going back.";
                case ItemType.SaveState: return "Nobody remembers saving. Somebody did.";
                case ItemType.KiteString: return "Still attached to something, somewhere, pulling.";
                default: return "";
            }
        }

        // ---------- ants ----------

        static T Ant<T>(string name, float size, int health, Material body, Material dark, bool flying, out Transform model) where T : Enemy
        {
            var root = new GameObject(name);

            var controller = root.AddComponent<CharacterController>();
            controller.radius = 0.55f * size;
            controller.height = 1.2f * size;
            controller.center = new Vector3(0f, 0.6f * size, 0f);

            var hp = root.AddComponent<Health>();
            hp.team = Team.Enemy;
            hp.maxHealth = health;

            // "Visual" is resized by code (telegraphs, death pop); "Model" inside it holds the ant's own size.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            model = new GameObject("Model").transform;
            model.SetParent(visual, false);
            model.localScale = Vector3.one * size;

            Decor(PrimitiveType.Sphere, "Abdomen", model, new Vector3(0f, 0.6f, -0.65f), new Vector3(0.85f, 0.75f, 1.05f), body);
            Decor(PrimitiveType.Sphere, "Thorax", model, new Vector3(0f, 0.55f, 0f), new Vector3(0.55f, 0.5f, 0.65f), dark);
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 0.65f, 0.5f), new Vector3(0.6f, 0.5f, 0.55f), body);
            Decor(PrimitiveType.Cube, "Mandible L", model, new Vector3(-0.14f, 0.55f, 0.85f), new Vector3(0.08f, 0.08f, 0.3f), dark).transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            Decor(PrimitiveType.Cube, "Mandible R", model, new Vector3(0.14f, 0.55f, 0.85f), new Vector3(0.08f, 0.08f, 0.3f), dark).transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
            var legs = new Transform[6];
            for (int i = 0; i < 3; i++)
            {
                float z = 0.25f - i * 0.25f; // front to back
                float droop = flying ? 60f : 35f; // legs hang down in flight
                legs[i] = Decor(PrimitiveType.Cube, "Leg L" + i, model, new Vector3(-0.5f, 0.3f, z), new Vector3(0.7f, 0.07f, 0.07f), dark).transform;
                legs[i].localRotation = Quaternion.Euler(0f, 0f, droop);
                legs[3 + i] = Decor(PrimitiveType.Cube, "Leg R" + i, model, new Vector3(0.5f, 0.3f, z), new Vector3(0.7f, 0.07f, 0.07f), dark).transform;
                legs[3 + i].localRotation = Quaternion.Euler(0f, 0f, -droop);
            }

            var enemy = root.AddComponent<T>();
            enemy.visual = visual;
            var motion = root.AddComponent<AntMotion>();
            motion.legs = legs;
            motion.stride = 7f / size; // big ants take long, slow steps
            if (flying) motion.legSwing = 6f;
            root.AddComponent<HitFlash>();
            var mutations = root.AddComponent<EnemyMutations>();
            mutations.badgeHeight = 1.3f * size + 0.9f;
            mutations.shellOffset = 1.1f * size;
            mutations.bodySize = size;
            return enemy;
        }

        static Enemy SaveEnemy(Enemy enemy) =>
            ArenaSceneBuilder.SavePrefab(enemy.gameObject, "Enemies/" + enemy.name).GetComponent<Enemy>();

        // ---------- mutations ----------

        static MutationDefinition Mutation(MutationType type, string name, int cost, string effect, Color color)
        {
            string path = "Assets/Data/Mutations/" + type + ".asset";
            var mutation = AssetDatabase.LoadAssetAtPath<MutationDefinition>(path);
            if (mutation == null)
            {
                mutation = ScriptableObject.CreateInstance<MutationDefinition>();
                AssetDatabase.CreateAsset(mutation, path);
            }
            mutation.type = type;
            mutation.displayName = name;
            mutation.cost = cost;
            mutation.effect = effect;
            mutation.color = color;
            mutation.effectMaterial = ArenaSceneBuilder.Unlit("Mutation" + type, color);
            EditorUtility.SetDirty(mutation);
            return mutation;
        }

        // ---------- special room props ----------

        // Each prop fits inside the 3×3 tiles (6 m square) that every room keeps clear around its centre.
        static void BuildProps(BiomeDefinition biome)
        {
            var wall = biome.wallMaterial;
            var root = ArenaSceneBuilder.Lit("Root", new Color(0.3f, 0.2f, 0.12f), 0.1f);
            var gold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));
            var pad = ArenaSceneBuilder.Unlit("StartPad", new Color(0.2f, 0.5f, 0.55f));
            var beetle = ArenaSceneBuilder.Lit("Beetle", new Color(0.1f, 0.25f, 0.2f), 0.7f);

            var startProp = new GameObject("StartPad");
            Decor(PrimitiveType.Cylinder, "Pad", startProp.transform, new Vector3(0f, 0.01f, 0f), new Vector3(4f, 0.01f, 4f), pad);
            biome.startProp = ArenaSceneBuilder.SavePrefab(startProp, "Props/StartPad");

            var exit = new GameObject("ExitHole");
            Decor(PrimitiveType.Cylinder, "Rim", exit.transform, new Vector3(0f, 0.01f, 0f), new Vector3(3.6f, 0.01f, 3.6f), gold);
            Decor(PrimitiveType.Cylinder, "Hole", exit.transform, new Vector3(0f, 0.02f, 0f), new Vector3(3f, 0.02f, 3f), black);
            biome.exitProp = ArenaSceneBuilder.SavePrefab(exit, "Props/ExitHole");

            var treasure = new GameObject("TreasurePedestal");
            Solid(PrimitiveType.Cylinder, "Pedestal", treasure.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.5f, 1.4f), wall);
            biome.treasureProp = ArenaSceneBuilder.SavePrefab(treasure, "Props/TreasurePedestal");

            var shop = new GameObject("ShopStall");
            Solid(PrimitiveType.Cube, "Counter", shop.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 1.2f, 1f), root);
            Solid(PrimitiveType.Sphere, "Beetle Merchant", shop.transform, new Vector3(0f, 1f, 1.7f), new Vector3(1.6f, 1.6f, 1.6f), beetle);
            biome.shopProp = ArenaSceneBuilder.SavePrefab(shop, "Props/ShopStall");

            // The portal room after the biome boss: one way home, one way deeper.
            var sample = ArenaSceneBuilder.Unlit("Sample", new Color(0.45f, 0.95f, 1f));
            var deep = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            biome.extractPortalProp = ArenaSceneBuilder.SavePrefab(Portal("ExtractPortal", sample, black, wall), "Props/ExtractPortal");
            biome.deeperPortalProp = ArenaSceneBuilder.SavePrefab(Portal("DeeperPortal", deep, black, wall), "Props/DeeperPortal");
        }

        /// <summary>A glowing ring on the floor between two posts.</summary>
        public static GameObject Portal(string name, Material glow, Material inner, Material post)
        {
            var portal = new GameObject(name);
            Decor(PrimitiveType.Cylinder, "Ring", portal.transform, new Vector3(0f, 0.02f, 0f), new Vector3(3.2f, 0.02f, 3.2f), glow);
            Decor(PrimitiveType.Cylinder, "Inside", portal.transform, new Vector3(0f, 0.03f, 0f), new Vector3(2.5f, 0.02f, 2.5f), inner);
            Decor(PrimitiveType.Cube, "Post L", portal.transform, new Vector3(-1.9f, 1.4f, 0f), new Vector3(0.35f, 2.8f, 0.35f), post);
            Decor(PrimitiveType.Cube, "Post R", portal.transform, new Vector3(1.9f, 1.4f, 0f), new Vector3(0.35f, 2.8f, 0.35f), post);
            Decor(PrimitiveType.Cube, "Lintel", portal.transform, new Vector3(0f, 2.9f, 0f), new Vector3(4.2f, 0.3f, 0.35f), glow);
            return portal;
        }

        /// <summary>The six demo upgrades, with the design doc's costs.</summary>
        public static UpgradeDefinition[] BuildUpgrades()
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Data");
            ArenaSceneBuilder.EnsureFolder("Assets/Data", "Upgrades");
            return new[]
            {
                Upgrade(UpgradeType.ThickerShadow, "Thicker Shadow", "+1 max health per level.", 50, 150, 400),
                Upgrade(UpgradeType.QuickReflexes, "Quick Reflexes", "Dodge recharges 15% faster per level.", 100, 300),
                Upgrade(UpgradeType.SamplePouch, "Sample Pouch", "Keep more samples on death: 25%, then 35%, then 45%.", 150, 450),
                Upgrade(UpgradeType.OverchargeCoil, "Overcharge Coil", "The hero power meter fills 20% faster per level.", 200, 500),
                Upgrade(UpgradeType.FieldKit, "Field Kit", "Start every run with one random common item.", 350),
                Upgrade(UpgradeType.LuckyFind, "Lucky Find", "Treasure rooms offer a choice of 2 items.", 600),
                Upgrade(UpgradeType.PocketMoney, "Pocket Money", "Start every run with 15 coins per level.", 120, 360),
                Upgrade(UpgradeType.RegularCustomer, "Regular Customer", "The shop's bell is free the first time on every floor.", 300),
            };
        }

        static UpgradeDefinition Upgrade(UpgradeType type, string name, string description, params int[] costs)
        {
            string path = "Assets/Data/Upgrades/" + type + ".asset";
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
                AssetDatabase.CreateAsset(upgrade, path);
            }
            upgrade.type = type;
            upgrade.displayName = name;
            upgrade.description = description;
            upgrade.costs = costs;
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        static GameObject Solid(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, true);

        static GameObject Decor(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            ArenaSceneBuilder.Primitive(type, name, parent, position, scale, material, false);
    }
}
