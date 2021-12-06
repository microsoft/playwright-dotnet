# Rolling Playwright-dotnet to the latest Playwright driver

## Pre-requisites

* install .NET SDK 5 with installer: https://dotnet.microsoft.com/download/dotnet/5.0
* install [PowerShell](https://github.com/PowerShell/PowerShell)

## Rolling

1. Checkout the upstream main/release branch of the `playwright` repository.
1. Checkout the main/release branch of the `playwright-dotnet` repository.
1. Pick the latest driver from GitHub Action where you want to roll to. For releases, it should be `v1.X.Y`. For main branch, it should be something like `v1.X.Y-<timestamp>`.
1. By default it will pick `playwright` from `../playwright`. You can override it by setting the `PW_SRC_DIR` environment variable.

```powershell
./build.ps1 roll <driver-version>
```

This will complete the entire process (downnload and set the new driver, re-generate API and transport channels, update the README).
