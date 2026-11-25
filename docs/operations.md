# Operations

## Running the tests

```bash
dotnet build PerformanceAnomalies.slnx -c Release
dotnet test tests/Storefront.UnitTests/Storefront.UnitTests.csproj -c Release --no-build
dotnet test tests/Storefront.IntegrationTests/Storefront.IntegrationTests.csproj -c Release --no-build
```

## Continuous integration

Every push to `master` runs the test suite on Linux (x64 and arm64), Windows and macOS. Each job uploads
its test results as an artifact.

## Maintenance windows

Maintenance windows are announced one week ahead. During a window the storefront
keeps accepting orders but defers payment capture.
