# Spawning System Redesign - Brainstorming & Exploration

**Status:** Exploratory brainstorming - foundation for future implementation
**Date:** 2025-11-24
**Context:** With robust AI, effects, conditions, and progression systems now in place, the spawning system has opportunity for deeper integration. This document explores principles, patterns, and possibilities for a more sophisticated spawn architecture.

---

## Current State Assessment

### What Works Well

**Budget-Based Spawning**: Global floor budgets provide natural variation between runs. Dice notation creates meaningful density differences. Works with any map topology.

**Weighted Pool Selection**: Two-tier selection (pool → entry) enables controlled rarity. Designers can tune without code changes. Same creature can appear across tiers at different frequencies.

**Strategy Pattern**: Clean separation between spawn types (single, band, unique) and placement (random, center, surrounding). Composable and extensible.

**Band System**: Leader-follower packs create tactical encounters. Recent wild dog pack work demonstrates AI integration potential—leaders can have distinct behaviors (Patrol) while followers protect and follow.

### Current Limitations

**No Encounter Budgeting**: Budget counts entities, not threat. A goblin and a goblin ruffian cost the same despite power difference. No per-encounter difficulty ceiling.

**Static Spawn Configuration**: Floor tables are fixed lists. No awareness of player state, dungeon features, or game progression within a run.

**Limited Spatial Intelligence**: Placement strategies don't consider room function, chokepoints, patrol routes, or strategic positioning relative to map features.

**No Cross-System Integration**: Spawning doesn't leverage dungeon generation metadata, room types, or corridor layouts. Effects and conditions can't influence spawn decisions.

**Homogeneous Encounters**: Each spawn is independent. No thematic clustering, faction territory, or ecosystem relationships between spawn groups.

---

## Design Principles

### 1. Fair Play Through Transparency

**Principle**: Players should be able to assess threat before engagement. Surprise difficulty spikes feel unfair. Challenge should come from tactical execution, not hidden information.

**Implications**:
- Powerful creatures should be visually distinct (different glyphs, colors)
- Dangerous areas should have environmental cues before entry
- Band formations should be visible before triggering aggro
- No "gotcha" spawns in blind corners with no escape route

### 2. Even Distribution with Intentional Clustering

**Principle**: Encounters should be distributed across the map, not clumped in one area. But thematic clustering (faction territory, nest areas) creates memorable exploration.

**Implications**:
- Minimum spacing between spawn groups prevents overwhelming clusters
- Room-based distribution ensures all areas have content
- Themed zones intentionally group related creatures
- Empty rooms are valid—not every space needs monsters

### 3. Threat-Appropriate Challenge Curve

**Principle**: Early encounters should be survivable for new players. Deeper floors should test mastery. Within a floor, challenge should escalate toward objective (stairs/boss).

**Implications**:
- Spawn budget should weight threat level, not just count
- Distance from stairs affects spawn power
- Player level/equipment could influence spawn tables
- "Out of depth" spawns should be rare and escapable

### 4. Emergent Behavior Through System Integration

**Principle**: Rich gameplay emerges when systems interact. Spawning should leverage AI, effects, conditions, and dungeon features rather than exist in isolation.

**Implications**:
- Room types influence spawn selection
- Creatures spawn with contextual equipment/abilities
- AI components assigned based on spawn location
- Environmental hazards combined with creature placement

### 5. Replayability Through Meaningful Variation

**Principle**: Each run should feel different. Variation should be meaningful (different tactical challenges), not just cosmetic (same goblin, different position).

**Implications**:
- Multiple valid spawn configurations per floor template
- Different faction combinations change encounter dynamics
- Band compositions vary even for same band type
- Item availability influences creature loadouts

---

## Threat Budget System

### Concept: Weighted Entity Costs

Replace simple entity counting with threat-weighted budgets. Each creature has a "threat cost" based on its power level.

**Example Threat Costs**:
```
rat: 1
goblin: 2
goblin_scout: 2 (speed + yell makes it 2 despite low stats)
skeleton: 3 (damage resistances)
zombie: 3 (slow but tanky)
goblin_ruffian: 4 (leadership, higher stats)
elder_rat: 2 (slightly tougher rat)
wild_dog: 2
```

