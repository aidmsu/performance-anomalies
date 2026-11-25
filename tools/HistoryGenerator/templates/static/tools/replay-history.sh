#!/usr/bin/env bash
# Replays the commit history locally: checks out every commit in a temporary worktree, builds it,
# runs both test projects the same way CI does and appends the durations of each commit to one CSV.
#
#   tools/replay-history.sh [--from REV] [--to REV] [--passes N] [--out DIR]
#
# --from is exclusive (default: the root commit), --to is inclusive (default: HEAD).
# Each pass is recorded as run_attempt N, which also changes the seeded decisions of the code under test.
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
from="$(git -C "$repo_root" rev-list --max-parents=0 HEAD | tail -n 1)"
to="HEAD"
passes=1
out="$repo_root/replay-results"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --from) from="$2"; shift 2 ;;
    --to) to="$2"; shift 2 ;;
    --passes) passes="$2"; shift 2 ;;
    --out) out="$2"; shift 2 ;;
    -h|--help) sed -n '2,9p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

mkdir -p "$out"
out="$(cd "$out" && pwd)"
work="$(mktemp -d "${TMPDIR:-/tmp}/storefront-replay.XXXXXX")"
tree="$work/tree"
cleanup() {
  git -C "$repo_root" worktree remove --force "$tree" >/dev/null 2>&1 || true
  rm -rf "$work"
}
trap cleanup EXIT

git -C "$repo_root" worktree add --detach "$tree" "$to" >/dev/null
commits=()
while IFS= read -r line; do commits+=("$line"); done < <(git -C "$repo_root" rev-list --reverse --first-parent "$from..$to")
merged="$out/durations.csv"
run_id="replay-$(date -u +%Y%m%dT%H%M%SZ)"
echo "Replaying ${#commits[@]} commits x $passes pass(es) into $merged"

for ((pass = 1; pass <= passes; pass++)); do
  n=0
  for sha in "${commits[@]}"; do
    n=$((n + 1))
    git -C "$tree" checkout --quiet --detach "$sha"
    rm -rf "$work/TestResults"
    (
      cd "$tree"
      export GITHUB_SHA="$sha" GITHUB_RUN_ATTEMPT="$pass" GITHUB_RUN_ID="$run_id" GITHUB_EVENT_NAME="replay"
      unset GITHUB_STEP_SUMMARY
      dotnet build PerformanceAnomalies.slnx -c Release --nologo -v quiet -clp:ErrorsOnly >/dev/null
      for project in tests/*/*.csproj; do
        name="$(basename "$project" .csproj)"
        dotnet test "$project" -c Release --no-build --nologo --results-directory "$work/TestResults" \
          --blame-hang-timeout 2m --logger "trx;LogFileName=$name.trx" --logger "console;verbosity=quiet" >/dev/null
      done
      dotnet run --file build/TestResultsSummary.cs -- "$work/TestResults" --output "$work/durations.csv" >/dev/null
    )
    if [[ -f "$merged" ]]; then tail -n +2 "$work/durations.csv" >> "$merged"; else cp "$work/durations.csv" "$merged"; fi
    echo "pass $pass: $n/${#commits[@]} ${sha:0:7}"
  done
done
echo "Done: $merged"
