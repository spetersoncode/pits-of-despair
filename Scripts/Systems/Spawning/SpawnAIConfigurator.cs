using System.Collections.Generic;
using System.Linq;
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

        // Configure patrol behavior (grouped and individual)
        ConfigurePatrols(encounter);
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
    /// Configures patrol behavior for all creatures with PatrolComponent.
    /// FreeRoaming patrollers get individual routes; LeaderPack patrollers form a pack.
    /// </summary>
    private void ConfigurePatrols(SpawnedEncounter encounter)
    {
        if (encounter?.Creatures == null)
            return;

        // Separate patrollers by mode
        var freeRoamingPatrollers = new List<(SpawnedCreature creature, PatrolComponent patrol)>();
        var leaderPackPatrollers = new List<(SpawnedCreature creature, PatrolComponent patrol)>();

        foreach (var creature in encounter.Creatures)
        {
            if (creature.Entity == null)
                continue;

            var patrolComp = creature.Entity.GetNodeOrNull<PatrolComponent>("PatrolComponent");
            if (patrolComp == null)
                continue;

            if (patrolComp.Mode == PatrolMode.LeaderPack)
                leaderPackPatrollers.Add((creature, patrolComp));
            else
                freeRoamingPatrollers.Add((creature, patrolComp));
        }

        // Configure leader pack patrollers with pack components
        if (leaderPackPatrollers.Count > 0)
        {
            ConfigureLeaderPackPatrollers(leaderPackPatrollers, encounter);
        }

        // Configure free roaming patrollers with individual routes
        foreach (var (creature, patrolComp) in freeRoamingPatrollers)
        {
            ConfigureIndividualPatroller(creature, patrolComp, encounter);
        }
    }

    /// <summary>
    /// Configures LeaderPack patrollers with PackLeaderComponent and PackFollowerComponent.
    /// Leader is selected from encounter.Leader if present, otherwise randomly.
    /// </summary>
    private void ConfigureLeaderPackPatrollers(
        List<(SpawnedCreature creature, PatrolComponent patrol)> patrollers,
        SpawnedEncounter encounter)
    {
        if (patrollers.Count == 0)
            return;

        var config = patrollers[0].patrol;
        var centerPos = encounter.CenterPosition;

        // Generate the shared patrol route
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
            GD.PushWarning($"[LeaderPack] Failed to generate {config.Scope} route");
            return;
        }

        // Select the leader: use encounter leader if in pack, otherwise random
        SpawnedCreature leaderCreature = null;

        if (encounter.Leader != null && patrollers.Any(p => p.creature == encounter.Leader))
        {
            leaderCreature = encounter.Leader;
        }
        else
        {
            // Random selection
            var randomIndex = GD.RandRange(0, patrollers.Count - 1);
            leaderCreature = patrollers[randomIndex].creature;
        }

        // Create PackLeaderComponent for the leader
        var leaderComp = new PackLeaderComponent
        {
            Route = route,
            WaitTurnsAtWaypoint = config.WaitTurns
        };
        leaderComp.Name = "PackLeaderComponent";
        leaderCreature.Entity.AddChild(leaderComp);

        // Create PackFollowerComponent for each follower
        foreach (var (creature, patrolComp) in patrollers)
        {
            if (creature == leaderCreature)
                continue;

            var followerComp = new PackFollowerComponent
            {
                Leader = leaderCreature.Entity,
                FollowDistance = patrolComp.FollowDistance
            };
            followerComp.Name = "PackFollowerComponent";
            creature.Entity.AddChild(followerComp);

            // Register follower with leader
            leaderComp.AddFollower(creature.Entity);
        }
    }

    /// <summary>
    /// Configures an individual patroller with their own route based on scope.
    /// </summary>
    private void ConfigureIndividualPatroller(
        SpawnedCreature creature,
        PatrolComponent patrolComp,
        SpawnedEncounter encounter)
    {
        // Local scope uses existing behavior (no pre-generated route needed)
        if (patrolComp.Scope == PatrolScope.Local)
            return;

        var route = PatrolRouteGenerator.GeneratePatrol(
            patrolComp.Scope,
            creature.Entity.GridPosition,
            encounter.Region,
            _mapSystem,
            patrolComp.WaypointCount,
            patrolComp.MinDistance,
            patrolComp.MaxDistance);

        if (route == null || route.Waypoints.Count < 2)
        {
            GD.PushWarning($"[IndividualPatrol] Failed to generate {patrolComp.Scope} route for {creature.Entity.DisplayName}");
            return;
        }

        AddPatrolRouteComponent(creature.Entity, route);
    }

    /// <summary>
    /// Adds a PatrolRouteComponent to an entity if not already present.
    /// </summary>
    private void AddPatrolRouteComponent(BaseEntity entity, AI.Patrol.PatrolRoute route)
    {
        if (entity.GetNodeOrNull<PatrolRouteComponent>("PatrolRouteComponent") != null)
            return;

        var routeComp = new PatrolRouteComponent();
        routeComp.Name = "PatrolRouteComponent";
        routeComp.Route = route;
        entity.AddChild(routeComp);
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
