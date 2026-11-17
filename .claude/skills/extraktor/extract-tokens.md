---
name: extract-tokens
description: Extract design tokens (colors, fonts, spacing) from any URL into Tailwind config, CSS variables, and DTCG tokens. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Extract Design Tokens

Pure extraction, no AI — works for any Claude Code user (subscription or API).

```bash
npx extraktor extract <url> -o ./tokens --format tailwind tokens css-vars
```

After extraction, read the output files:
- `tailwind.config.ts` — drop into any Tailwind project
- `variables.css` — CSS custom properties
- `tokens.json` — DTCG standard design tokens

Present the extracted colors, fonts, and spacing to the user. If they want to use them, help integrate into their project.

Options: `--scroll` (lazy content), `--hover-states`, `--timeout <ms>`, `--no-headless` (visible browser), `--devtools` (connect to authenticated Chrome).

## When to use
User says: "extract tokens", "get the design tokens", "what tokens does X use", "pull colors and fonts from <url>".
