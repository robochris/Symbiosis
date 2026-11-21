using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The sixth world: The Forest. Open sky, moss for a floor and bark for walls, lit by a low green sun. Seven
    /// enemies on the seven behaviours, the Froglet half way and the Toad at the end, whose fight is about hops you
    /// can see coming and a tongue you can step out of. Its portal opens once the Librarian has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string ForestPath = "Assets/Data/TheForest.asset";

        public static BiomeDefinition BuildForest(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var moss = ArenaSceneBuilder.Lit("ForestMoss", new Color(0.24f, 0.42f, 0.16f), 0.1f);
            var bark = ArenaSceneBuilder.Lit("ForestBark", new Color(0.3f, 0.2f, 0.13f), 0.15f);
            var leaf = ArenaSceneBuilder.Lit("ForestLeaf", new Color(0.36f, 0.6f, 0.2f), 0.25f);
            var autumn = ArenaSceneBuilder.Lit("ForestAutumn", new Color(0.78f, 0.42f, 0.12f), 0.2f);
            var louse = ArenaSceneBuilder.Lit("Woodlouse", new Color(0.42f, 0.44f, 0.48f), 0.55f);
            var stag = ArenaSceneBuilder.Lit("StagBeetle", new Color(0.24f, 0.1f, 0.06f), 0.8f);
            var capRed = ArenaSceneBuilder.Lit("ToadstoolRed", new Color(0.8f, 0.12f, 0.1f), 0.4f);
            var stalk = ArenaSceneBuilder.Lit("ToadstoolStalk", new Color(0.93f, 0.9f, 0.82f), 0.2f);
            var spore = ArenaSceneBuilder.Unlit("SporeCloud", new Color(0.85f, 1f, 0.45f));
            var mosquito = ArenaSceneBuilder.Lit("Mosquito", new Color(0.25f, 0.24f, 0.22f), 0.3f);
            var wingGlass = ArenaSceneBuilder.Lit("WingGlass", new Color(0.75f, 0.85f, 0.9f), 0.9f);
            var acorn = ArenaSceneBuilder.Lit("Acorn", new Color(0.55f, 0.36f, 0.16f), 0.5f);
            var acornCap = ArenaSceneBuilder.Lit("AcornCap", new Color(0.36f, 0.27f, 0.15f), 0.1f);
            var firefly = ArenaSceneBuilder.Unlit("FireflyGlow", new Color(1f, 0.95f, 0.4f));
            var hornet = ArenaSceneBuilder.Lit("Hornet", new Color(0.95f, 0.7f, 0.1f), 0.6f);
            var stripe = ArenaSceneBuilder.Lit("HornetStripe", new Color(0.08f, 0.07f, 0.05f), 0.4f);
            var sting = ArenaSceneBuilder.Unlit("Sting", new Color(1f, 0.9f, 0.55f));
            var caterpillar = ArenaSceneBuilder.Lit("Caterpillar", new Color(0.5f, 0.78f, 0.25f), 0.35f);
            var toadSkin = ArenaSceneBuilder.Lit("ToadSkin", new Color(0.42f, 0.4f, 0.18f), 0.45f);
            var toadBelly = ArenaSceneBuilder.Lit("ToadBelly", new Color(0.85f, 0.8f, 0.55f), 0.3f);
            var froglet = ArenaSceneBuilder.Lit("FrogletSkin", new Color(0.3f, 0.7f, 0.25f), 0.7f);
            var eyeGold = ArenaSceneBuilder.Unlit("ToadEye", new Color(1f, 0.75f, 0.15f));
            var tongue = ArenaSceneBuilder.Unlit("ToadTongue", new Color(1f, 0.45f, 0.55f));
            var sunbeam = ArenaSceneBuilder.Unlit("Glowcap", new Color(0.55f, 1f, 0.8f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var sporePuff = new GameObject("SporePuff");
            Decor(PrimitiveType.Sphere, "Visual", sporePuff.transform, Vector3.zero, Vector3.one * 0.38f, spore);
            sporePuff.AddComponent<Bullet>().radius = 0.22f;
            var sporePrefab = ArenaSceneBuilder.SavePrefab(sporePuff, "Enemies/SporePuff").GetComponent<Bullet>();

            var stinger = new GameObject("Stinger");
            Decor(PrimitiveType.Cube, "Visual", stinger.transform, Vector3.zero, new Vector3(0.12f, 0.12f, 0.6f), sting);
            stinger.AddComponent<Bullet>().radius = 0.18f;
            var stingerPrefab = ArenaSceneBuilder.SavePrefab(stinger, "Enemies/Stinger").GetComponent<Bullet>();

            // ----- the seven -----
            // Woodlouse: the Chaser. Fourteen legs, and every one of them in a hurry.
            var woodlouse = Creature<ChaserEnemy>("Woodlouse", 0.5f, 0.9f, 5, 1f, 1.7f, out var louseModel);
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Sphere, "Plate " + i, louseModel, new Vector3(0f, 0.35f, 0.45f - i * 0.3f), new Vector3(0.9f - i * 0.08f, 0.45f, 0.4f), louse);
            Decor(PrimitiveType.Sphere, "Eye L", louseModel, new Vector3(-0.18f, 0.42f, 0.65f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", louseModel, new Vector3(0.18f, 0.42f, 0.65f), Vector3.one * 0.1f, dark);
            woodlouse.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.ChitinPlate };
            var woodlousePrefab = SaveEnemy(woodlouse);

            // Stag beetle: the Charger. It has antlers, and it has had them out before.
            var beetle = Creature<ChargerEnemy>("StagBeetle", 0.75f, 1.2f, 10, 1.5f, 2.2f, out var beetleModel);
            Decor(PrimitiveType.Sphere, "Shell", beetleModel, new Vector3(0f, 0.55f, -0.2f), new Vector3(1.2f, 0.75f, 1.5f), stag);
            Decor(PrimitiveType.Sphere, "Head", beetleModel, new Vector3(0f, 0.55f, 0.6f), new Vector3(0.7f, 0.5f, 0.5f), stag);
            Decor(PrimitiveType.Cube, "Jaw L", beetleModel, new Vector3(-0.25f, 0.6f, 1.05f), new Vector3(0.1f, 0.12f, 0.7f), stag).transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            Decor(PrimitiveType.Cube, "Jaw R", beetleModel, new Vector3(0.25f, 0.6f, 1.05f), new Vector3(0.1f, 0.12f, 0.7f), stag).transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", beetleModel, new Vector3(-0.25f, 0.7f, 0.8f), Vector3.one * 0.12f, autumn);
            Decor(PrimitiveType.Sphere, "Eye R", beetleModel, new Vector3(0.25f, 0.7f, 0.8f), Vector3.one * 0.12f, autumn);
            beetle.knockbackOnHit = 2f;
            beetle.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.SplitterMandible };
            beetle.telegraphMaterial = telegraph;
            var beetlePrefab = SaveEnemy(beetle);

            // Toadstool: the Shooter. It puffs, and you would rather it did not.
            var toadstool = Creature<ShooterEnemy>("Toadstool", 0.45f, 1.4f, 4, 0.9f, 2.1f, out var stoolModel);
            Decor(PrimitiveType.Cylinder, "Stalk", stoolModel, new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.45f, 0.35f), stalk);
            Decor(PrimitiveType.Sphere, "Cap", stoolModel, new Vector3(0f, 1.05f, 0f), new Vector3(1.1f, 0.55f, 1.1f), capRed);
            for (int i = 0; i < 5; i++)
            {
                Vector3 dot = Quaternion.Euler(0f, i * 72f, 0f) * new Vector3(0.32f, 1.25f, 0f);
                Decor(PrimitiveType.Sphere, "Spot " + i, stoolModel, dot, Vector3.one * 0.14f, stalk);
            }
            Decor(PrimitiveType.Sphere, "Eye L", stoolModel, new Vector3(-0.1f, 0.65f, 0.17f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", stoolModel, new Vector3(0.1f, 0.65f, 0.17f), Vector3.one * 0.1f, dark);
            var stoolMuzzle = new GameObject("Muzzle").transform;
            stoolMuzzle.SetParent(toadstool.transform, false);
            stoolMuzzle.localPosition = new Vector3(0f, 1f, 0.6f);
            toadstool.muzzle = stoolMuzzle;
            toadstool.projectilePrefab = sporePrefab;
            toadstool.favouredDrops = new[] { ItemType.AcidGland, ItemType.PheromoneCloud };
            var toadstoolPrefab = SaveEnemy(toadstool);

            // Mosquito: the Flyer. It is only small to everybody else.
            var mozzie = Creature<FlyerEnemy>("Mosquito", 0.4f, 2f, 3, 0.8f, 2.9f, out var mozzieModel);
            Decor(PrimitiveType.Sphere, "Body", mozzieModel, new Vector3(0f, 0.7f, -0.2f), new Vector3(0.3f, 0.3f, 0.9f), mosquito);
            Decor(PrimitiveType.Sphere, "Head", mozzieModel, new Vector3(0f, 0.75f, 0.35f), Vector3.one * 0.26f, mosquito);
            Decor(PrimitiveType.Cube, "Nose", mozzieModel, new Vector3(0f, 0.7f, 0.75f), new Vector3(0.03f, 0.03f, 0.6f), dark);
            var mozzieLeft = Decor(PrimitiveType.Cube, "Wing L", mozzieModel, new Vector3(-0.4f, 0.85f, -0.1f), new Vector3(0.7f, 0.02f, 0.3f), wingGlass).transform;
            var mozzieRight = Decor(PrimitiveType.Cube, "Wing R", mozzieModel, new Vector3(0.4f, 0.85f, -0.1f), new Vector3(0.7f, 0.02f, 0.3f), wingGlass).transform;
            var mozzieMotion = mozzie.gameObject.AddComponent<AntMotion>();
            mozzieMotion.legs = new Transform[0];
            mozzieMotion.wings = new[] { mozzieLeft, mozzieRight };
            mozzie.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            mozzie.favouredDrops = new[] { ItemType.WingBuds, ItemType.NeedleTip };
            var mozziePrefab = SaveEnemy(mozzie);

            // Acorn weevil: the Lobber. It has a nose for acorns, and an arm for them too.
            var weevil = Creature<LobberEnemy>("AcornWeevil", 0.5f, 1.1f, 6, 1f, 2f, out var weevilModel);
            Decor(PrimitiveType.Sphere, "Body", weevilModel, new Vector3(0f, 0.45f, -0.1f), new Vector3(0.8f, 0.7f, 1f), acorn);
            Decor(PrimitiveType.Sphere, "Head", weevilModel, new Vector3(0f, 0.6f, 0.45f), Vector3.one * 0.4f, acornCap);
            Decor(PrimitiveType.Cube, "Snout", weevilModel, new Vector3(0f, 0.55f, 0.85f), new Vector3(0.08f, 0.08f, 0.6f), acornCap).transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", weevilModel, new Vector3(-0.15f, 0.72f, 0.58f), Vector3.one * 0.1f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", weevilModel, new Vector3(0.15f, 0.72f, 0.58f), Vector3.one * 0.1f, dark);
            weevil.blastMaterial = telegraph;
            weevil.lobMaterial = acorn;
            weevil.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.Lunchbox };
            var weevilPrefab = SaveEnemy(weevil);

            // Firefly: the Support. It lights the way for the others, and the way is toward you.
            var fly = Creature<SupportEnemy>("Firefly", 0.45f, 1.4f, 4, 1f, 2.3f, out var flyModel);
            Decor(PrimitiveType.Sphere, "Body", flyModel, new Vector3(0f, 0.8f, 0.1f), new Vector3(0.4f, 0.4f, 0.7f), stripe);
            Decor(PrimitiveType.Sphere, "Lantern", flyModel, new Vector3(0f, 0.75f, -0.4f), new Vector3(0.5f, 0.5f, 0.6f), firefly);
            Decor(PrimitiveType.Cube, "Wing L", flyModel, new Vector3(-0.3f, 1.05f, 0f), new Vector3(0.5f, 0.02f, 0.25f), wingGlass);
            Decor(PrimitiveType.Cube, "Wing R", flyModel, new Vector3(0.3f, 1.05f, 0f), new Vector3(0.5f, 0.02f, 0.25f), wingGlass);
            fly.pulseColor = new Color(1f, 0.95f, 0.4f);
            fly.favouredDrops = new[] { ItemType.SpareBattery, ItemType.LostMarble };
            var flyPrefab = SaveEnemy(fly);

            // Hornet: the Sniper. It lines you up from across the clearing and does not miss its line.
            var wasp = Creature<SniperEnemy>("Hornet", 0.45f, 1.4f, 5, 0.9f, 2.2f, out var waspModel);
            Decor(PrimitiveType.Sphere, "Thorax", waspModel, new Vector3(0f, 0.8f, 0.2f), new Vector3(0.45f, 0.45f, 0.5f), hornet);
            Decor(PrimitiveType.Sphere, "Abdomen", waspModel, new Vector3(0f, 0.7f, -0.35f), new Vector3(0.5f, 0.5f, 0.8f), hornet);
            Decor(PrimitiveType.Cube, "Stripe 1", waspModel, new Vector3(0f, 0.7f, -0.25f), new Vector3(0.52f, 0.52f, 0.1f), stripe);
            Decor(PrimitiveType.Cube, "Stripe 2", waspModel, new Vector3(0f, 0.68f, -0.5f), new Vector3(0.44f, 0.44f, 0.1f), stripe);
            Decor(PrimitiveType.Sphere, "Head", waspModel, new Vector3(0f, 0.85f, 0.55f), Vector3.one * 0.32f, hornet);
            Decor(PrimitiveType.Cube, "Wing L", waspModel, new Vector3(-0.35f, 1.1f, 0f), new Vector3(0.6f, 0.02f, 0.25f), wingGlass);
            Decor(PrimitiveType.Cube, "Wing R", waspModel, new Vector3(0.35f, 1.1f, 0f), new Vector3(0.6f, 0.02f, 0.25f), wingGlass);
            var waspMuzzle = new GameObject("Muzzle").transform;
            waspMuzzle.SetParent(wasp.transform, false);
            waspMuzzle.localPosition = new Vector3(0f, 0.8f, 0.9f);
            wasp.muzzle = waspMuzzle;
            wasp.projectilePrefab = stingerPrefab;
            wasp.telegraphMaterial = telegraph;
            wasp.favouredDrops = new[] { ItemType.NeedleTip, ItemType.CompoundEye };
            var waspPrefab = SaveEnemy(wasp);

            // Caterpillar: grows into a woodlouse if it is left. Biology is not this forest's strong point.
            var grub = Creature<LarvaEnemy>("Caterpillar", 0.4f, 0.7f, 2, 0.6f, 1.3f, out var grubModel);
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Sphere, "Segment " + i, grubModel, new Vector3(0f, 0.22f, 0.3f - i * 0.22f), Vector3.one * 0.32f, caterpillar);
            Decor(PrimitiveType.Sphere, "Eye L", grubModel, new Vector3(-0.08f, 0.3f, 0.45f), Vector3.one * 0.07f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", grubModel, new Vector3(0.08f, 0.3f, 0.45f), Vector3.one * 0.07f, dark);
            grub.growsInto = woodlousePrefab;
            grub.dropsLoot = false;
            grub.knockbackOnHit = 1f;
            Object.DestroyImmediate(grub.GetComponent<EnemyMutations>());
            var grubPrefab = (LarvaEnemy)SaveEnemy(grub);

            // ----- bosses: a froglet, and what it grows up to be -----
            var frog = Amphibian("TheFroglet", 0.7f, 1.2f, 110, 2.4f, 1.1f, froglet, toadBelly, eyeGold, dark);
            frog.displayName = "The Froglet";
            frog.subtitle = "Recently a tadpole. Has opinions about it.";
            frog.canTongue = false;
            frog.hopsPerRun = new[] { 1, 2, 2 };
            frog.hopRange = 7f;
            frog.landingRadius = 1.9f;
            frog.sitTime = 1.9f;
            frog.telegraphMaterial = telegraph;
            frog.tongueMaterial = tongue;
            frog.friends = new[] { woodlousePrefab };
            frog.sampleReward = 12;
            frog.knockbackOnHit = 1f;
            Object.DestroyImmediate(frog.GetComponent<EnemyMutations>());
            var frogPrefab = (BossEnemy)SaveEnemy(frog);

            var toad = Amphibian("TheToad", 1.2f, 2.2f, 380, 4f, 2f, toadSkin, toadBelly, eyeGold, dark);
            toad.displayName = "The Toad";
            toad.subtitle = "Sits very still. Eats whatever moves. You are moving.";
            // Big, and slow to come down: the circle is up for most of a second, and it sits a while between runs.
            toad.hopTime = 0.95f;
            toad.landingRadius = 2.1f;
            toad.sitTime = 1.9f;
            toad.telegraphMaterial = telegraph;
            toad.tongueMaterial = tongue;
            toad.friends = new[] { mozziePrefab, flyPrefab };
            toad.sampleReward = 30;
            toad.knockbackOnHit = 0.3f;
            Object.DestroyImmediate(toad.GetComponent<EnemyMutations>());
            var toadPrefab = (BossEnemy)SaveEnemy(toad);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(ForestPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, ForestPath);
            }

            biome.displayName = "The Forest";
            biome.id = "forest";
            biome.mutationTheme = "shells, spores, things that bite back";
            biome.isBottom = false;
            biome.floorMaterial = moss;
            biome.wallMaterial = bark;
            biome.doorMaterial = autumn;
            biome.wallHeight = 7f;
            // Out under the sky, for once: a low sun through the leaves, and a green haze in the distance.
            biome.ceiling = false;
            biome.ceilingMaterial = null;
            biome.skyColor = new Color(0.55f, 0.75f, 0.6f);
            biome.ambientColor = new Color(0.55f, 0.66f, 0.45f);
            biome.sunIntensity = 1.15f;
            biome.fog = true;
            biome.fogColor = new Color(0.45f, 0.6f, 0.45f);
            biome.fogStart = 28f;
            biome.fogEnd = 95f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = bark;                // fallen logs
            biome.crumbMaterial = autumn;              // dead leaves
            biome.sugarMaterial = leaf;
            biome.rootMaterial = bark;                 // roots, actual roots
            biome.podMaterial = toadBelly;             // eggs on a leaf
            biome.podHatchling = grubPrefab;
            biome.dewMaterial = wingGlass;             // dew, actual dew
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = sunbeam;              // glowcaps
            biome.glowStemMaterial = stalk;
            biome.glowLightRange = 10f;
            biome.glowLightIntensity = 1.8f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = woodlousePrefab;
            biome.miniBoss = frogPrefab;
            biome.boss = toadPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = woodlousePrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = toadstoolPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = beetlePrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = mozziePrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = weevilPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = flyPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = waspPrefab, weight = 1, minFloor = 5 },
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
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("ForestPortal", leaf, black, bark), "Props/ForestPortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }

        // A toad, more or less: a wide low body, a pale belly, two gold eyes on top and a long mouth.
        static ToadBoss Amphibian(string name, float radius, float height, int health, float badge, float size,
            Material skin, Material belly, Material eye, Material dark)
        {
            var toad = Creature<ToadBoss>(name, radius, height, health, 1f, badge, out var model);
            model.localScale = Vector3.one * size;
            Decor(PrimitiveType.Sphere, "Body", model, new Vector3(0f, 0.45f, 0f), new Vector3(1.3f, 0.8f, 1.2f), skin);
            Decor(PrimitiveType.Sphere, "Belly", model, new Vector3(0f, 0.35f, 0.25f), new Vector3(1.05f, 0.55f, 0.8f), belly);
            Decor(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 0.7f, 0.45f), new Vector3(1.05f, 0.5f, 0.6f), skin);
            Decor(PrimitiveType.Sphere, "Eye L", model, new Vector3(-0.3f, 0.95f, 0.5f), Vector3.one * 0.26f, eye);
            Decor(PrimitiveType.Sphere, "Eye R", model, new Vector3(0.3f, 0.95f, 0.5f), Vector3.one * 0.26f, eye);
            Decor(PrimitiveType.Sphere, "Pupil L", model, new Vector3(-0.3f, 0.98f, 0.62f), new Vector3(0.14f, 0.06f, 0.06f), dark);
            Decor(PrimitiveType.Sphere, "Pupil R", model, new Vector3(0.3f, 0.98f, 0.62f), new Vector3(0.14f, 0.06f, 0.06f), dark);
            Decor(PrimitiveType.Cube, "Mouth", model, new Vector3(0f, 0.58f, 0.74f), new Vector3(0.8f, 0.03f, 0.04f), dark);
            Decor(PrimitiveType.Sphere, "Leg L", model, new Vector3(-0.6f, 0.2f, -0.3f), new Vector3(0.45f, 0.35f, 0.7f), skin);
            Decor(PrimitiveType.Sphere, "Leg R", model, new Vector3(0.6f, 0.2f, -0.3f), new Vector3(0.45f, 0.35f, 0.7f), skin);
            Decor(PrimitiveType.Sphere, "Hand L", model, new Vector3(-0.45f, 0.1f, 0.55f), new Vector3(0.3f, 0.15f, 0.3f), skin);
            Decor(PrimitiveType.Sphere, "Hand R", model, new Vector3(0.45f, 0.1f, 0.55f), new Vector3(0.3f, 0.15f, 0.3f), skin);
            return toad;
        }
    }
}
