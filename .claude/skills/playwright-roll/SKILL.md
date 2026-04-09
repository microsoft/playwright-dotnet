---
name: playwright-roll
description: Roll Playwright .NET to a new version
---

Help the user roll to a new version of Playwright.
../../../ROLLING.md contains general instructions and scripts.
See also [CLAUDE.md](../../../CLAUDE.md) for build, test, and architecture details.

Unless the exact version is specified by the user, you need to find the latest version of the driver.
Check the publish workflow here: https://github.com/microsoft/playwright/actions/workflows/publish_release.yml. The step that builds and publishes the driver contains the exact version you need.

Now, with the driver version known, always start with running the roll script to update the version and regenerate the API to see the state of things.

```bash
./build.sh --roll <driver-version>
```

Afterwards, work through the list of changes that need to be backported.
You can find a list of pull requests that might need to be taking into account in the issue titled "Backport changes". Ignore the items that are already checked off.

Some items may be irrelevant to the .NET implementation - feel free to check with the upstream.

Some items may be connected, for example when the API has changed multiple times. In this case, handle them alltogether, aligning with the latest change. Check upstream to see the latest implementation.

Otherwise, work through items one-by-one.

Rolling includes:
- updating client implementation to match changes in the upstream JS implementation (see ../playwright/packages/playwright-core/src/client)
- adding a couple of new tests to verify new/changed functionality

## Renaming generated types

When the API generator produces an unhelpful name for a return type (e.g. `Bind` instead of `BrowserBindResult`), you can control it by adding a struct alias in the upstream docs.

In the docs markdown file (e.g. `docs/src/api/class-browser.md`), change the return type from `<[Object]>` to `<[Object=DesiredName]>`:

```diff
-- returns: <[Object]>
+- returns: <[Object=BrowserBindResult]>
   - `endpoint` <[string]>
```

The `=Name` syntax sets `structName` on the parsed type, which the .NET generator uses directly as the class name. After making this change, re-run `./build.sh --roll <version>` to regenerate, then update any hand-written implementation code to use the new type name.

## Tips & Tricks
- Project checkouts are in the parent directory (`../`).
- When updating checkboxes, store the issue content into /tmp and edit it there, then update the issue based on the file.
- Use the "gh" cli to interact with GitHub.
