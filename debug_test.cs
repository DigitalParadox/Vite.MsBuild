using System;
using System.IO;
using System.Linq;

var sourceFileExtensions = new string[] {
    "*.ts", "*.tsx", "*.js", "*.jsx", "*.mts", "*.mjs", "*.cts", "*.cjs",
    "*.vue", "*.svelte", "*.solid", "*.astro",
    "*.css", "*.scss", "*.sass", "*.less", "*.styl", "*.stylus", "*.postcss",
    "*.svg", "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.ico", "*.woff", "*.woff2",
    "*.json", "*.yaml", "*.yml", "*.toml", "*.md", "*.html"
};

var testFile = new FileInfo("app.ts");
var extension = testFile.Extension.ToLowerInvariant();
Console.WriteLine($"File: {testFile.Name}, Extension: '{extension}'");

foreach (var pattern in sourceFileExtensions)
{
    var patternExt = pattern.Substring(1); // Remove *
    var matches = extension == patternExt;
    Console.WriteLine($"Pattern: {pattern} -> PatternExt: '{patternExt}' -> Match: {matches}");
    if (matches) break;
}

var shouldMatch = sourceFileExtensions.Any(pattern => 
{
    var patternExt = pattern.Substring(1); // Remove *
    return extension == patternExt;
});

Console.WriteLine($"Final result: {shouldMatch}");