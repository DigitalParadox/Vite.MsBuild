# Script to update test files from old ViteCommandBuilder constructor to new configuration-based pattern
# Old: new ViteCommandBuilder(projectRoot, PackageManager.Npm)
# New: new ViteCommandBuilder(new ViteBuildConfiguration { ProjectRoot = projectRoot, PackageManager = PackageManager.Npm })

$testFiles = Get-ChildItem -Path "tests" -Recurse -Filter "*Tests.cs"

$totalReplacements = 0

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # Pattern 1: new ViteCommandBuilder(var, PackageManager.XXX)
    # Replace with config object
    $pattern1 = 'new ViteCommandBuilder\(([^,]+),\s*PackageManager\.(\w+)\)'
    $replacement1 = 'new ViteCommandBuilder(new ViteBuildConfiguration { ProjectRoot = $1, PackageManager = PackageManager.$2 })'
    $content = $content -replace $pattern1, $replacement1
    
    # Pattern 2: .WithConfig(...) -> set ConfigFile in config
    # Pattern 3: .WithMode(...) -> set Mode in config
    # Pattern 4: .WithOutputDir(...) -> set OutputDir in config
    # These will need manual review, but let's do the basic constructor replacement first
    
    if ($content -ne $originalContent) {
        $replacementCount = ([regex]::Matches($originalContent, $pattern1)).Count
        $totalReplacements += $replacementCount
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "✅ Updated $($file.Name): $replacementCount replacements" -ForegroundColor Green
    }
}

Write-Host "`n📊 Total replacements: $totalReplacements" -ForegroundColor Cyan
Write-Host "⚠️  Note: Some tests may need manual review for method chaining (WithConfig, WithMode, etc.)" -ForegroundColor Yellow
