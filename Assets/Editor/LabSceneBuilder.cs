using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// Builds the Lab scene: the hub between runs. A small room you walk around in, with the workbench (six upgrade
    /// pads) along one wall and the portal opposite. Menu: Shrunk > Rebuild Lab Scene.
    /// </summary>
    public static class LabSceneBuilder
    {
        /// <summary>How far east the workshop's middle is (and west, the office's).</summary>
        public const float WorkshopX = 21f;
        const float SideHalfWidth = 9f, SideHalfDepth = 7f, DoorHalf = 1.4f, DoorHeight = 3.2f;

        // One of the hub's side walls: two pieces, a lintel, and floor under the gap, so the doorway is walked straight through.
        static void SideWall(Transform room, string side, float x, float halfDepth, float height, Material wall, Material floor)
        {
            float northLength = halfDepth - DoorHalf;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall " + side + " N", room, new Vector3(x, height * 0.5f, DoorHalf + northLength * 0.5f), new Vector3(1f, height, northLength), wall, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall " + side + " S", room, new Vector3(x, height * 0.5f, -DoorHalf - northLength * 0.5f), new Vector3(1f, height, northLength), wall, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall " + side + " Lintel", room, new Vector3(x, (DoorHeight + height) * 0.5f, 0f), new Vector3(1f, height - DoorHeight, DoorHalf * 2f), wall, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Threshold " + side, room, new Vector3(x, -0.5f, 0f), new Vector3(1.4f, 1f, DoorHalf * 2f + 0.4f), floor, true);
        }

        // A side room off the hub: floor, three walls and a roof, the open side against the hub's doorway, and a light of its own.
        static void SideRoom(string name, Vector3 centre, float height, Material floor, Material wall, Color light)
        {
            var root = new GameObject(name).transform;
            float w = SideHalfWidth, d = SideHalfDepth;
            float outer = centre.x + Mathf.Sign(centre.x) * (w + 0.5f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Floor", root, new Vector3(centre.x, -0.5f, 0f), new Vector3(w * 2f, 1f, d * 2f), floor, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall North", root, new Vector3(centre.x, height * 0.5f, d + 0.5f), new Vector3(w * 2f + 2f, height, 1f), wall, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall South", root, new Vector3(centre.x, height * 0.5f, -d - 0.5f), new Vector3(w * 2f + 2f, height, 1f), wall, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall Far", root, new Vector3(outer, height * 0.5f, 0f), new Vector3(1f, height, d * 2f), wall, true);
            // The side against the hub: the hub's own wall, faced in this room's colour.
            float near = centre.x - Mathf.Sign(centre.x) * w;
            float faceX = near + Mathf.Sign(centre.x) * 0.02f;
            float pieceLength = d - DoorHalf;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Facing N", root, new Vector3(faceX, height * 0.5f, DoorHalf + pieceLength * 0.5f), new Vector3(0.04f, height, pieceLength), wall, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Facing S", root, new Vector3(faceX, height * 0.5f, -DoorHalf - pieceLength * 0.5f), new Vector3(0.04f, height, pieceLength), wall, false);
            var roof = ArenaSceneBuilder.Primitive(PrimitiveType.Quad, "Ceiling", root, new Vector3(centre.x, height, 0f), new Vector3(w * 2f + 2f, d * 2f + 2f, 1f), wall, true);
            roof.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            var lamp = new GameObject("Light").AddComponent<Light>();
            lamp.transform.SetParent(root, false);
            lamp.transform.position = new Vector3(centre.x, height - 0.8f, 0f);
            lamp.type = LightType.Point;
            lamp.color = light;
            lamp.range = 16f;
            lamp.intensity = 1.6f;
            lamp.shadows = LightShadows.None;
        }

        // A name over a doorway, on the hub side.
        static void Sign(string text, Transform parent, Vector3 at, float yaw)
        {
            var go = new GameObject("Sign " + text);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var mesh = go.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = 0.06f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(1f, 0.95f, 0.8f);
        }

        public const string ScenePath = "Assets/Scenes/Lab.unity";

        [MenuItem("Shrunk/Rebuild Lab Scene", priority = 22)]
        public static void Build()
        {
            ArenaSceneBuilder.EnsureFolder("Assets", "Scenes");
            ContentBuilder.Build(out var bulletPrefab);
            UpgradeDefinition[] upgrades = ContentBuilder.BuildUpgrades();

            var floorMat = ArenaSceneBuilder.Lit("LabFloor", new Color(0.32f, 0.36f, 0.42f), 0.5f);
            var wallMat = ArenaSceneBuilder.Lit("LabWall", new Color(0.18f, 0.2f, 0.26f), 0.3f);
            var benchMat = ArenaSceneBuilder.Lit("LabBench", new Color(0.5f, 0.52f, 0.56f), 0.7f);
            var voidBody = ArenaSceneBuilder.Lit("VoidBody", new Color(0.03f, 0.03f, 0.05f), 0.35f);
            var eyeGlow = ArenaSceneBuilder.Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));
            var portalGlow = ArenaSceneBuilder.Unlit("EyeGlow", new Color(0.45f, 0.95f, 1f));
            var black = ArenaSceneBuilder.Unlit("ExitHole", new Color(0.01f, 0.01f, 0.02f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArenaSceneBuilder.BuildLighting();

            // ----- the room -----
            const float halfWidth = 11f, halfDepth = 8f, height = 6f;
            var room = new GameObject("Lab Room").transform;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Floor", room, new Vector3(0f, -0.5f, 0f), new Vector3(halfWidth * 2f, 1f, halfDepth * 2f), floorMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall North", room, new Vector3(0f, height * 0.5f, halfDepth + 0.5f), new Vector3(halfWidth * 2f + 2f, height, 1f), wallMat, true);
            // The south wall has a doorway in it, between the portal and the power rack: the way to the firing range.
            const float doorLeft = 1.6f, doorRight = 3.6f, doorHeight = 3.2f;
            float southZ = -halfDepth - 0.5f, wallLeft = -halfWidth - 1f, wallRight = halfWidth + 1f;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall South W", room, new Vector3((wallLeft + doorLeft) * 0.5f, height * 0.5f, southZ), new Vector3(doorLeft - wallLeft, height, 1f), wallMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall South E", room, new Vector3((doorRight + wallRight) * 0.5f, height * 0.5f, southZ), new Vector3(wallRight - doorRight, height, 1f), wallMat, true);
            // The floor under the doorway: the wall is a metre thick, and the two rooms' floors stop either side of it.
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Threshold", room, new Vector3((doorLeft + doorRight) * 0.5f, -0.5f, southZ), new Vector3(doorRight - doorLeft + 0.4f, 1f, 1.4f), floorMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall South Lintel", room, new Vector3((doorLeft + doorRight) * 0.5f, (doorHeight + height) * 0.5f, southZ), new Vector3(doorRight - doorLeft, height - doorHeight, 1f), wallMat, true);
            // East and west: a doorway in the middle of each, to the workshop and the office.
            SideWall(room, "East", halfWidth + 0.5f, halfDepth, height, wallMat, floorMat);
            SideWall(room, "West", -halfWidth - 0.5f, halfDepth, height, wallMat, floorMat);

            // ----- the workshop, through the east doorway: getting stronger. Wood, pegboard, a warm light. -----
            var workFloor = ArenaSceneBuilder.Lit("WorkshopFloor", new Color(0.45f, 0.32f, 0.2f), 0.35f);
            var workWall = ArenaSceneBuilder.Lit("Pegboard", new Color(0.55f, 0.4f, 0.26f), 0.15f);
            SideRoom("Workshop", new Vector3(WorkshopX, 0f, 0f), height, workFloor, workWall, new Color(1f, 0.78f, 0.5f));
            Sign("WORKSHOP", room, new Vector3(halfWidth - 0.05f, DoorHeight + 0.6f, 0f), -90f);

            // ----- the office, through the west doorway: how it has gone so far. Carpet, green walls, lamplight. -----
            var officeFloor = ArenaSceneBuilder.Lit("OfficeCarpet", new Color(0.28f, 0.16f, 0.18f), 0.05f);
            var officeWall = ArenaSceneBuilder.Lit("OfficeWall", new Color(0.2f, 0.3f, 0.24f), 0.2f);
            SideRoom("Office", new Vector3(-WorkshopX, 0f, 0f), height, officeFloor, officeWall, new Color(1f, 0.85f, 0.6f));
            Sign("OFFICE", room, new Vector3(-halfWidth + 0.05f, DoorHeight + 0.6f, 0f), 90f);

            // A roof that only shows from below, so the camera never sees out over the walls.
            var roof = ArenaSceneBuilder.Primitive(PrimitiveType.Quad, "Ceiling", room, new Vector3(0f, height, 0f), new Vector3(halfWidth * 2f + 2f, halfDepth * 2f + 2f, 1f), wallMat, true);
            roof.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            // ----- the workbench: a long bench with one pad per upgrade in front of it -----
            var bench = new GameObject("Workbench").transform;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Bench", bench, new Vector3(WorkshopX, 0.55f, 5.6f), new Vector3(16.5f, 1.1f, 1.6f), benchMat, true);

            Color[] colours =
            {
                new Color(0.9f, 0.3f, 0.35f), new Color(0.3f, 0.6f, 1f), new Color(0.45f, 0.95f, 1f),
                new Color(1f, 0.8f, 0.2f), new Color(0.5f, 0.9f, 0.45f), new Color(0.85f, 0.5f, 1f)
            };
            for (int i = 0; i < upgrades.Length; i++)
            {
                var upgrade = upgrades[i];
                var glow = ArenaSceneBuilder.Unlit("Upgrade" + upgrade.type, colours[i % colours.Length]);
                // Spread along the bench however many there are.
                float x = WorkshopX - 7.2f + i * (14.4f / Mathf.Max(1, upgrades.Length - 1));

                var station = new GameObject(upgrade.displayName + " Pad");
                station.transform.SetParent(bench, false);
                station.transform.position = new Vector3(x, 0f, 3.2f);
                ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Pad", station.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), glow, false);

                // Equipment on the bench that lights up level by level, so progress is something you can see.
                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Machine", station.transform, new Vector3(0f, 1.35f, 2.4f), new Vector3(1.2f, 0.5f, 0.9f), wallMat, false);
                var markers = new GameObject[upgrade.MaxLevel];
                for (int level = 0; level < upgrade.MaxLevel; level++)
                {
                    markers[level] = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Level " + (level + 1), station.transform,
                        new Vector3(0f, 1.85f + level * 0.45f, 2.4f), new Vector3(0.6f, 0.35f, 0.6f), glow, false);
                    markers[level].SetActive(false);
                }

                var pad = station.AddComponent<UpgradePad>();
                pad.upgrade = upgrade;
                pad.levelMarkers = markers;
            }

            // ----- the portal -----
            var portal = ContentBuilder.Portal("Portal", portalGlow, black, benchMat);
            portal.transform.position = new Vector3(0f, 0f, -5.2f);
            portal.AddComponent<LabPortal>();

            // ----- the mirror: character styles -----
            var mirrorGlass = ArenaSceneBuilder.Lit("MirrorGlass", new Color(0.75f, 0.85f, 0.9f), 0.95f);
            mirrorGlass.SetFloat("_Metallic", 1f);
            var mirror = new GameObject("Mirror");
            mirror.transform.position = new Vector3(-WorkshopX - 6.2f, 0f, 4.2f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Frame", mirror.transform, new Vector3(-2.55f, 2.1f, 0f), new Vector3(0.2f, 4.2f, 3f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Glass", mirror.transform, new Vector3(-2.42f, 2.1f, 0f), new Vector3(0.1f, 3.8f, 2.6f), mirrorGlass, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", mirror.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), ArenaSceneBuilder.Unlit("StyleMat", new Color(0.9f, 0.9f, 0.95f)), false);
            mirror.AddComponent<StyleMirror>();

            // ----- the record board: how the runs have gone -----
            var board = new GameObject("Record Board");
            board.transform.position = new Vector3(-halfWidth - 4.2f, 0f, 4.4f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Board", board.transform, new Vector3(2.55f, 2.2f, 0f), new Vector3(0.2f, 2.6f, 4f), benchMat, false);
            var pegGold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
            var pegs = new GameObject[12];
            for (int i = 0; i < pegs.Length; i++)
            {
                pegs[i] = ArenaSceneBuilder.Primitive(PrimitiveType.Sphere, "Peg " + (i + 1), board.transform,
                    new Vector3(2.4f, 2.8f - (i / 6) * 0.9f, -1.5f + (i % 6) * 0.6f), Vector3.one * 0.32f, pegGold, false);
                pegs[i].SetActive(false);
            }
            board.AddComponent<TrophyBoard>().pegs = pegs;

            // ----- the evidence board: pages of Finch's notebook -----
            var evidence = new GameObject("Evidence Board");
            // Clear of the last upgrade pad, so standing at the board reads the board.
            evidence.transform.position = new Vector3(-halfWidth - 4.2f, 0f, -4.3f);
            var cork = ArenaSceneBuilder.Lit("Cork", new Color(0.55f, 0.38f, 0.22f), 0.1f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Board", evidence.transform, new Vector3(2.55f, 2f, 0f), new Vector3(0.2f, 3.5f, 3.2f), cork, false);
            var paper = ArenaSceneBuilder.Unlit("FinchNote", new Color(1f, 0.96f, 0.8f));
            var pinned = new GameObject[FinchNotes.All.Length];
            for (int i = 0; i < pinned.Length; i++)
            {
                pinned[i] = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Page " + (i + 1), evidence.transform,
                    new Vector3(2.42f, 3.1f - (i / 3) * 1.1f, -0.95f + (i % 3) * 0.95f), new Vector3(0.04f, 0.8f, 0.6f), paper, false);
                // Pinned by someone in a hurry.
                pinned[i].transform.localRotation = Quaternion.Euler((i * 37 % 13) - 6f, 0f, 0f);
                pinned[i].SetActive(false);
            }
            evidence.AddComponent<EvidenceBoard>().pages = pinned;

            // ----- the specimen shelf: a jar for every boss beaten, against the south wall -----
            var shelf = new GameObject("Specimen Shelf");
            shelf.transform.position = new Vector3(-WorkshopX + 0.5f, 0f, -4.4f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Shelf", shelf.transform, new Vector3(0f, 1.1f, -2.05f), new Vector3(5.6f, 0.15f, 0.7f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Leg L", shelf.transform, new Vector3(-2.6f, 0.52f, -2.05f), new Vector3(0.15f, 1.05f, 0.6f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Leg R", shelf.transform, new Vector3(2.6f, 0.52f, -2.05f), new Vector3(0.15f, 1.05f, 0.6f), benchMat, false);
            // A second shelf above the first: there are more bosses than one shelf holds.
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Shelf Top", shelf.transform, new Vector3(0f, 2.05f, -2.05f), new Vector3(5.6f, 0.15f, 0.7f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Upright L", shelf.transform, new Vector3(-2.75f, 1.55f, -2.05f), new Vector3(0.1f, 1.1f, 0.6f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Upright R", shelf.transform, new Vector3(2.75f, 1.55f, -2.05f), new Vector3(0.1f, 1.1f, 0.6f), benchMat, false);
            var jarGlass = ArenaSceneBuilder.Lit("JarGlass", new Color(0.6f, 0.8f, 0.85f), 0.95f);
            Color[] jarColours =
            {
                new Color(0.3f, 0.08f, 0.06f), new Color(0.45f, 0.1f, 0.35f), new Color(0.3f, 0.8f, 0.25f), new Color(0.55f, 0.8f, 0.95f),
                new Color(0.62f, 0.6f, 0.58f), new Color(0.13f, 0.14f, 0.16f), new Color(0.45f, 0.5f, 0.25f), new Color(0.7f, 0.88f, 1f), new Color(0.45f, 1f, 0.6f),
                new Color(0.93f, 0.88f, 0.74f), new Color(0.3f, 0.7f, 0.25f), new Color(0.42f, 0.4f, 0.18f), new Color(0.95f, 0.3f, 0.35f), new Color(0.32f, 0.34f, 0.42f), new Color(0.97f, 0.97f, 1f), new Color(1f, 0.1f, 0.9f), new Color(0.6f, 0.4f, 0.2f), new Color(1f, 0.55f, 0.1f), new Color(0.55f, 0.55f, 0.58f), new Color(0.85f, 0.15f, 0.15f), new Color(1f, 0.75f, 0.8f), new Color(1f, 0.85f, 0.15f), new Color(0.2f, 0.15f, 0.18f), new Color(0.12f, 0.1f, 0.1f), new Color(0.25f, 0.55f, 0.3f), new Color(0.8f, 0.1f, 0.1f), new Color(0.45f, 0.45f, 0.5f), new Color(0.15f, 0.4f, 0.25f)
            };
            var jars = new GameObject[Specimens.All.Length];
            for (int i = 0; i < jars.Length; i++)
            {
                var jar = new GameObject("Jar " + Specimens.All[i].key);
                jar.transform.SetParent(shelf.transform, false);
                // The first half along the bottom shelf, the rest along the top.
                int perShelf = (jars.Length + 1) / 2;
                int row = i / perShelf, along = i % perShelf;
                float spacing = 4.8f / Mathf.Max(1, perShelf - 1);
                jar.transform.localPosition = new Vector3(-2.4f + along * spacing, row == 0 ? 1.18f : 2.13f, -2.05f);
                jar.transform.localScale = Vector3.one * Mathf.Min(1f, spacing / 0.56f);
                // No see-through glass yet, so a jar is a base, a lid and four thin posts, with the specimen in between.
                ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Base", jar.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.54f, 0.03f, 0.54f), jarGlass, false);
                ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Lid", jar.transform, new Vector3(0f, 0.67f, 0f), new Vector3(0.54f, 0.04f, 0.54f), benchMat, false);
                for (int post = 0; post < 4; post++)
                {
                    Vector3 corner = Quaternion.Euler(0f, 45f + post * 90f, 0f) * new Vector3(0.24f, 0.35f, 0f);
                    ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Post " + post, jar.transform, corner, new Vector3(0.03f, 0.32f, 0.03f), jarGlass, false);
                }
                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Label", jar.transform, new Vector3(0f, -0.2f, 0.36f), new Vector3(0.4f, 0.16f, 0.02f), paper, false);
                // The last jar is Finch's, and is empty.
                if (i < jars.Length - 1)
                {
                    ArenaSceneBuilder.Primitive(PrimitiveType.Sphere, "Specimen", jar.transform, new Vector3(0f, 0.33f, 0f), Vector3.one * 0.36f,
                        ArenaSceneBuilder.Unlit("Specimen" + Specimens.All[i].key, jarColours[i % jarColours.Length]), false);
                }
                jar.SetActive(false);
                jars[i] = jar;
            }
            shelf.AddComponent<SpecimenShelf>().jars = jars;

            // ----- the power rack: which hero power to carry, against the south wall on the other side of the portal -----
            var rack = new GameObject("Power Rack");
            rack.transform.position = new Vector3(WorkshopX + 4f, 0f, -4.2f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Rack", rack.transform, new Vector3(0f, 0.6f, -2.2f), new Vector3(3.4f, 1.2f, 0.7f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", rack.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), ArenaSceneBuilder.Unlit("PowerMat", new Color(1f, 0.82f, 0.2f)), false);
            Color[] powerColours = { new Color(0.45f, 0.95f, 1f), new Color(0.6f, 0.75f, 1f), new Color(0.5f, 1f, 0.55f), new Color(1f, 0.96f, 0.8f), new Color(0.85f, 0.7f, 0.45f) };
            var canisters = new Transform[HeroPower.All.Length];
            for (int i = 0; i < canisters.Length; i++)
            {
                var canister = ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, HeroPower.All[i].name, rack.transform,
                    new Vector3(-1.4f + i * (2.8f / Mathf.Max(1, HeroPower.All.Length - 1)), 1.6f, -2.2f), new Vector3(0.5f, 0.4f, 0.5f), ArenaSceneBuilder.Unlit("Power" + HeroPower.All[i].kind, powerColours[i % powerColours.Length]), false);
                canisters[i] = canister.transform;
            }
            rack.AddComponent<PowerRack>().canisters = canisters;

            // ----- the filing cabinet: every kind of item ever found, in the corner by the record board -----
            var cabinet = new GameObject("Filing Cabinet");
            // Against the south wall in the corner, far enough from the record board that standing at one never reads the other.
            cabinet.transform.position = new Vector3(-WorkshopX - 3.5f, 0f, -6.1f);
            var cabinetMat = ArenaSceneBuilder.Lit("FilingCabinet", new Color(0.4f, 0.47f, 0.42f), 0.6f);
            var cabinetBody = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Body", cabinet.transform, Vector3.zero, new Vector3(1.1f, 2.4f, 0.9f), cabinetMat, false);
            cabinetBody.transform.localPosition = new Vector3(0f, 1.2f, -0.35f);
            var drawers = new Transform[4];
            for (int i = 0; i < drawers.Length; i++)
            {
                var drawer = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Drawer " + (i + 1), cabinet.transform, Vector3.zero, new Vector3(0.95f, 0.48f, 0.3f), benchMat, false);
                drawer.transform.localPosition = new Vector3(0f, 0.36f + i * 0.56f, 0.18f);
                var handle = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Handle", drawer.transform, Vector3.zero, new Vector3(0.4f, 0.14f, 0.2f), paper, false);
                handle.transform.localPosition = new Vector3(0f, 0f, 0.55f);
                drawers[i] = drawer.transform;
            }
            cabinet.AddComponent<FilingCabinet>().drawers = drawers;

            // ----- the time clock: overtime, for after the ending. In the corner past the mirror. -----
            var clock = new GameObject("Time Clock");
            clock.transform.position = new Vector3(-WorkshopX - 6.2f, 0f, 0.3f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Box", clock.transform, new Vector3(-1.5f, 1.9f, 0f), new Vector3(0.35f, 1.3f, 0.9f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Face", clock.transform, new Vector3(-1.3f, 2.15f, 0f), new Vector3(0.6f, 0.03f, 0.6f), paper, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Slot", clock.transform, new Vector3(-1.3f, 1.5f, 0f), new Vector3(0.05f, 0.08f, 0.5f), black, false);
            var clockLamp = ArenaSceneBuilder.Primitive(PrimitiveType.Sphere, "Lamp", clock.transform, new Vector3(-1.4f, 2.75f, 0f), Vector3.one * 0.3f, ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f)), false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", clock.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), paper, false);
            clock.AddComponent<TimeClock>().lamp = clockLamp;

            // ----- the equipment cupboard: which weapon to carry. Free-standing, west of the middle of the room. -----
            var cupboard = new GameObject("Equipment Cupboard");
            cupboard.transform.position = new Vector3(WorkshopX - 4.6f, 0f, -3.6f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Cupboard", cupboard.transform, new Vector3(-1.9f, 0.6f, 0f), new Vector3(0.7f, 1.2f, 3.2f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", cupboard.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), ArenaSceneBuilder.Unlit("WeaponMat", new Color(1f, 0.5f, 0.3f)), false);
            Color[] weaponColours = { new Color(0.45f, 0.95f, 1f), new Color(1f, 0.6f, 0.3f), new Color(0.85f, 0.4f, 1f), new Color(0.5f, 1f, 0.55f), new Color(0.8f, 0.82f, 0.88f) };
            var pieces = new Transform[Weapons.All.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, Weapons.All[i].name, cupboard.transform,
                    new Vector3(-1.9f, 1.5f, -1.2f + i * (2.4f / Mathf.Max(1, pieces.Length - 1))), new Vector3(0.3f, 0.3f, i == 1 ? 0.4f : 0.6f), ArenaSceneBuilder.Unlit("Weapon" + Weapons.All[i].kind, weaponColours[i % weaponColours.Length]), false);
                pieces[i] = piece.transform;
            }
            cupboard.AddComponent<WeaponRack>().pieces = pieces;

            // ----- the machine's panel: there from the first day, with a socket for each missing part. East of the middle. -----
            var panel = new GameObject("Machine Panel");
            panel.transform.position = new Vector3(4.2f, 0f, -1.2f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Console", panel.transform, new Vector3(1.9f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 3.2f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", panel.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), ArenaSceneBuilder.Unlit("MachineGlow", new Color(0.45f, 0.95f, 1f)), false);
            var partGlow = ArenaSceneBuilder.Unlit("MachineShot", new Color(1f, 0.5f, 0.9f));
            var lamps = new GameObject[MachineParts.All.Length];
            for (int i = 0; i < lamps.Length; i++)
            {
                float z = -1.05f + i * 0.7f;
                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Socket " + (i + 1), panel.transform, new Vector3(1.9f, 1.45f, z), new Vector3(0.5f, 0.12f, 0.5f), black, false);
                lamps[i] = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Part " + (i + 1), panel.transform, new Vector3(1.9f, 1.7f, z), Vector3.one * 0.36f, partGlow, false);
            }
            panel.AddComponent<MachinePanel>().lamps = lamps;

            // ----- the field guide: a book on a lectern, every creature met so far -----
            var guide = new GameObject("Field Guide");
            guide.transform.position = new Vector3(-WorkshopX - 2.5f, 0f, 4.6f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Lectern", guide.transform, new Vector3(0f, 0.55f, 1.1f), new Vector3(0.6f, 1.1f, 0.5f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Book", guide.transform, new Vector3(0f, 1.15f, 1.05f), new Vector3(0.75f, 0.08f, 0.55f), ArenaSceneBuilder.Lit("LeatherGreen", new Color(0.15f, 0.36f, 0.24f), 0.35f), false).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Pages", guide.transform, new Vector3(0f, 1.2f, 1.04f), new Vector3(0.68f, 0.03f, 0.5f), paper, false).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", guide.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), ArenaSceneBuilder.Unlit("GuideMat", new Color(0.55f, 0.85f, 0.6f)), false);
            guide.AddComponent<FieldGuideBook>();

            // ----- the noticeboard: three jobs at a time. Free-standing, west of where the day starts. -----
            var notices = new GameObject("Noticeboard");
            notices.transform.position = new Vector3(-WorkshopX + 1f, 0f, 2.2f);
            var corkBoard = ArenaSceneBuilder.Lit("Cork", new Color(0.55f, 0.38f, 0.22f), 0.1f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Post L", notices.transform, new Vector3(-1.5f, 0.9f, -0.9f), new Vector3(0.12f, 1.8f, 0.12f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Post R", notices.transform, new Vector3(-1.5f, 0.9f, 0.9f), new Vector3(0.12f, 1.8f, 0.12f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Board", notices.transform, new Vector3(-1.5f, 1.9f, 0f), new Vector3(0.12f, 1.4f, 2.2f), corkBoard, false);
            for (int i = 0; i < Jobs.Pinned; i++)
            {
                var slip = ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Job " + (i + 1), notices.transform, new Vector3(-1.42f, 1.95f, -0.65f + i * 0.65f), new Vector3(0.03f, 0.75f, 0.5f), paper, false);
                slip.transform.localRotation = Quaternion.Euler((i * 5) - 5f, 0f, 0f);
            }
            notices.AddComponent<JobBoard>();

            // ----- the firing range: its own room, through the doorway in the south wall -----
            // Twenty metres by twelve. The firing line is at the west end; the targets stand toward the east.
            const float rangeLeft = -8f, rangeRight = 12f, rangeNear = -9f, rangeFar = -21f;
            float rangeX = (rangeLeft + rangeRight) * 0.5f, rangeZ = (rangeNear + rangeFar) * 0.5f;
            var rangeRoom = new GameObject("Range Room").transform;
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Floor", rangeRoom, new Vector3(rangeX, -0.5f, rangeZ), new Vector3(rangeRight - rangeLeft, 1f, rangeNear - rangeFar), floorMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall South", rangeRoom, new Vector3(rangeX, height * 0.5f, rangeFar - 0.5f), new Vector3(rangeRight - rangeLeft + 2f, height, 1f), wallMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall East", rangeRoom, new Vector3(rangeRight + 0.5f, height * 0.5f, rangeZ), new Vector3(1f, height, rangeNear - rangeFar), wallMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Wall West", rangeRoom, new Vector3(rangeLeft - 0.5f, height * 0.5f, rangeZ), new Vector3(1f, height, rangeNear - rangeFar), wallMat, true);
            var rangeRoof = ArenaSceneBuilder.Primitive(PrimitiveType.Quad, "Ceiling", rangeRoom, new Vector3(rangeX, height, rangeZ), new Vector3(rangeRight - rangeLeft + 2f, rangeNear - rangeFar + 2f, 1f), wallMat, true);
            rangeRoof.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            // A sign over the doorway, on the lab's side.
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Range Sign", rangeRoom, new Vector3(2.6f, 3.7f, -7.95f), new Vector3(1.8f, 0.5f, 0.08f), paper, false);

            var rangeRed = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));
            var range = new GameObject("Firing Range");
            range.transform.position = new Vector3(-5.5f, 0f, -15f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", range.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), rangeRed, false);
            // The firing line, painted on the floor.
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Line", range.transform, new Vector3(2.2f, 0.01f, 0f), new Vector3(0.2f, 0.02f, 11f), paper, false);

            // Five targets: three standing at different distances, two sliding across.
            var targetSpots = new[] { new Vector3(3f, 0f, -12f), new Vector3(6f, 0f, -18.5f), new Vector3(8.5f, 0f, -13.5f), new Vector3(5f, 0f, -15.5f), new Vector3(10f, 0f, -16.5f) };
            var targetSlides = new[] { Vector3.zero, Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 2.2f), new Vector3(0f, 0f, 3f) };
            for (int i = 0; i < targetSpots.Length; i++)
            {
                var target = new GameObject("Target " + (i + 1));
                target.transform.position = targetSpots[i];
                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Post", target.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.6f, 0.12f), benchMat, false);
                var face = new GameObject("Face");
                face.transform.SetParent(target.transform, false);
                // Discs on edge, facing the firing line: red, white, red.
                var outer = ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Outer", face.transform, new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 0.05f, 1.1f), rangeRed, false);
                var middle = ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Middle", face.transform, new Vector3(-0.03f, 1.1f, 0f), new Vector3(0.72f, 0.05f, 0.72f), paper, false);
                var bull = ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Bull", face.transform, new Vector3(-0.06f, 1.1f, 0f), new Vector3(0.3f, 0.05f, 0.3f), rangeRed, false);
                foreach (var disc in new[] { outer, middle, bull }) disc.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var hit = target.AddComponent<SphereCollider>();
                hit.center = new Vector3(0f, 1.1f, 0f);
                hit.radius = 0.6f;
                var targetHealth = target.AddComponent<Health>();
                targetHealth.team = Team.Enemy;
                targetHealth.maxHealth = 1;
                var rangeTarget = target.AddComponent<RangeTarget>();
                rangeTarget.face = face;
                rangeTarget.slide = targetSlides[i];
                rangeTarget.slideSeconds = 3f + i * 0.4f;
            }

            // A sixth, gold, at the very back: up for a few seconds every so often, and worth three.
            {
                var rangeGold = ArenaSceneBuilder.Unlit("Gold", new Color(1f, 0.82f, 0.2f));
                var golden = new GameObject("Target Gold");
                golden.transform.position = new Vector3(11f, 0f, -19.5f);
                ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Post", golden.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.6f, 0.12f), benchMat, false);
                var goldFace = new GameObject("Face");
                goldFace.transform.SetParent(golden.transform, false);
                ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Disc", goldFace.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.8f, 0.05f, 0.8f), rangeGold, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Bull", goldFace.transform, new Vector3(-0.04f, 1.1f, 0f), new Vector3(0.25f, 0.05f, 0.25f), paper, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var goldHit = golden.AddComponent<SphereCollider>();
                goldHit.center = new Vector3(0f, 1.1f, 0f);
                goldHit.radius = 0.45f;
                var goldHealth = golden.AddComponent<Health>();
                goldHealth.team = Team.Enemy;
                goldHealth.maxHealth = 1;
                var goldTarget = golden.AddComponent<RangeTarget>();
                goldTarget.face = goldFace;
                goldTarget.appearsEvery = 8f;
                goldTarget.staysFor = 3.5f;
                goldTarget.worth = 3;
            }

            // The board on the far wall.
            var rangeBoard = new GameObject("Range Board");
            rangeBoard.transform.position = new Vector3(rangeRight - 0.15f, 3.9f, -15f);
            rangeBoard.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var boardText = rangeBoard.AddComponent<TextMesh>();
            Font rangeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            boardText.font = rangeFont;
            rangeBoard.GetComponent<MeshRenderer>().sharedMaterial = rangeFont.material;
            boardText.fontSize = 96;
            boardText.characterSize = 0.11f;
            boardText.anchor = TextAnchor.MiddleCenter;
            boardText.alignment = TextAlignment.Center;
            boardText.color = new Color(1f, 0.95f, 0.7f);
            boardText.text = "BEST 0";
            range.AddComponent<FiringRange>().board = boardText;

            // The fortune machine, against the west wall of the range, by the door.
            var fortune = new GameObject("Fortune Machine");
            fortune.transform.position = new Vector3(-6.2f, 0f, -11f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Cabinet", fortune.transform, new Vector3(-1.4f, 1.1f, 0f), new Vector3(0.9f, 2.2f, 1.2f), cork, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Window", fortune.transform, new Vector3(-0.94f, 1.5f, 0f), new Vector3(0.04f, 0.8f, 0.9f), paper, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Sphere, "Ball", fortune.transform, new Vector3(-1.4f, 2.45f, 0f), Vector3.one * 0.55f, ArenaSceneBuilder.Unlit("MachineShot", new Color(1f, 0.5f, 0.9f)), false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", fortune.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), paper, false);
            fortune.AddComponent<FortuneMachine>();

            // Crates in the far corner, and behind them, out of sight of the firing line, the vent.
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Crate A", rangeRoom, new Vector3(9.6f, 0.9f, -10.6f), new Vector3(1.8f, 1.8f, 1.8f), benchMat, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Crate B", rangeRoom, new Vector3(9.3f, 2.3f, -10.4f), new Vector3(1.2f, 1f, 1.2f), cork, true);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Crate C", rangeRoom, new Vector3(8f, 0.6f, -10f), new Vector3(1.2f, 1.2f, 1.2f), cork, true);
            var vent = new GameObject("Range Vent");
            vent.transform.position = new Vector3(11.2f, 0f, -9.9f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Vent", vent.transform, new Vector3(0.72f, 0.5f, 0f), new Vector3(0.08f, 0.8f, 1f), black, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Cover", vent.transform, new Vector3(0.3f, 0.05f, 0.9f), new Vector3(0.9f, 0.06f, 1.1f), benchMat, false);
            vent.AddComponent<RangeVent>();

            // ----- a mouse hole in the north wall, lit from inside. Finch's radio stands next to it, cable and all. -----
            var hole = new GameObject("Wall Hole");
            hole.transform.position = new Vector3(0f, 0f, halfDepth - 0.05f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Dark", hole.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.1f), black, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Arch", hole.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.05f, 0.9f), black, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Glow", hole.transform, new Vector3(0f, 0.4f, -0.07f), new Vector3(0.5f, 0.5f, 0.02f), ArenaSceneBuilder.Unlit("HoleLight", new Color(1f, 0.85f, 0.5f)), false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Cable", hole.transform, new Vector3(-0.95f, 0.02f, -0.4f), new Vector3(1.1f, 0.03f, 0.05f), black, false);
            hole.AddComponent<WallHole>();

            // ----- the Daily Experiment: one run a day on the day's own floors, chalked up on the north wall -----
            var daily = new GameObject("Daily Experiment");
            daily.transform.position = new Vector3(-6f, 0f, halfDepth - 1.6f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Board", daily.transform, new Vector3(0f, 3f, 1.53f), new Vector3(4.2f, 1.7f, 0.1f), ArenaSceneBuilder.Lit("Chalkboard", new Color(0.08f, 0.14f, 0.1f), 0.1f), false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", daily.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), ArenaSceneBuilder.Unlit("DailyMat", new Color(0.95f, 0.95f, 0.85f)), false);
            var chalk = new GameObject("Chalk");
            chalk.transform.SetParent(daily.transform, false);
            chalk.transform.localPosition = new Vector3(0f, 3f, 1.46f);
            var chalkText = chalk.AddComponent<TextMesh>();
            Font chalkFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            chalkText.font = chalkFont;
            chalk.GetComponent<MeshRenderer>().sharedMaterial = chalkFont.material;
            chalkText.fontSize = 48;
            chalkText.characterSize = 0.045f;
            chalkText.anchor = TextAnchor.MiddleCenter;
            chalkText.alignment = TextAlignment.Center;
            chalkText.color = new Color(0.92f, 0.95f, 0.9f);
            daily.AddComponent<DailyExperiment>().board = chalkText;

            // ----- the Gauntlet: boss after boss, chalked up on the north wall the other side of the hole -----
            var gauntlet = new GameObject("Gauntlet");
            gauntlet.transform.position = new Vector3(6f, 0f, halfDepth - 1.6f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Board", gauntlet.transform, new Vector3(0f, 3f, 1.53f), new Vector3(4.2f, 1.4f, 0.1f), ArenaSceneBuilder.Lit("GauntletBoard", new Color(0.25f, 0.06f, 0.06f), 0.2f), false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", gauntlet.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), ArenaSceneBuilder.Unlit("GauntletMat", new Color(1f, 0.35f, 0.3f)), false);
            var gauntletWords = new GameObject("Words");
            gauntletWords.transform.SetParent(gauntlet.transform, false);
            gauntletWords.transform.localPosition = new Vector3(0f, 3f, 1.46f);
            var gauntletText = gauntletWords.AddComponent<TextMesh>();
            gauntletText.font = chalkFont;
            gauntletWords.GetComponent<MeshRenderer>().sharedMaterial = chalkFont.material;
            gauntletText.fontSize = 48;
            gauntletText.characterSize = 0.05f;
            gauntletText.anchor = TextAnchor.MiddleCenter;
            gauntletText.alignment = TextAlignment.Center;
            gauntletText.color = new Color(1f, 0.85f, 0.75f);
            gauntlet.AddComponent<GauntletStation>().board = gauntletText;

            // ----- Dr. Finch's radio set, beside the hole. It can be shot. He would rather it were not. -----
            var radioSet = new GameObject("Finch's Radio");
            radioSet.transform.SetPositionAndRotation(new Vector3(-1.9f, 0f, halfDepth - 1.2f), Quaternion.Euler(0f, 90f, 0f));
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Stand", radioSet.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.7f, 0.8f, 0.7f), benchMat, false);
            var radioFace = new GameObject("Face");
            radioFace.transform.SetParent(radioSet.transform, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Set", radioFace.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.6f, 0.6f, 0.9f), cork, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Speaker", radioFace.transform, new Vector3(0.31f, 1.1f, -0.15f), new Vector3(0.4f, 0.02f, 0.4f), black, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Dial", radioFace.transform, new Vector3(0.31f, 1.2f, 0.28f), new Vector3(0.02f, 0.12f, 0.2f), paper, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Aerial", radioFace.transform, new Vector3(-0.15f, 1.9f, 0.3f), new Vector3(0.03f, 1f, 0.03f), benchMat, false);
            var radioHit = radioSet.AddComponent<SphereCollider>();
            radioHit.center = new Vector3(0f, 1.1f, 0f);
            radioHit.radius = 0.55f;
            var radioHealth = radioSet.AddComponent<Health>();
            radioHealth.team = Team.Enemy;
            radioHealth.maxHealth = 1;
            var radioTarget = radioSet.AddComponent<RangeTarget>();
            radioTarget.face = radioFace;
            radioTarget.isRadio = true;
            radioTarget.standsUpAfter = 2.5f;

            // ----- the hidden door: turns up with the last page, on the wall the radio never mentions -----
            var door = new GameObject("Hidden Door");
            door.transform.position = new Vector3(-WorkshopX - 6.2f, 0f, -3.6f);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Frame", door.transform, new Vector3(-2.6f, 1.7f, 0f), new Vector3(0.25f, 3.4f, 2.4f), benchMat, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Dark", door.transform, new Vector3(-2.5f, 1.55f, 0f), new Vector3(0.2f, 3.1f, 1.9f), black, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cube, "Sign", door.transform, new Vector3(-2.42f, 3.6f, 0f), new Vector3(0.06f, 0.3f, 1.4f), paper, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat", door.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.8f, 0.02f, 1.8f), black, false);
            ArenaSceneBuilder.Primitive(PrimitiveType.Cylinder, "Mat Rim", door.transform, new Vector3(0f, 0.01f, 0f), new Vector3(2.1f, 0.01f, 2.1f), paper, false);
            door.AddComponent<HiddenDoor>();

            // ----- player, camera, HUD -----
            var player = ArenaSceneBuilder.BuildPlayer(voidBody, eyeGlow, bulletPrefab);
            // Start facing the portal, with the workbench behind: the first thing you see is the way into a run.
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 1.5f), Quaternion.Euler(0f, 180f, 0f));
            ArenaSceneBuilder.BuildCamera(player.transform);
            var touch = ArenaSceneBuilder.BuildHud();
            player.GetComponent<PlayerInputReader>().touch = touch;

            var hud = touch.GetComponent<Hud>();
            hud.lab = true;
            hud.killsText.gameObject.SetActive(false);
            hud.powerFill.transform.parent.gameObject.SetActive(false); // nothing to fight in the lab

            var game = new GameObject("Game");
            ContentBuilder.AddGameAssets(game);
            game.AddComponent<LabDirector>();
            game.AddComponent<Playtest>();
            game.GetComponent<ScreenCards>().title = true;
            // The lab starts with the portal and the workbench, and fills in as there is a reason for each thing.
            game.AddComponent<LabGrowth>().stations = new[]
            {
                new LabGrowth.Station { name = "the record board", thing = board, needs = LabGrowth.Needs.ARunStarted },
                new LabGrowth.Station { name = "the mirror", thing = mirror, needs = LabGrowth.Needs.AStyleUnlocked },
                new LabGrowth.Station { name = "the evidence board", thing = evidence, needs = LabGrowth.Needs.ANoteFound },
                new LabGrowth.Station { name = "a door that was not there before", thing = door, needs = LabGrowth.Needs.AllNotesFound },
                // New stations go on the end: the save remembers which ones the player has been told about by position.
                new LabGrowth.Station { name = "the specimen shelf", thing = shelf, needs = LabGrowth.Needs.ABossBeaten },
                new LabGrowth.Station { name = "the power rack", thing = rack, needs = LabGrowth.Needs.ARunStarted },
                new LabGrowth.Station { name = "the filing cabinet", thing = cabinet, needs = LabGrowth.Needs.AnItemFound },
                new LabGrowth.Station { name = "a time clock", thing = clock, needs = LabGrowth.Needs.FinchBeaten },
                new LabGrowth.Station { name = "the equipment cupboard", thing = cupboard, needs = LabGrowth.Needs.ABossBeaten },
            };

            EditorSceneManager.SaveScene(scene, ScenePath);
            RunSceneBuilder.UpdateBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Shrunk] Lab scene rebuilt at " + ScenePath);
        }
    }
}
