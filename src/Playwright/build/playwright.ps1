#!/usr/bin/env pwsh

[Reflection.Assembly]::LoadFile("$($PSScriptRoot)/Microsoft.Playwright.dll") | Out-Null
[Microsoft.Playwright.Program]::Main($args)
