using System;

namespace CryptKnight.Loot
{
    [Serializable]
    public sealed class LootRarityWeights
    {
        public LootRarityWeights(float common, float uncommon, float rare, float legendary)
        {
            Common = Math.Max(0f, common);
            Uncommon = Math.Max(0f, uncommon);
            Rare = Math.Max(0f, rare);
            Legendary = Math.Max(0f, legendary);
        }

        public float Common { get; }
        public float Uncommon { get; }
        public float Rare { get; }
        public float Legendary { get; }

        public static LootRarityWeights CommonOnly { get; } = new LootRarityWeights(1f, 0f, 0f, 0f);

        public float GetWeight(LootRarity rarity)
        {
            switch (rarity)
            {
                case LootRarity.Uncommon:
                    return Uncommon;
                case LootRarity.Rare:
                    return Rare;
                case LootRarity.Legendary:
                    return Legendary;
                default:
                    return Common;
            }
        }
    }
}
