using UnityEngine;

namespace CryptKnight.Loot
{
    public static class LootRarityPresentation
    {
        private static readonly Color CommonColor = new Color(0.84f, 0.82f, 0.76f, 1f);
        private static readonly Color UncommonColor = new Color(0.33f, 0.79f, 0.42f, 1f);
        private static readonly Color RareColor = new Color(0.31f, 0.64f, 1f, 1f);
        private static readonly Color LegendaryColor = new Color(0.95f, 0.72f, 0.29f, 1f);

        public static string GetLabel(LootRarity rarity)
        {
            return rarity.ToString();
        }

        public static Color GetColor(LootRarity rarity)
        {
            switch (rarity)
            {
                case LootRarity.Uncommon:
                    return UncommonColor;
                case LootRarity.Rare:
                    return RareColor;
                case LootRarity.Legendary:
                    return LegendaryColor;
                default:
                    return CommonColor;
            }
        }
    }
}
