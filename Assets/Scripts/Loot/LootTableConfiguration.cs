using System;
using System.Collections.Generic;
using System.Linq;
using CryptKnight.Data;
using UnityEngine;

namespace CryptKnight.Loot
{
    public sealed class LootTableConfiguration
    {
        private const string DefaultResourcePath = "Loot/loot_table";

        private readonly Dictionary<LootSourceType, float> sourceDropRates;
        private readonly Dictionary<LootSourceType, LootRarityWeights> sourceRarityWeights;
        private readonly List<LootItemDefinition> items;

        public LootTableConfiguration(
            IEnumerable<LootItemDefinition> items,
            IReadOnlyDictionary<LootSourceType, float> sourceDropRates,
            IReadOnlyDictionary<LootSourceType, LootRarityWeights> rarityWeights = null)
        {
            this.items = new List<LootItemDefinition>(items ?? Enumerable.Empty<LootItemDefinition>());
            this.sourceDropRates = new Dictionary<LootSourceType, float>();
            sourceRarityWeights = new Dictionary<LootSourceType, LootRarityWeights>();

            if (sourceDropRates != null)
            {
                foreach (KeyValuePair<LootSourceType, float> sourceRate in sourceDropRates)
                {
                    this.sourceDropRates[sourceRate.Key] = ClampChance(sourceRate.Value);
                }
            }

            if (rarityWeights == null)
            {
                return;
            }

            foreach (KeyValuePair<LootSourceType, LootRarityWeights> sourceWeights in rarityWeights)
            {
                if (sourceWeights.Value != null)
                {
                    sourceRarityWeights[sourceWeights.Key] = sourceWeights.Value;
                }
            }
        }

        public IReadOnlyList<LootItemDefinition> Items => items;

        public float GetDropRate(LootSourceType sourceType)
        {
            return sourceDropRates.TryGetValue(sourceType, out float dropRate) ? dropRate : 0f;
        }

        public IReadOnlyList<LootItemDefinition> GetItemsForSource(LootSourceType sourceType)
        {
            return items.Where(item => item.CanAppearFrom(sourceType)).ToArray();
        }

        public LootRarityWeights GetRarityWeights(LootSourceType sourceType)
        {
            return sourceRarityWeights.TryGetValue(sourceType, out LootRarityWeights weights)
                ? weights
                : LootRarityWeights.CommonOnly;
        }

        public static LootTableConfiguration CreateDefault()
        {
            TextAsset configAsset = Resources.Load<TextAsset>(DefaultResourcePath);
            return configAsset != null ? FromJson(configAsset.text) : CreateBuiltInFallback();
        }

        public static LootTableConfiguration FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new LootTableConfiguration(Array.Empty<LootItemDefinition>(), new Dictionary<LootSourceType, float>());
            }