**Band Threat Calculation**: Sum of all members plus synergy bonus. A goblin pack (ruffian + 2-4 goblins) costs 4 + (2×2 to 4×2) = 8-12 threat, plus synergy bonus of +2 for coordinated behavior = 10-14 total.

**Benefits**:
- Budget naturally limits dangerous spawns
- Allows more weak creatures OR fewer strong ones
- "Out of depth" creatures cost proportionally more
- Balancing becomes math rather than intuition

### Per-Encounter Limits

**Problem**: Even with threat budgets, a single encounter could consume entire budget.

**Solution**: Per-encounter caps alongside floor budget.
- Floor budget: 100-150 threat points
- Per-encounter cap: 15-20 threat points
- Band cap: 12-15 threat points

**Example Floor 1**:
- Budget: 100 threat
- Cap: 15 per encounter
- Result: ~7-10 encounters, none overwhelming

### Dynamic Budget Adjustment

**Floor-Based Scaling**: Budget increases with depth.
```
floor_budget = base_budget + (floor × scaling_factor)
floor_cap = base_cap + (floor × cap_scaling)
```

**Player Power Adjustment** (experimental): Scale budget based on player level or equipment value.
```
adjusted_budget = base_budget × (1 + player_power_factor)
```

This keeps challenge appropriate even if player is over/under-leveled.

---

## Spatial Intelligence

### Room-Type Aware Spawning

**Concept**: Dungeon generator could tag rooms with types. Spawn system selects creatures appropriate to room type.

**Room Types**:
- **Storage**: Rats, vermin, scavengers
- **Barracks**: Humanoid groups, organized bands
- **Temple**: Undead, cultists, thematic creatures
- **Laboratory**: Magical creatures, experiments gone wrong
- **Treasure**: Guardians, traps, high-value encounters
- **Passage**: Patrols, ambush positions

**Implementation**: Room metadata in dungeon generation → SpawnManager queries room type → filters spawn pools by room affinity.

### Strategic Positioning

**Concept**: Place creatures in tactically interesting positions, not just random tiles.

**Position Types**:
- **Chokepoint Guards**: Single powerful creature at doorway
- **Room Center**: Surrounded enemies, tactical positioning matters
- **Corner Ambush**: Creatures hidden from entry sightlines
- **Patrol Routes**: Mobile creatures along corridors
- **Objective Guards**: Stronger spawns near stairs/treasures

**Implementation**: Placement strategies query map topology. Center placement already exists; add chokepoint detection, corner identification, and route generation.

### Minimum Engagement Distance

**Concept**: Ensure players have space to assess and retreat from dangerous encounters.

**Rules**:
- Spawn no closer than N tiles from any doorway
- Bands spawn with formation visible from room entrance
- Powerful creatures spawn with "safe" tiles between them and entry
- No spawns in 1-tile-wide dead ends

### Zone-Based Distribution

**Concept**: Divide floor into zones, ensure each has appropriate content.

**Zone Types**:
- **Near Spawn**: Light encounters, tutorial-friendly
- **Exploration Zone**: Standard encounter density
- **Deep Zone**: Challenging encounters, better loot
- **Objective Zone**: Boss/stairs area, climactic encounters

**Distribution Rules**:
- Near spawn zone: 20% of budget, low threat cap
- Exploration zones: 50% of budget, standard cap
- Deep zones: 20% of budget, elevated cap
- Objective zone: 10% of budget, highest threat allowed

---

## Faction Ecosystems

### Creature Relationships

**Concept**: Creatures have relationships affecting spawn logic and emergent behavior.

**Relationship Types**:
- **Allied**: Same faction, will reinforce each other
- **Hostile**: Will fight each other if spawned nearby
- **Neutral**: Ignore each other
- **Predator/Prey**: One hunts the other

**Spawn Implications**:
- Allied creatures cluster together (goblin territory)
- Hostile factions spawn in separate areas (orcs vs undead)
- Predator/prey creates environmental storytelling (dead prey creatures near predator)
- Neutral creatures appear anywhere

### Faction Territory

**Concept**: Parts of the dungeon belong to specific factions.

**Implementation**:
- Mark rooms/zones with faction ownership
- Faction-owned areas spawn only that faction
- Borders between territories create conflict zones
- Some areas are "neutral ground" (anything spawns)

**Example Floor Layout**:
```
[Goblin Territory] --- [Neutral Zone] --- [Undead Territory]
    Goblins              Mixed              Skeletons
    Goblin bands         Any creature       Zombies
    Goblin scouts        Conflict!          Undead bands
```

