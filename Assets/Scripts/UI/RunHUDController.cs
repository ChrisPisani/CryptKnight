using System.Collections.Generic;
using CryptKnight.Application;
using CryptKnight.Data;
using CryptKnight.Loot;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CryptKnight.UI
{
    public sealed class RunHUDController : MonoBehaviour
    {
        private static readonly Color HudTextColor = new Color(0.96f, 0.93f, 0.84f, 1f);
        private static readonly Color ItemPanelColor = new Color(0.035f, 0.036f, 0.045f, 0.88f);
        private static readonly Color QuantityBadgeColor = new Color(0.02f, 0.022f, 0.028f, 0.86f);

        private Font defaultFont;
        private GameObject hudRoot;
        private Transform heartsRoot;
        private Text keyCountText;
        private Transform itemRoot;
        private string lastItemSignature = string.Empty;

        private Sprite fullHeartSprite;
        private Sprite halfHeartSprite;
        private Sprite emptyHeartSprite;
        private Sprite keySprite;

        private readonly List<HeartView> heartViews = new List<HeartView>();

        public void Initialize(Transform parent, Font font)
        {
            defaultFont = font;
            LoadHudSprites();
            BuildHud(parent);

            GameManager.Instance.RunStateChanged += HandleRunStateChanged;
            HandleRunStateChanged(GameManager.Instance.CurrentRun);
        }

        private void OnDestroy()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RunStateChanged -= HandleRunStateChanged;
            }
        }

        private void Update()
        {
            GameRunState currentRun = GameManager.Instance.CurrentRun;
            if (currentRun != null && currentRun.IsActive)
            {
                Refresh(currentRun);
            }
        }

        private void BuildHud(Transform parent)
        {
            hudRoot = new GameObject("Run HUD");
            hudRoot.transform.SetParent(parent, false);

            RectTransform hudRect = hudRoot.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            GameObject topLeft = CreateAnchoredGroup(hudRoot.transform, "Top Left HUD", new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(420f, 160f), new Vector2(0f, 1f));
            heartsRoot = CreateAnchoredGroup(topLeft.transform, "Hearts", new Vector2(0f, 1f), Vector2.zero, new Vector2(360f, 56f), new Vector2(0f, 1f)).transform;
            CreateKeyDisplay(topLeft.transform);

            GameObject bottomLeft = CreateAnchoredGroup(hudRoot.transform, "Collected Items HUD", new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(560f, 108f), new Vector2(0f, 0f));
            Image itemPanel = bottomLeft.AddComponent<Image>();
            itemPanel.color = ItemPanelColor;
            itemRoot = bottomLeft.transform;
        }

        private void CreateKeyDisplay(Transform parent)
        {
            GameObject keyGroup = CreateAnchoredGroup(parent, "Key Count", new Vector2(0f, 1f), new Vector2(0f, -66f), new Vector2(180f, 56f), new Vector2(0f, 1f));

            GameObject iconObject = new GameObject("Key Icon");
            iconObject.transform.SetParent(keyGroup.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = keySprite;
            icon.preserveAspect = true;
            icon.color = Color.white;

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(86f, 44f);

            keyCountText = CreateText(keyGroup.transform, "Key Quantity", "x0", 24, FontStyle.Bold, TextAnchor.MiddleLeft, HudTextColor, new Vector2(96f, 0f), new Vector2(80f, 40f));
        }

        private void HandleRunStateChanged(GameRunState runState)
        {
            bool hasActiveRun = runState != null && runState.IsActive;
            hudRoot.SetActive(hasActiveRun);

            if (hasActiveRun)
            {
                lastItemSignature = string.Empty;
                Refresh(runState);
            }
        }

        private void Refresh(GameRunState runState)
        {
            RefreshHearts(runState.CurrentHealth, runState.MaxHealth);
            keyCountText.text = $"x{runState.KeyCount}";
            RefreshItems(runState.CollectedItems);
        }

        private void RefreshHearts(int currentHealth, int maxHealth)
        {
            int heartCount = Mathf.CeilToInt(maxHealth / 2f);

            while (heartViews.Count < heartCount)
            {
                heartViews.Add(CreateHeartView(heartsRoot, heartViews.Count));
            }

            for (int i = 0; i < heartViews.Count; i++)
            {
                bool isVisible = i < heartCount;
                heartViews[i].Root.SetActive(isVisible);

                if (!isVisible)
                {
                    continue;
                }

                int heartValue = Mathf.Clamp(currentHealth - i * 2, 0, 2);
                heartViews[i].Icon.sprite = heartValue == 2 ? fullHeartSprite : heartValue == 1 ? halfHeartSprite : emptyHeartSprite;
            }
        }

        private void RefreshItems(IReadOnlyList<CollectedItemStack> items)
        {
            // this avoids rebuilding this HUD every frame when item counts are unchanged.
            string itemSignature = CreateItemSignature(items);
            if (itemSignature == lastItemSignature)
            {
                return;
            }

            lastItemSignature = itemSignature;

            for (int i = itemRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(itemRoot.GetChild(i).gameObject);
            }

            if (items.Count == 0)
            {
                CreateText(itemRoot, "No Items", "No items collected", 18, FontStyle.Normal, TextAnchor.MiddleLeft, HudTextColor, new Vector2(18f, 0f), new Vector2(280f, 54f));
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                CreateItemStack(items[i], i);
            }
        }

        private void CreateItemStack(CollectedItemStack itemStack, int index)
        {
            float x = 22f + index * 68f;

            GameObject stackObject = CreateAnchoredGroup(itemRoot, $"Item {itemStack.ItemId}", new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(62f, 62f), new Vector2(0f, 0.5f));

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(stackObject.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = LootItemVisuals.GetItemSprite(itemStack.ItemId);
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.raycastTarget = false;

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(52f, 52f);

            CreateQuantityBadge(stackObject.transform, itemStack.Quantity, new Vector2(40f, -20f));
        }

        private HeartView CreateHeartView(Transform parent, int index)
        {
            GameObject root = CreateAnchoredGroup(parent, $"Heart {index + 1}", new Vector2(0f, 1f), new Vector2(index * 62f, 0f), new Vector2(56f, 52f), new Vector2(0f, 1f));
            Image icon = root.AddComponent<Image>();
            icon.sprite = fullHeartSprite;
            icon.preserveAspect = true;
            icon.color = Color.white;

            return new HeartView(root, icon);
        }

        private void LoadHudSprites()
        {
            fullHeartSprite = LoadSpriteAtPath("Assets/Art/UI/heartDisplay.png", "heartDisplay_0");
            emptyHeartSprite = LoadSpriteAtPath("Assets/Art/UI/heartDisplay.png", "heartDisplay_1");
            halfHeartSprite = LoadSpriteAtPath("Assets/Art/UI/heartDisplay.png", "heartDisplay_2");
            keySprite = LoadSpriteAtPath("Assets/Art/Items/key.png", "key_0");
            if (keySprite == null)
            {
                keySprite = LoadSpriteAtPath("Assets/Art/Items/key.png", "key");
            }

            if (fullHeartSprite == null)
            {
                fullHeartSprite = CreateFallbackSprite(new Color(0.82f, 0.05f, 0.08f, 1f));
            }

            if (halfHeartSprite == null)
            {
                halfHeartSprite = CreateFallbackSprite(new Color(0.82f, 0.18f, 0.08f, 1f));
            }

            if (emptyHeartSprite == null)
            {
                emptyHeartSprite = CreateFallbackSprite(new Color(0.20f, 0.18f, 0.18f, 0.92f));
            }

            if (keySprite == null)
            {
                keySprite = CreateFallbackSprite(new Color(0.95f, 0.74f, 0.18f, 1f));
            }
        }

        private static Sprite LoadSpriteAtPath(string assetPath, string spriteName)
        {
#if UNITY_EDITOR
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }

        private static Sprite CreateFallbackSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        private static string CreateItemSignature(IReadOnlyList<CollectedItemStack> items)
        {
            if (items.Count == 0)
            {
                return "empty";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                parts.Add($"{items[i].ItemId}:{items[i].Quantity}");
            }

            return string.Join("|", parts);
        }

        private static GameObject CreateAnchoredGroup(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            GameObject group = new GameObject(objectName);
            group.transform.SetParent(parent, false);

            RectTransform rectTransform = group.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            return group;
        }

        private Text CreateText(Transform parent, string objectName, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 position, Vector2 size)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = defaultFont;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform rectTransform = textComponent.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            return textComponent;
        }

        private void CreateQuantityBadge(Transform parent, int quantity, Vector2 position)
        {
            GameObject badgeObject = new GameObject("Quantity Badge");
            badgeObject.transform.SetParent(parent, false);

            Image badge = badgeObject.AddComponent<Image>();
            badge.color = QuantityBadgeColor;
            badge.raycastTarget = false;

            RectTransform badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = position;
            badgeRect.sizeDelta = new Vector2(32f, 22f);

            GameObject textObject = new GameObject("Quantity");
            textObject.transform.SetParent(badgeObject.transform, false);

            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = $"x{quantity}";
            textComponent.font = defaultFont;
            textComponent.fontSize = 13;
            textComponent.fontStyle = FontStyle.Bold;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = HudTextColor;
            textComponent.raycastTarget = false;

            RectTransform textRect = textComponent.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private sealed class HeartView
        {
            public HeartView(GameObject root, Image icon)
            {
                Root = root;
                Icon = icon;
            }

            public GameObject Root { get; }
            public Image Icon { get; }
        }
    }
}
