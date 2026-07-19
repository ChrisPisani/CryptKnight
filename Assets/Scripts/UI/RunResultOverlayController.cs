using System;
using System.Collections;
using CryptKnight.Application;
using CryptKnight.Content;
using CryptKnight.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CryptKnight.UI
{
    public sealed class RunResultOverlayController : MonoBehaviour
    {
        private const string BloodlinesPanelPath = "Art/UI/Bloodlines/Frames/Frame_main_menu_red";
        private const string BloodlinesStatusPath = "Art/UI/Bloodlines/Buttons/Status_Red_Default";
        private const string BloodlinesButtonDefaultPath = "Art/UI/Bloodlines/Buttons/Status_Red_Default";
        private const string BloodlinesButtonHoverPath = "Art/UI/Bloodlines/Buttons/Status_Red_Hover";
        private const string BloodlinesButtonPressedPath = "Art/UI/Bloodlines/Buttons/Status_Pressed";
        private const string VictoryScreenPath = "Art/UI/victory_end_game_screen";

        private static readonly Color VictoryColor = new Color(0.94f, 0.70f, 0.24f, 1f);
        private static readonly Color DefeatColor = new Color(0.92f, 0.20f, 0.16f, 1f);
        private static readonly Color TextColor = new Color(0.96f, 0.93f, 0.84f, 1f);

        private GameObject overlayRoot;
        private GameObject resultPanel;
        private GameObject victoryScreen;
        private GameObject returnButton;
        private Image backdrop;
        private Text resultText;
        private Action showMenu;
        private Coroutine resultRoutine;
        private bool showingVictory;

        public void Initialize(Transform parent, Font font, Action showMenuAction)
        {
            showMenu = showMenuAction;
            BuildOverlay(parent, font);
            GameManager.Instance.RunStateChanged += HandleRunStateChanged;
            HandleRunStateChanged(GameManager.Instance.CurrentRun);
        }

        public void Dismiss()
        {
            bool wasVisible = overlayRoot != null && overlayRoot.activeSelf;
            StopResultRoutine();
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            if (wasVisible)
            {
                showMenu?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RunStateChanged -= HandleRunStateChanged;
            }
        }

        private void HandleRunStateChanged(GameRunState runState)
        {
            bool showResult = runState != null &&
                (runState.Status == GameRunStatus.Completed || runState.Status == GameRunStatus.Failed);
            if (!showResult)
            {
                StopResultRoutine();
                overlayRoot.SetActive(false);
                return;
            }

            bool victory = runState.Status == GameRunStatus.Completed;
            showingVictory = victory;
            resultText.text = victory ? "VICTORY" : "DEFEAT";
            resultText.color = victory ? VictoryColor : DefeatColor;
            resultPanel.SetActive(false);
            victoryScreen.SetActive(false);
            returnButton.SetActive(false);
            SetBackdropAlpha(0f);
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            StopResultRoutine();
            resultRoutine = StartCoroutine(ShowResultSequence());
        }

        private IEnumerator ShowResultSequence()
        {
            float elapsed = 0f;
            while (elapsed < RunEndTransitionTiming.FadeToBlackSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetBackdropAlpha(elapsed / RunEndTransitionTiming.FadeToBlackSeconds);
                yield return null;
            }

            SetBackdropAlpha(1f);
            victoryScreen.SetActive(showingVictory);
            resultPanel.SetActive(!showingVictory);
            returnButton.SetActive(true);
            resultRoutine = null;
        }

        private void StopResultRoutine()
        {
            if (resultRoutine == null)
            {
                return;
            }

            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }

        private void SetBackdropAlpha(float alpha)
        {
            backdrop.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        }

        private void BuildOverlay(Transform parent, Font font)
        {
            overlayRoot = new GameObject("Run Result Overlay");
            overlayRoot.transform.SetParent(parent, false);
            backdrop = overlayRoot.AddComponent<Image>();
            SetBackdropAlpha(0f);
            RectTransform rootRect = backdrop.rectTransform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            victoryScreen = new GameObject("Victory End Game Screen");
            victoryScreen.transform.SetParent(overlayRoot.transform, false);
            Image victoryImage = victoryScreen.AddComponent<Image>();
            victoryImage.sprite = RuntimeAssetLoader.LoadSprite(VictoryScreenPath);
            victoryImage.preserveAspect = true;
            victoryImage.color = Color.white;
            victoryImage.raycastTarget = false;
            RectTransform victoryRect = victoryImage.rectTransform;
            victoryRect.anchorMin = Vector2.zero;
            victoryRect.anchorMax = Vector2.one;
            victoryRect.offsetMin = Vector2.zero;
            victoryRect.offsetMax = Vector2.zero;

            resultPanel = new GameObject("Result Panel");
            resultPanel.transform.SetParent(overlayRoot.transform, false);
            Image panel = resultPanel.AddComponent<Image>();
            panel.sprite = RuntimeAssetLoader.LoadSprite(BloodlinesPanelPath);
            panel.type = panel.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panel.color = panel.sprite != null ? Color.white : new Color(0.05f, 0.025f, 0.03f, 0.98f);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600f, 460f);

            GameObject resultBanner = new GameObject("Result Banner");
            resultBanner.transform.SetParent(resultPanel.transform, false);
            Image banner = resultBanner.AddComponent<Image>();
            banner.sprite = RuntimeAssetLoader.LoadSprite(BloodlinesStatusPath);
            banner.preserveAspect = banner.sprite != null;
            banner.color = banner.sprite != null ? Color.white : new Color(0.18f, 0.03f, 0.03f, 1f);
            RectTransform bannerRect = banner.rectTransform;
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = new Vector2(0f, 72f);
            bannerRect.sizeDelta = new Vector2(440f, 130f);

            resultText = CreateText(resultBanner.transform, "Result", font, 48, FontStyle.Bold, TextColor, Vector2.zero, new Vector2(380f, 72f));
            CreateText(resultPanel.transform, "Message", font, 20, FontStyle.Normal, TextColor, new Vector2(0f, -8f), new Vector2(440f, 42f)).text = "THE RUN HAS ENDED";
            returnButton = CreateButton(overlayRoot.transform, font);
            victoryScreen.SetActive(false);
            resultPanel.SetActive(false);
            overlayRoot.SetActive(false);
        }

        private GameObject CreateButton(Transform parent, Font font)
        {
            GameObject buttonObject = new GameObject("Return To Menu Button");
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            Sprite normalSprite = RuntimeAssetLoader.LoadSprite(BloodlinesButtonDefaultPath);
            image.sprite = normalSprite;
            image.preserveAspect = normalSprite != null;
            image.color = normalSprite != null ? Color.white : DefeatColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Dismiss);
            if (normalSprite != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = RuntimeAssetLoader.LoadSprite(BloodlinesButtonHoverPath),
                    pressedSprite = RuntimeAssetLoader.LoadSprite(BloodlinesButtonPressedPath)
                };
            }

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -132f);
            rect.sizeDelta = new Vector2(300f, 88f);
            CreateText(buttonObject.transform, "Label", font, 20, FontStyle.Bold, TextColor, Vector2.zero, new Vector2(250f, 48f)).text = "RETURN TO MENU";
            return buttonObject;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Font font,
            int size,
            FontStyle style,
            Color color,
            Vector2 position,
            Vector2 dimensions)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return text;
        }
    }
}
