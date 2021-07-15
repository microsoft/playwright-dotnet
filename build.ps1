[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch][Alias('a')]$api,
  [switch]$certs,
  [switch][Alias('d')]$driver,
  [switch][Alias('r')]$restore,
  [switch][Alias('b')]$build,
  [switch][Alias('e')]$all,
  [switch][Alias('t')]$test,
  [switch][Alias('h')]$help,
  [switch]$prereqs,
  [ValidateSet("Debug", "Release")][Alias('c')][string]$configuration = "Debug",
  [ValidateSet("net5.0", "netcoreapp3.1")][Alias('f')][string]$framework = "net5.0",
  [int]$workers = 6,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Get-Help() 
{
  Write-Host "Actions (defaults to -help):"
  Write-Host "  -api                    Performs the API generation using the currently checked out submodule."
  Write-Host "  -certs                  Installs the https certificates for localhost tests."
  Write-Host "  -driver                 Downloads the drivers."
  Write-Host "  -restore                Performs a dotnet restore command."
  Write-Host "  -build                  Builds the project."
  Write-Host "  -prereqs                Installs the pre-requisites needed to build this project."
  Write-Host "  -prereqs                Installs the prerequisites needed to build this project."
  Write-Host "  -test                   Runs the test suite with the default browser."
  Write-Host "                          You can specify the number of workers to use, by passing"
  Write-Host "                          the -workers parameter."
  Write-Host "  -all                    Same as calling with -driver -restore -certs -build"
  Write-Host "  -help                   Prints this message."
  Write-Host " "
  Write-Host " "
  Write-Host "Common settings:"
  Write-Host "  -configuration (-c)     Build configuration: Debug (default) or Release."
  Write-Host "  -framework (-f)         The .NET framework to use, either net5.0 (default) or netcoreapp3.1."
  Write-Host ""
}

$exec = "";

foreach ($argument in $PSBoundParameters.Keys)
{
  $exec = "";
  switch($argument) 
  {
    "help"      { Get-Help; return; }
    "driver"    { $exec += "dotnet run -p ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- download-drivers --basepath ." }
    "api"       { $exec += "node ""playwright/utils/doclint/generateDotnetApi.js"" ""src/Playwright""" }
    "certs"     { $exec += "dotnet dev-certs https -ep src/Playwright.Tests.TestServer/testCert.cer" }
    "restore"   { $exec += "dotnet restore ./src/Playwright.sln" }
    "build"     { $exec += "dotnet build --configuration " + "--configuration " + $configuration + " --framework " + $framework + " ./src/Playwright.sln" }
    "all"       { $exec += ".\build.ps1 -driver -restore -certs -build" }
    "test"      { $exec += "dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c " + $configuration + " -f " + $framework +  " " + $properties + " -- NUnit.NumberOfTestWorkers=" + $workers}
    "prereqs"   { Invoke-Expression "& dotnet tool install --global dotnet-format"; $exec += ".\build.ps1 -certs"; }
  }

  if($exec) 
  {
    Write-Host "Executing: " $exec
    Invoke-Expression "& $exec"
  }
}

if (!$exec) 
{
  Get-Help
}