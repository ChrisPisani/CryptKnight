using System.Collections.Generic;
using CryptKnight.Data;

namespace CryptKnight.Loot
{
    public static class LootItemEffectFormatter
    {
        public static string FormatEffects(LootItemDefinition itemDefinition, int quantity = 1)
        {
            if (itemDefinition == null)
            {
                return "No effect";
            }

            int safeQuantity = quantity < 1 ? 1 : quantity;
            List<string> effects = new List<string>();
            PlayerStatModifier modifier = itemDefinition.StatModifier;

            AddMaxHeartEffect(effects, modifier.MaxHealthBonus * safeQuantity);
            AddFloatEffect(effects, modifier.DamageBonus * safeQuantity, "damage");
            AddFloatEffect(effects, modifier.MovementSpeedBonus * safeQuantity, "movement speed");
            AddFloatEffect(effects, modifier.AttackRateBonus * safeQuantity, "attack speed");
            AddCountEffect(effects, modifier.ProjectileCountBonus * safeQuantity, "projectile", "projectiles");
            AddFloatEffect(effects, modifier.ProjectileSpeedBonus * safeQuantity, "projectile speed");
            AddCountEffect(effects, modifier.ProjectileBouncesBonus * safeQuantity, "projectile bounce", "projectile bounces");
            AddPercentageEffect(effects, modifier.ProjectileSizeBonus * safeQuantity, "projectile size");
            AddCountEffect(effects, itemDefinition.KeyAmount * safeQuantity, "key", "keys");

            return effects.Count == 0 ? "No effect" : string.Join("\n", effects);
        }

        private static void AddCountEffect(List<string> effects, int value, string singularLabel, string pluralLabel)
        {
            if (value == 0)
            {
                return;
            }

            string label = value == 1 || value == -1 ? singularLabel : pluralLabel;
            effects.Add($"{FormatSigned(value)} {label}");
        }

        private static void AddMaxHeartEffect(List<string> effects, int halfHeartValue)
        {
            if (halfHeartValue == 0)
            {
                return;
            }

            // Max health is stored as half hearts internally but visually is whole hearts
            float heartValue = halfHeartValue / 2f;
            string label = heartValue == 1f || heartValue == -1f ? "max heart" : "max hearts";
            effects.Add($"{FormatSigned(heartValue)} {label}");
        }

        private static void AddFloatEffect(List<string> effects, float value, string label)
        {
            if (value == 0f)
            {
                return;
            }

            effects.Add($"{FormatSigned(value)} {label}");
        }

        private static void AddPercentageEffect(List<string> effects, float value, string label)
        {
            if (value == 0f)
            {
                return;
            }

            effects.Add($"{FormatSigned(value * 100f)}% {label}");
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private static string FormatSigned(float value)
        {
            string formatted = value.ToString("0.##");
            return value > 0f ? $"+{formatted}" : formatted;
        }
    }
}
