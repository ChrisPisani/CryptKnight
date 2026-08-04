using CryptKnight.Application;
using CryptKnight.Content;
using CryptKnight.Data;
using CryptKnight.Dungeon;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.UI
{
    public sealed class DungeonMapController : MonoBehaviour
    {
        private const string ItemSheetPath = "Art/Items/items_sheet_no_numbers_alpha";
        private const string SpikePath = "Art/Traps/spike_trap";
        private const string ChestPath = "Art/Items/treasure_chest_opening_strip_alpha";
        private static readonly Color PanelColor = new Color(0.025f, 0.02f, 0.03f, 0.68f);
        private static readonly Color UnknownColor = new Color(0.12f, 0.11f, 0.14f, 0.76f);
        private static readonly Color VisitedColor = new Color(0.20f, 0.18f, 0.20f, 0.78f);
        private static readonly Color IvoryColor = new Color(0.86f, 0.82f, 0.70f, 0.86f);
        private static readonly Color CurrentColor = new Color(1f, 0.72f, 0.12f, 0.98f);
        private static readonly Color CombatColor = new Color(0.76f, 0.13f, 0.12f, 0.92f);
        private static readonly Color TrapColor = new Color(0.95f, 0.57f, 0.12f, 0.92f);
        private static readonly Color BossColor = new Color(0.82f, 0.12f, 0.16f, 0.94f);

        private Font mapFont;
        private GameObject minimapRoot;
        private GameObject expandedMapRoot;
        private RectTransform minimapContent;
        private RectTransform expandedMapContent;
        private DungeonRunState boundDungeon;
        private Sprite combatSprite;
        private Sprite bossSprite;
        private Sprite trapSprite;
        private Sprite chestSprite;

        public bool IsExpanded { get; private set; }

        public void Initialize(Transform parent, Font font)
        {
            mapFont = font;
            LoadSprites();
            BuildMaps(parent);

            GameManager.Instance.RunStateChanged += HandleRunStateChanged;
            HandleRunStateChanged(GameManager.Instance.CurrentRun);
        }

        private void OnDestroy()
        {
            BindDungeon(null);
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RunStateChanged -= HandleRunStateChanged;
            }
        }

        private void Update()
        {
            if (CanToggleMap() && IsTogglePressed())
            {
                ToggleMap();
            }
        }

        public void ToggleMap()
        {
            if (boundDungeon == null)
            {
                return;
            }

            IsExpanded = !IsExpanded;
            RefreshVisibility();
        }

        private bool CanToggleMap()
        {
            GameRunState run = GameManager.Instance.CurrentRun;
            return run != null
                && run.IsActive
                && boundDungeon != null
                && !GameManager.Instance.IsGameplayPaused
                && !GameplayInputGate.IsBlocked;
        }

        private void HandleRunStateChanged(GameRunState run)
        {
            DungeonRunState nextDungeon = run != null && run.IsActive ? run.Dungeon : null;
            if (nextDungeon != boundDungeon)
            {
                IsExpanded = false;
                BindDungeon(nextDungeon);
            }

            RefreshVisibility();
            RefreshMaps();
        }

        private void BindDungeon(DungeonRunState dungeon)
        {
            if (boundDungeon != null)
            {
                boundDungeon.MapChanged -= RefreshMaps;
            }

            boundDungeon = dungeon;
            if (boundDungeon != null)
            {
                boundDungeon.MapChanged += RefreshMaps;
            }
        }

        private void RefreshVisibility()
        {
            bool hasDungeon = boundDungeon != null;
            minimapRoot.SetActive(hasDungeon && !IsExpanded);
            expandedMapRoot.SetActive(hasDungeon && IsExpanded);
        }

        private void RefreshMaps()
        {
            if (boundDungeon == null)
            {
                return;
            }

            DungeonMapSnapshot snapshot = DungeonMapSnapshot.Create(boundDungeon);
            RenderMap(minimapContent, snapshot, 32f, 14f, 5f, false);
            RenderMap(expandedMapContent, snapshot, 76f, 24f, 8f, true);
        }

        private void BuildMaps(Transform parent)
        {
            minimapRoot = CreatePanel(
                parent,
                "Dungeon Minimap",
                new Vector2(1f, 1f),
                new Vector2(-28f, -82f),
                new Vector2(208f, 208f),
                new Vector2(1f, 1f));
            minimapContent = CreateContent(minimapRoot.transform);

            expandedMapRoot = CreatePanel(
                parent,
                "Expanded Dungeon Map",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(440f, 440f),
                new Vector2(0.5f, 0.5f));
            expandedMapContent = CreateContent(expandedMapRoot.transform);
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            Image panel = panelObject.AddComponent<Image>();
            panel.color = PanelColor;
            panel.raycastTarget = false;

            RectTransform rect = panel.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return panelObject;
        }

        private static RectTransform CreateContent(Transform parent)
        {
            GameObject content = new GameObject("Map Content");
            content.transform.SetParent(parent, false);
            RectTransform rect = content.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private void RenderMap(
            RectTransform parent,
            DungeonMapSnapshot snapshot,
            float cellSize,
            float gap,
            float connectionThickness,
            bool showLargeIcons)
        {
            ClearChildren(parent);
            float step = cellSize + gap;
            Vector2 origin = new Vector2(
                -(snapshot.Width - 1) * step * 0.5f,
                -(snapshot.Height - 1) * step * 0.5f);

            for (int i = 0; i < snapshot.Connections.Count; i++)
            {
                DungeonMapConnection connection = snapshot.Connections[i];
                Vector2 from = origin + new Vector2(connection.From.x * step, connection.From.y * step);
                Vector2 to = origin + new Vector2(connection.To.x * step, connection.To.y * step);
                CreateConnection(parent, from, to, connectionThickness);
            }

            for (int i = 0; i < snapshot.Rooms.Count; i++)
            {
                DungeonMapRoomInfo room = snapshot.Rooms[i];
                Vector2 position = origin + new Vector2(room.GridPosition.x * step, room.GridPosition.y * step);
                CreateRoomCell(parent, room, position, cellSize, showLargeIcons);
            }
        }

        private static void CreateConnection(Transform parent, Vector2 from, Vector2 to, float thickness)
        {
            GameObject connectionObject = new GameObject("Room Connection");
            connectionObject.transform.SetParent(parent, false);
            Image connection = connectionObject.AddComponent<Image>();
            connection.color = new Color(IvoryColor.r, IvoryColor.g, IvoryColor.b, 0.44f);
            connection.raycastTarget = false;

            RectTransform rect = connection.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            Vector2 difference = to - from;
            rect.sizeDelta = Mathf.Abs(difference.x) > Mathf.Abs(difference.y)
                ? new Vector2(Mathf.Abs(difference.x), thickness)
                : new Vector2(thickness, Mathf.Abs(difference.y));
        }

        private void CreateRoomCell(
            Transform parent,
            DungeonMapRoomInfo room,
            Vector2 position,
            float cellSize,
            bool showLargeIcons)
        {
            GameObject borderObject = CreateImageObject(parent, "Map Room", position, new Vector2(cellSize, cellSize));
            Image border = borderObject.GetComponent<Image>();
            border.color = room.IsCurrent ? CurrentColor : IvoryColor;

            float borderWidth = room.IsCurrent ? 4f : 2f;
            GameObject innerObject = CreateImageObject(
                borderObject.transform,
                "Room Fill",
                Vector2.zero,
                new Vector2(cellSize - borderWidth * 2f, cellSize - borderWidth * 2f));
            Image inner = innerObject.GetComponent<Image>();
            inner.color = room.IsVisited ? VisitedColor : UnknownColor;

            float iconSize = cellSize * (showLargeIcons ? 0.58f : 0.54f);
            CreateMarker(innerObject.transform, room, iconSize);
            if (room.HasUnopenedChest)
            {
                CreateChestBadge(borderObject.transform, cellSize);
            }
        }

        private void CreateMarker(Transform parent, DungeonMapRoomInfo room, float size)
        {
            if (room.IsStarter || room.Marker == DungeonMapMarker.None)
            {
                return;
            }

            switch (room.Marker)
            {
                case DungeonMapMarker.Unknown:
                    CreateLabel(parent, "Unknown", "?", size * 0.92f, IvoryColor);
                    break;
                case DungeonMapMarker.Boss:
                    CreateSpriteMarker(parent, "Boss", bossSprite, size, BossColor, "B");
                    break;
                case DungeonMapMarker.Trap:
                    CreateSpriteMarker(parent, "Trap", trapSprite, size, TrapColor, "T");
                    break;
                case DungeonMapMarker.Combat:
                    CreateCombatMarker(parent, size, room.IsCleared ? IvoryColor : CombatColor);
                    break;
            }
        }

        private void CreateCombatMarker(Transform parent, float size, Color color)
        {
            if (combatSprite == null)
            {
                CreateLabel(parent, "Combat", "!", size * 0.78f, color);
                return;
            }

            CreateSpriteImage(parent, "Combat", combatSprite, new Vector2(size, size), color, -35f);
        }

        private void CreateSpriteMarker(
            Transform parent,
            string name,
            Sprite sprite,
            float size,
            Color color,
            string fallbackText)
        {
            if (sprite == null)
            {
                CreateLabel(parent, name, fallbackText, size * 0.72f, color);
                return;
            }

            CreateSpriteImage(parent, name, sprite, new Vector2(size, size), color, 0f);
        }

        private void CreateChestBadge(Transform parent, float cellSize)
        {
            float chestWidth = cellSize * 0.68f;
            float chestHeight = chestSprite != null && chestSprite.rect.width > 0f
                ? chestWidth * chestSprite.rect.height / chestSprite.rect.width
                : chestWidth;
            GameObject badge = CreateImageObject(
                parent,
                "Unopened Chest",
                new Vector2(cellSize * 0.19f, cellSize * 0.19f),
                new Vector2(chestWidth, chestHeight));
            Image chest = badge.GetComponent<Image>();
            chest.sprite = chestSprite;
            chest.preserveAspect = chestSprite != null;
            chest.color = chestSprite != null ? Color.white : Color.clear;
            if (chestSprite == null)
            {
                CreateLabel(badge.transform, "Chest Label", "C", chestWidth * 0.68f, CurrentColor);
            }
        }

        private static GameObject CreateImageObject(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return imageObject;
        }

        private static void CreateSpriteImage(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 size,
            Color color,
            float rotation)
        {
            GameObject imageObject = CreateImageObject(parent, name, Vector2.zero, size);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = color;
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void CreateLabel(Transform parent, string name, string value, float fontSize, Color color)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            Text label = labelObject.AddComponent<Text>();
            label.font = mapFont;
            label.text = value;
            label.fontSize = Mathf.RoundToInt(fontSize);
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void LoadSprites()
        {
            combatSprite = RuntimeAssetLoader.LoadSprite(ItemSheetPath, "bloody_knife");
            bossSprite = RuntimeAssetLoader.LoadSprite(ItemSheetPath, "forgotten_skull");
            trapSprite = RuntimeAssetLoader.LoadSprite(SpikePath);
            chestSprite = RuntimeAssetLoader.LoadSprite(ChestPath, "treasure_chest_opening_0");
        }

        private static bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.M);
#endif
        }
    }
}
