#!/usr/bin/env bash
# Summarise A/B runs of `claude -p ... --output-format stream-json --verbose`.
#
# Usage (from the repo root):
#   bash docs/kb/_verification/ab/summary.sh                 # all runs
#   bash docs/kb/_verification/ab/summary.sh q2              # files starting with q2
#   bash docs/kb/_verification/ab/summary.sh 'q2-*-v2'       # glob, quoted
#   bash docs/kb/_verification/ab/summary.sh q2 -a           # also print the start of each answer
#
# For each run it prints: cost, turns, time, tools Claude called, tools that were
# refused (permission_denied), and how many gbrain calls actually ran.

dir="$(cd "$(dirname "$0")" && pwd)"
pattern="${1:-}"
show_answer=0
[ "${2:-}" = "-a" ] && show_answer=1

shopt -s nullglob
files=("$dir"/${pattern}*.jsonl)
if [ ${#files[@]} -eq 0 ]; then
  echo "No runs match '${pattern}*.jsonl' in $dir"
  exit 1
fi

for f in "${files[@]}"; do
  name="$(basename "$f" .jsonl)"
  last="$(tail -1 "$f")"

  cost="$(echo "$last" | grep -o '"total_cost_usd":[0-9.]*' | cut -d: -f2)"
  turns="$(echo "$last" | grep -o '"num_turns":[0-9]*' | cut -d: -f2)"
  ms="$(echo "$last" | grep -o '"duration_ms":[0-9]*' | cut -d: -f2)"
  secs=""
  [ -n "$ms" ] && secs="$(awk "BEGIN{printf \"%.1f\", $ms/1000}")"
  [ -n "$cost" ] && cost="$(awk "BEGIN{printf \"%.3f\", $cost}")"

  used="$(grep -o '"type":"tool_use","id":"[^"]*","name":"[^"]*"' "$f" \
    | grep -o '"name":"[^"]*"' | cut -d'"' -f4 | sort | uniq -c \
    | awk '{printf "%s %s, ", $2, $1}' | sed 's/, $//')"
  denied="$(grep -o '"subtype":"permission_denied","tool_name":"[^"]*"' "$f" \
    | cut -d'"' -f8 | sort | uniq -c \
    | awk '{printf "%s %s, ", $2, $1}' | sed 's/, $//')"
  gb_called="$(grep -o '"type":"tool_use","id":"[^"]*","name":"mcp__gbrain__[^"]*"' "$f" | wc -l | tr -d ' ')"
  gb_denied="$(grep -o '"subtype":"permission_denied","tool_name":"mcp__gbrain__[^"]*"' "$f" | wc -l | tr -d ' ')"
  gb_ran=$((gb_called - gb_denied))
  deferred="$(grep -o '"total_deferred_tools":[0-9]*' "$f" | head -1 | cut -d: -f2)"

  echo "== $name"
  [ -n "$deferred" ] && echo "   deferred tools in session: $deferred (full surface ~189)"
  echo "   cost \$${cost:-?} | turns ${turns:-?} | time ${secs:-?} s"
  echo "   tools called : ${used:-none}"
  echo "   refused      : ${denied:-none}"
  echo "   gbrain calls : ${gb_ran} ran, ${gb_denied} refused"

  if [ "$show_answer" -eq 1 ]; then
    echo "   answer (start):"
    echo "$last" | grep -o '"result":"[^"]*' | cut -c11- | head -c 800 | sed 's/\\n/\n/g' | sed 's/^/     /'
    echo
  fi
done
