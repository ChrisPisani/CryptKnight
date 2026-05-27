using CryptKnight.Application;
using CryptKnight.Data;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using CryptKnight.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.Gameplay
{
    public sealed class GameplaySceneController : MonoBehaviour
    {
        private const float RoomWidth = 13.5f;
        private const float RoomHeight = 7.5f;
        private const float WallThickness = 0.75f;

        private static GameplaySceneController instance;
        private static Sprite squareSprite;

        private GameObject gameplayRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateController()
        {
            if (instance != null)
            {
                return;
            }

            GameObject controllerObject = new GameObject("Gameplay Scene Controller");
            instance = controllerObject.AddComponent<GameplaySceneController>();
            DontDestroyOnLoad(controllerObject);
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

        private void Update()
        {
            if (GameManager.Instance.CurrentRun == null || !GameManager.Instance.CurrentRun.IsActive)
            {
                return;
            }

            if (IsQuitPressed())
            {
                GameManager.Instance.QuitCurrentRun();
            }
        }

        private void HandleRunStateChanged(GameRunState runState)
        {
            if (runState != null && runState.IsActive)
            {
                if (gameplayRoot == null)
                {
                    BuildGameplayScene();
                }

                return;
            }

            ClearGameplayScene();
        }

        private void BuildGameplayScene()
        {
            ClearGameplayScene();

            // This is a temporary room scene, will be replaced by dungeon and room generation later
            gameplayRoot = new GameObject("Runtime Gameplay Scene");
            CreateRoomFloor(gameplayRoot.transform);
            CreateRoomWalls(gameplayRoot.transform);
            Transform player = CreatePlayer(gameplayRoot.transform);
            CreateTestEnemy(gameplayRoot.transform, player);
            CreateTestLootPickups(gameplayRoot.transform);
            FrameCamera();
        }

        private void ClearGameplayScene()
        {
            if (gameplayRoot == null)
            {
                return;
            }

            Destroy(gameplayRoot);
            gameplayRoot = null;
        }

        private void CreateRoomFloor(Transform parent)
        {
            GameObject floor = CreateSpriteObject("Room Floor", parent, new Vector2(0f, 0f), new Vector2(RoomWidth, RoomHeight), new Color(0.11f, 0.12f, 0.13f, 1f));
            floor.GetComponent<SpriteRenderer>().sortingOrder = -10;
            floor.AddComponent<BoxCollider2D>().isTrigger = true;
        }

        private void CreateRoomWalls(Transform parent)
        {
            Color wallColor = new Color(0.30f, 0.27f, 0.25f, 1f);
            float halfWidth = RoomWidth * 0.5f;
            float halfHeight = RoomHeight * 0.5f;

            CreateWall(parent, "North Wall", new Vector2(0f, halfHeight + WallThickness * 0.5f), new Vector2(RoomWidth + WallThickness * 2f, WallThickness), wallColor);
            CreateWall(parent, "South Wall", new Vector2(0f, -halfHeight - WallThickness * 0.5f), new Vector2(RoomWidth + WallThickness * 2f, WallThickness), wallColor);
            CreateWall(parent, "West Wall", new Vector2(-halfWidth - WallThickness * 0.5f, 0f), new Vector2(WallThickness, RoomHeight), wallColor);
            CreateWall(parent, "East Wall", new Vector2(halfWidth + WallThickness * 0.5f, 0f), new Vector2(WallThickness, RoomHeight), wallColor);
        }

        private void CreateWall(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject wall = CreateSpriteObject(objectName, parent, position, size, color);
            wall.GetComponent<SpriteRenderer>().sortingOrder = 0;
            wall.AddComponent<BoxCollider2D>();
        }

        private Transform CreatePlayer(Transform parent)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.transform.position = Vector2.zero;

            // This allows sprites to scale independently from the player's physics collider.
            GameObject visual = CreateSpriteObject("Player Visual", player.transform, new Vector2(0f, 0.16f), new Vector2(3f, 3f), Color.white);
            visual.GetComponent<SpriteRenderer>().sortingOrder = 10;
            visual.AddComponent<PlayerIdleAnimator>();

            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CircleCollider2D>().radius = 0.35f;
            player.AddComponent<PlayerDamageReceiver>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerAttackController>();
            return player.transform;
        }

        private void CreateTestEnemy(Transform parent, Transform player)
        {
            GameObject enemy = CreateSpriteObject("Test Enemy", parent, new Vector2(4f, 0f), new Vector2(0.8f, 0.8f), new Color(0.72f, 0.18f, 0.22f, 1f));
            enemy.GetComponent<SpriteRenderer>().sortingOrder = 9;
            enemy.AddComponent<CircleCollider2D>().radius = 0.45f;
            enemy.AddComponent<EnemyHealth>();
            enemy.AddComponent<TestEnemyShooter>().Initialize(player);
        }

        private void CreateTestLootPickups(Transform parent)
        {
            LootTableConfiguration lootTable = LootTableConfiguration.CreateDefault();
            Vector2[] spawnPositions =
            {
                new Vector2(-5.0f, 2.45f),
                new Vector2(-3.1f, 1.45f),
                new Vector2(-1.2f, 2.45f),
                new Vector2(2.1f, 2.45f),
                new Vector2(4.2f, 1.45f)
            };

            int pickupCount = Mathf.Min(lootTable.Items.Count, spawnPositions.Length);
            for (int i = 0; i < pickupCount; i++)
            {
                CreateLootPickup(parent, lootTable.Items[i], spawnPositions[i]);
            }
        }

        private void CreateLootPickup(Transform parent, LootItemDefinition itemDefinition, Vector2 position)
        {
            GameObject pickupObject = new GameObject($"Pickup {itemDefinition.DisplayName}");
            pickupObject.transform.SetParent(parent, false);
            pickupObject.transform.position = position;

            pickupObject.AddComponent<SpriteRenderer>();
            pickupObject.AddComponent<CircleCollider2D>();
            pickupObject.AddComponent<LootPickup>().Initialize(itemDefinition);
        }

        private static GameObject CreateSpriteObject(string objectName, Transform parent, Vector2 position, Vector2 scale, Color color)
        {
            GameObject spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.position = position;
            spriteObject.transform.localScale = scale;

            SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSquareSprite();
            spriteRenderer.color = color;

            return spriteObject;
        }

        private static Sprite GetSquareSprite()
        {
            if (squareSprite != null)
            {
                return squareSprite;
            }
            
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return squareSprite;
        }

        private static void FrameCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.045f, 0.05f, 1f);
        }

        private static bool IsQuitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
