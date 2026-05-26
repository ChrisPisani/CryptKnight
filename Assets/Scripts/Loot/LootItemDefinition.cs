using System;
using System.Collections.Generic;
using System.Linq;
using CryptKnight.Data;

namespace CryptKnight.Loot
{
    [Serializable]
    public sealed class LootItemDefinition
    {
        private readonly HashSet<LootSourceType> allowedSources;

        public LootItemDefinition(string itemId, string displayName, string description, PlayerStatModifier statModifier, IEnumerable<LootSourceType> allowedSources, string iconAssetPath = "", int keyAmount = 0)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Loot item definitions need a stable id.", nameof(itemId));
            }

            ItemId = itemId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
            Description = description ?? string.Empty;
            IconAssetPath = iconAssetPath ?? string.Empty;
            KeyAmount = Math.Max(0, keyAmount);
            StatModifier = statModifier ?? new PlayerStatModifier();
            this.allowedSources = new HashSet<LootSourceType>(allowedSources ?? Enumerable.Empty<LootSourceType>());
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string IconAssetPath { get; }
        public int KeyAmount { get; }
        public PlayerStatModifier StatModifier { get; }
        public IReadOnlyCollection<LootSourceType> AllowedSources => allowedSources;

        public bool CanAppearFrom(LootSourceType sourceType)
        {
            return allowedSources.Contains(sourceType);
        }
    }
}
