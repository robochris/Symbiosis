using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The flat debug view: a whole floor drawn in 2D, every room with its real carved shape, no 3D.
    /// Menu: Shrunk > Floor Layout Debug View. Layout bugs are obvious here and painful to spot once rooms are built.
    /// </summary>
    public class FloorLayoutWindow : EditorWindow
    {
        int _seed = 1;
        int _floor = 1;
        FloorLayout _layout;
        readonly Dictionary<Vector2Int, RoomShape> _shapes = new Dictionary<Vector2Int, RoomShape>();
        readonly List<string> _problems = new List<string>();

        [MenuItem("Shrunk/Floor Layout Debug View", priority = 40)]
        static void Open()
        {
            GetWindow<FloorLayoutWindow>("Floor Layout");
        }

        public static Color ColorFor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return new Color(0.45f, 0.95f, 1f);
                case RoomType.Exit: return new Color(0.35f, 0.9f, 0.4f);
                case RoomType.Treasure: return new Color(1f, 0.82f, 0.2f);
                case RoomType.Shop: return new Color(0.45f, 0.55f, 1f);
                default: return new Color(0.75f, 0.75f, 0.75f);
            }
        }

        void Regenerate()
        {
            _layout = FloorLayout.Generate(_seed, FloorLayout.RoomCountForFloor(_floor));
            _shapes.Clear();
            _problems.Clear();
            _problems.AddRange(_layout.Validate());
            foreach (var node in _layout.ordered)
            {
                var shape = RoomShape.GenerateFor(_layout, node, _floor);
                _shapes[node.cell] = shape;
                foreach (var problem in shape.Validate()) _problems.Add($"room {node.cell}: {problem}");
            }
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _floor = EditorGUILayout.IntSlider("Floor", _floor, 1, 10);
            bool changed = EditorGUI.EndChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Previous seed")) { _seed--; changed = true; }
                if (GUILayout.Button("Next seed")) { _seed++; changed = true; }
                if (GUILayout.Button("Random seed")) { _seed = Random.Range(1, int.MaxValue); changed = true; }
            }

            if (_layout == null || changed) Regenerate();

            if (_problems.Count == 0)
            {
                EditorGUILayout.HelpBox($"{_layout.rooms.Count} rooms, exit {_layout.exit.distance} rooms from the start. No problems.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(string.Join("\n", _problems), MessageType.Error);
            }

            EditorGUILayout.LabelField("Start = cyan   Exit = green   Treasure = gold   Shop = blue   Dark = solid earth   White dot = room centre", EditorStyles.miniLabel);
            Draw(GUILayoutUtility.GetRect(100f, 4000f, 100f, 4000f));
        }

        void Draw(Rect area)
        {
            EditorGUI.DrawRect(area, new Color(0.08f, 0.08f, 0.08f));

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var cell in _layout.rooms.Keys)
            {
                minX = Mathf.Min(minX, cell.x); maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y); maxY = Mathf.Max(maxY, cell.y);
            }

            int columns = maxX - minX + 1, rows = maxY - minY + 1;
            float tile = Mathf.Floor(Mathf.Min(area.width / (columns * RoomShape.Size), area.height / (rows * RoomShape.Size), 12f));
            tile = Mathf.Max(tile, 1f);
            float room = tile * RoomShape.Size;
            float originX = area.x + (area.width - columns * room) * 0.5f;
            float originY = area.y + (area.height - rows * room) * 0.5f;
            var earth = new Color(0.2f, 0.15f, 0.11f);

            foreach (var node in _layout.ordered)
            {
                float x = originX + (node.cell.x - minX) * room;
                float y = originY + (maxY - node.cell.y) * room; // grid Y goes up, screen Y goes down
                EditorGUI.DrawRect(new Rect(x, y, room, room), earth);

                var shape = _shapes[node.cell];
                var color = ColorFor(node.type);
                for (int tx = 0; tx < RoomShape.Size; tx++)
                {
                    for (int ty = 0; ty < RoomShape.Size; ty++)
                    {
                        if (!shape.open[tx, ty]) continue;
                        EditorGUI.DrawRect(new Rect(x + tx * tile, y + (RoomShape.Size - 1 - ty) * tile, tile, tile), color);
                    }
                }

                EditorGUI.DrawRect(new Rect(x + shape.centre.x * tile + tile * 0.25f, y + (RoomShape.Size - 1 - shape.centre.y) * tile + tile * 0.25f, tile * 0.5f, tile * 0.5f), Color.white);
            }
        }
    }
}
