using CryptKnight.Dungeon;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    public static class DungeonRoomGeometry
    {
        public const float Width = 18.9f;
        public const float Height = 10.5f;
        public const float WallThickness = 0.75f;
        public const float RewardBoundaryPadding = 0.75f;

        public static Rect PlayableBounds
        {
            get
            {
                float halfWidth = Width * 0.5f - RewardBoundaryPadding;
                float halfHeight = Height * 0.5f - RewardBoundaryPadding;
                return Rect.MinMaxRect(-halfWidth, -halfHeight, halfWidth, halfHeight);
            }
        }

        public static Vector2 ClampToPlayableBounds(Vector2 position)
        {
            Rect bounds = PlayableBounds;
            return new Vector2(
                Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(position.y, bounds.yMin, bounds.yMax));
        }

        public static Vector2 GetDoorPosition(RoomDirection direction)
        {
            float halfWidth = Width * 0.5f;
            float halfHeight = Height * 0.5f;
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

        public static Vector2 GetSpawnPositionForEntry(RoomDirection exitDirection)
        {
            float halfWidth = Width * 0.5f;
            float halfHeight = Height * 0.5f;
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

        public static bool IsHorizontalDoor(RoomDirection direction)
        {
            return direction == RoomDirection.North || direction == RoomDirection.South;
        }
    }
}
