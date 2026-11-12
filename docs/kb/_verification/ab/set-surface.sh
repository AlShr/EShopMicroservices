#!/usr/bin/env bash
# Re-register the user-scope `gbrain` MCP server with a different tool surface.
# Only the --surface flag changes; the launcher (bun + gbrain cli.ts) stays the same.
#
# Usage (from the repo root):
#   bash docs/kb/_verification/ab/set-surface.sh starter   # memory verbs + ~20 ops
#   bash docs/kb/_verification/ab/set-surface.sh full      # back to the original (no --surface = full)
#
# Takes effect in NEW Claude Code sessions (including every `claude -p` run).
# A backup of ~/.claude.json is written first.

set -euo pipefail

surface="${1:-}"
case "$surface" in
  full|starter|verbs) ;;
  *) echo "usage: $0 full|starter|verbs" >&2; exit 2 ;;
esac

BUN="C:/Users/user/AppData/Local/Microsoft/WinGet/Packages/Oven-sh.Bun_Microsoft.Winget.Source_8wekyb3d8bbwe/bun-windows-x64/bun.exe"
CLI="C:/Users/user/.bun/install/global/node_modules/gbrain/src/cli.ts"

backup="$HOME/.claude.json.bak-$(date +%Y%m%d-%H%M%S)"
cp "$HOME/.claude.json" "$backup"
echo "Backup: $backup"

claude mcp remove gbrain --scope user

if [ "$surface" = "full" ]; then
  claude mcp add gbrain --scope user -- "$BUN" "$CLI" serve
else
  claude mcp add gbrain --scope user -- "$BUN" "$CLI" serve --surface "$surface"
fi

echo
claude mcp get gbrain
