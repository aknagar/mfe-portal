# Add OpenAPI/Swagger to a .NET ASP.NET Core project (Windows PowerShell)
# Usage: .\add-openapi.ps1 -ProjectPath "." -Title "My API" -UIChoice "swagger"

param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectPath = ".",
    
    [Parameter(Mandatory = $false)]
    [string]$Title = "My API",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("swagger", "scalar")]
    [string]$UIChoice = "swagger"
)

$ErrorActionPreference = "Stop"

# Find .csproj file
$ProjectFile = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" -File | Select-Object -First 1

if (-not $ProjectFile) {
    Write-Error "No .csproj file found in $ProjectPath"
    exit 1
}

$ProjectName = $ProjectFile.BaseName

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Adding OpenAPI Documentation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Project: $ProjectName"
Write-Host "Documentation Title: $Title"
Write-Host "UI Choice: $UIChoice"
Write-Host ""

# Add NuGet packages
Write-Host "Adding NuGet packages..." -ForegroundColor Yellow
dotnet add $ProjectFile.FullName package Swashbuckle.AspNetCore --version 6.4.0

if ($UIChoice -eq "scalar") {
    dotnet add $ProjectFile.FullName package Scalar.AspNetCore --version 1.2.28
    Write-Host "Added Scalar.AspNetCore" -ForegroundColor Green
}

# Enable XML documentation
Write-Host ""
Write-Host "Enabling XML documentation..." -ForegroundColor Yellow

# Backup original file
Copy-Item -Path $ProjectFile.FullName -Destination "$($ProjectFile.FullName).bak"

# Read project file
$xmlContent = [xml](Get-Content $ProjectFile.FullName)

# Find or create PropertyGroup
$propertyGroup = $xmlContent.Project.PropertyGroup[0]

if (-not $propertyGroup) {
    Write-Error "Could not find PropertyGroup in .csproj"
    exit 1
}

# Add/update GenerateDocumentationFile
$genDocElement = $propertyGroup.SelectSingleNode("GenerateDocumentationFile")
if ($genDocElement) {
    $genDocElement.InnerText = "true"
} else {
    $newElement = $xmlContent.CreateElement("GenerateDocumentationFile")
    $newElement.InnerText = "true"
    $propertyGroup.AppendChild($newElement) | Out-Null
}

# Save updated project file
$xmlContent.Save($ProjectFile.FullName)

Write-Host "✓ XML documentation enabled" -ForegroundColor Green

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Configuration Complete!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update Program.cs with Swagger configuration"
Write-Host "   - Add 'using Microsoft.OpenApi.Models;'"
if ($UIChoice -eq "scalar") {
    Write-Host "   - Add 'using Scalar.AspNetCore;'"
}
Write-Host "   - Configure builder.Services.AddSwaggerGen()"
Write-Host "   - Add app.UseSwagger() in development"
if ($UIChoice -eq "scalar") {
    Write-Host "   - Add app.MapScalarApiReference()"
} else {
    Write-Host "   - Add app.UseSwaggerUI()"
}
Write-Host ""
Write-Host "2. Document your endpoints with /// <summary> comments"
Write-Host ""
Write-Host "3. Add [ProducesResponseType] attributes to endpoints"
Write-Host ""
if ($UIChoice -eq "swagger") {
    Write-Host "4. Access documentation at: https://localhost:xxxx/swagger" -ForegroundColor Green
} else {
    Write-Host "4. Access documentation at: https://localhost:xxxx/scalar" -ForegroundColor Green
}
Write-Host ""
