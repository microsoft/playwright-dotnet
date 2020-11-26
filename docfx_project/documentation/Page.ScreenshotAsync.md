# How to take screenshots
_Contributors: [Meir Blachman](https://www.github.com/meir017)_

## Problem

You need to take an screenshot of a page.

## Solution

Use `Page.ScreenshotAsync` passing a file path as an argument.

```cs
using var playwright = await Playwright.CreateAsync();

var url = "https://www.somepage.com";
var file = ".\\somepage.jpg";

await using IBrowser browser = await playwright.Chromium.LaunchAsync(headless: false);
var page = await browser.NewPageAsync();

await page.GoToAsync(url);
await page.ScreenshotAsync(path: file);
```