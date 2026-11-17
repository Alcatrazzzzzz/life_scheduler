---
name: design-md
description: Generate a DESIGN.md file from any website. Stitch-compatible 9-section format that AI agents can use to build pixel-perfect UI. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Generate DESIGN.md

Produce a 9-section Stitch-format `DESIGN.md` from any URL. Two paths — the **subscription path is the default**.

## Subscription path (recommended, no API key)

1. Run the prep command (pure extraction, API-free):
   ```bash
   npx extraktor design-md-prep <url> -o ./design-md-prep
   ```
   Outputs:
   - `prep.json` — orchestration manifest
   - `raw-tokens.json` — colors, typography, spacing, effects, animations
   - `layout.json` — page architecture (sections, grid, breakpoints)
   - `claude-instructions.md` — exact steps for you to follow

2. Read `./design-md-prep/claude-instructions.md` and follow it. You will:
   - Ground every color / font / spacing value in `raw-tokens.json`
   - Fill all 9 Stitch sections (Visual Theme, Color Palette, Typography, Components, Layout, Depth & Effects, Dos & Don'ts, Responsive Behavior, Agent Prompt)
   - Write a single `DESIGN.md` to the user's project root (or wherever they asked)

3. Print the summary line from the instructions when done (e.g. `DESIGN.md generated — 9 sections, 12 colors, 2 fonts`).

## Zero-AI path (legacy)

If the user just wants the extract-based template without any AI polish:
```bash
npx extraktor design-md <url> -o ./DESIGN.md
```
This produces a valid DESIGN.md from pure extraction — no AI polish on the Visual Theme narrative.

## Standalone CLI with API key (optional)

Users outside Claude Code can run the one-shot with AI narrative polish:
```bash
npx extraktor design-md <url> --api-key $ANTHROPIC_API_KEY -o ./DESIGN.md
```

## After generating

Tell the user to drop `DESIGN.md` into their project root. Any AI coding agent (Claude Code, Cursor, Copilot) can then read it and build UI that matches the source design. Compatible with the Stitch DESIGN.md format used by [awesome-design-md](https://github.com/VoltAgent/awesome-design-md).

## Variant: Claude Design chat paste

`npx extraktor design-md <url> --for-claude-design -o ./DESIGN.md` (or the same flag on `design-md-prep`) outputs a tight, imperative version (~80 lines) designed to be pasted into the Claude Design setup chat. See `skills/claude-design.md`.

## When to use
User says: "generate DESIGN.md", "make a design doc from this site", "Stitch format DESIGN.md", "drop in a DESIGN.md for Cursor/Copilot".
