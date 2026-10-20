---
name: playwright-dotnet-release
description: Prepare a Playwright .NET release after the rolling PR has merged — mark the version, cut the release branch, add .NET-specific changes to the release notes, draft the GitHub release, and tick the .NET boxes in the internal checklist.
---

Use this skill once the roll PR has merged into `main` and the upstream JS `v1.X.0` is published. The rolling work itself is covered by the [[playwright-roll]] skill.

Throughout this doc, replace `X` with the minor version (e.g. `64` for `1.64.0`) and `<user>` with the fork owner (`gh api user --jq .login`).

The full release checklist lives in the private `microsoft/playwright-internal` repo as the `v1.X release checklist` issue. Find its number once:

```bash
unset GITHUB_TOKEN
ISSUE=$(gh search issues --repo microsoft/playwright-internal "v1.X release checklist" --json number --jq '.[0].number')
```

Tick each .NET box incrementally (one PATCH per item) so the issue reflects accurate state if the flow is interrupted. The PR links in a new checklist are copied from the previous release, so replace them as you go:

```bash
gh api repos/microsoft/playwright-internal/issues/$ISSUE --jq '.body' > /tmp/body.md
# edit /tmp/body.md to flip "- [ ]" → "- [x]" on the relevant .NET item
gh api repos/microsoft/playwright-internal/issues/$ISSUE -X PATCH --field body=@/tmp/body.md
```

## 1. Mark the version on `main`

Set `AssemblyVersion` in `src/Common/Version.props` to `1.X.0`, dropping any suffix. `PackageVersion` stays `$(AssemblyVersion)`. Then PR it to `main`:

```bash
git fetch upstream main
git checkout -b mark-v1.X.0 upstream/main
# edit src/Common/Version.props: <AssemblyVersion>1.X.0</AssemblyVersion>
git commit -am "chore: mark v1.X.0"
git push -u origin mark-v1.X.0
gh pr create --repo microsoft/playwright-dotnet --head <user>:mark-v1.X.0 --base main \
  --title "chore: mark v1.X.0" \
  --body "Updates \`AssemblyVersion\` to \`1.X.0\` for the v1.X release."
```

The diff is exactly one line in `Version.props` (e.g. microsoft/playwright-dotnet#3382). Anything else is a red flag.

## 2. Cut the release branch

Once the mark PR has merged, push `release-1.X` from `upstream/main`, then confirm it carries the new version:

```bash
git fetch upstream main
git push upstream upstream/main:refs/heads/release-1.X
git show upstream/release-1.X:src/Common/Version.props | grep AssemblyVersion
```

## 3. Release notes

The .NET notes in `docs/src/release-notes-csharp.md` are written upstream, together with the Node.js notes. The `playwright-release-notes` skill in `../playwright/.claude/skills/playwright-release-notes/SKILL.md` covers that, including the .NET code style. Check that the `## Version 1.X` section is on the release branch:

```bash
git -C ../playwright fetch upstream release-1.X
git -C ../playwright show upstream/release-1.X:docs/src/release-notes-csharp.md | grep -n '^## Version 1.X$'
```

If it's missing, tell the user: it should have landed with the Node.js notes.

### 3a. Add .NET-specific changes

List the user-visible changes in this repo since the previous release branch was cut:

```bash
git log --oneline --no-merges $(git merge-base upstream/release-1.<X-1> upstream/main)..upstream/main | grep -E '^\w+ (feat|fix)'
```

Pick only significant changes that the .NET port alone has, such as a new feature in the NUnit, MSTest or xUnit integration, a .NET-only API, or changed .NET requirements. Leave out minor bug fixes, internal changes and anything the notes already cover. If nothing is significant, leave the notes as they are and move on.

List the candidates for the user and ask which to add. Add them to the section in the style of the existing notes. Then send an upstream PR titled `docs: release notes for v1.X .NET`. Once it merges, it needs a cherry-pick to `release-1.X` (the upstream `playwright-cherry-pick` skill). Tick the .NET "write release notes" box with the PR link, or with the PR that added the section when there was nothing to add.

### 3b. Write the GitHub release body

Convert the `## Version 1.X` section of `upstream/release-1.X:docs/src/release-notes-csharp.md` to GitHub markdown with .NET link text, and save it as `/tmp/v1.X.0-release-notes.md`. Follow step 3 of the upstream `playwright-release-notes` skill, including its link check and the .NET scan. If the conversion shows a problem in the notes themselves, fix them upstream as well.

Create the draft directly against `release-1.X`. Drafting against `main` and retargeting later is fragile, because every `gh release edit` rotates the `untagged-<hash>` ID:

```bash
gh release create v1.X.0 --repo microsoft/playwright-dotnet --draft \
  --title "v1.X.0" --notes-file /tmp/v1.X.0-release-notes.md --target release-1.X
```

## 4. Publish

The user publishes the draft release manually. Publishing creates the `v1.X.0` tag, which triggers the Azure DevOps pipelines:
- `.azure-pipelines/publish.yml` pushes the packages to NuGet.
- `.azure-pipelines/publish-docker.yml` pushes the Docker images.

The checklist links both pipelines. Tick its boxes once they succeed.
