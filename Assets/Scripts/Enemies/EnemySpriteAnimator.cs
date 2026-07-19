using System.Collections.Generic;
using CryptKnight.Content;
using CryptKnight.Player;
using UnityEngine;

namespace CryptKnight.Enemies
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemySpriteAnimator : MonoBehaviour
    {
        private const float MoveFrameDuration = 0.16f;
        private const float AttackFrameDuration = 0.22f;
        private const int FramesPerRow = 4;
        private const float MinFrameScaleMultiplier = 0.85f;
        private const float MaxFrameScaleMultiplier = 1.20f;
        private const float SpiderWalkDownScaleMultiplier = 0.80f;

        private static readonly Dictionary<EnemyKind, Dictionary<string, Sprite>> cachedFrames = new Dictionary<EnemyKind, Dictionary<string, Sprite>>();

        private readonly Dictionary<CardinalDirection, List<Sprite>> moveFrames = new Dictionary<CardinalDirection, List<Sprite>>();
        private readonly Dictionary<CardinalDirection, List<Sprite>> attackFrames = new Dictionary<CardinalDirection, List<Sprite>>();
        private readonly Dictionary<Sprite, float> frameScaleMultipliers = new Dictionary<Sprite, float>();
        private readonly Dictionary<Sprite, Vector3> frameLocalOffsets = new Dictionary<Sprite, Vector3>();
        private SpriteRenderer spriteRenderer;
        private EnemyKind enemyKind;
        private Vector3 baseLocalScale = Vector3.one;
        private Vector3 baseLocalPosition;
        private CardinalDirection facingDirection = CardinalDirection.Down;
        private float elapsed;
        private int frameIndex;
        private bool isMoving;
        private bool isAttacking;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (isAttacking)
            {
                AdvanceAttackAnimation();
                return;
            }

            if (!TryGetFrames(moveFrames, facingDirection, out List<Sprite> frames))
            {
                return;
            }

            if (!isMoving)
            {
                frameIndex = 0;
                ApplySprite(frames[0]);
                return;
            }

            AdvanceLoopingAnimation(frames, MoveFrameDuration);
        }

        public void Initialize(EnemyKind kind)
        {
            enemyKind = kind;
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            baseLocalScale = transform.localScale;
            baseLocalPosition = transform.localPosition;
            moveFrames.Clear();
            attackFrames.Clear();
            frameScaleMultipliers.Clear();
            frameLocalOffsets.Clear();

            Dictionary<string, Sprite> loadedFrames = GetFrames(kind);
            BuildFrameAdjustments(loadedFrames);

            AddDirectionFrames(kind, moveFrames, CardinalDirection.Down, false);
            AddDirectionFrames(kind, moveFrames, CardinalDirection.Right, false);
            AddDirectionFrames(kind, moveFrames, CardinalDirection.Left, false);
            AddDirectionFrames(kind, moveFrames, CardinalDirection.Up, false);
            AddDirectionFrames(kind, attackFrames, CardinalDirection.Down, true);
            AddDirectionFrames(kind, attackFrames, CardinalDirection.Right, true);
            AddDirectionFrames(kind, attackFrames, CardinalDirection.Left, true);
            AddDirectionFrames(kind, attackFrames, CardinalDirection.Up, true);

            if (TryGetFrames(moveFrames, facingDirection, out List<Sprite> frames))
            {
                ApplySprite(frames[0]);
            }
        }

        public void SetMovement(Vector2 movement)
        {
            isMoving = movement.sqrMagnitude > 0.001f;
            if (!isMoving || isAttacking)
            {
                return;
            }

            CardinalDirection nextDirection = GetAnimationDirection(movement);
            if (nextDirection != facingDirection)
            {
                facingDirection = nextDirection;
                ResetFrameTimer();
            }
        }

        public void PlayAttack(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                facingDirection = GetAnimationDirection(direction);
            }

            isAttacking = true;
            ResetFrameTimer();
            if (TryGetFrames(attackFrames, facingDirection, out List<Sprite> frames))
            {
                ApplySprite(frames[0]);
            }
        }

        private void AddDirectionFrames(
            EnemyKind kind,
            Dictionary<CardinalDirection, List<Sprite>> destination,
            CardinalDirection direction,
            bool isAttack)
        {
            Dictionary<string, Sprite> framesByName = GetFrames(kind);
            string prefix = GetFramePrefix(kind, direction, isAttack);
            List<Sprite> rowFrames = new List<Sprite>(FramesPerRow);
            for (int frame = 0; frame < FramesPerRow; frame++)
            {
                if (framesByName.TryGetValue($"{prefix}_{frame}", out Sprite sprite))
                {
                    rowFrames.Add(sprite);
                }
            }

            if (rowFrames.Count > 0)
            {
                destination[direction] = rowFrames;
            }
        }

        private void AdvanceAttackAnimation()
        {
            if (!TryGetFrames(attackFrames, facingDirection, out List<Sprite> frames))
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

            ApplySprite(frames[frameIndex]);
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
            ApplySprite(frames[frameIndex]);
        }

        private void ResetFrameTimer()
        {
            elapsed = 0f;
            frameIndex = 0;
        }

        private CardinalDirection GetAnimationDirection(Vector2 direction)
        {
            if (enemyKind == EnemyKind.Spider && Mathf.Abs(direction.x) > 0.001f)
            {
                return direction.x < 0f ? CardinalDirection.Left : CardinalDirection.Right;
            }

            return PlayerIdleAnimator.GetCardinalDirection(direction);
        }

        private void ApplySprite(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
            if (sprite != null && frameScaleMultipliers.TryGetValue(sprite, out float scaleMultiplier))
            {
                transform.localScale = baseLocalScale * scaleMultiplier;
                transform.localPosition = baseLocalPosition + frameLocalOffsets[sprite];
                return;
            }

            transform.localScale = baseLocalScale;
            transform.localPosition = baseLocalPosition;
        }

        private void BuildFrameAdjustments(Dictionary<string, Sprite> framesByName)
        {
            float totalFootprint = 0f;
            int spriteCount = 0;
            foreach (Sprite sprite in framesByName.Values)
            {
                float footprint = GetSpriteFootprint(sprite);
                if (footprint <= 0f)
                {
                    continue;
                }

                totalFootprint += footprint;
                spriteCount++;
            }

            if (spriteCount == 0)
            {
                return;
            }

            float targetFootprint = totalFootprint / spriteCount;
            foreach (Sprite sprite in framesByName.Values)
            {
                float footprint = GetSpriteFootprint(sprite);
                if (footprint <= 0f)
                {
                    continue;
                }

                // Frame slices are not always perfectly centered or equally full, so we correct the visual child only.
                float scaleMultiplier = Mathf.Clamp(
                    targetFootprint / footprint,
                    MinFrameScaleMultiplier,
                    MaxFrameScaleMultiplier);
                scaleMultiplier *= GetManualFrameScaleMultiplier(sprite);
                frameScaleMultipliers[sprite] = scaleMultiplier;
                frameLocalOffsets[sprite] = GetCenteredLocalOffset(sprite, scaleMultiplier);
            }
        }

        private static float GetManualFrameScaleMultiplier(Sprite sprite)
        {
            if (sprite != null && sprite.name.StartsWith("spider_walk_down_"))
            {
                return SpiderWalkDownScaleMultiplier;
            }

            return 1f;
        }

        private Vector3 GetCenteredLocalOffset(Sprite sprite, float scaleMultiplier)
        {
            Vector3 scaledCenter = Vector3.Scale(
                sprite.bounds.center,
                new Vector3(baseLocalScale.x * scaleMultiplier, baseLocalScale.y * scaleMultiplier, 1f));
            return -scaledCenter;
        }

        private static float GetSpriteFootprint(Sprite sprite)
        {
            if (sprite == null)
            {
                return 0f;
            }

            Vector3 size = sprite.bounds.size;
            return Mathf.Sqrt(Mathf.Max(0f, size.x * size.y));
        }

        private static bool TryGetFrames(
            Dictionary<CardinalDirection, List<Sprite>> framesByDirection,
            CardinalDirection direction,
            out List<Sprite> frames)
        {
            return framesByDirection.TryGetValue(direction, out frames) && frames.Count > 0;
        }

        private static string GetFramePrefix(EnemyKind kind, CardinalDirection direction, bool isAttack)
        {
            string enemyPrefix = kind == EnemyKind.Zombie ? "zombie" : "spider";
            string actionPrefix = isAttack ? "attack" : "walk";
            return $"{enemyPrefix}_{actionPrefix}_{GetDirectionName(direction)}";
        }

        private static string GetDirectionName(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.Down:
                    return "down";
                case CardinalDirection.Right:
                    return "right";
                case CardinalDirection.Left:
                    return "left";
                case CardinalDirection.Up:
                    return "up";
                default:
                    return "down";
            }
        }

        private static Dictionary<string, Sprite> GetFrames(EnemyKind kind)
        {
            if (cachedFrames.TryGetValue(kind, out Dictionary<string, Sprite> frames))
            {
                return frames;
            }

            Dictionary<string, Sprite> loadedFrames = LoadFrames(kind);
            cachedFrames[kind] = loadedFrames;
            return loadedFrames;
        }

        private static Dictionary<string, Sprite> LoadFrames(EnemyKind kind)
        {
            Dictionary<string, Sprite> framesByName = new Dictionary<string, Sprite>();
            string path = kind == EnemyKind.Zombie
                ? "Art/Enemies/zombie_enemy_combined_4x8"
                : "Art/Enemies/spider_enemy_all_rows_4x8_transparent";
            Sprite[] assets = RuntimeAssetLoader.LoadSprites(path);
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite sprite = assets[i];
                framesByName[sprite.name] = sprite;
            }
            return framesByName;
        }
    }
}
