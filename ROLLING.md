# Rolling Playwright-dotnet

`DriverVersion` in `src/Common/Version.props` holds the `microsoft/playwright` commit that the driver, the generated API and the ported code come from. `.upstream/` (gitignored) is a clone of `microsoft/playwright` at that commit: `./build.sh --build-driver` clones it, checks out the commit and builds the driver from it. A roll moves `DriverVersion` to a new commit and ports what changed in between. The full process, for people and agents, is in [.claude/skills/playwright-roll/SKILL.md](./.claude/skills/playwright-roll/SKILL.md).

## Pre-requisites

- .NET SDK 8: https://dotnet.microsoft.com/download/dotnet/8.0
- git, and Node.js 20 or newer with npm

## Rolling

1. **Roll** to a branch (`main`, `release-1.66`), a tag (`v1.66.0`) or a commit of `microsoft/playwright`.

   ```bash
   ./build.sh --roll <ref>
   ```

   This writes the commit and the latest Node.js LTS version to `src/Common/Version.props`, checks the commit out in `.upstream/`, builds the driver from it, generates the API and the transport channels again, and updates the browser versions in the README. The driver reports the version of `packages/playwright-core/package.json` at that commit: `1.66.0` on a release tag, `1.67.0-next` on `main`.

2. **Port** what changed upstream into `src/Playwright/` and `src/Playwright.Tests/`.

   ```bash
   ./build.sh --changes     # the upstream commits to port, since DriverVersion at HEAD
   ```

3. **Check**, then commit and open the PR.

   ```bash
   dotnet build ./src
   BROWSER=chromium dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net8.0
   ```
