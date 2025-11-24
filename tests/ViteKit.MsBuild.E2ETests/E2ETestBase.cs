using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.E2ETests;

/// <summary>
/// Base class for E2E tests that execute real MSBuild projects
/// </summary>
public abstract class E2ETestBase : IDisposable
{
    protected readonly ITestOutputHelper Output;
    protected readonly string TestProjectsRoot;
    
    protected E2ETestBase(ITestOutputHelper output)
    {
        Output = output;
        
        // TestProjects are copied to output directory
        var baseDir = AppContext.BaseDirectory;
        TestProjectsRoot = Path.Combine(baseDir, "TestProjects");
        
        if (!Directory.Exists(TestProjectsRoot))
        {
            throw new DirectoryNotFoundException(
                $"TestProjects directory not found at: {TestProjectsRoot}. " +
                "Ensure test projects are being copied to output directory.");
        }
    }

    /// <summary>
    /// Execute dotnet build on a test project and capture output
    /// </summary>
    protected BuildResult ExecuteBuild(string projectName, string configuration = "Debug", string? additionalArgs = null)
    {
        var projectPath = Path.Combine(TestProjectsRoot, projectName);
        var csprojFile = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault();
        
        if (csprojFile == null)
            throw new FileNotFoundException($"No .csproj found in {projectPath}");

        return ExecuteDotnetCommand("build", csprojFile, $"-c {configuration} {additionalArgs}".Trim());
    }

    /// <summary>
    /// Execute dotnet clean on a test project
    /// </summary>
    protected BuildResult ExecuteClean(string projectName)
    {
        var projectPath = Path.Combine(TestProjectsRoot, projectName);
        var csprojFile = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault();
        
        if (csprojFile == null)
            throw new FileNotFoundException($"No .csproj found in {projectPath}");

        return ExecuteDotnetCommand("clean", csprojFile, "");
    }

    /// <summary>
    /// Execute dotnet restore on a test project
    /// </summary>
    protected BuildResult ExecuteRestore(string projectName)
    {
        var projectPath = Path.Combine(TestProjectsRoot, projectName);
        var csprojFile = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault();
        
        if (csprojFile == null)
            throw new FileNotFoundException($"No .csproj found in {projectPath}");

        return ExecuteDotnetCommand("restore", csprojFile, "");
    }

    /// <summary>
    /// Execute a dotnet command and capture output
    /// </summary>
    private BuildResult ExecuteDotnetCommand(string command, string project, string args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{command} \"{project}\" {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(project)!
        };

        var output = new StringBuilder();
        var errors = new StringBuilder();
        var sw = Stopwatch.StartNew();

        using var process = new Process { StartInfo = startInfo };
        
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                Output.WriteLine(e.Data);
            }
        };
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errors.AppendLine(e.Data);
                Output.WriteLine($"ERROR: {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        
        sw.Stop();

        return new BuildResult
        {
            ExitCode = process.ExitCode,
            Output = output.ToString(),
            Errors = errors.ToString(),
            Duration = sw.Elapsed,
            Success = process.ExitCode == 0
        };
    }

    /// <summary>
    /// Check if a file exists in the test project
    /// </summary>
    protected bool FileExists(string projectName, string relativePath)
    {
        var projectPath = Path.Combine(TestProjectsRoot, projectName);
        var fullPath = Path.Combine(projectPath, relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Check if a directory exists in the test project
    /// </summary>
    protected bool DirectoryExists(string projectName, string relativePath)
    {
        var projectPath = Path.Combine(TestProjectsRoot, projectName);
        var fullPath = Path.Combine(projectPath, relativePath);
        return Directory.Exists(fullPath);
    }

    /// <summary>
    /// Get files in a directory within the test project
    /// </summary>
    protected string[] GetFiles(string projectName, string relativePath, string searchPattern = "*.*")
    {
        var projectPath = Path.Combine(TestProjectsRoot, projectName);
        var fullPath = Path.Combine(projectPath, relativePath);
        
        if (!Directory.Exists(fullPath))
            return Array.Empty<string>();
        
        return Directory.GetFiles(fullPath, searchPattern, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(projectPath, f))
            .ToArray();
    }

    public virtual void Dispose()
    {
        // Cleanup can be done here if needed
    }
}

/// <summary>
/// Result of executing a dotnet command
/// </summary>
public class BuildResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Errors { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public bool Success { get; init; }
}
