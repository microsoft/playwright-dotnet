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

const path = require('path');

// The API docs and the modules that parse them stay in microsoft/playwright. The
// generator reads both from the same checkout, the first argument of the script.
if (process.argv.length < 4) {
  console.error(`Usage: node ${path.relative(process.cwd(), process.argv[1])} <microsoft/playwright checkout> <output directory>`);
  console.error('For example: node utils/generateDotnetApi.js .upstream src/Playwright');
  process.exit(1);
}

const upstreamDir = path.resolve(process.argv[2]);

module.exports = {
  apiDocsDir: path.join(upstreamDir, 'docs', 'src', 'api'),
  Documentation: require(path.join(upstreamDir, 'utils', 'doclint', 'documentation')),
  parseApi: require(path.join(upstreamDir, 'utils', 'doclint', 'api_parser')).parseApi,
  markdown: require(path.join(upstreamDir, 'utils', 'markdown')),
};
