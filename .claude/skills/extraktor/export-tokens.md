---
name: export-tokens
description: Export design tokens in semantic + component layers or Tokens Studio (Figma) format. Works with Claude Code subscription — no API key needed.
apiKeyRequired: false
---

# Export Tokens

Extraktor can emit tokens in three shapes:

1. **Flat** (default `--format tokens`) — one-level `{ name -> value }`, Style Dictionary compatible.
2. **Semantic** (`--format semantic`) — three-layer model: `primitive` / `semantic` / `component`. This is how professional design systems are organised.
3. **Figma / Tokens Studio** (`--format figma`) — JSON importable by the [Tokens Studio for Figma](https://tokens.studio/) plugin.

All three are orthogonal — you can combine them. They are produced from the same underlying `ExtractionResult`, so the numbers line up.

## When to use

User says:
- "export to Figma"
- "give me semantic tokens"
- "tokens studio format"
- "layered tokens"
- "primitive vs semantic vs component"
- "feed this to Figma"
- "three-layer tokens"
- "rename accent / bg / surface" (user is reasoning in semantic-token language)

## Invocation

```bash
# Semantic + component layers (stdlib for downstream tools)
npx extraktor extract https://example.com -o ./tokens --format semantic

# Tokens Studio plugin (Figma)
npx extraktor extract https://example.com -o ./tokens --format figma

# Combine — produces flat + semantic + figma in one run
npx extraktor extract https://example.com -o ./tokens --format tokens semantic figma
```

## Output files

| Format              | File                   | Consumer                                 |
| ------------------- | ---------------------- | ---------------------------------------- |
| `tokens` (flat)     | `tokens.json`          | Style Dictionary, custom scripts         |
| `semantic`          | `semantic-tokens.json` | any tool that understands DTCG-like refs |
| `figma`             | `figma-tokens.json`    | Tokens Studio Figma plugin               |

## The three layers explained

- **Primitive**: raw palette entries (`color.blue-500: #2563eb`). Never referenced directly by components.
- **Semantic**: role-based aliases that point at primitives (`semantic.color.accent -> {color.blue-500}`). Fixed slot list — don't invent new ones.
- **Component**: contextual tokens that point at semantic, never primitive (`component.button.bg -> {semantic.color.accent}`).

Fixed semantic slot list (do not extend):

```
bg, bg-subtle, surface, surface-raised,
text-primary, text-secondary, text-muted,
accent, accent-hover,
border, border-subtle,
success, warning, danger
```

## After export — user guidance

**Tokens Studio plugin** — tell the user:
1. Open Figma, install the "Tokens Studio for Figma" plugin
2. In the plugin: Settings → Import → JSON
3. Paste the contents of `figma-tokens.json` or select the file
4. The `global` set will appear with `color.primitive`, `color.semantic`, `color.component`, and other groups

**Consuming in code**: `semantic-tokens.json` follows a DTCG-style shape. You can feed it directly to Style Dictionary with a custom parser, or traverse the `primitive` / `semantic` / `component` layers in your build script.

## Heuristic mapping — what the tool does

No AI. Pure logic:

1. **Primitives** = deduplicated extracted palette entries, normalised to hex.
2. **Semantic slots** are filled by:
   - Matching usage tags first (`background` → `bg`, `text` → `text-primary`, `accent` → `accent`, `border` → `border`)
   - Falling back to luminance ordering if usage tags are missing (lightest → `bg`, darkest → `text-primary`)
   - Selecting the most saturated non-used colour for `accent`
   - Synthesising `success` / `warning` / `danger` from sensible defaults when the palette has no status colours
3. **Aesthetic family hints** adjust picks: `brutalist` forces near-binary contrast for `accent`; `minimalist` slightly mutes `accent`.
4. **Component layer always references semantic, never primitive.** This is the invariant Tokens Studio and downstream tools rely on.

## Refining a semantic token

If the user says "change accent to purple":
1. Edit the primitive hex the semantic `accent` points at (check the reference string — e.g. `{color.blue-600}`, so edit `primitive.color.blue-600`).
2. Do NOT rename the primitive entry — downstream `semantic.color.accent` and `component.button.bg` still reference it by name.
3. If a different primitive is needed, add a new primitive entry and update the single semantic reference. Components keep working because they point at semantic.

This is the whole point of the three-layer model: one primitive edit cascades cleanly.
