using System.Collections;
using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Content;
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
        private const string DemoVersion = "v0.9.0";
        private const string MainMenuBackgroundPath = "Art/UI/main_menu_background";
        private const string MainMenuButtonSheetPath = "Art/UI/crypt-knight-buttons-transparent-full-sheet";
        private const string LaunchStingerResourcePath = "Audio/Menu/crypt-knight-launch-stinger";
        private const string MainMenuLoopResourcePath = "Audio/Menu/crypt-knight-main-menu-loop";
        private const string BloodlinesUIFontPath = "Art/UI/Bloodlines/Fonts/MedievalSharp-Regular";
        private const string BloodlinesPanelSpritePath = "Art/UI/Bloodlines/Frames/Frame_main_menu_red";
        private const string BloodlinesButtonDefaultPath = "Art/UI/Bloodlines/Buttons/Status_Red_Default";
        private const string BloodlinesButtonHoverPath = "Art/UI/Bloodlines/Buttons/Status_Red_Hover";
        private const string BloodlinesButtonPressedPath = "Art/UI/Bloodlines/Buttons/Status_Pressed";
        private const string BloodlinesButtonDisabledPath = "Art/UI/Bloodlines/Buttons/Status_Disable";
        private const string BloodlinesSliderEmptyPath = "Art/UI/Bloodlines/Sliders/Slider_empty";
        private const string BloodlinesSliderFullPath = "Art/UI/Bloodlines/Sliders/Slider_full_v1";
        private const string BloodlinesSliderHandlePath = "Art/UI/Bloodlines/Sliders/Slider_toggler";
        private const float MainMenuLoopFadeSeconds = .6f;
        private const float MainMenuIntroToLoopFadeSeconds = 1.5f;

        private static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.16f, 0.94f);
        private static readonly Color PopupBackdropColor = new Color(0.015f, 0.012f, 0.014f, 0.72f);
        private static readonly Color PopupPanelColor = new Color(0.07f, 0.065f, 0.07f, 0.97f);
        private static readonly Color TitleShadowColor = new Color(0.02f, 0.02f, 0.025f, 0.9f);
        private static readonly Color TitleAccentColor = new Color(0.85f, 0.11f, 0.09f, 1f);
        private static readonly Color ButtonColor = new Color(0.14f, 0.025f, 0.022f, 0.95f);
        private static readonly Color ButtonHoverColor = new Color(0.36f, 0.055f, 0.045f, 0.98f);
        private static readonly Color ButtonPressedColor = new Color(0.08f, 0.012f, 0.014f, 1f);
        private static readonly Color CompactButtonColor = new Color(0.65f, 0.12f, 0.12f, 1f);
        private static readonly Color CompactButtonHoverColor = new Color(0.82f, 0.18f, 0.16f, 1f);
        private static readonly Color CompactButtonPressedColor = new Color(0.42f, 0.08f, 0.08f, 1f);
        private static readonly Color GoldColor = new Color(0.86f, 0.61f, 0.24f, 1f);
        private static readonly Color DarkGoldColor = new Color(0.35f, 0.19f, 0.065f, 1f);
        private static readonly Color TextColor = new Color(0.94f, 0.91f, 0.84f, 1f);
        private static readonly Color MutedTextColor = new Color(0.72f, 0.70f, 0.65f, 1f);
        private static readonly Color TextOutlineColor = new Color(0.025f, 0.018f, 0.014f, 0.92f);
        private static readonly Color TextShadowColor = new Color(0f, 0f, 0f, 0.78f);

        private Font defaultFont;
        private GameObject menuScreen;
        private GameObject menuPanel;
        private GameObject runPanel;
        private GameObject settingsPopup;
        private GameObject continueUnavailablePopup;
        private Text runInfoText;
        private Text masterVolumeValueText;
        private Text musicVolumeValueText;
        private Text gameSoundsVolumeValueText;
        private Slider masterVolumeSlider;
        private Slider musicVolumeSlider;
        private Slider gameSoundsVolumeSlider;
        private AudioSource menuIntroSource;
        private AudioSource menuLoopSource;
        private AudioClip launchStingerClip;
        private AudioClip mainMenuLoopClip;
        private Coroutine menuMusicRoutine;
        private Coroutine continueUnavailableRoutine;
        private bool launchStingerPlayed;
        private bool wasRunActive;
        private static Sprite bloodlinesPanelSprite;
        private static Sprite bloodlinesButtonDefaultSprite;
        private static Sprite bloodlinesButtonHoverSprite;
        private static Sprite bloodlinesButtonPressedSprite;
        private static Sprite bloodlinesButtonDisabledSprite;
        private static Sprite bloodlinesSliderEmptySprite;
        private static Sprite bloodlinesSliderFullSprite;
        private static Sprite bloodlinesSliderHandleSprite;

        private static readonly Vector2 MainMenuButtonSize = new Vector2(430f, 191f);
        private static readonly Vector2 MainMenuButtonHoverOffset = new Vector2(14.5f, 0f);

        private void Awake()
        {
            defaultFont = LoadBloodlinesFont();
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameAudioSettings.Initialize();
            EnsureEventSystem();
            BuildInterface();
            ConfigureMenuMusic();
        }

        private void OnEnable()
        {
            GameAudioSettings.VolumesChanged += HandleAudioSettingsChanged;
            GameManager.Instance.RunStateChanged += HandleRunStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RunStateChanged -= HandleRunStateChanged;
            }

            GameAudioSettings.VolumesChanged -= HandleAudioSettingsChanged;
            StopMenuMusic();
        }

        private void Start()
        {
            HandleRunStateChanged(GameManager.Instance.CurrentRun);
        }

        private void BuildInterface()
        {
            Canvas canvas = CreateCanvas();

            // Menu and HUD share one generated canvas with their own root
            menuScreen = new GameObject("Menu Screen");
            menuScreen.transform.SetParent(canvas.transform, false);
            RectTransform menuScreenRect = menuScreen.AddComponent<RectTransform>();
            menuScreenRect.anchorMin = Vector2.zero;
            menuScreenRect.anchorMax = Vector2.one;
            menuScreenRect.offsetMin = Vector2.zero;
            menuScreenRect.offsetMax = Vector2.zero;

            bool hasMenuArt = CreateFullScreenBackground(menuScreen.transform);

            menuPanel = CreateLayoutRoot(menuScreen.transform, "Menu Layout", new Vector2(1920f, 1080f));
            if (!hasMenuArt)
            {
                CreateTitle(menuPanel.transform);
                CreateText(menuPanel.transform, "Subtitle", "Work in progress demo", 22, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, 86f), new Vector2(620f, 40f));
            }

            CreateMenuImageButton(menuPanel.transform, "New Run Button", "NEW RUN", "menu_button_new_run_normal", "menu_button_new_run_hover", new Vector2(0f, -82f), StartNewRun);
            CreateMenuImageButton(menuPanel.transform, "Continue Button", "CONTINUE", "menu_button_continue_normal", "menu_button_continue_hover", new Vector2(0f, -252f), ContinueUnavailable);
            CreateMenuImageButton(menuPanel.transform, "Settings Button", "SETTINGS", "menu_button_settings_normal", "menu_button_settings_hover", new Vector2(0f, -422f), ShowSettings);
            continueUnavailablePopup = CreateContinueUnavailablePopup(menuScreen.transform);
            continueUnavailablePopup.SetActive(false);
            settingsPopup = CreateSettingsPopup(menuScreen.transform);
            settingsPopup.SetActive(false);
            CreateVersionLabel(menuScreen.transform);

            runPanel = CreatePanel(canvas.transform, "Run Panel", new Vector2(520f, 320f));
            CreateText(runPanel.transform, "Run Title", "RUN STARTED", 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 108f), new Vector2(460f, 54f));
            runInfoText = CreateText(runPanel.transform, "Run Info", string.Empty, 18, FontStyle.Normal, TextAnchor.MiddleCenter, MutedTextColor, new Vector2(0f, 18f), new Vector2(460f, 150f));
            CreateButton(runPanel.transform, "Quit Run Button", "QUIT RUN", new Vector2(0f, -108f), QuitRun);

            RunHUDController hudController = gameObject.AddComponent<RunHUDController>();
            hudController.Initialize(canvas.transform, defaultFont);

            RunPauseMenuController pauseMenuController = gameObject.AddComponent<RunPauseMenuController>();
            pauseMenuController.Initialize(canvas.transform, defaultFont);

            FinalWaveDisplayController waveDisplay = gameObject.AddComponent<FinalWaveDisplayController>();
            waveDisplay.Initialize(canvas.transform, defaultFont);

            RunResultOverlayController resultOverlay = gameObject.AddComponent<RunResultOverlayController>();
            resultOverlay.Initialize(canvas.transform, defaultFont, ShowMenuAfterRunResult);
        }

        private void ConfigureMenuMusic()
        {
            launchStingerClip = Resources.Load<AudioClip>(LaunchStingerResourcePath);
            mainMenuLoopClip = Resources.Load<AudioClip>(MainMenuLoopResourcePath);

            if (launchStingerClip == null && mainMenuLoopClip == null)
            {
                Debug.LogWarning("Main menu music clips could not be loaded from Resources/Audio/Menu.");
                return;
            }

            menuIntroSource = CreateMenuMusicSource("Main Menu Intro Music");
            menuLoopSource = CreateMenuMusicSource("Main Menu Loop Music");
            UpdateMenuMusicVolume();
        }

        private AudioSource CreateMenuMusicSource(string sourceName)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = GameAudioSettings.MusicVolume;
            source.name = sourceName;
            return source;
        }

        private void PlayMenuMusic()
        {
            if (menuLoopSource == null || mainMenuLoopClip == null)
            {
                return;
            }

            if (menuMusicRoutine != null || IsMenuMusicPlaying())
            {
                return;
            }

            if (!launchStingerPlayed && launchStingerClip != null)
            {
                menuMusicRoutine = StartCoroutine(PlayLaunchStingerThenLoop());
                return;
            }

            PlayMainMenuLoop();
        }

        private IEnumerator PlayLaunchStingerThenLoop()
        {
            launchStingerPlayed = true;
            menuIntroSource.loop = false;
            menuIntroSource.clip = launchStingerClip;
            menuIntroSource.volume = GameAudioSettings.MusicVolume;
            menuIntroSource.Play();

            float fadeDuration = GetIntroToLoopFadeDuration();
            float playTimeBeforeFade = Mathf.Max(0f, launchStingerClip.length - fadeDuration);
            yield return WaitForMenuMusicSeconds(playTimeBeforeFade);

            menuLoopSource.loop = false;
            menuLoopSource.clip = mainMenuLoopClip;
            menuLoopSource.volume = 0f;
            menuLoopSource.Play();

            // The intro clip now contains the beginning of the loop, so we overlap the two clips
            // and fade between them instead of cutting the intro short at an arbitrary timestamp.
            yield return CrossfadeMenuMusic(menuIntroSource, menuIntroSource.volume, 0f, menuLoopSource, 0f, GameAudioSettings.MusicVolume, fadeDuration);
            menuIntroSource.Stop();

            yield return PlayMainMenuLoopWithEndFade(true, fadeDuration, false);
        }

        private void PlayMainMenuLoop()
        {
            if (menuLoopSource == null || mainMenuLoopClip == null)
            {
                return;
            }

            menuMusicRoutine = StartCoroutine(PlayMainMenuLoopWithEndFade(false, 0f, false));
        }

        private IEnumerator PlayMainMenuLoopWithEndFade(bool loopAlreadyPlaying, float elapsedInCurrentLoop, bool fadeInFirstLoop)
        {
            menuLoopSource.loop = false;
            bool shouldFadeIn = fadeInFirstLoop;

            while (true)
            {
                if (!loopAlreadyPlaying)
                {
                    menuLoopSource.clip = mainMenuLoopClip;
                    menuLoopSource.volume = shouldFadeIn ? 0f : GameAudioSettings.MusicVolume;
                    menuLoopSource.Play();
                    elapsedInCurrentLoop = 0f;
                }

                float fadeDuration = Mathf.Min(MainMenuLoopFadeSeconds, mainMenuLoopClip.length * 0.25f);
                if (shouldFadeIn)
                {
                    yield return FadeMenuMusic(menuLoopSource, 0f, GameAudioSettings.MusicVolume, fadeDuration);
                    elapsedInCurrentLoop += fadeDuration;
                }

                loopAlreadyPlaying = false;
                shouldFadeIn = true;
                float playTimeBeforeFadeOut = Mathf.Max(0f, mainMenuLoopClip.length - elapsedInCurrentLoop - fadeDuration);
                yield return WaitForMenuMusicSeconds(playTimeBeforeFadeOut);

                yield return FadeMenuMusic(menuLoopSource, menuLoopSource.volume, 0f, fadeDuration);
                menuLoopSource.Stop();
                elapsedInCurrentLoop = 0f;
            }
        }

        private IEnumerator FadeMenuMusic(AudioSource source, float startVolume, float endVolume, float duration)
        {
            if (duration <= 0f)
            {
                source.volume = endVolume;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVolume, endVolume, progress);
                yield return null;
            }

            source.volume = endVolume;
        }

        private IEnumerator CrossfadeMenuMusic(AudioSource fadeOutSource, float fadeOutStart, float fadeOutEnd, AudioSource fadeInSource, float fadeInStart, float fadeInEnd, float duration)
        {
            if (duration <= 0f)
            {
                fadeOutSource.volume = fadeOutEnd;
                fadeInSource.volume = fadeInEnd;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                fadeOutSource.volume = Mathf.Lerp(fadeOutStart, fadeOutEnd, progress);
                fadeInSource.volume = Mathf.Lerp(fadeInStart, fadeInEnd, progress);
                yield return null;
            }

            fadeOutSource.volume = fadeOutEnd;
            fadeInSource.volume = fadeInEnd;
        }

        private float GetIntroToLoopFadeDuration()
        {
            float maxIntroFade = launchStingerClip == null ? 0f : launchStingerClip.length * 0.25f;
            float maxLoopFade = mainMenuLoopClip == null ? 0f : mainMenuLoopClip.length * 0.25f;
            return Mathf.Min(MainMenuIntroToLoopFadeSeconds, maxIntroFade, maxLoopFade);
        }

        private bool IsMenuMusicPlaying()
        {
            return (menuIntroSource != null && menuIntroSource.isPlaying) || (menuLoopSource != null && menuLoopSource.isPlaying);
        }

        private static IEnumerator WaitForMenuMusicSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void StopMenuMusic()
        {
            if (menuMusicRoutine != null)
            {
                StopCoroutine(menuMusicRoutine);
                menuMusicRoutine = null;
            }

            if (menuIntroSource != null)
            {
                menuIntroSource.Stop();
            }

            if (menuLoopSource != null)
            {
                menuLoopSource.Stop();
            }

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

        private bool CreateFullScreenBackground(Transform parent)
        {
            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(parent, false);

            Image background = backgroundObject.AddComponent<Image>();
            Sprite backgroundSprite = LoadMenuBackgroundSprite();
            if (backgroundSprite != null)
            {
                background.sprite = backgroundSprite;
                background.color = Color.white;
            }
            else
            {
                background.color = BackgroundColor;
            }

            RectTransform rectTransform = background.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            return backgroundSprite != null;
        }

        private static Sprite LoadMenuBackgroundSprite()
        {
            return RuntimeAssetLoader.LoadSprite(MainMenuBackgroundPath, "main_menu_background_0")
                ?? RuntimeAssetLoader.LoadSprite(MainMenuBackgroundPath);
        }

        private static Font LoadBloodlinesFont()
        {
            return RuntimeAssetLoader.LoadFont(BloodlinesUIFontPath);
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

        private GameObject CreateLayoutRoot(Transform parent, string objectName, Vector2 size)
        {
            GameObject layoutObject = new GameObject(objectName);
            layoutObject.transform.SetParent(parent, false);

            RectTransform rectTransform = layoutObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;

            return layoutObject;
        }

        private GameObject CreatePanel(Transform parent, string objectName, Vector2 size)
        {
            GameObject panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(parent, false);

            Image panel = panelObject.AddComponent<Image>();
            StylePanel(panel, size, PanelColor);

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

        private Button CreateMenuImageButton(
            Transform parent,
            string objectName,
            string fallbackLabel,
            string normalSpriteName,
            string hoverSpriteName,
            Vector2 position,
            UnityEngine.Events.UnityAction onClick)
        {
            Sprite normalSprite = LoadMenuButtonSprite(normalSpriteName);
            Sprite hoverSprite = LoadMenuButtonSprite(hoverSpriteName);
            if (normalSprite == null || hoverSprite == null)
            {
                return CreateButton(parent, objectName, fallbackLabel, position, onClick);
            }

            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = normalSprite;
            image.color = Color.white;
            image.preserveAspect = true;

            Image hoverImage = CreateMenuButtonHoverImage(buttonObject.transform, hoverSprite);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = MainMenuButtonSize;

            EventTrigger eventTrigger = buttonObject.AddComponent<EventTrigger>();
            AddHoverToggle(eventTrigger, image, hoverImage);

            return button;
        }

        private static Image CreateMenuButtonHoverImage(Transform parent, Sprite hoverSprite)
        {
            GameObject hoverObject = new GameObject("Hover Image");
            hoverObject.transform.SetParent(parent, false);

            Image hoverImage = hoverObject.AddComponent<Image>();
            hoverImage.sprite = hoverSprite;
            hoverImage.color = Color.white;
            hoverImage.preserveAspect = true;
            hoverImage.raycastTarget = false;
            hoverImage.gameObject.SetActive(false);

            RectTransform rectTransform = hoverImage.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = MainMenuButtonHoverOffset;
            rectTransform.sizeDelta = MainMenuButtonSize;

            return hoverImage;
        }

        private static void AddHoverToggle(EventTrigger eventTrigger, Image normalImage, Image hoverImage)
        {
            EventTrigger.Entry enterEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener(_ =>
            {
                normalImage.color = new Color(1f, 1f, 1f, 0f);
                hoverImage.gameObject.SetActive(true);
            });
            eventTrigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener(_ =>
            {
                hoverImage.gameObject.SetActive(false);
                normalImage.color = Color.white;
            });
            eventTrigger.triggers.Add(exitEntry);
        }

        private static Sprite LoadMenuButtonSprite(string spriteName)
        {
            return RuntimeAssetLoader.LoadSprite(MainMenuButtonSheetPath, spriteName);
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

        private Button CreateButton(Transform parent, string objectName, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = ButtonColor;
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = GoldColor;
            outline.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.86f);
            shadow.effectDistance = new Vector2(5f, -5f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            button.colors = new ColorBlock
            {
                normalColor = ButtonColor,
                highlightedColor = ButtonHoverColor,
                pressedColor = ButtonPressedColor,
                selectedColor = ButtonHoverColor,
                disabledColor = new Color(0.18f, 0.16f, 0.15f, 1f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            bool hasBloodlinesStyle = TryStyleBloodlinesButton(image, button);
            outline.enabled = !hasBloodlinesStyle;
            shadow.enabled = !hasBloodlinesStyle;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = hasBloodlinesStyle ? new Vector2(270f, 80f) : new Vector2(326f, 58f);

            if (!hasBloodlinesStyle)
            {
                CreateButtonStripe(buttonObject.transform, "Top Bevel", new Vector2(0f, 24f), new Vector2(310f, 2f), GoldColor);
                CreateButtonStripe(buttonObject.transform, "Bottom Bevel", new Vector2(0f, -24f), new Vector2(310f, 2f), DarkGoldColor);
            }

            Text buttonText = CreateText(buttonObject.transform, "Label", label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, Vector2.zero, new Vector2(240f, 52f));
            buttonText.raycastTarget = false;
            if (!hasBloodlinesStyle)
            {
                Outline textOutline = buttonText.gameObject.AddComponent<Outline>();
                textOutline.effectColor = new Color(0.05f, 0.015f, 0.01f, 0.95f);
                textOutline.effectDistance = new Vector2(1.5f, -1.5f);
                Shadow textShadow = buttonText.gameObject.AddComponent<Shadow>();
                textShadow.effectColor = new Color(0f, 0f, 0f, 0.88f);
                textShadow.effectDistance = new Vector2(2f, -2f);
            }

            return button;
        }

        private static void CreateButtonStripe(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject stripeObject = new GameObject(objectName);
            stripeObject.transform.SetParent(parent, false);

            Image stripe = stripeObject.AddComponent<Image>();
            stripe.color = color;
            stripe.raycastTarget = false;

            RectTransform rectTransform = stripe.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private GameObject CreateContinueUnavailablePopup(Transform parent)
        {
            GameObject popupObject = CreatePanel(parent, "Continue Unavailable Popup", new Vector2(600f, 82f));
            Image panelImage = popupObject.GetComponent<Image>();
            StylePanel(panelImage, new Vector2(600f, 82f), new Color(0.045f, 0.038f, 0.036f, 0.96f));
            panelImage.raycastTarget = false;

            RectTransform rectTransform = panelImage.rectTransform;
            rectTransform.anchoredPosition = new Vector2(0f, 112f);

            Outline outline = popupObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.45f, 0.18f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Shadow shadow = popupObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.84f);
            shadow.effectDistance = new Vector2(4f, -4f);

            Text message = CreateText(
                popupObject.transform,
                "Message",
                "That feature is currently unimplemented",
                23,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                TextColor,
                Vector2.zero,
                new Vector2(540f, 52f));
            message.raycastTarget = false;

            Outline textOutline = message.gameObject.AddComponent<Outline>();
            textOutline.effectColor = new Color(0.08f, 0.02f, 0.01f, 0.92f);
            textOutline.effectDistance = new Vector2(1.25f, -1.25f);

            return popupObject;
        }

        private GameObject CreateSettingsPopup(Transform parent)
        {
            GameObject popupObject = new GameObject("Settings Popup");
            popupObject.transform.SetParent(parent, false);

            Image backdrop = popupObject.AddComponent<Image>();
            backdrop.color = PopupBackdropColor;

            RectTransform popupRect = backdrop.rectTransform;
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            GameObject panelObject = CreatePanel(popupObject.transform, "Settings Panel", new Vector2(620f, 430f));
            Image panelImage = panelObject.GetComponent<Image>();
            StylePanel(panelImage, new Vector2(620f, 430f), PopupPanelColor);
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = GoldColor;
            outline.effectDistance = new Vector2(2f, -2f);

            CreateText(panelObject.transform, "Settings Title", "SETTINGS", 34, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor, new Vector2(0f, 158f), new Vector2(480f, 48f));
            masterVolumeSlider = CreateAudioSliderRow(panelObject.transform, "MASTER", new Vector2(0f, 82f), GameAudioSettings.MasterVolume, GameAudioSettings.SetMasterVolume, out masterVolumeValueText);
            musicVolumeSlider = CreateAudioSliderRow(panelObject.transform, "MUSIC", new Vector2(0f, 8f), GameAudioSettings.MusicVolume, GameAudioSettings.SetMusicVolume, out musicVolumeValueText);
            gameSoundsVolumeSlider = CreateAudioSliderRow(panelObject.transform, "GAME SOUNDS", new Vector2(0f, -66f), GameAudioSettings.GameSoundsVolume, GameAudioSettings.SetGameSoundsVolume, out gameSoundsVolumeValueText);
            RefreshSettingsValues();

            CreateCompactButton(panelObject.transform, "Close Settings Button", "CLOSE", new Vector2(0f, -160f), HideSettings);
            return popupObject;
        }

        private Button CreateCompactButton(Transform parent, string objectName, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = CompactButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            button.colors = new ColorBlock
            {
                normalColor = CompactButtonColor,
                highlightedColor = CompactButtonHoverColor,
                pressedColor = CompactButtonPressedColor,
                selectedColor = CompactButtonHoverColor,
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

            return button;
        }

        private Slider CreateAudioSliderRow(Transform parent, string label, Vector2 position, float value, UnityEngine.Events.UnityAction<float> onChanged, out Text valueText)
        {
            CreateText(parent, $"{label} Label", label, 20, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor, position + new Vector2(-178f, 18f), new Vector2(210f, 32f));
            valueText = CreateText(parent, $"{label} Value", string.Empty, 18, FontStyle.Normal, TextAnchor.MiddleRight, MutedTextColor, position + new Vector2(198f, 18f), new Vector2(96f, 30f));

            Slider slider = CreateSlider(parent, $"{label} Slider", position + new Vector2(0f, -18f), new Vector2(420f, 26f), onChanged);
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

            Image background = CreateSliderPart(sliderObject.transform, "Background", new Vector2(0f, 0f), size, new Color(0.025f, 0.022f, 0.024f, 1f));
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

        private void ContinueUnavailable()
        {
            if (continueUnavailablePopup == null)
            {
                return;
            }

            continueUnavailablePopup.SetActive(true);
            continueUnavailablePopup.transform.SetAsLastSibling();

            if (continueUnavailableRoutine != null)
            {
                StopCoroutine(continueUnavailableRoutine);
            }

            continueUnavailableRoutine = StartCoroutine(HideContinueUnavailablePopupAfterDelay());
        }

        private IEnumerator HideContinueUnavailablePopupAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2.25f);

            if (continueUnavailablePopup != null)
            {
                continueUnavailablePopup.SetActive(false);
            }

            continueUnavailableRoutine = null;
        }

        private void ShowSettings()
        {
            if (continueUnavailableRoutine != null)
            {
                StopCoroutine(continueUnavailableRoutine);
                continueUnavailableRoutine = null;
            }

            if (continueUnavailablePopup != null)
            {
                continueUnavailablePopup.SetActive(false);
            }

            settingsPopup.SetActive(true);
            RefreshSettingsValues();
        }

        private void HideSettings()
        {
            settingsPopup.SetActive(false);
        }

        private void HandleAudioSettingsChanged()
        {
            UpdateMenuMusicVolume();
            RefreshSettingsValues();
        }

        private void UpdateMenuMusicVolume()
        {
            bool isCrossfading = menuIntroSource != null && menuIntroSource.isPlaying && menuLoopSource != null && menuLoopSource.isPlaying;
            if (isCrossfading)
            {
                // Keep the intro/loop volume ratio intact during the short crossfade.
                return;
            }

            if (menuIntroSource != null)
            {
                menuIntroSource.volume = GameAudioSettings.MusicVolume;
            }

            if (menuLoopSource != null)
            {
                menuLoopSource.volume = GameAudioSettings.MusicVolume;
            }

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

        private void QuitRun()
        {
            launchStingerPlayed = false;
            GameManager.Instance.QuitCurrentRun();
            if (GameManager.Instance.CurrentRun == null || !GameManager.Instance.CurrentRun.IsActive)
            {
                menuScreen.SetActive(true);
                PlayMenuMusic();
            }
        }

        private void HandleRunStateChanged(GameRunState runState)
        {
            bool hasActiveRun = runState != null && runState.IsActive;
            bool hasRunResult = runState != null &&
                (runState.Status == GameRunStatus.Completed || runState.Status == GameRunStatus.Failed);
            menuScreen.SetActive(!hasActiveRun && !hasRunResult);
            runPanel.SetActive(false);

            if (hasActiveRun)
            {
                wasRunActive = true;
                StopMenuMusic();
            }
            else if (hasRunResult)
            {
                if (wasRunActive)
                {
                    launchStingerPlayed = false;
                    wasRunActive = false;
                }

                StopMenuMusic();
            }
            else
            {
                if (wasRunActive)
                {
                    launchStingerPlayed = false;
                    wasRunActive = false;
                }

                PlayMenuMusic();
            }

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

        private void ShowMenuAfterRunResult()
        {
            menuScreen.SetActive(true);
            PlayMenuMusic();
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
            if (Object.FindAnyObjectByType<EventSystem>() != null)
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
