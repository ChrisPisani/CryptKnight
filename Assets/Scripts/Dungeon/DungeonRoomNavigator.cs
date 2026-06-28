using UnityEngine;

namespace CryptKnight.Dungeon
{
    public sealed class DungeonRoomNavigator
    {
        private readonly DungeonLayout layout;

        public DungeonRoomNavigator(DungeonLayout dungeonLayout)
        {
            layout = dungeonLayout;
            CurrentRoom = layout.StartRoom;
        }

        public DungeonLayout Layout => layout;
        public DungeonRoom CurrentRoom { get; private set; }

        public bool TryMove(RoomDirection direction)
        {
            if (!CurrentRoom.TryGetConnection(direction, out Vector2Int targetPosition))
            {
                return false;
            }

            if (!layout.TryGetRoom(targetPosition, out DungeonRoom nextRoom))
            {
                return false;
            }

            CurrentRoom = nextRoom;
            return true;
        }
    }
}
