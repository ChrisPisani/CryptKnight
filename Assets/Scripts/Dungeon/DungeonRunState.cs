using System;
using System.Collections.Generic;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using CryptKnight.Traps;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public sealed class DungeonRunState
    {
        private readonly Dictionary<Vector2Int, DungeonRoomRuntimeState> rooms;

        public DungeonRunState(
            DungeonLayout layout,
            Dictionary<Vector2Int, DungeonRoomRuntimeState> roomStates,
            LootTableConfiguration lootConfiguration,
            int runSeed,
            FinalEncounterConfiguration finalEncounterConfiguration = null,
            TrapRoomConfiguration trapConfiguration = null,
            int floorNumber = 1,
            EnemyDifficulty difficulty = EnemyDifficulty.Normal)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            rooms = roomStates ?? throw new ArgumentNullException(nameof(roomStates));
            LootConfiguration = lootConfiguration ?? throw new ArgumentNullException(nameof(lootConfiguration));
            Navigator = new DungeonRoomNavigator(layout);
            LootSystem = new LootSystem(lootConfiguration);
            LootRandom = new System.Random(runSeed ^ 0x4C4F4F54);
            FinalEncounterConfig = finalEncounterConfiguration ?? FinalEncounterConfiguration.CreateDefault();
            TrapConfiguration = trapConfiguration ?? TrapRoomConfiguration.CreateDefault();
            FloorNumber = Math.Max(1, floorNumber);
            FloorSeed = runSeed;
            Difficulty = difficulty;
        }

        public DungeonLayout Layout { get; }
        public DungeonRoomNavigator Navigator { get; }
        public LootTableConfiguration LootConfiguration { get; }
        public LootSystem LootSystem { get; }
        public System.Random LootRandom { get; }
        public FinalEncounterConfiguration FinalEncounterConfig { get; }
        public TrapRoomConfiguration TrapConfiguration { get; }
        public int FloorNumber { get; }
        public int FloorSeed { get; }
        public EnemyDifficulty Difficulty { get; }
        public IReadOnlyDictionary<Vector2Int, DungeonRoomRuntimeState> Rooms => rooms;
        public DungeonRoomRuntimeState CurrentRoomState => GetRoomState(Navigator.CurrentRoom.GridPosition);

        public DungeonRoomRuntimeState GetRoomState(Vector2Int roomPosition)
        {
            if (!rooms.TryGetValue(roomPosition, out DungeonRoomRuntimeState state))
            {
                throw new InvalidOperationException($"No runtime state exists for dungeon room {roomPosition}.");
            }

            return state;
        }
    }
}
