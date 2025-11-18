using System;
using System.IO;
using System.Collections.Generic;
using Vite.MsBuild.Tasks;

// Demo: How the C# Command Builder Simplifies Everything

// OLD WAY: Complex MSBuild XML with nested conditions
/*
<PropertyGroup>
  <_ViteCommand>$(ViteBuildCommand)</_ViteCommand>
  <_ViteCommand Condition="'$(_ViteCommand)' == ''">$(PackageManagerBuildCommand)</_ViteCommand>
  <_ViteCommand Condition="'$(_ViteCommand)' == '' AND '$(ViteUseDlxFallback)' == 'true'">$(DlxCommand)</_ViteCommand>
  <_ViteArgs>--config "$(ViteSingleConfigFile)" --mode $(ViteSingleMode) --logLevel $(ViteLogLevel)</_ViteArgs>
  <_ViteArgs Condition="'$(ViteEnableColors)' == 'false'">$(_ViteArgs) --clearScreen false</_ViteArgs>
</PropertyGroup>
*/

// NEW WAY: Clean, testable C# code
class Program
{
    static void Main()
    {
        Console.WriteLine("🎯 Vite.MsBuild C# Command Builder Demo\n");

        // Demo 1: Multi-package manager support
        Console.WriteLine("📦 Multi-Package Manager Support:");
        TestPackageManagers();

        Console.WriteLine("\n🔄 Command Fallback Strategy:");
        TestCommandFallback();

        Console.WriteLine("\n🛠️  Build Configuration:");
        TestBuildConfiguration();
    }

    static void TestPackageManagers()
    {
        var tempDir = Path.GetTempPath();
        var packageManagers = new[] { PackageManager.Npm, PackageManager.Yarn, PackageManager.Pnpm, PackageManager.Bun };

        foreach (var pm in packageManagers)
        {
            var builder = new ViteCommandBuilder(tempDir, pm)
                .WithMode("production")
                .WithLogLevel("info");

            var command = builder.Build();
            Console.WriteLine($"  {pm}: {command}");
        }
    }

    static void TestCommandFallback()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ViteDemo_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Custom command (highest priority)
            var customBuilder = new ViteCommandBuilder(tempDir, PackageManager.Npm)
                .WithCustomCommand("yarn build:custom")
                .WithMode("staging");
            Console.WriteLine($"  Custom: {customBuilder.Build()}");

            // 2. Package script (with package.json)
            CreatePackageJson(tempDir, new() { ["build"] = "vite build" });
            var scriptBuilder = new ViteCommandBuilder(tempDir, PackageManager.Npm)
                .WithMode("development");
            Console.WriteLine($"  Script: {scriptBuilder.Build()}");

            // 3. Direct fallback (no package.json)
            File.Delete(Path.Combine(tempDir, "package.json"));
            var fallbackBuilder = new ViteCommandBuilder(tempDir, PackageManager.Npm)
                .WithMode("production");
            Console.WriteLine($"  Fallback: {fallbackBuilder.Build()}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    static void TestBuildConfiguration()
    {
        var tempDir = Path.GetTempPath();

        var builder = new ViteCommandBuilder(tempDir, PackageManager.Npm)
            .WithConfig("vite.config.ts")
            .WithMode("production")
            .WithOutputDir("dist/production")
            .WithLogLevel("silent")
            .WithColors(false)
            .WithEnvironmentVariable("NODE_ENV", "production")
            .WithEnvironmentVariable("VITE_API_URL", "https://api.production.com");

        var command = builder.Build();
        
        Console.WriteLine($"  Command: {command}");
        Console.WriteLine($"  Working Dir: {command.WorkingDirectory}");
        Console.WriteLine($"  Environment Variables:");
        foreach (var env in command.Environment)
        {
            Console.WriteLine($"    {env.Key} = {env.Value}");
        }
    }

    static void CreatePackageJson(string directory, Dictionary<string, string> scripts)
    {
        var packageJson = new
        {
            name = "demo-project",
            scripts = scripts
        };

        var json = System.Text.Json.JsonSerializer.Serialize(packageJson, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(directory, "package.json"), json);
    }
}

/*
🎯 Key Benefits of C# Command Builder:

✅ TESTABILITY:
   - Pure C# logic = easy unit testing
   - No MSBuild execution required for testing
   - Full code coverage possible

✅ MAINTAINABILITY: 
   - Clear conditional logic vs nested MSBuild XML
   - Proper error handling with try/catch
   - IntelliSense support

✅ FLEXIBILITY:
   - Easy to add new package managers
   - Simple to extend with new features
   - Environment variables handled cleanly

✅ RELIABILITY:
   - Compile-time validation
   - Type safety throughout
   - Better error messages

✅ PERFORMANCE:
   - Command construction is fast
   - No MSBuild evaluation overhead for logic
   - Can cache commands if needed

OLD MSBuild XML approach required:
- 50+ lines of complex conditional logic
- Difficult to test
- Hard to debug
- Prone to escaping issues

NEW C# approach:
- 20 lines of clear logic
- 100% testable
- Easy to debug
- Type-safe
*/