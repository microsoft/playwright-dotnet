/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

// Builds the Playwright driver into src/Playwright/.drivers. The driver is the
// playwright-core package of microsoft/playwright at the commit in DriverVersion
// (src/Common/Version.props), plus the official Node.js binaries of
// DriverNodeVersion. This:
//   1. clones microsoft/playwright into .upstream/ (gitignored) the first time,
//      and checks out the commit;
//   2. builds it with `npm ci` and `npm run build` (once per commit);
//   3. packs packages/playwright-core with `npm pack`, and adds the Node.js
//      binary of each platform.
// The resulting directory layout matches the .playwright folder shipped in the
// NuGet package:
//   .drivers/package/**                  - playwright-core npm package contents
//   .drivers/node/LICENSE                - Node.js license
//   .drivers/node/<platform>/node[.exe]  - per-platform Node.js binary.
// A driver that is built is not built again until DriverVersion or
// DriverNodeVersion changes. Do not commit, branch or edit in .upstream/: the
// driver is built only from a checkout with no local changes.
//
// This needs git, npm and tar on PATH, and no `npm install`.

const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

const upstreamUrl = 'https://github.com/microsoft/playwright.git';
const basePath = path.join(__dirname, '..');
const upstreamPath = path.join(basePath, '.upstream');
const isWindows = process.platform === 'win32';

// Directory names under .drivers/node/ are the platform ids that Driver.cs
// resolves at runtime; the suffixes name the Node.js download for that platform.
const platforms = [
  ['darwin-x64', 'darwin-x64'],
  ['darwin-arm64', 'darwin-arm64'],
  ['linux-x64', 'linux-x64'],
  ['linux-arm64', 'linux-arm64'],
  ['win32_x64', 'win-x64'],
];

// Resolves with the exit code and, with `captureOutput`, the standard output.
function run(command, args, options) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd: options.cwd,
      env: options.env,
      shell: options.shell,
      stdio: ['inherit', options.captureOutput ? 'pipe' : 'inherit', 'inherit'],
    });
    let output = '';
    child.stdout?.on('data', data => output += data);
    child.on('error', error => {
      if (error.code === 'ENOENT')
        error = new Error(`${command} was not found on PATH; git, npm and tar are required to build the driver.`, { cause: error });
      reject(error);
    });
    child.on('close', exitCode => resolve({ exitCode, output: output.trim() }));
  });
}

async function git(...args) {
  const { exitCode, output } = await run('git', args, { cwd: upstreamPath, captureOutput: true });
  if (exitCode !== 0)
    throw new Error(`git ${args.join(' ')} failed with exit code ${exitCode}`);
  return output;
}

async function hasCommit(commit) {
  const { exitCode } = await run('git', ['rev-parse', '--verify', '--quiet', commit + '^{commit}'], { cwd: upstreamPath, captureOutput: true });
  return exitCode === 0;
}

async function hasLocalChanges() {
  return (await git('status', '--porcelain')).length > 0;
}

async function npm(cwd, args, env = {}) {
  env = { ...process.env, ...env };
  const npmrc = path.join(basePath, '.npmrc');
  // npm does not run in the repository root, so name the root-level .npmrc: the
  // release pipeline writes the registry and its credentials there, to point npm
  // at the internal Azure Artifacts feed.
  if (fs.existsSync(npmrc))
    env.npm_config_userconfig = npmrc;
  console.log(`Running npm ${args.join(' ')} in ${cwd}`);
  // On Windows npm is npm.cmd, which only a shell can start. The arguments are
  // plain words, so the command line needs no quoting.
  const { exitCode } = isWindows
    ? await run(`npm ${args.join(' ')}`, [], { cwd, env, shell: true })
    : await run('npm', args, { cwd, env });
  if (exitCode !== 0)
    throw new Error(`npm ${args.join(' ')} failed with exit code ${exitCode}`);
}

// Extracts a .tar.gz that is in `directory`, into it. tar is bsdtar on macOS and
// Windows, and GNU tar on Linux and in Git Bash on Windows. GNU tar reads a name with
// a colon, such as C:\driver.tgz, as an archive on another machine, so tar runs in
// the directory and gets names without a path.
// -m gives the files a current modification time. npm packs with an mtime far in the
// past, which breaks "newer file wins" update checks down the line, see
// https://github.com/microsoft/playwright-dotnet/issues/2069.
async function untar(directory, archive, ...args) {
  const { exitCode } = await run('tar', ['-xzmf', archive, '--no-same-owner', ...args], { cwd: directory });
  if (exitCode !== 0)
    throw new Error(`tar failed to extract ${path.join(directory, archive)} with exit code ${exitCode}`);
}

