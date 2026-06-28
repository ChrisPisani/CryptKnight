using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public sealed class DungeonLayout
    {
        private readonly Dictionary<Vector2Int, DungeonRoom> rooms;

        public DungeonLayout(int width, int height, IEnumerable<DungeonRoom> generatedRooms, Vector2Int startPosition, Vector2Int finalPosition)
        {
            Width = width;
            Height = height;
            rooms = generatedRooms.ToDictionary(room => room.GridPosition);
            StartPosition = startPosition;
            FinalPosition = finalPosition;
        }

        public int Width { get; }
        public int Height { get; }
        public Vector2Int StartPosition { get; }
        public Vector2Int FinalPosition { get; }
        public IReadOnlyCollection<DungeonRoom> Rooms => rooms.Values;
        public DungeonRoom StartRoom => rooms[StartPosition];
        public DungeonRoom FinalRoom => rooms[FinalPosition];

        public bool TryGetRoom(Vector2Int position, out DungeonRoom room)
        {
            return rooms.TryGetValue(position, out room);
        }

        public bool AreAllRoomsConnected()
        {
            if (rooms.Count == 0)
            {
                return false;
            }

            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(StartPosition);
            visited.Add(StartPosition);

            while (frontier.Count > 0)
            {
                Vector2Int position = frontier.Dequeue();
                DungeonRoom room = rooms[position];
                foreach (Vector2Int connectedPosition in room.Connections.Values)
                {
                    if (rooms.ContainsKey(connectedPosition) && visited.Add(connectedPosition))
                    {
                        frontier.Enqueue(connectedPosition);
                    }
                }
            }

            return visited.Count == rooms.Count;
        }
    }
}
