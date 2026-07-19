using System.Collections.Generic;
using System.Linq;
using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Content;
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
        private const string BloodlinesPanelSpritePath = "Art/UI/Bloodlines/Frames/Frame_main_menu_red";
        private const string BloodlinesButtonDefaultPath = "Art/UI/Bloodlines/Buttons/Status_Red_Default";
        private const string BloodlinesButtonHoverPath = "Art/UI/Bloodlines/Buttons/Status_Red_Hover";
        private const string BloodlinesButtonPressedPath = "Art/UI/Bloodlines/Buttons/Status_Pressed";
        private const string BloodlinesButtonDisabledPath = "Art/UI/Bloodlines/Buttons/Status_Disable";
        private const string BloodlinesSliderEmptyPath = "Art/UI/Bloodlines/Sliders/Slider_empty";
        private const string BloodlinesSliderFullPath = "Art/UI/Bloodlines/Sliders/Slider_full_v1";
        private const string BloodlinesSliderHandlePath = "Art/UI/Bloodlines/Sliders/Slider_toggler";

        private static readonly Color OverlayColor = new Color(0.01f, 0.012f, 0.016f, 0.82f);
        private static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.13f, 0.97f);
        private static readonly Color ItemSlotColor = new Color(0.16f, 0.16f, 0.20f, 0.92f);
        private static readonly Color QuantityBadgeColor = new Color(0.02f, 0.022f, 0.028f, 0.86f);
        private static readonly Color TextColor = new Color(0.95f, 0.92f, 0.84f, 1f);
        private static readonly Color MutedTextColor = new Color(0.72f, 0.70f, 0.64f, 1f);
        private static readonly Color ButtonColor = new Color(0.65f, 0.12f, 0.12f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.82f, 0.18f, 0.16f, 1f);
        private static readonly Color GoldColor = new Color(0.86f, 0.61f, 0.24f, 1f);
        private static readonly Color TextOutlineColor = new Color(0.025f, 0.018f, 0.014f, 0.92f);
        private static readonly Color TextShadowColor = new Color(0f, 0f, 0f, 0.78f);

        private Font defaultFont;
        private GameObject pauseRoot;
        private Transform itemListRoot;
        private GameObject settingsRoot;
        private GameObject tooltipRoot;
        private Text tooltipTitle;
        private Text tooltipBody;
        private Text statsText;
        private Text masterVolumeValueText;
        private Text musicVolumeValueText;
        private Text gameSoundsVolumeValueText;
        private Slider masterVolumeSlider;
        private Slider musicVolumeSlider;
        private Slider gameSoundsVolumeSlider;
        private LootTableConfiguration lootTable;
        private bool isPaused;
        private string lastItemSignature = string.Empty;
        private static Sprite bloodlinesPanelSprite;
        private static Sprite bloodlinesButtonDefaultSprite;
        private static Sprite bloodlinesButtonHoverSprite;
        private static Sprite bloodlinesButtonPressedSprite;
        private static Sprite bloodlinesButtonDisabledSprite;
        private static Sprite bloodlinesSliderEmptySprite;
        private static Sprite bloodlinesSliderFullSprite;
        private static Sprite bloodlinesSliderHandleSprite;

        public bool IsPaused => isPaused;

        public void Initialize(Transform parent, Font font)
        {
            defaultFont = font;
            lootTable = LootTableConfiguration.CreateDefault();
            BuildPauseMenu(parent);

            GameAudioSettings.VolumesChanged += HandleAudioSettingsChanged;
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
            GameAudioSettings.VolumesChanged -= HandleAudioSettingsChanged;
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
            CreateButton(panel.transform, "Unpause Button", "UNPAUSE", new Vector2(-250f, -280f), Unpause);
            CreateButton(panel.transform, "Settings Button", "SETTINGS", new Vector2(0f, -280f), ShowSettings);
            CreateButton(panel.transform, "Quit Run Button", "QUIT RUN", new Vector2(250f, -280f), QuitRun);

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
            tooltipTitle = CreateText(tooltipRoot.transform, "Tooltip Title", string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, new Vector2(0f, 78f), new Vector2(300f, 42f));
            tooltipBody = CreateText(tooltipRoot.transform, "Tooltip Body", string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft, MutedTextColor, new Vector2(0f, -28f), new Vector2(300f, 140f));
            tooltipBody.verticalOverflow = VerticalWrapMode.Overflow;
            tooltipRoot.SetActive(false);

            settingsRoot = CreateSettingsPanel(panel.transform);
            settingsRoot.SetActive(false);

            pauseRoot.SetActive(false);
        }

        private GameObject CreateSettingsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "Settings Panel", new Vector2(560f, 420f), new Color(0.055f, 0.052f, 0.065f, 0.99f));
            panel.transform.SetAsLastSibling();

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = GoldColor;
            outline.effectDistance = new Vector2(2f, -2f);

            CreateText(panel.transform, "Settings Title", "SETTINGS", 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 156f), new Vector2(460f, 48f));
            masterVolumeSlider = CreateAudioSliderRow(panel.transform, "MASTER", new Vector2(0f, 82f), GameAudioSettings.MasterVolume, GameAudioSettings.SetMasterVolume, out masterVolumeValueText);
            musicVolumeSlider = CreateAudioSliderRow(panel.transform, "MUSIC", new Vector2(0f, 8f), GameAudioSettings.MusicVolume, GameAudioSettings.SetMusicVolume, out musicVolumeValueText);
            gameSoundsVolumeSlider = CreateAudioSliderRow(panel.transform, "GAME SOUNDS", new Vector2(0f, -66f), GameAudioSettings.GameSoundsVolume, GameAudioSettings.SetGameSoundsVolume, out gameSoundsVolumeValueText);
            RefreshSettingsValues();

            CreateButton(panel.transform, "Close Settings Button", "CLOSE", new Vector2(0f, -160f), HideSettings);
            return panel;
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
            GameManager.Instance.SetGameplayPaused(isPaused);
            if (!isPaused)
            {
                HideItemTooltip();
                HideSettings();
            }
        }

        private void Unpause()
        {
            SetPaused(false);
        }

        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pauseRoot != null)
            {
                pauseRoot.SetActive(false);
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetGameplayPaused(false);
            }
        }

        private void ShowSettings()
        {
            if (settingsRoot != null)
            {
                RefreshSettingsValues();
                settingsRoot.SetActive(true);
                settingsRoot.transform.SetAsLastSibling();
            }
        }

        private void HideSettings()
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(false);
            }
        }

        private void HandleAudioSettingsChanged()
        {
            RefreshSettingsValues();
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

        private static Sprite LoadBloodlinesSprite(string assetPath)
        {
            return RuntimeAssetLoader.LoadSprite(assetPath);
        }

        private static Sprite GetBloodlinesPanelSprite()
        {
            if (bloodlinesPanelSprite == null)
            {
                bloodlinesPanelSprite = LoadBloodlinesSprite(BloodlinesPanelSpritePath);
            }

            return bloodlinesPanelSprite;
        }

        private static Sprite GetBloodlinesButtonDefaultSprite()
        {
            if (bloodlinesButtonDefaultSprite == null)
            {
                bloodlinesButtonDefaultSprite = LoadBloodlinesSprite(BloodlinesButtonDefaultPath);
            }

            return bloodlinesButtonDefaultSprite;
        }

        private static Sprite GetBloodlinesButtonHoverSprite()
        {
            if (bloodlinesButtonHoverSprite == null)
            {
                bloodlinesButtonHoverSprite = LoadBloodlinesSprite(BloodlinesButtonHoverPath);
            }

            return bloodlinesButtonHoverSprite;
        }

        private static Sprite GetBloodlinesButtonPressedSprite()
        {
            if (bloodlinesButtonPressedSprite == null)
            {
                bloodlinesButtonPressedSprite = LoadBloodlinesSprite(BloodlinesButtonPressedPath);
            }

            return bloodlinesButtonPressedSprite;
        }

        private static Sprite GetBloodlinesButtonDisabledSprite()
        {
            if (bloodlinesButtonDisabledSprite == null)
            {
                bloodlinesButtonDisabledSprite = LoadBloodlinesSprite(BloodlinesButtonDisabledPath);
            }

            return bloodlinesButtonDisabledSprite;
        }

        private static Sprite GetBloodlinesSliderEmptySprite()
        {
            if (bloodlinesSliderEmptySprite == null)
            {
                bloodlinesSliderEmptySprite = LoadBloodlinesSprite(BloodlinesSliderEmptyPath);
            }

            return bloodlinesSliderEmptySprite;
        }

        private static Sprite GetBloodlinesSliderFullSprite()
        {
            if (bloodlinesSliderFullSprite == null)
            {
                bloodlinesSliderFullSprite = LoadBloodlinesSprite(BloodlinesSliderFullPath);
            }

            return bloodlinesSliderFullSprite;
        }

        private static Sprite GetBloodlinesSliderHandleSprite()
        {
            if (bloodlinesSliderHandleSprite == null)
            {
                bloodlinesSliderHandleSprite = LoadBloodlinesSprite(BloodlinesSliderHandlePath);
            }

            return bloodlinesSliderHandleSprite;
        }

        private GameObject CreatePanel(Transform parent, string objectName, Vector2 size, Color color)
        {
            GameObject panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(parent, false);

            Image panel = panelObject.AddComponent<Image>();
            StylePanel(panel, size, color);

            RectTransform rectTransform = panel.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;

            return panelObject;
        }

        private static void StylePanel(Image panel, Vector2 size, Color fallbackColor)
        {
            Sprite sprite = size.x >= 140f && size.y >= 70f ? GetBloodlinesPanelSprite() : null;
            if (sprite != null)
            {
                panel.sprite = sprite;
                panel.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                panel.preserveAspect = false;
                panel.color = Color.white;
                return;
            }

            panel.color = fallbackColor;
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
            ApplyDungeonTextStyle(textObject, fontSize);

            RectTransform rectTransform = textComponent.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            return textComponent;
        }

        private static void ApplyDungeonTextStyle(GameObject textObject, int fontSize)
        {
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = TextOutlineColor;
            outline.effectDistance = fontSize >= 30 ? new Vector2(2f, -2f) : new Vector2(1.1f, -1.1f);

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = TextShadowColor;
            shadow.effectDistance = fontSize >= 30 ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
        }

        private void CreateQuantityBadge(Transform parent, int quantity, Vector2 position)
        {
            GameObject badgeObject = CreatePanel(parent, "Quantity Badge", new Vector2(32f, 22f), QuantityBadgeColor);
            badgeObject.GetComponent<Image>().raycastTarget = false;
            badgeObject.GetComponent<RectTransform>().anchoredPosition = position;
            Text badgeText = CreateText(badgeObject.transform, "Quantity", $"x{quantity}", 13, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, Vector2.zero, new Vector2(30f, 20f));
            badgeText.raycastTarget = false;
        }

        private static bool TryStyleBloodlinesButton(Image image, Button button)
        {
            Sprite normalSprite = GetBloodlinesButtonDefaultSprite();
            if (normalSprite == null)
            {
                return false;
            }

            // Bloodlines button art includes its own border, so sprite swap handles hover states cleanly.
            image.sprite = normalSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;

            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = GetBloodlinesButtonHoverSprite(),
                pressedSprite = GetBloodlinesButtonPressedSprite(),
                disabledSprite = GetBloodlinesButtonDisabledSprite()
            };

            return true;
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
            bool hasBloodlinesStyle = TryStyleBloodlinesButton(image, button);

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = hasBloodlinesStyle ? new Vector2(236f, 72f) : new Vector2(220f, 56f);

            Text buttonText = CreateText(buttonObject.transform, "Label", label, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, Vector2.zero, new Vector2(200f, 44f));
            buttonText.raycastTarget = false;
        }

        private Slider CreateAudioSliderRow(Transform parent, string label, Vector2 position, float value, UnityEngine.Events.UnityAction<float> onChanged, out Text valueText)
        {
            CreateText(parent, $"{label} Label", label, 19, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, position + new Vector2(-154f, 18f), new Vector2(200f, 30f));
            valueText = CreateText(parent, $"{label} Value", string.Empty, 17, FontStyle.Normal, TextAnchor.MiddleRight, MutedTextColor, position + new Vector2(174f, 18f), new Vector2(90f, 28f));

            Slider slider = CreateSlider(parent, $"{label} Slider", position + new Vector2(0f, -18f), new Vector2(380f, 24f), onChanged);
            slider.value = value;
            return slider;
        }

        private Slider CreateSlider(Transform parent, string objectName, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction<float> onChanged)
        {
            GameObject sliderObject = new GameObject(objectName);
            sliderObject.transform.SetParent(parent, false);

            RectTransform rectTransform = sliderObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(onChanged);

            Image background = CreateSliderPart(sliderObject.transform, "Background", Vector2.zero, size, new Color(0.025f, 0.022f, 0.024f, 1f));
            ApplyBloodlinesSprite(background, GetBloodlinesSliderEmptySprite());
            slider.targetGraphic = background;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(7f, 0f);
            fillAreaRect.offsetMax = new Vector2(-7f, 0f);

            Image fill = CreateSliderPart(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, GoldColor);
            ApplyBloodlinesSprite(fill, GetBloodlinesSliderFullSprite());
            ConfigureSliderFill(fill);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            slider.fillRect = fillRect;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(12f, 0f);
            handleAreaRect.offsetMax = new Vector2(-12f, 0f);

            Image handle = CreateSliderPart(handleArea.transform, "Handle", Vector2.zero, new Vector2(14f, 40f), TextColor);
            ApplyBloodlinesSprite(handle, GetBloodlinesSliderHandleSprite());
            slider.handleRect = handle.rectTransform;

            return slider;
        }

        private static Image CreateSliderPart(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject partObject = new GameObject(objectName);
            partObject.transform.SetParent(parent, false);

            Image image = partObject.AddComponent<Image>();
            image.color = color;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            return image;
        }

        private static void ApplyBloodlinesSprite(Image image, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private static void ConfigureSliderFill(Image fill)
        {
            // Filled images reveal the existing sprite from left to right instead of resizing it,
            // which keeps the Bloodlines segment art from stretching as the slider value changes.
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
        }

        private void RefreshSettingsValues()
        {
            SetSliderValue(masterVolumeSlider, GameAudioSettings.MasterVolume);
            SetSliderValue(musicVolumeSlider, GameAudioSettings.MusicVolume);
            SetSliderValue(gameSoundsVolumeSlider, GameAudioSettings.GameSoundsVolume);
            SetVolumeText(masterVolumeValueText, GameAudioSettings.MasterVolume);
            SetVolumeText(musicVolumeValueText, GameAudioSettings.MusicVolume);
            SetVolumeText(gameSoundsVolumeValueText, GameAudioSettings.GameSoundsVolume);
        }

        private static void SetSliderValue(Slider slider, float volume)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(volume);
            }
        }

        private static void SetVolumeText(Text text, float volume)
        {
            if (text != null)
            {
                text.text = $"{Mathf.RoundToInt(volume * 100f)}%";
            }
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
