param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SPTPath,
    [string]$Configuration = "Release",
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$resolvedRoot = (Resolve-Path -LiteralPath $root).Path
$resolvedSpt = (Resolve-Path -LiteralPath $SPTPath).Path

$clientReference = Join-Path $resolvedSpt "EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"
if (-not (Test-Path -LiteralPath $clientReference)) {
    throw "SPTPath does not look like a complete SPT install. Missing: $clientReference"
}

$serverProject = Join-Path $resolvedRoot "src\Server\WhereItFrom.Server.csproj"
$clientProject = Join-Path $resolvedRoot "src\Client\WhereItFrom.Client.csproj"

dotnet build $serverProject -c $Configuration
dotnet build $clientProject -c $Configuration -p:SPTPath="$resolvedSpt"

$serverDll = Join-Path $resolvedRoot "src\Server\bin\$Configuration\net9.0\WhereItFrom.Server.dll"
$clientDll = Join-Path $resolvedRoot "src\Client\bin\$Configuration\netstandard2.1\WhereItFrom.Client.dll"

$resolvedOutputDir = Join-Path $resolvedRoot $OutputDir
$stageRoot = Join-Path $resolvedOutputDir "package"
$packageFileName = "WhereItFrom-v1.0.0-SPT4.0.zip"
$zipPath = Join-Path $resolvedOutputDir $packageFileName

New-Item -ItemType Directory -Force -Path $resolvedOutputDir | Out-Null

if (Test-Path -LiteralPath $stageRoot) {
    $resolvedStageRoot = (Resolve-Path -LiteralPath $stageRoot).Path
    if (-not $resolvedStageRoot.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove staging path outside workspace: $resolvedStageRoot"
    }

    Remove-Item -LiteralPath $resolvedStageRoot -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    $resolvedZipPath = (Resolve-Path -LiteralPath $zipPath).Path
    if (-not $resolvedZipPath.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove zip outside workspace: $resolvedZipPath"
    }

    Remove-Item -LiteralPath $resolvedZipPath -Force
}

$clientDest = Join-Path $stageRoot "BepInEx\plugins\WhereItFrom"
$serverDest = Join-Path $stageRoot "SPT\user\mods\WhereItFrom"
New-Item -ItemType Directory -Force -Path $clientDest | Out-Null
New-Item -ItemType Directory -Force -Path $serverDest | Out-Null

Copy-Item -LiteralPath $clientDll -Destination $clientDest -Force
Copy-Item -LiteralPath $serverDll -Destination $serverDest -Force
Copy-Item -LiteralPath (Join-Path $resolvedRoot "README.md") -Destination (Join-Path $stageRoot "README.md") -Force

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zipStream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::CreateNew)
try {
    $archive = [System.IO.Compression.ZipArchive]::new($zipStream, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -LiteralPath $stageRoot -Recurse -File | ForEach-Object {
            $relativePath = $_.FullName.Substring($stageRoot.Length).TrimStart([char[]]@('\', '/'))
            $entryName = $relativePath -replace '\\', '/'
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $_.FullName,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $zipStream.Dispose()
}

Remove-Item -LiteralPath $stageRoot -Recurse -Force

Write-Host "Created package: $(Join-Path $OutputDir $packageFileName)"
