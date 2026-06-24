using System;
using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Player;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.Loot
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class LockedChest : MonoBehaviour
    {
        private const float InteractionRadius = 1.15f;
        private const float NoKeyFeedbackSeconds = 1.15f;
        private const float PromptWorldOffsetY = 1.15f;
        private const float OpenFrameDuration = 0.11f;
        private const float OpenedSecondsBeforeFade = 2.5f;
        private const float FadeDurationSeconds = 1.25f;
        private const int ChestSortingOrder = 5;
        private const int PromptSortingOrder = 31;
        private const string KeyItemId = "key";
        private const string ChestSpriteSheetPath = "Assets/Art/Items/treasure_chest_opening_strip_alpha.png";
        private static readonly Color ClosedColor = new Color(0.48f, 0.27f, 0.08f, 1f);
        private static readonly Color OpenColor = new Color(0.25f, 0.18f, 0.12f, 1f);
        private static readonly Color PromptTextColor = new Color(0.98f, 0.96f, 0.88f, 1f);
        private static readonly Color WarningTextColor = new Color(1f, 0.34f, 0.24f, 1f);
        private static readonly Vector2 RewardSpawnOffset = new Vector2(0f, 2.15f);

        private LootSystem lootSystem;
        private Action<LootItemDefinition, Vector2> spawnReward;
        private Action opened;
        private System.Random random;
        private SpriteRenderer spriteRenderer;
        private CircleCollider2D interactionCollider;
        private GameObject promptRoot;
        private TextMesh promptText;
        private int playersInRange;
        private bool isOpened;
        private float openedAt;
        private float animationElapsed;
        private float noKeyFeedbackEndsAt;
        private static Sprite[] chestFrames;

        public bool IsOpened => isOpened;
        public bool IsPlayerInRange => playersInRange > 0;
        public bool IsPromptVisible => promptRoot != null && promptRoot.activeSelf;
        public string PromptMessage => promptText != null ? promptText.text : string.Empty;

        public void Initialize(LootTableConfiguration configuration, Action<LootItemDefinition, Vector2> rewardSpawner, int? randomSeed = null, Action onOpened = null)
        {
            lootSystem = new LootSystem(configuration ?? LootTableConfiguration.CreateDefault());
            spawnReward = rewardSpawner;
            opened = onOpened;
            random = randomSeed.HasValue ? new System.Random(randomSeed.Value) : new System.Random();
            EnsureComponents();
            ConfigureVisual(false);
            ConfigurePrompt();
            SetPromptVisible(false);
        }

        private void Awake()
        {
            EnsureComponents();
            ConfigureVisual(false);
            ConfigurePrompt();
            SetPromptVisible(false);
        }

        private void Update()
        {
            if (isOpened)
            {
                UpdateOpenedVisual();
                return;
            }

            if (noKeyFeedbackEndsAt > 0f && Time.time >= noKeyFeedbackEndsAt)
            {
                SetPromptText(GetLockedPrompt(), PromptTextColor);
                noKeyFeedbackEndsAt = 0f;
            }

            if (GameManager.Instance.IsGameplayPaused)
            {
                return;
            }

            if (IsPlayerInRange && IsInteractPressed())
            {
                TryOpen();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            playersInRange++;
            if (!isOpened)
            {
                SetPromptVisible(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            playersInRange = Mathf.Max(0, playersInRange - 1);
            SetPromptVisible(IsPlayerInRange && !isOpened);
        }

        public bool TryOpen()
        {
            if (isOpened)
            {
                return false;
            }

            if (!GameManager.Instance.SpendKey())
            {
                ShowNoKeyFeedback();
                return false;
            }

            isOpened = true;
            openedAt = Time.time;
            animationElapsed = 0f;
            ConfigureVisual(true);
            SetPromptVisible(false);
            GameSfxPlayer.PlayChestOpen();
            // So the chest cannot reappear after travel
            opened?.Invoke();

            EnsureLootSystem();
            // Chests use the normal chest loot pool, but cannot drop keys
            LootDropResult result = lootSystem.RollDrop(LootSourceType.Chest, random, item => item.ItemId != KeyItemId);
            if (result.HasDrop)
            {
                spawnReward?.Invoke(result.Item, (Vector2)transform.position + RewardSpawnOffset);
            }

            return true;
        }

        private void UpdateOpenedVisual()
        {
            Sprite[] frames = GetChestFrames();
            if (frames.Length > 0)
            {
                animationElapsed += Time.deltaTime;
                int frameIndex = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(animationElapsed / OpenFrameDuration));
                spriteRenderer.sprite = frames[frameIndex];
            }

            // Let the reward burst read clearly before the spent chest fades out of the room.
            float fadeAge = Time.time - openedAt - OpenedSecondsBeforeFade;
            if (fadeAge < 0f)
            {
                return;
            }

            float alpha = 1f - Mathf.Clamp01(fadeAge / FadeDurationSeconds);
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, alpha);

            if (alpha <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void EnsureLootSystem()
        {
            if (lootSystem == null)
            {
                lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());
            }

            if (random == null)
            {
                random = new System.Random();
            }
        }

        private void EnsureComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (interactionCollider == null)
            {
                interactionCollider = GetComponent<CircleCollider2D>();
                interactionCollider.isTrigger = true;
                interactionCollider.radius = InteractionRadius;
            }
        }

        private void ConfigureVisual(bool opened)
        {
            Sprite[] frames = GetChestFrames();
            spriteRenderer.sprite = frames.Length > 0 ? frames[0] : LootItemVisuals.GetSquareSprite();
            spriteRenderer.color = frames.Length > 0 ? Color.white : opened ? OpenColor : ClosedColor;
            spriteRenderer.sortingOrder = ChestSortingOrder;
            transform.localScale = Vector3.one;

            if (interactionCollider != null)
            {
                interactionCollider.enabled = !opened;
            }
        }

        private void ConfigurePrompt()
        {
            if (promptRoot != null)
            {
                return;
            }

            promptRoot = new GameObject("Chest Prompt");
            promptRoot.transform.SetParent(transform, false);
            promptRoot.transform.localPosition = new Vector3(0f, PromptWorldOffsetY, 0f);
            promptRoot.transform.localScale = Vector3.one;

            GameObject background = new GameObject("Prompt Background");
            background.transform.SetParent(promptRoot.transform, false);
            background.transform.localScale = new Vector3(2.4f, 0.35f, 1f);
            SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = LootItemVisuals.GetSquareSprite();
            backgroundRenderer.color = new Color(0.03f, 0.035f, 0.04f, 0.84f);
            backgroundRenderer.sortingOrder = PromptSortingOrder;

            GameObject textObject = new GameObject("Prompt Text");
            textObject.transform.SetParent(promptRoot.transform, false);
            textObject.transform.localPosition = new Vector3(0f, -0.03f, 0f);

            promptText = textObject.AddComponent<TextMesh>();
            promptText.anchor = TextAnchor.MiddleCenter;
            promptText.alignment = TextAlignment.Center;
            promptText.characterSize = 0.055f;
            promptText.fontSize = 32;
            SetPromptText(GetLockedPrompt(), PromptTextColor);

            MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
            textRenderer.sortingOrder = PromptSortingOrder + 1;
        }

        private void ShowNoKeyFeedback()
        {
            SetPromptText("You do not have any keys!", WarningTextColor);
            SetPromptVisible(true);
            noKeyFeedbackEndsAt = Time.time + NoKeyFeedbackSeconds;
        }

        private void SetPromptText(string message, Color color)
        {
            if (promptText == null)
            {
                return;
            }

            promptText.text = message;
            promptText.color = color;
        }

        private void SetPromptVisible(bool isVisible)
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(isVisible);
            }
        }

        private static string GetLockedPrompt()
        {
            return "Press E to open chest";
        }

        private static Sprite[] GetChestFrames()
        {
            if (chestFrames != null)
            {
                return chestFrames;
            }

#if UNITY_EDITOR
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ChestSpriteSheetPath);
            System.Collections.Generic.List<Sprite> frames = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name.StartsWith("treasure_chest_opening_", StringComparison.Ordinal))
                {
                    frames.Add(sprite);
                }
            }

            frames.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            chestFrames = frames.ToArray();
#else
            chestFrames = Array.Empty<Sprite>();
#endif
            return chestFrames;
        }

        private static bool IsInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }
    }
}
