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

- **File naming**: lowercase with hyphens (e.g., `action-system.md`, `ai-architecture.md`)
- **Format**: Markdown with clear headings for easy parsing and navigation
- **Structure**: Optimized for both human readability and AI consumption with scannable sections
- **Content**: Design-focused and implementation-agnostic where possible

## Project Overview

See **[README.md](../README.md)** for game overview, mechanics, and technical stack. See **[CLAUDE.md](../CLAUDE.md)** for development principles, code practices, and workflow.
