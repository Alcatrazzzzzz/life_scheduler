---
name: merge-genomes
description: Combine 2+ extraktor genomes into a hybrid design system. Mix and match tokens, components, and narratives from multiple sources. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Merge Genomes

When the user wants to combine design systems from multiple extraktor outputs — "Stripe colors with Linear typography", "Vercel layout with Notion's warmth" — produce a hybrid genome without re-running extraction on either source.

## Input check

Identify the source genome directories. Each must have either:
- `./<name>/genome.json` + `./<name>/output-template/` (full genome output), or
- `./<name>/tokens.json` + `./<name>/tailwind.config.ts` (tokens-only output)

Prompt once if unclear: "Which two (or more) output dirs are we merging? Which one is the primary (used for collision resolution)?"

## Merge plan — ask the user for the split

Before editing anything, surface a concrete split proposal. Examples:

- "Stripe colors + Linear typography" → primary = Stripe; take `theme.colors`, `theme.gradients` from Stripe, take `theme.fontFamily`, `theme.fontSize`, `theme.letterSpacing`, `theme.lineHeight` from Linear.
- "Everything from A except motion from B" → primary = A; take `theme.animations`, `theme.transitions`, `theme.keyframes` from B.
- "A's layout + B's components" → primary = A; take spacing + breakpoints + section rhythm from A, copy `components/*` from B.

Print the proposed split as a table before running. User confirms (or redirects), then you execute.

## Execute — produce merged output

1. Create `./merged-<name>/` (or user-supplied output dir).

2. **Merge `genome.json`:**
   - Start from primary genome.
   - Overlay the agreed slices from secondary (secondary wins on overlapping keys within its slice scope only).
   - Merge `colorRelationships` by union, deduplicated.
   - For `styleNarrative`: do NOT concatenate both narratives. Write a fresh reconciled narrative (3-5 sentences) explaining the hybrid character. Example: "Takes Stripe's precise indigo palette and crisp surfaces, paired with Linear's Inter-based typographic rhythm. Result is a financial-grade but developer-native aesthetic — trustworthy, not sterile."
   - For `aestheticFamily`: pick one family that best fits the hybrid, or mark as `hybrid` if neither source fits.

3. **Merge theme files** (`tailwind.config.ts`, `globals.css`):
   - Walk the theme config key by key using the agreed slice plan. Primary provides defaults; secondary overrides only its allocated keys.
   - CSS variables in `globals.css`: same logic applied to `:root { --* }` blocks.

4. **Merge components:**
   - If primary has a component and secondary does not: copy from primary.
   - If both have the same component (e.g., both have `Button.tsx`): prefer primary. Do NOT auto-blend component code — the visual result will look inconsistent. If the user explicitly said "take Button from B", copy B's Button and re-style it to use the merged theme tokens (not B's original tokens).
   - If only secondary has a component the user wants: copy it, then audit class references. Any token name secondary uses that isn't in the merged theme gets remapped to the nearest merged-theme equivalent (e.g., `bg-linear-accent` → `bg-accent`).

5. **Merge semantic tokens** (if either source has `semantic-tokens.json`):
   - Primitive layer: union of both primitive color/spacing/typography entries, primary wins on name collisions.
   - Semantic layer: re-map using the merged palette. Don't blindly copy — semantic slot values (`accent`, `surface`, `text-primary`) need to reference the merged primitives, not stale ones from either source.
   - Component layer: regenerate from the merged semantic layer.

6. **Merged `styleNarrative.md`** (if genome output): write the reconciled 3-5 sentence narrative as a standalone file, so later `refine-design` runs have consistent direction.

## Rules

- **Primary always wins on collisions unless the slice plan overrides.** This keeps the hybrid coherent instead of a Frankenstein.
- **Never blend component code line-by-line.** Pick one source per component and re-theme it. Line-blending produces broken TSX.
- **Never invent new token keys** during merge. Use the union of existing keys from both sources; if a key exists in only one source and the user's plan doesn't select it, drop it.
- **Re-map token references** in copied secondary components to merged-theme names.
- **Print a merge manifest** after completing: source A provided (list of keys), source B provided (list of keys), collisions resolved to primary (count), components copied from each, narrative rewritten yes/no.

## Anti-patterns

- Do not produce two parallel theme files (`theme-a.css`, `theme-b.css`). The output is one coherent theme.
- Do not ship secondary's `globals.css` import chain unchanged — its `@font-face` or `@layer` directives may conflict with primary's.
- Do not keep both source narratives in the output. Reconcile or the hybrid reads as indecisive.

## When to use

User says: "combine", "merge", "hybrid", "mix", "take X from A and Y from B", "blend these", "what if we used A's colors with B's type", "frankenstein these", "mashup".
