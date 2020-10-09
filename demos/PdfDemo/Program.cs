﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp;

namespace PdfDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Installing playwright");
            await Playwright.InstallAsync();
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddDebug();
                builder.AddFilter((f, _) => f == "PlaywrightSharp.Playwright");
            });

            using var playwright = await Playwright.CreateAsync(loggerFactory, debug: "pw:api");
            await using var browser = await playwright.Chromium.LaunchAsync(new LaunchOptions { Headless = true });

            var page = await browser.NewPageAsync();
            Console.WriteLine("Navigating google");
            await page.GoToAsync("http://www.google.com");

            Console.WriteLine("Generating PDF");
            await page.GetPdfAsync(Path.Combine(Directory.GetCurrentDirectory(), "google.pdf"));

            Console.WriteLine("Export completed");
        }
    }
}