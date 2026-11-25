# Performance anomalies

`aidmsu/performance-anomalies` is a synthetic .NET 10 benchmark for services that detect performance regressions and improvements from ordinary test durations. Its generated history contains deliberately injected timing changes, with functional tests that pass by design.

**At a glance**

- 120 generated commits on `master` (`commit_index` 1–120) above an empty root (index 0).
- Four targets and about 135 test cases per commit across two xUnit projects.
- One initial push-triggered CI run per generated commit.
- Test durations in TRX, JUnit XML, and `durations.csv` artifacts.
- Answers on the separate `ground-truth` branch.

## How the measurements are produced

The fictional storefront simulates database, storage, network, and compute costs through primitives in `src/Storefront/Infrastructure/` that end in `Workload`. Each simulated cost has small multiplicative jitter, and some runs simulate a noisy CI machine. With `GITHUB_SHA` set, `Workload.Random(key)` seeds decisions from the key, commit, target, and run attempt. Repeating those inputs reproduces the decisions. See `src/Storefront/Infrastructure/Workload.cs` for details.

The unit and integration projects use xUnit 2.9.3 on VSTest with `Microsoft.NET.Test.Sdk` and `JunitXml.TestLogger`. Before the first test, `WarmedUpTestFramework` uses `RuntimeHelpers.PrepareMethod` to precompile all eligible methods in the product and test assemblies and warms up common xUnit assertions, culture data, and serializers to keep first-call JIT costs outside the measured tests. The runner sets `maxParallelThreads` to `2` and `methodDisplay` to `classAndMethod`. Tests assert functional behavior only, so the injected timing changes leave assertions passing.

The `tests` workflow runs on pushes to `master`, pull requests, and manual dispatch. Each run has four jobs.

| Target | GitHub-hosted runner |
| --- | --- |
| `linux-x64` | `ubuntu-24.04` |
| `linux-arm64` | `ubuntu-24.04-arm` |
| `windows-x64` | `windows-2025` |
| `macos-arm64` | `macos-15` |

Each job checks out the full history, installs the SDK from `global.json`, builds in Release, and runs unit tests followed by integration tests with `--no-build`, a two-minute hang timeout, and TRX, JUnit, and console loggers. The console log shows each test and its duration. `build/TestResultsSummary.cs` reads the TRX files, writes `durations.csv`, and adds a Markdown table to the job summary. The job uploads `TestResults/` (`*.trx`, `*.junit.xml`, and `durations.csv`) as `test-results-<target>-a<run_attempt>` with 90-day retention.

## Get the data

**Collect CI artifacts.** From the repository root, use an authenticated GitHub CLI account with access to the workflow runs.

```bash
bash tools/collect-ci-results.sh --repo aidmsu/performance-anomalies
```

The per-commit series uses only runs with `event=push` on `master`. Runs from `pull_request` or `workflow_dispatch` are extra data. The collector defaults to `--event push` and merges available CSVs from completed runs into `ci-results/durations.csv`. It reuses artifacts from previously downloaded run IDs. `--repo` defaults to the checkout's GitHub repository, `--branch` to `master`, `--workflow` to `tests.yml`, and `--out` to `ci-results`.

**Replay the history locally.** Requires Bash 3.2 or newer and the SDK from `global.json`. Check out the generated history with its full ancestry.

```bash
bash tools/replay-history.sh
```

Replay creates a temporary worktree, builds each commit in Release, and runs the two projects serially with TRX and quiet console logging. For each commit, replay exports `GITHUB_SHA` and `GITHUB_RUN_ATTEMPT` (the pass number), so for the same commit, target, and attempt its seeded decisions match CI and only the simulated jitter varies. It writes local measurements to `replay-results/durations.csv`. `--from` is exclusive and defaults to the root commit. `--to` is inclusive and defaults to `HEAD`. `--passes N` runs the range N times and records the pass number as `run_attempt`, starting at 1. `--out` sets the output directory. Replay appends to an existing CSV, so use a fresh output directory for an independent replay.

