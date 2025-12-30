#!/bin/bash

# Add OpenAPI/Swagger to a .NET ASP.NET Core project
# Usage: ./add-openapi.sh <ProjectPath> <DocumentationTitle> [ui-choice]
# ui-choice: 'swagger' (default) or 'scalar'

set -e

PROJECT_PATH="${1:-.}"
TITLE="${2:-My API}"
UI_CHOICE="${3:-swagger}"

if [ ! -f "$PROJECT_PATH/*.csproj" ]; then
    echo "Error: No .csproj file found in $PROJECT_PATH"
    exit 1
fi

PROJECT_FILE=$(find "$PROJECT_PATH" -name "*.csproj" -type f | head -1)
PROJECT_NAME=$(basename "$PROJECT_FILE" .csproj)

echo "========================================="
echo "Adding OpenAPI Documentation"
echo "========================================="
echo "Project: $PROJECT_NAME"
echo "Documentation Title: $TITLE"
echo "UI Choice: $UI_CHOICE"
echo ""

# Add NuGet packages
echo "Adding NuGet packages..."
dotnet add "$PROJECT_FILE" package Swashbuckle.AspNetCore --version 6.4.0

if [ "$UI_CHOICE" = "scalar" ]; then
    dotnet add "$PROJECT_FILE" package Scalar.AspNetCore --version 1.2.28
    echo "Added Scalar.AspNetCore"
fi

# Enable XML documentation
echo ""
echo "Enabling XML documentation..."

# Create backup of .csproj
cp "$PROJECT_FILE" "$PROJECT_FILE.bak"

# Add/update GenerateDocumentationFile property
if grep -q "GenerateDocumentationFile" "$PROJECT_FILE"; then
    sed -i 's/<GenerateDocumentationFile>.*<\/GenerateDocumentationFile>/<GenerateDocumentationFile>true<\/GenerateDocumentationFile>/' "$PROJECT_FILE"
else
    # Add before closing PropertyGroup
    sed -i '/<\/PropertyGroup>/i\    <GenerateDocumentationFile>true</GenerateDocumentationFile>' "$PROJECT_FILE"
fi

echo "✓ XML documentation enabled"

echo ""
echo "========================================="
echo "Configuration Complete!"
echo "========================================="
echo ""
echo "Next steps:"
echo "1. Update Program.cs with Swagger configuration"
echo "   - Add 'using Microsoft.OpenApi.Models;'"
if [ "$UI_CHOICE" = "scalar" ]; then
    echo "   - Add 'using Scalar.AspNetCore;'"
fi
echo "   - Configure builder.Services.AddSwaggerGen()"
echo "   - Add app.UseSwagger() in development"
if [ "$UI_CHOICE" = "scalar" ]; then
    echo "   - Add app.MapScalarApiReference()"
else
    echo "   - Add app.UseSwaggerUI()"
fi
echo ""
echo "2. Document your endpoints with /// <summary> comments"
echo ""
echo "3. Add [ProducesResponseType] attributes to endpoints"
echo ""
if [ "$UI_CHOICE" = "swagger" ]; then
    echo "4. Access documentation at: https://localhost:xxxx/swagger"
else
    echo "4. Access documentation at: https://localhost:xxxx/scalar"
fi
echo ""
