using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The floor as small squares. Rooms you have been in are bright, rooms next to them are dim, the rest stay hidden.
    /// Special rooms keep their colour so you can plan a route.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        [Tooltip("The squares are created inside this rect and scaled to fit it.")]
        public RectTransform container;
        public float maxTileSize = 30f;
        public float gap = 4f;

        public Color startColor = new Color(0.45f, 0.95f, 1f);
        public Color normalColor = new Color(0.8f, 0.8f, 0.8f);
        public Color exitColor = new Color(0.35f, 0.9f, 0.4f);
        public Color treasureColor = new Color(1f, 0.82f, 0.2f);
        public Color shopColor = new Color(0.45f, 0.55f, 1f);
        public Color lockedColor = new Color(1f, 0.35f, 0.25f);

        readonly Dictionary<Vector2Int, Image> _tiles = new Dictionary<Vector2Int, Image>();

        public int VisibleTiles
        {
            get
            {
                int count = 0;
                foreach (var tile in _tiles.Values) if (tile.enabled) count++;
                return count;
            }
        }

        void OnEnable()
        {
            FloorManager.FloorBuilt += Rebuild;
            FloorManager.RoomChanged += Refresh;
        }

        void OnDisable()
        {
            FloorManager.FloorBuilt -= Rebuild;
            FloorManager.RoomChanged -= Refresh;
        }

        void Rebuild(FloorManager floors)
        {
            foreach (var tile in _tiles.Values) Destroy(tile.gameObject);
            _tiles.Clear();

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var cell in floors.Layout.rooms.Keys)
            {
                minX = Mathf.Min(minX, cell.x); maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y); maxY = Mathf.Max(maxY, cell.y);
            }

            int columns = maxX - minX + 1, rows = maxY - minY + 1;
            float pitch = Mathf.Min(maxTileSize + gap, container.rect.width / columns, container.rect.height / rows);
            var centre = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            foreach (var node in floors.Layout.ordered)
            {
                var go = new GameObject("Tile " + node.cell.x + "," + node.cell.y, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(container, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = (new Vector2(node.cell.x, node.cell.y) - centre) * pitch;
                rect.sizeDelta = Vector2.one * (pitch - gap);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                _tiles.Add(node.cell, image);
            }
        }

        void Refresh(FloorManager floors)
        {
            foreach (var node in floors.Layout.ordered)
            {
                if (!_tiles.TryGetValue(node.cell, out var tile)) continue;

                bool discovered = node.visited;
                if (!discovered)
                {
                    foreach (var neighbour in floors.Layout.Neighbours(node))
                    {
                        if (neighbour.visited) { discovered = true; break; }
                    }
                }

                tile.enabled = discovered;
                if (!discovered) continue;

                bool current = node == floors.CurrentNode;
                Color color = ColorFor(node.type);
                if (current && floors.IsLocked) color = lockedColor;
                color.a = node.visited ? 1f : 0.35f;
                tile.color = color;
                tile.rectTransform.localScale = Vector3.one * (current ? 1.25f : 1f);
            }
        }

        Color ColorFor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return startColor;
                case RoomType.Exit: return exitColor;
                case RoomType.Treasure: return treasureColor;
                case RoomType.Shop: return shopColor;
                default: return normalColor;
            }
        }
    }
}
