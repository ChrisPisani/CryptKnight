using CryptKnight.Application;
using CryptKnight.Content;
using CryptKnight.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CryptKnight.UI
{
    public sealed class FinalWaveDisplayController : MonoBehaviour
    {
        private const string BloodlinesStatusPath = "Art/UI/Bloodlines/Buttons/Status_Red_Default";
        private static readonly Color PanelColor = new Color(0.035f, 0.025f, 0.03f, 0.96f);
        private static readonly Color AccentColor = new Color(0.88f, 0.16f, 0.12f, 1f);
        private static readonly Color TextColor = new Color(0.96f, 0.93f, 0.84f, 1f);

        private GameObject displayRoot;
        private Text waveText;
        private Text statusText;

        public void Initialize(Transform parent, Font font)
        {
            displayRoot = new GameObject("Final Wave Display");
            displayRoot.transform.SetParent(parent, false);

            Image panel = displayRoot.AddComponent<Image>();
            Sprite statusFrame = RuntimeAssetLoader.LoadSprite(BloodlinesStatusPath);
            panel.sprite = statusFrame;
            panel.preserveAspect = statusFrame != null;
            panel.color = statusFrame != null ? Color.white : PanelColor;

            RectTransform rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(390f, 86f);

            waveText = CreateText(displayRoot.transform, "Wave", font, 25, FontStyle.Bold, TextColor, new Vector2(0f, 17f));
            statusText = CreateText(displayRoot.transform, "Status", font, 17, FontStyle.Bold, AccentColor, new Vector2(0f, -20f));
            displayRoot.SetActive(false);
        }

        private void Update()
        {
            DungeonRoomRuntimeState roomState = GetCurrentFinalRoom();
            FinalEncounterState encounter = roomState?.FinalEncounter;
            if (encounter == null || encounter.Status == FinalEncounterStatus.NotStarted || encounter.IsComplete)
            {
                displayRoot.SetActive(false);
                return;
            }

            displayRoot.SetActive(true);
            waveText.text = $"FINAL WAVE {encounter.CurrentWaveNumber} / {encounter.TotalWaves}";
            statusText.text = encounter.Status == FinalEncounterStatus.Intermission
                ? "PREPARE"
                : $"{encounter.RemainingEnemies} ENEMIES REMAIN";
        }

        private static DungeonRoomRuntimeState GetCurrentFinalRoom()
        {
            if (!GameManager.HasInstance || GameManager.Instance.CurrentRun == null || !GameManager.Instance.CurrentRun.IsActive)
            {
                return null;
            }

            DungeonRunState dungeon = GameManager.Instance.CurrentRun.Dungeon;
            if (dungeon == null || dungeon.Navigator.CurrentRoom.RoomType != RoomType.Final)
            {
                return null;
            }

            return dungeon.CurrentRoomState;
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, FontStyle style, Color color, Vector2 position)
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
            rect.sizeDelta = new Vector2(350f, 34f);
            return text;
        }
    }
}
