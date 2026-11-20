using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The machine room: where the lab's panel leads once all four parts are back in. One quiet floor, like the
    /// bottom, and the machine at the end of it.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string MachineRoomPath = "Assets/Data/TheMachineRoom.asset";

        public static BiomeDefinition BuildMachineRoom(BiomeDefinition bottom, BiomeDefinition[] worlds)
        {
            var metal = ArenaSceneBuilder.Lit("MachineMetal", new Color(0.42f, 0.45f, 0.5f), 0.8f);
            var dark = ArenaSceneBuilder.Lit("MachineDark", new Color(0.1f, 0.11f, 0.14f), 0.5f);
            var glow = ArenaSceneBuilder.Unlit("MachineGlow", new Color(0.45f, 0.95f, 1f));
            var hot = ArenaSceneBuilder.Unlit("MachineShot", new Color(1f, 0.5f, 0.9f));
            var telegraph = ArenaSceneBuilder.Unlit("Telegraph", new Color(1f, 0.25f, 0.2f));

            var shot = new GameObject("MachineShot");
            Decor(PrimitiveType.Sphere, "Visual", shot.transform, Vector3.zero, Vector3.one * 0.42f, hot);
            shot.AddComponent<Bullet>().radius = 0.24f;
            var shotPrefab = ArenaSceneBuilder.SavePrefab(shot, "Enemies/MachineShot").GetComponent<Bullet>();

            // ----- the machine: a ring on a plinth, with the way to everywhere in the middle of it -----
            var machine = Creature<MachineBoss>("TheMachine", 2.4f, 5f, 600, 1f, 7.4f, out var model);
            Decor(PrimitiveType.Cylinder, "Plinth", model, new Vector3(0f, 0.35f, 0f), new Vector3(5.4f, 0.35f, 5.4f), dark);
            Decor(PrimitiveType.Cylinder, "Step", model, new Vector3(0f, 0.8f, 0f), new Vector3(4.2f, 0.12f, 4.2f), metal);
            const int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                Vector3 at = Quaternion.Euler(0f, 0f, angle) * Vector3.up * 2.9f + Vector3.up * 3.9f;
                Decor(PrimitiveType.Cube, "Ring " + i, model, at, new Vector3(1.25f, 0.7f, 0.9f), i % 4 == 0 ? dark : metal).transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            Decor(PrimitiveType.Sphere, "Way Through", model, new Vector3(0f, 3.9f, 0f), new Vector3(5f, 5f, 0.35f), glow);
            // The four parts, back where they belong.
            for (int i = 0; i < 4; i++)
            {
                Vector3 at = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward * 2.2f + Vector3.up * 1.1f;
                Decor(PrimitiveType.Cube, "Part " + i, model, at, Vector3.one * 0.5f, hot);
            }

            machine.projectilePrefab = shotPrefab;
            machine.displayName = "The Machine";
            machine.subtitle = "Have you tried turning it off and on again? You are about to.";
            machine.telegraphMaterial = telegraph;
            machine.sampleReward = 80;
            machine.knockbackOnHit = 0f;
            var visitors = new List<Enemy>();
            foreach (var world in worlds)
            {
                if (world == null || world.spawnTable == null) continue;
                for (int i = 0; i < world.spawnTable.Length && i < 2; i++)
                {
                    if (world.spawnTable[i].prefab != null) visitors.Add(world.spawnTable[i].prefab);
                }
            }
            machine.visitors = visitors.ToArray();
            Object.DestroyImmediate(machine.GetComponent<EnemyMutations>());
            var prefab = (BossEnemy)SaveEnemy(machine);

            // ----- its room: the bottom's, relit -----
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(MachineRoomPath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(biome, MachineRoomPath);
            }
            EditorUtility.CopySerialized(bottom, biome);
            biome.name = "TheMachineRoom";
            biome.displayName = "The Machine Room";
            biome.id = "machine";
            biome.mutationTheme = "all four parts are in";
            biome.isBottom = true;
            biome.ambientColor = new Color(0.7f, 0.85f, 0.95f);
            biome.glowMaterial = glow;
            biome.miniBoss = prefab;
            biome.boss = prefab;

            EditorUtility.SetDirty(biome);
            return biome;
        }
    }
}
