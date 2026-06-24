using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.Loot
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class LootPickup : MonoBehaviour
    {
        private const float PickupRadius = 1.05f;
        private const float DefaultVisualScale = 0.28f;
        private const float KeyVisualScale = 0.8f;
        private const string KeyItemId = "key";
        private const float PromptWorldOffsetY = 1.1f;
        private const float BobAmplitude = 0.08f;
        private const float BobSpeed = 3.2f;
        private const float SpawnLaunchSeconds = 0.32f;
        private const float SpawnLaunchArcHeight = 0.45f;
        private const int PickupSortingOrder = 4;

        private LootItemDefinition itemDefinition;
        private SpriteRenderer spriteRenderer;
        private CircleCollider2D pickupCollider;
        private GameObject promptRoot;
        private TextMesh promptText;
        private Vector3 bobBasePosition;
        private float bobPhase;
        private bool hasBobBasePosition;
        private int playersInRange;
        private bool wasCollected;
        private float visualScale = DefaultVisualScale;
        private Vector3 launchStartPosition;
        private Vector3 launchEndPosition;
        private Rect? launchBounds;
        private float launchElapsed;
        private bool isLaunching;

        public LootItemDefinition ItemDefinition => itemDefinition;
        public bool IsPlayerInRange => playersInRange > 0;
        public bool IsPromptVisible => promptRoot != null && promptRoot.activeSelf;

        public void Initialize(LootItemDefinition definition)
        {
            itemDefinition = definition;
            EnsureComponents();
            ConfigureVisual();
            ConfigurePrompt();
            ConfigurePromptText();
            SetPromptVisible(false);
            CaptureBobBasePosition();
        }

        public void PlaySpawnLaunch(Vector3 startPosition, Vector3 endPosition)
        {
            PlaySpawnLaunch(startPosition, endPosition, null);
        }

        public void PlaySpawnLaunch(Vector3 startPosition, Vector3 endPosition, Rect? bounds)
        {
            launchStartPosition = startPosition;
            launchEndPosition = endPosition;
            launchBounds = bounds;
            launchElapsed = 0f;
            isLaunching = true;
            transform.position = startPosition;
            bobBasePosition = endPosition;
            hasBobBasePosition = true;
        }

        private void Awake()
        {
            EnsureComponents();
            ConfigurePrompt();
            SetPromptVisible(false);
        }

        private void Update()
        {
            if (ApplySpawnLaunch())
            {
                return;
            }

            ApplyBobbing();

            if (GameManager.Instance.IsGameplayPaused)
            {
                return;
            }

            if (IsPlayerInRange && IsInteractPressed())
            {
                TryPickUp();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            playersInRange++;
            SetPromptVisible(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            playersInRange = Mathf.Max(0, playersInRange - 1);
            SetPromptVisible(IsPlayerInRange);
        }

        public bool TryPickUp()
        {
            if (wasCollected || itemDefinition == null)
            {
                return false;
            }

            wasCollected = GameManager.Instance.CollectLootItem(itemDefinition);
            if (!wasCollected)
            {
                return false;
            }

            GameSfxPlayer.PlayItemPowerupPickup();
            Destroy(gameObject);
            return true;
        }

        private void EnsureComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<CircleCollider2D>();
                pickupCollider.isTrigger = true;
                pickupCollider.radius = PickupRadius;
            }
        }

        private void ConfigureVisual()
        {
            visualScale = GetVisualScale(itemDefinition);
            spriteRenderer.sprite = LootItemVisuals.GetItemSprite(itemDefinition);
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = PickupSortingOrder;
            transform.localScale = new Vector3(visualScale, visualScale, 1f);

            // The collider is larger than the item so you don't have to be directly on top
            pickupCollider.radius = PickupRadius / visualScale;
        }

        private void ConfigurePrompt()
        {
            if (promptRoot != null)
            {
                ApplyPromptTransform();
                return;
            }

            promptRoot = new GameObject("Pickup Prompt");
            promptRoot.transform.SetParent(transform, false);
            ApplyPromptTransform();

            GameObject background = new GameObject("Prompt Background");
            background.transform.SetParent(promptRoot.transform, false);
            background.transform.localScale = new Vector3(2.7f, 0.35f, 1f);
            SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = LootItemVisuals.GetSquareSprite();
            backgroundRenderer.color = new Color(0.03f, 0.035f, 0.04f, 0.84f);
            backgroundRenderer.sortingOrder = 29;

            GameObject textObject = new GameObject("Prompt Text");
            textObject.transform.SetParent(promptRoot.transform, false);
            textObject.transform.localPosition = new Vector3(0f, -0.03f, 0f);

            promptText = textObject.AddComponent<TextMesh>();
            promptText.anchor = TextAnchor.MiddleCenter;
            promptText.alignment = TextAlignment.Center;
            promptText.characterSize = 0.055f;
            promptText.fontSize = 32;
            promptText.color = new Color(0.98f, 0.96f, 0.88f, 1f);

            MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
            textRenderer.sortingOrder = 30;

            ConfigurePromptText();
        }

        private void ApplyPromptTransform()
        {
            promptRoot.transform.localPosition = new Vector3(0f, PromptWorldOffsetY / visualScale, 0f);
            promptRoot.transform.localScale = new Vector3(1f / visualScale, 1f / visualScale, 1f);
        }

        private void CaptureBobBasePosition()
        {
            bobBasePosition = transform.position;
            bobPhase = Mathf.Abs(transform.position.x * 0.73f + transform.position.y * 0.41f);
            hasBobBasePosition = true;
        }

        private bool ApplySpawnLaunch()
        {
            if (!isLaunching)
            {
                return false;
            }

            launchElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(launchElapsed / SpawnLaunchSeconds);
            Vector3 position = Vector3.Lerp(launchStartPosition, launchEndPosition, progress);
            // Add a quick arc so chest rewards read like they popped out instead of teleporting into place.
            position.y += Mathf.Sin(progress * Mathf.PI) * SpawnLaunchArcHeight;
            position = ClampToLaunchBounds(position);
            transform.position = position;

            if (progress >= 1f)
            {
                isLaunching = false;
                launchBounds = null;
                CaptureBobBasePosition();
            }

            return true;
        }

        private Vector3 ClampToLaunchBounds(Vector3 position)
        {
            if (!launchBounds.HasValue)
            {
                return position;
            }

            Rect bounds = launchBounds.Value;
            return new Vector3(
                Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(position.y, bounds.yMin, bounds.yMax),
                position.z);
        }

        private void ApplyBobbing()
        {
            if (!hasBobBasePosition)
            {
                CaptureBobBasePosition();
            }

            float bobOffset = Mathf.Sin(Time.time * BobSpeed + bobPhase) * BobAmplitude;
            transform.position = bobBasePosition + new Vector3(0f, bobOffset, 0f);
        }

        private void ConfigurePromptText()
        {
            if (promptText == null || itemDefinition == null)
            {
                return;
            }

            promptText.text = $"Press E to pick up {itemDefinition.DisplayName}";
        }

        private void SetPromptVisible(bool isVisible)
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(isVisible);
            }
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

        private static float GetVisualScale(LootItemDefinition itemDefinition)
        {
            return itemDefinition != null && itemDefinition.ItemId == KeyItemId ? KeyVisualScale : DefaultVisualScale;
        }
    }
}
