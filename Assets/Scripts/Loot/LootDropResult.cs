namespace CryptKnight.Loot
{
    public readonly struct LootDropResult
    {
        public LootDropResult(LootItemDefinition item)
        {
            Item = item;
        }

        public LootItemDefinition Item { get; }
        public bool HasDrop => Item != null;

        public static LootDropResult NoDrop()
        {
            return new LootDropResult(null);
        }
    }
}
