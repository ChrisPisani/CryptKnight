using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public sealed class DungeonRoom
    {
        private readonly Dictionary<RoomDirection, Vector2Int> connections = new Dictionary<RoomDirection, Vector2Int>();

        public DungeonRoom(Vector2Int gridPosition, RoomType roomType)
        {
            GridPosition = gridPosition;
            RoomType = roomType;
        }

        public Vector2Int GridPosition { get; }
        public RoomType RoomType { get; private set; }
        public IReadOnlyDictionary<RoomDirection, Vector2Int> Connections => connections;

        public void SetRoomType(RoomType roomType)
        {
            RoomType = roomType;
        }

        public void Connect(RoomDirection direction, Vector2Int targetPosition)
        {
            connections[direction] = targetPosition;
        }

        public bool TryGetConnection(RoomDirection direction, out Vector2Int targetPosition)
        {
            return connections.TryGetValue(direction, out targetPosition);
        }

        public bool IsConnectedTo(Vector2Int targetPosition)
        {
            foreach (Vector2Int connectedPosition in connections.Values)
            {
                if (connectedPosition == targetPosition)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