// Checks out DriverVersion in .upstream/, after cloning microsoft/playwright there the first time.
async function checkoutUpstream(driverVersion) {
  let cloned = false;
  if (!fs.existsSync(path.join(upstreamPath, '.git'))) {
    console.log(`Cloning ${upstreamUrl} into ${upstreamPath}`);
    const { exitCode } = await run('git', ['clone', '--no-checkout', upstreamUrl, upstreamPath], { cwd: basePath });
    if (exitCode !== 0)
      throw new Error(`git clone ${upstreamUrl} failed with exit code ${exitCode}`);
    cloned = true;
  }
  if (cloned || await git('rev-parse', 'HEAD') !== driverVersion) {
    if (!cloned && await hasLocalChanges())
      throw new Error(`${upstreamPath} has local changes. Discard them before checking out ${driverVersion}.`);
    if (!await hasCommit(driverVersion))
      await git('fetch', '--quiet', '--tags', 'origin');
    // A commit that no branch or tag reaches any more, for example on a deleted branch.
    if (!await hasCommit(driverVersion))
      await git('fetch', '--quiet', 'origin', driverVersion);
    await git('checkout', '--quiet', '--detach', driverVersion);
  }
  console.log(`${upstreamPath} is at ${driverVersion}`);
}

// Builds .upstream/ and extracts its playwright-core package into .drivers/package.
async function buildPlaywrightPackage(driversDirectory, driverVersion) {
  if (await hasLocalChanges())
    throw new Error(`${upstreamPath} has local changes. The driver is stamped with the commit, so build it from a clean checkout.`);

  const coreDirectory = path.join(upstreamPath, 'packages', 'playwright-core');
  // npm ci removes node_modules, and the stamp with it.
  const buildStamp = path.join(upstreamPath, 'node_modules', '.playwright-dotnet-build');
  const built = fs.existsSync(buildStamp)
    && fs.readFileSync(buildStamp, 'utf8').trim() === driverVersion
    && fs.existsSync(path.join(coreDirectory, 'lib'));
  if (!built) {
    await npm(upstreamPath, ['ci', '--no-audit', '--no-fund']);
    await npm(upstreamPath, ['run', 'build']);
    fs.writeFileSync(buildStamp, driverVersion + '\n');
  }

  // The directory goes in the environment, because its path can have spaces.
  await npm(coreDirectory, ['pack'], { npm_config_pack_destination: driversDirectory });
  const tgzs = fs.readdirSync(driversDirectory).filter(name => /^playwright-core-.*\.tgz$/.test(name));
  if (tgzs.length !== 1)
    throw new Error(`npm pack did not produce one playwright-core-*.tgz in ${driversDirectory}`);
  // npm nests the package contents under a top-level "package/" directory, which is
  // the layout that we want under .drivers/.
  await untar(driversDirectory, tgzs[0]);
  fs.rmSync(path.join(driversDirectory, tgzs[0]));

  // Upstream copies these into the package before it publishes it
  // (LICENCE_FILES in utils/workspace.js).
  for (const name of ['NOTICE', 'LICENSE'])
    fs.copyFileSync(path.join(upstreamPath, name), path.join(driversDirectory, 'package', name));
  console.log(`Built playwright-core at ${driverVersion}`);
}

async function download(url, destination) {
  const maxAttempts = 3;
  for (let attempt = 1; ; attempt++) {
    try {
      console.log(`Downloading ${url}`);
      const response = await fetch(url);
      if (!response.ok)
        throw new Error(`status ${response.status} and content ${await response.text()}`);
      fs.writeFileSync(destination, Buffer.from(await response.arrayBuffer()));
      return;
    } catch (error) {
      if (attempt === maxAttempts)
        throw new Error(`Unable to download ${url}`, { cause: error });
      // fetch() reports "fetch failed", and the reason as the cause.
      const reason = error.cause ? `${error.message}: ${error.cause.message}` : error.message;
      console.log(`Attempt ${attempt} to download ${url} failed: ${reason}, retrying...`);
      await new Promise(resolve => setTimeout(resolve, attempt * 1000));
    }
  }
}

