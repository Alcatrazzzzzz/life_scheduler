---
name: refine-design
description: Iterate on an existing extraktor output (genome, tokens, DESIGN.md, cloned site) based on user feedback. Modifies files in place, preserves consistency. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Refine Design

When the user wants changes to an existing extraktor output, don't re-run from scratch. Read the output files, interpret the feedback, modify only what's needed, and report what changed in a 3-line summary.

## Input check — which output shape?

Infer from the directory layout (or ask if ambiguous):

| Output shape | Tell-tale paths | Primary edit surface |
|---|---|---|
| **Genome** (from `genome-prep` + build) | `./genome-output/output-template/theme/`, `./genome-output/output-template/components/`, `./genome-output/genome.json` | theme files + components |
| **Tokens** (from `extract`) | `./tokens/tailwind.config.ts`, `./tokens/variables.css`, `./tokens/tokens.json`, optionally `./tokens/semantic-tokens.json`, `./tokens/figma-tokens.json` | tokens.json + variables.css |
| **DESIGN.md** (from `design-md`) | a single `DESIGN.md` file (Stitch format) | the markdown directly |
| **Clone** (from `clone`) | `./cloned/<project>/app/`, `./cloned/<project>/components/`, `./cloned/<project>/tailwind.config.ts` | Next.js app tree |

Never guess. If two shapes look equally likely, ask one question: "Are you refining `./genome-output` or the cloned `./cloned/foo`?"

## Change types — what to read, what to edit

### Color tweaks ("make accent darker / more purple / warmer")

1. Read the shape-specific color source of truth:
   - Genome: `output-template/theme/tailwind.config.ts` + `output-template/theme/globals.css` + `genome.json` `theme.colors` + `genome.json` `colorRelationships`
   - Tokens: `tailwind.config.ts` + `variables.css` + `tokens.json` + (if present) `semantic-tokens.json` and `figma-tokens.json`
   - DESIGN.md: the `## Color Palette` table rows
   - Clone: `tailwind.config.ts` + `app/globals.css` + any `bg-[#...]` inline usages across `components/`
2. Identify which named key (`accent`, `primary`, `brand`, `surface`...) maps to the user's target. If they say "purple-ier accent", the key is `accent`, not `primary`.
3. Update the hex value in every place it appears. **Do not rename the key** — downstream components, stories, and semantic/component token layers reference it by name.
4. If a semantic-tokens.json exists, update the primitive entry. Semantic and component layers use token references (`{color.primitive.blue-500}`) and will resolve automatically.
5. Grep for inline hex overrides (`bg-[#...]`, `text-[#...]`, `border-[#...]`) across components and update matches to the new value.
6. Summary: "Updated `accent` from `#4F46E5` → `#7C3AED`. 4 components still reference by name; 1 inline hex updated. No dev server restart needed (CSS vars hot-reload)."

### Typography changes ("make headings serif / smaller / tighter")

1. Read `theme/tailwind.config.ts` `fontFamily` + `fontSize`, and `theme/globals.css` `@font-face` / `next/font` imports.
2. If changing **family**: update `fontFamily.sans` (or `.display`) in the config, update the `next/font` import or Google Fonts link in globals.css, and audit any component-level `font-serif` / `font-sans` class overrides.
3. If changing **scale**: update `fontSize` entries in the config. Avoid editing individual `text-2xl` classes on components — scale changes should cascade from the config.
4. If changing **tracking/leading**: update `letterSpacing` / `lineHeight` keys; spot-check hero/h1 components.
5. For DESIGN.md: edit the `## Typography` table rows.
6. Summary must note if a font file swap requires a dev server restart (it does for `next/font`).

### Spacing changes ("more whitespace", "tighten up")

1. Identify the layout rhythm: base unit (usually `4` / `8`), and section padding (commonly on `<section>` or a `Container` wrapper).
2. Apply **proportional** adjustments across layout wrappers — do NOT touch every arbitrary gap on every element.
3. Edit targets, in order of effect:
   - `tailwind.config.ts` `spacing` extensions (affects everything)
   - Shared `Container` / `Section` / `Wrapper` component padding classes
   - Top-level page gap classes (`space-y-*` on the root layout)
4. Summary must name the magnitude: "Increased section padding from `py-16` → `py-24` (50% more vertical air). Affects 6 sections."

### Add / remove a component ("add a testimonial section", "remove pricing")

**Remove:**
1. Delete the component file.
2. Remove its import + JSX usage from the parent page (`app/page.tsx` for clone; `stories/*.stories.tsx` for genome output).
3. Rebuild the barrel export `components/index.ts` if one exists.
4. Grep for any stale references; none should remain.

