using System.Collections.Generic;
using CryptKnight.Content;
using CryptKnight.Dungeon;
using CryptKnight.Loot;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    public sealed class DungeonRoomEnvironmentBuilder
    {
        private const string FloorPath = "Art/Environment/crypt-knight-dungeon-floor-alt-dark-1024";
        private const string WallFramePath = "Art/Environment/crypt-knight-dungeon-wall-frame-transparent";
        private const string HorizontalDoorPath = "Art/Environment/crypt-knight-dungeon-wall-door";
        private const string VerticalDoorPath = "Art/Environment/crypt-knight-dungeon-wall-door-side";
        private const string ArchwayPath = "Art/Environment/door_archway_environment_half_row";
        private static readonly Vector2 WallFrameSize = new Vector2(22.4f, 12.74f);
        private static readonly Color DoorColor = new Color(0.55f, 0.40f, 0.18f, 1f);

        public void Build(Transform parent, DungeonRoom room, DungeonLayout layout, System.Action<RoomDirection> travel)
        {
            CreateFloor(parent);
            CreateWalls(parent);
            foreach (KeyValuePair<RoomDirection, Vector2Int> connection in room.Connections)
            {
                CreateDoor(parent, connection.Key, layout.FinalPosition == connection.Value, travel);
            }
        }

        private static void CreateFloor(Transform parent)
        {
            Sprite floorSprite = RuntimeAssetLoader.LoadSprite(FloorPath);
            GameObject floor = floorSprite != null
                ? CreateTiledSprite("Room Floor", parent, Vector2.zero, new Vector2(DungeonRoomGeometry.Width, DungeonRoomGeometry.Height), floorSprite, -10)
                : CreateSprite("Room Floor", parent, Vector2.zero, new Vector2(DungeonRoomGeometry.Width, DungeonRoomGeometry.Height), new Color(0.11f, 0.12f, 0.13f, 1f));
            floor.AddComponent<BoxCollider2D>().isTrigger = true;
        }

        private static void CreateWalls(Transform parent)
        {
            Sprite wallFrame = RuntimeAssetLoader.LoadSprite(WallFramePath);
            Color wallColor = wallFrame != null ? Color.clear : new Color(0.30f, 0.27f, 0.25f, 1f);
            float halfWidth = DungeonRoomGeometry.Width * 0.5f;
            float halfHeight = DungeonRoomGeometry.Height * 0.5f;
            float thickness = DungeonRoomGeometry.WallThickness;

            CreateWall(parent, "North Wall", new Vector2(0f, halfHeight + thickness * 0.5f), new Vector2(DungeonRoomGeometry.Width + thickness * 2f, thickness), wallColor);
            CreateWall(parent, "South Wall", new Vector2(0f, -halfHeight - thickness * 0.5f), new Vector2(DungeonRoomGeometry.Width + thickness * 2f, thickness), wallColor);
            CreateWall(parent, "West Wall", new Vector2(-halfWidth - thickness * 0.5f, 0f), new Vector2(thickness, DungeonRoomGeometry.Height), wallColor);
            CreateWall(parent, "East Wall", new Vector2(halfWidth + thickness * 0.5f, 0f), new Vector2(thickness, DungeonRoomGeometry.Height), wallColor);

            if (wallFrame != null)
            {
                CreateTexturedSprite("Dungeon Wall Frame", parent, Vector2.zero, WallFrameSize, wallFrame, 6);
            }
        }

        private static void CreateWall(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject wall = CreateSprite(name, parent, position, size, color);
            wall.GetComponent<SpriteRenderer>().enabled = color.a > 0f;
            wall.AddComponent<BoxCollider2D>();
        }

        private static void CreateDoor(Transform parent, RoomDirection direction, bool leadsToFinal, System.Action<RoomDirection> travel)
        {
            Vector2 position = DungeonRoomGeometry.GetDoorPosition(direction);
            Vector2 size = DungeonRoomGeometry.IsHorizontalDoor(direction) ? new Vector2(1.05f, 0.42f) : new Vector2(0.42f, 1.05f);
            GameObject door = CreateSprite($"Door {direction}", parent, position, size, DoorColor);
            door.GetComponent<SpriteRenderer>().enabled = false;
            CreateDoorFrame(parent, direction);
            if (leadsToFinal)
            {
                CreateDoorArchway(parent, direction);
            }

            door.AddComponent<BoxCollider2D>().isTrigger = true;
            door.AddComponent<RoomDoorTrigger>().Initialize(travel, direction);
        }

        private static void CreateDoorFrame(Transform parent, RoomDirection direction)
        {
            Sprite sprite = RuntimeAssetLoader.LoadSprite(DungeonRoomGeometry.IsHorizontalDoor(direction) ? HorizontalDoorPath : VerticalDoorPath);
            if (sprite == null)
            {
                return;
            }

            GameObject frame = CreateTexturedSprite($"Door Frame {direction}", parent, DungeonRoomGeometry.GetDoorPosition(direction) + GetFrameOffset(direction), sprite.bounds.size, sprite, 7);
            frame.transform.rotation = Quaternion.Euler(0f, 0f, direction == RoomDirection.East || direction == RoomDirection.South ? 180f : 0f);
        }

        private static void CreateDoorArchway(Transform parent, RoomDirection direction)
        {
            string spriteName = DungeonRoomGeometry.IsHorizontalDoor(direction) ? "door_archway_environment_north" : "door_archway_environment_west";
            Sprite sprite = RuntimeAssetLoader.LoadSprite(ArchwayPath, spriteName);
            if (sprite == null)
            {
                return;
            }

            GameObject archway = CreateTexturedSprite($"Door Archway {direction}", parent, DungeonRoomGeometry.GetDoorPosition(direction) + GetArchwayOffset(direction), sprite.bounds.size, sprite, 8);
            archway.transform.rotation = Quaternion.Euler(0f, 0f, direction == RoomDirection.South || direction == RoomDirection.East ? 180f : 0f);
        }

        private static Vector2 GetFrameOffset(RoomDirection direction)
        {
            switch (direction)
            {
                case RoomDirection.North: return new Vector2(0f, 0.28f);
                case RoomDirection.East: return new Vector2(0.88f, 0f);
                case RoomDirection.South: return new Vector2(0f, -0.28f);
                case RoomDirection.West: return new Vector2(-0.88f, 0f);
                default: return Vector2.zero;
            }
        }

        private static Vector2 GetArchwayOffset(RoomDirection direction)
        {
            switch (direction)
            {
                case RoomDirection.North: return new Vector2(0f, 0.7f);
                case RoomDirection.East: return new Vector2(1f, 0f);
                case RoomDirection.South: return new Vector2(0f, -0.7f);
                case RoomDirection.West: return new Vector2(-1f, 0f);
                default: return Vector2.zero;
            }
        }

        private static GameObject CreateSprite(string name, Transform parent, Vector2 position, Vector2 scale, Color color)
        {
            GameObject value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            SpriteRenderer renderer = value.AddComponent<SpriteRenderer>();
            renderer.sprite = LootItemVisuals.GetSquareSprite();
            renderer.color = color;
            return value;
        }

        private static GameObject CreateTiledSprite(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, int order)
        {
            GameObject value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            SpriteRenderer renderer = value.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = order;
            return value;
        }

        private static GameObject CreateTexturedSprite(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, int order)
        {
            GameObject value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            SpriteRenderer renderer = value.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            Vector2 spriteSize = sprite.bounds.size;
            value.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            return value;
        }
    }
}
