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

## Code Style

Though this list will change over time, these are the things to consider now:
 * [We are team spaces](https://www.youtube.com/watch?v=SsoOG6ZeyUI).
 * Every public API should have an XML documentation.
 * Try to follow the current style.
 * Don’t reinvent the wheel.

## Commit Messages

Don’t worry about commit messages or about how many commits your PR has. [Your PR will be squashed](https://help.github.com/articles/about-pull-request-merges/#squash-and-merge-your-pull-request-commits), so the commit message will be set at that time.

## Writing Tests

* Every feature should be accompanied by a test.
* Every public api event/method should be accompanied by a test.
