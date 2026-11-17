---
name: scan-repo
description: Extract the design system from a local codebase (not a URL). Reads package.json, tailwind config, CSS, and component files to produce the same token artifacts as `extract`. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Scan Repo

When the user points at a **local directory** instead of a URL, run the repo scanner. Pure filesystem — no Playwright, no network, safe in locked-down environments.

```bash
npx extraktor repo-scan ./path/to/project -o ./scanned
```

## What it produces

In the output directory:

- `tailwind.config.ts` — Tailwind config generated from the detected tokens (same transformer as `extract`)
- `variables.css` — CSS custom properties
- `tokens.json` — DTCG-style design tokens
- `repo-manifest.json` — summary of what was detected (framework, file counts, token counts, Google Fonts, font/icon deps)
- `extraction.json` — full `ExtractionResult`-shaped JSON (downstream transformers / genome tooling can consume this)

## Detection flow (fall-through)

1. `package.json` — framework (next/react/vue/svelte/angular), tailwind version, shadcn signature (CVA + tailwind-merge, or radix-slot + clsx), font deps (`@fontsource/*`, `next/font`, `geist`), icon deps (`lucide-react`, `@radix-ui/react-icons`, `@heroicons/*`, etc.)
2. `tailwind.config.{ts,js,mjs,cjs}` — `theme.extend.*` parsed via brace-matching + regex (no TS eval). Handles nested color blocks (`primary: { DEFAULT, foreground }`), fontFamily arrays, spacing, borderRadius, boxShadow, keyframes, screens.
3. **CSS files** (`*.css`, `*.scss`, `*.sass`) — `:root { --* }` custom properties AND Tailwind 4 `@theme { ... }` blocks. Extracts colors, fonts, spacing, radii, shadows, and `@keyframes` names.
4. **Component files** (`.tsx`, `.jsx`, `.vue`, `.svelte`) — inline hex colors (frequency counted), inline `font-family` declarations.
5. **HTML files** — Google Fonts `<link>` tags parsed for family names.

Ignored dirs: `node_modules`, `.git`, `.next`, `dist`, `build`, `out`, `coverage`, `.turbo`, `.cache`. Hidden dirs are skipped (except `.storybook`).

## Use this when the user says

- "scan my repo"
- "extract tokens from this project"
- "what design system is already in here"
- "convert my existing tailwind config to..."
- "I have a codebase not a URL"
- "read my design system"

## After scanning

1. Read `repo-manifest.json` and summarize for the user: detected framework, tailwind version, shadcn yes/no, token counts.
2. Flag gaps worth filling:
   - 0 fontSizes → the project relies on Tailwind defaults; worth extracting explicit scale if they want portability.
   - 0 shadows → no depth tokens; probably intentional if design is flat.
   - 0 keyframes → no motion tokens; worth asking before using `refine-design` to add them.
3. Offer next steps:
   - Run `extraktor extract` on a deployed URL to augment what's missing.
   - Hand the output to `refine-design` for iterative tweaks.
   - Pipe `tokens.json` into Figma via `export-tokens`.

## What this skill does NOT do

- It does **not** run any browser or network code.
- It does **not** evaluate `tailwind.config.ts` — it parses it statically. Dynamic `require()` / spreads from other modules are NOT followed. If a project's theme is built dynamically, fall through to CSS scanning and note it in the summary.
- It does **not** write to the scanned directory. Output goes to `-o <dir>` only.
