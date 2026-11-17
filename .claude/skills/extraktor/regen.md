---
name: regen
description: Generate a completely new page from an extracted genome. Uses the genome's components and design language with original content. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Regen — Generate New Pages from a Genome

Compose a new page using a previously extracted genome's components and design language — entirely within your Claude Code session. The user must already have a genome directory (produced by the `genome` skill). If they don't, run the `genome` skill first.

## Execute (subscription path — recommended)

1. Run the prep command (pure file scaffolding — no API key used):
   ```bash
   npx extraktor regen-prep <genome-dir> --prompt "<description>" -o ./new-page
   ```
   Optional: `--sections <list>` (comma-separated section types).

2. Read the output structure:
   - `./new-page/prep.json` — orchestration manifest (includes your prompt)
   - `./new-page/claude-instructions.md` — **exact steps for you to follow**
   - `./new-page/genome.json` — copied from the source genome
   - `./new-page/components/` — drop-in React/Tailwind components (copied from genome)
   - `./new-page/theme/` — `tailwind.config.ts` + `globals.css`
   - `./new-page/component-inventory.md` — table of every available component + its props
   - `./new-page/styleNarrative.md` — aesthetic guardrails from the source
   - `./new-page/target-scaffold/` — Next.js 15 project ready to receive `src/app/page.tsx`

3. Follow `claude-instructions.md`:
   - Copy `components/` and `theme/` into `target-scaffold/src/` (theme → `target-scaffold/src/app/globals.css`; tailwind config → `target-scaffold/tailwind.config.ts`)
   - Compose a new `target-scaffold/src/app/page.tsx` using **only** the components in `component-inventory.md`, imported from `../../components`
   - Write realistic copy matching the user prompt — no lorem ipsum
   - Respect the `styleNarrative.md` aesthetic
   - Add `'use client'` if the page uses framer-motion or component state
   - Keep code formatted and typed

4. After you finish, tell the user:
   ```bash
   cd ./new-page/target-scaffold
   npm install
   npm run dev
   ```
   And print the line: `Regen complete — page at target-scaffold/src/app/page.tsx`.

## Example

```bash
npx extraktor regen-prep ./stripe-genome \
  --prompt "SaaS pricing page for a project management tool called TaskFlow" \
  -o ./taskflow-site
```

Then proceed with step 3 in the session.

## Flags (regen-prep)

`<genome-dir>` (required, must contain `genome.json`), `--prompt <text>` (required), `-o <dir>`, `--sections <types>` (comma-separated list of section types to include).

## Standalone CLI fallback (API key)

For CI or scripted one-shot runs:
```bash
npx extraktor regen <genome-dir> --prompt "..." -o ./new-page --api-key $ANTHROPIC_API_KEY
```
Not needed from a Claude Code session.

## When to use

User says: "build a new page with this genome", "make a site in the style of the extracted one", "regen", "use that design system for a pricing page", "give me a landing page in this aesthetic".

## When the user hasn't extracted a genome yet

Tell them to run the `genome` skill first, then come back with the generated genome directory path.

## Optional — premium taste layer (taste-skill interop)

If the user has installed [Leonxlnx/taste-skill](https://github.com/Leonxlnx/taste-skill) via `npx skills add https://github.com/Leonxlnx/taste-skill` **and** they explicitly ask for it ("regen with taste", "premium regen", "with editorial polish", "make it feel premium") — compose as a second pass, never as a replacement for the extracted genome.

**Trigger**: the user phrase must contain a clear taste opt-in phrase (e.g. "with taste" / "premium" / "less AI-slop" / "polish"). Treat "editorial" / "minimalist" / "brutalist" as routing keywords once taste is opted in, not as opt-in triggers. Mere presence of `~/.claude/skills/taste-skill/` without the phrase → skip this section, the subscription path above is complete on its own.

**Composition order (non-negotiable)**:

1. Run `regen-prep` as documented above. Read `genome.json`, `component-inventory.md`, `styleNarrative.md`.
2. Generate the page **first** against the extracted design — use `genome.theme` (colors, typography, spacing, effects, animations) and `genome.styleNarrative` (the prose aesthetic description). The extracted design wins.
3. Only after the page compiles, pick the matching taste-skill variant by scanning `genome.styleNarrative` and the user phrase:
   - narrative mentions "minimal" / "editorial" / "restrained" → `minimalist-skill`
   - narrative mentions "brutalist" / "neobrutalism" / "raw" → `brutalist-skill`
   - narrative mentions "playful" / "soft" / "handmade" / "organic" → `soft-skill`
   - user reports bland or truncated `regen` output → `output-skill`
   - user said "premium" / none of the above matched → `taste-skill` (the generalist) with dials clamped: `DESIGN_VARIANCE ≤ 4`, `MOTION_INTENSITY ≤ 5`, `VISUAL_DENSITY` inferred from `genome.layout.patterns` (if the array contains `bento` or `grid-dense`, density is high; `single-column` or `centered`, density is low; default medium).
4. Genome tokens are never overridden. If the taste pass rewrote more than 20% of a component or touched any value inside `genome.theme` (colors / typography / spacing / effects / animations), **revert** that change — genome fidelity wins.

**When NOT to layer**: skip the taste pass if `genome.styleNarrative` is empty or shorter than 50 characters (the aesthetic signal is too weak to route safely), or if the user said "faithful", "exact", "match the source", or "1:1".

**If taste-skill is not installed**: silently continue with the subscription path above. Do not prompt the user to install it.
