# CLAUDE.md

This synthetic .NET 10 benchmark evaluates detection of performance regressions and improvements from ordinary test durations. Treat `master` as a frozen, generated history.

## Commands

Run from the repository root with the SDK selected by `global.json`. Build once, then run the test projects sequentially in CI order.

```bash
dotnet build PerformanceAnomalies.slnx -c Release
```

```bash
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
```

Run one test class for a quick local check.

```bash
dotnet test tests/Storefront.UnitTests/Storefront.UnitTests.csproj -c Release --filter "FullyQualifiedName~MoneyTests"
```

Turn the TRX files into `TestResults/durations.csv`. Set `GITHUB_STEP_SUMMARY` to write the Markdown job summary.

```bash
dotnet run --file build/TestResultsSummary.cs -- TestResults
```

Replay through the current checkout's `HEAD`. Requires Bash 3.2+ and full history. Replay appends to its CSV, so use a fresh output directory for a fresh measurement set.

```bash
bash tools/replay-history.sh --out replay-results
```

Collect GitHub CI measurements with an authenticated GitHub CLI.

```bash
bash tools/collect-ci-results.sh --repo aidmsu/performance-anomalies --branch master
```

## Repository map

- Find domain code by business area in `src/Storefront/` and simulated costs in `src/Storefront/Infrastructure/`.
- Find xUnit tests and their warm-up frameworks in `tests/Storefront.UnitTests/` and `tests/Storefront.IntegrationTests/`.
- Check `.github/workflows/tests.yml` for the target matrix, test flags, and artifact names.
- Check `build/TestResultsSummary.cs` for TRX parsing and CSV fields. Find capture scripts in `tools/`.
- Find domain and architecture notes in `docs/`. Find the history generator and scoring data only on `ground-truth`.

## IMPORTANT

- Assert behaviour only. Keep timing assertions out of tests.
- Route all simulated cost through the `Infrastructure/` cost primitives and `Workload`. Keep `Thread.Sleep`, `Task.Delay`, and real I/O out of product components.
- Never hand-edit or commit to `master`, amend commits, rebase, or force-push its history.
- Keep `.github/workflows/tests.yml`, `global.json`, `Directory.*.props`, all `*.csproj` and `xunit.runner.json` files, and `src/Storefront/Infrastructure/` identical across the generated commits.
- Keep test names stable. Renames break duration series.
- Keep ground-truth information off `master`, including commit messages, comments, docs, and test names.
- Keep LF line endings as set by `.gitattributes`.

## Changing the benchmark

Keep `master` frozen. Make all changes to `src/`, `tests/`, CI, or docs in the scenario classes and templates under `tools/HistoryGenerator` on `ground-truth`. Use the separate worktree described in the README.

Build and lint from the `ground-truth` worktree root, then print the timeline and render a snapshot to build and test in a disposable `/tmp/render-60` directory.
Set `BENCHMARK_CHECKOUT` to a fresh clone with a clean working tree and `master` checked out at the benchmark's empty root commit. Generate and check the new history before pushing it one commit at a time.

```bash
dotnet build tools/HistoryGenerator/HistoryGenerator.csproj -c Release
dotnet tools/HistoryGenerator/bin/Release/net10.0/HistoryGenerator.dll lint
dotnet tools/HistoryGenerator/bin/Release/net10.0/HistoryGenerator.dll plan
dotnet tools/HistoryGenerator/bin/Release/net10.0/HistoryGenerator.dll render --commit 60 --out /tmp/render-60
dotnet tools/HistoryGenerator/bin/Release/net10.0/HistoryGenerator.dll generate --repo "$BENCHMARK_CHECKOUT"
dotnet tools/HistoryGenerator/bin/Release/net10.0/HistoryGenerator.dll check --repo "$BENCHMARK_CHECKOUT" --ref master
```

## Measurement gotchas

- Use the full TRX test name, including theory arguments such as `Namespace.Class.Method(param: value)`, together with the target to identify a series.
- Order samples by `commit_index` or by Git first-parent history. Compare durations within a target because timings differ by design across targets.
- Run one test project at a time. Keep each project's logger output in its own named file.
- Do not remove or bypass `WarmedUpTestFramework`. It keeps JIT and first-call costs out of test durations.
- Check `GITHUB_SHA` and `GITHUB_RUN_ATTEMPT` before local runs. With a SHA set, `Workload.Random(key)` uses the key, commit, target, and attempt. Without a SHA it uses `Random.Shared`.
- Pass `LogFileName` to the JUnit logger so it writes under `--results-directory`. Avoid `LogFilePath` for this workflow.
- Use only push runs on `master` for the per-commit series. Pull requests and manual dispatch add runs the collector ignores by default.
- Keep generated output out of commits. Git ignores `TestResults/`, `replay-results/`, and `ci-results/`.
