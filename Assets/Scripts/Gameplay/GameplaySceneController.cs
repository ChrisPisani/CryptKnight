using System.Collections.Generic;
using CryptKnight.Application;
using CryptKnight.Combat;
using CryptKnight.Data;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using CryptKnight.Player;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    public sealed class GameplaySceneController : MonoBehaviour
    {
        private const float RoomWidth = 13.5f;
        private const float RoomHeight = 7.5f;
        private const float WallThickness = 0.75f;
        private static readonly Color DoorColor = new Color(0.55f, 0.40f, 0.18f, 1f);
        private static readonly Color FinalRoomDoorColor = new Color(1f, 0.10f, 0.86f, 1f);

        private static GameplaySceneController instance;
        private static Sprite squareSprite;

        private GameObject gameplayRoot;
        private GameObject roomRoot;
        private DungeonRoomNavigator roomNavigator;
        private Transform playerTransform;

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

            GameRunState runState = GameManager.Instance.CurrentRun;
            DungeonLayout dungeonLayout = DungeonLayoutGenerator.Generate(runState.DungeonWidth, runState.DungeonHeight, runState.Seed);
            roomNavigator = new DungeonRoomNavigator(dungeonLayout);

            gameplayRoot = new GameObject("Runtime Gameplay Scene");
            roomRoot = new GameObject("Active Room");
            roomRoot.transform.SetParent(gameplayRoot.transform, false);
            playerTransform = CreatePlayer(gameplayRoot.transform);
            BuildCurrentRoom(Vector2.zero);
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
            roomRoot = null;
            roomNavigator = null;
            playerTransform = null;
        }

        public void TravelThroughDoor(RoomDirection direction)
        {
            if (roomNavigator == null || !roomNavigator.TryMove(direction))
            {
                return;
            }

            BuildCurrentRoom(GetSpawnPositionForEntry(direction));
        }

        private void BuildCurrentRoom(Vector2 playerSpawnPosition)
        {
            ClearRoomInstance();
            CreateRoomFloor(roomRoot.transform);
            CreateRoomWalls(roomRoot.transform);
            CreateDoors(roomRoot.transform, roomNavigator.CurrentRoom);
            CreateRoomContents(roomRoot.transform, roomNavigator.CurrentRoom);

            if (playerTransform != null)
            {
                playerTransform.position = playerSpawnPosition;
            }
        }

        private void ClearRoomInstance()
        {
            if (roomRoot == null)
            {
                return;
            }

            ClearActiveProjectiles();

            for (int i = roomRoot.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = roomRoot.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private static void ClearActiveProjectiles()
        {
            ProjectileController[] projectiles = FindObjectsByType<ProjectileController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < projectiles.Length; i++)
            {
                projectiles[i].gameObject.SetActive(false);
                Destroy(projectiles[i].gameObject);
            }
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

        private void CreateDoors(Transform parent, DungeonRoom room)
        {
            foreach (KeyValuePair<RoomDirection, Vector2Int> connection in room.Connections)
            {
                bool leadsToFinalRoom = roomNavigator != null && roomNavigator.Layout.FinalPosition == connection.Value;
                CreateDoor(parent, connection.Key, leadsToFinalRoom);
            }
        }

        private void CreateDoor(Transform parent, RoomDirection direction, bool leadsToFinalRoom)
        {
            Vector2 position = GetDoorPosition(direction);
            Vector2 size = IsHorizontalDoor(direction) ? new Vector2(1.6f, 0.5f) : new Vector2(0.5f, 1.6f);
            GameObject door = CreateSpriteObject($"Door {direction}", parent, position, size, leadsToFinalRoom ? FinalRoomDoorColor : DoorColor);
            door.GetComponent<SpriteRenderer>().sortingOrder = 2;

            BoxCollider2D trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            door.AddComponent<RoomDoorTrigger>().Initialize(this, direction);
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

        private void CreateRoomContents(Transform parent, DungeonRoom room)
        {
            switch (room.RoomType)
            {
                case RoomType.Enemy:
                    CreateTestEnemy(parent, playerTransform);
                    break;
                case RoomType.Trap:
                    CreateTrapMarker(parent);
                    break;
                case RoomType.Final:
                    CreateFinalRoomMarker(parent);
                    break;
                case RoomType.Starter:
                    CreateTestLootPickups(parent);
                    break;
            }
        }

        private void CreateTrapMarker(Transform parent)
        {
            GameObject trap = CreateSpriteObject("Trap Room Marker", parent, Vector2.zero, new Vector2(2.0f, 0.35f), new Color(0.85f, 0.12f, 0.08f, 1f));
            trap.GetComponent<SpriteRenderer>().sortingOrder = 3;
            trap.AddComponent<BoxCollider2D>().isTrigger = true;
        }

        private void CreateFinalRoomMarker(Transform parent)
        {
            GameObject finalMarker = CreateSpriteObject("Final Room Marker", parent, Vector2.zero, new Vector2(1.4f, 1.4f), new Color(0.95f, 0.76f, 0.20f, 1f));
            finalMarker.GetComponent<SpriteRenderer>().sortingOrder = 3;
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

        private static Vector2 GetDoorPosition(RoomDirection direction)
        {
            float halfWidth = RoomWidth * 0.5f;
            float halfHeight = RoomHeight * 0.5f;
            switch (direction)
            {
                case RoomDirection.North:
                    return new Vector2(0f, halfHeight - 0.35f);
                case RoomDirection.East:
                    return new Vector2(halfWidth - 0.35f, 0f);
                case RoomDirection.South:
                    return new Vector2(0f, -halfHeight + 0.35f);
                case RoomDirection.West:
                    return new Vector2(-halfWidth + 0.35f, 0f);
                default:
                    return Vector2.zero;
            }
        }

        private static Vector2 GetSpawnPositionForEntry(RoomDirection exitDirection)
        {
            float halfWidth = RoomWidth * 0.5f;
            float halfHeight = RoomHeight * 0.5f;
            switch (exitDirection)
            {
                case RoomDirection.North:
                    return new Vector2(0f, -halfHeight + 1.2f);
                case RoomDirection.East:
                    return new Vector2(-halfWidth + 1.2f, 0f);
                case RoomDirection.South:
                    return new Vector2(0f, halfHeight - 1.2f);
                case RoomDirection.West:
                    return new Vector2(halfWidth - 1.2f, 0f);
                default:
                    return Vector2.zero;
            }
        }

        private static bool IsHorizontalDoor(RoomDirection direction)
        {
            return direction == RoomDirection.North || direction == RoomDirection.South;
        }

    }
}
