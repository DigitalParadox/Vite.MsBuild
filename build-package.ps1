# Build Vite.MsBuild NuGet Package

param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = ".\nupkg"
)

# Set UTF-8 encoding for proper emoji display
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "📦 Building Vite.MsBuild NuGet Package v$Version" -ForegroundColor Cyan
Write-Host ""

# Ensure we're in the right directory
$packageDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $packageDir

try {
    # Update version in nuspec
    Write-Host "📝 Updating version to $Version..." -ForegroundColor Yellow
    $nuspecPath = "Vite.MsBuild.nuspec"
    $nuspec = Get-Content $nuspecPath -Raw
    $nuspec = $nuspec -replace '<version>.*?</version>', "<version>$Version</version>"
    Set-Content $nuspecPath -Value $nuspec

    # Create output directory
    if (!(Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }

    # Pack the NuGet package
    Write-Host "📦 Packing NuGet package..." -ForegroundColor Yellow
    nuget pack Vite.MsBuild.nuspec -OutputDirectory $OutputDir

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Package created successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📁 Output: $OutputDir\Vite.MsBuild.$Version.nupkg" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Test locally: dotnet add package Vite.MsBuild --source $((Resolve-Path $OutputDir).Path)"
        Write-Host "  2. Publish: nuget push $OutputDir\Vite.MsBuild.$Version.nupkg -Source nuget.org -ApiKey YOUR_API_KEY"
    } else {
        Write-Host ""
        Write-Host "❌ Package build failed!" -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}
