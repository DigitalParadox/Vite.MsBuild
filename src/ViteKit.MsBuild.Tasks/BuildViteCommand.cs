using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
    
namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// MSBuild task that builds Vite commands and optionally executes them
    /// </summary>
    public class BuildViteCommand : Microsoft.Build.Utilities.Task, ICancelableTask
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Input: Project root directory
        /// </summary>
        [Required]
        public string ProjectRoot { get; set; } = string.Empty;

        /// <summary>
        /// Input: Detected package manager
        /// </summary>
        [Required]
        public string PackageManagerName { get; set; } = string.Empty;

        /// <summary>
        /// Input: Custom build command override
        /// </summary>
        public string? CustomCommand { get; set; }

        /// <summary>
        /// Input: Path to Vite config file
        /// </summary>
        public string? ConfigPath { get; set; }

        /// <summary>
        /// Input: Build mode (development, production, etc.)
        /// </summary>
        public string? Mode { get; set; }

        /// <summary>
        /// Input: Output directory
        /// </summary>
        public string? OutputDir { get; set; }

        /// <summary>
        /// Input: Log level for Vite
        /// </summary>
        public string? LogLevel { get; set; }

        /// <summary>
        /// Input: Whether to enable colored output
        /// </summary>
        public bool EnableColors { get; set; } = true;

        /// <summary>
        /// Input: Which package.json script to run (default: "build")
        /// </summary>
        public string? BuildScript { get; set; } = "build";

        /// <summary>
        /// Input: Force direct vite CLI calls, bypass package.json scripts
        /// </summary>
        public bool DirectViteBuild { get; set; }

        /// <summary>
        /// Input: Whether to only validate/build command without executing
        /// </summary>
        public bool ValidateOnly { get; set; }

        /// <summary>
        /// Input: Additional environment variables as semicolon-separated key=value pairs
        /// </summary>
        public string? EnvironmentVariables { get; set; }

        /// <summary>
        /// Input: Enable diagnostic logging
        /// </summary>
        public bool EnableDiagnostics { get; set; }

        /// <summary>
        /// Input: Custom diagnostic log path
        /// </summary>
        public string? DiagnosticLogPath { get; set; }

        /// <summary>
        /// Output: The built command line
        /// </summary>
        [Output]
        public string? CommandLine { get; set; }

        /// <summary>
        /// Output: The working directory for the command
        /// </summary>
        [Output]
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Output: Whether the command was executed successfully
        /// </summary>
        [Output]
        public bool ExecutedSuccessfully { get; set; }

        /// <summary>
        /// Output: Exit code (if executed)
        /// </summary>
        [Output]
        public int ExitCode { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "[BUILD] Building Vite command for project: {0}", ProjectRoot);

                // Parse package manager
                if (!Enum.TryParse<PackageManager>(PackageManagerName, true, out var packageManager))
                {
                    Log.LogError("Invalid package manager: {0}", PackageManagerName);
                    return false;
                }

                // Build configuration from MSBuild properties
                var config = new ViteBuildConfiguration
                {
                    ProjectRoot = ProjectRoot,
                    PackageManager = packageManager,
                    CustomBuildCommand = CustomCommand,
                    BuildScript = BuildScript,
                    DirectViteBuild = DirectViteBuild,
                    ConfigFile = ConfigPath,
                    Mode = Mode,
                    OutputDir = OutputDir,
                    LogLevel = LogLevel,
                    EnableColors = EnableColors,
                    EnableDiagnostics = EnableDiagnostics,
                    DiagnosticLogPath = DiagnosticLogPath
                };

                // Add environment variables from MSBuild (semicolon-separated key=value pairs)
                if (!string.IsNullOrEmpty(EnvironmentVariables))
                {
                    var envPairs = EnvironmentVariables.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var envPair in envPairs)
                    {
                        var equalIndex = envPair.IndexOf('=');
                        if (equalIndex > 0 && equalIndex < envPair.Length - 1)
                        {
                            var key = envPair.Substring(0, equalIndex).Trim();
                            var value = envPair.Substring(equalIndex + 1).Trim();
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                            {
                                config.Environment[key] = value;
                            }
                        }
                    }
                }

                var command = new ViteCommandBuilder(config).Build();
                
                // Set outputs
                CommandLine = command.ToString();
                WorkingDirectory = command.WorkingDirectory;

                Log.LogMessage(MessageImportance.Normal, "* Command: {0}", CommandLine);
                Log.LogMessage(MessageImportance.Low, "📂 Working Directory: {0}", WorkingDirectory);

                // Execute if not validation-only
                if (!ValidateOnly)
                {
                    return ExecuteCommand(command);
                }

                ExecutedSuccessfully = true;
                ExitCode = 0;
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to build/execute Vite command: {0}", ex.Message);
                return false;
            }
        }

        private bool ExecuteCommand(Commands.ViteCommand command)
        {
            try
            {
                Log.LogMessage(MessageImportance.Normal, ">> Executing Vite build...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = command.Executable,
                    Arguments = BuildCommandArguments(command),
                    WorkingDirectory = command.WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Set environment variables
                foreach (var env in command.Environment)
                {
                    startInfo.EnvironmentVariables[env.Key] = env.Value;
                }

                using var process = new Process { StartInfo = startInfo };
                
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                        Log.LogWarning(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for completion or cancellation
                while (!process.WaitForExit(1000))
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Log.LogMessage(MessageImportance.High, "🛑 Vite build cancelled");
                        process.Kill(true); // Kill process tree
                        return false;
                    }
                }

                ExitCode = process.ExitCode;
                ExecutedSuccessfully = ExitCode == 0;

                if (ExecutedSuccessfully)
                {
                    Log.LogMessage(MessageImportance.Normal, "[OK] Vite build completed successfully");
                }
                else
                {
                    var errorOutput = errorBuilder.ToString();
                    Log.LogError("[ERROR] Vite build failed with exit code {0}. Error output: {1}", 
                        ExitCode, errorOutput is { Length: > 0 } ? errorOutput : "No error output");
                }

                return ExecutedSuccessfully;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to execute Vite command: {0}", ex.Message);
                ExitCode = -1;
                ExecutedSuccessfully = false;
                return false;
            }
        }

        private string BuildCommandArguments(Commands.ViteCommand command)
        {
            List<string> args = [command.Command];

            if (command.ConfigPath is { Length: > 0 })
                args.Add($"--config \"{command.ConfigPath}\"");
            if (command.Mode is { Length: > 0 })
                args.Add($"--mode {command.Mode}");
            if (command.OutputDir is { Length: > 0 })
                args.Add($"--outDir \"{command.OutputDir}\"");
            if (command.LogLevel is { Length: > 0 })
                args.Add($"--logLevel {command.LogLevel}");
            
            // Note: Colors are handled via environment variables in command.Environment
            // NO_COLOR=1 (disable) or FORCE_COLOR=1 (enable) - not CLI flags

            return string.Join(" ", args);
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}