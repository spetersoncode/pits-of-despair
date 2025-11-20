# Pits of Despair

**Don't even think about trying to escape.**

## Overview

Pits of Despair is a classic-style tile-based dungeon crawler roguelike built with Godot 4.5.1 and C#. You are a prisoner condemned to die by exile into the Pits of Despair - a foreboding, ever-shifting dungeon complex filled with unspeakable horrors and deadly monsters.

The only way to survive is to descend into the deepest depths of the Pits and claim the Throne of Despair. Battle creatures, discover treasures, and grow powerful enough to reach the final floor - or perish like countless others before you.

## Getting Started

1. Clone this repository
2. Open the project in Godot 4.5.1 or later
3. Ensure .NET support is enabled in your Godot installation
4. Run the project from the Godot editor

## Gameplay & Mechanics

- **Turn-Based Combat**: Opposed roll system, melee and ranged combat, armor reduces damage while evasion avoids hits
- **Grid-Based Movement**: Tile-based positioning with 8-directional movement
- **Procedural Dungeons**: Randomly generated dungeon layouts with rooms and corridors
- **Intelligent Enemies**: Monsters hunt, flee when wounded, call for help, and remember your last known position
- **Inventory System**: 26-slot inventory with item stacking for consumables, equipment slots, and item activation
- **Equipment**: Weapons and armor with stat bonuses that affect combat effectiveness
- **Status Effects**: Temporary buffs and debuffs that modify character attributes
- **Line of Sight**: Exploration through fog of war with field-of-view based visibility

## Inspirations

- [**Brogue**](https://sites.google.com/site/broguegame/) - Streamlined interface and elegant procedural generation
- [**Caves of Qud**](https://www.cavesofqud.com/) - Emergent gameplay through interacting systems
- [**Dungeon Crawl: Stone Soup**](https://crawl.develz.org/) - Refined mechanics and tactical depth
- [**Sil**](http://www.amirrorclear.net/flowers/game/sil/) - Minimalist stats and meaningful numbers
- [**Smart Kobold**](https://www.roguebasin.com/index.php/Smart_Kobold) - Goal-based intelligent monster AI

## Technical Details

- **Engine**: Godot 4.5.1
- **Language**: C#
- **Architecture**: Component-based design with signal-driven communication
- **Style**: 2D top-down perspective with ASCII-style rendering

## Architecture

**Component Composition**: Entities are built from composable child node components rather than inheritance hierarchies. Component types can be mixed and attached to entities as needed.

**Signal-Based Decoupling**: Cross-system communication uses Godot's signal system to avoid direct dependencies. Components emit signals for state changes; systems subscribe to relevant signals without knowing implementation details.

**Action System**: Turn-based gameplay uses an action queue with defined action types. Actions validate feasibility before execution and handle success/failure states.

**Turn Management**: A centralized TurnManager coordinates the turn cycle, alternating between player input and AI execution phases. Each turn processes actions sequentially through the action system.

**Data-Driven Configuration**: Game content (creatures, items, equipment, spawn tables, monster bands) is defined in YAML files loaded at runtime. The spawning system supports multiple placement strategies (formations, surrounding, random, center) with configurable density and out-of-depth spawning.

**System Architecture**: Independent systems operate on entities through their components. Systems read component data, emit events, and update state without direct coupling to other systems.

See [CLAUDE.md](CLAUDE.md) for detailed development guidelines and architectural principles.
