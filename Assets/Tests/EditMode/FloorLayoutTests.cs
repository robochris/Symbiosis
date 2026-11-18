using NUnit.Framework;
using UnityEngine;

namespace Shrunk.Tests
{
    /// <summary>Checks the flat layout generator on its own, with no scene and no 3D.</summary>
    public class FloorLayoutTests
    {
        [Test]
        public void Twenty_thousand_floors_obey_every_rule()
        {
            int checkedFloors = 0;
            for (int seed = 1; seed <= 2000; seed++)
            {
                for (int floor = 1; floor <= 10; floor++)
                {
                    var layout = FloorLayout.Generate(seed * 7919 + floor, FloorLayout.RoomCountForFloor(floor));
                    var problems = layout.Validate();
                    if (problems.Count > 0)
                    {
                        Assert.Fail($"seed {layout.seed}, floor {floor}:\n{string.Join("\n", problems)}\n{layout.ToAscii()}");
                    }
                    checkedFloors++;
                }
            }
            Assert.AreEqual(20000, checkedFloors);
        }

        [Test]
        public void Same_seed_gives_the_same_floor()
        {
            var a = FloorLayout.Generate(12345, 12);
            var b = FloorLayout.Generate(12345, 12);
            Assert.AreEqual(a.ToAscii(), b.ToAscii());
            Assert.AreNotEqual(a.ToAscii(), FloorLayout.Generate(12346, 12).ToAscii());
        }

        [Test]
        public void Floors_grow_from_8_rooms_to_15()
        {
            Assert.AreEqual(8, FloorLayout.RoomCountForFloor(1));
            Assert.AreEqual(15, FloorLayout.RoomCountForFloor(9));
            Assert.AreEqual(15, FloorLayout.RoomCountForFloor(10));

            // Extra dead ends for treasure and shop may add a room or two, never more.
            for (int seed = 1; seed <= 500; seed++)
            {
                var layout = FloorLayout.Generate(seed, 8);
                Assert.That(layout.rooms.Count, Is.InRange(8, 10), $"seed {seed}");
            }
        }

        [Test]
        public void Print_sample_floors()
        {
            foreach (int floor in new[] { 1, 5, 9 })
            {
                var layout = FloorLayout.Generate(100 + floor, FloorLayout.RoomCountForFloor(floor));
                Debug.Log($"[Shrunk] Sample floor {floor} (seed {layout.seed}, {layout.rooms.Count} rooms)\n{layout.ToAscii()}");
            }
        }
    }
}
