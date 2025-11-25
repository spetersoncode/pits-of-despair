# Spawning System

The spawning system populates dungeon floors through a region-based, encounter-driven pipeline. Power budgets distribute across regions by size and danger level, faction themes create territorial coherence, and encounter templates define creature compositions with tactical behaviors.

## Core Architecture

**Pipeline Orchestration**: Spawning executes as an 11-phase pipeline. Each phase has clear inputs, outputs, and dependencies. Phases execute sequentially—theme assignment before budget allocation, encounters before items, AI configuration last. This separation enables independent testing and modification of individual phases.

**Region-Based Distribution**: Rather than per-room or random placement, spawning operates on detected regions from dungeon metadata. Regions receive theme assignments and power budgets proportional to their characteristics. This creates natural variation—large distant regions feel dangerous, small entry regions feel safe.

**Encounter-Driven Composition**: Instead of spawning individual creatures, the system spawns encounters—structured groups with defined roles, positions, and behaviors. Encounters transform generic creature lists into tactical situations: ambushes near passages, lairs in large rooms, patrols through corridors.

**Theme Clustering**: Adjacent regions can share faction themes (40% probability), creating territorial zones. Walking through a dungeon, players encounter coherent goblin territory or undead crypts rather than random creature soup. Themes also filter creature selection to thematically appropriate choices.

## Spawning Pipeline

**Phase 1 - Load Configuration**: Retrieve floor spawn config for current depth. Config defines budgets, theme weights, encounter weights, threat limits.

**Phase 2 - Assign Themes**: Distribute faction themes across regions using weighted selection. Adjacent regions may cluster. Prefab spawn hints can override theme assignment.

**Phase 3 - Allocate Budgets**: Distribute floor power budget across regions. Weight factors: region size, distance from entrance, isolation (dead-ends more dangerous), region tags. Each region receives proportional share.

**Phase 4 - Process Prefabs**: Handle spawn hints from prefab rooms. These can specify exact creatures, encounter templates, items, or theme overrides. Prefab spawns deduct from region budgets.

**Phase 5 - Spawn Uniques**: Place unique/boss creatures if configured for this floor. Uniques tracked per-run to prevent duplicates. Prefer large regions far from entrance.

**Phase 6 - Spawn Encounters**: Main spawning loop. For each region with remaining budget: select encounter template fitting budget, spawn creatures filling template slots, track positions, repeat until budget exhausted.

**Phase 7 - Place Treasures**: Distribute valuable items with guardians. Higher-value items placed in more dangerous regions. Risk equals reward principle.

**Phase 8 - Place Items**: Scatter remaining items across regions. Distribution weighted by region danger level.

**Phase 9 - Place Gold**: Distribute gold piles across floor. Pile sizes vary randomly within configured range.

**Phase 10 - Place Stairs**: Position level exit (or throne on final floor) in region farthest from entrance. Validates player can path to exit.

**Phase 11 - Configure AI**: Set up AI behaviors for all spawned encounters. Configure territory bounds, initial states (sleeping for ambushes), leader-follower relationships.

## Power Budget System

**Budget Calculation**: Floor configs specify budgets as dice notation (e.g., `3d6+8`). Rolled once per floor, distributed across regions. Creatures have threat ratings consuming budget on spawn.

**Threat Rating**: Unbounded integer measuring creature danger. Guidelines: 1-5 trivial (rats, bats), 6-15 standard (goblins, skeletons), 16-30 dangerous (ogres, wraiths), 31-50 elite, 51+ boss. XP awards derive from threat.

**Budget Allocation**: Regions receive budget proportional to weighted area. Weight factors:
- **Size**: Larger regions get more budget
- **Distance**: Farther from entrance increases weight
- **Isolation**: Single-connection regions (dead-ends) get danger bonus
- **Tags**: Special regions (treasure rooms) can have modifiers

## Encounter Templates

Templates define structured creature groups with roles, positions, and behaviors. Seven template types cover common tactical situations:

**Lair**: Leader at region center, followers distributed throughout. Good for large rooms and dead-ends. Creates boss-fight dynamics with minions.

**Patrol**: Small group (2-4) spawning at region edge. Configured with patrol behavior through region or into adjacent areas. Creates mobile threats.

**Ambush**: Creatures placed near chokepoints, initially sleeping. Wake on player proximity. Creates surprise encounters in passages.

**Guard Post**: 1-2 creatures at strategic position, configured to hold ground. May call for reinforcements. Good for chokepoints and entrances.

**Treasure Guard**: Valuable item placed first, guardian scaled to match value. Risk-reward principle—better loot has tougher guardians.

