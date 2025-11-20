# Design Decisions

This document captures conscious design choices that shape gameplay philosophy and development direction. These decisions represent intentional trade-offs to create focused, engaging mechanics without unnecessary complexity.

## Core Philosophy

**Streamlined Roguelike**: Preserve tactical depth and meaningful choices while removing tedious or outdated mechanics that add busywork without strategic value.

**UI Clarity Over Feature Depth**: Game design prioritizes clarity, readability, and providing information needed for tactical gameplay. New mechanics must justify their UI complexity cost. Simple, clear interfaces that communicate essential information beat feature-rich systems requiring constant menu navigation.

## Core Mechanics

**Plentiful Consumables with Large Inventory**: Consumables are abundant resources for tactical problem-solving, not precious hoarded items. Large inventory space (52 slots) removes artificial scarcity and "save it for later" syndrome. Players make tactical decisions about *which* consumables solve current problems, not *whether* to use them at all. Combat depth comes from consumable combinations and situational usage, not resource anxiety. Liberal consumable use rewards experimentation and tactical creativity over hoarding.

## Rejected Mechanics

**No Identification System**: Item identification adds tedium (burning consumables to learn effects) or becomes rote puzzle-solving once players memorize "tricks" (price-checking, dip-testing, etc.). Players see exact item properties immediately. Focus stays on tactical decisions rather than meta-game knowledge.

**No Cursed Items**: Cursed equipment exists primarily to justify identification systems—an annoying "gotcha" mechanic punishing players for equipping unidentified items. Without item ID, curses serve no purpose beyond arbitrary punishment.

**No Hunger Clock**: Hunger timers force movement through levels without adding interesting decisions—just "eat food or die slowly." Pacing and difficulty scaling managed through spawning mechanics (enemy density, reinforcements, elite spawns) that create dynamic pressure without arbitrary timers.

**No Backtracking**: Strictly linear descent through dungeon levels with no branches. Players win by progressing forward and collecting the macguffin—never returning through cleared content or revisiting explored branches. Eliminates tedious navigation through empty areas and choice paralysis from branch exploration. Level transitions are one-way—decisions about engagement and descent become permanent, adding weight to resource management without requiring memory of cleared areas or navigation busywork.

## Design Constraints

**UI Complexity Budget**: Each new mechanic evaluated against UI cost. Features requiring dedicated panels, mode switching, or resource sub-types must provide proportional strategic depth.

**Example - Arrow Types**: Different arrow types (fire, ice, piercing) require inventory slots, equipment switching UI, and mental tracking during combat. Rejected unless combat system demands that level of tactical granularity. Prefer unified ammo with weapon-based variety.

**Principle**: Players should think about tactics and positioning, not inventory management and mode switching.

## Decision Criteria

**Adding Mechanics**: Does it create interesting decisions? Can existing systems handle it? What's the UI cost? Does it align with streamlined philosophy?

**Removing Mechanics**: Is it busywork disguised as depth? Does it require external knowledge or meta-gaming? Could spawning/pacing systems achieve the same goal?

---

*These decisions guide feature development and scope management. New entries added as significant design choices emerge.*