**Add:**
1. Read `genome.json` (or `DESIGN.md`) `styleNarrative` / aesthetic family first — the new component must match.
2. Read 2-3 sibling components (ideally the most visually adjacent — e.g., Card and Feature for a Testimonial) to learn the idiom: class conventions, spacing scale, radius/shadow usage, typography tokens.
3. Write the new component in the same idiom to `components/<PascalName>.tsx`. Use existing tokens, not fresh hex values.
4. Add to the barrel export. Wire into the page where the user indicated (or after the most semantically adjacent section).
5. Summary: "Added `Testimonial.tsx` (matched `Feature.tsx` idiom: `rounded-2xl`, `bg-surface-raised`, serif display heading). Wired below `Features` in `app/page.tsx`."

### Style shift ("make it more editorial / more playful / warmer")

1. Read current aesthetic direction from `genome.styleNarrative` (in `genome.json`) or a standalone `styleNarrative.md` file (if present, e.g. produced by `regen-prep`).
2. Map the user's word to concrete minimum changes:
   - **Editorial**: tighter letter-spacing on display sizes (`-0.02em`), serif body, larger leading (`1.65`), narrower line-length (`max-w-prose`), desaturated accent.
   - **Playful**: increased border-radius (`2xl` → `3xl`), warmer accent, bouncier animation curves (`cubic-bezier(0.34, 1.56, 0.64, 1)`), looser type scale.
   - **Warmer**: shift backgrounds from neutral-cool (`#F8FAFC`) to neutral-warm (`#FAF7F2`), accent toward orange/amber.
   - **Minimalist**: remove decorative shadows, flatten to 1 radius token, reduce palette by 30%.
3. Apply changes in **theme files first** — Tailwind cascades will propagate. Only touch components to fix the 1-2 spots that don't inherit (usually hero overrides).
4. Update `genome.json` `styleNarrative` to reflect the new direction so later refinements stay consistent.
5. Summary: "Shifted from modern-minimal → editorial. Changed: body font → Source Serif, letter-spacing on h1/h2 → -0.02em, accent desaturated 15%. 2 hero overrides adjusted."

### Optional — taste-skill editorial polish

If `~/.claude/skills/taste-skill/redesign-skill/SKILL.md` exists **and** the user's phrasing implies a generic-quality upgrade ("make it feel premium", "less AI-slop", "more polished") — not a targeted tweak — invoke `redesign-skill` as a **second reviewer** after your own style shift pass.

Rules:
- The extracted `styleNarrative` is the source of truth. redesign-skill is a lint pass, not a redesign.
- Only accept its suggestions that map to the audit categories already in this skill (typography, color, spacing, component patterns). Reject any suggestion that introduces a new aesthetic family, renames a token key, or contradicts `genome.styleNarrative`.
- After the second pass, re-print the 3-line summary with prefix: `"Style shift + taste lint —"` so the user knows two passes ran.
- If taste-skill is not installed, silently continue with the 5-step style shift above. Do not prompt the user to install it.

## Rules

- **Never rename** color keys, font keys, or spacing keys. Downstream refs (components, stories, semantic tokens, CSS vars) break silently.
- **Never regenerate** components from scratch for minor changes. Edit in place.
- **Never diverge** from the existing `styleNarrative`. If a change would conflict, update the narrative as part of the refinement.
- **Always print** a 3-line summary after every refinement:
  1. What changed (token names + before → after values).
  2. What downstream files it affects (count + examples).
  3. Whether the dev server needs a restart (font swaps = yes; color/spacing/component edits = no).
- **If the request is too vague** ("make it better", "improve this"), ask 1-2 sharper questions before editing. Good disambiguators: "warmer or cooler?", "tighter or more airy?", "which section specifically?".

## Shape-specific nuances

- **DESIGN.md refinements** are text-only. Keep the Stitch format intact: section headers, table structure, the final "Rationale" blocks. Never change the document outline.
- **Cloned sites** have real Next.js wiring — after editing, verify imports still resolve (`tsc --noEmit`) before declaring done. Dev server restart needed only for font or config changes.
- **Tokens-only outputs** (no components) mean refinements stop at the token files. Don't hallucinate components that don't exist in the output dir.
- **Genome output-template** is scaffolding-plus-filled: some components exist, some are `.template` files waiting to be realized. A refinement might need to realize a template before editing it.

## When to use

User says: "make it X-er", "change the Y to Z", "add/remove/replace", "tweak this", "refine", "iterate", "tune", "adjust", "not quite right", "too much Y", "needs more Z", "what if we tried".
