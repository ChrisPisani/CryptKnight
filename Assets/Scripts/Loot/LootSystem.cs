using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptKnight.Loot
{
    public sealed class LootSystem
    {
        private static readonly LootRarity[] Rarities =
        {
            LootRarity.Common,
            LootRarity.Uncommon,
            LootRarity.Rare,
            LootRarity.Legendary
        };

        private readonly LootTableConfiguration configuration;

        public LootSystem(LootTableConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IReadOnlyList<LootItemDefinition> GetItemsForSource(LootSourceType sourceType)
        {
            return configuration.GetItemsForSource(sourceType);
        }

        public LootDropResult RollDrop(LootSourceType sourceType, Random random)
        {
            return RollDrop(sourceType, random, null);
        }

        public LootDropResult RollDrop(LootSourceType sourceType, Random random, Predicate<LootItemDefinition> itemFilter)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            IReadOnlyList<LootItemDefinition> possibleItems = GetFilteredItems(sourceType, itemFilter);
            if (possibleItems.Count == 0)
            {
                return LootDropResult.NoDrop();
            }

            return RollDrop(
                sourceType,
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                random.Next(),
                itemFilter);
        }

        public LootDropResult RollDrop(LootSourceType sourceType, float chanceRoll, int itemRoll)
        {
            return RollDrop(sourceType, chanceRoll, 0f, itemRoll, null);
        }

        public LootDropResult RollDrop(LootSourceType sourceType, float chanceRoll, int itemRoll, Predicate<LootItemDefinition> itemFilter)
        {
            return RollDrop(sourceType, chanceRoll, 0f, itemRoll, itemFilter);
        }

        public LootDropResult RollDrop(
            LootSourceType sourceType,
            float chanceRoll,
            float rarityRoll,
            int itemRoll)
        {
            return RollDrop(sourceType, chanceRoll, rarityRoll, itemRoll, null);
        }

        public LootDropResult RollDrop(
            LootSourceType sourceType,
            float chanceRoll,
            float rarityRoll,
            int itemRoll,
            Predicate<LootItemDefinition> itemFilter)
        {
            IReadOnlyList<LootItemDefinition> possibleItems = GetFilteredItems(sourceType, itemFilter);
            if (possibleItems.Count == 0 || chanceRoll >= configuration.GetDropRate(sourceType))
            {
                return LootDropResult.NoDrop();
            }

            IReadOnlyList<LootItemDefinition> rarityItems = GetItemsForRolledRarity(
                sourceType,
                possibleItems,
                rarityRoll);
            // Keep item selection stable even if invalid table values are passed around.
            int itemIndex = ((itemRoll % rarityItems.Count) + rarityItems.Count) % rarityItems.Count;
            return new LootDropResult(rarityItems[itemIndex]);
        }

        private IReadOnlyList<LootItemDefinition> GetFilteredItems(LootSourceType sourceType, Predicate<LootItemDefinition> itemFilter)
        {
            IReadOnlyList<LootItemDefinition> possibleItems = configuration.GetItemsForSource(sourceType);
            return itemFilter == null ? possibleItems : possibleItems.Where(item => itemFilter(item)).ToArray();
        }

        private IReadOnlyList<LootItemDefinition> GetItemsForRolledRarity(
            LootSourceType sourceType,
            IReadOnlyList<LootItemDefinition> possibleItems,
            float rarityRoll)
        {
            LootRarityWeights weights = configuration.GetRarityWeights(sourceType);
            float totalWeight = 0f;
            for (int i = 0; i < Rarities.Length; i++)
            {
                LootRarity rarity = Rarities[i];
                if (HasItemsOfRarity(possibleItems, rarity))
                {
                    totalWeight += weights.GetWeight(rarity);
                }
            }

            // A source with missing weights should still honor a successful drop roll.
            if (totalWeight <= 0f)
            {
                return possibleItems;
            }

            float normalizedRoll = NormalizeRoll(rarityRoll);
            float targetWeight = normalizedRoll * totalWeight;
            float cumulativeWeight = 0f;
            for (int i = 0; i < Rarities.Length; i++)
            {
                LootRarity rarity = Rarities[i];
                if (!HasItemsOfRarity(possibleItems, rarity))
                {
                    continue;
                }

                cumulativeWeight += weights.GetWeight(rarity);
                if (targetWeight < cumulativeWeight || i == Rarities.Length - 1)
                {
                    return possibleItems.Where(item => item.Rarity == rarity).ToArray();
                }
            }

            return possibleItems;
        }

        private static bool HasItemsOfRarity(
            IReadOnlyList<LootItemDefinition> items,
            LootRarity rarity)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Rarity == rarity)
                {
                    return true;
                }
            }

            return false;
        }

        private static float NormalizeRoll(float roll)
        {
            if (float.IsNaN(roll) || float.IsInfinity(roll))
            {
                return 0f;
            }

            float normalized = roll - (float)Math.Floor(roll);
            return normalized < 0f ? normalized + 1f : normalized;
        }
    }
}
