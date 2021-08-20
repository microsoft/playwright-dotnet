# Rolling Playwright-dotnet to the latest Playwright driver

## Pre-requisites
* remove old gen files: `find src/Playwright | grep Generated | xargs rm`
* install dotnet with installer: https://dotnet.microsoft.com/download/dotnet/5.0
* install dotnet format tool: `dotnet tool install -g dotnet-format`

## Rolling
* obstain the commit hash (long form) to where we're rolling to
* from the repo folder:
    ```powershell
    .\Build.ps1 roll <commit hash>
    ```

This will complete the entire process including replacing the version number and downloading drivers.

If you do not have PowerShell installed, you can:

    ```bash
    node utils/doclint/generateDotnetApi.js /Users/aslushnikov/prog/playwright-dotnet/src/Playwright`
    ```

* update the driver version here: https://github.com/microsoft/playwright-dotnet/blob/main/src/Common/Version.props#L5
* download new browsers:

    ```
    dotnet run -p ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- download-drivers --basepath .
    ```