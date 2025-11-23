# Documentation Library

This folder contains architecture and game design documentation for **Pits of Despair**. Documentation is written for human reference and optimized for efficient AI agent consumption.

## For AI Coding Agents

**Prioritize these documents before deep codebase exploration** for efficiency (quickly understand architecture and decisions), context (the "why" behind choices), and accuracy (align with established patterns).

- **Use docs for**: System architecture, design patterns, data formats, intended behavior, cross-system interactions
- **Use code for**: Implementation details, current state, specific APIs, edge cases

## Creating Documentation

See **[documentation.md](documentation.md)** for documentation philosophy, conventions, and creation guidelines.

## Project Overview

See **[README.md](../README.md)** for game overview, mechanics, and technical stack. See **[CLAUDE.md](../CLAUDE.md)** for development principles, code practices, and workflow.

## Game Design

- **[design-decisions.md](design-decisions.md)**: Conscious design choices and rejected mechanics

## System Documentation

**Core Systems**:
- **[actions.md](actions.md)**: Action system architecture for turn-based entity behaviors
- **[combat.md](combat.md)**: Combat resolution with opposed rolls, damage calculation, and attack types
- **[components.md](components.md)**: Component-based entity composition patterns
- **[dungeon-generator.md](dungeon-generator.md)**: Binary Space Partitioning algorithm for procedural dungeon generation
- **[entities.md](entities.md)**: Entity architecture and lifecycle management
- **[spawning.md](spawning.md)**: Budget-based dungeon population with weighted spawn tables
- **[turn-based.md](turn-based.md)**: Turn-based coordination and phase management
- **[yaml.md](yaml.md)**: YAML data system with type-based defaults and content creation patterns

**Supporting Systems**:
- **[ai.md](ai.md)**: AI architecture and goal-based decision making
- **[autoexplore.md](autoexplore.md)**: Automated dungeon exploration with safety interrupts
- **[effects.md](effects.md)**: Instantaneous and time-based effect application
- **[progression.md](progression.md)**: Experience points, leveling, and stat growth
- **[statistics.md](statistics.md)**: Multi-source stat tracking and combat value calculations
- **[conditions.md](conditions.md)**: Condition system with turn-based lifecycle and equipment bonuses
- **[targeting.md](targeting.md)**: Unified targeting system for ranged attacks, reach weapons, and targeted items

**Rendering & Presentation**:
- **[text-renderer.md](text-renderer.md)**: Tile-based glyph rendering with fog-of-war and layered display

**Visual Design**:
- **[color.md](color.md)**: Color theming and visual feedback patterns
- **[glyphs.md](glyphs.md)**: Glyph design philosophy and readability guidelines

**Development Tools**:
- **[debug-commands.md](debug-commands.md)**: Debug command system for runtime testing and inspection
