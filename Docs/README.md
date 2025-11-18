# Documentation Library

This folder contains architecture and game design documentation for **Pits of Despair**. Documentation is written for human reference and optimized for efficient AI agent consumption.

## For AI Coding Agents

When working on this project, **prioritize these documents** before conducting deep codebase exploration:

- **Efficiency**: Check relevant docs here first to quickly understand system architecture and design decisions
- **Context**: Use documentation to understand the "why" behind implementation choices
- **Accuracy**: Reference docs to ensure changes align with established patterns and architectural principles

**When to use documentation vs code exploration:**
- Use docs for: System architecture, design patterns, data formats, intended behavior, cross-system interactions
- Use code exploration for: Implementation details, current state, specific APIs, edge cases

## Documentation Philosophy

**Focus on Design, Not Implementation**

Documentation should capture enduring design decisions, architectural principles, and the "why" behind systems - not implementation details that change over time.

**What to Document:**
- Design philosophy and core principles
- Architectural patterns and system interactions
- Data formats and their intended semantics
- Decision rationale and trade-offs
- Conceptual models and intended behavior

**What NOT to Document:**
- Specific file paths or class names
- Detailed code examples or APIs
- Step-by-step implementation instructions
- Current state of the codebase

Implementation details belong in code, comments, and exploration. Documentation should help you understand the system's design so you can navigate the implementation effectively.

## Conventions

- **File naming**: lowercase with hyphens (e.g., `action-system.md`, `ai-architecture.md`)
- **Format**: Markdown with clear headings for easy parsing and navigation
- **Structure**: Optimized for both human readability and AI consumption with scannable sections
- **Content**: Design-focused and implementation-agnostic where possible

## Project Overview

For general project information, see:
- **[README.md](../README.md)** - Game overview, mechanics, and technical stack
- **[CLAUDE.md](../CLAUDE.md)** - Development principles, code practices, and workflow

---

*As the project evolves, this library will grow to include comprehensive documentation on major systems, architectural patterns, and game design decisions.*
