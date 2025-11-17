using System.Collections.Generic;
using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Data;

namespace PitsOfDespair.Systems;

/// <summary>
/// Factory for creating entities from JSON data.
/// Handles component instantiation based on data configuration.
/// </summary>
public partial class EntityFactory : Node
{
    private DataLoader _dataLoader;

    public override void _Ready()
    {
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
    }

    /// <summary>
    /// Create an entity from creature ID and position it on the grid.
    /// </summary>
    /// <param name="creatureId">The creature ID (JSON filename without extension).</param>
    /// <param name="position">The grid position to place the entity.</param>
    /// <returns>The created and configured BaseEntity, or null if creature not found.</returns>
    public BaseEntity CreateEntity(string creatureId, GridPosition position)
    {
        var data = _dataLoader.GetCreature(creatureId);
        if (data == null)
        {
            GD.PrintErr($"EntityFactory: Failed to create entity '{creatureId}' - creature data not found");
            return null;
        }

        return CreateEntityFromJsonData(data, position);
    }

    /// <summary>
    /// Create an item from item ID and position it on the grid.
    /// </summary>
    /// <param name="itemId">The item ID (JSON filename without extension).</param>
    /// <param name="position">The grid position to place the item.</param>
    /// <returns>The created and configured BaseEntity with ItemComponent, or null if item not found.</returns>
    public BaseEntity CreateItem(string itemId, GridPosition position)
    {
        var data = _dataLoader.GetItem(itemId);
        if (data == null)
        {
            GD.PrintErr($"EntityFactory: Failed to create item '{itemId}' - item data not found");
            return null;
        }

        // Create base entity for the item
        var entity = new BaseEntity
        {
            GridPosition = position,
            DisplayName = data.Name,
            Glyph = !string.IsNullOrEmpty(data.Glyph) ? data.Glyph : "?",
            GlyphColor = data.GetColor(),
            Name = data.Name // Set node name for debugging
        };

        // Add ItemComponent to mark as collectible item
        // Create ItemInstance with randomized/specified charges
        var itemInstance = new ItemInstance(data);
        var itemComponent = new ItemComponent
        {
            Name = "ItemComponent",
            Item = itemInstance // Store item instance with per-instance state
        };
        entity.AddChild(itemComponent);

        return entity;
    }

    /// <summary>
    /// Create an entity from EntityData and position it on the grid.
    /// DEPRECATED: Use CreateEntity(string creatureId, GridPosition position) instead.
    /// </summary>
    /// <param name="data">The EntityData resource defining the entity.</param>
    /// <param name="position">The grid position to place the entity.</param>
    /// <returns>The created and configured BaseEntity.</returns>
    [System.Obsolete("Use CreateEntity(string creatureId, GridPosition position) instead")]
    public BaseEntity CreateEntity(EntityData data, GridPosition position)
    {
        // Create base entity
        var entity = new BaseEntity
        {
            GridPosition = position,
            DisplayName = data.Name,
            Glyph = !string.IsNullOrEmpty(data.Glyph) ? data.Glyph : "?",
            GlyphColor = data.GlyphColor,
            Name = data.Name // Set node name for debugging
        };

        // Add MovementComponent if entity can move
        if (data.HasMovement)
        {
            var movementComponent = new MovementComponent
            {
                Name = "MovementComponent"
            };
            entity.AddChild(movementComponent);
        }

        // Add VisionComponent if vision range is specified
        if (data.VisionRange > 0)
        {
            var visionComponent = new VisionComponent
            {
                Name = "VisionComponent",
                VisionRange = data.VisionRange
            };
            entity.AddChild(visionComponent);
        }

        // Add HealthComponent if max HP is specified
        if (data.MaxHP > 0)
        {
            var healthComponent = new HealthComponent
            {
                Name = "HealthComponent",
                BaseMaxHP = data.MaxHP
            };
            entity.AddChild(healthComponent);
        }

        // Add AttackComponent if attacks are specified
        if (data.Attacks.Count > 0)
        {
            var attackComponent = new AttackComponent
            {
                Name = "AttackComponent",
                Attacks = data.Attacks
            };
            entity.AddChild(attackComponent);
        }

        // Add AIComponent if entity has AI behavior enabled
        if (data.HasAI)
        {
            var aiComponent = new AIComponent
            {
                Name = "AIComponent"
            };
            entity.AddChild(aiComponent);

            // Initialize AI with spawn position
            // Note: Must be called after AddChild so component is in tree
            aiComponent.Initialize(position);

            // Instantiate goals from creature data
            var goalsList = new List<string>();
            if (data.Goals != null && data.Goals.Count > 0)
            {
                goalsList.AddRange(data.Goals);
            }

            // Always add IdleGoal as the default fallback (unless explicitly included)
            if (!goalsList.Contains("Idle"))
            {
                goalsList.Add("Idle");
            }

            var goals = GoalFactory.CreateGoals(goalsList);
            aiComponent.SetGoals(goals);
        }

        return entity;
    }

