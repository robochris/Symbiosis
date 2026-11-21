using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The eighth world: Inside the Console. Circuit board for a floor, black casing for walls, a lid overhead, lit by
    /// the glow of its own lights. Seven enemies on the seven behaviours, the Cursor half way and the Glitch at the
    /// end, whose fight is marks to step out of: clicks, a blink, and rows sweeping across the floor with one gap in
    /// each. Its portal opens once the Thundercloud has been beaten.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string ConsolePath = "Assets/Data/TheConsole.asset";

        public static BiomeDefinition BuildConsole(BiomeDefinition ants)
        {
            var dark = ArenaSceneBuilder.Lit("CandyDark", new Color(0.15f, 0.08f, 0.06f), 0.5f);
            var board = ArenaSceneBuilder.Lit("CircuitBoard", new Color(0.06f, 0.28f, 0.16f), 0.5f);
            var casing = ArenaSceneBuilder.Lit("ConsoleCase", new Color(0.07f, 0.07f, 0.09f), 0.7f);
            var solder = ArenaSceneBuilder.Lit("Solder", new Color(0.75f, 0.76f, 0.8f), 0.9f);
            var copper = ArenaSceneBuilder.Lit("Trace", new Color(0.85f, 0.55f, 0.25f), 0.85f);
            var neonCyan = ArenaSceneBuilder.Unlit("Neon", new Color(0.3f, 1f, 0.95f));
            var neonPink = ArenaSceneBuilder.Unlit("NeonPink", new Color(1f, 0.3f, 0.8f));
            var neonGreen = ArenaSceneBuilder.Unlit("NeonGreen", new Color(0.4f, 1f, 0.4f));
            var capBlue = ArenaSceneBuilder.Lit("CapacitorBlue", new Color(0.15f, 0.3f, 0.75f), 0.6f);
            var bugRed = ArenaSceneBuilder.Lit("BugRed", new Color(0.85f, 0.15f, 0.15f), 0.5f);
            var fanGrey = ArenaSceneBuilder.Lit("FanGrey", new Color(0.35f, 0.36f, 0.4f), 0.6f);
            var cursorWhite = ArenaSceneBuilder.Lit("CursorWhite", new Color(0.97f, 0.97f, 1f), 0.4f);
            var glitchMagenta = ArenaSceneBuilder.Unlit("GlitchMagenta", new Color(1f, 0.1f, 0.9f));
            var glitchCyan = ArenaSceneBuilder.Unlit("GlitchCyan", new Color(0.1f, 1f, 1f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var pixel = new GameObject("PixelShot");
            Decor(PrimitiveType.Cube, "Visual", pixel.transform, Vector3.zero, Vector3.one * 0.3f, neonPink);
            pixel.AddComponent<Bullet>().radius = 0.2f;
            var pixelPrefab = ArenaSceneBuilder.SavePrefab(pixel, "Enemies/PixelShot").GetComponent<Bullet>();

            var beam = new GameObject("LaserShot");
            Decor(PrimitiveType.Cube, "Visual", beam.transform, Vector3.zero, new Vector3(0.08f, 0.08f, 0.9f), neonGreen);
            beam.AddComponent<Bullet>().radius = 0.16f;
            var beamPrefab = ArenaSceneBuilder.SavePrefab(beam, "Enemies/LaserShot").GetComponent<Bullet>();

            // ----- the seven -----
            // Electron: the Chaser. Negatively charged, and it takes it out on you.
            var electron = Creature<ChaserEnemy>("Electron", 0.45f, 0.9f, 5, 1f, 1.8f, out var electronModel);
            Decor(PrimitiveType.Sphere, "Core", electronModel, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.7f, neonCyan);
            Decor(PrimitiveType.Sphere, "Eye L", electronModel, new Vector3(-0.14f, 0.55f, 0.3f), Vector3.one * 0.12f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", electronModel, new Vector3(0.14f, 0.55f, 0.3f), Vector3.one * 0.12f, dark);
            electron.favouredDrops = new[] { ItemType.ExtraLegs, ItemType.StaticSock };
            var electronPrefab = SaveEnemy(electron);

            // Capacitor: the Charger. Stores it all up, then lets go at once.
            var cap = Creature<ChargerEnemy>("Capacitor", 0.7f, 1.6f, 10, 1.5f, 2.4f, out var capModel);
            Decor(PrimitiveType.Cylinder, "Can", capModel, new Vector3(0f, 0.8f, 0f), new Vector3(1.1f, 0.8f, 1.1f), capBlue);
            Decor(PrimitiveType.Cylinder, "Top", capModel, new Vector3(0f, 1.62f, 0f), new Vector3(1f, 0.02f, 1f), solder);
            Decor(PrimitiveType.Cube, "Stripe", capModel, new Vector3(0f, 0.8f, 0.55f), new Vector3(0.25f, 1.5f, 0.04f), cursorWhite);
            Decor(PrimitiveType.Sphere, "Eye L", capModel, new Vector3(-0.25f, 1.15f, 0.5f), Vector3.one * 0.18f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", capModel, new Vector3(0.25f, 1.15f, 0.5f), Vector3.one * 0.18f, dark);
            cap.knockbackOnHit = 1.5f;
            cap.telegraphMaterial = telegraph;
            cap.favouredDrops = new[] { ItemType.ChitinPlate, ItemType.SpareBattery };
            var capPrefab = SaveEnemy(cap);

            // Sprite: the Shooter. Eight pixels by eight, and it has been waiting since 1985.
            var sprite = Creature<ShooterEnemy>("Sprite", 0.5f, 1.4f, 4, 0.9f, 2.2f, out var spriteModel);
            Decor(PrimitiveType.Cube, "Body", spriteModel, new Vector3(0f, 0.8f, 0f), new Vector3(1f, 0.6f, 0.3f), neonGreen);
            Decor(PrimitiveType.Cube, "Head", spriteModel, new Vector3(0f, 1.2f, 0f), new Vector3(0.6f, 0.3f, 0.3f), neonGreen);
            Decor(PrimitiveType.Cube, "Leg L", spriteModel, new Vector3(-0.3f, 0.35f, 0f), new Vector3(0.2f, 0.3f, 0.3f), neonGreen);
            Decor(PrimitiveType.Cube, "Leg R", spriteModel, new Vector3(0.3f, 0.35f, 0f), new Vector3(0.2f, 0.3f, 0.3f), neonGreen);
            Decor(PrimitiveType.Cube, "Eye L", spriteModel, new Vector3(-0.2f, 0.9f, 0.16f), new Vector3(0.15f, 0.15f, 0.02f), dark);
            Decor(PrimitiveType.Cube, "Eye R", spriteModel, new Vector3(0.2f, 0.9f, 0.16f), new Vector3(0.15f, 0.15f, 0.02f), dark);
            var spriteMuzzle = new GameObject("Muzzle").transform;
            spriteMuzzle.SetParent(sprite.transform, false);
            spriteMuzzle.localPosition = new Vector3(0f, 0.9f, 0.5f);
            sprite.muzzle = spriteMuzzle;
            sprite.projectilePrefab = pixelPrefab;
            sprite.favouredDrops = new[] { ItemType.AcidGland, ItemType.CompoundEye };
            var spritePrefab = SaveEnemy(sprite);

            // Bug: the Flyer. Somebody's mistake, with wings.
            var bug = Creature<FlyerEnemy>("Bug", 0.45f, 2f, 3, 0.8f, 2.9f, out var bugModel);
            Decor(PrimitiveType.Sphere, "Shell", bugModel, new Vector3(0f, 0.7f, -0.05f), new Vector3(0.6f, 0.35f, 0.75f), bugRed);
            Decor(PrimitiveType.Sphere, "Head", bugModel, new Vector3(0f, 0.7f, 0.4f), Vector3.one * 0.3f, dark);
            var bugLeft = Decor(PrimitiveType.Cube, "Wing L", bugModel, new Vector3(-0.4f, 0.85f, 0f), new Vector3(0.6f, 0.02f, 0.35f), neonCyan).transform;
            var bugRight = Decor(PrimitiveType.Cube, "Wing R", bugModel, new Vector3(0.4f, 0.85f, 0f), new Vector3(0.6f, 0.02f, 0.35f), neonCyan).transform;
            var bugMotion = bug.gameObject.AddComponent<AntMotion>();
            bugMotion.legs = new Transform[0];
            bugMotion.wings = new[] { bugLeft, bugRight };
            bug.GetComponent<CharacterController>().center = new Vector3(0f, 1f, 0f);
            bug.favouredDrops = new[] { ItemType.WingBuds, ItemType.NeedleTip };
            var bugPrefab = SaveEnemy(bug);

            // Solder blob: the Lobber. Hot, silver, and very sticky about where it lands.
            var blob = Creature<LobberEnemy>("SolderBlob", 0.5f, 1.1f, 6, 1f, 2f, out var blobModel);
            Decor(PrimitiveType.Sphere, "Blob", blobModel, new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.8f, 1f), solder);
            Decor(PrimitiveType.Cylinder, "Iron", blobModel, new Vector3(0.3f, 1f, -0.2f), new Vector3(0.12f, 0.5f, 0.12f), copper).transform.localRotation = Quaternion.Euler(20f, 0f, 20f);
            Decor(PrimitiveType.Sphere, "Eye L", blobModel, new Vector3(-0.17f, 0.6f, 0.42f), Vector3.one * 0.13f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", blobModel, new Vector3(0.17f, 0.6f, 0.42f), Vector3.one * 0.13f, dark);
            blob.blastMaterial = telegraph;
            blob.lobMaterial = solder;
            blob.favouredDrops = new[] { ItemType.SplitterMandible, ItemType.BirthdayCandle };
            var blobPrefab = SaveEnemy(blob);

            // Cooling fan: the Support. It keeps everybody's temper down, and their speed up.
            var fan = Creature<SupportEnemy>("CoolingFan", 0.55f, 1.4f, 4, 1f, 2.2f, out var fanModel);
            Decor(PrimitiveType.Cube, "Frame", fanModel, new Vector3(0f, 0.8f, 0f), new Vector3(1.1f, 1.1f, 0.25f), fanGrey);
            for (int i = 0; i < 4; i++)
                Decor(PrimitiveType.Cube, "Blade " + i, fanModel, new Vector3(0f, 0.8f, 0.15f), new Vector3(0.15f, 0.9f, 0.04f), casing).transform.localRotation = Quaternion.Euler(0f, 0f, 45f * i);
            Decor(PrimitiveType.Sphere, "Hub", fanModel, new Vector3(0f, 0.8f, 0.18f), Vector3.one * 0.25f, neonCyan);
            fan.pulseColor = new Color(0.3f, 1f, 0.95f);
            fan.favouredDrops = new[] { ItemType.PheromoneCloud, ItemType.ColdCoffee };
            var fanPrefab = SaveEnemy(fan);

            // Laser diode: the Sniper. A thin red line first, then the rest of the light.
            var diode = Creature<SniperEnemy>("LaserDiode", 0.45f, 1.3f, 5, 0.9f, 2.2f, out var diodeModel);
            Decor(PrimitiveType.Cylinder, "Body", diodeModel, new Vector3(0f, 0.7f, 0f), new Vector3(0.45f, 0.45f, 0.45f), casing).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Lens", diodeModel, new Vector3(0f, 0.7f, 0.45f), Vector3.one * 0.3f, neonGreen);
            Decor(PrimitiveType.Cube, "Leg A", diodeModel, new Vector3(-0.1f, 0.25f, -0.1f), new Vector3(0.04f, 0.5f, 0.04f), solder);
            Decor(PrimitiveType.Cube, "Leg B", diodeModel, new Vector3(0.1f, 0.25f, -0.1f), new Vector3(0.04f, 0.5f, 0.04f), solder);
            var diodeMuzzle = new GameObject("Muzzle").transform;
            diodeMuzzle.SetParent(diode.transform, false);
            diodeMuzzle.localPosition = new Vector3(0f, 0.7f, 0.7f);
            diode.muzzle = diodeMuzzle;
            diode.projectilePrefab = beamPrefab;
            diode.telegraphMaterial = telegraph;
            diode.favouredDrops = new[] { ItemType.NeedleTip, ItemType.CompoundEye };
            var diodePrefab = SaveEnemy(diode);

            // Bit: grows into an electron if it is left. A one, or possibly a zero.
            var bit = Creature<LarvaEnemy>("Bit", 0.35f, 0.6f, 2, 0.6f, 1.2f, out var bitModel);
            Decor(PrimitiveType.Cube, "Body", bitModel, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.4f, neonCyan);
            Decor(PrimitiveType.Cube, "Eye L", bitModel, new Vector3(-0.08f, 0.3f, 0.21f), new Vector3(0.07f, 0.07f, 0.02f), dark);
            Decor(PrimitiveType.Cube, "Eye R", bitModel, new Vector3(0.08f, 0.3f, 0.21f), new Vector3(0.07f, 0.07f, 0.02f), dark);
            bit.growsInto = electronPrefab;
            bit.dropsLoot = false;
            bit.knockbackOnHit = 1f;
            Object.DestroyImmediate(bit.GetComponent<EnemyMutations>());
            var bitPrefab = (LarvaEnemy)SaveEnemy(bit);

            // ----- bosses: what points at things, and what goes wrong with them -----
            var cursor = Creature<GlitchBoss>("TheCursor", 0.8f, 2.4f, 110, 1f, 3.4f, out var cursorModel);
            Decor(PrimitiveType.Cube, "Arrow", cursorModel, new Vector3(0f, 1.4f, 0f), new Vector3(0.15f, 1.6f, 1.3f), cursorWhite).transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            Decor(PrimitiveType.Cube, "Edge", cursorModel, new Vector3(0f, 1.4f, 0.02f), new Vector3(0.18f, 1.7f, 0.08f), casing).transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            Decor(PrimitiveType.Cube, "Tail", cursorModel, new Vector3(0f, 0.45f, -0.45f), new Vector3(0.15f, 0.8f, 0.3f), cursorWhite).transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            Decor(PrimitiveType.Sphere, "Eye L", cursorModel, new Vector3(-0.1f, 1.7f, 0.3f), Vector3.one * 0.14f, dark);
            Decor(PrimitiveType.Sphere, "Eye R", cursorModel, new Vector3(0.1f, 1.7f, 0.3f), Vector3.one * 0.14f, dark);
            cursor.displayName = "The Cursor";
            cursor.subtitle = "It has been pointing at things for years. Now it would like to click on one.";
            cursor.canBlink = false;
            cursor.canScan = false;
            cursor.clicks = 3;
            cursor.telegraphMaterial = telegraph;
            cursor.friends = new[] { electronPrefab };
            cursor.sampleReward = 12;
            cursor.knockbackOnHit = 1f;
            Object.DestroyImmediate(cursor.GetComponent<EnemyMutations>());
            var cursorPrefab = (BossEnemy)SaveEnemy(cursor);

            var glitch = Creature<GlitchBoss>("TheGlitch", 1.2f, 3.2f, 380, 1f, 4.6f, out var glitchModel);
            // Three blocks out of line with each other, in colours that should not be there.
            Decor(PrimitiveType.Cube, "Block A", glitchModel, new Vector3(0f, 1.6f, 0f), new Vector3(1.8f, 1.8f, 1.8f), casing);
            Decor(PrimitiveType.Cube, "Block B", glitchModel, new Vector3(0.35f, 2.1f, 0.1f), new Vector3(1.4f, 0.5f, 1.9f), glitchMagenta);
            Decor(PrimitiveType.Cube, "Block C", glitchModel, new Vector3(-0.4f, 1.2f, -0.1f), new Vector3(1.5f, 0.35f, 1.85f), glitchCyan);
            Decor(PrimitiveType.Cube, "Eye L", glitchModel, new Vector3(-0.4f, 1.9f, 0.92f), new Vector3(0.35f, 0.2f, 0.04f), neonGreen);
            Decor(PrimitiveType.Cube, "Eye R", glitchModel, new Vector3(0.4f, 1.8f, 0.92f), new Vector3(0.35f, 0.2f, 0.04f), neonGreen);
            glitch.displayName = "The Glitch";
            glitch.subtitle = "Not supposed to be here. Neither are you. One of you is leaving.";
            glitch.telegraphMaterial = telegraph;
            glitch.friends = new[] { bugPrefab, fanPrefab };
            glitch.sampleReward = 30;
            glitch.knockbackOnHit = 0.3f;
            Object.DestroyImmediate(glitch.GetComponent<EnemyMutations>());
            var glitchPrefab = (BossEnemy)SaveEnemy(glitch);

            // ----- the biome -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(ConsolePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, ConsolePath);
            }

            biome.displayName = "Inside the Console";
            biome.id = "console";
            biome.mutationTheme = "static, shells, things that repeat";
            biome.isBottom = false;
            biome.floorMaterial = board;
            biome.wallMaterial = casing;
            biome.doorMaterial = neonPink;
            biome.wallHeight = 5f;
            // Shut in, dark, lit by its own lights.
            biome.ceiling = true;
            biome.ceilingMaterial = casing;
            biome.skyColor = new Color(0.02f, 0.02f, 0.04f);
            biome.ambientColor = new Color(0.35f, 0.45f, 0.5f);
            biome.sunIntensity = 0.55f;
            biome.fog = true;
            biome.fogColor = new Color(0.02f, 0.05f, 0.06f);
            biome.fogStart = 22f;
            biome.fogEnd = 80f;
            biome.roomCastsShadows = false;

            biome.coverMaterial = casing;              // chips
            biome.crumbMaterial = copper;              // offcuts of wire
            biome.sugarMaterial = neonGreen;
            biome.rootMaterial = copper;               // traces
            biome.podMaterial = capBlue;               // spare components
            biome.podHatchling = bitPrefab;
            biome.dewMaterial = solder;
            biome.eliteMaterial = ants.eliteMaterial;
            biome.glowMaterial = neonCyan;             // status lights
            biome.glowStemMaterial = casing;
            biome.glowLightRange = 11f;
            biome.glowLightIntensity = 2.4f;
            biome.baseEnemiesPerRoom = ants.baseEnemiesPerRoom;
            biome.floorsPerExtraEnemy = ants.floorsPerExtraEnemy;
            biome.tilesPerExtraEnemy = ants.tilesPerExtraEnemy;
            biome.maxExtraEnemiesForSize = ants.maxExtraEnemiesForSize;

            biome.swarmEnemy = electronPrefab;
            biome.miniBoss = cursorPrefab;
            biome.boss = glitchPrefab;
            biome.spawnTable = new[]
            {
                new EnemySpawnEntry { prefab = electronPrefab, weight = 5, minFloor = 1 },
                new EnemySpawnEntry { prefab = spritePrefab, weight = 2, minFloor = 2 },
                new EnemySpawnEntry { prefab = capPrefab, weight = 2, minFloor = 3 },
                new EnemySpawnEntry { prefab = bugPrefab, weight = 2, minFloor = 4 },
                new EnemySpawnEntry { prefab = blobPrefab, weight = 2, minFloor = 5 },
                new EnemySpawnEntry { prefab = fanPrefab, weight = 1, minFloor = 6 },
                new EnemySpawnEntry { prefab = diodePrefab, weight = 1, minFloor = 5 },
                new EnemySpawnEntry { prefab = ants.EnemyNamed("Mimic"), weight = 1, minFloor = 4 },
            };
            var mutations = new System.Collections.Generic.List<MutationDefinition>
            {
                ants.Mutation(MutationType.Fast), ants.Mutation(MutationType.Armored), ants.Mutation(MutationType.Regenerates),
                ants.Mutation(MutationType.SwarmCall), ants.Mutation(MutationType.HardenedShell), ants.Mutation(MutationType.AcidBlood),
            };
            var staticMutation = AssetDatabase.LoadAssetAtPath<MutationDefinition>("Assets/Data/Mutations/" + MutationType.Static + ".asset");
            if (staticMutation != null) mutations.Add(staticMutation);
            var sequel = AssetDatabase.LoadAssetAtPath<MutationDefinition>("Assets/Data/Mutations/" + MutationType.Sequel + ".asset");
            if (sequel != null) mutations.Add(sequel);
            mutations.RemoveAll(mutation => mutation == null);
            biome.mutations = mutations.ToArray();

            biome.startProp = ants.startProp;
            biome.exitProp = ants.exitProp;
            biome.treasureProp = ants.treasureProp;
            biome.shopProp = ants.shopProp;
            biome.extractPortalProp = ants.extractPortalProp;
            biome.deeperPortalProp = ants.deeperPortalProp;
            biome.portalProp = ArenaSceneBuilder.SavePrefab(Portal("ConsolePortal", neonPink, black, casing), "Props/ConsolePortal");

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
