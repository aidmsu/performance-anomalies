#!/usr/bin/env bash
# Downloads the test-result artifacts of every completed CI run of a branch and merges their
# durations.csv files into one CSV.
#
#   tools/collect-ci-results.sh [--repo OWNER/NAME] [--branch master] [--workflow tests.yml]
#                               [--event push] [--out ci-results]
#
# Requires the GitHub CLI (gh), authenticated. Artifacts that were already downloaded are skipped,
# so the script can be re-run while CI is still working through the history.
set -euo pipefail

repo=""
branch="master"
workflow="tests.yml"
event="push"
out="ci-results"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo) repo="$2"; shift 2 ;;
    --branch) branch="$2"; shift 2 ;;
    --workflow) workflow="$2"; shift 2 ;;
    --event) event="$2"; shift 2 ;;
    --out) out="$2"; shift 2 ;;
    -h|--help) sed -n '2,10p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

if [[ -z "$repo" ]]; then
  repo="$(gh repo view --json nameWithOwner --jq .nameWithOwner)"
fi

mkdir -p "$out/runs"
gh run list --repo "$repo" --workflow "$workflow" --branch "$branch" --event "$event" --limit 1000 \
  --json databaseId,headSha,status,conclusion,attempt,createdAt > "$out/runs.json"

total=0
pending=0
while IFS=$'\t' read -r id status; do
  total=$((total + 1))
  if [[ "$status" != "completed" ]]; then
    pending=$((pending + 1))
    continue
  fi
  if [[ ! -d "$out/runs/$id" ]]; then
    if ! gh run download "$id" --repo "$repo" --dir "$out/runs/$id.partial" >/dev/null 2>&1; then
      echo "warning: could not download artifacts of run $id" >&2
      rm -rf "$out/runs/$id.partial"
      continue
    fi
    mv "$out/runs/$id.partial" "$out/runs/$id"
  fi
done < <(gh run list --repo "$repo" --workflow "$workflow" --branch "$branch" --event "$event" --limit 1000 \
           --json databaseId,status --jq '.[] | "\(.databaseId)\t\(.status)"')

merged="$out/durations.csv"
files=()
while IFS= read -r -d '' file; do files+=("$file"); done < <(find "$out/runs" -name durations.csv -print0 | sort -z)
if [[ ${#files[@]} -eq 0 ]]; then
  echo "No durations.csv files downloaded yet ($total runs, $pending not completed)." >&2
  exit 1
fi
awk 'FNR == 1 && NR != 1 { next } { print }' "${files[@]}" > "$merged"

echo "Runs: $total (not completed: $pending). Result files: ${#files[@]}. Rows: $(($(wc -l < "$merged") - 1))."
echo "Merged CSV: $merged"