    /// <summary>
    /// Create an entity from JSON creature data.
    /// </summary>
    private BaseEntity CreateEntityFromJsonData(CreatureData data, GridPosition position)
    {
        // Create base entity
        var entity = new BaseEntity
        {
            GridPosition = position,
            DisplayName = data.Name,
            Glyph = !string.IsNullOrEmpty(data.Glyph) ? data.Glyph : "?",
            GlyphColor = data.GetColor(),
            Name = data.Name // Set node name for debugging
        };

        // Add StatsComponent (required for combat system)
        var statsComponent = new StatsComponent
        {
            Name = "StatsComponent",
            BaseStrength = data.Strength,
            BaseAgility = data.Agility,
            BaseEndurance = data.Endurance,
            BaseWill = data.Will
        };
        entity.AddChild(statsComponent);

        // Add MovementComponent if entity can move
        if (data.HasMovement)
        {
            var movementComponent = new MovementComponent
            {
                Name = "MovementComponent"
            };
            entity.AddChild(movementComponent);
        }

        // Add VisionComponent if vision range is specified
        if (data.VisionRange > 0)
        {
            var visionComponent = new VisionComponent
            {
                Name = "VisionComponent",
                VisionRange = data.VisionRange
            };
            entity.AddChild(visionComponent);
        }

        // Add HealthComponent if max HP is specified
        if (data.MaxHP > 0)
        {
            var healthComponent = new HealthComponent
            {
                Name = "HealthComponent",
                BaseMaxHP = data.MaxHP // Will be modified by Endurance in _Ready
            };
            entity.AddChild(healthComponent);
        }

        // Add AttackComponent if entity has attacks OR can equip weapons
        if (data.Attacks.Count > 0 || data.GetCanEquip())
        {
            var naturalAttacks = new Godot.Collections.Array<AttackData>();

            if (data.Attacks.Count > 0)
            {
                // Use defined natural attacks from creature data
                foreach (var attackData in data.Attacks)
                {
                    var attack = new AttackData
                    {
                        Name = attackData.Name,
                        Type = attackData.Type,
                        DiceNotation = attackData.DiceNotation,
                        Range = attackData.Range
                    };
                    naturalAttacks.Add(attack);
                }
            }
            else
            {
                // No natural attacks defined - use default punch
                naturalAttacks.Add(GetDefaultNaturalAttack());
            }

            var attackComponent = new AttackComponent
            {
                Name = "AttackComponent",
                NaturalAttacks = naturalAttacks, // Store as natural attacks
                Attacks = naturalAttacks // Current attacks (will be updated if weapons equipped)
            };
            entity.AddChild(attackComponent);
        }

        // Add EquipComponent if creature can equip items
        if (data.GetCanEquip())
        {
            var equipComponent = new Scripts.Components.EquipComponent
            {
                Name = "EquipComponent"
            };
            entity.AddChild(equipComponent);

            // Load and equip creature equipment
            if (data.Equipment != null && data.Equipment.Count > 0)
            {
                foreach (var itemId in data.Equipment)
                {
                    var itemData = _dataLoader.GetItem(itemId);
                    if (itemData == null)
                    {
                        GD.PushWarning($"EntityFactory: Equipment item '{itemId}' not found for creature '{data.Name}'");
                        continue;
                    }

                    // Create item instance
                    var itemInstance = new ItemInstance(itemData);

                    // Get equipment slot from item
                    var equipSlot = itemData.GetEquipmentSlot();
                    if (equipSlot == EquipmentSlot.None)
                    {
                        GD.PushWarning($"EntityFactory: Item '{itemId}' has no equipment slot for creature '{data.Name}'");
                        continue;
                    }

                    // Equip the item
                    equipComponent.EquipCreatureItem(itemInstance, equipSlot);
                }
            }
        }

        // Add AIComponent if entity has AI behavior enabled
        if (data.HasAI)
        {
            var aiComponent = new AIComponent
            {
                Name = "AIComponent"
            };
            entity.AddChild(aiComponent);

            // Initialize AI with spawn position
            // Note: Must be called after AddChild so component is in tree
            aiComponent.Initialize(position);

            // Instantiate goals from creature data
            var goalsList = new List<string>();
            if (data.Goals != null && data.Goals.Count > 0)
            {
                goalsList.AddRange(data.Goals);
            }

            // Always add IdleGoal as the default fallback (unless explicitly included)
            if (!goalsList.Contains("Idle"))
            {
                goalsList.Add("Idle");
            }

            var goals = GoalFactory.CreateGoals(goalsList);
            aiComponent.SetGoals(goals);
        }

        return entity;
    }

    /// <summary>
    /// Creates the default natural attack (punch) for entities with no defined natural attacks.
    /// </summary>
    private AttackData GetDefaultNaturalAttack()
    {
        return new AttackData
        {
            Name = "punch",
            Type = AttackType.Melee,
            DiceNotation = "1d2"
        };
    }
}
