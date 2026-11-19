using NUnit.Framework;
using UnityEngine;

namespace Shrunk.Tests
{
    /// <summary>Checks the random room carver on its own, with no scene and no 3D.</summary>
    public class RoomShapeTests
    {
        [Test]
        public void Forty_thousand_rooms_obey_every_rule()
        {
            var types = new[] { RoomType.Start, RoomType.Normal, RoomType.Exit, RoomType.Treasure, RoomType.Shop };
            int checkedRooms = 0;

            for (int seed = 1; seed <= 2700; seed++)
            {
                // Every combination of doorways (1-15), every room type, small to large.
                Doors doors = (Doors)(1 + seed % 15);
                RoomType type = types[seed % types.Length];
                float size = (seed % 11) / 10f;

                var shape = RoomShape.Generate(seed * 104729, doors, type, size);
                var problems = shape.Validate();
                if (problems.Count > 0)
                {
                    Assert.Fail($"seed {shape.seed}, doors {doors}, {type}, size {size}:\n{string.Join("\n", problems)}\n{shape.ToAscii()}");
                }
                checkedRooms++;
            }

            // Plus the rooms of whole floors, exactly as the game makes them.
            for (int seed = 1; seed <= 400; seed++)
            {
                for (int floor = 1; floor <= 10; floor++)
                {
                    var layout = FloorLayout.Generate(seed * 7919 + floor, FloorLayout.RoomCountForFloor(floor));
                    foreach (var node in layout.ordered)
                    {
                        var shape = RoomShape.GenerateFor(layout, node, floor);
                        var problems = shape.Validate();
                        if (problems.Count > 0)
                        {
                            Assert.Fail($"floor seed {layout.seed}, floor {floor}, room {node.cell}:\n{string.Join("\n", problems)}\n{shape.ToAscii()}");
                        }
                        checkedRooms++;
                    }
                }
            }

            Assert.Greater(checkedRooms, 40000);
        }

        [Test]
        public void Same_seed_gives_the_same_room_and_different_seeds_differ()
        {
            var a = RoomShape.Generate(777, Doors.North | Doors.West, RoomType.Normal, 0.8f);
            var b = RoomShape.Generate(777, Doors.North | Doors.West, RoomType.Normal, 0.8f);
            Assert.AreEqual(a.ToAscii(), b.ToAscii());

            int different = 0;
            for (int seed = 1; seed <= 50; seed++)
            {
                if (RoomShape.Generate(seed, Doors.North | Doors.West, RoomType.Normal, 0.8f).ToAscii() != a.ToAscii()) different++;
            }
            Assert.Greater(different, 45, "rooms are not varied enough");
        }

        [Test]
        public void Early_floors_have_smaller_rooms_than_deep_floors()
        {
            float early = AverageOpenTiles(2), deep = AverageOpenTiles(9);
            Debug.Log($"[Shrunk] Average open tiles per fight room: floor 2 = {early:F1}, floor 9 = {deep:F1}");
            Assert.Less(early, deep * 0.8f);
        }

        static float AverageOpenTiles(int floor)
        {
            int rooms = 0, tiles = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var layout = FloorLayout.Generate(seed, FloorLayout.RoomCountForFloor(floor));
                foreach (var node in layout.ordered)
                {
                    if (node.type != RoomType.Normal) continue;
                    tiles += RoomShape.GenerateFor(layout, node, floor).interior.Count;
                    rooms++;
                }
            }
            return (float)tiles / rooms;
        }

        [Test]
        public void Fight_rooms_have_cover_and_quiet_rooms_do_not()
        {
            int fightRooms = 0, withCover = 0, pebbles = 0;
            for (int seed = 1; seed <= 300; seed++)
            {
                var fight = RoomShape.Generate(seed * 31, (Doors)(1 + seed % 15), RoomType.Normal, (seed % 11) / 10f);
                fightRooms++;
                pebbles += fight.cover.Count;
                if (fight.cover.Count > 0) withCover++;
                Assert.AreEqual(fight.interior.Count, fight.free.Count + fight.cover.Count);

                Assert.AreEqual(0, RoomShape.Generate(seed * 31, Doors.South, RoomType.Treasure, 0f).cover.Count, "cover in a treasure room");
                Assert.AreEqual(0, RoomShape.Generate(seed * 31, Doors.South, RoomType.Exit, 1f, arena: true).cover.Count, "cover in a boss arena");
            }
            Debug.Log($"[Shrunk] {withCover} of {fightRooms} fight rooms have cover, {(float)pebbles / fightRooms:F1} pebbles each on average");
            Assert.Greater(withCover, fightRooms * 0.85f);
        }

        [TearDown]
        public void NoForcedFeature() => RoomShape.ForceFeature = null;

