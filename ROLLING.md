# Rolling Playwright-dotnet to the latest Playwright driver

## Pre-requisites
* remove old gen files: `find src/Playwright | grep Generated | xargs rm`
* install dotnet with installer: https://dotnet.microsoft.com/download/dotnet/5.0
* install dotnet format tool: `dotnet tool install -g dotnet-format`
* install [PowerShell](https://github.com/PowerShell/PowerShell)

## Rolling
* obtain the commit hash (long form) to where we're rolling to
* in the repo folder, run

    ```powershell
    .\Build.ps1 roll <commit hash>
    ```

This will complete the entire process including replacing the version number and downloading drivers.