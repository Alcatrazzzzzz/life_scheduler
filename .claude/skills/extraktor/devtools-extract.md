---
name: devtools-extract
description: Extract design from authenticated pages (dashboards, admin panels) by connecting to the user's existing Chrome session via DevTools Protocol. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# DevTools Extract

Extract from pages the user is logged into by connecting to their Chrome. Pure browser automation, no AI calls for the extract itself.

## Setup

Tell the user to start Chrome with remote debugging:
```bash
# macOS
/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --remote-debugging-port=9222

# Linux
google-chrome --remote-debugging-port=9222
```

Then they log into the site they want to extract from.

## Run with `--devtools`

```bash
npx extraktor extract <url> --devtools
npx extraktor clone <url> --devtools --no-ai
npx extraktor genome-prep <url> --devtools    # prep-only, API-free; then Claude does vision in-session
```

Connects to the running Chrome via CDP. Cookies, sessions, and authentication are preserved. The user's browser stays running after extraction.

Custom endpoint: `--devtools http://localhost:9333`

## When to use
User says: "extract from my dashboard", "behind a login", "authenticated page", "my admin panel", "I'm logged into X, pull its design".
