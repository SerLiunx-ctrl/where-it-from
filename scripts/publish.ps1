param(
    [Parameter(Mandatory = $true)]
    [string]$SPTPath,

    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$resolvedSpt = (Resolve-Path -LiteralPath $SPTPath).Path

$clientReference = Join-Path $resolvedSpt "EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"
$serverRoot = Join-Path $resolvedSpt "SPT\user\mods"
if (-not (Test-Path -LiteralPath $serverRoot)) {
    $serverRoot = Join-Path $resolvedSpt "user\mods"
}
$bepInExRoot = Join-Path $resolvedSpt "BepInEx\plugins"

if (-not (Test-Path -LiteralPath $clientReference)) {
    throw "SPTPath does not look like a complete SPT install. Missing: $clientReference"
}

$serverProject = Join-Path $root "src\Server\WhereItFrom.Server.csproj"
$clientProject = Join-Path $root "src\Client\WhereItFrom.Client.csproj"

dotnet build $serverProject -c $Configuration
dotnet build $clientProject -c $Configuration -p:SPTPath="$resolvedSpt"

$serverDll = Join-Path $root "src\Server\bin\$Configuration\net9.0\WhereItFrom.Server.dll"
$clientDll = Join-Path $root "src\Client\bin\$Configuration\netstandard2.1\WhereItFrom.Client.dll"

$serverDest = Join-Path $serverRoot "WhereItFrom"
$clientDest = Join-Path $bepInExRoot "WhereItFrom"

New-Item -ItemType Directory -Force -Path $serverDest | Out-Null
New-Item -ItemType Directory -Force -Path $clientDest | Out-Null

Copy-Item -LiteralPath $serverDll -Destination $serverDest -Force
Copy-Item -LiteralPath $clientDll -Destination $clientDest -Force

Write-Host "Installed WhereItFrom into $resolvedSpt"
