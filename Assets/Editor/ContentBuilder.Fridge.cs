using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The fourth biome: The Fridge. Bright, white and cold, the opposite of under the sofa. Six enemies on the six
    /// behaviours, and two bosses with fights of their own: the Leftovers, which can only be hurt while its lid is
    /// up, and the Ice Maker. Its portal opens once the Robot Vacuum has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string FridgePath = "Assets/Data/TheFridge.asset";

        public static BiomeDefinition BuildFridge(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var white = ArenaSceneBuilder.Lit("Sugar", new Color(0.95f, 0.95f, 0.92f), 0.6f);
            var berry = ArenaSceneBuilder.Lit("MouldyBerry", new Color(0.35f, 0.25f, 0.6f), 0.5f);
            var mould = ArenaSceneBuilder.Lit("Mould", new Color(0.55f, 0.75f, 0.45f), 0.02f);
            var pea = ArenaSceneBuilder.Lit("FrozenPea", new Color(0.35f, 0.75f, 0.3f), 0.7f);
            var frostCap = ArenaSceneBuilder.Lit("FrostCap", new Color(0.88f, 0.95f, 1f), 0.3f);
            var ketchup = ArenaSceneBuilder.Lit("KetchupRed", new Color(0.8f, 0.1f, 0.08f), 0.6f);
            var ketchupShot = ArenaSceneBuilder.Unlit("KetchupShot", new Color(0.95f, 0.15f, 0.1f));
            var flyBody = ArenaSceneBuilder.Lit("FruitFlyBody", new Color(0.4f, 0.3f, 0.15f), 0.3f);
            var flyEye = ArenaSceneBuilder.Unlit("FruitFlyEye", new Color(0.9f, 0.15f, 0.1f));
            var wing = ArenaSceneBuilder.Lit("Wing", new Color(0.85f, 0.9f, 0.95f), 0.8f);
            var foil = ArenaSceneBuilder.Lit("Foil", new Color(0.8f, 0.82f, 0.86f), 0.95f);
            var yogurt = ArenaSceneBuilder.Unlit("Yogurt", new Color(1f, 0.7f, 0.8f));
            var sodaBox = ArenaSceneBuilder.Lit("BakingSodaBox", new Color(0.95f, 0.6f, 0.15f), 0.3f);
            var tub = ArenaSceneBuilder.Lit("TubPlastic", new Color(0.75f, 0.85f, 0.9f), 0.85f);
            var tubLid = ArenaSceneBuilder.Lit("TubLid", new Color(0.2f, 0.45f, 0.8f), 0.6f);
            var contents = ArenaSceneBuilder.Lit("LeftoverContents", new Color(0.45f, 0.5f, 0.25f), 0.1f);
            var machine = ArenaSceneBuilder.Lit("IceMakerBody", new Color(0.86f, 0.88f, 0.9f), 0.85f);
            var machineDark = ArenaSceneBuilder.Lit("IceMakerSlot", new Color(0.1f, 0.12f, 0.15f), 0.5f);
            var ice = ArenaSceneBuilder.Lit("IceCube", new Color(0.7f, 0.88f, 1f), 0.95f);
            var iceGlow = ArenaSceneBuilder.Unlit("IceGlow", new Color(0.55f, 0.85f, 1f));
            var frost = ArenaSceneBuilder.Unlit("FrostPatch", new Color(0.8f, 0.93f, 1f));
            var shelf = ArenaSceneBuilder.Lit("FridgeShelf", new Color(0.5f, 0.6f, 0.68f), 0.9f);
            var liner = ArenaSceneBuilder.Lit("FridgeLiner", new Color(0.68f, 0.73f, 0.78f), 0.4f);
            var seal = ArenaSceneBuilder.Lit("FridgeSeal", new Color(0.45f, 0.5f, 0.55f), 0.3f);
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            // ----- shots -----
            var squirt = new GameObject("KetchupSquirt");
            Decor(PrimitiveType.Sphere, "Visual", squirt.transform, Vector3.zero, new Vector3(0.3f, 0.3f, 0.45f), ketchupShot);
            squirt.AddComponent<Bullet>().radius = 0.22f;
            var squirtPrefab = ArenaSceneBuilder.SavePrefab(squirt, "Enemies/KetchupSquirt").GetComponent<Bullet>();

            var sporeShot = new GameObject("MouldShot");
            Decor(PrimitiveType.Sphere, "Visual", sporeShot.transform, Vector3.zero, Vector3.one * 0.5f, mould);
            sporeShot.AddComponent<Bullet>().radius = 0.25f;
            var sporeShotPrefab = ArenaSceneBuilder.SavePrefab(sporeShot, "Enemies/MouldShot").GetComponent<Bullet>();

            // ----- the six -----
            // Mouldy berry: the Chaser. It used to be a blueberry.
            var mouldy = Creature<ChaserEnemy>("MouldyBerry", 0.5f, 1.2f, 5, 1f, 2.2f, out var mouldyModel);
            Decor(PrimitiveType.Sphere, "Berry", mouldyModel, new Vector3(0f, 0.5f, 0f), Vector3.one * 0.95f, berry);
            Vector3[] fuzz = { new Vector3(0.25f, 0.85f, 0.1f), new Vector3(-0.3f, 0.7f, -0.15f), new Vector3(0.05f, 0.95f, -0.2f), new Vector3(-0.1f, 0.4f, -0.42f) };
            for (int i = 0; i < fuzz.Length; i++) Decor(PrimitiveType.Sphere, "Fuzz " + i, mouldyModel, fuzz[i], Vector3.one * (0.32f - 0.03f * i), mould);
            Decor(PrimitiveType.Sphere, "Eye L", mouldyModel, new Vector3(-0.17f, 0.6f, 0.42f), Vector3.one * 0.11f, white);
            Decor(PrimitiveType.Sphere, "Eye R", mouldyModel, new Vector3(0.17f, 0.6f, 0.42f), Vector3.one * 0.11f, white);
            mouldy.favouredDrops = new[] { ItemType.Lunchbox, ItemType.SugarCube };
            var mouldyPrefab = SaveEnemy(mouldy);

            // Frozen pea: the Charger. Small, hard, and thrown by nobody in particular.
            var frozenPea = Creature<ChargerEnemy>("FrozenPea", 0.8f, 1.6f, 10, 1.5f, 2.5f, out var peaModel);
            Decor(PrimitiveType.Sphere, "Pea", peaModel, new Vector3(0f, 0.8f, 0f), Vector3.one * 1.6f, pea);
            Decor(PrimitiveType.Sphere, "Frost", peaModel, new Vector3(0f, 1.25f, -0.1f), new Vector3(1.25f, 0.7f, 1.25f), frostCap);
            Decor(PrimitiveType.Sphere, "Eye L", peaModel, new Vector3(-0.28f, 0.85f, 0.72f), Vector3.one * 0.2f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", peaModel, new Vector3(0.28f, 0.85f, 0.72f), Vector3.one * 0.2f, dark);
            frozenPea.knockbackOnHit = 2f;
            frozenPea.favouredDrops = new[] { ItemType.IceCube, ItemType.ChitinPlate };
            frozenPea.telegraphMaterial = telegraph;
            var peaPrefab = SaveEnemy(frozenPea);

            // Ketchup packet: the Shooter. Somebody saved it from a takeaway in case.
            var packet = Creature<ShooterEnemy>("KetchupPacket", 0.45f, 1.5f, 4, 0.9f, 2.3f, out var packetModel);
            Decor(PrimitiveType.Cube, "Packet", packetModel, new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 1.1f, 0.2f), ketchup);
            Decor(PrimitiveType.Cube, "Label", packetModel, new Vector3(0f, 0.55f, 0.11f), new Vector3(0.5f, 0.35f, 0.02f), white);
            Decor(PrimitiveType.Cube, "Crimp", packetModel, new Vector3(0f, 1.34f, 0f), new Vector3(0.85f, 0.1f, 0.06f), foil);
            Decor(PrimitiveType.Sphere, "Eye L", packetModel, new Vector3(-0.17f, 1f, 0.11f), Vector3.one * 0.13f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", packetModel, new Vector3(0.17f, 1f, 0.11f), Vector3.one * 0.13f, dark);
            var packetMuzzle = new GameObject("Muzzle").transform;
            packetMuzzle.SetParent(packet.transform, false);
            packetMuzzle.localPosition = new Vector3(0f, 1f, 0.7f);
            packet.muzzle = packetMuzzle;
            packet.projectilePrefab = squirtPrefab;
            packet.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.NeedleTip };
            var packetPrefab = SaveEnemy(packet);

            // Fruit fly: the Flyer. It got in when the door was open and has regretted nothing.
            var fruitFly = Creature<FlyerEnemy>("FruitFly", 0.45f, 2f, 3, 0.8f, 2.9f, out var fruitFlyModel);
            Decor(PrimitiveType.Sphere, "Body", fruitFlyModel, new Vector3(0f, 0.6f, -0.1f), new Vector3(0.4f, 0.4f, 0.7f), flyBody);
            Decor(PrimitiveType.Sphere, "Eye L", fruitFlyModel, new Vector3(-0.16f, 0.68f, 0.3f), Vector3.one * 0.26f, flyEye);
            Decor(PrimitiveType.Sphere, "Eye R", fruitFlyModel, new Vector3(0.16f, 0.68f, 0.3f), Vector3.one * 0.26f, flyEye);
            var fruitWingLeft = Decor(PrimitiveType.Cube, "Wing L", fruitFlyModel, new Vector3(-0.5f, 0.88f, -0.1f), new Vector3(0.85f, 0.03f, 0.4f), wing).transform;
            fruitWingLeft.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var fruitWingRight = Decor(PrimitiveType.Cube, "Wing R", fruitFlyModel, new Vector3(0.5f, 0.88f, -0.1f), new Vector3(0.85f, 0.03f, 0.4f), wing).transform;
            fruitWingRight.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var fruitMotion = fruitFly.gameObject.AddComponent<AntMotion>();
            fruitMotion.legs = new Transform[0];
            fruitMotion.wings = new[] { fruitWingLeft, fruitWingRight };
            fruitFly.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            fruitFly.favouredDrops = new[] { ItemType.WingBuds, ItemType.Sprinkles };
            var fruitFlyPrefab = SaveEnemy(fruitFly);

            // Yogurt pot: the Lobber. Best before a date nobody can read any more.
            var pot = Creature<LobberEnemy>("YogurtPot", 0.5f, 1.3f, 6, 1f, 2.2f, out var potModel);
            Decor(PrimitiveType.Cylinder, "Pot", potModel, new Vector3(0f, 0.5f, 0f), new Vector3(0.85f, 0.5f, 0.85f), white);
            Decor(PrimitiveType.Cylinder, "Foil", potModel, new Vector3(0f, 1.02f, 0f), new Vector3(0.95f, 0.03f, 0.95f), foil);
            Decor(PrimitiveType.Sphere, "Dollop", potModel, new Vector3(0.1f, 1.12f, 0.05f), new Vector3(0.45f, 0.3f, 0.45f), yogurt);
            Decor(PrimitiveType.Sphere, "Eye L", potModel, new Vector3(-0.17f, 0.65f, 0.42f), Vector3.one * 0.12f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", potModel, new Vector3(0.17f, 0.65f, 0.42f), Vector3.one * 0.12f, dark);
            pot.blastMaterial = telegraph;
            pot.lobMaterial = yogurt;
            pot.favouredDrops = new[] { ItemType.AcidGland, ItemType.BirthdayCandle };
            var potPrefab = SaveEnemy(pot);

            // Baking soda: the Support. It is in here to keep things fresh, and it does.
            var soda = Creature<SupportEnemy>("BakingSoda", 0.5f, 1.5f, 4, 1f, 2.4f, out var sodaModel);
            Decor(PrimitiveType.Cube, "Box", sodaModel, new Vector3(0f, 0.7f, 0f), new Vector3(0.75f, 1.3f, 0.5f), sodaBox);
            Decor(PrimitiveType.Cube, "Label", sodaModel, new Vector3(0f, 0.55f, 0.26f), new Vector3(0.55f, 0.45f, 0.02f), white);
            Decor(PrimitiveType.Cube, "Flap", sodaModel, new Vector3(0f, 1.38f, -0.12f), new Vector3(0.75f, 0.04f, 0.4f), sodaBox).transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", sodaModel, new Vector3(-0.17f, 1.05f, 0.26f), Vector3.one * 0.12f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", sodaModel, new Vector3(0.17f, 1.05f, 0.26f), Vector3.one * 0.12f, dark);
            soda.pulseColor = new Color(0.8f, 0.93f, 1f);
            soda.favouredDrops = new[] { ItemType.FridgeMagnet, ItemType.ExtraLegs };
            var sodaPrefab = SaveEnemy(soda);

            // Spore: grows into a mouldy berry if it is left. (What the egg-box pods hatch.)
            var spore = Creature<LarvaEnemy>("Spore", 0.4f, 0.8f, 2, 0.6f, 1.4f, out var sporeModel);
            Decor(PrimitiveType.Sphere, "Fuzz", sporeModel, new Vector3(0f, 0.28f, 0f), new Vector3(0.6f, 0.5f, 0.6f), mould);
            spore.growsInto = mouldyPrefab;
            spore.dropsLoot = false;
            spore.knockbackOnHit = 1f;
            Object.DestroyImmediate(spore.GetComponent<EnemyMutations>());
            var sporePrefab = (LarvaEnemy)SaveEnemy(spore);

            // ----- bosses -----
            // The Leftovers (floor 5): a tub with a lid. The lid is the whole fight.
            var leftovers = Creature<Leftovers>("Leftovers", 1.5f, 2.6f, 100, 2.6f, 3.6f, out var leftoversModel);
            Decor(PrimitiveType.Cube, "Tub", leftoversModel, new Vector3(0f, 0.8f, 0f), new Vector3(2.8f, 1.6f, 2.2f), tub);
            Decor(PrimitiveType.Sphere, "Contents", leftoversModel, new Vector3(0f, 1.55f, 0f), new Vector3(2.3f, 0.9f, 1.8f), contents);
            Decor(PrimitiveType.Sphere, "Growth A", leftoversModel, new Vector3(0.6f, 1.95f, 0.2f), Vector3.one * 0.55f, mould);
            Decor(PrimitiveType.Sphere, "Growth B", leftoversModel, new Vector3(-0.5f, 1.9f, -0.3f), Vector3.one * 0.45f, mould);
            Decor(PrimitiveType.Sphere, "Eye L", leftoversModel, new Vector3(-0.4f, 1.85f, 0.7f), Vector3.one * 0.24f, white);
            Decor(PrimitiveType.Sphere, "Eye R", leftoversModel, new Vector3(0.4f, 1.85f, 0.7f), Vector3.one * 0.24f, white);
            var leftoversLid = new GameObject("Lid").transform;
            leftoversLid.SetParent(leftoversModel, false);
            leftoversLid.localPosition = new Vector3(0f, 1.9f, 0f);
            Decor(PrimitiveType.Cube, "Top", leftoversLid, new Vector3(0f, 0.35f, 0f), new Vector3(3f, 0.9f, 2.4f), tubLid);
            Decor(PrimitiveType.Cube, "Tab", leftoversLid, new Vector3(0f, 0.1f, 1.3f), new Vector3(0.8f, 0.12f, 0.3f), tubLid);
            leftovers.lid = leftoversLid;
            leftovers.displayName = "The Leftovers";
            leftovers.subtitle = "In here since March. Nobody will say which March.";
            leftovers.projectilePrefab = sporeShotPrefab;
            leftovers.sporePrefab = mouldyPrefab;
            leftovers.telegraphMaterial = telegraph;
            leftovers.sampleReward = 12;
            leftovers.knockbackOnHit = 0f;
            var leftoversPrefab = (BossEnemy)SaveEnemy(leftovers);

            // The Ice Maker (floor 10): a white box that makes ice, at you.
            var maker = Creature<IceMaker>("IceMaker", 1.9f, 4f, 300, 3.4f, 5.4f, out var makerModel);
            Decor(PrimitiveType.Cube, "Body", makerModel, new Vector3(0f, 2.1f, 0f), new Vector3(3.2f, 4.2f, 2.8f), machine);
            Decor(PrimitiveType.Cube, "Slot", makerModel, new Vector3(0f, 1.5f, 1.42f), new Vector3(1.8f, 1.3f, 0.1f), machineDark);
            Decor(PrimitiveType.Cube, "Light", makerModel, new Vector3(0f, 3.3f, 1.42f), new Vector3(1.4f, 0.25f, 0.06f), iceGlow);
            Decor(PrimitiveType.Sphere, "Eye L", makerModel, new Vector3(-0.55f, 2.75f, 1.42f), Vector3.one * 0.34f, machineDark);
            Decor(PrimitiveType.Sphere, "Eye R", makerModel, new Vector3(0.55f, 2.75f, 1.42f), Vector3.one * 0.34f, machineDark);
            Decor(PrimitiveType.Cube, "Tray", makerModel, new Vector3(0f, 0.35f, 1.9f), new Vector3(2.2f, 0.2f, 1f), machine);
            for (int i = 0; i < 4; i++)
            {
                Decor(PrimitiveType.Cube, "Cube " + i, makerModel, new Vector3(-0.75f + i * 0.5f, 0.65f, 1.9f + (i % 2) * 0.2f), Vector3.one * 0.4f, ice).transform.localRotation = Quaternion.Euler(i * 20f, i * 35f, 0f);
            }
            maker.displayName = "The Ice Maker";
            maker.subtitle = "It has one job. It is doing it at you.";
            maker.telegraphMaterial = telegraph;
            maker.frostMaterial = frost;
            maker.cubeMaterial = ice;
            maker.friends = new[] { peaPrefab };
            maker.sampleReward = 30;
            maker.knockbackOnHit = 0f;
            var makerPrefab = (BossEnemy)SaveEnemy(maker);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(FridgePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, FridgePath);
            }

            biome.displayName = "The Fridge";
            biome.id = "fridge";
            biome.mutationTheme = "static, armour, things past their date";
            biome.isBottom = false;
            biome.floorMaterial = shelf;
            biome.wallMaterial = liner;
            biome.doorMaterial = seal;
            biome.wallHeight = 6f;
            // The light is on because you are in it. White, bright and cold, with a blue haze at a distance.
            biome.ceiling = true;
            biome.ceilingMaterial = liner;
            biome.skyColor = new Color(0.6f, 0.72f, 0.82f);
            biome.ambientColor = new Color(0.72f, 0.8f, 0.9f);
            biome.sunIntensity = 1f;
            biome.fog = true;
            biome.fogColor = new Color(0.6f, 0.72f, 0.82f);
            biome.fogStart = 26f;
            biome.fogEnd = 95f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = ice;                 // ice cubes somebody dropped
            biome.crumbMaterial = ArenaSceneBuilder.Lit("Cheese", new Color(0.95f, 0.8f, 0.3f), 0.3f);
            biome.sugarMaterial = ArenaSceneBuilder.Lit("Butter", new Color(0.98f, 0.92f, 0.55f), 0.4f);
            biome.rootMaterial = ArenaSceneBuilder.Lit("Celery", new Color(0.6f, 0.8f, 0.4f), 0.3f);
            biome.podMaterial = white;                 // eggs
            biome.podHatchling = sporePrefab;
            biome.dewMaterial = ArenaSceneBuilder.Lit("Condensation", new Color(0.6f, 0.82f, 1f), 0.95f);
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = iceGlow;
            biome.glowStemMaterial = machine;
            biome.glowLightRange = 11f;
            biome.glowLightIntensity = 2f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = mouldyPrefab;
            biome.miniBoss = leftoversPrefab;
            biome.boss = makerPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = mouldyPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = packetPrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = peaPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = fruitFlyPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = potPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = sodaPrefab, weight = 1, minFloor = 6 },
                // The mimic is the ants' own prefab: coins are coins everywhere.
                new EnemySpawnEntry { prefab = ants.EnemyNamed("Mimic"), weight = 1, minFloor = 4 },
            };
            var mutations = new System.Collections.Generic.List<MutationDefinition>
            {
                ants.Mutation(MutationType.Fast), ants.Mutation(MutationType.Armored), ants.Mutation(MutationType.Regenerates),
                ants.Mutation(MutationType.ExplodesOnDeath), ants.Mutation(MutationType.SwarmCall), ants.Mutation(MutationType.HardenedShell),
            };
            foreach (MutationType borrowed in new[] { MutationType.Sticky, MutationType.Static })
            {
                var mutation = AssetDatabase.LoadAssetAtPath<MutationDefinition>("Assets/Data/Mutations/" + borrowed + ".asset");
                if (mutation != null) mutations.Add(mutation);
            }
            biome.mutations = mutations.ToArray();

            biome.startProp = ants.startProp;
            biome.exitProp = ants.exitProp;
            biome.treasureProp = ants.treasureProp;
            biome.shopProp = ants.shopProp;
            biome.extractPortalProp = ants.extractPortalProp;
            biome.deeperPortalProp = ants.deeperPortalProp;
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("FridgePortal", iceGlow, black, machine), "Props/FridgePortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
