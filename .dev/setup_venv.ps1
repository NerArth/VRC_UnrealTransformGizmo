# PowerShell script to set up a local Python virtual environment
# and install the create-unity-package tool from GitHub.

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir = (Get-Item $scriptDir).Parent.FullName
Set-Location $rootdir

Write-Host "=== Setting up Unity Package Automation Environment ===" -ForegroundColor Cyan

# Check for Python
try {
    $pythonVersion = & python --version 2>&1
    Write-Host "Using $pythonVersion" -ForegroundColor Gray
} catch {
    Write-Host "Error: Python 3 is not installed or not in PATH." -ForegroundColor Red
    exit 1
}

# Create virtual environment if it doesn't exist
if (-not (Test-Path ".\.venv")) {
    Write-Host "Creating virtual environment in .venv..." -ForegroundColor Cyan
    python -m venv .\.venv
} else {
    Write-Host "Virtual environment already exists." -ForegroundColor Gray
}

# Install/Update dependencies
Write-Host "Updating pip and cloning create_unity_package..." -ForegroundColor Cyan
& ".\.venv\Scripts\python.exe" -m pip install --upgrade pip
& ".\.venv\Scripts\python.exe" -m pip install inquirer jinja2

# Download source files
$cupDir = ".\.venv\cup-cloned"
if (Test-Path $cupDir) { Remove-Item $cupDir -Recurse -Force }
New-Item -ItemType Directory -Path $cupDir

Write-Host "Downloading create-unity-package source from GitHub..." -ForegroundColor Cyan
$tempDir = Join-Path $env:TEMP ([Guid]::NewGuid().ToString())
git clone --depth 1 --quiet https://github.com/ShiJbey/create_unity_package $tempDir
# We want the folder itself within cup-cloned to keep package structure
Copy-Item "$tempDir\src\create_unity_package" $cupDir -Recurse -Force
Remove-Item $tempDir -Recurse -Force

Write-Host "Setup complete! You can now use create_package.ps1." -ForegroundColor Green
