---
name: analyze-design
description: Quick analysis of any website's design — colors, fonts, spacing, layout patterns, framework detection. No files generated. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Analyze Design

Quick design read, no output files, no AI calls.

```bash
npx extraktor analyze <url>
```

Returns: color palette, font families, font sizes, spacing values, shadow count, animation count, SPA framework detection, element count, CSS variable count.

Summarize the result for the user in plain English — highlight the 2-3 most distinctive signals (e.g. "monochrome palette, 2 fonts, heavy motion").

## When to use
User asks things like:
- "What colors does this site use?"
- "What font is that?"
- "How many design tokens does this site have?"
- "What framework is this built with?"
- "Give me a quick read on <url>"
