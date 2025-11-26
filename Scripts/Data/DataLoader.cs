using Godot;
using PitsOfDespair.Data.Loaders;

namespace PitsOfDespair.Data;

/// <summary>
/// Singleton autoload that loads and provides access to game data from YAML files.
/// Acts as a facade coordinating individual typed loaders.
/// </summary>
public partial class DataLoader : Node
{
    // Typed loader properties
    public CreatureLoader Creatures { get; private set; }
    public ItemLoader Items { get; private set; }
    public SkillLoader Skills { get; private set; }
    public FloorConfigLoader FloorConfigs { get; private set; }
    public PrefabDataLoader Prefabs { get; private set; }
    public SpawningDataLoader Spawning { get; private set; }
    public DecorationLoader Decorations { get; private set; }

    public override void _Ready()
    {
        // Initialize loaders
        Creatures = new CreatureLoader();
        Items = new ItemLoader();
        Skills = new SkillLoader();
        FloorConfigs = new FloorConfigLoader();
        Prefabs = new PrefabDataLoader();
        Spawning = new SpawningDataLoader();
        Decorations = new DecorationLoader();

        // Load all data (order matters for validation)
        Creatures.Load();
        Items.Load();
        Skills.Load();
        FloorConfigs.Load();
        Prefabs.Load();
        Spawning.Load();
        Decorations.Load();

        // Cross-reference validation (creatures must be loaded first)
        Spawning.ValidateFactionThemes(Creatures);
    }
}
