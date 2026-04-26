param(
    [string]$Version = "0.1.1"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "src\PinBox.App\PinBox.App.csproj"
$configuration = "Release"
$runtime = "win-x64"
$packageName = "PinBox-v$Version-$runtime"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$releaseRoot = Join-Path $artifactsRoot "release"
$stageRoot = Join-Path $artifactsRoot "package"
$stagePath = Join-Path $stageRoot $packageName
$smokeRoot = Join-Path $artifactsRoot "smoke-test\$runtime"
$zipPath = Join-Path $releaseRoot "$packageName.zip"

function Reset-Directory([string]$Path) {
    if (Test-Path $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Find-BuildOutput {
    $candidatePaths = @(
        (Join-Path $repoRoot "src\PinBox.App\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64"),
        (Join-Path $repoRoot "src\PinBox.App\bin\Release\net8.0-windows10.0.19041.0\win-x64"),
        (Join-Path $repoRoot "src\PinBox.App\bin\x64\Release\net8.0-windows10.0.19041.0"),
        (Join-Path $repoRoot "src\PinBox.App\bin\Release\net8.0-windows10.0.19041.0")
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path (Join-Path $candidate "PinBox.App.exe")) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Could not find Release build output containing PinBox.App.exe."
}

function Assert-RequiredFile([string]$Root, [string]$RelativePath) {
    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path $path)) {
        throw "Missing required release file: $RelativePath"
    }
}

function Assert-RequiredXbf([string]$Root) {
    $validMainWindowXbfPaths = @(
        "MainWindow.xbf",
        "Views\MainWindow.xbf"
    )

    foreach ($relativePath in $validMainWindowXbfPaths) {
        if (Test-Path (Join-Path $Root $relativePath)) {
            return
        }
    }

    throw "Missing required MainWindow XBF file. Expected MainWindow.xbf or Views\MainWindow.xbf."
}

function Copy-ReleaseFiles([string]$Source, [string]$Destination) {
    $excludedExtensions = @(
        ".pdb",
        ".binlog",
        ".log",
        ".ilk",
        ".iobj",
        ".ipdb",
        ".tmp",
        ".cache"
    )

    Get-ChildItem -Path $Source -Recurse -File |
        Where-Object { $excludedExtensions -notcontains $_.Extension.ToLowerInvariant() } |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($Source.Length).TrimStart([char[]]@("\", "/"))
            $targetPath = Join-Path $Destination $relativePath
            $targetDirectory = Split-Path $targetPath -Parent

            New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
        }
}

function Test-SmokeLaunch([string]$ExePath, [string]$WorkingDirectory) {
    $startTime = Get-Date
    $process = Start-Process -FilePath $ExePath -WorkingDirectory $WorkingDirectory -PassThru

    Start-Sleep -Seconds 5

    $applicationErrors = Get-WinEvent -FilterHashtable @{ LogName = "Application"; ProviderName = "Application Error"; StartTime = $startTime } -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -like "*PinBox.App.exe*" }

    if ($process.HasExited) {
        throw "Smoke test failed: PinBox.App.exe exited immediately with code $($process.ExitCode)."
    }

    if ($applicationErrors) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        throw "Smoke test failed: Windows Application Error was logged for PinBox.App.exe."
    }

    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "Building PinBox $Version ($configuration/$runtime)..."
dotnet build $projectPath --configuration $configuration --runtime $runtime -p:Platform=x64 --self-contained false

$buildOutput = Find-BuildOutput
Write-Host "Using build output: $buildOutput"

Reset-Directory $stagePath
New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

Copy-ReleaseFiles -Source $buildOutput -Destination $stagePath

Assert-RequiredFile -Root $stagePath -RelativePath "PinBox.App.exe"
Assert-RequiredFile -Root $stagePath -RelativePath "PinBox.App.dll"
Assert-RequiredFile -Root $stagePath -RelativePath "PinBox.App.deps.json"
Assert-RequiredFile -Root $stagePath -RelativePath "PinBox.App.runtimeconfig.json"
Assert-RequiredFile -Root $stagePath -RelativePath "PinBox.App.pri"
Assert-RequiredFile -Root $stagePath -RelativePath "App.xbf"
Assert-RequiredXbf -Root $stagePath

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $stagePath "*") -DestinationPath $zipPath -Force
Write-Host "Created ZIP: $zipPath"

Reset-Directory $smokeRoot
Expand-Archive -Path $zipPath -DestinationPath $smokeRoot -Force

$smokeExe = Join-Path $smokeRoot "PinBox.App.exe"
Assert-RequiredFile -Root $smokeRoot -RelativePath "PinBox.App.exe"

Write-Host "Running clean-folder smoke test..."
Test-SmokeLaunch -ExePath $smokeExe -WorkingDirectory $smokeRoot

Write-Host "Smoke test passed."
Write-Host "Final ZIP: $zipPath"
