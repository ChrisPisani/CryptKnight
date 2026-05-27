using System.Collections.Generic;
using System.Linq;
using CryptKnight.Application;
using CryptKnight.Data;
using CryptKnight.Loot;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.UI
{
    public sealed class RunPauseMenuController : MonoBehaviour
    {
        private static readonly Color OverlayColor = new Color(0.01f, 0.012f, 0.016f, 0.82f);
        private static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.13f, 0.97f);
        private static readonly Color ItemSlotColor = new Color(0.16f, 0.16f, 0.20f, 0.92f);
        private static readonly Color QuantityBadgeColor = new Color(0.02f, 0.022f, 0.028f, 0.86f);
        private static readonly Color TextColor = new Color(0.95f, 0.92f, 0.84f, 1f);
        private static readonly Color MutedTextColor = new Color(0.72f, 0.70f, 0.64f, 1f);
        private static readonly Color ButtonColor = new Color(0.65f, 0.12f, 0.12f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.82f, 0.18f, 0.16f, 1f);

        private Font defaultFont;
        private GameObject pauseRoot;
        private Transform itemListRoot;
        private GameObject tooltipRoot;
        private Text tooltipTitle;
        private Text tooltipBody;
        private Text statsText;
        private LootTableConfiguration lootTable;
        private bool isPaused;
        private string lastItemSignature = string.Empty;

        public bool IsPaused => isPaused;

        public void Initialize(Transform parent, Font font)
        {
            defaultFont = font;
            lootTable = LootTableConfiguration.CreateDefault();
            BuildPauseMenu(parent);

            GameManager.Instance.RunStateChanged += HandleRunStateChanged;
            HandleRunStateChanged(GameManager.Instance.CurrentRun);
        }

        private void OnDestroy()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RunStateChanged -= HandleRunStateChanged;
            }

            ResumeGame();
        }

        private void Update()
        {
            GameRunState currentRun = GameManager.Instance.CurrentRun;
            if (currentRun == null || !currentRun.IsActive)
            {
                return;
            }

            if (IsPausePressed())
            {
                SetPaused(!isPaused);
            }
        }

        public void ShowItemTooltip(LootItemDefinition itemDefinition, int quantity)
        {
            if (tooltipRoot == null)
            {
                return;
            }

            tooltipTitle.text = itemDefinition != null ? itemDefinition.DisplayName : "Unknown Item";
            tooltipBody.text = itemDefinition != null
                ? $"{itemDefinition.Description}\n\n{LootItemEffectFormatter.FormatEffects(itemDefinition, quantity)}"
                : "Item details are missing.";
            tooltipRoot.SetActive(true);
        }

        public void HideItemTooltip()
        {
            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(false);
            }
        }

        private void HandleRunStateChanged(GameRunState runState)
        {
            if (runState == null || !runState.IsActive)
            {
                SetPaused(false);
                return;
            }

            RefreshItems(runState.CollectedItems);
            RefreshStats(runState);
        }

        private void BuildPauseMenu(Transform parent)
        {
            pauseRoot = new GameObject("Pause Menu");
            pauseRoot.transform.SetParent(parent, false);

            RectTransform rootRect = pauseRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image overlay = pauseRoot.AddComponent<Image>();
            overlay.color = OverlayColor;

            GameObject panel = CreatePanel(pauseRoot.transform, "Pause Panel", new Vector2(980f, 680f), PanelColor);
            CreateText(panel.transform, "Title", "PAUSED", 46, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 280f), new Vector2(880f, 60f));
            CreateButton(panel.transform, "Quit Run Button", "QUIT RUN", new Vector2(0f, -280f), QuitRun);

            CreateText(panel.transform, "Items Heading", "Collected Items", 24, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, new Vector2(-210f, 206f), new Vector2(500f, 40f));
            GameObject itemPanel = CreatePanel(panel.transform, "Collected Items Panel", new Vector2(500f, 320f), new Color(0.06f, 0.06f, 0.075f, 0.92f));
            itemPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-210f, 20f);
            itemListRoot = itemPanel.transform;

            CreateText(panel.transform, "Stats Heading", "Current Stats", 24, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, new Vector2(270f, 206f), new Vector2(360f, 40f));
            GameObject statsPanel = CreatePanel(panel.transform, "Stats Panel", new Vector2(360f, 190f), new Color(0.075f, 0.075f, 0.095f, 0.96f));
            statsPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(270f, 82f);
            statsText = CreateText(statsPanel.transform, "Stats Text", string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft, TextColor, new Vector2(0f, -14f), new Vector2(300f, 140f));
            statsText.verticalOverflow = VerticalWrapMode.Overflow;

            tooltipRoot = CreatePanel(panel.transform, "Item Tooltip", new Vector2(360f, 210f), new Color(0.075f, 0.075f, 0.095f, 0.96f));
            tooltipRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(270f, -128f);
            tooltipTitle = CreateText(tooltipRoot.transform, "Tooltip Title", string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, new Vector2(0f, 88f), new Vector2(300f, 42f));
            tooltipBody = CreateText(tooltipRoot.transform, "Tooltip Body", string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(0f, -28f), new Vector2(300f, 140f));
            tooltipBody.verticalOverflow = VerticalWrapMode.Overflow;
            tooltipRoot.SetActive(false);

            pauseRoot.SetActive(false);
        }

        private void RefreshStats(GameRunState runState)
        {
            if (statsText != null)
            {
                statsText.text = PlayerStatSummaryFormatter.FormatStatsOnly(runState);
            }
        }

        private void RefreshItems(IReadOnlyList<CollectedItemStack> items)
        {
            string itemSignature = CreateItemSignature(items);
            if (itemSignature == lastItemSignature)
            {
                return;
            }

            lastItemSignature = itemSignature;
            HideItemTooltip();

            for (int i = itemListRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(itemListRoot.GetChild(i).gameObject);
            }

            if (items.Count == 0)
            {
                CreateText(itemListRoot, "No Items", "No items collected", 20, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, Vector2.zero, new Vector2(420f, 56f));
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                CreateItemSlot(items[i], i);
            }
        }

        private void CreateItemSlot(CollectedItemStack itemStack, int index)
        {
            LootItemDefinition itemDefinition = FindItemDefinition(itemStack.ItemId);
            const int columns = 6;
            const float slotSpacing = 76f;
            int rowIndex = index / columns;
            int columnIndex = index % columns;
            Vector2 slotPosition = new Vector2(-190f + columnIndex * slotSpacing, 104f - rowIndex * slotSpacing);

            GameObject slot = CreatePanel(itemListRoot, $"Item {itemStack.ItemId}", new Vector2(64f, 64f), ItemSlotColor);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchoredPosition = slotPosition;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slot.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = LootItemVisuals.GetItemSprite(itemStack.ItemId);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(48f, 48f);

            CreateQuantityBadge(slot.transform, itemStack.Quantity, new Vector2(20f, -20f));

            // Names stay out of the grid so future item pools can grow without turning into list
            slot.AddComponent<RunPauseMenuItemHover>().Initialize(this, itemDefinition, itemStack.Quantity);
        }

        private LootItemDefinition FindItemDefinition(string itemId)
        {
            return lootTable.Items.FirstOrDefault(item => item.ItemId == itemId);
        }

        private void SetPaused(bool shouldPause)
        {
            isPaused = shouldPause;
            if (pauseRoot != null)
            {
                pauseRoot.SetActive(isPaused);
            }

            Time.timeScale = isPaused ? 0f : 1f;
            if (!isPaused)
            {
                HideItemTooltip();
            }
        }

        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
        }

        private void QuitRun()
        {
            SetPaused(false);
            GameManager.Instance.QuitCurrentRun();
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

        private GameObject CreatePanel(Transform parent, string objectName, Vector2 size, Color color)
        {
            GameObject panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(parent, false);

            Image panel = panelObject.AddComponent<Image>();
            panel.color = color;

            RectTransform rectTransform = panel.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;

            return panelObject;
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
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            return textComponent;
        }

        private void CreateQuantityBadge(Transform parent, int quantity, Vector2 position)
        {
            GameObject badgeObject = CreatePanel(parent, "Quantity Badge", new Vector2(32f, 22f), QuantityBadgeColor);
            badgeObject.GetComponent<Image>().raycastTarget = false;
            badgeObject.GetComponent<RectTransform>().anchoredPosition = position;
            Text badgeText = CreateText(badgeObject.transform, "Quantity", $"x{quantity}", 13, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, Vector2.zero, new Vector2(30f, 20f));
            badgeText.raycastTarget = false;
        }

        private void CreateButton(Transform parent, string objectName, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            button.colors = new ColorBlock
            {
                normalColor = ButtonColor,
                highlightedColor = ButtonHoverColor,
                pressedColor = new Color(0.42f, 0.08f, 0.08f, 1f),
                selectedColor = ButtonHoverColor,
                disabledColor = new Color(0.24f, 0.24f, 0.24f, 1f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(220f, 56f);

            Text buttonText = CreateText(buttonObject.transform, "Label", label, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, Vector2.zero, new Vector2(200f, 44f));
            buttonText.raycastTarget = false;
        }

        private static bool IsPausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
