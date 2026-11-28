# Builds the release zip (Phase 9): dist\HorsePersonalities-<version>.zip
#   scripts\HorsePersonalities.dll
#   scripts\HorsePersonalities\breeds.json, test-personalities.json
#   scripts\HorsePersonalities.ini        (the defaults the mod would write on first run)
#   README.md, LICENSE, CHANGELOG.md
# DevTools is never packed. Builds against lib\ScriptHookRDRNetAPI.dll (from the hook release you play with),
# or pass -Api C:\path\to\ScriptHookRDRNetAPI.dll.
# Run from anywhere:  powershell -ExecutionPolicy Bypass -File tools\make-release.ps1
param([string]$Api = "")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
if ($Api -eq "") { $Api = Join-Path $root "lib\ScriptHookRDRNetAPI.dll" }
if (-not (Test-Path $Api)) { throw "ScriptHookRDRNetAPI.dll not found at $Api (see lib\README.md)" }
$version = ([xml](Get-Content (Join-Path $root "Directory.Build.props"))).Project.PropertyGroup.Version

function Run($exe, [string[]]$argList) {
    & $exe @argList
    if ($LASTEXITCODE -ne 0) { throw "$exe $($argList -join ' ') failed ($LASTEXITCODE)" }
}
Run dotnet @("run", "--project", "tools/sheet-to-json", "--", "--check")
Run dotnet @("test", "tests/HorsePersonalities.Tests", "-nologo", "-v", "q")
Run dotnet @("build", "src/HorsePersonalities.Game", "-c", "Release", "-nologo", "-v", "q", "/p:ShdnApiPath=$Api")

$stage = Join-Path $root "dist\stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $stage "scripts\HorsePersonalities") | Out-Null
Copy-Item "src\HorsePersonalities.Game\bin\Release\HorsePersonalities.dll" (Join-Path $stage "scripts")
Copy-Item "data\breeds.json", "data\test-personalities.json" (Join-Path $stage "scripts\HorsePersonalities")
Run dotnet @("run", "--project", "tools/sheet-to-json", "--", "--default-ini", (Join-Path $stage "scripts\HorsePersonalities.ini"))
Copy-Item "README.md", "LICENSE", "CHANGELOG.md" $stage

$zip = Join-Path $root "dist\HorsePersonalities-$version.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip
Remove-Item $stage -Recurse -Force
Write-Host "Built $zip"
