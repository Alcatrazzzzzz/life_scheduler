---
name: clone-site
description: Clone any website into a working Next.js project with images, fonts, videos, and animations. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Clone Site

Clone a website into a complete Next.js project. Default path is fully API-free.

```bash
npx extraktor clone <url> -o ./cloned --no-ai
```

Downloads all assets (images, fonts, videos), preserves animations, creates a runnable Next.js 15 project. No API key used with `--no-ai`.

## AI-enhanced clone (in-session)

For a cleaner React component split, run the `--no-ai` clone first, then ask the user if they want you (Claude, in this session) to refactor the pages into components. You already have everything you need from the cloned source and your subscription auth — no subprocess API call required.

Standalone CLI users on an API key can instead run:
```bash
npx extraktor clone <url> -o ./cloned --api-key $ANTHROPIC_API_KEY
```
but this path is optional and costs API credits.

## After cloning

Tell the user:
```bash
cd ./cloned/<project-name>
npm install
npm run dev
# Visit http://localhost:3000
```

Options: `--content` (extract content.json), `--crawl` (multi-page), `--handover` (zip package), `--devtools` (authenticated Chrome).

## When to use
User says: "clone this site", "make a Next.js project from <url>", "copy this page locally", "rip this site".
