using UnityEditor;
using UnityEngine;

namespace Shrunk.EditorTools
{
    /// <summary>
    /// The flat debug view: floors drawn as coloured squares, no 3D. Menu: Shrunk > Floor Layout Debug View.
    /// Layout bugs are obvious here and painful to spot once rooms are built.
    /// </summary>
    public class FloorLayoutWindow : EditorWindow
    {
        int _seed = 1;
        int _floor = 1;
        FloorLayout _layout;

        [MenuItem("Shrunk/Floor Layout Debug View")]
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

            if (_layout == null || changed) _layout = FloorLayout.Generate(_seed, FloorLayout.RoomCountForFloor(_floor));

            var problems = _layout.Validate();
            if (problems.Count == 0)
            {
                EditorGUILayout.HelpBox($"{_layout.rooms.Count} rooms, exit {_layout.exit.distance} rooms from the start. No problems.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(string.Join("\n", problems), MessageType.Error);
            }

            EditorGUILayout.LabelField("Start = cyan   Exit = green   Treasure = gold   Shop = blue   Number = rooms from the start", EditorStyles.miniLabel);
            Draw(GUILayoutUtility.GetRect(100f, 4000f, 100f, 4000f));
        }

        void Draw(Rect area)
        {
            EditorGUI.DrawRect(area, new Color(0.12f, 0.12f, 0.12f));

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var cell in _layout.rooms.Keys)
            {
                minX = Mathf.Min(minX, cell.x); maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y); maxY = Mathf.Max(maxY, cell.y);
            }

            int columns = maxX - minX + 1, rows = maxY - minY + 1;
            float size = Mathf.Floor(Mathf.Min(area.width / columns, area.height / rows, 64f));
            float originX = area.x + (area.width - columns * size) * 0.5f;
            float originY = area.y + (area.height - rows * size) * 0.5f;
            float gap = Mathf.Max(2f, size * 0.12f);

            var label = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = Color.black;

            foreach (var node in _layout.ordered)
            {
                float x = originX + (node.cell.x - minX) * size;
                float y = originY + (maxY - node.cell.y) * size; // grid Y goes up, screen Y goes down
                var square = new Rect(x + gap, y + gap, size - gap * 2f, size - gap * 2f);
                var color = ColorFor(node.type);
                EditorGUI.DrawRect(square, color);

                // Doors: a bridge across the gap to each neighbour.
                float door = size * 0.3f;
                if ((node.doors & Doors.East) != 0) EditorGUI.DrawRect(new Rect(square.xMax, square.center.y - door * 0.5f, gap * 2f, door), color);
                if ((node.doors & Doors.South) != 0) EditorGUI.DrawRect(new Rect(square.center.x - door * 0.5f, square.yMax, door, gap * 2f), color);

                if (size >= 24f) GUI.Label(square, node.distance.ToString(), label);
            }
        }
    }
}
