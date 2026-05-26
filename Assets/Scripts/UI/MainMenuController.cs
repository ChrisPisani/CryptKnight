using CryptKnight.Application;
using CryptKnight.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace CryptKnight.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string DemoVersion = "v0.1.0";

        private static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.16f, 0.94f);
        private static readonly Color TitleShadowColor = new Color(0.02f, 0.02f, 0.025f, 0.9f);
        private static readonly Color TitleAccentColor = new Color(0.85f, 0.11f, 0.09f, 1f);
        private static readonly Color ButtonColor = new Color(0.65f, 0.12f, 0.12f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.82f, 0.18f, 0.16f, 1f);
        private static readonly Color TextColor = new Color(0.94f, 0.91f, 0.84f, 1f);
        private static readonly Color MutedTextColor = new Color(0.72f, 0.70f, 0.65f, 1f);

        private Font defaultFont;
        private GameObject menuScreen;
        private GameObject menuPanel;
        private GameObject runPanel;
        private Text runInfoText;

        private void Awake()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            EnsureEventSystem();
            BuildInterface();
        }

        private void OnEnable()
        {
            GameManager.Instance.RunStateChanged += HandleRunStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RunStateChanged -= HandleRunStateChanged;
            }
        }

        private void Start()
        {
            HandleRunStateChanged(GameManager.Instance.CurrentRun);
        }

        private void BuildInterface()
        {
            Canvas canvas = CreateCanvas();

            menuScreen = new GameObject("Menu Screen");
            menuScreen.transform.SetParent(canvas.transform, false);
            RectTransform menuScreenRect = menuScreen.AddComponent<RectTransform>();
            menuScreenRect.anchorMin = Vector2.zero;
            menuScreenRect.anchorMax = Vector2.one;
            menuScreenRect.offsetMin = Vector2.zero;
            menuScreenRect.offsetMax = Vector2.zero;

            CreateFullScreenBackground(menuScreen.transform);

            menuPanel = CreatePanel(menuScreen.transform, "Menu Panel", new Vector2(1920f, 1080f));
            CreateTitle(menuPanel.transform);
            CreateText(menuPanel.transform, "Subtitle", "Work in progress demo", 22, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, 86f), new Vector2(620f, 40f));
            CreateButton(menuPanel.transform, "New Run Button", "NEW RUN", new Vector2(0f, -56f), StartNewRun);
            CreateVersionLabel(menuScreen.transform);

            runPanel = CreatePanel(canvas.transform, "Run Panel", new Vector2(520f, 320f));
            CreateText(runPanel.transform, "Run Title", "RUN STARTED", 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 108f), new Vector2(460f, 54f));
            runInfoText = CreateText(runPanel.transform, "Run Info", string.Empty, 18, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, 18f), new Vector2(460f, 150f));
            CreateButton(runPanel.transform, "Quit Run Button", "QUIT RUN", new Vector2(0f, -108f), QuitRun);
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Crypt Knight UI");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void CreateFullScreenBackground(Transform parent)
        {
            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(parent, false);

            Image background = backgroundObject.AddComponent<Image>();
            background.color = BackgroundColor;

            RectTransform rectTransform = background.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private GameObject CreatePanel(Transform parent, string objectName, Vector2 size)
        {
            GameObject panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(parent, false);

            Image panel = panelObject.AddComponent<Image>();
            panel.color = PanelColor;

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

        private void CreateTitle(Transform parent)
        {
            CreateText(parent, "Title Shadow", "CRYPT KNIGHT", 68, FontStyle.Bold, TextAnchor.MiddleCenter, TitleShadowColor, new Vector2(5f, 157f), new Vector2(700f, 96f));

            Text accentText = CreateText(parent, "Title Accent", "CRYPT KNIGHT", 68, FontStyle.Bold, TextAnchor.MiddleCenter, TitleAccentColor, new Vector2(0f, 152f), new Vector2(700f, 96f));
            accentText.resizeTextForBestFit = true;
            accentText.resizeTextMinSize = 42;
            accentText.resizeTextMaxSize = 68;

            Text titleText = CreateText(parent, "Title", "CRYPT KNIGHT", 66, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 160f), new Vector2(700f, 96f));
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 42;
            titleText.resizeTextMaxSize = 66;

            CreateDivider(parent, "Title Divider", new Vector2(0f, 103f), new Vector2(520f, 4f), TitleAccentColor);
            CreateDivider(parent, "Title Divider Shadow", new Vector2(0f, 96f), new Vector2(390f, 2f), TitleShadowColor);
        }

        private static void CreateDivider(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject dividerObject = new GameObject(objectName);
            dividerObject.transform.SetParent(parent, false);

            Image divider = dividerObject.AddComponent<Image>();
            divider.color = color;

            RectTransform rectTransform = divider.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private void StartNewRun()
        {
            GameManager.Instance.StartNewRun();
        }

        private void QuitRun()
        {
            GameManager.Instance.QuitCurrentRun();
        }

        private void HandleRunStateChanged(GameRunState runState)
        {
            bool hasActiveRun = runState != null && runState.IsActive;
            menuScreen.SetActive(!hasActiveRun);
            runPanel.SetActive(false);

            if (runState == null)
            {
                return;
            }

            if (hasActiveRun)
            {
                runInfoText.text =
                    $"Seed: {runState.Seed}\n" +
                    $"Dungeon: {runState.DungeonWidth} x {runState.DungeonHeight}\n" +
                    $"Hearts: {FormatHearts(runState.CurrentHealth)} / {FormatHearts(runState.MaxHealth)}\n" +
                    $"Keys: {runState.KeyCount}";
                return;
            }

            runInfoText.text = string.Empty;
        }

        private void CreateVersionLabel(Transform parent)
        {
            Text versionText = CreateText(parent, "Demo Version", DemoVersion, 14, FontStyle.Normal, TextAnchor.LowerRight, MutedTextColor, Vector2.zero, new Vector2(240f, 32f));

            RectTransform rectTransform = versionText.rectTransform;
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-18f, 14f);
        }

        private static string FormatHearts(int halfHeartValue)
        {
            int fullHearts = halfHeartValue / 2;
            bool hasHalfHeart = halfHeartValue % 2 != 0;

            if (!hasHalfHeart)
            {
                return $"{fullHearts}";
            }

            return $"{fullHearts}.5";
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureInputSystemModule(inputModule);
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static void ConfigureInputSystemModule(InputSystemUIInputModule inputModule)
        {
            InputActionAsset actionsAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            actionsAsset.name = "Runtime UI Input Actions";

            InputActionMap uiMap = actionsAsset.AddActionMap("UI");

            InputAction point = uiMap.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            InputAction leftClick = uiMap.AddAction("LeftClick", InputActionType.PassThrough, "<Pointer>/press");
            InputAction scrollWheel = uiMap.AddAction("ScrollWheel", InputActionType.PassThrough, "<Pointer>/scroll");

            InputAction move = uiMap.AddAction("Move", InputActionType.PassThrough);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            InputAction submit = uiMap.AddAction("Submit", InputActionType.Button);
            submit.AddBinding("<Keyboard>/enter");
            submit.AddBinding("<Keyboard>/space");
            submit.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancel = uiMap.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");

            inputModule.actionsAsset = actionsAsset;
            inputModule.point = InputActionReference.Create(point);
            inputModule.leftClick = InputActionReference.Create(leftClick);
            inputModule.scrollWheel = InputActionReference.Create(scrollWheel);
            inputModule.move = InputActionReference.Create(move);
            inputModule.submit = InputActionReference.Create(submit);
            inputModule.cancel = InputActionReference.Create(cancel);

            uiMap.Enable();
        }
#endif
    }
}
