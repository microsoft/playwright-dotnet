﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace ScreenshotsDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            Console.WriteLine("Navigating microsoft");
            var page = await browser.NewPageAsync();
            await page.GotoAsync("http://www.microsoft.com");

            Console.WriteLine("Taking Screenshot");
            await File.WriteAllBytesAsync(Path.Combine(Directory.GetCurrentDirectory(), "microsoft.png"), await page.ScreenshotAsync());

            Console.WriteLine("Export completed");
        }
    }
}
