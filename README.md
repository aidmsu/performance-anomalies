# performance-anomalies: ground truth

This orphan branch holds the answers for the `master` branch of this repository. `master` has a generated
history of 120 commits in which the durations of ordinary xUnit tests change in known ways. Every commit has
its own CI run on four targets (`linux-x64`, `linux-arm64`, `windows-x64`, `macos-arm64`). The answers are
kept here, and not on `master`, so that a service that reads the repository cannot see them.

The branch has no CI workflow; pushing to it never triggers a run.

## Contents

| Path | What it is |
|---|---|
| `GROUND_TRUTH.md` | Readable description of every scenario and its events, with commit numbers and SHAs |
| `ground-truth/events.json` | One row per atomic event: test, target, commit, kind, direction, tier, tolerance window, links |
| `ground-truth/segments.jsonl` | For each test and target: the modelled distribution of every segment of commits |
| `ground-truth/expected-durations.csv` | Model value and realized seeded value (run attempt 1) for every test, target and commit |
| `ground-truth/commits.json` | Commit index → SHA, subject, and the scenario changes each commit carries |
| `ground-truth/scenarios.json` | Scenario catalog with its changes |
| `ground-truth/bad-runners.json` | Runs that simulate a noisy CI machine (all simulated costs ×1.35) |
| `ground-truth/negative-series.json` | (test, target) series that never change; every alert on them is a false positive |
| `measurements/` | Merged `durations.csv` of all CI runs of `master`, plus the validation report |
| `tools/HistoryGenerator` | The generator that renders every commit, the duration model, lint and validation |

## Scoring a detector

- Commit numbers are 1-based positions in `git rev-list --reverse --first-parent 8b937a7..master`.
  Never order commits by date.
- Identify a test by its TRX `testName` (theory arguments included) and evaluate every target separately.
- An event counts as found when the detector reports the same test, target and direction with a change point
  inside `[window_from, window_to]`. Step windows are ±2 commits. Distribution changes (variance, modality,
  tails) get `[commit, commit + 10]`. Gradual changes get the whole ramp plus 5 commits.
- `tier` says how hard an event is: `must` (clearly above noise), `should` (needs statistics), `optional`
  (at or below the noise floor). `must_not` marks explicit no-change points, for example the targets an
  OS-specific change does not affect, unaffected theory cases, lifecycle events, decoy commits and test-scope
  changes reported as product regressions.
- `cause_group` groups events with one root cause (one commit that slows several tests). `link` connects
  fixes, partial fixes, reverts and reintroductions to the change they refer to.
- Runs listed in `bad-runners.json` are noisy-machine runs. A per-test alert there is a false positive; a
  run-level anomaly may be reported.

## Regenerating and validating

```bash
cd tools/HistoryGenerator
dotnet build -c Release
G=bin/Release/net10.0/HistoryGenerator.dll

dotnet $G plan                                   # commit timeline
dotnet $G lint                                   # scenarios, files, model and commit messages are consistent
dotnet $G check --repo <master-checkout> --ref master    # every commit equals its rendering
dotnet $G ground-truth --repo <master-checkout> --ref master --out ../../ground-truth
dotnet $G validate --repo <master-checkout> --ref master --measurements ../../measurements/durations.csv
```

`generate --repo <path>` rebuilds the 120 commits on top of the empty initial commit (HEAD must be that
commit). Doing that for this repository would rewrite `master`. Do it only for a fresh copy, and push the
commits one at a time so that every commit gets its own CI run.
