using System.Collections;
using CryptKnight.Application;
using CryptKnight.Audio;
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
        private const float PlayerVisualScale = 0.85f;
        private const float PlayerColliderRadius = 0.35f;
        private const float ZombiePhaseSpreadSeconds = 2.0f;
        private const float SpiderPhaseSpreadSeconds = 1.5f;
        private static readonly Vector2 PlayerVisualOffset = new Vector2(0f, 0.08f);
        private static readonly Vector2 EnemyDropOffset = new Vector2(0f, 0.85f);
        private static readonly Vector2 RoomClearDropOffset = new Vector2(-0.85f, 0.85f);

        private static GameplaySceneController instance;
        private static Sprite squareSprite;

        private GameObject gameplayRoot;
        private GameObject roomRoot;
        private DungeonRunState dungeonRun;
        private DungeonRoomNavigator roomNavigator;
        private Transform playerTransform;
        private LootTableConfiguration lootConfiguration;
        private LootSystem lootSystem;
        private System.Random lootRandom;
        private GameplayMusicController musicController;
        private FinalEncounterController finalEncounterController;
        private Coroutine runEndRoutine;
        private readonly DungeonRoomEnvironmentBuilder environmentBuilder = new DungeonRoomEnvironmentBuilder();

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
            musicController = GetComponent<GameplayMusicController>() ?? gameObject.AddComponent<GameplayMusicController>();
            GameAudioSettings.VolumesChanged += HandleAudioSettingsChanged;
            GameManager.Instance.RunStateChanged += HandleRunStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
            {
                GameManager.Instance.RunStateChanged -= HandleRunStateChanged;
            }

            GameAudioSettings.VolumesChanged -= HandleAudioSettingsChanged;
        }

        private void HandleRunStateChanged(GameRunState runState)
        {
            if (runState != null && runState.IsActive)
            {
                if (runEndRoutine != null)
                {
                    StopCoroutine(runEndRoutine);
                    runEndRoutine = null;
                }

                if (gameplayRoot == null)
                {
                    BuildGameplayScene();
                }

                return;
            }

            bool showRunResult = runState != null &&
                (runState.Status == GameRunStatus.Completed || runState.Status == GameRunStatus.Failed);
            if (showRunResult && gameplayRoot != null)
            {
                musicController?.StopMusic();
                FreezeGameplayForResult();
                if (runEndRoutine == null)
                {
                    runEndRoutine = StartCoroutine(ClearGameplayAfterFade());
                }

                return;
            }

            ClearGameplayScene();
        }

        private IEnumerator ClearGameplayAfterFade()
        {
            yield return new WaitForSecondsRealtime(RunEndTransitionTiming.FadeToBlackSeconds);
            runEndRoutine = null;
            ClearGameplayScene();
        }

        private void FreezeGameplayForResult()
        {
            MonoBehaviour[] behaviours = gameplayRoot.GetComponentsInChildren<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }

            Rigidbody2D[] bodies = gameplayRoot.GetComponentsInChildren<Rigidbody2D>();
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].simulated = false;
            }
        }

        private void BuildGameplayScene()
        {
            ClearGameplayScene();
            musicController.Configure();

            GameRunState runState = GameManager.Instance.CurrentRun;
            if (runState.Dungeon == null)
            {
                runState.InitializeDungeon(DungeonRunStateFactory.Create(runState.DungeonWidth, runState.DungeonHeight, runState.Seed));
            }

            dungeonRun = runState.Dungeon;
            roomNavigator = dungeonRun.Navigator;
            lootConfiguration = dungeonRun.LootConfiguration;
            lootSystem = dungeonRun.LootSystem;
            lootRandom = dungeonRun.LootRandom;

            // The layout stores room data
            gameplayRoot = new GameObject("Runtime Gameplay Scene");
            roomRoot = new GameObject("Active Room");
            roomRoot.transform.SetParent(gameplayRoot.transform, false);
            playerTransform = CreatePlayer(gameplayRoot.transform);
            BuildCurrentRoom(Vector2.zero);
            FrameCamera();
        }

        private void ClearGameplayScene()
        {
            musicController?.StopMusic();

            if (gameplayRoot != null)
            {
                Destroy(gameplayRoot);
            }

            gameplayRoot = null;
            roomRoot = null;
            roomNavigator = null;
            dungeonRun = null;
            playerTransform = null;
            lootConfiguration = null;
            lootSystem = null;
            lootRandom = null;
            finalEncounterController = null;
        }

        public void TravelThroughDoor(RoomDirection direction)
        {
            TryTravelThroughDoor(direction);
        }

        public bool CanTravelFromCurrentRoom()
        {
            if (roomNavigator == null)
            {
                return false;
            }

            DungeonRoomRuntimeState roomState = dungeonRun.CurrentRoomState;
            return roomState.RoomType != RoomType.Final && !roomState.IsLocked;
        }

        public bool TryTravelThroughDoor(RoomDirection direction)
        {
            // Combat/trap rooms stay locked until their state says the room is clear.
            if (!CanTravelFromCurrentRoom())
            {
                return false;
            }

            if (roomNavigator == null || !roomNavigator.TryMove(direction))
            {
                return false;
            }

            BuildCurrentRoom(GetSpawnPositionForEntry(direction));
            return true;
        }

        private void BuildCurrentRoom(Vector2 playerSpawnPosition)
        {
            // Rebuild the room so only one room instance is active at a time.
            ClearRoomInstance();
            DungeonRoom currentRoom = roomNavigator.CurrentRoom;
            DungeonRoomRuntimeState roomState = dungeonRun.GetRoomState(currentRoom.GridPosition);
            environmentBuilder.Build(roomRoot.transform, currentRoom, dungeonRun.Layout, TravelThroughDoor);
            CreateRoomContents(roomRoot.transform, currentRoom, roomState);
            if (currentRoom.RoomType == RoomType.Final)
            {
                StartFinalEncounter(roomRoot.transform, roomState);
            }

            musicController?.TransitionTo(currentRoom.RoomType);

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

            // Projectiles may be linked to player/enemy, so clear them separately on room swaps.
            ClearActiveProjectiles();
            finalEncounterController = null;

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

        private void HandleAudioSettingsChanged()
        {
            musicController?.RefreshVolume();
        }

        private Transform CreatePlayer(Transform parent)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.transform.position = Vector2.zero;

            // This allows sprites to scale independently from the player's physics collider.
            GameObject visual = CreateSpriteObject("Player Visual", player.transform, PlayerVisualOffset, new Vector2(PlayerVisualScale, PlayerVisualScale), Color.white);
            visual.GetComponent<SpriteRenderer>().sortingOrder = 10;
            visual.AddComponent<PlayerIdleAnimator>();

            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CircleCollider2D>().radius = PlayerColliderRadius;
            player.AddComponent<PlayerDamageReceiver>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerAttackController>();
            return player.transform;
        }

        private void CreateRoomEnemies(Transform parent, DungeonRoomRuntimeState roomState)
        {
            for (int i = 0; i < roomState.Enemies.Count; i++)
            {
                RoomEnemyInstance enemyInstance = roomState.Enemies[i];
                if (!enemyInstance.IsDefeated)
                {
                    CreateEnemy(parent, roomState, enemyInstance);
                }
            }
        }

        private void CreateEnemy(Transform parent, DungeonRoomRuntimeState roomState, RoomEnemyInstance enemyInstance)
        {
            GameObject enemy = new GameObject($"{enemyInstance.Kind} Enemy");
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = enemyInstance.Position;

            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = enemyInstance.Kind == EnemyKind.Spider ? 0.55f : 0.42f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(enemy.transform, false);
            visual.transform.localScale = enemyInstance.Kind == EnemyKind.Spider
                ? new Vector3(0.70f, 0.70f, 1f)
                : new Vector3(1.60f, 1.60f, 1f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 9;
            EnemySpriteAnimator animator = visual.AddComponent<EnemySpriteAnimator>();
            animator.Initialize(enemyInstance.Kind);

            EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();
            enemyHealth.Initialize(enemyInstance.MaxHealth, enemyInstance.CurrentHealth);
            enemy.AddComponent<EnemyRoomStateTracker>().Initialize(enemyInstance, enemyHealth);
            enemyHealth.Died += defeatedEnemy =>
            {
                PlayEnemyDeathSound(enemyInstance.Kind);
                HandleEnemyDefeated(roomState, enemyInstance.Id, defeatedEnemy.transform.position);
            };

            Rect playableBounds = GetPlayableRoomBounds();
            float phaseOffset = GetEnemyPhaseOffset(enemyInstance.Id, enemyInstance.Kind);
            if (enemyInstance.Kind == EnemyKind.Spider)
            {
                enemy.AddComponent<SpiderEnemyAI>().Initialize(playerTransform, parent, playableBounds, phaseOffset);
                return;
            }

            enemy.AddComponent<ZombieEnemyAI>().Initialize(playerTransform, parent, playableBounds, phaseOffset);
        }

        private static float GetEnemyPhaseOffset(int enemyId, EnemyKind enemyKind)
        {
            float maxOffset = enemyKind == EnemyKind.Spider ? SpiderPhaseSpreadSeconds : ZombiePhaseSpreadSeconds;
            int hash = Mathf.Abs(enemyId * 73856093);
            return (hash % 1000) / 1000f * maxOffset;
        }

        private static void PlayEnemyDeathSound(EnemyKind enemyKind)
        {
            switch (enemyKind)
            {
                case EnemyKind.Spider:
                    GameSfxPlayer.PlaySpiderDeath();
                    break;
                case EnemyKind.Zombie:
                    GameSfxPlayer.PlayZombieDeath();
                    break;
            }
        }

        private void CreateRoomContents(Transform parent, DungeonRoom room, DungeonRoomRuntimeState roomState)
        {
            switch (room.RoomType)
            {
                case RoomType.Trap:
                    CreateTrapMarker(parent);
                    break;
            }

            CreateRoomEnemies(parent, roomState);
            CreateRoomChests(parent, roomState);
            CreateRoomLoot(parent, roomState);
        }

        private void CreateTrapMarker(Transform parent)
        {
            GameObject trap = CreateSpriteObject("Trap Room Marker", parent, Vector2.zero, new Vector2(2.0f, 0.35f), new Color(0.85f, 0.12f, 0.08f, 1f));
            trap.GetComponent<SpriteRenderer>().sortingOrder = 3;
            trap.AddComponent<BoxCollider2D>().isTrigger = true;
        }

        private void CreateRoomChests(Transform parent, DungeonRoomRuntimeState roomState)
        {
            for (int i = 0; i < roomState.Chests.Count; i++)
            {
                RoomChestInstance chestInstance = roomState.Chests[i];
                if (!chestInstance.IsOpened)
                {
                    CreateLockedChest(parent, roomState, chestInstance);
                }
            }
        }

        private void CreateLockedChest(Transform parent, DungeonRoomRuntimeState roomState, RoomChestInstance chestInstance)
        {
            GameObject chestObject = CreateSpriteObject("Locked Chest", parent, chestInstance.Position, Vector2.one, Color.white);
            chestObject.AddComponent<CircleCollider2D>();
            LockedChest chest = chestObject.AddComponent<LockedChest>();
            EnsureLootServices();
            chest.Initialize(
                lootConfiguration,
                (itemDefinition, rewardPosition) =>
                {
                    AddLootToRoom(roomState, itemDefinition, rewardPosition, chest.transform.position);
                },
                chestInstance.RewardSeed,
                // Mark chest as opened for deletion
                () => roomState.MarkChestOpened(chestInstance.Id));
        }

        private void CreateRoomLoot(Transform parent, DungeonRoomRuntimeState roomState)
        {
            for (int i = 0; i < roomState.Loot.Count; i++)
            {
                RoomLootInstance lootInstance = roomState.Loot[i];
                if (!lootInstance.IsCollected)
                {
                    CreateLootPickup(parent, roomState, lootInstance);
                }
            }
        }

        private LootPickup CreateLootPickup(Transform parent, DungeonRoomRuntimeState roomState, RoomLootInstance lootInstance)
        {
            GameObject pickupObject = new GameObject($"Pickup {lootInstance.ItemDefinition.DisplayName}");
            pickupObject.transform.SetParent(parent, false);
            pickupObject.transform.position = lootInstance.Position;

            pickupObject.AddComponent<SpriteRenderer>();
            pickupObject.AddComponent<CircleCollider2D>();
            LootPickup pickup = pickupObject.AddComponent<LootPickup>();
            pickup.Initialize(lootInstance.ItemDefinition, _ => roomState.MarkLootCollected(lootInstance.Id));
            return pickup;
        }

        private void HandleEnemyDefeated(DungeonRoomRuntimeState roomState, int enemyId, Vector2 enemyPosition)
        {
            if (roomState == null)
            {
                return;
            }

            bool roomCleared = roomState.MarkEnemyDefeated(enemyId);
            // Enemy and room-clear rewards are intentionally separate rolls, so the last enemy can drop both.
            RollAndAddLoot(roomState, LootSourceType.Enemy, enemyPosition + EnemyDropOffset, enemyPosition);
            if (roomState.RoomType == RoomType.Final)
            {
                finalEncounterController?.NotifyEnemyDefeated();
                return;
            }

            if (roomCleared)
            {
                RollAndAddLoot(roomState, LootSourceType.RoomClear, enemyPosition + RoomClearDropOffset, enemyPosition);
            }
        }

        private void StartFinalEncounter(Transform parent, DungeonRoomRuntimeState roomState)
        {
            finalEncounterController = parent.gameObject.AddComponent<FinalEncounterController>();
            finalEncounterController.Initialize(
                roomState,
                dungeonRun.FinalEncounterConfig,
                GameManager.Instance.CurrentRun.Seed,
                enemy => CreateEnemy(parent, roomState, enemy),
                GameManager.Instance.CompleteCurrentRun);
        }

        private void RollAndAddLoot(DungeonRoomRuntimeState roomState, LootSourceType sourceType, Vector2 position, Vector2 launchStart)
        {
            EnsureLootServices();
            LootDropResult result = lootSystem.RollDrop(sourceType, lootRandom);
            if (result.HasDrop)
            {
                AddLootToRoom(roomState, result.Item, position, launchStart);
            }
        }

        private void AddLootToRoom(DungeonRoomRuntimeState roomState, LootItemDefinition itemDefinition, Vector2 position, Vector2 launchStart)
        {
            Vector2 clampedPosition = ClampToPlayableRoom(position);
            // Drops enter room state first so uncollected rewards survive room changes.
            RoomLootInstance lootInstance = roomState.AddLoot(itemDefinition, clampedPosition);
            if (!IsCurrentRoom(roomState) || roomRoot == null)
            {
                return;
            }

            LootPickup pickup = CreateLootPickup(roomRoot.transform, roomState, lootInstance);
            pickup.PlaySpawnLaunch(launchStart, clampedPosition, GetPlayableRoomBounds());
        }

        private bool IsCurrentRoom(DungeonRoomRuntimeState roomState)
        {
            return roomNavigator != null && roomNavigator.CurrentRoom.GridPosition == roomState.GridPosition;
        }

        private void EnsureLootServices()
        {
            if (dungeonRun == null)
            {
                dungeonRun = GameManager.Instance.CurrentRun?.Dungeon;
            }

            if (lootConfiguration == null)
            {
                lootConfiguration = dungeonRun?.LootConfiguration ?? LootTableConfiguration.CreateDefault();
            }

            if (lootSystem == null)
            {
                lootSystem = dungeonRun?.LootSystem ?? new LootSystem(lootConfiguration);
            }

            if (lootRandom == null)
            {
                lootRandom = dungeonRun?.LootRandom ?? new System.Random(GameManager.Instance.CurrentRun?.Seed ?? 0);
            }
        }

        private static Vector2 ClampToPlayableRoom(Vector2 position)
        {
            return DungeonRoomGeometry.ClampToPlayableBounds(position);
        }

        private static Rect GetPlayableRoomBounds()
        {
            return DungeonRoomGeometry.PlayableBounds;
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
            camera.orthographicSize = 6.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }

        private static Vector2 GetSpawnPositionForEntry(RoomDirection exitDirection)
        {
            return DungeonRoomGeometry.GetSpawnPositionForEntry(exitDirection);
        }

    }
}
