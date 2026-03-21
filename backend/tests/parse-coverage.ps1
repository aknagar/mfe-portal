# Discover coverage XML files dynamically under coverage-results/
# This avoids hardcoded GUID directory names that change between runs.
$resultsDir = Join-Path -Path (Get-Location) -ChildPath 'coverage-results'
if (-not (Test-Path -Path $resultsDir -PathType Container)) {
  throw "Coverage results directory '$resultsDir' not found. Run tests with --collect:'XPlat Code Coverage' first."
}

$coverageFiles = Get-ChildItem -Path $resultsDir -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
  throw "No coverage.cobertura.xml files found under '$resultsDir'."
}

# Build entries from discovered files — derive test project name from parent directory structure
$entries = $coverageFiles | ForEach-Object {
  # Path pattern: coverage-results/<guid>/<ProjectName>/coverage.cobertura.xml
  # or:           coverage-results/<ProjectName>/<guid>/coverage.cobertura.xml
  # Use the grandparent or nearest named segment that looks like a test project
  $parts = $_.FullName -split '[\\/]'
  $testProject = ($parts | Where-Object { $_ -match 'Tests$' } | Select-Object -Last 1)
  if (-not $testProject) { $testProject = $_.Directory.Parent.Name }
  @{ Test = $testProject; Xml = $_.FullName }
}

# Accumulate covered/total per (file, test) pair
$acc = @{}

foreach ($e in $entries) {
  if (-not (Test-Path -Path $e.Xml -PathType Leaf)) {
    Write-Warning "Coverage file '$($e.Xml)' not found, skipping."
    continue
  }
  [xml]$xml = Get-Content $e.Xml
  foreach ($cls in $xml.SelectNodes('//class')) {
    $fn = $cls.filename
    # skip test/fixture/migration files and generated code
    if ($fn -match 'UnitTests' -or $fn -match '[\\/]obj[\\/]' -or $fn -match '\.g\.cs' -or
        $fn -match 'migrations[\\/]' -or $fn -match 'Fixtures[\\/]' -or
        $fn -match 'TestDataBuilders[\\/]' -or $fn -match 'Builders[\\/]' -or
        $fn -match 'TestHelpers') { continue }

    $lines = $cls.SelectNodes('.//line')
    if ($lines.Count -eq 0) { continue }

    $total   = $lines.Count
    $covered = ($lines | Where-Object { [int]$_.hits -gt 0 }).Count

    # Split on both \ and / for cross-platform/report-format compatibility
    $parts = $fn -split '[\\/]'
    $short = if ($parts.Count -ge 2) { "$($parts[-2])/$($parts[-1])" } else { $parts[-1] }

    $key = "$short|$($e.Test)"
    if ($acc.ContainsKey($key)) {
      $acc[$key].Covered += $covered
      $acc[$key].Total   += $total
    } else {
      $acc[$key] = [pscustomobject]@{
        SourceFile  = $short
        TestProject = $e.Test
        Covered     = $covered
        Total       = $total
      }
    }
  }
}

# Build output rows, merging results for same file across test projects
$merged = @{}
foreach ($key in $acc.Keys) {
  $r = $acc[$key]
  $fileKey = $r.SourceFile
  if ($merged.ContainsKey($fileKey)) {
    $merged[$fileKey].Covered += $r.Covered
    $merged[$fileKey].Total   += $r.Total
    # combine test names
    if (-not $merged[$fileKey].TestProject.Contains($r.TestProject)) {
      $merged[$fileKey].TestProject += ", $($r.TestProject)"
    }
  } else {
    $merged[$fileKey] = [pscustomobject]@{
      SourceFile  = $r.SourceFile
      TestProject = $r.TestProject
      Covered     = $r.Covered
      Total       = $r.Total
    }
  }
}

$rows = $merged.Values | ForEach-Object {
  $pct = if ($_.Total -gt 0) { [math]::Round($_.Covered / $_.Total * 100) } else { 0 }
  [pscustomobject]@{
    'Source File' = $_.SourceFile
    'Test Project(s)' = $_.TestProject
    'Coverage' = "$pct%"
    'Lines (hit/total)' = "$($_.Covered)/$($_.Total)"
  }
}

$rows | Sort-Object 'Source File' | Format-Table -AutoSize
