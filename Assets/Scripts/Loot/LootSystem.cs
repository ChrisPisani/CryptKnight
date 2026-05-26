using System;
using System.Collections.Generic;

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
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            IReadOnlyList<LootItemDefinition> possibleItems = configuration.GetItemsForSource(sourceType);
            if (possibleItems.Count == 0)
            {
                return LootDropResult.NoDrop();
            }

            return RollDrop(sourceType, (float)random.NextDouble(), random.Next(possibleItems.Count));
        }

        public LootDropResult RollDrop(LootSourceType sourceType, float chanceRoll, int itemRoll)
        {
            IReadOnlyList<LootItemDefinition> possibleItems = configuration.GetItemsForSource(sourceType);
            if (possibleItems.Count == 0 || chanceRoll >= configuration.GetDropRate(sourceType))
            {
                return LootDropResult.NoDrop();
            }

            // Keep item selection stable even if invalid table values are passed around.
            int itemIndex = ((itemRoll % possibleItems.Count) + possibleItems.Count) % possibleItems.Count;
            return new LootDropResult(possibleItems[itemIndex]);
        }
    }
}
