using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages persistent tile hazards like poison clouds, fire patches, etc.
/// Creates visual effects for hazards and processes damage each turn.
/// </summary>
public partial class TileHazardManager : Node
{
    [Signal]
    public delegate void HazardDamageDealtEventHandler(BaseEntity entity, int damage, string hazardType);

    private Dictionary<GridPosition, List<TileHazard>> _hazards = new();
    private EntityManager _entityManager;
    private TurnManager _turnManager;
    private TextRenderer _renderer;
    private Dictionary<string, Shader> _loadedShaders = new();

    private const string PoisonCloudShader = "res://Resources/Shaders/Hazards/poison_cloud.gdshader";
    private const int TileSize = 18;

    public override void _Ready()
    {
        // Pre-load hazard shaders
        LoadShader(PoisonCloudShader);
    }

    /// <summary>
    /// Sets dependencies for the hazard manager.
    /// </summary>
    public void SetDependencies(EntityManager entityManager, TurnManager turnManager, TextRenderer renderer)
    {
        _entityManager = entityManager;
        _renderer = renderer;

        // Disconnect from old turn manager if exists
        if (_turnManager != null)
        {
            _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnEnded, Callable.From(OnTurnEnded));
        }

        _turnManager = turnManager;

        // Connect to turn manager to process hazards each turn
        if (_turnManager != null)
        {
            _turnManager.Connect(TurnManager.SignalName.PlayerTurnEnded, Callable.From(OnTurnEnded));
        }
    }

    /// <summary>
    /// Creates a hazard at the specified position.
    /// </summary>
    public TileHazard CreateHazard(string hazardType, GridPosition position, int duration, int damagePerTurn, DamageType damageType, Color color)
    {
        var hazard = new TileHazard(hazardType, position, duration, damagePerTurn, damageType, color);

        if (!_hazards.ContainsKey(position))
        {
            _hazards[position] = new List<TileHazard>();
        }

        // Check if same hazard type already exists at position - refresh duration instead
        var existing = _hazards[position].FirstOrDefault(h => h.HazardType == hazardType);
        if (existing != null)
        {
            existing.RemainingTurns = System.Math.Max(existing.RemainingTurns, duration);
            return existing;
        }

        _hazards[position].Add(hazard);
        CreateHazardVisual(hazard);

        return hazard;
    }

    /// <summary>
    /// Creates hazards in an area around a center position.
    /// </summary>
    public List<TileHazard> CreateHazardArea(string hazardType, GridPosition center, int radius, int duration, int damagePerTurn, DamageType damageType, Color color)
    {
        var hazards = new List<TileHazard>();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                var pos = new GridPosition(center.X + dx, center.Y + dy);

                // Use Euclidean distance for circular area
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                {
                    var hazard = CreateHazard(hazardType, pos, duration, damagePerTurn, damageType, color);
                    hazards.Add(hazard);
                }
            }
        }

        return hazards;
    }

    /// <summary>
    /// Gets all hazards at a specific position.
    /// </summary>
    public IReadOnlyList<TileHazard> GetHazardsAt(GridPosition position)
    {
        if (_hazards.TryGetValue(position, out var list))
        {
            return list.AsReadOnly();
        }
        return System.Array.Empty<TileHazard>();
    }

    /// <summary>
    /// Checks if there are any hazards at the given position.
    /// </summary>
    public bool HasHazardAt(GridPosition position)
    {
        return _hazards.ContainsKey(position) && _hazards[position].Count > 0;
    }

    /// <summary>
    /// Called at the end of each turn to process hazard effects and durations.
    /// </summary>
    private void OnTurnEnded()
    {
        var expiredHazards = new List<(GridPosition pos, TileHazard hazard)>();

        foreach (var kvp in _hazards)
        {
            var position = kvp.Key;
            var hazardsAtPos = kvp.Value;

            // Get entity at this position
            var entity = _entityManager?.GetEntityAtPosition(position);

            foreach (var hazard in hazardsAtPos)
            {
                // Apply damage to entity if present
                if (entity != null && hazard.DamagePerTurn > 0)
                {
                    var healthComponent = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
                    if (healthComponent != null)
                    {
                        healthComponent.TakeDamage(hazard.DamagePerTurn, hazard.DamageType);
                        EmitSignal(SignalName.HazardDamageDealt, entity, hazard.DamagePerTurn, hazard.HazardType);
                    }
                }

                // Decrement duration
                hazard.RemainingTurns--;

                if (hazard.RemainingTurns <= 0)
                {
                    expiredHazards.Add((position, hazard));
                }
            }
        }

        // Remove expired hazards
        foreach (var (pos, hazard) in expiredHazards)
        {
            RemoveHazard(pos, hazard);
        }
    }

    /// <summary>
    /// Removes a specific hazard.
    /// </summary>
    private void RemoveHazard(GridPosition position, TileHazard hazard)
    {
        // Clean up visual
        if (hazard.VisualNode != null)
        {
            hazard.VisualNode.QueueFree();
            hazard.VisualNode = null;
        }
        hazard.Material = null;

        // Remove from list
        if (_hazards.TryGetValue(position, out var list))
        {
            list.Remove(hazard);
            if (list.Count == 0)
            {
                _hazards.Remove(position);
            }
        }
    }

    /// <summary>
    /// Clears all hazards (e.g., when changing levels).
    /// </summary>
    public void ClearAllHazards()
    {
        foreach (var kvp in _hazards)
        {
            foreach (var hazard in kvp.Value)
            {
                if (hazard.VisualNode != null)
                {
                    hazard.VisualNode.QueueFree();
                    hazard.VisualNode = null;
                }
                hazard.Material = null;
            }
        }
        _hazards.Clear();
    }

    #region Visual Effects

    private Shader? LoadShader(string shaderPath)
    {
        if (_loadedShaders.TryGetValue(shaderPath, out var cached))
        {
            return cached;
        }

        var shader = GD.Load<Shader>(shaderPath);
        if (shader == null)
        {
            GD.PrintErr($"TileHazardManager: Failed to load shader: {shaderPath}");
            return null;
        }

        _loadedShaders[shaderPath] = shader;
        return shader;
    }

    private void CreateHazardVisual(TileHazard hazard)
    {
        if (_renderer == null) return;

        var shaderPath = GetShaderPath(hazard.HazardType);
        var shader = LoadShader(shaderPath);
        if (shader == null) return;

        var material = new ShaderMaterial();
        material.Shader = shader;

        // Set shader uniforms
        material.SetShaderParameter("hazard_color", hazard.Color);
        material.SetShaderParameter("time", 0.0f);

        var colorRect = new ColorRect();
        colorRect.Material = material;
        colorRect.Size = new Vector2(TileSize, TileSize);
        colorRect.ZIndex = 50; // Below entities but above floor

        hazard.VisualNode = colorRect;
        hazard.Material = material;

        _renderer.AddChild(colorRect);
        UpdateHazardPosition(hazard);
    }

    private string GetShaderPath(string hazardType)
    {
        return hazardType switch
        {
            "poison_cloud" => PoisonCloudShader,
            _ => PoisonCloudShader // Default fallback
        };
    }

    private void UpdateHazardPosition(TileHazard hazard)
    {
        if (hazard.VisualNode == null || _renderer == null) return;

        var offset = _renderer.GetRenderOffset();
        var tilePos = _renderer.GridToOverlayPosition(hazard.Position);

        hazard.VisualNode.Position = new Vector2(
            offset.X + tilePos.X,
            offset.Y + tilePos.Y
        );
    }

    /// <summary>
    /// Updates all hazard positions (call when camera moves).
    /// </summary>
    public void UpdateAllHazardPositions()
    {
        foreach (var kvp in _hazards)
        {
            foreach (var hazard in kvp.Value)
            {
                UpdateHazardPosition(hazard);
            }
        }
    }

    public override void _Process(double delta)
    {
        // Update shader time for animations
        foreach (var kvp in _hazards)
        {
            foreach (var hazard in kvp.Value)
            {
                if (hazard.Material != null)
                {
                    var currentTime = (float)hazard.Material.GetShaderParameter("time");
                    hazard.Material.SetShaderParameter("time", currentTime + (float)delta);
                }
            }
        }

        // Update positions in case camera moved
        UpdateAllHazardPositions();
    }

    #endregion

    public override void _ExitTree()
    {
        if (_turnManager != null)
        {
            _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnEnded, Callable.From(OnTurnEnded));
        }
        ClearAllHazards();
    }
}
