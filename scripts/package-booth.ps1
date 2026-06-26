$ErrorActionPreference = "Stop"

$version = "1.0.0"
$packageName = "PicPack_v$version`_BOOTH"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$distDir = Join-Path $repoRoot "dist"
$publishDir = Join-Path $distDir "publish-win-x64"
$packageDir = Join-Path $distDir $packageName
$zipPath = Join-Path $distDir "$packageName.zip"
$projectPath = Join-Path $repoRoot "src\PicPack.App\PicPack.App.csproj"
$releaseDocsDir = Join-Path $repoRoot "docs\release"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file was not found: $projectPath"
}

$resolvedRepoRoot = [System.IO.Path]::GetFullPath($repoRoot)
$resolvedDistDir = [System.IO.Path]::GetFullPath($distDir)
if (-not $resolvedDistDir.StartsWith($resolvedRepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to remove a dist path outside the repository: $resolvedDistDir"
}

if (Test-Path -LiteralPath $distDir) {
    Remove-Item -LiteralPath $distDir -Recurse -Force
}

New-Item -ItemType Directory -Path $distDir, $publishDir, $packageDir | Out-Null

dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $publishDir `
    /p:AssemblyName=PicPack `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true

$exePath = Join-Path $publishDir "PicPack.exe"
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Published executable was not found: $exePath"
}

Copy-Item -LiteralPath $exePath -Destination (Join-Path $packageDir "PicPack.exe")
Copy-Item -LiteralPath (Join-Path $releaseDocsDir "README.txt") -Destination (Join-Path $packageDir "README.txt")
Copy-Item -LiteralPath (Join-Path $releaseDocsDir "LICENSE.txt") -Destination (Join-Path $packageDir "LICENSE.txt")
Copy-Item -LiteralPath (Join-Path $releaseDocsDir "CHANGELOG.txt") -Destination (Join-Path $packageDir "CHANGELOG.txt")

Compress-Archive -LiteralPath $packageDir -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Created BOOTH package:"
Write-Host $zipPath
Write-Host ""
Write-Host "ZIP contents:"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $zip.Entries |
        Sort-Object FullName |
        ForEach-Object {
            Write-Host ("- {0} ({1} bytes)" -f $_.FullName, $_.Length)
        }
}
finally {
    $zip.Dispose()
}
