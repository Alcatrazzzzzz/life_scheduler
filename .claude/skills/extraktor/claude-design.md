---
name: claude-design
description: Seed a design system in Claude Design from any URL. Produces a paste-ready bundle (code, assets, DESIGN.md) that plugs into Anthropic's Claude Design setup flow. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Seed Claude Design from a URL

Use when the user mentions Claude Design, Anthropic's design tool, or wants to initialize a new Claude Design project with an existing site's aesthetic.

Trigger phrases: "use with claude design", "generate a claude design bundle", "seed claude design from ...", "make a claude design starter", "claude design from <url>".

## Bundle (subscription path — recommended)

1. Run the genome prep (API-free):
   ```bash
   npx extraktor genome-prep <url> -o ./site-cd --claude-design
   ```
   This extracts tokens, screenshots, and DOM, and scaffolds the `site-cd/{site}-claude-design/` folder.

2. Follow the genome skill's in-session flow to fill the components directory from the screenshots + DOM fragments. You (Claude) produce the React + Tailwind component code using your subscription — no subprocess API call.

3. After the components are populated, extraktor's prep step has already laid down the bundle scaffolding:
   - `README-FOR-CLAUDE-DESIGN.md` — upload instructions
   - `code/components/`, `code/theme/`, `code/package.json`, `code/tsconfig.json`
   - `assets/colors.md`, `assets/typography.md`, optional `assets/logo.svg`, optional `assets/fonts/`
   - `DESIGN.md` — compact, imperative, paste-ready into Claude Design chat

## Paste-only briefing

If the user only wants the chat briefing (no code), generate the compact DESIGN.md directly via the `design-md` skill's `--for-claude-design` flag. See `skills/design-md.md` for the prep+Claude-in-session flow.

## Standalone CLI fallback (API key)

Users running extraktor outside Claude Code can still run the all-in-one command:
```bash
npx extraktor genome <url> --claude-design -o ./site-cd --api-key $ANTHROPIC_API_KEY
```
This burns API credits to call Claude Vision from a subprocess — not needed if you are in a Claude Code session.

## After running

Tell the user to:

1. Upload `code/` (or the generated `.zip`) to Claude Design's code-repo ingestion.
2. Upload each file in `assets/` through Claude Design's asset uploader.
3. Paste the contents of `DESIGN.md` into the Claude Design setup chat.
