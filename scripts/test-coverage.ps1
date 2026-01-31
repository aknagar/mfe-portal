<#
.SYNOPSIS
    Runs .NET tests with code coverage and generates HTML reports.

.DESCRIPTION
    This script runs backend tests with configurable scope (unit, integration, or all),
    generates coverage reports in multiple formats, and opens the HTML report in a browser.
    
.PARAMETER UnitOnly
    Run only unit tests (excludes IntegrationTests and E2eTests projects).
    
.PARAMETER IntegrationOnly
    Run only integration and E2E tests.
    
.PARAMETER All
    Run all tests (default behavior).
    
.PARAMETER SkipReport
    Skip generating the HTML report (only generate Cobertura XML).
    
.PARAMETER Threshold
    Minimum coverage threshold percentage (default: 80).
    
.EXAMPLE
    .\test-coverage.ps1
    Runs all tests with coverage and generates reports.
    
.EXAMPLE
    .\test-coverage.ps1 -UnitOnly
    Runs only unit tests with coverage.
    
.EXAMPLE
    .\test-coverage.ps1 -IntegrationOnly
    Runs only integration and E2E tests.
    
.EXAMPLE
    .\test-coverage.ps1 -Threshold 85
    Runs all tests and enforces 85% coverage threshold.
#>

[CmdletBinding(DefaultParameterSetName='All')]
param(
    [Parameter(ParameterSetName='UnitOnly')]
    [switch]$UnitOnly,
    
    [Parameter(ParameterSetName='IntegrationOnly')]
    [switch]$IntegrationOnly,
    
    [Parameter(ParameterSetName='All')]
    [switch]$All,
    
    [switch]$SkipReport,
    
    [int]$Threshold = 80
)

$ErrorActionPreference = "Stop"

# Script constants
$BackendDir = Join-Path $PSScriptRoot "..\backend"
$TestResultsDir = Join-Path $BackendDir "TestResults"
$CoverageReportDir = Join-Path $TestResultsDir "CoverageReport"
$RunSettingsFile = Join-Path $BackendDir "coverlet.runsettings"
$SolutionFile = Join-Path $BackendDir "MfePortal.Backend.sln"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Backend Test Coverage Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Determine test filter
$testFilter = ""
if ($UnitOnly) {
    $testFilter = "--filter `"FullyQualifiedName!~IntegrationTests&FullyQualifiedName!~E2eTests`""
    Write-Host "Test Scope: " -NoNewline -ForegroundColor Yellow
    Write-Host "Unit Tests Only" -ForegroundColor Green
} elseif ($IntegrationOnly) {
    $testFilter = "--filter `"FullyQualifiedName~IntegrationTests|FullyQualifiedName~E2eTests`""
    Write-Host "Test Scope: " -NoNewline -ForegroundColor Yellow
    Write-Host "Integration & E2E Tests Only" -ForegroundColor Green
} else {
    Write-Host "Test Scope: " -NoNewline -ForegroundColor Yellow
    Write-Host "All Tests" -ForegroundColor Green
}

Write-Host "Threshold: " -NoNewline -ForegroundColor Yellow
Write-Host "$Threshold%" -ForegroundColor Green
Write-Host ""

# Clean previous test results
if (Test-Path $TestResultsDir) {
    Write-Host "Cleaning previous test results..." -ForegroundColor Yellow
    Remove-Item -Path $TestResultsDir -Recurse -Force
}

# Check if solution exists
if (-not (Test-Path $SolutionFile)) {
    Write-Host "ERROR: Solution file not found at $SolutionFile" -ForegroundColor Red
    exit 1
}

