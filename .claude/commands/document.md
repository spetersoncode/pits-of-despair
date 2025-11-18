Research the codebase and create architecture documentation on the specified topic.

**Input:** {0}

Parse the input to extract:
- **Topic:** The subject to document (e.g., "effects system")
- **Filename:** If specified using "in filename.md" pattern (e.g., "effects.md"), otherwise generate appropriate filename following lowercase-with-hyphens convention

**Process:**
1. Review Docs/README.md for documentation philosophy and conventions
2. Use Task tool with subagent_type=Explore (thoroughness: "very thorough") to research the codebase for the topic
3. Create or update a markdown document in Docs/ following ALL conventions from the README
4. Focus on design decisions and architecture - avoid implementation details
5. Check for existing documentation on this topic first - update rather than duplicate

Follow the README's documentation philosophy, style guide, and DRY principles.