**Infestation**: Many weak creatures spread throughout region. No leader, chaotic behavior. Good for caves and large open areas.

**Pack**: Alpha creature with pack members configured to follow. Alpha uses call-for-help. Good for beast themes.

**Template Structure**: Each template defines slots with role requirements (leader, follower, guard), preferred archetypes, count ranges, placement preferences, and threat multipliers. Slots filled by selecting creatures matching requirements from region's theme.

## Faction Themes

Themes group thematically related creatures for coherent encounters:

**Theme Definition**: Name, creature list, optional floor range (min/max depth), display color. Creatures belong to theme by ID reference—no role tagging required.

**Current Themes**: Vermin (rats, spiders, insects), Goblinoid (goblins, hobgoblins), Undead (skeletons, zombies, wraiths), Beast (wolves, bears, natural creatures).

**Theme Selection**: Floor configs specify theme weights. Selection uses weighted random—higher weight means more likely. Floor range filtering ensures themes appear at appropriate depths.

**Territory Clustering**: When assigning themes, adjacent regions have 40% chance to receive same theme as neighbor. Creates faction territories rather than random distribution.

## Archetype System

Archetypes categorize creatures by combat role, inferred from stats rather than manual tagging.

**Eight Archetypes**:
- **Minion**: Low threat (≤5)—fodder creatures
- **Elite**: High threat (≥16)—dangerous individuals
- **Tank**: High END relative to other stats—damage absorbers
- **Warrior**: High STR, balanced—front-line fighters
- **Assassin**: High AGI + STR, low END—glass cannons
- **Ranged**: Has ranged attack or high AGI with low STR—ranged damage
- **Support**: Healing effects or high WIL—buffers/healers
- **Brute**: High END + STR, low AGI, slow speed—slow heavy hitters

**Inference**: Archetypes derived from creature data at runtime. Stats distribution, attack types, and equipment determine applicable archetypes. Creatures can match multiple archetypes.

**Slot Matching**: Encounter template slots specify preferred archetypes. Creature selection filters theme's creatures by archetype match, then by threat fitting remaining budget. This enables role-based composition without manual creature tagging.

## Special Spawning

**Unique Monsters**: Floor configs can specify unique creatures guaranteed to spawn. Tracked per-run—once spawned, won't appear again. Placed in lair-style configuration in appropriate region.

**Out-of-Depth**: Configurable chance per floor to spawn creature from deeper floor's pools. Creates memorable danger moments. Controlled by `outOfDepthChance` (0.0-1.0) and `outOfDepthFloors` (how many floors ahead).

**Prefab Integration**: Prefab rooms can include spawn hints specifying:
- Exact creatures at positions
- Encounter template to use
- Items to place
- Theme override for region
Prefab spawns execute before regular spawning, deducting from region budget.

## Design Patterns

**Pipeline Pattern**: Clear phase separation with defined interfaces. Each phase can be tested, modified, or replaced independently.

**Budget System**: Global resource distributed locally. Prevents over/under-spawning. Enables natural density variation.

**Composition**: Encounters compose creatures by role. Themes compose creature pools. Configs compose themes and templates. No deep inheritance hierarchies.

**Data-Driven**: All spawning parameters in YAML. New floors, themes, encounters added without code changes. Designers tune difficulty through data.

**Inference Over Tagging**: Archetypes computed from existing data. No manual role assignment. Creatures automatically fit appropriate slots.

## Adding New Encounter Templates

1. Define template in YAML with type, budget range, region requirements
2. Specify slots with roles, archetype preferences, count ranges, placement
3. Configure AI settings (initial state, territory binding, wake conditions)
4. Reference template in floor spawn configs with appropriate weight
5. Design considerations: What tactical situation does this create? What region types suit it? How do creatures interact?

## Adding New Faction Themes

1. Define theme in YAML with name, creature list, floor range
2. Ensure theme has archetype variety (tanks, ranged, support) for encounter slot filling
3. Reference theme in floor spawn configs with appropriate weight
4. Design considerations: What creatures belong together thematically? What depths feel appropriate? Does the creature list cover needed archetypes?

## Adding New Floor Spawn Configs

1. Define config with budgets (power, item, gold) as dice notation
2. Specify theme weights for faction distribution
3. Specify encounter weights for tactical variety
4. Set threat limits (min/max) for creature filtering
5. Configure out-of-depth settings for danger moments
6. Design considerations: How difficult should this floor feel? What themes dominate? What encounter types create the right pacing?

---

*See [dungeon-generator.md](dungeon-generator.md) for region generation, [yaml.md](yaml.md) for data formats, [entities.md](entities.md) for creature creation, [ai.md](ai.md) for AI configuration.*
