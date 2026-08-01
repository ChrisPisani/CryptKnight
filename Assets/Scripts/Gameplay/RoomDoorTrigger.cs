using System;
using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Content;
using CryptKnight.Dungeon;
using CryptKnight.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.Gameplay
{
    public sealed class RoomDoorTrigger : MonoBehaviour
    {
        private const float MinimumEntryInputDot = 0.65f;

        private Action<RoomDirection> travel;
        private RoomDirection direction;

        public void Initialize(Action<RoomDirection> travelAction, RoomDirection doorDirection)
        {
            travel = travelAction;
            direction = doorDirection;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTravel(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryTravel(other);
        }

        private void TryTravel(Collider2D other)
        {
            if (travel == null || other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            // Require input toward the doorway so moving sideways through trigger doesn't cause room swap.
            if (!IsInputEnteringDoor(direction, playerController.MoveInput))
            {
                return;
            }

            travel(direction);
        }

        public static bool IsInputEnteringDoor(RoomDirection doorDirection, Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            return Vector2.Dot(moveInput.normalized, GetDoorEntryDirection(doorDirection)) >= MinimumEntryInputDot;
        }

        private static Vector2 GetDoorEntryDirection(RoomDirection doorDirection)
        {
            switch (doorDirection)
            {
                case RoomDirection.North:
                    return Vector2.up;
                case RoomDirection.East:
                    return Vector2.right;
                case RoomDirection.South:
                    return Vector2.down;
                case RoomDirection.West:
                    return Vector2.left;
                default:
                    return Vector2.zero;
            }
        }
    }

    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class FloorPortal : MonoBehaviour
    {
        private const string PortalSpritePath = "Art/Environment/purple_portal_animation_sheet_alpha";
        private const string PortalIdleSoundPath = "Audio/SFX/crypt-knight-sfx-portal-idle";
        private const float AnimationFrameSeconds = 0.12f;
        private const float IdleVolumeScale = 0.62f;

        private Action enterPortal;
        private CircleCollider2D interactionCollider;
        private SpriteRenderer portalRenderer;
        private Sprite[] animationFrames = Array.Empty<Sprite>();
        private AudioSource idleAudioSource;
        private GameObject promptRoot;
        private bool playerInRange;
        private bool spawnFinished;
        private float animationElapsed;
        private int animationFrameIndex;

        public bool IsUsed { get; private set; }
        public bool IsInteractable => spawnFinished && !IsUsed;
        public bool ShouldPlayIdleAudio => playerInRange && IsInteractable;
        public int AnimationFrameCount => animationFrames.Length;

        public void Initialize(Action enterPortalAction)
        {
            enterPortal = enterPortalAction ?? throw new ArgumentNullException(nameof(enterPortalAction));
            interactionCollider = GetComponent<CircleCollider2D>();
            interactionCollider.isTrigger = true;
            interactionCollider.radius = 0.85f;
            interactionCollider.enabled = false;
            BuildVisuals();
            ConfigureIdleAudio();
            GameAudioSettings.VolumesChanged += RefreshIdleVolume;

            if (animationFrames.Length <= 1)
            {
                FinishSpawn();
            }
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGameplayPaused)
            {
                AdvanceAnimation(Time.deltaTime);
            }

            if (!IsInteractable
                || !playerInRange
                || GameManager.Instance.IsGameplayPaused
                || GameplayInputGate.IsBlocked
                || !IsInteractPressed())
            {
                return;
            }

            TryActivate();
        }

        public bool AdvanceAnimation(float deltaTime)
        {
            if (animationFrames.Length <= 1 || IsUsed)
            {
                return false;
            }

            animationElapsed += Mathf.Max(0f, deltaTime);
            bool frameChanged = false;
            while (animationElapsed >= AnimationFrameSeconds)
            {
                animationElapsed -= AnimationFrameSeconds;
                animationFrameIndex++;
                frameChanged = true;

                if (!spawnFinished && animationFrameIndex >= animationFrames.Length)
                {
                    animationFrameIndex = 0;
                    FinishSpawn();
                }
                else
                {
                    animationFrameIndex %= animationFrames.Length;
                }
            }

            if (frameChanged)
            {
                portalRenderer.sprite = animationFrames[animationFrameIndex];
            }

            return frameChanged;
        }

        public bool TryActivate()
        {
            if (!IsInteractable || enterPortal == null)
            {
                return false;
            }

            IsUsed = true;
            playerInRange = false;
            StopIdleSound();
            GameSfxPlayer.PlayPortalEnter();
            if (interactionCollider != null)
            {
                interactionCollider.enabled = false;
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }

            enterPortal();
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            playerInRange = true;
            if (promptRoot != null && IsInteractable)
            {
                promptRoot.SetActive(true);
            }

            RefreshIdlePlayback();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            playerInRange = false;
            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }

            RefreshIdlePlayback();
        }

        private void BuildVisuals()
        {
            animationFrames = RuntimeAssetLoader.LoadSprites(PortalSpritePath);
            Array.Sort(animationFrames, (left, right) => string.CompareOrdinal(left.name, right.name));
            if (animationFrames.Length > 0)
            {
                GameObject visual = new GameObject("Animated Portal");
                visual.transform.SetParent(transform, false);
                portalRenderer = visual.AddComponent<SpriteRenderer>();
                portalRenderer.sprite = animationFrames[0];
                portalRenderer.sortingOrder = 8;
            }
            else
            {
                Debug.LogWarning($"Portal sprites could not be loaded from Resources/{PortalSpritePath}.");
            }

            promptRoot = new GameObject("Portal Prompt");
            promptRoot.transform.SetParent(transform, false);
            promptRoot.transform.localPosition = new Vector3(0f, 3f, 0f);

            SpriteRenderer background = promptRoot.AddComponent<SpriteRenderer>();
            background.sprite = CryptKnight.Loot.LootItemVisuals.GetSquareSprite();
            background.color = new Color(0.02f, 0.015f, 0.03f, 0.88f);
            background.sortingOrder = 31;
            promptRoot.transform.localScale = new Vector3(3.8f, 0.62f, 1f);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(promptRoot.transform, false);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = "Press E to enter Floor 2";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.055f;
            text.color = Color.white;
            text.GetComponent<MeshRenderer>().sortingOrder = 32;
            textObject.transform.localScale = new Vector3(0.263f, 1.613f, 1f);
            promptRoot.SetActive(false);
        }

        private void ConfigureIdleAudio()
        {
            idleAudioSource = gameObject.AddComponent<AudioSource>();
            idleAudioSource.playOnAwake = false;
            idleAudioSource.loop = true;
            idleAudioSource.spatialBlend = 0f;
            idleAudioSource.clip = Resources.Load<AudioClip>(PortalIdleSoundPath);
            RefreshIdleVolume();
        }

        private void FinishSpawn()
        {
            if (spawnFinished)
            {
                return;
            }

            spawnFinished = true;
            interactionCollider.enabled = true;
            if (playerInRange && promptRoot != null)
            {
                promptRoot.SetActive(true);
            }

            RefreshIdlePlayback();
        }

        private void RefreshIdlePlayback()
        {
            if (idleAudioSource == null)
            {
                return;
            }

            if (!ShouldPlayIdleAudio)
            {
                StopIdleSound();
                return;
            }

            if (UnityEngine.Application.isPlaying
                && idleAudioSource.clip != null
                && !idleAudioSource.isPlaying)
            {
                idleAudioSource.Play();
            }
        }

        private void RefreshIdleVolume()
        {
            if (idleAudioSource != null)
            {
                idleAudioSource.volume = IdleVolumeScale * GameAudioSettings.GameSoundsVolume;
            }
        }

        private void StopIdleSound()
        {
            if (idleAudioSource != null)
            {
                idleAudioSource.Stop();
            }
        }

        private void OnDestroy()
        {
            GameAudioSettings.VolumesChanged -= RefreshIdleVolume;
            StopIdleSound();
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
