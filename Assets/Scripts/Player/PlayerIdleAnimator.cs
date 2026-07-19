using System;
using System.Collections.Generic;
using CryptKnight.Content;
using UnityEngine;

namespace CryptKnight.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerIdleAnimator : MonoBehaviour
    {
        private const string PlayerSpriteSheetPath = "Art/Player/player_sheet_4x8";
        private const float WalkFrameDuration = 0.14f;
        private const float AttackFrameDuration = 0.09f;
        private const int FramesPerDirection = 4;

        private readonly Dictionary<CardinalDirection, List<Sprite>> walkFramesByDirection = new Dictionary<CardinalDirection, List<Sprite>>();
        private readonly Dictionary<CardinalDirection, List<Sprite>> attackFramesByDirection = new Dictionary<CardinalDirection, List<Sprite>>();
        private SpriteRenderer spriteRenderer;
        private CardinalDirection facingDirection = CardinalDirection.Down;
        private float elapsed;
        private int frameIndex;
        private bool isMoving;
        private bool isAttacking;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadAnimationFrames();

            if (TryGetFrames(walkFramesByDirection, facingDirection, out List<Sprite> initialFrames))
            {
                spriteRenderer.sprite = initialFrames[0];
            }
        }

        private void Update()
        {
            if (isAttacking)
            {
                AdvanceAttackAnimation();
                return;
            }

            if (!TryGetFrames(walkFramesByDirection, facingDirection, out List<Sprite> frames))
            {
                return;
            }

            if (!isMoving)
            {
                frameIndex = 0;
                spriteRenderer.sprite = frames[frameIndex];
                return;
            }

            AdvanceLoopingAnimation(frames, WalkFrameDuration);
        }

        public void SetMovement(Vector2 movement)
        {
            isMoving = movement.sqrMagnitude > 0.001f;
            // Attack animations are facing attack direction until their frames finish.
            if (!isMoving || isAttacking)
            {
                return;
            }

            CardinalDirection nextDirection = GetCardinalDirection(movement);
            if (nextDirection != facingDirection)
            {
                facingDirection = nextDirection;
                ResetFrameTimer();
            }
        }

        public void PlayAttack(Vector2 direction)
        {
            facingDirection = GetCardinalDirection(direction);
            isAttacking = true;
            ResetFrameTimer();

            if (TryGetFrames(attackFramesByDirection, facingDirection, out List<Sprite> frames))
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        public static CardinalDirection GetCardinalDirection(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return direction.x < 0f ? CardinalDirection.Left : CardinalDirection.Right;
            }

            return direction.y < 0f ? CardinalDirection.Down : CardinalDirection.Up;
        }

        private void AdvanceAttackAnimation()
        {
            if (!TryGetFrames(attackFramesByDirection, facingDirection, out List<Sprite> frames))
            {
                isAttacking = false;
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < AttackFrameDuration)
            {
                return;
            }

            elapsed -= AttackFrameDuration;
            frameIndex++;

            if (frameIndex >= frames.Count)
            {
                isAttacking = false;
                frameIndex = 0;
                return;
            }

            spriteRenderer.sprite = frames[frameIndex];
        }

        private void AdvanceLoopingAnimation(List<Sprite> frames, float frameDuration)
        {
            if (frames.Count <= 1)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < frameDuration)
            {
                return;
            }

            elapsed -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Count;
            spriteRenderer.sprite = frames[frameIndex];
        }

        private void ResetFrameTimer()
        {
            elapsed = 0f;
            frameIndex = 0;
        }

        private void LoadAnimationFrames()
        {
            Sprite[] assets = RuntimeAssetLoader.LoadSprites(PlayerSpriteSheetPath);
            Dictionary<string, Sprite> spritesByName = new Dictionary<string, Sprite>();

            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i];
                if (sprite.name.StartsWith("player_", StringComparison.Ordinal) && sprite.rect.width > 1f && sprite.rect.height > 1f)
                {
                    spritesByName[sprite.name] = sprite;
                }
            }

            AddFrames(spritesByName, walkFramesByDirection, CardinalDirection.Down, "player_walk_down");
            AddFrames(spritesByName, walkFramesByDirection, CardinalDirection.Left, "player_walk_left");
            AddFrames(spritesByName, walkFramesByDirection, CardinalDirection.Right, "player_walk_right");
            AddFrames(spritesByName, walkFramesByDirection, CardinalDirection.Up, "player_walk_up");
            AddFrames(spritesByName, attackFramesByDirection, CardinalDirection.Down, "player_attack_down");
            AddFrames(spritesByName, attackFramesByDirection, CardinalDirection.Left, "player_attack_left");
            AddFrames(spritesByName, attackFramesByDirection, CardinalDirection.Right, "player_attack_right");
            AddFrames(spritesByName, attackFramesByDirection, CardinalDirection.Up, "player_attack_up");
        }

        private static void AddFrames(
            Dictionary<string, Sprite> spritesByName,
            Dictionary<CardinalDirection, List<Sprite>> destination,
            CardinalDirection direction,
            string prefix)
        {
            List<Sprite> frames = new List<Sprite>();
            for (int i = 0; i < FramesPerDirection; i++)
            {
                if (spritesByName.TryGetValue($"{prefix}_{i}", out Sprite sprite))
                {
                    frames.Add(sprite);
                }
            }

            if (frames.Count > 0)
            {
                destination[direction] = frames;
            }
        }

        private static bool TryGetFrames(
            Dictionary<CardinalDirection, List<Sprite>> framesByDirection,
            CardinalDirection direction,
            out List<Sprite> frames)
        {
            return framesByDirection.TryGetValue(direction, out frames) && frames.Count > 0;
        }
    }

    public enum CardinalDirection
    {
        Down,
        Left,
        Right,
        Up
    }
}
