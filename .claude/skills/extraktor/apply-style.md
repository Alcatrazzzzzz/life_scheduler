---
name: apply-style
description: Apply a website's design style to the user's existing project. Extracts tokens and integrates them into Tailwind, CSS, or component files. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Apply Style

Extract a site's design and apply it to the user's project. Fully API-free — you (Claude) do the merge in-session.

## Steps

1. Extract tokens:
   ```bash
   npx extraktor extract <url> -o /tmp/style-source
   ```

2. Read the generated `tailwind.config.ts` and `variables.css`.

3. Merge the extracted theme into the user's existing Tailwind config:
   - Add colors to `theme.extend.colors`
   - Add fonts to `theme.extend.fontFamily`
   - Add spacing to `theme.extend.spacing`
   - Add shadows/radii under `theme.extend.boxShadow` / `borderRadius` where present

4. If the user has a `globals.css`, append the CSS variables from `variables.css` inside `:root`.

5. Report: which tokens were merged, which conflicted (and how you resolved them), and suggest where to try the new tokens first (e.g. primary buttons, hero section).

## When to use
User says: "make my site look like X", "apply linear.app's style", "steal Stripe's theme", "skin my project with this palette".
