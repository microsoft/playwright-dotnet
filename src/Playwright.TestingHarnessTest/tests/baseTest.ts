import fs from 'fs';
import http from 'http';
import path from 'path';
import childProcess from 'child_process';
import { test as base, BrowserServer } from '@playwright/test';
import { XMLParser } from 'fast-xml-parser';
import { AddressInfo } from 'net';

type RunResult = {
  command: string;
  rawStdout: string;
  stdout: string;
  stderr: string;
  passed: number;
  failed: number;
  total: number;
  exitCode: number;
}

export const test = base.extend<{
  proxyServer: ProxyServer;
  testMode: 'nunit' | 'mstest' | 'xunit';
  runTest: (files: Record<string, string>, command: string, env?: NodeJS.ProcessEnv) => Promise<RunResult>;
  launchServer: ({ port: number }) => Promise<void>;
}>({
  proxyServer: async ({}, use) => {
    const proxyServer = new ProxyServer();
    await proxyServer.listen();
    await use(proxyServer);
    await proxyServer.stop();
  },
  testMode: null,
  launchServer: async ({ playwright }, use) => {
    const servers: BrowserServer[] = [];
    await use(async ({port}: {port: number}) => {
      servers.push(await playwright.chromium.launchServer({ port }));
    });
    for (const server of servers)
      await server.close();
  },
  runTest: async ({ testMode }, use, testInfo) => {
    const testResults: RunResult[] = [];
    await use(async (files, command, env) => {
      const testDir = testInfo.outputPath();
      const testClassName = testInfo.titlePath.join(' ').replace(/[^\w]/g, '');
      for (const [fileName, fileContent] of Object.entries(files)) {
        await fs.promises.writeFile(path.join(testDir, fileName), unintentFile(fileContent).replaceAll('<class-name>', testClassName));
      }
      const trxFile = path.join(testDir, 'result.trx');
      command += ` --logger "trx;logfilename=${trxFile}" --logger "console;verbosity=detailed" --filter "${testClassName}" ${path.join('..', '..')}`;
      const cp = childProcess.spawn(command, {
        cwd: testDir,
        shell: true,
        env: {
          ...process.env,
          ...env,
          NODE_OPTIONS: undefined,
          TEST_MODE: testMode,
          PWTEST_TEST_DIR: testDir,
        },
        stdio: 'pipe',
      });
      if (process.env.PWTEST_DEBUG) {
        cp.stdout.pipe(process.stdout);
        cp.stderr.pipe(process.stderr);
        console.log(`Running: TEST_MODE=${testMode} PWTEST_TEST_DIR=${testDir} ${command}`);
      }
      let [rawStdout, rawStderr] = ['', ''];
      cp.stdout.on('data', (data: Buffer) => rawStdout += data.toString());
      cp.stderr.on('data', (data: Buffer) => rawStderr += data.toString());
      const exitCode = await new Promise<number>((resolve, reject) => {
        cp.on('error', reject);
        cp.on('exit', (code) => resolve(code))
      });
      const { passed, failed, total } = await parseTrx(trxFile);
      const testResult: RunResult = {
        command,
        rawStdout,
        stdout: extractVstestMessages(rawStdout, 'Standard Output Messages:'),
        stderr: extractVstestMessages(rawStdout, 'Standard Error Messages:'),
        passed,
        failed,
        total,
        exitCode,
      };
      testResults.push(testResult)
      return testResult;
    });
    if (testInfo.status !== 'passed' && testInfo.status !== 'skipped' && !process.env.PWTEST_DEBUG) {
      for (const testResult of testResults) {
        console.log('=========================================');
        console.log(`Command: ${testResult.command}`);
        console.log(`Exit code: ${testResult.exitCode}`);
        if (testResult.rawStdout) {
          console.log(`Stdout:`);
          console.log(testResult.rawStdout);
        }
        console.log('=========================================');
      }
    }
  }
});

async function parseTrx(trxFile: string): Promise<{
  passed: number,
  failed: number,
  total: number,
}> {
  if (!fs.existsSync(trxFile))
    return { failed: 0, passed: 0, total: 0 };
  const parser = new XMLParser({
    ignoreAttributes: false,
  });
  const xmlData = await fs.promises.readFile(trxFile, 'utf8');
  const xmlParsed = parser.parse(xmlData);
  const counters = xmlParsed['TestRun']['ResultSummary']['Counters'];
  return {
    total: parseInt(counters['@_total'], 10),
    passed: parseInt(counters['@_passed'], 10),
    failed: parseInt(counters['@_failed'], 10),
  }
}

function unintentFile(content: string): string {
  const lines = content.split('\n');
  const minIntention = Math.min(...lines.map(line => {
    const match = /^ +/.exec(line);
    return match ? match[0].length : 0;
  }).filter(line => line > 0));
  if (minIntention > 0) {
    lines.forEach((line, index) => {
      lines[index] = line.slice(minIntention);
    });
  }
  return lines.join('\n').trim();
}

// https://github.com/microsoft/vstest/blob/200a783858425e5ac6f4ebb8f87a7811b0ad39e3/src/vstest.console/Internal/ConsoleLogger.cs#L319-L343
function extractVstestMessages(stdout: string, prefix: string): string {
  const matches = stdout.matchAll(new RegExp(`  ${prefix}\n(.*?)\n\n`, 'gs'));
  let out = '';
  for (const match of matches) {
    out += match[1];
  }
  return out;
}

class ProxyServer {
  private _server: http.Server;
  public requests: { url: string, auth: string}[] = [];

  constructor() {
    this._server = http.createServer(this.handler.bind(this));
  }

  handler(req: http.IncomingMessage, res: http.ServerResponse) {
    if (req.url.includes('google.com')) // Telemetry requests.
      return;
    if (!req.headers['proxy-authorization']) {
      res.writeHead(407, { 'Proxy-Authenticate': 'Basic realm="Access to internal site"' });
      res.end();
      return;
    }
    const auth = Buffer.from(req.headers['proxy-authorization'].split(' ')[1], 'base64').toString();
    this.requests.push({
      url: req.url,
      auth: auth,
    })
    res.writeHead(200);
    res.end('OK');
  }

  listenAddr() {
    return `http://127.0.0.1:${(this._server.address() as AddressInfo).port}`;
  }

  async listen() {
    await new Promise<void>(resolve => this._server.listen(0, resolve));
  }

  async stop() {
    await new Promise<void>(resolve => this._server.close(() => resolve()));
  }
}

export { expect } from '@playwright/test';
