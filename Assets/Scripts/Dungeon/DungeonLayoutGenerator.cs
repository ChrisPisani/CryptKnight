using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public static class DungeonLayoutGenerator
    {
        public static DungeonLayout Generate(int width, int height, int seed)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Dungeon width and height must be positive.");
            }

            List<DungeonRoom> rooms = new List<DungeonRoom>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    rooms.Add(new DungeonRoom(new Vector2Int(x, y), RoomType.Enemy));
                }
            }

            ConnectGridRooms(rooms, width, height);
            Vector2Int startPosition = PickStartPosition(width, height, seed);
            Vector2Int finalPosition = PickFinalPosition(width, height, startPosition, seed);
            AssignRoomTypes(rooms, startPosition, finalPosition, seed);

            return new DungeonLayout(width, height, rooms, startPosition, finalPosition);
        }

        private static void ConnectGridRooms(List<DungeonRoom> rooms, int width, int height)
        {
            Dictionary<Vector2Int, DungeonRoom> roomLookup = new Dictionary<Vector2Int, DungeonRoom>();
            foreach (DungeonRoom room in rooms)
            {
                roomLookup[room.GridPosition] = room;
            }

            foreach (DungeonRoom room in rooms)
            {
                Vector2Int position = room.GridPosition;
                TryConnect(room, roomLookup, RoomDirection.North, new Vector2Int(position.x, position.y + 1));
                TryConnect(room, roomLookup, RoomDirection.East, new Vector2Int(position.x + 1, position.y));
                TryConnect(room, roomLookup, RoomDirection.South, new Vector2Int(position.x, position.y - 1));
                TryConnect(room, roomLookup, RoomDirection.West, new Vector2Int(position.x - 1, position.y));
            }
        }

        private static void TryConnect(DungeonRoom room, IReadOnlyDictionary<Vector2Int, DungeonRoom> roomLookup, RoomDirection direction, Vector2Int targetPosition)
        {
            if (roomLookup.ContainsKey(targetPosition))
            {
                room.Connect(direction, targetPosition);
            }
        }

        private static Vector2Int PickStartPosition(int width, int height, int seed)
        {
            System.Random random = new System.Random(seed);
            return new Vector2Int(random.Next(width), random.Next(height));
        }

        private static Vector2Int PickFinalPosition(int width, int height, Vector2Int startPosition, int seed)
        {
            List<Vector2Int> farthestRooms = new List<Vector2Int>();
            int farthestDistance = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (candidate == startPosition)
                    {
                        continue;
                    }

                    int distance = Math.Abs(candidate.x - startPosition.x) + Math.Abs(candidate.y - startPosition.y);
                    if (distance > farthestDistance)
                    {
                        farthestRooms.Clear();
                        farthestDistance = distance;
                    }

                    if (distance == farthestDistance)
                    {
                        farthestRooms.Add(candidate);
                    }
                }
            }

            if (farthestRooms.Count == 0)
            {
                return startPosition;
            }

            // Attempt to move the final room to be far from the starter room
            System.Random random = new System.Random(seed ^ 0x4D595DF4);
            return farthestRooms[random.Next(farthestRooms.Count)];
        }

        private static void AssignRoomTypes(List<DungeonRoom> rooms, Vector2Int startPosition, Vector2Int finalPosition, int seed)
        {
            int trapOffset = Math.Abs(seed) % Math.Max(1, rooms.Count);

            for (int i = 0; i < rooms.Count; i++)
            {
                DungeonRoom room = rooms[i];
                if (room.GridPosition == startPosition)
                {
                    room.SetRoomType(RoomType.Starter);
                    continue;
                }

                if (room.GridPosition == finalPosition)
                {
                    room.SetRoomType(RoomType.Final);
                    continue;
                }

                // Spread trap rooms predictably across the generated layout so every seeded run has variety
                // without risking disconnected or untyped rooms during the MVP phase.
                bool isTrapRoom = (i + trapOffset) % 5 == 0;
                room.SetRoomType(isTrapRoom ? RoomType.Trap : RoomType.Enemy);
            }
        }
    }
}