        [Test]
        public void Fight_rooms_come_in_every_kind_and_quiet_rooms_are_plain()
        {
            var seen = new System.Collections.Generic.Dictionary<RoomFeature, int>();
            for (int seed = 1; seed <= 600; seed++)
            {
                var fight = RoomShape.Generate(seed * 131, (Doors)(1 + seed % 15), RoomType.Normal, (seed % 11) / 10f);
                seen.TryGetValue(fight.feature, out int count);
                seen[fight.feature] = count + 1;
                Assert.AreEqual(fight.feature == RoomFeature.DewRoom, fight.spots.Count > 0, "only dew rooms have dew spots");

                Assert.AreEqual(RoomFeature.Open, RoomShape.Generate(seed * 131, Doors.South, RoomType.Shop, 0f).feature);
                Assert.AreEqual(RoomFeature.Open, RoomShape.Generate(seed * 131, Doors.South, RoomType.Exit, 0.5f).feature, "the exit room stays plain");
            }
            foreach (RoomFeature feature in System.Enum.GetValues(typeof(RoomFeature)))
            {
                seen.TryGetValue(feature, out int count);
                Debug.Log($"[Shrunk] {RoomDressing.NameOf(feature)}: {count} of 600 fight rooms");
                Assert.Greater(count, 30, $"{feature} almost never comes up");
            }
        }

        [Test]
        public void Boss_floors_get_a_big_arena_that_is_the_same_place_every_time()
        {
            int floors = 0, withArena = 0;
            string picture = null;
            for (int seed = 1; seed <= 600; seed++)
            {
                foreach (int floor in new[] { 5, 10 })
                {
                    floors++;
                    var layout = FloorLayout.Generate(seed * 7919 + floor, FloorLayout.RoomCountForFloor(floor), true);
                    Assert.IsEmpty(layout.Validate(), layout.ToAscii());

                    // The rest of the floor is exactly what it would be without the arena.
                    var plain = FloorLayout.Generate(seed * 7919 + floor, FloorLayout.RoomCountForFloor(floor));
                    foreach (var node in plain.ordered)
                    {
                        // An ordinary room may be swallowed by the arena; nothing else changes.
                        if (layout.rooms[node.cell].type == RoomType.Arena) Assert.AreEqual(RoomType.Normal, node.type);
                        else Assert.AreEqual(node.type, layout.rooms[node.cell].type);
                        if (node.type != RoomType.Normal && node.type != RoomType.Exit) Assert.AreEqual(node.doors, layout.rooms[node.cell].doors, "the arena gave a " + node.type + " room a new doorway");
                    }
                    if (!layout.hasArena) continue;
                    withArena++;
                    Assert.That(layout.rooms.Count, Is.InRange(plain.rooms.Count, plain.rooms.Count + 3));

                    // Stitch the four quarters together and compare with the hand-laid plan.
                    var kind = floor == 10 ? RoomShape.ArenaKind.ThroneHall : RoomShape.ArenaKind.Nursery;
                    int variant = Mathf.Abs(layout.seed % 3), open = 0;
                    var text = new System.Text.StringBuilder();
                    for (int y = RoomShape.ArenaSize - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < RoomShape.ArenaSize; x++)
                        {
                            var cell = layout.arenaOrigin + new Vector2Int(x / RoomShape.Size, y / RoomShape.Size);
                            var shape = RoomShape.GenerateFor(layout, layout.rooms[cell], floor);
                            bool isOpen = shape.open[x % RoomShape.Size, y % RoomShape.Size];
                            if (isOpen) open++;
                            text.Append(isOpen ? ". " : "# ");
                            // Tunnels in from outside doorways may open more; nothing planned open is ever closed.
                            if (RoomShape.ArenaOpen(kind, variant, x, y)) Assert.IsTrue(isOpen, $"arena tile {x},{y} should be open");
                            else if (RoomShape.ArenaOpen(kind, -1, x, y)) Assert.IsFalse(isOpen, $"a pillar or column at {x},{y} was dug away");
                        }
                        text.AppendLine();
                    }
                    if (picture == null && floor == 10) picture = text.ToString();
                    Assert.Greater(open, 400, "an arena should be far bigger than any fight room (225 tiles at most)");

                    foreach (var node in layout.ordered)
                    {
                        if (!layout.InArena(node.cell)) continue;
                        var shape = RoomShape.GenerateFor(layout, node, floor);
                        Assert.IsTrue(shape.isArena && shape.isArenaPart);
                        Assert.IsEmpty(shape.Validate(), shape.ToAscii());
                    }
                }
            }
            Debug.Log($"[Shrunk] {withArena} of {floors} boss floors have room for the big arena. A throne hall:\n{picture}");
            Assert.Greater(withArena, floors * 0.9f, "the big arena should nearly always fit");

