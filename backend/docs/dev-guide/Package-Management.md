# Central NuGet Package Management

## Overview

This project uses **Central Package Management (CPM)** to manage NuGet package versions across all projects in the solution. CPM is a .NET feature that centralizes package version declarations in a single file, reducing duplication and ensuring consistency.

## Benefits

- **Single Source of Truth**: All package versions are declared in one location
- **Version Consistency**: Prevents version conflicts across projects
- **Reduced Duplication**: No need to specify versions in each project file
- **Easier Maintenance**: Update versions once and it applies everywhere
- **Cleaner Project Files**: Less clutter in individual `.csproj` files
- **Risk Reduction**: Lower chance of accidental version mismatches

## How It Works

### Central Configuration File

The file [Directory.Packages.props](../../Directory.Packages.props) serves as the central package version registry for the entire backend solution.

**Key Property:**
```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
```

This setting enables CPM for all projects in the solution.

### Project File Format

When CPM is enabled, project files reference packages **without** specifying versions:

**❌ WITHOUT CPM (not used here):**
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.1" />
  <PackageReference Include="Azure.Identity" Version="1.12.0" />
</ItemGroup>
```

**✅ WITH CPM (current approach):**
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Azure.Identity" />
</ItemGroup>
```

The versions are managed in Directory.Packages.props:
```xml
<ItemGroup>
  <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.1" />
  <PackageVersion Include="Azure.Identity" Version="1.12.0" />
</ItemGroup>
```

## Adding New Packages

### Step 1: Check if Package Version Already Exists

First, check [Directory.Packages.props](../../Directory.Packages.props) to see if the package you need is already defined.

```powershell
# Search for package in the props file
Select-String -Path backend/Directory.Packages.props -Pattern "PackageName"
```

### Step 2a: If Package Version Exists

Simply add a `PackageReference` (without version) to your project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
</ItemGroup>
```

### Step 2b: If Package Version Doesn't Exist

1. Add the `PackageVersion` entry to [Directory.Packages.props](../../Directory.Packages.props) in the appropriate category:

```xml
<PackageVersion Include="NewPackageName" Version="X.Y.Z" />
```

2. Then add the `PackageReference` to your project file (without version):

```xml
<ItemGroup>
  <PackageReference Include="NewPackageName" />
</ItemGroup>
```

### Step 3: Verify

Build the solution to ensure the package is properly resolved:

```powershell
dotnet build MfePortal.Backend.sln
```

## Package Organization in Directory.Packages.props

Packages are organized by category with comments for clarity:

- **Aspire**: Cloud-native orchestration packages
- **Azure**: Azure SDK packages (Identity, Key Vault, Service Bus, etc.)
- **Dapr**: Distributed application runtime packages
- **Validation & Business Logic**: FluentValidation, FluentResults
- **MediatR**: CQRS and mediator pattern packages
- **ASP.NET Core & Web**: Web frameworks, OpenAPI, database drivers
- **Build & Code Generation**: Build tools, code generation
- **Entity Framework Core**: ORM packages
- **Identity & Security**: JWT and authentication
- **Extensions**: Microsoft.Extensions namespace packages
- **OpenTelemetry**: Observability and tracing
- **Utilities**: General-purpose utility packages

When adding new packages, place them in the most appropriate category or create a new category if needed.

## Updating Package Versions

### For Single Package

1. Locate the package in [Directory.Packages.props](../../Directory.Packages.props)
2. Update the Version attribute
3. Rebuild and test thoroughly:

```powershell
dotnet build MfePortal.Backend.sln --configuration Release
```

### For Multiple Related Packages

Group updates logically (e.g., all Entity Framework Core packages, all OpenTelemetry packages) to ensure compatibility.

### Important Considerations

- **Breaking Changes**: Check release notes for breaking changes before updating
- **Dependency Chain**: Some packages depend on other packages; verify compatibility
- **Testing**: Always test the solution after version updates
- **Documentation**: Update relevant documentation if the update introduces new APIs or changes behavior

## Transitive Dependencies

.NET may pull in transitive dependencies (packages required by the packages you reference). You generally don't need to explicitly manage these in Directory.Packages.props unless you need to:

- Pin a specific version to resolve conflicts
- Control a transitive dependency that's causing issues
- Ensure a security patch is applied

If you need to manage a transitive dependency, add it to Directory.Packages.props:

```xml
<PackageVersion Include="TransitiveDependencyName" Version="X.Y.Z" />
```

## Version Strategy

The project uses **semantic versioning** (Major.Minor.Patch):
- **Major**: Breaking changes
- **Minor**: New features (backward compatible)
- **Patch**: Bug fixes (backward compatible)

**General Approach:**
- Use **specific versions** (e.g., 9.0.1) to avoid unexpected changes
- Update regularly but test thoroughly
- Batch security-related updates and test them together
- Keep Microsoft packages relatively in sync for framework consistency

## Troubleshooting

### Package Not Found

If you get a "package not found" error:

1. Verify the spelling in Directory.Packages.props
2. Check that the PackageVersion entry uses the exact same Include name as the PackageReference
3. Rebuild: `dotnet build --no-cache`

### Version Conflict

If two packages require different versions of a dependency:

1. Update Directory.Packages.props to define a version that satisfies both constraints
2. If impossible, document the conflict and consider alternative packages
3. NuGet will show which packages are conflicting

### Project Not Finding Package

If one project references a package in Directory.Packages.props but can't find it:

1. Ensure the PackageReference exists in the project file
2. Verify the Include name matches exactly
3. Check that the project file includes the necessary ItemGroup element
4. Do a clean rebuild: `dotnet clean && dotnet build`

## Best Practices

### ✅ DO

- Add all package references to Directory.Packages.props
- Organize packages by category
- Use explicit, sensible versions (no wildcards)
- Keep related packages (e.g., same vendor) at compatible versions
- Document why you're pinning a specific version if it's unusual
- Review [Directory.Packages.props](../../Directory.Packages.props) before adding new packages
- Keep it in sync across all team members
- Test after any version updates

### ❌ DON'T

- Specify versions in individual project files
- Use version ranges or wildcards (e.g., `9.0.*`)
- Add packages to projects without declaring them in Directory.Packages.props
- Forget to include a PackageVersion in the props file when adding a new package
- Commit version changes without testing them
- Leave old/unused package declarations in Directory.Packages.props

## Additional Resources

- [Microsoft Docs: Manage NuGet packages using the Directory.Packages.props file](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [NuGet Documentation](https://docs.microsoft.com/en-us/nuget/)
- [Semantic Versioning](https://semver.org/)

## Related Documentation

- [Configuration Management](./Configuration-Management.md) - How configuration is managed across environments
- [Secret Management](./Secret-Management.md) - How secrets are stored and accessed
- [ARCHITECTURE.md](../ARCHITECTURE.md) - Overall solution architecture
