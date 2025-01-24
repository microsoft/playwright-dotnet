using System;
using System.Threading.Tasks;
using Microsoft.Playwright.TestAdapter;
using Xunit.Sdk;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(MyStartupClass))]

public class MyStartupClass : ITestPipelineStartup
{
    public async ValueTask StartAsync(IMessageSink diagnosticMessageSink) 
    {
        throw new NotImplementedException();
        Console.Error.WriteLine("Starting the test pipeline");
        Console.Error.WriteLine("BrowserName: " + PlaywrightSettingsProvider.BrowserName);
        // Read and store settings
    }

    public async ValueTask StopAsync() 
    {
        throw new NotImplementedException();
        // Read and store settings
    }
}