            // The plan itself: the same every time, symmetric, and clear along the seams between the quarters.
            foreach (RoomShape.ArenaKind kind in System.Enum.GetValues(typeof(RoomShape.ArenaKind)))
            {
                for (int variant = 0; variant < 3; variant++)
                {
                    int last = RoomShape.ArenaSize - 1;
                    for (int x = 0; x <= last; x++)
                    {
                        for (int y = 0; y <= last; y++)
                        {
                            bool open = RoomShape.ArenaOpen(kind, variant, x, y);
                            Assert.AreEqual(open, RoomShape.ArenaOpen(kind, variant, last - x, y), "not the same left and right");
                            Assert.AreEqual(open, RoomShape.ArenaOpen(kind, variant, x, last - y), "not the same front and back");
                        }
                        // The two tiles either side of a seam always match, so no wall is ever missing there.
                        Assert.AreEqual(RoomShape.ArenaOpen(kind, variant, RoomShape.Size - 1, x), RoomShape.ArenaOpen(kind, variant, RoomShape.Size, x));
                        Assert.AreEqual(RoomShape.ArenaOpen(kind, variant, x, RoomShape.Size - 1), RoomShape.ArenaOpen(kind, variant, x, RoomShape.Size));
                    }
                }
            }
        }

        [Test]
        public void One_through_room_in_four_is_a_tunnel_bend()
        {
            int bends = 0, through = 0, bendTiles = 0, plainTiles = 0;
            string sample = null;
            for (int seed = 1; seed <= 800; seed++)
            {
                // Every pair of doorways: corners and straight-throughs.
                Doors[] pairs = { Doors.North | Doors.East, Doors.North | Doors.South, Doors.East | Doors.West, Doors.South | Doors.West, Doors.North | Doors.West, Doors.East | Doors.South };
                var shape = RoomShape.Generate(seed * 977, pairs[seed % pairs.Length], RoomType.Normal, (seed % 11) / 10f);
                through++;
                if (shape.feature == RoomFeature.TunnelBend)
                {
                    bends++;
                    bendTiles += shape.interior.Count;
                    if (sample == null) sample = shape.ToAscii();
                    Assert.IsEmpty(shape.Validate(), shape.ToAscii());
                }
                else plainTiles += shape.interior.Count;

                // Never with one doorway, three or four.
                Assert.AreNotEqual(RoomFeature.TunnelBend, RoomShape.Generate(seed * 977, Doors.North, RoomType.Normal, 0.5f).feature);
                Assert.AreNotEqual(RoomFeature.TunnelBend, RoomShape.Generate(seed * 977, Doors.North | Doors.East | Doors.South, RoomType.Normal, 0.5f).feature);
            }
            Debug.Log($"[Shrunk] {bends} of {through} two-door fight rooms are tunnel bends; {(float)bendTiles / bends:F0} open tiles each against {(float)plainTiles / (through - bends):F0}\n{sample}");
            Assert.That(bends, Is.InRange(through * 0.18f, through * 0.32f));
            Assert.Less((float)bendTiles / bends, (float)plainTiles / (through - bends) * 0.8f, "a bend should be tighter than a chamber");
        }

        [Test]
        public void A_feature_never_changes_the_shape_of_a_room()
        {
            for (int seed = 1; seed <= 60; seed++)
            {
                RoomShape.ForceFeature = null;
                string plain = RoomShape.Generate(seed, Doors.North | Doors.East, RoomType.Normal, 0.6f).ToAscii().Replace('~', '.');
                foreach (RoomFeature feature in System.Enum.GetValues(typeof(RoomFeature)))
                {
                    RoomShape.ForceFeature = feature;
                    var forced = RoomShape.Generate(seed, Doors.North | Doors.East, RoomType.Normal, 0.6f);
                    Assert.AreEqual(plain, forced.ToAscii().Replace('~', '.'));
                    Assert.IsEmpty(forced.Validate());
                }
            }
        }

        [Test]
        public void Bigger_rooms_hold_more_enemies()
        {
            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            Assert.AreEqual(2, biome.EnemiesFor(1, 81), "a small room on floor 1");
            Assert.AreEqual(3, biome.EnemiesFor(1, 121));
            Assert.AreEqual(5, biome.EnemiesFor(1, 225), "the biggest chamber on floor 1");
            Assert.AreEqual(5, biome.EnemiesFor(10, 81), "a small room on floor 10");
            Assert.AreEqual(9, biome.EnemiesFor(10, 400), "the size bonus is capped");
            Object.DestroyImmediate(biome);
        }

        [Test]
        public void Print_sample_rooms()
        {
            Debug.Log("[Shrunk] Small fight room, doors north and west\n" + RoomShape.Generate(11, Doors.North | Doors.West, RoomType.Normal, 0.1f).ToAscii());
            Debug.Log("[Shrunk] Large fight room, all four doors\n" + RoomShape.Generate(12, Doors.North | Doors.East | Doors.South | Doors.West, RoomType.Normal, 1f).ToAscii());
            Debug.Log("[Shrunk] Large fight room, doors east and south\n" + RoomShape.Generate(13, Doors.East | Doors.South, RoomType.Normal, 0.9f).ToAscii());
            Debug.Log("[Shrunk] Treasure room, one door\n" + RoomShape.Generate(14, Doors.South, RoomType.Treasure, 0f).ToAscii());
        }
    }
}