# Check if runsettings file exists
if (-not (Test-Path $RunSettingsFile)) {
    Write-Host "WARNING: coverlet.runsettings not found at $RunSettingsFile" -ForegroundColor Yellow
    Write-Host "Running without custom settings..." -ForegroundColor Yellow
    $settingsArg = ""
} else {
    $settingsArg = "--settings `"$RunSettingsFile`""
}

# Run tests with coverage
Write-Host ""
Write-Host "Running tests with coverage..." -ForegroundColor Cyan
Write-Host "Command: dotnet test `"$SolutionFile`" $testFilter --collect:`"XPlat Code Coverage`" $settingsArg" -ForegroundColor Gray
Write-Host ""

$testCommand = "dotnet test `"$SolutionFile`" $testFilter --collect:`"XPlat Code Coverage`" $settingsArg --results-directory `"$TestResultsDir`" --verbosity normal"

$testResult = Invoke-Expression $testCommand

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  TESTS FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Tests completed successfully!" -ForegroundColor Green

# Find coverage files
$coverageFiles = Get-ChildItem -Path $TestResultsDir -Filter "coverage.cobertura.xml" -Recurse

if ($coverageFiles.Count -eq 0) {
    Write-Host ""
    Write-Host "ERROR: No coverage files found!" -ForegroundColor Red
    Write-Host "Expected location: $TestResultsDir" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

# Generate HTML report if not skipped
if (-not $SkipReport) {
    Write-Host ""
    Write-Host "Generating HTML coverage report..." -ForegroundColor Cyan
    
    # Check if ReportGenerator is installed
    $reportGeneratorExists = $null -ne (dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool")
    
    if (-not $reportGeneratorExists) {
        Write-Host "ReportGenerator not found. Installing..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Failed to install ReportGenerator" -ForegroundColor Red
            exit 1
        }
    }
    
    # Build coverage files argument
    $coverageFilePaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
    
    # Generate report
    $reportCommand = "reportgenerator `"-reports:$coverageFilePaths`" `"-targetdir:$CoverageReportDir`" `"-reporttypes:Html;HtmlSummary;Cobertura;JsonSummary`" `"-verbosity:Warning`""
    
    Invoke-Expression $reportCommand
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to generate coverage report" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "HTML report generated at: $CoverageReportDir" -ForegroundColor Green
}

# Parse coverage summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Coverage Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Read the first coverage file to get summary
$coverageXml = [xml](Get-Content $coverageFiles[0].FullName)
$lineRate = [math]::Round([decimal]$coverageXml.coverage.'line-rate' * 100, 2)
$branchRate = [math]::Round([decimal]$coverageXml.coverage.'branch-rate' * 100, 2)
$linesCovered = $coverageXml.coverage.'lines-covered'
$linesValid = $coverageXml.coverage.'lines-valid'
$branchesCovered = $coverageXml.coverage.'branches-covered'
$branchesValid = $coverageXml.coverage.'branches-valid'

Write-Host ""
Write-Host "Line Coverage:   " -NoNewline -ForegroundColor Yellow
if ($lineRate -ge $Threshold) {
    Write-Host "$lineRate% ($linesCovered / $linesValid)" -ForegroundColor Green
} else {
    Write-Host "$lineRate% ($linesCovered / $linesValid)" -ForegroundColor Red
}

Write-Host "Branch Coverage: " -NoNewline -ForegroundColor Yellow
if ($branchRate -ge $Threshold) {
    Write-Host "$branchRate% ($branchesCovered / $branchesValid)" -ForegroundColor Green
} else {
    Write-Host "$branchRate% ($branchesCovered / $branchesValid)" -ForegroundColor Yellow
}

Write-Host ""

# Show package-level coverage
Write-Host "Coverage by Package:" -ForegroundColor Cyan
$packages = $coverageXml.coverage.packages.package
foreach ($package in $packages) {
    $pkgLineRate = [math]::Round([decimal]$package.'line-rate' * 100, 2)
    $pkgName = $package.name
    
    Write-Host "  ${pkgName}: " -NoNewline -ForegroundColor Gray
    
    if ($pkgLineRate -ge $Threshold) {
        Write-Host "$pkgLineRate%" -ForegroundColor Green
    } elseif ($pkgLineRate -ge ($Threshold - 20)) {
        Write-Host "$pkgLineRate%" -ForegroundColor Yellow
    } else {
        Write-Host "$pkgLineRate%" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Open HTML report in browser
if (-not $SkipReport) {
    $reportIndexFile = Join-Path $CoverageReportDir "index.html"
    
    if (Test-Path $reportIndexFile) {
        Write-Host ""
        Write-Host "Opening coverage report in browser..." -ForegroundColor Cyan
        Start-Process $reportIndexFile
    }
}

# Check threshold
if ($lineRate -lt $Threshold) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  COVERAGE BELOW THRESHOLD" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Required: $Threshold%" -ForegroundColor Yellow
    Write-Host "Actual:   $lineRate%" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ALL CHECKS PASSED" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

exit 0
