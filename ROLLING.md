# Rolling Playwright-dotnet to the latest Playwright driver

* remove old gen files: `find src/Playwright | grep Generated | xargs rm`
* install dotnet with installer: https://dotnet.microsoft.com/download/dotnet/5.0
* install dotnet format tool: `dotnet tool install -g dotnet-format`
* from Playwright repo source, run:
    ```
    node utils/doclint/generateDotnetApi.js /Users/aslushnikov/prog/playwright-dotnet/src/Playwright`
    node utils/generate_dotnet_channels.js /Users/aslushnikov/prog/playwright-dotnet/src/Playwright
    ```
* put a new driver version here: https://github.com/microsoft/playwright-dotnet/blob/main/src/Common/Version.props#L5
* download new browsers:
    ```
    dotnet run -p ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- download-drivers --basepath .
    ```

