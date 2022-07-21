import fs from 'fs';
import path from 'path';
import childProcess from 'child_process';
import { test as base } from '@playwright/test';
import { XMLParser } from 'fast-xml-parser';

type TestResult = {
  command: string;
  stdout: string;
  stderr: string;
  passed: number;
  failed: number;
  total: number;
  exitCode: number;
}

export const test = base.extend<{
  runTest: (files: Record<string, string>, command: string, env?: NodeJS.ProcessEnv) => Promise<TestResult>;
}>({
  runTest: async ({ }, use, testInfo) => {
    const testResults: TestResult[] = [];
    await use(async (files, command, env) => {
      const testDir = testInfo.outputPath();
      const testClassName = testInfo.titlePath.join(' ').replace(/[^\w]/g, '');
      for (const [fileName, fileContent] of Object.entries(files)) {
        await fs.promises.writeFile(path.join(testDir, fileName), unintentFile(fileContent).replace('<class-name>', testClassName));
      }
      const trxFile = path.join(testDir, 'result.trx');
      command += ` --logger "trx;logfilename=${trxFile}" --logger "console;verbosity=detailed" --filter "${testClassName}" ${path.join('..', '..')}`;
      const cp = childProcess.spawn(command, {
        cwd: testDir,
        shell: true,
        env: {
          ...process.env,
          ...env,
          NODE_OPTIONS: undefined
        },
        stdio: 'pipe',
      });
      if (process.env.PWTEST_DEBUG) {
        cp.stdout.pipe(process.stdout);
        cp.stderr.pipe(process.stderr);
        console.log(`Running: ${command}`);
      }
      let [stdout, stderr] = ['', ''];
      cp.stdout.on('data', (data: Buffer) => stdout += data.toString());
      cp.stderr.on('data', (data: Buffer) => stderr += data.toString());
      const exitCode = await new Promise<number>((resolve, reject) => {
        cp.on('error', reject);
        cp.on('exit', (code) => resolve(code))
      });
      const { passed, failed, total } = await parseTrx(trxFile);
      const testResult: TestResult = {
        command,
        stdout,
        stderr,
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
        if (testResult.stdout) {
          console.log(`Stdout:`);
          console.log(testResult.stdout);
        }
        if (testResult.stderr) {
          console.log(`Stderr:`);
          console.log(testResult.stderr);
        }
        console.log('=========================================');
      }
    }
  }
});

type TrxFile = {
  passed: number;
  failed: number;
  total: number;
}

async function parseTrx(trxFile: string): Promise<TrxFile> {
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

export { expect } from '@playwright/test';
