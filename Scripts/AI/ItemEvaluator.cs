using PitsOfDespair.Brands;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI;

/// <summary>
/// Utility class for AI item evaluation and prioritization.
/// Helps AI decide which items are worth picking up.
/// </summary>
public static class ItemEvaluator
{
    /// <summary>
    /// Base scores for different item types.
    /// </summary>
    private const int HealingItemScore = 50;
    private const int OffensiveItemScore = 40;
    private const int WeaponScore = 30;
    private const int ArmorScore = 30;
    private const int AmmoScore = 20;
    private const int DefaultScore = 10;

    /// <summary>
    /// Brand scoring values.
    /// </summary>
    private const int DamageBrandScore = 15;
    private const int AccuracyBrandScore = 10;
    private const int ElementalBrandScore = 20;
    private const int VampiricBrandScore = 25;

    /// <summary>
    /// Evaluates the desirability of an item for a specific entity.
    /// Higher score = more desirable.
    /// </summary>
    /// <param name="itemData">The item template to evaluate.</param>
    /// <param name="entity">The entity considering the item.</param>
    /// <returns>A score representing item desirability (0 = not useful, higher = more desirable).</returns>
    public static int EvaluateItem(ItemData itemData, BaseEntity entity)
    {
        if (itemData == null)
            return 0;

        int score = 0;

        // Check for healing items - especially valuable when damaged
        if (HasHealingEffect(itemData))
        {
            score += HealingItemScore;

            // Bonus if entity is damaged
            var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health != null)
            {
                float hpRatio = (float)health.CurrentHealth / health.MaxHealth;
                if (hpRatio < 0.5f)
                {
                    score += 30; // Extra value when damaged
                }
            }
        }

        // Check for offensive items (targeted items)
        if (itemData.RequiresTargeting())
        {
            score += OffensiveItemScore;
        }

        // Check equipment types
        string itemType = itemData.Type?.ToLower() ?? "";
        switch (itemType)
        {
            case "weapon":
                score += WeaponScore;
                // Check if better than current weapon
                if (IsBetterWeapon(itemData, entity))
                {
                    score += 20;
                }
                break;

            case "armor":
                score += ArmorScore;
                // Check if better than current armor
                if (IsBetterArmor(itemData, entity))
                {
                    score += 20;
                }
                break;

            case "ammo":
                score += AmmoScore;
                // Check if entity has a ranged weapon
                if (HasRangedWeapon(entity))
                {
                    score += 15;
                }
                break;

            default:
                score += DefaultScore;
                break;
        }

        return score;
    }

    /// <summary>
    /// Evaluates the desirability of an item instance for a specific entity.
    /// Includes brand evaluation for enchanted items.
    /// </summary>
    /// <param name="itemInstance">The item instance to evaluate.</param>
    /// <param name="entity">The entity considering the item.</param>
    /// <returns>A score representing item desirability.</returns>
    public static int EvaluateItem(ItemInstance itemInstance, BaseEntity entity)
    {
        if (itemInstance?.Template == null)
            return 0;

        // Start with base item evaluation
        int score = EvaluateItem(itemInstance.Template, entity);

        // Add brand value
        score += EvaluateBrands(itemInstance);

        return score;
    }

    /// <summary>
    /// Evaluates the total value of brands on an item.
    /// </summary>
    /// <param name="itemInstance">The item instance to evaluate.</param>
    /// <returns>Combined score from all brands.</returns>
    public static int EvaluateBrands(ItemInstance itemInstance)
    {
        if (itemInstance == null)
            return 0;

        int brandScore = 0;
        foreach (var brand in itemInstance.GetBrands())
        {
            brandScore += brand switch
            {
                VampiricBrand => VampiricBrandScore,
                ElementalBrand => ElementalBrandScore,
                IDamageBrand db => DamageBrandScore + (db.GetDamageBonus() * 5),
                IHitBrand hb => AccuracyBrandScore + (hb.GetHitBonus() * 3),
                _ => 5 // Default brand value
            };
        }

        return brandScore;
    }

    /// <summary>
    /// Checks if item has any healing effects.
    /// </summary>
    private static bool HasHealingEffect(ItemData itemData)
    {
        foreach (var effectDef in itemData.Effects)
        {
            if (effectDef.Type?.ToLower() == "heal")
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the weapon is better than currently equipped.
    /// Simplified: any weapon is considered valuable if entity doesn't have one equipped.
    /// </summary>
    private static bool IsBetterWeapon(ItemData itemData, BaseEntity entity)
    {
        // If entity has no attack component or only natural attacks, weapon is valuable
        var attackComponent = entity.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
            return true;

        // If the new item has attack data, it's potentially an upgrade
        return itemData.Attack != null;
    }

    /// <summary>
    /// Checks if the armor is better than currently equipped.
    /// Simplified: any armor with armor_modifier effect is considered valuable.
    /// </summary>
    private static bool IsBetterArmor(ItemData itemData, BaseEntity entity)
    {
        // Any armor is valuable if entity has none
        // Check if item has any armor_modifier effect
        foreach (var effect in itemData.Effects)
        {
            if (effect.Type?.ToLower() == "apply_condition" &&
                effect.ConditionType?.ToLower() == "armor_modifier" &&
                effect.Amount > 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if entity has any ranged attacks available.
    /// </summary>
    private static bool HasRangedWeapon(BaseEntity entity)
    {
        var attackComponent = entity.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
            return false;

        // Check if any attack is ranged
        foreach (var attack in attackComponent.Attacks)
        {
            if (attack != null && attack.Type == AttackType.Ranged)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if an item is worth picking up at all.
    /// Returns false for items the AI has no use for.
    /// </summary>
    public static bool IsItemWorthPicking(ItemData itemData, BaseEntity entity)
    {
        // Always worth picking up consumables with effects
        if (itemData.GetIsConsumable() && itemData.Effects.Count > 0)
            return true;

        // Check if entity can equip the item
        if (itemData.GetIsEquippable())
        {
            var equipComponent = entity.GetNodeOrNull<EquipComponent>("EquipComponent");
            if (equipComponent != null)
            {
                return true;
            }
        }

        // Charged items are useful
        if (itemData.GetMaxCharges() > 0)
            return true;

        return false;
    }
}
