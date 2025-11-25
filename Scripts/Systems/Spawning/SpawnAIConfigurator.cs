using System.Collections.Generic;
using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Components;
using PitsOfDespair.AI.Patrol;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Configures AI behavior for spawned encounters.
/// Sets up protection targets, territory behavior, patrol routes, and initial states.
/// </summary>
public class SpawnAIConfigurator
{
    private readonly MapSystem _mapSystem;

    public SpawnAIConfigurator(MapSystem mapSystem)
    {
        _mapSystem = mapSystem;
    }

    /// <summary>
    /// Configures all creatures in an encounter with appropriate AI settings.
    /// </summary>
    public void ConfigureEncounter(SpawnedEncounter encounter)
    {
        if (encounter?.Creatures == null || encounter.Creatures.Count == 0)
            return;

        var aiConfig = encounter.Template?.AiConfig;

        // Configure each creature
        foreach (var creature in encounter.Creatures)
        {
            ConfigureCreatureAI(creature, encounter, aiConfig);
        }

        // Configure group relationships
        ConfigureGroupRelationships(encounter, aiConfig);

        // Configure grouped patrol behavior
        ConfigureGroupedPatrol(encounter);
    }

    /// <summary>
    /// Configures a single creature's AI settings.
    /// </summary>
    private void ConfigureCreatureAI(
        SpawnedCreature creature,
        SpawnedEncounter encounter,
        EncounterAIConfig aiConfig)
    {
        var aiComponent = creature.Entity?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent == null)
            return;

        // Set home region/territory
        if (encounter.Region != null)
        {
            aiComponent.HomeRegionId = encounter.Region.Id;
            aiComponent.HomeCenter = encounter.CenterPosition;
            aiComponent.HomeTerritoryRadius = CalculateTerritoryRadius(encounter.Region);
        }

        // Configure initial state based on template
        ConfigureInitialState(aiComponent, creature, aiConfig);