**Inspect individual runs.** Open [Tests workflow runs](https://github.com/aidmsu/performance-anomalies/actions/workflows/tests.yml) and select a target job to see the duration table in its summary and the per-test console output in its logs.

### More samples per commit

Use `gh run rerun <run-id>` to create attempts 2, 3, and so on under the same run ID. Each attempt uploads a separately named artifact per target with the suffix `-a<attempt>`. The new `run_attempt` changes the seeded decisions. GitHub allows reruns for up to 30 days after the original run (see [GitHub's rerun documentation](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/re-run-workflows-and-jobs?tool=cli)).

Use a fresh `--out` directory to collect attempts added after a run ID was already downloaded.

## Read `durations.csv`

The .NET 10 file-based summary app emits one row per TRX test case result, using RFC 4180 field quoting and UTF-8 with LF line endings. CI metadata comes from the job environment where available. Some fields may be empty in local runs.

| Column | Meaning |
| --- | --- |
| `commit_sha` | Commit SHA from `GITHUB_SHA` or the checked-out commit. |
| `commit_index` | Reachable commit count minus one. The empty root is index 0 and the generated commits are 1–120. Blank if the count is unavailable. |
| `ref` | GitHub ref name or the local checkout's ref. |
| `event` | Event name, such as `push`, `replay`, or `local`. |
| `run_id` | Run identifier. An ordinary local run uses `local`. |
| `run_attempt` | Run attempt, defaulting to `1`. Replay uses the pass number. |
| `job` | GitHub Actions job identifier, if set. |
| `target` | OS and .NET process architecture, such as `linux-x64`. |
| `os` | Runtime-reported OS description. |
| `arch` | Runtime-reported OS architecture in lowercase. |
| `image_os` | Runner image OS identifier from `ImageOS`, if set. |
| `image_version` | Runner image version from `ImageVersion`, if set. |
| `runner_name` | Runner name or local machine name. |
| `cpu_model` | CPU model when available. |
| `logical_cores` | Logical processor count available to the summary process. |
| `sdk` | Version reported by `dotnet --version`. |
| `runtime` | .NET runtime description of the summary process. |
| `project` | Test assembly name or the TRX filename stem if the assembly is unavailable. |
| `test` | Full TRX test name, including arguments for theory cases. |
| `method_fqn` | Fully qualified method name before the first `(` in `test`. |
| `args` | Argument text inside the parentheses, empty for a name without arguments. |
| `outcome` | `Passed`, or `NotExecuted` for a skipped test. Tests pass by design. |
| `duration_ms` | TRX duration in milliseconds, with up to three fractional digits. |
| `start_time` | Start timestamp as recorded in TRX. |
| `end_time` | End timestamp as recorded in TRX. |

Identify a series by the full TRX name (`test`) and `target`. Theory case names have the form `Namespace.Class.Method(param: value)`. Durations differ by target by design. Sort measurements by `commit_index` or first-parent commit order. Do not use commit dates. With the generated tip checked out, list commits oldest first from the repository root.

```bash
git rev-list --reverse --first-parent "$(git rev-list --max-parents=0 HEAD)..HEAD"
```

## Ground truth

Keep the detector's input on `master` and consult the [ground-truth branch](https://github.com/aidmsu/performance-anomalies/tree/ground-truth) after collecting its detections. The orphan branch has no CI workflow.

- The branch's `README.md` explains scoring, including tolerance windows, tiers, `must_not`, `cause_group`, event links, and bad runners.
- `GROUND_TRUTH.md` describes the individual scenarios.
- `ground-truth/events.json` contains atomic events, tiers, and tolerance windows.
- `ground-truth/segments.jsonl`, `ground-truth/expected-durations.csv`, `ground-truth/commits.json`, `ground-truth/scenarios.json`, `ground-truth/bad-runners.json`, and `ground-truth/negative-series.json` accompany the event list.
- The branch also contains merged CI measurements and the `tools/HistoryGenerator` generator. It renders commits from scenario definitions and supports linting, checking, regenerating, and validating measurements.

Match detections to the corresponding events within their tolerance windows, report results by tier, and count detections covered by `must_not` as false positives.

If `origin` has the branch and you do not already have it checked out, use a separate worktree to keep the benchmark checkout intact.

```bash
git fetch origin ground-truth && git worktree add ../performance-anomalies-ground-truth ground-truth
```

## Repository layout

| Path | Contents |
| --- | --- |
| `src/Storefront/` | Fictional storefront library, grouped by business area, with simulated costs in `Infrastructure/`. |
| `tests/Storefront.UnitTests/` | Unit tests. |
| `tests/Storefront.IntegrationTests/` | Integration tests. |
| `build/TestResultsSummary.cs` | TRX-to-CSV conversion and job summary. |
| `tools/` | CI artifact collection and local history replay. |
| `docs/` | Fictional storefront domain and architecture documentation. |
| `.github/workflows/tests.yml` | CI matrix and test commands. |
| `global.json`, `Directory.Build.props`, `Directory.Packages.props` | SDK pin, shared build properties, and central package versions. |

## Build and test

Install .NET SDK `10.0.101` or a later patch accepted by `global.json` (`latestPatch`). Run from the repository root in a Bash-compatible shell.

```bash
dotnet build PerformanceAnomalies.slnx -c Release

dotnet test tests/Storefront.UnitTests/Storefront.UnitTests.csproj -c Release --no-build \
  --results-directory TestResults --blame-hang-timeout 2m \
  --logger "trx;LogFileName=Storefront.UnitTests.trx" \
  --logger "junit;LogFileName=Storefront.UnitTests.junit.xml" \
  --logger "console;verbosity=normal"

dotnet test tests/Storefront.IntegrationTests/Storefront.IntegrationTests.csproj -c Release --no-build \
  --results-directory TestResults --blame-hang-timeout 2m \
  --logger "trx;LogFileName=Storefront.IntegrationTests.trx" \
  --logger "junit;LogFileName=Storefront.IntegrationTests.junit.xml" \
  --logger "console;verbosity=normal"

dotnet run --file build/TestResultsSummary.cs -- TestResults
```

For a quick development pass, run one test class.

```bash
dotnet test tests/Storefront.UnitTests/Storefront.UnitTests.csproj -c Release --filter "FullyQualifiedName~MoneyTests"
```

Do not commit to `master`, amend its commits, rebase it, or force-push to it. The generated history is the benchmark input. To change the benchmark, update the generator on the `ground-truth` branch and generate a new history in a fresh repository.
