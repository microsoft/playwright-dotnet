# Rolling Playwright-dotnet to the latest Playwright driver

## Pre-requisites

* install .NET SDK 5 with installer: https://dotnet.microsoft.com/download/dotnet/5.0
* install [PowerShell](https://github.com/PowerShell/PowerShell)

## Rolling

1. Checkout the upstream release branch of the `playwright` repository.
1. Checkout the upstream release branch of the `playwright-dotnet` repository.
1. Pick the latest driver from GitHub Action where you want to roll to. Usually from the Node.js release.

```powershell
./build.ps1 roll <driver-version>
```

This will complete the entire process including replacing the version number and downloading drivers.