        // Configure patrol route if enabled
        if (aiConfig?.GeneratePatrolRoute == true && encounter.Region != null)
        {
            GD.Print($"[Patrol] Configuring patrol route for {creature.Entity?.DisplayName}");
            ConfigurePatrolRoute(creature, encounter);
        }
    }

    /// <summary>
    /// Configures the initial AI state (sleeping, guarding, etc.).
    /// </summary>
    private void ConfigureInitialState(
        AIComponent aiComponent,
        SpawnedCreature creature,
        EncounterAIConfig aiConfig)
    {
        if (aiConfig == null)
            return;

        // Set sleeping state for ambush encounters
        if (aiConfig.InitialState?.ToLowerInvariant() == "sleeping")
        {
            aiComponent.Sleep();
            aiComponent.WakeDistance = aiConfig.WakeRadius > 0 ? aiConfig.WakeRadius : 5;
        }

        // Note: Guarding and other states are handled via the existing goal system.
        // The BoredGoal checks for threats and handles guarding behavior naturally.
    }

    /// <summary>
    /// Configures patrol route for a creature based on its spawn region.
    /// </summary>
    private void ConfigurePatrolRoute(SpawnedCreature creature, SpawnedEncounter encounter)
    {
        if (creature.Entity == null || encounter.Region == null)
        {
            GD.Print($"[Patrol] Skipped: entity={creature.Entity != null}, region={encounter.Region != null}");
            return;
        }

        var route = PatrolRouteGenerator.GenerateRegionPatrol(
            encounter.Region,
            creature.Entity.GridPosition,
            waypointCount: 4);

        if (route == null || route.Waypoints.Count < 2)
        {
            GD.Print($"[Patrol] Route generation failed: route={route != null}, waypoints={route?.Waypoints?.Count ?? 0}");
            return;
        }

        GD.Print($"[Patrol] Created route with {route.Waypoints.Count} waypoints for {creature.Entity.DisplayName}");

        // Add PatrolRouteComponent to entity
        var routeComp = new PatrolRouteComponent();
        routeComp.Name = "PatrolRouteComponent";
        routeComp.Route = route;
        creature.Entity.AddChild(routeComp);

        // Add injector if not present
        if (creature.Entity.GetNodeOrNull<PatrolRouteInjector>("PatrolRouteInjector") == null)
        {
            var injector = new PatrolRouteInjector();
            injector.Name = "PatrolRouteInjector";
            creature.Entity.AddChild(injector);
        }
    }

    /// <summary>
    /// Configures group relationships (followers protect leader, etc.).
    /// </summary>
    private void ConfigureGroupRelationships(SpawnedEncounter encounter, EncounterAIConfig aiConfig)
    {
        if (aiConfig == null || encounter.Leader?.Entity == null)
            return;

        // Configure followers to protect leader
        if (aiConfig.FollowersProtectLeader)
        {
            foreach (var creature in encounter.Creatures)
            {
                // Skip the leader
                if (creature == encounter.Leader)
                    continue;

                var aiComponent = creature.Entity?.GetNodeOrNull<AIComponent>("AIComponent");
                if (aiComponent != null)
                {
                    aiComponent.ProtectionTarget = encounter.Leader.Entity;
                    aiComponent.FollowDistance = 3; // Default follow distance
                }
            }
        }
    }

    /// <summary>
    /// Configures grouped patrol behavior for creatures with Grouped=true PatrolComponent.
    /// Creates a shared PatrolGroup and assigns it to all grouped patrollers.
    /// Uses the first patroller's config for route generation.
    /// </summary>
    private void ConfigureGroupedPatrol(SpawnedEncounter encounter)
    {
        if (encounter?.Creatures == null)
            return;

        // Find all creatures with grouped PatrolComponent
        var groupedPatrollers = new List<(SpawnedCreature creature, PatrolComponent patrol)>();
        foreach (var creature in encounter.Creatures)
        {
            if (creature.Entity == null)
                continue;

            var patrolComp = creature.Entity.GetNodeOrNull<PatrolComponent>("PatrolComponent");
            if (patrolComp != null && patrolComp.Grouped)
            {
                groupedPatrollers.Add((creature, patrolComp));
            }
        }

        if (groupedPatrollers.Count == 0)
            return;

        // Use the first patroller's config for route generation
        var config = groupedPatrollers[0].patrol;
        var centerPos = encounter.CenterPosition;

        // Generate route based on scope
        var route = PatrolRouteGenerator.GeneratePatrol(
            config.Scope,
            centerPos,
            encounter.Region,
            _mapSystem,
            config.WaypointCount,
            config.MinDistance,
            config.MaxDistance);

        if (route == null || route.Waypoints.Count < 2)
        {
            GD.Print($"[GroupedPatrol] Failed to generate {config.Scope} route for encounter");
            return;
        }

        // Create shared patrol group
        var patrolGroup = new PatrolGroup(route);

        GD.Print($"[GroupedPatrol] Created {config.Scope} group with {groupedPatrollers.Count} members, {route.Waypoints.Count} waypoints");

        // Assign patrol group to all grouped patrollers
        foreach (var (creature, patrolComp) in groupedPatrollers)
        {
            patrolComp.PatrolGroup = patrolGroup;
            patrolGroup.AddMember(creature.Entity);

            // Also add PatrolRouteComponent for compatibility with existing patrol goal
            if (creature.Entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent") == null)
            {
                var routeComp = new PatrolRouteComponent();
                routeComp.Name = "PatrolRouteComponent";
                routeComp.Route = route;
                creature.Entity.AddChild(routeComp);
            }
        }
    }

    /// <summary>
    /// Calculates territory radius based on region size.
    /// </summary>
    private int CalculateTerritoryRadius(Region region)
    {
        if (region?.Tiles == null || region.Tiles.Count == 0)
            return 10;

        // Use half the bounding box diagonal as radius
        int width = region.BoundingBox.Size.X;
        int height = region.BoundingBox.Size.Y;
        int diagonal = Mathf.RoundToInt(Mathf.Sqrt(width * width + height * height));
        return Mathf.Max(5, diagonal / 2);
    }

    /// <summary>
    /// Configures a creature to start in sleeping state.
    /// Used for ambush encounters.
    /// </summary>
    public void SetSleeping(BaseEntity entity, int wakeDistance = 5)
    {
        var aiComponent = entity?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.Sleep();
            aiComponent.WakeDistance = wakeDistance;
        }
    }

    /// <summary>
    /// Configures a creature to protect another entity.
    /// Used for bodyguard/follower behavior.
    /// </summary>
    public void SetProtectionTarget(BaseEntity follower, BaseEntity target, int followDistance = 3)
    {
        var aiComponent = follower?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.ProtectionTarget = target;
            aiComponent.FollowDistance = followDistance;
        }
    }

    /// <summary>
    /// Configures a creature's home territory.
    /// </summary>
    public void SetHomeTerritory(BaseEntity entity, GridPosition center, int radius, int regionId = -1)
    {
        var aiComponent = entity?.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.HomeCenter = center;
            aiComponent.HomeTerritoryRadius = radius;
            aiComponent.HomeRegionId = regionId;
        }
    }

    /// <summary>
    /// Batch configures multiple creatures for a pack/group encounter.
    /// Sets up the alpha as leader with all others as followers.
    /// </summary>
    public void ConfigurePackBehavior(
        BaseEntity alpha,
        List<BaseEntity> packMembers,
        GridPosition homeCenter,
        int territoryRadius = 10)
    {
        // Configure alpha
        SetHomeTerritory(alpha, homeCenter, territoryRadius);

        // Configure pack members to follow alpha
        foreach (var member in packMembers)
        {
            if (member != alpha)
            {
                SetProtectionTarget(member, alpha, followDistance: 2);
                SetHomeTerritory(member, homeCenter, territoryRadius);
            }
        }
    }
}
