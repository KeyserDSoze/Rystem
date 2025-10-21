# Script per aggiungere sezione Resources a tutti i README dei progetti Rystem
# Usage: .\add-resources-to-readmes.ps1

$resourcesSection = @"
## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

"@

$rootPath = (Get-Item $PSScriptRoot).Parent.Parent.FullName

# Lista dei README da aggiornare (escludiamo quelli generati e di test)
$readmePaths = @(
    # Core
    "src\Core\Rystem.DependencyInjection.Web\README.md",
    
    # Authentication
    "src\Authentication\Rystem.Authentication.Social\README.md",
    "src\Authentication\Rystem.Authentication.Social.Blazor\README.md",
    "src\Authentication\rystem.authentication.social.react\README.md",
    
    # API
    "src\Api\Rystem.Api.Server\README.md",
    "src\Api\Rystem.Api.Client\README.md",
    
    # Extensions
    "src\Extensions\Concurrency\Rystem.Concurrency\README.md",
    "src\Extensions\Concurrency\Rystem.Concurrency.Redis\README.md",
    "src\Extensions\BackgroundJob\Rystem.BackgroundJob\README.md",
    "src\Extensions\Queue\Rystem.Queue\README.md",
    
    # Repository
    "src\Repository\RepositoryFramework.Abstractions\README.md",
    "src\Repository\RepositoryFramework.Api.Server\README.md",
    "src\Repository\RepositoryFramework.Api.Client\README.md",
    "src\Repository\RepositoryFramework.Infrastructure.InMemory\README.md",
    "src\Repository\RepositoryFramework.Infrastructures\RepositoryFramework.Infrastructure.EntityFramework\README.md",
    "src\Repository\RepositoryFramework.Infrastructures\RepositoryFramework.Infrastructure.Azure.Cosmos.Sql\README.md",
    "src\Repository\RepositoryFramework.Infrastructures\RepositoryFramework.Infrastructure.Azure.Storage.Blob\README.md",
    "src\Repository\RepositoryFramework.Infrastructures\RepositoryFramework.Infrastructure.Azure.Storage.Table\README.md",
    "src\Repository\RepositoryFramework.Cache\RepositoryFramework.Cache\README.md",
    
    # Content
    "src\Content\Rystem.Content.Abstractions\README.md",
    "src\Content\Rystem.Content.Infrastructure.Storage.Blob\README.md",
    "src\Content\Rystem.Content.Infrastructure.Storage.File\README.md",
    "src\Content\Rystem.Content.Infrastructure.M365.Sharepoint\README.md",
    "src\Content\Rystem.Content.Infrastructure.InMemory\README.md",
    
    # Localization
    "src\Localization\Rystem.Localization\README.md"
)

Write-Host "🚀 Adding Resources section to README files..." -ForegroundColor Cyan
Write-Host ""

$updated = 0
$skipped = 0
$errors = 0

foreach ($relativePath in $readmePaths) {
    $fullPath = Join-Path $rootPath $relativePath
    
    if (!(Test-Path $fullPath)) {
        Write-Host "⚠️  SKIP: $relativePath (file not found)" -ForegroundColor Yellow
        $skipped++
        continue
    }
    
    try {
        $content = Get-Content -Path $fullPath -Raw -Encoding UTF8
        
        # Check if Resources section already exists
        if ($content -match "Resources.*rystem\.net|rystem\.cloud/mcp") {
            Write-Host "SKIP: $relativePath (Resources section already exists)" -ForegroundColor Yellow
            $skipped++
            continue
        }
        
        # Find the first ## heading after the title
        if ($content -match "(?m)^### \[What is Rystem\?\].*?\n\n(## |\z)") {
            # Insert Resources section after title
            $content = $content -replace "(?m)(^### \[What is Rystem\?\].*?\n\n)", "`$1$resourcesSection"
            
            # Write back to file
            Set-Content -Path $fullPath -Value $content -Encoding UTF8 -NoNewline
            
            Write-Host "✅ Updated: $relativePath" -ForegroundColor Green
            $updated++
        }
        else {
            Write-Host "⚠️  SKIP: $relativePath (pattern not found)" -ForegroundColor Yellow
            $skipped++
        }
    }
    catch {
        Write-Host "❌ ERROR: $relativePath - $_" -ForegroundColor Red
        $errors++
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Updated: $updated" -ForegroundColor Green
Write-Host "  Skipped: $skipped" -ForegroundColor Yellow
Write-Host "  Errors: $errors" -ForegroundColor Red
