#!/usr/bin/env pwsh

[CmdletBinding(PositionalBinding = $false)]
Param(
  [Parameter(ValueFromRemainingArguments = $true)][String[]]$verbs,
  [Switch]$prereqs
)

$upstream_repo_path = Join-Path -Path ".." -ChildPath "playwright"

function Get-Help() {
  Write-Host "Commands:"
  Write-Host "  init                      Performs the initalization of the repository, and installs/downloads"
  Write-Host "                            all the required artifacts (i.e. driver)."
  Write-Host "                            -prereqs   Installs all the required prerequisites and dev certificates."
  Write-Host ""
  Write-Host "  roll <driver-version>     Updates the API version, downloads driver, generates the API, and transport interfaces."
  Write-Host "  driver                    Downloads the driver."
  Write-Host "  help                      Prints this message."
  Write-Host "  wwwroot                   Copies over the wwwroot."
}

function Invoke-Init() {
  if ($prereqs) { Invoke-InstallRequirements }
  Invoke-DownloadDriver
  Invoke-WWWRoot
}

function Invoke-InstallRequirements() {
  Write-Host "🔨 Installing requirements..." -NoNewline
  dotnet tool install --global dotnet-format >$null 2>&1
  Write-Host " ✔ Dotnet tooling" -NoNewline
  Write-Host "`r✅ Finished initializing tooling requirements."
}

function Invoke-DownloadDriver() {
  if ($prereqs) { Invoke-InstallRequirements }
  Write-Host "🚀 Downloading drivers..." -NoNewline
  dotnet run --project ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- download-drivers --basepath .
}

function Invoke-UpdateWWWRoot() {
  Write-Host "🌐 Synchronizing wwwroot folder..."
  $dotnet_wwwroot_path = "./src/Playwright.Tests.TestServer/wwwroot"
  Remove-Item -Path "$dotnet_wwwroot_path" -Recurse -Force
  Copy-Item -Path "$upstream_repo_path/tests/assets" -Destination $dotnet_wwwroot_path -Recurse
}

function Invoke-Roll() {
  $new_driver_version = $verbs[1];
  $package = Get-Content "$upstream_repo_path/package.json" | ConvertFrom-Json
  $upstream_package_version = $package.version

  Write-Host "🚀 Rolling .NET to driver $new_driver_version and upstream version $upstream_package_version"

  # Let's update the project file
  [xml]$version = Get-Content "./src/Common/Version.props"
  $version.Project.PropertyGroup.DriverVersion = $new_driver_version
  $version.Project.PropertyGroup.AssemblyVersion = $package.version.Split("-")[0]
  $version.Save([IO.Path]::Combine($pwd, 'src', 'Common', 'Version.props'))

  Write-Host "🚀 Generating API..."
  node "$upstream_repo_path/utils/doclint/generateDotnetApi.js" "src/Playwright"
  Invoke-DownloadDriver
  Invoke-UpdateWWWRoot
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
  "wwwroot" { Invoke-UpdateWWWRoot }
}