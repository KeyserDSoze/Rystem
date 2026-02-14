# Update all test files to use new AddScene signature
$testFiles = @(
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/SimpleCalculatorTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/DynamicActorTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/BudgetLimitTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/DynamicSceneChainingTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/StreamingTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/AdvancedIntegrationTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/InteractiveMultiSceneDemo.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/AzureOpenAIIntegrationTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/McpIntegrationTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/CostTrackingTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/MultiSceneTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/PlayFrameworkServiceTests.cs",
    "src/AI/Test/Rystem.PlayFramework.Test/Tests/LoadBalancingAndFallbackTests.cs"
)

foreach ($file in $testFiles) {
    if (Test-Path $file) {
        Write-Host "Processing $file..."
        $content = Get-Content $file -Raw
        
        # Replace .AddScene(sceneBuilder => with .AddScene("default", "Default scene", sceneBuilder =>
        $content = $content -replace '\.AddScene\(sceneBuilder\s*=>', '.AddScene("default", "Default scene", sceneBuilder =>'
        
        # Replace .AddScene(scene => with .AddScene("default", "Default scene", scene =>
        $content = $content -replace '\.AddScene\(scene\s*=>', '.AddScene("default", "Default scene", scene =>'
        
        # Replace .AddScene(s => with .AddScene("default", "Default scene", s =>
        $content = $content -replace '\.AddScene\(s\s*=>', '.AddScene("default", "Default scene", s =>'
        
        # Replace .AddScene("test-scene", "Test scene"); with .AddScene("test-scene", "Test scene", _ => { });
        $content = $content -replace '\.AddScene\("test-scene",\s*"Test scene"\);', '.AddScene("test-scene", "Test scene", _ => { });'
        
        Set-Content -Path $file -Value $content -NoNewline
        Write-Host "Updated $file"
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "Done!"
