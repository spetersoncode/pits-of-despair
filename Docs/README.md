# Documentation Library

This folder contains architecture and game design documentation for **Pits of Despair**. Documentation is written for human reference and optimized for efficient AI agent consumption.

## For AI Coding Agents

**Prioritize these documents before deep codebase exploration** for efficiency (quickly understand architecture and decisions), context (the "why" behind choices), and accuracy (align with established patterns).

- **Use docs for**: System architecture, design patterns, data formats, intended behavior, cross-system interactions
- **Use code for**: Implementation details, current state, specific APIs, edge cases

## Documentation Philosophy

**Focus on Design, Not Implementation**

Capture enduring design decisions, architectural principles, and the "why"—not implementation details that change over time.

- **Document**: Design philosophy, architectural patterns, system interactions, data formats, decision rationale, conceptual models
- **Don't document**: File paths, class names, code examples, APIs, implementation steps, current codebase state

Implementation details belong in code and comments. Documentation helps understand design for effective navigation.

**Token Efficiency and Information Density**

Documentation should be concise and information-dense, maximizing value per token for AI consumption:

- **Get to the point**: Lead with key concepts, no lengthy preambles
- **No filler**: Every sentence should convey meaningful information
- **Dense explanations**: Pack concepts tightly without sacrificing clarity
- **Avoid redundancy**: Don't restate concepts already explained unless adding new insight
- **Concise examples**: Use minimal, targeted examples that illustrate the concept
- **Active voice**: Prefer direct, active constructions over passive voice

Think of documentation as a compressed knowledge format - readers (human or AI) should gain maximum understanding with minimum token expenditure.

**DRY and Atomic Documentation**

Each document has single, focused purpose. Information exists in exactly one place. Documents cover one coherent subject. Cross-reference rather than duplicate. Extract repeated concepts into dedicated documents.

**When you find duplication:**
1. Identify which document owns the concept
2. Remove duplicated content from others
3. Add cross-references to authoritative document
4. If no clear owner, create new focused document for shared concept

## Conventions

- **File naming**: lowercase, favor one word names, otherwise with hyphens (e.g., `action-system.md`, `ai-architecture.md`)
- **Format**: Markdown with clear headings for easy parsing and navigation
- **Structure**: Optimized for both human readability and AI consumption with scannable sections
- **Content**: Design-focused and implementation-agnostic where possible

### Markdown Style Guide

**Bold Labels**: Use `**Label**: description` format with colon after bold text for consistency.

**Lists**: Use bullet points (`-`) for categorized items or related concepts. Examples:
- Semantic naming patterns (State-Based, Material-Based, etc.)
- Document/Don't document guidelines
- Validation and execution patterns

**Two-Part Patterns**: When describing complementary concepts (Validation/Execution, Scene-based/Code-based), format as:

```
**First**: Description of first concept.

**Second**: Description of second concept.
```

**Document Closings**: Architecture docs end with `---` separator and italicized cross-reference or summary statement. Meta-docs (like this README) don't require closings.

**Code Identifiers**: Use backticks for method names with parentheses (`CanExecute()`), flags (`HasMovement`), and type names.

## Creating New Documentation

**When to create**: Coherent systems reach sufficient architectural complexity. Design decisions need recording. Cross-system interactions require clarification. New contributors would benefit from design context before exploring code.

**When not to create**: Single classes or small features (use code comments). Implementation details that change frequently. Temporary features. Topics already covered (extend existing docs instead).

**Process**: Research codebase thoroughly to understand architecture. Focus on design decisions, patterns, and integration points—not implementation details. Follow conventions above for structure and style. Check for duplication with existing docs—cross-reference rather than duplicate.

**Extensible Patterns**: If documenting a system following a command or factory pattern (actions, effects, components, etc.) where developers create new instances, include an "Adding New [X]" section. Explain the complete wiring process: what needs to be implemented, where to register (factories, inputs, etc.), integration points with other systems, and design considerations. See effects.md and actions.md for examples.

## Project Overview

See **[README.md](../README.md)** for game overview, mechanics, and technical stack. See **[CLAUDE.md](../CLAUDE.md)** for development principles, code practices, and workflow.

## System Documentation

**Core Systems**:
- **[actions.md](actions.md)**: Action system architecture for turn-based entity behaviors
- **[components.md](components.md)**: Component-based entity composition patterns
- **[dungeon-generator.md](dungeon-generator.md)**: Binary Space Partitioning algorithm for procedural dungeon generation
- **[entities.md](entities.md)**: Entity architecture and lifecycle management
- **[spawning.md](spawning.md)**: Budget-based dungeon population with weighted spawn tables
- **[turn-based.md](turn-based.md)**: Turn-based coordination and phase management

**Supporting Systems**:
- **[ai.md](ai.md)**: AI architecture and goal-based decision making
- **[effects.md](effects.md)**: Instantaneous and time-based effect application
- **[status.md](status.md)**: Status effect system with turn-based lifecycle

**Rendering & Presentation**:
- **[text-renderer.md](text-renderer.md)**: Tile-based glyph rendering with fog-of-war and layered display

**Visual Design**:
- **[color.md](color.md)**: Color theming and visual feedback patterns
- **[glyphs.md](glyphs.md)**: Glyph design philosophy and readability guidelines
