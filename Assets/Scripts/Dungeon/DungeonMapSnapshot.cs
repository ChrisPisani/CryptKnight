using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public enum DungeonMapMarker
    {
        None,
        Unknown,
        Boss,
        Combat,
        Trap
    }

    public sealed class DungeonMapRoomInfo
    {
        public DungeonMapRoomInfo(
            Vector2Int gridPosition,
            DungeonMapMarker marker,
            bool isCurrent,
            bool isStarter,
            bool isVisited,
            bool isCleared,
            bool hasUnopenedChest)
        {
            GridPosition = gridPosition;
            Marker = marker;
            IsCurrent = isCurrent;
            IsStarter = isStarter;
            IsVisited = isVisited;
            IsCleared = isCleared;
            HasUnopenedChest = hasUnopenedChest;
        }

        public Vector2Int GridPosition { get; }
        public DungeonMapMarker Marker { get; }
        public bool IsCurrent { get; }
        public bool IsStarter { get; }
        public bool IsVisited { get; }
        public bool IsCleared { get; }
        public bool HasUnopenedChest { get; }
    }

    public sealed class DungeonMapConnection
    {
        public DungeonMapConnection(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }

        public Vector2Int From { get; }
        public Vector2Int To { get; }
    }

    public sealed class DungeonMapSnapshot
    {
        private readonly Dictionary<Vector2Int, DungeonMapRoomInfo> roomLookup;

        private DungeonMapSnapshot(
            int width,
            int height,
            List<DungeonMapRoomInfo> rooms,
            List<DungeonMapConnection> connections)
        {
            Width = width;
            Height = height;
            Rooms = rooms;
            Connections = connections;
            roomLookup = new Dictionary<Vector2Int, DungeonMapRoomInfo>();
            for (int i = 0; i < rooms.Count; i++)
            {
                roomLookup[rooms[i].GridPosition] = rooms[i];
            }
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<DungeonMapRoomInfo> Rooms { get; }
        public IReadOnlyList<DungeonMapConnection> Connections { get; }

        public bool TryGetRoom(Vector2Int position, out DungeonMapRoomInfo room)
        {
            return roomLookup.TryGetValue(position, out room);
        }

        public static DungeonMapSnapshot Create(DungeonRunState dungeon)
        {
            if (dungeon == null)
            {
                throw new ArgumentNullException(nameof(dungeon));
            }

            DungeonLayout layout = dungeon.Layout;
            bool bossDiscovered = IsBossDiscovered(dungeon);
            Vector2Int currentPosition = dungeon.Navigator.CurrentRoom.GridPosition;
            List<DungeonMapRoomInfo> rooms = new List<DungeonMapRoomInfo>();
            List<DungeonMapConnection> connections = new List<DungeonMapConnection>();

            foreach (DungeonRoom room in layout.Rooms)
            {
                DungeonRoomRuntimeState state = dungeon.GetRoomState(room.GridPosition);
                DungeonMapMarker marker = GetMarker(room, state, bossDiscovered);
                rooms.Add(new DungeonMapRoomInfo(
                    room.GridPosition,
                    marker,
                    room.GridPosition == currentPosition,
                    room.GridPosition == layout.StartPosition,
                    state.IsVisited,
                    state.IsCleared,
                    HasVisibleChest(state)));

                foreach (Vector2Int target in room.Connections.Values)
                {
                    if (ComesBefore(room.GridPosition, target))
                    {
                        connections.Add(new DungeonMapConnection(room.GridPosition, target));
                    }
                }
            }

            return new DungeonMapSnapshot(layout.Width, layout.Height, rooms, connections);
        }

        private static DungeonMapMarker GetMarker(
            DungeonRoom room,
            DungeonRoomRuntimeState state,
            bool bossDiscovered)
        {
            if (room.RoomType == RoomType.Final && bossDiscovered)
            {
                return DungeonMapMarker.Boss;
            }

            if (!state.IsVisited)
            {
                return DungeonMapMarker.Unknown;
            }

            switch (room.RoomType)
            {
                case RoomType.Enemy:
                    return DungeonMapMarker.Combat;
                case RoomType.Trap:
                    return DungeonMapMarker.Trap;
                case RoomType.Final:
                    return DungeonMapMarker.Boss;
                default:
                    return DungeonMapMarker.None;
            }
        }

        private static bool IsBossDiscovered(DungeonRunState dungeon)
        {
            DungeonLayout layout = dungeon.Layout;
            DungeonRoomRuntimeState finalState = dungeon.GetRoomState(layout.FinalPosition);
            if (finalState.IsVisited)
            {
                return true;
            }

            foreach (Vector2Int neighbor in layout.FinalRoom.Connections.Values)
            {
                if (dungeon.GetRoomState(neighbor).IsVisited)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasVisibleChest(DungeonRoomRuntimeState state)
        {
            if (!state.IsVisited)
            {
                return false;
            }

            for (int i = 0; i < state.Chests.Count; i++)
            {
                if (!state.Chests[i].IsOpened)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ComesBefore(Vector2Int first, Vector2Int second)
        {
            return first.x < second.x || (first.x == second.x && first.y < second.y);
        }
    }
}
