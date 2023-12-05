#!/usr/bin/env pwsh

$Env:PLAYWRIGHT_DRIVER_SEARCH_PATH = $PSScriptRoot;
$playwrightLibrary = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, "Microsoft.Playwright.dll"))
# We load the library via the memory to not keep the .dll file locked/opened.
[Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($playwrightLibrary)) | Out-Null
exit [Microsoft.Playwright.Program]::Main($args)
