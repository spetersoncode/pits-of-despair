# Skills System Implementation Plan

Checklist document for implementing the Skills system. Each phase builds on the previous.

**Reference:** See `skills-system.md` for detailed design decisions and skill definitions.

---

## Phase 1: Foundation ✓

### Session 1A: Stat Cap & Willpower Component
- [x] `StatsComponent.cs` - Add `STAT_CAP = 12` constant
- [x] `StatsComponent.cs` - Enforce cap in stat modification methods
- [x] `WillpowerComponent.cs` - **CREATE**
  - [x] `MaxWillpower` property: `10 + (WIL × 5)`
  - [x] `CurrentWillpower` property
  - [x] Accumulator-based regen (like HealthComponent pattern)
  - [x] `BaseRegenRate` formula (e.g., `10 + MaxWillpower / 5`)
  - [x] Subscribe to TurnManager signals for per-turn regen tick
  - [x] `WillpowerChanged(current, max)` signal
  - [x] `SpendWillpower(amount)` - returns bool, emits signal
  - [x] `RestoreWillpower(amount)` - clamps to max, emits signal
  - [x] `FullRestore()` - for floor transitions
- [x] `Player.cs` - Add WillpowerComponent as child in `_Ready()`

### Session 1B: Willpower Integration
- [x] Hook floor descent event for full WP restore
- [x] Add WP bar to HUD (beside/below HP bar)
- [x] Verify WP updates when WIL stat changes

---

## Phase 2: Skill Data & Storage ✓

### Session 2A: Skill Data Schema
- [x] `SkillCategory.cs` - **CREATE** enum (Active, Passive, Reactive, Aura)
- [x] `TargetingType.cs` - **CREATE** enum (Self, Adjacent, Tile, Enemy, Ally, Area, Line, Cone)
- [x] `SkillDefinition.cs` - **CREATE** data class
  - [x] Id, Name, Description
  - [x] Category, TargetingType, Range, AreaSize
  - [x] WillpowerCost
  - [x] Prerequisites (STR, AGI, END, WIL requirements)
  - [x] Trigger type (for reactive skills)
  - [x] AuraRadius, AuraTarget (for auras)
  - [x] Effects list
  - [x] Tags, Tier (for sorting/filtering)
- [x] `Data/Skills/` - **CREATE** initial skill definitions (9 test skills)
  - [x] 1 Universal skill (second_wind)
  - [x] 2 STR skills (power_attack, cleave)
  - [x] 2 AGI skills (quick_step, double_shot)
  - [x] 2 END skills (tough, recover)
  - [x] 2 WIL skills (magic_missile, minor_heal)
- [x] `DataLoader.cs` - Add `GetSkill(id)` method
- [x] `DataLoader.cs` - Add `GetAllSkills()` method
- [x] `DataLoader.cs` - Load skills on startup

### Session 2B: Skill Component
- [x] `PrerequisiteChecker.cs` - **CREATE** utility class
  - [x] `MeetsPrerequisites(skillDef, statsComponent)` → bool
  - [x] `GetMissingPrerequisites(skillDef, statsComponent)` → dict
- [x] `SkillComponent.cs` - **CREATE**
  - [x] `LearnedSkills` list (skill IDs)
  - [x] `SkillPointsUsed` counter
  - [x] `LearnSkill(skillId)` - adds to list, emits signal
  - [x] `HasSkill(skillId)` → bool
  - [x] `GetAvailableSkills(statsComponent)` - filter by prereqs & not learned
  - [x] `GetLearnedSkillsByCategory()` - for UI grouping
  - [x] `CanLearnSkill(skillId, statsComponent)` → bool
  - [x] `SkillLearned(skillId)` signal
- [x] `Player.cs` - Add SkillComponent as child in `_Ready()`
- [x] Debug console: `skill learn <id>` command
- [x] Debug console: `skill list` command

---

## Phase 3: Level-Up Flow ✓

### Session 3A: Level-Up Modal - Stat Selection
- [x] `LevelUpModal.cs` - **CREATE**
- [x] `Scenes/UI/LevelUpModal.tscn` - **CREATE**
- [x] Display 4 stat options (S/A/E/W hotkeys)
- [x] Show current → new value for each stat
- [x] Preview derived stat changes (HP, WP, damage, etc.)
- [x] Show skills that would be unlocked by each choice
- [x] Grey out / disable stats at cap (12)
- [x] Apply stat immediately on selection
- [x] Emit signal when stat selected

### Session 3B: Level-Up Modal - Skill Selection
- [x] Add skill selection phase to LevelUpModal
- [x] Track which levels grant skills: 2-10, 12, 14, 16, 18, 20, 21
- [x] Filter skills: meets prereqs AND not already learned
- [x] Sort by: tier, category, stat requirement
- [x] Display: name, type, cost, description, prereqs
- [x] Highlight newly-unlocked skills (from stat just chosen)
- [x] Number keys for selection
- [x] Learn skill on selection, close modal

