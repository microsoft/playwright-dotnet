#!/usr/bin/env pwsh

[CmdletBinding(PositionalBinding = $false)]
Param(
  [Parameter(ValueFromRemainingArguments = $true)][String[]]$verbs,
  [Switch]$reset,
  [Switch]$prereqs
)

function Get-Help() {
  Write-Host "Actions (defaults to help):"
  Write-Host "  init                      Performs the initalization of the repository, and installs/downloads"
  Write-Host "                            all the required artifacts (i.e. driver), and initializes the submodule."
  Write-Host "                            -reset     Forces the submodule to be updated."
  Write-Host "                            -prereqs   Installs all the required prerequisites and dev certificates."
  Write-Host ""
  Write-Host "  roll [commit sha]         Moves the submodule to the commit specified, generates the API, and downloads"
  Write-Host "                            downloads the new driver."
  Write-Host "  driver                    Downloads the driver."
  Write-Host "  help                      Prints this message."
  Write-Host "  wwwroot                   Copies over the wwwroot."
}

function Invoke-Init() {
  Invoke-InitializeSubmodule
  if ($prereqs) { Invoke-InstallRequirements }
  Invoke-DownloadDriver
  Invoke-WWWRoot
}

function Get-SubmoduleStatus() {
  $smStatus = git submodule status
  return $smStatus
}

function Invoke-InitializeSubmodule($enableReset = $true) {
  # Check the status of the submodule, if not initialized, let's do that
  if ((Get-SubmoduleStatus).StartsWith("-") -or ($enableReset -and $reset)) {
    Write-Host "🔨 Initializing git submodule..." -NoNewline
    git submodule update --init >$null 2>&1
    if ((Get-SubmoduleStatus).StartsWith("-")) { throw 'Could not initialize git submodule' }
    Write-Host "`r✅ Finished initializing git submodule."
  }
}

function Invoke-InstallRequirements() {
  Write-Host "🔨 Installing requirements..." -NoNewline
  dotnet tool install --global dotnet-format >$null 2>&1
  Write-Host " ✔ Dotnet tooling" -NoNewline
  Write-Host "`r✅ Finished initializing tooling requirements."
}

function Invoke-DownloadDriver() {
  # We need the submodule to be initialized for this, so we're forcing the check
  Invoke-InitializeSubmodule $false
  if ($prereqs) { Invoke-InstallRequirements }
  Write-Host "🚀 Downloading drivers..." -NoNewline
  dotnet run --project ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- download-drivers --basepath .
}

function Invoke-WWWRoot() {
  Write-Host "🌐 Synchronizing wwwroot folder..."
  Remove-Item -Path .\src\Playwright.Tests.TestServer\wwwroot\ -Recurse -ErrorAction Ignore
  Copy-Item -Path .\playwright\tests\assets -Destination .\src\Playwright.Tests.TestServer\wwwroot\ -Recurse
}

function Invoke-Roll() {
  if ($verbs.Length -eq 2) {
    if ((Get-SubmoduleStatus).StartsWith("+")) {
      $decision = $Host.UI.PromptForChoice("Update Submodule",
        "The Submodule is already at a different commit, do you want to still use the new sha?",
        @('&Yes, update', "E&xit"),
        0)
      if ($decision -eq 1) {
        Write-Host "⚠ Stopping roll."
        return;
      }
    }
    else {
      Invoke-InitializeSubmodule
    }

    Push-Location "playwright"
    Write-Host "🚀 Moving submodule to" $verbs[1]
    git fetch
    git checkout $verbs[1]
    $rev = (git show -s --format=%ct HEAD) + '000'
    Pop-Location

    $package = Get-Content "playwright/package.json" | ConvertFrom-Json

    # Let's update the project file
    [xml]$version = Get-Content ".\src\Common\Version.props"
    $version.Project.PropertyGroup.DriverVersion = $package.version
    $version.Project.PropertyGroup.AssemblyVersion = $package.version
    "Updating to " + $package.version
    $version.Save([IO.Path]::Combine($pwd, 'src', 'Common', 'Version.props'))
  } else {
    Invoke-InitializeSubmodule
  }

  Write-Host "🚀 Generating API..."
  node "playwright/utils/doclint/generateDotnetApi.js" "src/Playwright"
  Invoke-DownloadDriver
  Invoke-WWWRoot
}

if ($verbs.Length -eq 0) {
  Get-Help
  return;
}

switch ($verbs[0]) {
  "init" { Invoke-Init }
  "help" { Get-Help }
  "roll" { Invoke-Roll }
  "driver" { Invoke-DownloadDriver }
  "wwwroot" { Invoke-WWWRoot }
}