---
name: genome
description: Reverse-engineer a website into a complete design system with React components, Storybook, and portable genome.json. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Genome — Design System Extraction

Capture a URL's visual design, then build a reusable React/Tailwind component library in the source's aesthetic — entirely within your Claude Code session.

## Execute (subscription path — recommended)

1. Run the prep command (pure extraction — tokens, screenshots, DOM, scaffolded output). No API key used:
   ```bash
   npx extraktor genome-prep <url> -o ./genome-output
   ```

2. Read the output structure:
   - `./genome-output/prep.json` — orchestration manifest
   - `./genome-output/claude-instructions.md` — **exact steps for you to follow**
   - `./genome-output/screenshots/full-page.png` — full source screenshot
   - `./genome-output/screenshots/section-*.png` — per-section crops
   - `./genome-output/sections.json` — DOM selectors + bounds + truncated HTML per section
   - `./genome-output/extraction/` — `design-tokens.json`, `tailwind.config.ts`, `variables.css`, `layout-architecture.json`
   - `./genome-output/output-template/` — scaffolded directory: `components/`, `theme/`, `stories/`, `genome.json.template`

3. Follow `claude-instructions.md` step-by-step:
   - For each section screenshot, identify the visual components (heading, nav, card, button, etc.)
   - For each unique component, read its section screenshot **and** DOM fragment, then write a clean React/Tailwind file to `./genome-output/output-template/components/{PascalName}.tsx`
   - Copy + extend theme files into `output-template/theme/` (Tailwind config + `globals.css`)
   - Generate `output-template/components/index.ts` barrel
   - Generate one `.stories.tsx` per component in `output-template/stories/`
   - Fill `genome.json.template`: populate `theme` (colors + relationships), `components`, `layout.patterns`, `layout.gridSystem`, and a 2–3 sentence `styleNarrative`
   - Rename the filled template to `genome.json` at the output root

4. Summarize for the user: component count, style narrative, color palette. Print the instructed line: `Genome built — N components, M colors, K sections`.

## Style rules (from claude-instructions.md)

- Every Tailwind class must reference the extracted theme — do not invent hex values.
- `styleNarrative` ~2–3 sentences, descriptive and matter-of-fact.
- Keep component props minimal; type them with a `PropsInterface`.
- No lorem ipsum — use realistic placeholder content fitting the section.

## Flags (genome-prep)

`-o <dir>`, `--timeout <ms>`, `--headless`, `--devtools [endpoint]` (connect to authenticated Chrome).

## Standalone CLI fallback (API key)

For CI or scripted one-shot runs outside Claude Code:
```bash
npx extraktor genome <url> -o ./genome --api-key $ANTHROPIC_API_KEY
```
This path burns API credits on a subprocess vision call. Not needed from a Claude Code session.

## When to use

User mentions: "reusable design system", "React components from this site", "Storybook", "extract components", "regenerate pages later", "genome", or phrases like "clone the design system of X into components".
