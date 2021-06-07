[Reflection.Assembly]::LoadFile("$($PSScriptRoot)/Microsoft.Playwright.dll")
[Microsoft.Playwright.Program]::Main($args)