**Benefits**:
- Creates exploration narrative (entering goblin territory)
- Makes faction relationships tangible
- Enables faction-specific strategies

### Ecosystem Balance

**Concept**: Creature populations should feel like living systems, not random placement.

**Ecosystem Rules**:
- Predators are rarer than prey
- Scavengers appear near corpse locations
- Pack animals cluster; solitary creatures spread out
- Intelligent creatures near resources; beasts near lairs

**Implementation**:
- Tag creatures with ecosystem roles (apex, predator, prey, scavenger, pack, solitary)
- Apply multipliers to spawn weights based on existing spawns
- After spawning predator, reduce prey weight in area
- Scavengers weighted toward already-populated areas

---

## AI Integration Opportunities

### Contextual AI Assignment

**Concept**: AI components assigned based on spawn context, not just creature definition.

**Current State**: Creatures have fixed AI in YAML. Wild dog pack shows dynamic assignment (leader gets Patrol).

**Opportunities**:
- Guards near objectives get "hold position" behavior
- Corridor spawns get patrol routes along passage
- Treasure room creatures get "protect area" behavior
- Scouts get "report to leader" behavior if leader exists

**Implementation**: Band and spawn strategies can add AI components based on:
- Spawn location (room type, distance from objective)
- Other spawns nearby (leader/follower relationships)
- Floor depth (deeper = more aggressive behaviors)

### Pack Dynamics

**Current**: BandSpawnStrategy sets ProtectionTarget on followers.

**Extensions**:
- **Alpha Challenge**: If leader dies, strongest follower becomes new leader
- **Reinforcement Calls**: YellForHelp brings nearby packs, not just individuals
- **Pack Hunting**: Groups coordinate movement to surround prey
- **Flee Together**: Pack flees as unit when alpha retreats

**Implementation**: AI components could query spawn context. FleeGoal already prioritizes fleeing toward allies. Extend with pack-awareness.

### Patrol Route Generation

**Concept**: Generate meaningful patrol routes at spawn time rather than random wandering.

**Route Types**:
- **Loop**: Circular path through connected rooms
- **Corridor**: Back-and-forth along passage
- **Guard Post**: Stand at position, occasionally check nearby
- **Perimeter**: Circle room edges

**Implementation**:
- PatrolComponent receives route at spawn
- Route generated from room/corridor metadata
- Multiple creatures can share routes (staggered timing)

### Environmental AI Modifiers

**Concept**: Spawn location affects creature behavior.

**Examples**:
- Near fire/light: Creatures avoid area or attack light sources
- In darkness: Stealth-capable creatures get ambush behavior
- Near treasure: Creatures prioritize protecting specific tile
- Near exit: Creatures block escape routes

---

## Effect & Condition Integration

### Equipment Variety

**Concept**: Creatures spawn with equipment variety based on context.

**Current**: Fixed equipment per creature type in YAML.

**Opportunities**:
- Guards spawn with shields more often
- Deep floor creatures have better weapon tiers
- Treasure room guardians have themed equipment
- Random chance for uncommon equipment variants

**Implementation**:
- Equipment pools in creature definition
- Spawn context influences pool selection
- EntityFactory.CreateCreature accepts optional equipment overrides

### Spawn-Applied Conditions

**Concept**: Some spawns apply conditions representing environmental or narrative state.

