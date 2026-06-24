using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptKnight.Loot
{
    public sealed class LootSystem
    {
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

            return RollDrop(sourceType, (float)random.NextDouble(), random.Next(possibleItems.Count), itemFilter);
        }

        public LootDropResult RollDrop(LootSourceType sourceType, float chanceRoll, int itemRoll)
        {
            return RollDrop(sourceType, chanceRoll, itemRoll, null);
        }

        public LootDropResult RollDrop(LootSourceType sourceType, float chanceRoll, int itemRoll, Predicate<LootItemDefinition> itemFilter)
        {
            IReadOnlyList<LootItemDefinition> possibleItems = GetFilteredItems(sourceType, itemFilter);
            if (possibleItems.Count == 0 || chanceRoll >= configuration.GetDropRate(sourceType))
            {
                return LootDropResult.NoDrop();
            }

            // Keep item selection stable even if invalid table values are passed around.
            int itemIndex = ((itemRoll % possibleItems.Count) + possibleItems.Count) % possibleItems.Count;
            return new LootDropResult(possibleItems[itemIndex]);
        }

        private IReadOnlyList<LootItemDefinition> GetFilteredItems(LootSourceType sourceType, Predicate<LootItemDefinition> itemFilter)
        {
            IReadOnlyList<LootItemDefinition> possibleItems = configuration.GetItemsForSource(sourceType);
            return itemFilter == null ? possibleItems : possibleItems.Where(item => itemFilter(item)).ToArray();
        }
    }
}
