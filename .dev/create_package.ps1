param(
    [switch]$Interactive
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir = (Get-Item $scriptDir).Parent.FullName
Set-Location $rootDir

Write-Host "=== Preparing Unity Package Release ===" -ForegroundColor Cyan

# Ensure venv exists
if (-not (Test-Path ".\.venv")) {
    Write-Host "Virtual environment not found. Running setup_venv.ps1..." -ForegroundColor Yellow
    & ".\.dev\setup_venv.ps1"
}

$pythonExe = ".\.venv\Scripts\python.exe"
$cupPackage = ".\.venv\cup-cloned\create_unity_package"
$autoScript = ".\.dev\automated_package.py"

if (-not (Test-Path $pythonExe)) {
    Write-Host "Error: Virtual environment not found. Please run .dev\setup_venv.ps1." -ForegroundColor Red
    exit 1
}

if ($Interactive) {
    if (-not (Test-Path $cupPackage)) {
        Write-Host "Error: Tool source not found at $cupPackage." -ForegroundColor Red
        exit 1
    }
    Write-Host "Running create-unity-package (Interactive Mode)..." -ForegroundColor Cyan
    $env:PYTHONPATH = ".\.venv\cup-cloned"
    & $pythonExe -m create_unity_package --verbose
} else {
    if (-not (Test-Path $autoScript)) {
        Write-Host "Error: Automated script not found at $autoScript." -ForegroundColor Red
        exit 1
    }
    Write-Host "Running automated package creation from package.json..." -ForegroundColor Cyan
    & $pythonExe $autoScript
    
    # Gold Standard: Delete existing index.json to ensure fresh regeneration
    $indexPath = ".\docs\index.json"
    if (Test-Path $indexPath) {
        Remove-Item $indexPath -Force
    }

    # New: Update VPM Repository Index
    $vpmRepoScript = ".\.dev\vpm_repo_manager.py"
    if (Test-Path $vpmRepoScript) {
        Write-Host "Updating VPM repository index..." -ForegroundColor Cyan
        & $pythonExe $vpmRepoScript
    }

    # Helper: Suggest Git Tag
    $packageJson = Get-Content ".\package.json" | ConvertFrom-Json
    $version = $packageJson.version
    Write-Host "`nNext steps to publish:" -ForegroundColor Yellow
    # I think we can skip the original first step since we logically do all that already and it's a bit redundant
    # Write-Host "1. Git: git add . ; git commit -m 'Release v$version'" -ForegroundColor Gray
    Write-Host "1. Tag: git tag -a v$version -m 'Release v$version' ; git push origin v$version" -ForegroundColor Gray
    Write-Host "2. GitHub: Upload the ZIP from .dev/Releases/ to the GitHub release page." -ForegroundColor Gray
}

Write-Host "Release preparation complete." -ForegroundColor Green