**Examples**:
- Sleeping creatures (must be woken, can't be seen until close)
- Enraged creatures (from prior combat, higher damage)
- Wounded creatures (environmental storytelling)
- Buffed guardians (protecting important areas)

**Implementation**:
- SpawnEntryData includes optional conditions list
- Applied after entity creation, before registration
- Duration = Permanent for spawn-applied buffs

### Environmental Hazard Pairing

**Concept**: Pair creature spawns with environmental hazards for tactical depth.

**Examples**:
- Fire-resistant creatures spawn near flames
- Flying creatures spawn over pits (player can't follow)
- Ranged creatures behind caltrops/webs
- Burrowing creatures near walls (can emerge anywhere)

**Implementation**:
- Hazard placement as spawn strategy step
- Creature capabilities determine valid hazard pairings
- Placement validates creature can navigate hazard safely

---

## Dungeon Feature Integration

### Objective-Centric Spawning

**Concept**: Spawn difficulty radiates from floor objectives.

**Stairs Protection**:
- Strongest single creature or band guards stairs
- Difficulty increases approaching stairs area
- Clear path exists but requires defeating guardians

**Treasure Distribution**:
- Better items spawn in more dangerous areas
- Risk/reward visible: see powerful guardian, infer good loot
- Some treasures unguarded (variety, exploration reward)

### Room Feature Utilization

**Concept**: Spawn creatures that use room features effectively.

**Examples**:
- Ranged creatures in rooms with cover
- Ambush predators in rooms with corners/pillars
- Pack creatures in open rooms (formation space)
- Single powerful creatures in confined spaces (no flanking)

**Implementation**:
- Room analysis for features (open, cluttered, linear)
- Feature affinity tags on creatures
- Spawn selection weighted by feature match

### Corridor Ambush Points

**Concept**: Identify and utilize corridor tactical positions.

**Ambush Positions**:
- Intersections (multiple approach angles)
- Blind corners (surprise factor)
- Dead ends (trap potential)
- Chokepoints (force player through)

**Implementation**:
- Corridor analysis identifies tactical positions
- Ambush-tagged creatures prioritized for these spots
- Reduced spawn density in long straight corridors (boring)

---

## Variety & Emergent Experiences

### Encounter Templates

**Concept**: Pre-designed encounter patterns with randomized specifics.

**Template Examples**:
- **The Nest**: Many weak creatures around central strong creature
- **The Ambush**: Hidden creatures triggered by proximity
- **The Patrol**: Mobile group moving along defined route
- **The Guard Post**: Stationary defender with reinforcement trigger
- **The Ecosystem**: Predator creature with prey creature corpses
- **The Rival Factions**: Two hostile groups in proximity

**Implementation**:
- Templates as spawn strategies
- Template selects creature types from pool
- Specific creatures randomized within template constraints

### Event-Based Spawns

**Concept**: Some spawns occur in response to player actions or game events.

**Trigger Types**:
- **Alarm**: YellForHelp spawns reinforcements from edges
- **Discovery**: Opening containers might spawn guardians
- **Time**: Wandering monsters spawn after N turns
- **Noise**: Combat in area attracts nearby creatures

**Implementation**:
- EventSpawnStrategy listens to game signals
- Maintains "potential spawn" budget for events
- Spawns from pool appropriate to trigger context

### Modular Band Composition

**Concept**: Bands built from modular components rather than fixed definitions.

**Components**:
- **Leader Role**: Tank, support, commander, alpha
- **Core Group**: Main force composition
- **Support Elements**: Ranged, healers, scouts
- **Special Units**: Unique creatures, champions

**Example Goblin Band Generation**:
```
Leader: [goblin_ruffian OR goblin_shaman] (weighted)
Core: [2-4 goblins]
Support: [0-2 goblin_archers] (50% chance per slot)
Special: [goblin_champion] (5% chance, rare)
```

**Benefits**:
- Same "goblin band" type produces varied encounters
- Rare champions create memorable moments
- Support composition changes tactics required

### Creature Variants

**Concept**: Base creatures with contextual modifications.

**Variant Types**:
- **Elite**: +50% HP, slight stat boost, different color
- **Weakened**: Reduced stats, spawn with damage
- **Enraged**: Damage boost, reduced defense
- **Armored**: Bonus armor, slower
- **Veteran**: Better equipment, tactical AI

**Implementation**:
- Variant applied at spawn time
- Modifies base creature stats via conditions
- Visual differentiation via color or name prefix

---

## Player Power Integration

### Adaptive Difficulty

**Concept**: Spawn system considers player state for appropriate challenge.

**Player Power Factors**:
- Level (XP indicates experience)
- Equipment value (gear indicates capability)
- Health percentage (resource state)
- Consumable count (preparation level)

**Implementation Options**:
1. **Passive**: Just log player power for tuning data
2. **Soft**: Adjust spawn weights slightly
3. **Active**: Scale budgets and caps based on player power

**Concerns**:
- Roguelikes traditionally don't adapt difficulty
- Player agency feels reduced if spawns "adjust"
- Could create perverse incentives (stay weak for easy spawns)

**Recommendation**: Start with passive logging. Consider opt-in "adaptive difficulty" mode.

### Floor Depth Progression

**Current**: Different spawn tables per floor.

**Enhancement**: Tables define progression curve, not fixed values.
```yaml
creatureBudget:
  base: 80
  perFloor: 15
  variance: "2d10"
```

Result: Floor 1 gets 80+15+2d10, Floor 5 gets 80+75+2d10

**Benefits**:
- Single table can serve multiple floors
- Progression math in one place
- Easier to tune difficulty curve

---

## Implementation Priorities

### Phase 1: Threat Budget Foundation
1. Add threat cost to creature YAML
2. Modify budget calculation to use threat costs
3. Add per-encounter caps
4. Test with existing spawn tables

### Phase 2: Spatial Improvements
1. Add minimum distance from doors/entrances
2. Implement zone-based budget distribution
3. Improve chokepoint placement
4. Add objective-distance weighting

### Phase 3: Faction System
1. Add faction tags to creatures
2. Implement faction territory zones
3. Add faction relationship rules
4. Test faction-aware spawning

### Phase 4: AI Integration
1. Contextual AI component assignment
2. Patrol route generation
3. Enhanced pack dynamics
4. Position-based behavior modifiers

### Phase 5: Variety Systems
1. Encounter templates
2. Modular band composition
3. Creature variants
4. Event-based spawns

### Phase 6: Advanced Features
1. Room-type aware spawning
2. Equipment variety system
3. Environmental hazard pairing
4. Adaptive difficulty (opt-in)

---

## Open Questions

### Balance Considerations

**How much threat variance is healthy?**
- Too little: Every run feels same
- Too much: Some runs unwinnable
- Sweet spot: Meaningful variation within completable range

**Should player be able to "clear" a floor?**
- Traditional: Yes, kill everything for safety
- Alternative: Respawning patrols create time pressure
- Hybrid: Finite spawns but patrol routes persist

**How to handle "unfair" spawn configurations?**
- Prevention: Validation rules catch impossible situations
- Detection: Log and adjust spawn failures
- Recovery: Provide escape routes from overwhelming encounters

### Technical Considerations

**Performance impact of spatial analysis?**
- Room detection: One-time at generation
- Chokepoint analysis: Can be precomputed
- Distance calculations: Already used for spacing

**How to serialize spawn state for saves?**
- Current: Spawned entities saved, budget state lost
- Enhanced: Save remaining budget, spawn log, zone states

**How to support custom spawn tables?**
- Modding: YAML files are already moddable
- Validation: Schema validation for custom tables
- Fallbacks: Graceful handling of missing references

### Design Considerations

**What's the right encounter density?**
- Too dense: Feels like grinding through enemies
- Too sparse: Exploration feels empty
- Ideal: Every few rooms has meaningful encounter

**How much should spawning "tell a story"?**
- Minimal: Random monsters, player imagines context
- Moderate: Faction territories, environmental cues
- Heavy: Every spawn has narrative explanation

**Should spawns be deterministic from seed?**
- Yes: Seeded runs for competition/sharing
- Partially: Map deterministic, spawns randomized
- No: Every run fully random

---

## Inspiration Sources

### DCSS (Dungeon Crawl Stone Soup)
- Monster bands with leader mechanics
- Out-of-depth spawns for excitement
- Branch-specific spawn tables
- Vault-based special encounters

### Brogue
- Environmental integration (fire, water, gas)
- Creature ecosystems (hunting, fleeing)
- Ally recruitment from spawns
- Tactical terrain importance

### Caves of Qud
- Faction territories and relationships
- Dynamic world simulation
- Creature variants and mutations
- Procedural histories affecting spawns

### Cogmind
- Zone-based encounter design
- Alert level affecting spawns
- Patrol routes and investigation
- Equipment-based threats

---

## Summary

The current spawning system provides a solid foundation. The opportunity lies in:

1. **Threat-based budgeting** for balanced challenge distribution
2. **Spatial intelligence** for tactically interesting placement
3. **Faction ecosystems** for thematic coherence and emergent conflict
4. **AI integration** for contextual behavior and pack dynamics
5. **System integration** leveraging effects, conditions, and dungeon features
6. **Variety mechanisms** ensuring each run presents unique challenges

The goal: Every spawn should feel intentional, fair, and create interesting decisions for the player.

---

*"The dungeon doesn't just contain monsters—it's their home. Spawn them like they belong there."*
