import type { PlaywrightTestConfig } from '@playwright/test';

const config: PlaywrightTestConfig = {
  testDir: './tests',
  timeout: 2 * 60 * 1_000,
  workers: 1,
  reporter: 'list',
  reportSlowTests: null,
};

export default config;