### Session 3C: Level-Up Integration
- [x] `InputManager.cs` - L key opens level-up modal when available
- [x] Track pending level-ups (XP can overflow multiple levels)
- [x] Discrete processing: one level at a time, no banking
- [x] HUD indicator: "LEVEL UP! [L]" when pending
- [x] Block other actions while modal open

---

## Phase 4: Active Skill Execution ✓

### Session 4A: Skill Action Framework
- [x] `SkillAction.cs` - **CREATE** (extends Action)
  - [x] Constructor takes skill ID + target info
  - [x] `CanExecute()`: skill learned, WP available, target valid
  - [x] `Execute()`: spend WP, run effects, return result
- [x] `SkillExecutor.cs` - **CREATE** central execution logic
  - [x] Resolve targeting
  - [x] Apply all effects in sequence
  - [x] Handle success/failure messaging

### Session 4B: Effect System
- [x] `Scripts/Skills/Effects/` directory - **CREATE**
- [x] `SkillEffect.cs` - **CREATE** base class
  - [x] `Apply(caster, targets, context)` abstract method
- [x] `DamageEffect.cs` - Deal damage (flat, dice, scaled)
- [x] `HealEffect.cs` - Restore HP
- [x] `ApplyConditionEffect.cs` - Apply status condition
- [x] `TeleportEffect.cs` - Move entity to tile
- [x] `KnockbackEffect.cs` - Push target away
- [ ] `ModifyStatEffect.cs` - Temporary stat change (deferred)
- [ ] `MultiTargetEffect.cs` - Wrapper for hitting multiple targets (deferred)
- [ ] `AreaEffect.cs` - Affect all entities in radius (deferred)

### Session 4C: Targeting System
- [x] `Scripts/Skills/Targeting/` directory - **CREATE**
- [x] `TargetingHandler.cs` - **CREATE** base class
  - [x] `GetValidTargets(caster, context)` → list
  - [x] `IsValidTarget(caster, target, context)` → bool
- [x] `SelfTargeting.cs` - No selection, targets caster
- [x] `AdjacentTargeting.cs` - 8 adjacent tiles
- [x] `TileTargeting.cs` - Any tile in range
- [x] `EnemyTargeting.cs` - Enemy entities in range
- [x] `AllyTargeting.cs` - Allied entities in range
- [x] `AreaTargeting.cs` - Select center, affect radius
- [x] Targeting overlay UI (highlight valid tiles)
- [x] Input mode: switch to targeting, click/key to confirm, Escape to cancel

---

## Phase 5: Skill Menu UI (In Progress)

### Session 5: Skill Menu
- [x] `SkillsModal.cs` - **CREATE**
- [ ] `Scenes/UI/SkillsModal.tscn` - **CREATE** (needs scene in Godot editor)
- [x] Z key opens/closes menu
- [x] Sections: Active / Passive / Reactive / Auras
- [x] For each skill: name, WP cost, type indicator, description
- [x] Keyboard navigation (number keys 1-9, 0)
- [x] Select active skill → close menu → enter targeting mode
- [x] Show "Always Active" for passives
- [x] Show trigger condition for reactives
- [x] Escape closes menu

---

## MVP Checkpoint (After Phase 5)

- [ ] Willpower system working
- [ ] 20-25 skills implemented (mix of types)
- [ ] Level-up grants stats + skills on correct levels
- [ ] Skill menu functional (Z key)
- [ ] Basic targeting working (self, adjacent, range)

---

## Phase 6: Passive Skills

### Session 6A: Passive Processor
- [ ] `PassiveSkillProcessor.cs` - **CREATE**
  - [ ] On skill learn: check if passive, register effects
  - [ ] Use multi-source modifier pattern: source = `skill_<skillId>`
  - [ ] Support: flat bonuses, percentage bonuses
  - [ ] Support: conditional bonuses (below 50% HP, etc.)
  - [ ] On skill "forget" (if ever): unregister effects

### Session 6B: Passive Skill Content
- [ ] Implement STR passives: Mighty Thews, Relentless
- [ ] Implement AGI passives: Fleet of Foot, Evasion
- [ ] Implement END passives: Tough, Grit, Thick Skinned, Regeneration, Fortified
- [ ] Implement WIL passives: Arcane Focus
- [ ] Implement hybrid passives: Weapon Mastery, Warmaster, etc.
- [ ] Verify stat modifiers apply correctly

---

## Phase 7: Reactive Skills

### Session 7: Reactive Processor
- [ ] `ReactiveSkillProcessor.cs` - **CREATE**
  - [ ] Subscribe to relevant signals based on learned reactive skills
  - [ ] Trigger types: on_hit, on_miss, on_dodge, on_damage, on_kill, on_low_hp
  - [ ] On trigger: check WP cost (if any), execute effect
  - [ ] Auto-trigger vs prompt (per skill definition)
- [ ] Implement: Die Hard (on lethal → survive at 1 HP)
- [ ] Implement: Riposte (on enemy miss → counter-attack)
- [ ] Implement: Sidestep (on dodge → free move)
- [ ] Implement: Rampage (on kill → free attack)
- [ ] Implement: Revenge (on damage → damage bonus)

