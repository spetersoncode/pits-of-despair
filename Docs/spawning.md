# Spawning System

The spawning system populates dungeon floors through a region-based, encounter-driven pipeline. Density-based distribution spreads content across walkable tiles, faction themes create territorial coherence, and encounter templates define creature compositions with tactical behaviors.

## Core Architecture

**Two-Config System**: Spawning configuration splits between Pipeline configs (layout-dependent) and Floor configs (difficulty/content). Pipeline configs in `Data/Pipelines/*.yaml` define spawn densities and encounter types suited to specific layouts. Floor configs in `Data/Floors/*.yaml` define threat ranges, themes, and content for depth progression. SpawnContext merges both at runtime.

**Pipeline Orchestration**: Spawning executes as a multi-phase pipeline. Each phase has clear inputs, outputs, and dependencies. Phases execute sequentially—theme assignment before encounters, encounters before items, AI configuration last. This separation enables independent testing and modification of individual phases.

**Region-Based Distribution**: Rather than per-room or random placement, spawning operates on detected regions from dungeon metadata. Regions receive theme assignments and encounters proportional to their characteristics. This creates natural variation—large distant regions feel dangerous, small entry regions feel safe.

**Density-Based Spawning**: Items and gold use density values (percentage of walkable tiles) rather than fixed budgets. This scales naturally with dungeon size—larger dungeons get proportionally more content without manual tuning.

**Encounter-Driven Composition**: Instead of spawning individual creatures, the system spawns encounters—structured groups with defined roles, positions, and behaviors. Encounters transform generic creature lists into tactical situations: ambushes near passages, lairs in large rooms, patrols through corridors.

**Theme Clustering**: Adjacent regions can share faction themes (40% probability), creating territorial zones. Walking through a dungeon, players encounter coherent goblin territory or undead crypts rather than random creature soup. Themes also filter creature selection to thematically appropriate choices.

## Spawning Pipeline

**Phase 1 - Load Configuration**: Merge Pipeline config (from MapSystem) and Floor config (by depth) into SpawnContext. Pipeline provides layout-suited spawn settings; Floor provides difficulty/content settings.

**Phase 2 - Assign Themes**: Distribute faction themes across regions using weighted selection from Floor config. Adjacent regions may cluster (40% chance). Prefab spawn hints can override theme assignment.

**Phase 3 - Process Prefabs**: Handle spawn hints from prefab rooms. These can specify exact creatures, encounter templates, items, or theme overrides.

**Phase 4 - Spawn Uniques**: Place unique/boss creatures if configured for this floor. Uniques tracked per-run to prevent duplicates. Prefer large regions far from entrance.

**Phase 5 - Spawn Encounters**: Main spawning loop. For each region: check encounter chance, select encounter template from Pipeline's encounter weights, spawn creatures within Floor's threat range, track positions.

**Phase 6 - Out-of-Depth**: Attempt to spawn creature from deeper floor's pools based on configured chance. Creates memorable danger moments.

**Phase 7 - Place Treasures**: Distribute valuable items with guardians. Higher-value items placed in more dangerous regions.

**Phase 8 - Place Items**: Scatter items across floor based on itemDensity (% of walkable tiles). Item values filtered by Floor's min/max item value.

**Phase 9 - Place Gold**: Distribute gold piles based on goldDensity (% of walkable tiles). Pile amounts scale with floor depth.

**Phase 10 - Place Stairs**: Position level exit (or throne on final floor) in region farthest from entrance.

**Phase 11 - Configure AI**: Set up AI behaviors for spawned encounters. Configure territory bounds, initial states, leader-follower relationships.

## Density-Based Distribution

**Item Density**: Pipeline configs specify `itemDensity` as percentage of walkable tiles (e.g., 0.006 = 0.6%). System calculates target count from total walkable tiles and distributes items across floor.

**Gold Density**: Similarly, `goldDensity` determines gold pile count. Pile amounts use Floor config's `baseGoldPerPile` scaled by `goldFloorScale` per depth.

**Encounter Chance**: Each region has `encounterChance` probability of receiving an encounter. `maxEncounterRatio` caps total encounters as fraction of regions. `minEncounterSpacing` prevents clustering.

**Threat Rating**: Unbounded integer measuring creature danger. Guidelines: 1-5 trivial (rats, bats), 6-15 standard (goblins, skeletons), 16-30 dangerous (ogres, wraiths), 31-50 elite, 51+ boss. Floor configs specify `minThreat`/`maxThreat` to filter appropriate creatures.

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

## Adding New Pipeline Configs

Pipeline configs define layout-dependent spawn settings in `Data/Pipelines/*.yaml`:

1. Define generation passes (BSP, cellular automata, etc.) with parameters
2. Configure `spawnSettings` block with density values suited to layout type
3. Specify `encounterWeights` for encounter types that work with the layout
4. Set spacing/exclusion parameters appropriate for typical region sizes
5. Design considerations: What encounter types suit this layout? How dense should content be given typical open space?

## Adding New Floor Configs

Floor configs define difficulty/content settings in `Data/Floors/*.yaml`:

1. Set `minFloor`/`maxFloor` depth range for this config
2. Reference `pipeline` to use (or list with weights for random selection)
3. Set `minThreat`/`maxThreat` for creature filtering
4. Specify `themeWeights` for faction distribution
5. Configure gold scaling (`baseGoldPerPile`, `goldFloorScale`)
6. Set out-of-depth settings (`creatureOutOfDepthChance`, `outOfDepthFloors`)
7. Add `uniqueCreatures` for guaranteed boss spawns
8. Design considerations: How difficult should this depth feel? What factions dominate? What's the risk/reward balance?

---

*See [dungeon-generator.md](dungeon-generator.md) for generation pipeline, [yaml.md](yaml.md) for data formats, [entities.md](entities.md) for creature creation, [ai.md](ai.md) for AI configuration.*