            LootTableConfigFile configFile = JsonUtility.FromJson<LootTableConfigFile>(json);
            return FromConfigFile(configFile);
        }

        private static LootTableConfiguration FromConfigFile(LootTableConfigFile configFile)
        {
            Dictionary<LootSourceType, float> dropRates = new Dictionary<LootSourceType, float>();
            if (configFile?.sourceDropRates != null)
            {
                foreach (LootSourceDropRateConfig sourceRate in configFile.sourceDropRates)
                {
                    if (TryParseSource(sourceRate.source, out LootSourceType sourceType))
                    {
                        dropRates[sourceType] = sourceRate.dropRate;
                    }
                }
            }

            Dictionary<LootSourceType, LootRarityWeights> rarityWeights = new Dictionary<LootSourceType, LootRarityWeights>();
            if (configFile?.sourceRarityWeights != null)
            {
                foreach (LootSourceRarityWeightsConfig sourceWeights in configFile.sourceRarityWeights)
                {
                    if (TryParseSource(sourceWeights.source, out LootSourceType sourceType))
                    {
                        rarityWeights[sourceType] = new LootRarityWeights(
                            sourceWeights.common,
                            sourceWeights.uncommon,
                            sourceWeights.rare,
                            sourceWeights.legendary);
                    }
                }
            }

            List<LootItemDefinition> configuredItems = new List<LootItemDefinition>();
            if (configFile?.items != null)
            {
                foreach (LootItemConfig item in configFile.items)
                {
                    if (string.IsNullOrWhiteSpace(item.itemId))
                    {
                        continue;
                    }

                    configuredItems.Add(new LootItemDefinition(
                        item.itemId,
                        item.displayName,
                        item.description,
                        CreateStatModifier(item.statModifier),
                        ParseSources(item.allowedSources),
                        GetConfiguredIconPath(item),
                        item.keyAmount,
                        ParseRarity(item.rarity)));
                }
            }

            return new LootTableConfiguration(configuredItems, dropRates, rarityWeights);
        }

        private static LootTableConfiguration CreateBuiltInFallback()
        {
            LootSourceType[] allSources =
            {
                LootSourceType.Enemy,
                LootSourceType.Chest,
                LootSourceType.RoomClear,
                LootSourceType.Shop
            };

            return new LootTableConfiguration(
                new[]
                {
                    CreateFallbackItem("heart_container", "Monster Heart", "It still beats with stubborn, impossible life.", new PlayerStatModifier(maxHealthBonus: 2), allSources),
                    CreateFallbackItem("damage_up", "Spinach", "A dented tin stamped with a hero no one remembers.", new PlayerStatModifier(damageBonus: 1), allSources),
                    CreateFallbackItem("speed_up", "Bottled Lightning", "Stormlight claws at the glass, desperate to escape.", new PlayerStatModifier(movementSpeedBonus: 1f), allSources),
                    CreateFallbackItem("attack_rate_up", "Chili Pepper", "Even the dead keep a safe distance from its heat.", new PlayerStatModifier(attackRateBonus: 0.2f), allSources),
                    CreateFallbackItem("key", "Key", "Cold iron teeth made for a lock deeper in the crypt.", new PlayerStatModifier(), allSources, 1)
                },
                new Dictionary<LootSourceType, float>
                {
                    { LootSourceType.Enemy, 0.08f },
                    { LootSourceType.Chest, 1f },
                    { LootSourceType.RoomClear, 0.20f },
                    { LootSourceType.Shop, 1f }
                },
                new Dictionary<LootSourceType, LootRarityWeights>
                {
                    { LootSourceType.Enemy, new LootRarityWeights(0.60f, 0.27f, 0.10f, 0.03f) },
                    { LootSourceType.RoomClear, new LootRarityWeights(0.60f, 0.27f, 0.10f, 0.03f) },
                    { LootSourceType.Chest, new LootRarityWeights(0.40f, 0.35f, 0.20f, 0.05f) },
                    { LootSourceType.Shop, new LootRarityWeights(0.40f, 0.35f, 0.20f, 0.05f) }
                });
        }

        private static LootItemDefinition CreateFallbackItem(string itemId, string displayName, string description, PlayerStatModifier statModifier, IEnumerable<LootSourceType> allowedSources, int keyAmount = 0)
        {
            return new LootItemDefinition(
                itemId,
                displayName,
                description,
                statModifier,
                allowedSources,
                $"Art/Items/{itemId}",
                keyAmount,
                LootRarity.Common);
        }

        private static float ClampChance(float chance)
        {
            return Math.Max(0f, Math.Min(1f, chance));
        }

        private static PlayerStatModifier CreateStatModifier(PlayerStatModifierConfig modifier)
        {
            if (modifier == null)
            {
                return new PlayerStatModifier();
            }

            return new PlayerStatModifier(
                maxHealthBonus: modifier.maxHealthBonus,
                damageBonus: modifier.damageBonus,
                movementSpeedBonus: modifier.movementSpeedBonus,
                attackRateBonus: modifier.attackRateBonus,
                projectileCountBonus: modifier.projectileCountBonus,
                projectileSpeedBonus: modifier.projectileSpeedBonus,
                projectileBouncesBonus: modifier.projectileBouncesBonus,
                projectileSizeBonus: modifier.projectileSizeBonus);
        }

        private static IEnumerable<LootSourceType> ParseSources(IEnumerable<string> sourceNames)
        {
            if (sourceNames == null)
            {
                yield break;
            }

            foreach (string sourceName in sourceNames)
            {
                if (TryParseSource(sourceName, out LootSourceType sourceType))
                {
                    yield return sourceType;
                }
            }
        }

        private static bool TryParseSource(string sourceName, out LootSourceType sourceType)
        {
            return Enum.TryParse(sourceName, true, out sourceType);
        }

        private static LootRarity ParseRarity(string rarityName)
        {
            return Enum.TryParse(rarityName, true, out LootRarity rarity)
                ? rarity
                : LootRarity.Common;
        }

        private static string GetConfiguredIconPath(LootItemConfig item)
        {
            if (!string.IsNullOrWhiteSpace(item.iconAssetPath))
            {
                return item.iconAssetPath;
            }

            return item.iconResourcePath ?? string.Empty;
        }

        [Serializable]
        private sealed class LootTableConfigFile
        {
            public LootSourceDropRateConfig[] sourceDropRates = Array.Empty<LootSourceDropRateConfig>();
            public LootSourceRarityWeightsConfig[] sourceRarityWeights = Array.Empty<LootSourceRarityWeightsConfig>();
            public LootItemConfig[] items = Array.Empty<LootItemConfig>();
        }

        [Serializable]
        private sealed class LootSourceDropRateConfig
        {
            public string source = string.Empty;
            public float dropRate = 0f;
        }

        [Serializable]
        private sealed class LootSourceRarityWeightsConfig
        {
            public string source = string.Empty;
            public float common = 0f;
            public float uncommon = 0f;
            public float rare = 0f;
            public float legendary = 0f;
        }

        [Serializable]
        private sealed class LootItemConfig
        {
            public string itemId = string.Empty;
            public string displayName = string.Empty;
            public string description = string.Empty;
            public string iconAssetPath = string.Empty;
            public string iconResourcePath = string.Empty;
            public int keyAmount = 0;
            public string rarity = string.Empty;
            public string[] allowedSources = Array.Empty<string>();
            public PlayerStatModifierConfig statModifier = new PlayerStatModifierConfig();
        }

        [Serializable]
        private sealed class PlayerStatModifierConfig
        {
            public int maxHealthBonus = 0;
            public float damageBonus = 0f;
            public float movementSpeedBonus = 0f;
            public float attackRateBonus = 0f;
            public int projectileCountBonus = 0;
            public float projectileSpeedBonus = 0f;
            public int projectileBouncesBonus = 0;
            public float projectileSizeBonus = 0f;
        }
    }
}
