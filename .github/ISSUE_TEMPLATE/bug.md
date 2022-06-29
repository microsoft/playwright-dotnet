---
name: Bug Report
about: Something doesn't work like it should? Tell us!
title: "[BUG]"
labels: ''
assignees: ''

---

**Context:**
- Playwright Version: [what Playwright version do you use?]
- Operating System: [e.g. Windows, Linux or Mac]
- .NET version: [e.g. .NET 6]
- Browser: [e.g. All, Chromium, Firefox, WebKit]
- Extra: [any specific details about your environment]

**Code Snippet**

Help us help you! Put down a short code snippet that illustrates your bug and
that we can run and debug locally. For example:

```cs
using System.Threading.Tasks;
using Microsoft.Playwright;

class Program
{
    public static async Task Main()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        // ...
    }
}
```

**Describe the bug**

Add any other details about the problem here.
