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
        private const float RoomWidth = 13.5f;
        private const float RoomHeight = 7.5f;
        private const float WallThickness = 0.75f;
        private const float RewardBoundaryPadding = 0.75f;
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
        private static readonly Vector2 PlayerVisualOffset = new Vector2(0f, 0.08f);
        private static readonly Vector2 DungeonWallFrameWorldSize = new Vector2(16.0f, 9.1f);
        private static readonly Vector2 DoorArchwayNorthOffset = new Vector2(0f, 0.7f);
        private static readonly Vector2 DoorArchwaySouthOffset = new Vector2(0f, -0.7f);
        private static readonly Vector2 DoorArchwayEastOffset = new Vector2(1.0f, 0f);
        private static readonly Vector2 DoorArchwayWestOffset = new Vector2(-1.0f, 0f);
        private static readonly Vector2 DoorFrameNorthOffset = new Vector2(0f, 0.28f);
        private static readonly Vector2 DoorFrameSouthOffset = new Vector2(0f, -0.28f);
        private static readonly Vector2 DoorFrameEastOffset = new Vector2(0.88f, 0f);
        private static readonly Vector2 DoorFrameWestOffset = new Vector2(-0.88f, 0f);
        private static readonly Color DoorColor = new Color(0.55f, 0.40f, 0.18f, 1f);
        private static readonly Color FinalRoomDoorColor = new Color(1f, 0.10f, 0.86f, 1f);

        private static GameplaySceneController instance;
        private static Sprite squareSprite;
        private static Sprite dungeonFloorSprite;
        private static Sprite dungeonWallFrameSprite;
        private static Sprite doorFrameHorizontalSprite;
        private static Sprite doorFrameVerticalSprite;
        private static readonly Dictionary<string, Sprite> doorArchwaySpritesByName = new Dictionary<string, Sprite>();

        private GameObject gameplayRoot;
        private GameObject roomRoot;
        private DungeonRoomNavigator roomNavigator;
        private Transform playerTransform;
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
            // Rebuild the room so only one room instance is active at a time.
            ClearRoomInstance();
            CreateRoomFloor(roomRoot.transform);
            CreateRoomWalls(roomRoot.transform);
            CreateDoors(roomRoot.transform, roomNavigator.CurrentRoom);
            CreateRoomContents(roomRoot.transform, roomNavigator.CurrentRoom);
            TransitionToRoomMusic(roomNavigator.CurrentRoom.RoomType);

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

            // targetMusicClip tracks so repeated room refreshes do not restart it.
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
                    CreateStarterChest(parent);
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

        private void CreateStarterChest(Transform parent)
        {
            GameObject chestObject = CreateSpriteObject("Locked Chest", parent, new Vector2(0f, -2.35f), Vector2.one, Color.white);
            chestObject.AddComponent<CircleCollider2D>();
            LockedChest chest = chestObject.AddComponent<LockedChest>();
            chest.Initialize(
                LootTableConfiguration.CreateDefault(),
                (itemDefinition, rewardPosition) =>
                {
                    Vector2 clampedRewardPosition = ClampToPlayableRoom(rewardPosition);
                    LootPickup pickup = CreateLootPickup(parent, itemDefinition, clampedRewardPosition);
                    pickup.PlaySpawnLaunch(chest.transform.position, clampedRewardPosition, GetPlayableRoomBounds());
                });
        }

        private LootPickup CreateLootPickup(Transform parent, LootItemDefinition itemDefinition, Vector2 position)
        {
            GameObject pickupObject = new GameObject($"Pickup {itemDefinition.DisplayName}");
            pickupObject.transform.SetParent(parent, false);
            pickupObject.transform.position = position;

            pickupObject.AddComponent<SpriteRenderer>();
            pickupObject.AddComponent<CircleCollider2D>();
            LootPickup pickup = pickupObject.AddComponent<LootPickup>();
            pickup.Initialize(itemDefinition);
            return pickup;
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
            camera.orthographicSize = 4.7f;
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
