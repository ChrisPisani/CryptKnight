using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CryptKnight.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerIdleAnimator : MonoBehaviour
    {
        private const string KnightSpritePath = "Assets/Art/Player/knight.png";
        private const float FrameDuration = 0.14f;
        private static readonly string[] FrameNames =
        {
            "knight_0",
            "knight_1",
            "knight_2",
            "knight_3",
            "knight_4",
            "knight_5",
            "knight_6",
            "knight_7"
        };

        private readonly List<Sprite> idleFrames = new List<Sprite>();
        private SpriteRenderer spriteRenderer;
        private float elapsed;
        private int frameIndex;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadIdleFrames();

            if (idleFrames.Count > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private void Update()
        {
            if (idleFrames.Count <= 1)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < FrameDuration)
            {
                return;
            }

            elapsed -= FrameDuration;
            frameIndex = (frameIndex + 1) % idleFrames.Count;
            spriteRenderer.sprite = idleFrames[frameIndex];
        }

        private void LoadIdleFrames()
        {
#if UNITY_EDITOR
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(KnightSpritePath);
            Dictionary<string, Sprite> spritesByName = new Dictionary<string, Sprite>();

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name.StartsWith("knight_", StringComparison.Ordinal) && sprite.rect.width > 1f && sprite.rect.height > 1f)
                {
                    spritesByName[sprite.name] = sprite;
                }
            }

            for (int i = 0; i < FrameNames.Length; i++)
            {
                if (spritesByName.TryGetValue(FrameNames[i], out Sprite sprite))
                {
                    idleFrames.Add(sprite);
                }
            }
#endif
        }
    }
}
