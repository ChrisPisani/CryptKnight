using CryptKnight.Data;

namespace CryptKnight.Loot
{
    public static class LootItemCollector
    {
        private const string KeyItemId = "key";

        public static bool ApplyToRun(GameRunState runState, LootItemDefinition itemDefinition)
        {
            if (runState == null || itemDefinition == null || !runState.IsActive)
            {
                return false;
            }

            runState.AddStatModifier(itemDefinition.StatModifier);
            runState.AddKeys(itemDefinition.KeyAmount);

            // Keys have their own slot not in powerups, so they only update the key counter
            if (itemDefinition.ItemId != KeyItemId)
            {
                runState.AddCollectedItem(itemDefinition.ItemId, itemDefinition.DisplayName);
            }

            return true;
        }
    }
}
