#!/usr/bin/env pwsh

$PlaywrightFileName = Join-Path $PSScriptRoot "Microsoft.Playwright.dll"
[Reflection.Assembly]::LoadFile($PlaywrightFileName) | Out-Null
exit [Microsoft.Playwright.Program]::Main($args)