async function downloadNode(driversDirectory, nodeVersion, platformId, nodeSuffix) {
  const nodeDirectory = path.join(driversDirectory, 'node');
  fs.mkdirSync(path.join(nodeDirectory, platformId), { recursive: true });
  const distUrl = `https://nodejs.org/dist/v${nodeVersion}`;
  if (nodeSuffix.startsWith('win-')) {
    // The binary alone, the same one as in the .zip of the platform.
    await download(`${distUrl}/${nodeSuffix}/node.exe`, path.join(nodeDirectory, platformId, 'node.exe'));
  } else {
    const archiveDir = `node-v${nodeVersion}-${nodeSuffix}`;
    const archive = `${archiveDir}.tar.gz`;
    await download(`${distUrl}/${archive}`, path.join(nodeDirectory, archive));
    await untar(nodeDirectory, archive, '-C', platformId, '--strip-components=2', `${archiveDir}/bin/node`);
    // Every Node.js archive carries the same LICENSE; take it from one platform.
    if (platformId === 'linux-x64')
      await untar(nodeDirectory, archive, '--strip-components=1', `${archiveDir}/LICENSE`);
    fs.rmSync(path.join(nodeDirectory, archive));
  }
  console.log(`Extracted Node.js ${nodeVersion} for ${platformId}`);
}

// Copies the browser versions from the README of .upstream/ to the README here.
function updateBrowserVersions() {
  try {
    const readmePath = path.join(basePath, 'README.md');
    const upstreamReadme = fs.readFileSync(path.join(upstreamPath, 'README.md'), 'utf8');
    const versions = new Map();
    for (const [, name, version] of upstreamReadme.matchAll(/<!-- GEN:(.*?) -->(.*?)<!-- GEN:stop -->/g))
      versions.set(name, version);
    const readme = fs.readFileSync(readmePath, 'utf8');
    const newReadme = readme.replace(/<!-- GEN:(.*?) -->.*?<!-- GEN:stop -->/g, (match, name) => {
      return versions.has(name) ? `<!-- GEN:${name} -->${versions.get(name)}<!-- GEN:stop -->` : match;
    });
    if (newReadme !== readme)
      fs.writeFileSync(readmePath, newReadme);
  } catch (error) {
    const [yellow, reset] = process.stdout.isTTY ? ['\x1b[33m', '\x1b[0m'] : ['', ''];
    console.log(`${yellow}WARNING: Could not update the browser versions in the README file.`);
    console.log(`${error.message}${reset}`);
  }
}

async function main() {
  const propsPath = path.join(basePath, 'src', 'Common', 'Version.props');
  const props = fs.readFileSync(propsPath, 'utf8');
  const driverVersion = props.match(/<DriverVersion>(.*?)<\/DriverVersion>/)?.[1].trim() ?? '';
  const nodeVersion = props.match(/<DriverNodeVersion>(.*?)<\/DriverNodeVersion>/)?.[1].trim() ?? '';
  if (!/^[0-9a-f]{40}$/.test(driverVersion))
    throw new Error(`DriverVersion in ${propsPath} must be a microsoft/playwright commit (40 hex digits), got "${driverVersion}"`);
  if (!/^[0-9]+\.[0-9]+\.[0-9]+$/.test(nodeVersion))
    throw new Error(`DriverNodeVersion in ${propsPath} must be an exact Node.js version such as 26.13.1, got "${nodeVersion}"`);

  await checkoutUpstream(driverVersion);

  const driversDirectory = path.join(basePath, 'src', 'Playwright', '.drivers');
  const stamp = `driver ${driverVersion} node ${nodeVersion}`;
  const stampFile = path.join(driversDirectory, '.stamp');
  if (fs.existsSync(stampFile) && fs.readFileSync(stampFile, 'utf8') === stamp) {
    console.log('The driver is up-to-date');
  } else {
    fs.rmSync(driversDirectory, { recursive: true, force: true });
    fs.mkdirSync(driversDirectory, { recursive: true });
    // Wait for all of them also when one fails, to not leave npm running behind.
    const results = await Promise.allSettled([
      buildPlaywrightPackage(driversDirectory, driverVersion),
      ...platforms.map(([platformId, nodeSuffix]) => downloadNode(driversDirectory, nodeVersion, platformId, nodeSuffix)),
    ]);
    for (const result of results) {
      if (result.status === 'rejected')
        throw result.reason;
    }
    fs.writeFileSync(stampFile, stamp);
  }

  updateBrowserVersions();
}

main().catch(error => {
  console.error(error);
  process.exit(1);
});
