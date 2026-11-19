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
