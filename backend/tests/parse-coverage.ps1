$entries = @(
  @{ Test='AugmentService.Api.UnitTests'; Xml='coverage-results\9b1952f4-3973-4652-bfc0-081fa4b55ddc\coverage.cobertura.xml' },
  @{ Test='AugmentService.Core.UnitTests'; Xml='coverage-results\657a74d2-00f2-40ba-9f41-dd40daef9ea2\coverage.cobertura.xml' }
)

# Accumulate covered/total per (file, test) pair
$acc = @{}

foreach ($e in $entries) {
  [xml]$xml = Get-Content $e.Xml
  foreach ($cls in $xml.SelectNodes('//class')) {
    $fn = $cls.filename
    # skip test/fixture/migration files and generated code
    if ($fn -match 'UnitTests' -or $fn -match '[\\\/]obj[\\\/]' -or $fn -match '\.g\.cs' -or
        $fn -match 'migrations[\\\/]' -or $fn -match 'Fixtures[\\\/]' -or
        $fn -match 'TestDataBuilders[\\\/]' -or $fn -match 'Builders[\\\/]' -or
        $fn -match 'TestHelpers') { continue }

    $lines = $cls.SelectNodes('.//line')
    if ($lines.Count -eq 0) { continue }

    $total   = $lines.Count
    $covered = ($lines | Where-Object { [int]$_.hits -gt 0 }).Count

    $parts = $fn -split '\\'
    $short = if ($parts.Count -ge 2) { "$($parts[-2])\$($parts[-1])" } else { $parts[-1] }

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

# Build output rows, merging Api+Core test results for same file
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