---

## Phase 8: Aura Skills

### Session 8: Aura Processor
- [ ] `AuraProcessor.cs` - **CREATE**
  - [ ] Track active auras on entities
  - [ ] On movement: recalculate affected entities
  - [ ] Apply aura conditions to entities in range
  - [ ] Remove aura conditions when entities leave range
  - [ ] Source tracking: `aura_<skillId>_<entityId>`
- [ ] Implement: Protective Aura (allies take reduced damage)
- [ ] Implement: Command Aura (allies gain attack/damage bonus)
- [ ] Implement: War Cry (enemies debuffed in range)

---

## Phase 9: Content & Polish

### Session 9A: Remaining Active Skills
- [ ] STR skills: Power Attack, Shove, War Cry, Cleave, Sunder Armor, Execute, Whirlwind, Rage, Earthquake, Unstoppable, Worldbreaker
- [ ] AGI skills: Quick Step, Precise Shot, Double Shot, Vanish, Shadow Step, Triple Shot, Assassinate, Perfect Dodge, Death from Above, Phantom
- [ ] END skills: Recover, Iron Skin, Indomitable, Undying, Stone Body, Eternal
- [ ] WIL skills: Magic Missile, Light, Minor Heal, Fireball, Fear, Blink, Slow, Chain Lightning, Paralyze, Summon Elemental, Disintegrate, Dominate, Time Stop, Archmage

### Session 9B: Hybrid Skills
- [ ] STR/AGI: Feint, Blade Dance, Perfect Strike, Thousand Cuts
- [ ] STR/END: Reckless Attack, Shield Wall, Unbreakable
- [ ] STR/WIL: Flame Blade, Shocking Grasp, Giant Strength, Spell Strike, Arcane Warrior
- [ ] AGI/END: Defensive Roll, Evasive Recovery, Mobile Shot, Ranger's Resilience, Slippery
- [ ] AGI/WIL: Mirror Image, Phase Arrow, Greater Invisibility, Arcane Archer, Dimensional Step
- [ ] END/WIL: Life Drain, Blood Magic, Raise Undead, Dark Pact
- [ ] Three-stat skills: Combat Expertise, Elemental Blade Dance, Dark Knight, Twilight Sentinel
- [ ] Four-stat skills: Jack of All Trades, Adaptability, Transcendence

### Session 9C: Visual & Audio Polish
- [ ] Skill activation floating text
- [ ] Spell particle effects (fire, lightning, etc.)
- [ ] Sound effects for skill categories
- [ ] Visual feedback for buff/debuff application

### Session 9D: Balance Pass
- [ ] Playtest pure STR build (1-21)
- [ ] Playtest pure WIL caster build
- [ ] Playtest balanced 5-5-5-5 build
- [ ] Playtest hybrid builds
- [ ] Adjust WP costs as needed
- [ ] Adjust damage/healing values as needed

---

## Architecture Notes

### Willpower Regeneration
Uses accumulator pattern like HealthComponent:
- Each turn adds `BaseRegenRate` to `_regenPoints`
- At 100 points, restore 1 WP and subtract 100
- `BaseRegenRate = 10 + MaxWillpower / 5` (tunable)
- Floor descent = full WP restore
- No WP from kills

### Multi-Source Modifier Pattern
For passive skill bonuses, use existing pattern from StatsComponent:
```csharp
// Source format: "skill_<skillId>"
statsComponent.AddStrengthModifier("skill_mighty_thews", 1);
```

### Signal Connections
Always use Godot signal pattern (not C# events):
```csharp
// Subscribe
component.Connect(Component.SignalName.Signal, Callable.From<T>(Handler));

// Unsubscribe in _ExitTree()
component.Disconnect(Component.SignalName.Signal, Callable.From<T>(Handler));
```

### File Structure
```
Scripts/
├── Components/
│   ├── WillpowerComponent.cs      (Phase 1)
│   └── SkillComponent.cs          (Phase 2)
├── Skills/
│   ├── SkillDefinition.cs         (Phase 2)
│   ├── SkillCategory.cs           (Phase 2)
│   ├── TargetingType.cs           (Phase 2)
│   ├── PrerequisiteChecker.cs     (Phase 2)
│   ├── SkillExecutor.cs           (Phase 4)
│   ├── PassiveSkillProcessor.cs   (Phase 6)
│   ├── ReactiveSkillProcessor.cs  (Phase 7)
│   ├── AuraProcessor.cs           (Phase 8)
│   ├── Effects/                   (Phase 4)
│   └── Targeting/                 (Phase 4)
├── Actions/
│   └── SkillAction.cs             (Phase 4)
└── UI/
    ├── LevelUpModal.cs            (Phase 3)
    └── SkillMenu.cs               (Phase 5)

Data/
└── Skills/
    └── skills.yaml                (Phase 2)
```

---

*Last updated: 2025-11-22*
