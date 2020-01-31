using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    internal static class TestConstants
    {
        public const string TestFixtureCollectionName = "PlaywrightSharpLoaderFixture collection";
        public const int Port = 8081;
        public const int HttpsPort = Port + 1;
        public const string ServerUrl = "http://localhost:8081";
        public const string ServerIpUrl = "http://127.0.0.1:8081";
        public const string HttpsPrefix = "https://localhost:8082";
        public const string AboutBlank = "about:blank";
        public const string CrossProcessHttpPrefix = "http://127.0.0.1:8081";
        public static readonly string EmptyPage = $"{ServerUrl}/empty.html";
        public static readonly string CrossProcessUrl = ServerIpUrl;
        public static readonly string ExtensionPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "simple-extension");
        public static readonly DeviceDescriptor IPhone = null;
        public static ILoggerFactory LoggerFactory { get; private set; }
        public static string FileToUpload => Path.Combine(Directory.GetCurrentDirectory(), "Assets", "file-to-upload.txt");

        //TODO
        internal static bool IsWebKit = false;
        internal static bool IsFirefox = false;
        internal static bool IsChromium = true;

        public static readonly IEnumerable<string> NestedFramesDumpResult = new List<string>()
        {
            "http://localhost:<PORT>/frames/nested-frames.html",
            "    http://localhost:<PORT>/frames/two-frames.html (2frames)",
            "        http://localhost:<PORT>/frames/frame.html (uno)",
            "        http://localhost:<PORT>/frames/frame.html (dos)",
            "    http://localhost:<PORT>/frames/frame.html (aframe)"
        };

        public static void SetupLogging(ITestOutputHelper output)
        {
            if (Debugger.IsAttached && LoggerFactory == null)
            {
                LoggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
            }
        }
    }
}