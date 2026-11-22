using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>
    /// The floor as small squares. Rooms you have been in are bright, rooms next to them are dim, the rest stay hidden.
    /// Special rooms keep their colour so you can plan a route. A small pointer shows where in the room you are and
    /// which way you are looking: with the camera behind the character there is no "up is north" to go by.
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
        public Color dareColor = new Color(1f, 0.4f, 0.3f);
        public Color lockedColor = new Color(1f, 0.35f, 0.25f);
        public Color pointerColor = new Color(0.05f, 0.05f, 0.08f);

        /// <summary>The "you are here" pointer. Its position is where you are; its up is where you are looking.</summary>
        public RectTransform Pointer { get; private set; }
        float _pitch;
        Vector2 _centre;

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
            if (Pointer != null) Destroy(Pointer.gameObject);

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

            _pitch = pitch;
            _centre = centre;
            Pointer = Mark("You", container, new Vector2(7f, 7f), Vector2.zero);
            Mark("Nose", Pointer, new Vector2(3f, 8f), new Vector2(0f, 6f));
        }

        RectTransform Mark(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = go.GetComponent<Image>();
            image.color = pointerColor;
            image.raycastTarget = false;
            return rect;
        }

        void LateUpdate()
        {
            var floors = FloorManager.Instance;
            var player = PlayerController.Instance;
            if (Pointer == null || floors == null || player == null) return;

            // The map is the floor seen from above with north up, one square per room.
            Vector3 position = player.transform.position;
            Vector2 onGrid = new Vector2(position.x, position.z) / floors.cellSize;
            Pointer.anchoredPosition = (onGrid - _centre) * _pitch;
            Pointer.SetAsLastSibling();

            var follow = CameraFollow.Instance;
            float yaw = follow != null && follow.IsLookMode ? follow.LookYaw : player.transform.eulerAngles.y;
            Pointer.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }

        void Refresh(FloorManager floors)
        {
            foreach (var node in floors.Layout.ordered)
            {
                if (!_tiles.TryGetValue(node.cell, out var tile)) continue;

                bool discovered = node.visited;
                // The machine's room is not on the map until somebody walks into it.
                if (!discovered && node.type != RoomType.Machine && node.type != RoomType.Secret)
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
                // The start room is not marked: the player's own marker says where they are, and it is only a room.
                case RoomType.Start: return normalColor;
                case RoomType.Exit: return exitColor;
                case RoomType.Arena: return exitColor;
                case RoomType.Treasure: return treasureColor;
                case RoomType.Shop: return shopColor;
                case RoomType.Dare: return dareColor;
                case RoomType.Machine: return new Color(0.45f, 1f, 0.6f);
                case RoomType.Secret: return new Color(0.85f, 0.75f, 0.55f);
                case RoomType.Vault: return new Color(0.75f, 0.55f, 1f);
                default: return normalColor;
            }
        }
    }
}
