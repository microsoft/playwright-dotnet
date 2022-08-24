# How to Contribute

You can contribute to Playwright for .NET with issues and PRs.

## Contribution Bar

Contributions must meet a certain standard of coding. To ensure this, the Project Maintainers perform regular Code Reviews.
Additionally, a suite of tests runs for each PR.

## DOs and DONT'Ts
Please do:

* **DO** follow our coding style (C# code-specific)
* **DO** include tests when adding new features. When fixing bugs, start with
  adding a test that highlights how the current behavior is broken.
* **DO** keep the discussions focused. When a new or related topic comes up
  it's often better to create new issue than to side track the discussion.
* **DO** blog and tweet (or whatever) about your contributions, frequently!

Please do not:

* **DON'T** make PRs for style changes.
* **DON'T** surprise us with big pull requests. Instead, file an issue and start
  a discussion so we can agree on a direction before you invest a large amount
  of time.
* **DON'T** commit code that you didn't write. If you find code that you think is a good fit to add, file an issue and start a discussion before proceeding.
* **DON'T** submit PRs that alter licensing related files or headers. If you believe there's a problem with them, file an issue and we'll be happy to discuss it.
* **DON'T** add API additions without filing an issue and discussing with us first.

## Breaking Changes

Playwright is evergreen. Breaking Changes _should not_ happen. If they do, they follow a strict process and should come from [upstream](https://github.com/microsoft/playwright).

### Commit Messages

Commit messages should follow the Semantic Commit Messages format:

```
label(namespace): title

description

footer
```

1. *label* is one of the following:
    - `fix` - playwright bug fixes.
    - `feat` - playwright features.
    - `docs` - changes to docs, e.g. `docs(api.md): ..` to change documentation.
    - `test` - changes to playwright tests infrastructure.
    - `devops` - build-related work, e.g. CI related patches and general changes to the browser build infrastructure
    - `chore` - everything that doesn't fall under previous categories
2. *namespace* is put in parenthesis after label and is optional. Must be lowercase.
3. *title* is a brief summary of changes.
4. *description* is **optional**, new-line separated from title and is in present tense.
5. *footer* is **optional**, new-line separated from *description* and contains "fixes" / "references" attribution to github issues.

Example:

```
fix(firefox): make sure session cookies work

This patch fixes session cookies in firefox browser.

Fixes #123, fixes #234
```

## PR Feedback

Microsoft team and community members will provide feedback on your change. Community feedback is highly valued. You will often see the absence of team feedback if the community has already provided good review feedback.

One or more Microsoft team members will review every PR prior to merge. They will often reply with "LGTM, modulo comments". That means that the PR will be merged once the feedback is resolved. "LGTM" == "looks good to me".

There are lots of thoughts and [approaches](https://github.com/antlr/antlr4-cpp/blob/master/CONTRIBUTING.md#emoji) for how to efficiently discuss changes. It is best to be clear and explicit with your feedback. Please be patient with people who might not understand the finer details about your approach to feedback.

## Development Workflow

### Prerequisites

Before building the solution for the first time, you will need to download the drivers. You can do this by either running commands manually, or by using the provided script.

#### Initialize

When you get the repo, you need to download the drivers, dotnet format tool, etc. To do this, you can call:

```bash
./build.sh --download-driver
```

#### Dotnet Format

To help with formatting, you can make use of `dotnet format`. All you have to do is run

```bash
dotnet format
```

The resulting code will follow our style guides. This is also enforced in our CI.

## Writing Tests

* Every feature should be accompanied by a test.
* Every public api event/method should be accompanied by a test.

### Running Tests Locally

#### Running tests

Tests can either be executed in their entirety:

```bash
dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj --logger:"console;verbosity=detailed"
```

You can also specify a single test to run:

```bash
dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj --logger:"console;verbosity=detailed" --filter Playwright.Tests.TapTests
```

Additionally, you can use the Test Explorer if you're using Visual Studio.

### Generating the APIs & rolling the driver (upstream)

We use [this](https://github.com/microsoft/playwright/blob/master/utils/doclint/generateDotnetApi.js) generator for the API and [that](https://github.com/microsoft/playwright/blob/master/utils/generate_dotnet_channels.js) for generating the transport channels. Both are located upstream.

To generate the API, identify the upstream driver version from the GitHub Actions logs of the point in time you wish to roll to, and run:

```bash
./build.sh --roll <driver-version>
```

This will re-generate the neccessary files for the new driver version.

### Collecting coverage when running tests

```shell
dotnet tool install -g dotnet-reportgenerator-globaltool

dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj --logger:"console;verbosity=detailed" -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura -p:CoverletOutput="coverage.xml" --filter "Playwright.Tests.Assertions.PageAssertionsTests"
reportgenerator -reports:src/Playwright.Tests/coverage.net6.0.xml -targetdir:coverage-report -reporttypes:HTML
open coverage-report/index.html
```
