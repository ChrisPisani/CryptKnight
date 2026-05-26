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
        private readonly List<LootItemDefinition> items;

        public LootTableConfiguration(IEnumerable<LootItemDefinition> items, IReadOnlyDictionary<LootSourceType, float> sourceDropRates)
        {
            this.items = new List<LootItemDefinition>(items ?? Enumerable.Empty<LootItemDefinition>());
            this.sourceDropRates = new Dictionary<LootSourceType, float>();

            if (sourceDropRates == null)
            {
                return;
            }

            foreach (KeyValuePair<LootSourceType, float> sourceRate in sourceDropRates)
            {
                this.sourceDropRates[sourceRate.Key] = ClampChance(sourceRate.Value);
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
                        item.keyAmount));
                }
            }

            return new LootTableConfiguration(configuredItems, dropRates);
        }

        private static LootTableConfiguration CreateBuiltInFallback()
        {
            LootSourceType[] allSources =
            {
                LootSourceType.Enemy,
                LootSourceType.Chest,
                LootSourceType.Shop
            };

            return new LootTableConfiguration(
                new[]
                {
                    CreateFallbackItem("heart_container", "Monster Heart", "Gain one full heart container.", new PlayerStatModifier(maxHealthBonus: 2), allSources),
                    CreateFallbackItem("damage_up", "Spinach", "Increase attack damage by 1.", new PlayerStatModifier(damageBonus: 1), allSources),
                    CreateFallbackItem("speed_up", "Bottled Lightning", "Increase movement speed by 1.", new PlayerStatModifier(movementSpeedBonus: 1f), allSources),
                    CreateFallbackItem("attack_rate_up", "Chili Pepper", "Increase attack rate by 0.2 shots per second.", new PlayerStatModifier(attackRateBonus: 0.2f), allSources),
                    CreateFallbackItem("key", "Key", "Gain 1 key.", new PlayerStatModifier(), allSources, 1)
                },
                new Dictionary<LootSourceType, float>
                {
                    { LootSourceType.Enemy, 0.10f },
                    { LootSourceType.Chest, 1f },
                    { LootSourceType.Shop, 1f }
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
                $"Assets/Art/Items/{itemId}.png",
                keyAmount);
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
                attackRateBonus: modifier.attackRateBonus);
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
            public LootItemConfig[] items = Array.Empty<LootItemConfig>();
        }

        [Serializable]
        private sealed class LootSourceDropRateConfig
        {
            public string source = string.Empty;
            public float dropRate = 0f;
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
            public string[] allowedSources = Array.Empty<string>();
            public PlayerStatModifierConfig statModifier = new PlayerStatModifierConfig();
        }

        [Serializable]
        private sealed class PlayerStatModifierConfig
        {
            public int maxHealthBonus = 0;
            public int damageBonus = 0;
            public float movementSpeedBonus = 0f;
            public float attackRateBonus = 0f;
        }
    }
}
