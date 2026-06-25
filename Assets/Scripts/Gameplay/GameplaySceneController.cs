using System.Collections;
using System.Collections.Generic;
using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Combat;
using CryptKnight.Data;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using CryptKnight.Player;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CryptKnight.Gameplay
{
    public sealed class GameplaySceneController : MonoBehaviour
    {
        private const float RoomWidth = 18.9f;
        private const float RoomHeight = 10.5f;
        private const float WallThickness = 0.75f;
        private const float RewardBoundaryPadding = 0.75f;
        private const string KeyItemId = "key";
        private const float GameMusicFadeDuration = 5f;
        private const float GameMusicVolume = 1f;
        private const string ExplorationMusicResourcePath = "Audio/Game/crypt-knight-dungeon-exploration-loop";
        private const string BossMusicResourcePath = "Audio/Game/crypt-knight-boss-room-heavy-fight-loop";
        private const string DungeonFloorAssetPath = "Assets/Art/Environment/crypt-knight-dungeon-floor-alt-dark-1024.png";
        private const string DungeonWallFrameAssetPath = "Assets/Art/Environment/crypt-knight-dungeon-wall-frame-transparent.png";
        private const string DoorFrameHorizontalAssetPath = "Assets/Art/Environment/crypt-knight-dungeon-wall-door.png";
        private const string DoorFrameVerticalAssetPath = "Assets/Art/Environment/crypt-knight-dungeon-wall-door-side.png";
        private const string DoorArchwayAssetPath = "Assets/Art/Environment/door_archway_environment_half_row.png";
        private const float PlayerVisualScale = 0.85f;
        private const float PlayerColliderRadius = 0.35f;
        private const int ZombieMaxHealth = 5;
        private const int SpiderMaxHealth = 3;
        private const float ZombiePhaseSpreadSeconds = 2.0f;
        private const float SpiderPhaseSpreadSeconds = 1.5f;
        private static readonly Vector2 PlayerVisualOffset = new Vector2(0f, 0.08f);
        private static readonly Vector2 DungeonWallFrameWorldSize = new Vector2(22.4f, 12.74f);
        private static readonly Vector2 DoorArchwayNorthOffset = new Vector2(0f, 0.7f);
        private static readonly Vector2 DoorArchwaySouthOffset = new Vector2(0f, -0.7f);
        private static readonly Vector2 DoorArchwayEastOffset = new Vector2(1.0f, 0f);
        private static readonly Vector2 DoorArchwayWestOffset = new Vector2(-1.0f, 0f);
        private static readonly Vector2 DoorFrameNorthOffset = new Vector2(0f, 0.28f);
        private static readonly Vector2 DoorFrameSouthOffset = new Vector2(0f, -0.28f);
        private static readonly Vector2 DoorFrameEastOffset = new Vector2(0.88f, 0f);
        private static readonly Vector2 DoorFrameWestOffset = new Vector2(-0.88f, 0f);
        private static readonly Vector2 EnemyDropOffset = new Vector2(0f, 0.85f);
        private static readonly Vector2 RoomClearDropOffset = new Vector2(-0.85f, 0.85f);
        private static readonly Vector2 StarterGiftKeyPosition = new Vector2(-1.15f, 1.15f);
        private static readonly Vector2 StarterGiftChestPosition = new Vector2(1.15f, 1.15f);
        private static readonly Color DoorColor = new Color(0.55f, 0.40f, 0.18f, 1f);
        private static readonly Color FinalRoomDoorColor = new Color(1f, 0.10f, 0.86f, 1f);

        private static GameplaySceneController instance;
        private static Sprite squareSprite;
        private static Sprite dungeonFloorSprite;
        private static Sprite dungeonWallFrameSprite;
        private static Sprite doorFrameHorizontalSprite;
        private static Sprite doorFrameVerticalSprite;
        private static readonly Dictionary<string, Sprite> doorArchwaySpritesByName = new Dictionary<string, Sprite>();

        // Rooms states are rebuilt on travel between them
        private readonly Dictionary<Vector2Int, DungeonRoomRuntimeState> roomStates = new Dictionary<Vector2Int, DungeonRoomRuntimeState>();
        private GameObject gameplayRoot;
        private GameObject roomRoot;
        private DungeonRoomNavigator roomNavigator;
        private Transform playerTransform;
        private LootTableConfiguration lootConfiguration;
        private LootSystem lootSystem;
        private LootDistributionRules lootRules;
        private EnemySpawnRules enemySpawnRules;
        private System.Random lootRandom;
        private AudioClip explorationMusicClip;
        private AudioClip bossMusicClip;
        private AudioSource activeMusicSource;
        private AudioClip currentMusicClip;
        private AudioClip targetMusicClip;
        private Coroutine musicTransitionRoutine;

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
            ConfigureGameMusic();

            GameRunState runState = GameManager.Instance.CurrentRun;
            DungeonLayout dungeonLayout = DungeonLayoutGenerator.Generate(runState.DungeonWidth, runState.DungeonHeight, runState.Seed);
            roomNavigator = new DungeonRoomNavigator(dungeonLayout);
            lootConfiguration = LootTableConfiguration.CreateDefault();
            lootSystem = new LootSystem(lootConfiguration);
            lootRules = LootDistributionRules.CreateDefault();
            enemySpawnRules = EnemySpawnRules.CreateDefault();
            lootRandom = new System.Random(runState.Seed ^ 0x4C4F4F54);
            InitializeRoomStates(dungeonLayout, runState.Seed);

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
            StopGameMusic();

            if (gameplayRoot != null)
            {
                Destroy(gameplayRoot);
            }

            gameplayRoot = null;
            roomRoot = null;
            roomNavigator = null;
            playerTransform = null;
            roomStates.Clear();
            lootConfiguration = null;
            lootSystem = null;
            lootRules = null;
            enemySpawnRules = null;
            lootRandom = null;
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

            return !GetRoomState(roomNavigator.CurrentRoom.GridPosition).IsLocked;
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
            DungeonRoomRuntimeState roomState = GetRoomState(currentRoom.GridPosition);
            CreateRoomFloor(roomRoot.transform);
            CreateRoomWalls(roomRoot.transform);
            CreateDoors(roomRoot.transform, currentRoom);
            CreateRoomContents(roomRoot.transform, currentRoom, roomState);
            TransitionToRoomMusic(currentRoom.RoomType);

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

        private void ConfigureGameMusic()
        {
            if (activeMusicSource != null)
            {
                return;
            }

            explorationMusicClip = Resources.Load<AudioClip>(ExplorationMusicResourcePath);
            bossMusicClip = Resources.Load<AudioClip>(BossMusicResourcePath);
            if (explorationMusicClip == null && bossMusicClip == null)
            {
                Debug.LogWarning("Gameplay music clips could not be loaded from Resources/Audio/Game.");
                return;
            }

            activeMusicSource = gameObject.AddComponent<AudioSource>();
            ConfigureMusicSource(activeMusicSource);
        }

        private void HandleAudioSettingsChanged()
        {
            if (activeMusicSource != null && activeMusicSource.isPlaying && musicTransitionRoutine == null)
            {
                activeMusicSource.volume = GetTargetMusicVolume();
            }
        }

        private static void ConfigureMusicSource(AudioSource audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0f;
        }

        private void TransitionToRoomMusic(RoomType roomType)
        {
            AudioClip targetClip = GetMusicClipForRoom(roomType);
            if (targetClip == null || activeMusicSource == null)
            {
                return;
            }

            if (targetMusicClip == targetClip)
            {
                return;
            }

            if (musicTransitionRoutine != null)
            {
                StopCoroutine(musicTransitionRoutine);
                musicTransitionRoutine = null;
            }

            if (currentMusicClip == targetClip && activeMusicSource.isPlaying)
            {
                if (activeMusicSource.volume < GetTargetMusicVolume())
                {
                    musicTransitionRoutine = StartCoroutine(FadeActiveRoomMusicToFull(targetClip));
                }

                return;
            }

            if (currentMusicClip == null || !activeMusicSource.isPlaying)
            {
                musicTransitionRoutine = StartCoroutine(FadeInRoomMusic(targetClip));
                return;
            }

            musicTransitionRoutine = StartCoroutine(FadeOutThenStartRoomMusic(targetClip));
        }

        private AudioClip GetMusicClipForRoom(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Final:
                    return bossMusicClip;
                case RoomType.Enemy:
                case RoomType.Starter:
                case RoomType.Trap:
                    return explorationMusicClip;
                default:
                    return null;
            }
        }

        private IEnumerator FadeInRoomMusic(AudioClip targetClip)
        {
            targetMusicClip = targetClip;
            activeMusicSource.clip = targetClip;
            activeMusicSource.volume = 0f;
            activeMusicSource.Play();
            currentMusicClip = targetClip;

            float elapsed = 0f;
            while (elapsed < GameMusicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / GameMusicFadeDuration);
                activeMusicSource.volume = Mathf.Lerp(0f, GetTargetMusicVolume(), progress);
                yield return null;
            }

            activeMusicSource.volume = GetTargetMusicVolume();
            targetMusicClip = null;
            musicTransitionRoutine = null;
        }

        private IEnumerator FadeActiveRoomMusicToFull(AudioClip targetClip)
        {
            targetMusicClip = targetClip;
            float elapsed = 0f;
            float startVolume = activeMusicSource.volume;

            while (elapsed < GameMusicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / GameMusicFadeDuration);
                activeMusicSource.volume = Mathf.Lerp(startVolume, GetTargetMusicVolume(), progress);
                yield return null;
            }

            activeMusicSource.volume = GetTargetMusicVolume();
            currentMusicClip = targetClip;
            targetMusicClip = null;
            musicTransitionRoutine = null;
        }

        private IEnumerator FadeOutThenStartRoomMusic(AudioClip targetClip)
        {
            targetMusicClip = targetClip;
            float elapsed = 0f;
            float startVolume = activeMusicSource.volume;

            while (elapsed < GameMusicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / GameMusicFadeDuration);
                activeMusicSource.volume = Mathf.Lerp(startVolume, 0f, progress);
                yield return null;
            }

            activeMusicSource.Stop();
            activeMusicSource.clip = targetClip;
            activeMusicSource.volume = GetTargetMusicVolume();
            activeMusicSource.Play();
            currentMusicClip = targetClip;
            targetMusicClip = null;
            musicTransitionRoutine = null;
        }

        private void StopGameMusic()
        {
            if (musicTransitionRoutine != null)
            {
                StopCoroutine(musicTransitionRoutine);
                musicTransitionRoutine = null;
            }

            if (activeMusicSource != null)
            {
                activeMusicSource.Stop();
                activeMusicSource.clip = null;
                activeMusicSource.volume = 0f;
            }

            currentMusicClip = null;
            targetMusicClip = null;
        }

        private static float GetTargetMusicVolume()
        {
            return GameMusicVolume * GameAudioSettings.MusicVolume;
        }

        private void CreateRoomFloor(Transform parent)
        {
            Sprite floorSprite = GetDungeonFloorSprite();
            GameObject floor = floorSprite != null
                ? CreateTiledSpriteObject("Room Floor", parent, Vector2.zero, new Vector2(RoomWidth, RoomHeight), floorSprite, -10)
                : CreateSpriteObject("Room Floor", parent, new Vector2(0f, 0f), new Vector2(RoomWidth, RoomHeight), new Color(0.11f, 0.12f, 0.13f, 1f));

            floor.GetComponent<SpriteRenderer>().sortingOrder = -10;
            floor.AddComponent<BoxCollider2D>().isTrigger = true;
        }

        private void CreateRoomWalls(Transform parent)
        {
            Sprite wallFrameSprite = GetDungeonWallFrameSprite();
            Color wallColor = wallFrameSprite != null ? Color.clear : new Color(0.30f, 0.27f, 0.25f, 1f);
            float halfWidth = RoomWidth * 0.5f;
            float halfHeight = RoomHeight * 0.5f;

            CreateWall(parent, "North Wall", new Vector2(0f, halfHeight + WallThickness * 0.5f), new Vector2(RoomWidth + WallThickness * 2f, WallThickness), wallColor);
            CreateWall(parent, "South Wall", new Vector2(0f, -halfHeight - WallThickness * 0.5f), new Vector2(RoomWidth + WallThickness * 2f, WallThickness), wallColor);
            CreateWall(parent, "West Wall", new Vector2(-halfWidth - WallThickness * 0.5f, 0f), new Vector2(WallThickness, RoomHeight), wallColor);
            CreateWall(parent, "East Wall", new Vector2(halfWidth + WallThickness * 0.5f, 0f), new Vector2(WallThickness, RoomHeight), wallColor);

            if (wallFrameSprite != null)
            {
                CreateTexturedSpriteObject("Dungeon Wall Frame", parent, Vector2.zero, DungeonWallFrameWorldSize, wallFrameSprite, 6);
            }
        }

        private void CreateWall(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject wall = CreateSpriteObject(objectName, parent, position, size, color);
            SpriteRenderer renderer = wall.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 0;
            renderer.enabled = color.a > 0f;
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
            Vector2 size = IsHorizontalDoor(direction) ? new Vector2(1.05f, 0.42f) : new Vector2(0.42f, 1.05f);
            GameObject door = CreateSpriteObject($"Door {direction}", parent, position, size, leadsToFinalRoom ? FinalRoomDoorColor : DoorColor);
            SpriteRenderer doorRenderer = door.GetComponent<SpriteRenderer>();
            doorRenderer.sortingOrder = 2;
            // The door object is the trigger, door art is offset
            doorRenderer.enabled = false;

            CreateDoorFrame(parent, direction);

            if (leadsToFinalRoom)
            {
                CreateDoorArchway(parent, direction);
            }

            BoxCollider2D trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            door.AddComponent<RoomDoorTrigger>().Initialize(this, direction);
        }

        private static void CreateDoorFrame(Transform parent, RoomDirection direction)
        {
            Sprite sprite = GetDoorFrameSprite(direction);
            if (sprite == null)
            {
                return;
            }

            Vector2 position = GetDoorPosition(direction) + GetDoorFrameOffset(direction);
            GameObject frame = CreateTexturedSpriteObject($"Door Frame {direction}", parent, position, sprite.bounds.size, sprite, 7);
            frame.transform.rotation = Quaternion.Euler(0f, 0f, GetDoorFrameRotation(direction));
        }

        private static void CreateDoorArchway(Transform parent, RoomDirection direction)
        {
            Sprite sprite = GetDoorArchwaySprite(direction);
            if (sprite == null)
            {
                return;
            }

            Vector2 position = GetDoorPosition(direction) + GetDoorArchwayOffset(direction);
            GameObject archway = CreateTexturedSpriteObject($"Door Archway {direction}", parent, position, GetDoorArchwayWorldSize(sprite), sprite, 8);
            archway.transform.rotation = Quaternion.Euler(0f, 0f, GetDoorArchwayRotation(direction));
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

        private void InitializeRoomStates(DungeonLayout layout, int runSeed)
        {
            roomStates.Clear();
            foreach (DungeonRoom room in layout.Rooms)
            {
                roomStates[room.GridPosition] = CreateRoomState(room, runSeed);
            }
        }

        private DungeonRoomRuntimeState CreateRoomState(DungeonRoom room, int runSeed)
        {
            EnsureLootServices();
            EnsureEnemyServices();
            DungeonRoomRuntimeState state = new DungeonRoomRuntimeState(room.GridPosition, room.RoomType);
            IReadOnlyList<RoomEnemySpawn> enemySpawns = enemySpawnRules.CreateSpawns(room.RoomType, runSeed, room.GridPosition);
            for (int i = 0; i < enemySpawns.Count; i++)
            {
                RoomEnemySpawn spawn = enemySpawns[i];
                state.AddEnemy(spawn.Kind, spawn.Position);
            }

            if (room.RoomType == RoomType.Starter)
            {
                AddStarterGiftToRoomState(state);
            }

            if (lootRules.ShouldPlaceChest(room.RoomType, runSeed, room.GridPosition))
            {
                state.AddChest(lootRules.GetChestSpawnPosition(runSeed, room.GridPosition));
            }

            if (lootRules.ShouldPlaceKey(room.RoomType, runSeed, room.GridPosition))
            {
                TryAddGeneratedKeyToRoomState(state, runSeed, room.GridPosition);
            }

            state.MarkContentsInitialized();
            return state;
        }

        private DungeonRoomRuntimeState GetRoomState(Vector2Int roomPosition)
        {
            if (roomStates.TryGetValue(roomPosition, out DungeonRoomRuntimeState state))
            {
                return state;
            }

            if (roomNavigator == null)
            {
                throw new System.InvalidOperationException("Room state cannot be created without an active dungeon navigator.");
            }

            DungeonRoom room = roomNavigator.Layout.TryGetRoom(roomPosition, out DungeonRoom foundRoom)
                ? foundRoom
                : roomNavigator.CurrentRoom;
            state = CreateRoomState(room, GameManager.Instance.CurrentRun?.Seed ?? 0);
            roomStates[roomPosition] = state;
            return state;
        }

        private void TryAddGeneratedKeyToRoomState(DungeonRoomRuntimeState state, int runSeed, Vector2Int roomPosition)
        {
            LootItemDefinition keyItem = GetKeyItemDefinition();
            if (keyItem != null)
            {
                state.AddLoot(keyItem, lootRules.GetKeySpawnPosition(runSeed, roomPosition));
            }
        }

        private LootItemDefinition GetKeyItemDefinition()
        {
            EnsureLootServices();
            for (int i = 0; i < lootConfiguration.Items.Count; i++)
            {
                LootItemDefinition item = lootConfiguration.Items[i];
                if (item.ItemId == KeyItemId)
                {
                    return item;
                }
            }

            return null;
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
            enemyHealth.Initialize(GetEnemyMaxHealth(enemyInstance.Kind));
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

        private static int GetEnemyMaxHealth(EnemyKind enemyKind)
        {
            return enemyKind == EnemyKind.Zombie ? ZombieMaxHealth : SpiderMaxHealth;
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
                case RoomType.Final:
                    CreateFinalRoomMarker(parent);
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

        private void CreateFinalRoomMarker(Transform parent)
        {
            GameObject finalMarker = CreateSpriteObject("Final Room Marker", parent, Vector2.zero, new Vector2(1.4f, 1.4f), new Color(0.95f, 0.76f, 0.20f, 1f));
            finalMarker.GetComponent<SpriteRenderer>().sortingOrder = 3;
        }

        private void AddStarterGiftToRoomState(DungeonRoomRuntimeState roomState)
        {
            LootItemDefinition keyItem = GetKeyItemDefinition();
            if (keyItem != null)
            {
                roomState.AddLoot(keyItem, StarterGiftKeyPosition);
            }

            roomState.AddChest(StarterGiftChestPosition);
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
                null,
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
            if (roomCleared)
            {
                RollAndAddLoot(roomState, LootSourceType.RoomClear, enemyPosition + RoomClearDropOffset, enemyPosition);
            }
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
            if (lootConfiguration == null)
            {
                lootConfiguration = LootTableConfiguration.CreateDefault();
            }

            if (lootSystem == null)
            {
                lootSystem = new LootSystem(lootConfiguration);
            }

            if (lootRules == null)
            {
                lootRules = LootDistributionRules.CreateDefault();
            }

            if (lootRandom == null)
            {
                lootRandom = new System.Random(GameManager.Instance.CurrentRun?.Seed ?? 0);
            }
        }

        private void EnsureEnemyServices()
        {
            if (enemySpawnRules == null)
            {
                enemySpawnRules = EnemySpawnRules.CreateDefault();
            }
        }

        private static Vector2 ClampToPlayableRoom(Vector2 position)
        {
            Rect bounds = GetPlayableRoomBounds();
            return new Vector2(
                Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(position.y, bounds.yMin, bounds.yMax));
        }

        private static Rect GetPlayableRoomBounds()
        {
            float halfWidth = RoomWidth * 0.5f - RewardBoundaryPadding;
            float halfHeight = RoomHeight * 0.5f - RewardBoundaryPadding;
            return Rect.MinMaxRect(-halfWidth, -halfHeight, halfWidth, halfHeight);
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

        private static GameObject CreateTiledSpriteObject(string objectName, Transform parent, Vector2 position, Vector2 size, Sprite sprite, int sortingOrder)
        {
            GameObject spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.position = position;

            SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.size = size;
            spriteRenderer.sortingOrder = sortingOrder;

            return spriteObject;
        }

        private static GameObject CreateTexturedSpriteObject(string objectName, Transform parent, Vector2 position, Vector2 size, Sprite sprite, int sortingOrder)
        {
            GameObject spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.position = position;

            SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = sortingOrder;
            SetSpriteWorldSize(spriteObject.transform, spriteRenderer, size);

            return spriteObject;
        }

        private static void SetSpriteWorldSize(Transform target, SpriteRenderer spriteRenderer, Vector2 size)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                target.localScale = Vector3.one;
                return;
            }

            target.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
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

        private static Sprite GetDungeonFloorSprite()
        {
            if (dungeonFloorSprite == null)
            {
                dungeonFloorSprite = LoadEditorSprite(DungeonFloorAssetPath);
            }

            return dungeonFloorSprite;
        }

        private static Sprite GetDungeonWallFrameSprite()
        {
            if (dungeonWallFrameSprite == null)
            {
                dungeonWallFrameSprite = LoadEditorSprite(DungeonWallFrameAssetPath);
            }

            return dungeonWallFrameSprite;
        }

        private static Sprite GetDoorFrameSprite(RoomDirection direction)
        {
            if (IsHorizontalDoor(direction))
            {
                if (doorFrameHorizontalSprite == null)
                {
                    doorFrameHorizontalSprite = LoadEditorSprite(DoorFrameHorizontalAssetPath);
                }

                return doorFrameHorizontalSprite;
            }

            if (doorFrameVerticalSprite == null)
            {
                doorFrameVerticalSprite = LoadEditorSprite(DoorFrameVerticalAssetPath);
            }

            return doorFrameVerticalSprite;
        }

        private static Sprite GetDoorArchwaySprite(RoomDirection direction)
        {
            bool useNorthSprite = direction == RoomDirection.North || direction == RoomDirection.South;
            string spriteName = useNorthSprite ? "door_archway_environment_north" : "door_archway_environment_west";

            if (doorArchwaySpritesByName.TryGetValue(spriteName, out Sprite sprite))
            {
                return sprite;
            }

            sprite = LoadEditorSprite(DoorArchwayAssetPath, spriteName);
            if (sprite != null)
            {
                doorArchwaySpritesByName[spriteName] = sprite;
            }

            return sprite;
        }

        private static Vector2 GetDoorFrameOffset(RoomDirection direction)
        {
            switch (direction)
            {
                case RoomDirection.North:
                    return DoorFrameNorthOffset;
                case RoomDirection.East:
                    return DoorFrameEastOffset;
                case RoomDirection.South:
                    return DoorFrameSouthOffset;
                case RoomDirection.West:
                    return DoorFrameWestOffset;
                default:
                    return Vector2.zero;
            }
        }

        private static float GetDoorFrameRotation(RoomDirection direction)
        {
            // These rotations match the way the door frame are supposed to be positioned.
            switch (direction)
            {
                case RoomDirection.North:
                    return 0f;
                case RoomDirection.East:
                    return 180f;
                case RoomDirection.South:
                    return 180f;
                case RoomDirection.West:
                    return 0f;
                default:
                    return 0f;
            }
        }

        private static Vector2 GetDoorArchwayOffset(RoomDirection direction)
        {
            switch (direction)
            {
                case RoomDirection.North:
                    return DoorArchwayNorthOffset;
                case RoomDirection.East:
                    return DoorArchwayEastOffset;
                case RoomDirection.South:
                    return DoorArchwaySouthOffset;
                case RoomDirection.West:
                    return DoorArchwayWestOffset;
                default:
                    return Vector2.zero;
            }
        }

        private static float GetDoorArchwayRotation(RoomDirection direction)
        {
            return direction == RoomDirection.South || direction == RoomDirection.East ? 180f : 0f;
        }

        private static Vector2 GetDoorArchwayWorldSize(Sprite sprite)
        {
            return sprite != null ? sprite.bounds.size : Vector2.one;
        }

        private static Sprite LoadEditorSprite(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        private static Sprite LoadEditorSprite(string assetPath, string spriteName)
        {
#if UNITY_EDITOR
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
#endif

            return null;
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
            // Spawn near the opposite doorway so moving through rooms feels continuous.
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
