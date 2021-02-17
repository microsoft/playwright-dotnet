# How to Contribute

If you are interested in contributing to Playwright Sharp, Thank you!

Coding is not for lonely wolves. Welcome to the pack!

The project has a clear roadmap we want to follow. If you want to contribute, ask before submitting a PR. We will analyze if it’s the right moment to implement that feature or not.
If you don’t know what to do, ASK! We have many many things to implement :)

## Code reviews

All submissions, including submissions by project members, require review. We
use GitHub pull requests for this purpose. Consult
[GitHub Help](https://help.github.com/articles/about-pull-requests/) for more
information on pull requests.

## Core Guidelines

The primary goal is to create an API as close as possible to Playwright. A developer should be able to switch easily to Playwright Sharp and vice-versa.

Playwright Sharp should have a .NET/C# flavor.

 * A developer should be able to inject its objects using dependency injection.
 * Getter functions should be expressed as properties.
 * Async suffix should be honored.

Our guide for architecture and code style will by [Microsoft.Extensions.Configuration](https://github.com/dotnet/extensions/tree/master/src/Configuration).

## Code Style

Though this list will change over time, these are the things to consider now:
 * [We are team spaces](https://www.youtube.com/watch?v=SsoOG6ZeyUI).
 * Every public API should have an XML documentation.
 * Try to follow the current style.
 * Don’t reinvent the wheel.

### Dotnet Format

To help with formatting, you can make use of `dotnet format`. All you have to do is run

```powershell
dotnet tool update dotnet-format --add-source https://dotnet.myget.org/F/format/api/v3/index.json -g
```

and then

```powershell
dotnet format
```

and the result should be formatted code according to our style guide.


## Commit Messages

Don’t worry about commit messages or about how many commits your PR has. [Your PR will be squashed](https://help.github.com/articles/about-pull-request-merges/#squash-and-merge-your-pull-request-commits), so the commit message will be set at that time.


## Prerequisites

Before building the solution for the first time, you will need to download the drivers by running the following command in your terminal:

```
dotnet run -p ./src/tools/PlaywrightSharp.Tooling/PlaywrightSharp.Tooling.csproj -- download-drivers --basepath .
```

## Writing Tests

* Every feature should be accompanied by a test.
* Every public api event/method should be accompanied by a test.

### Running Tests Locally

When you run the tests locally for the first time, you might be greeted with the following error message.

This happens because you're missing a certificate. To generate one, you can use the `dotnet dev-certs` tooling.

In your repository root, run the following:

```powershell
dotnet dev-certs https -ep src/PlaywrightSharp.TestServer/testCert.cer
```

You should be all set for running the tests now. You can run them by either executing all of them:

```powershell
dotnet test .\src\PlaywrightSharp.sln
```

or specifying a filter, such as:

```powershell
dotnet test .\src\PlaywrightSharp.sln --filter PlaywrightSharp.Tests.TapTests
```

to narrow down the tests.

Additionally, you can use the Test Explorer if you're using Visual Studio.