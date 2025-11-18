using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Vite.MsBuild.Tasks
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
        /// Input: Whether to only validate/build command without executing
        /// </summary>
        public bool ValidateOnly { get; set; }

        /// <summary>
        /// Input: Additional environment variables as semicolon-separated key=value pairs
        /// </summary>
        public string? EnvironmentVariables { get; set; }

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
                Log.LogMessage(MessageImportance.Low, "🔨 Building Vite command for project: {0}", ProjectRoot);

                // Parse package manager
                if (!Enum.TryParse<PackageManager>(PackageManagerName, true, out var packageManager))
                {
                    Log.LogError("Invalid package manager: {0}", PackageManagerName);
                    return false;
                }

                // Build the command
                var builder = new ViteCommandBuilder(ProjectRoot, packageManager)
                    .WithCustomCommand(CustomCommand)
                    .WithConfig(ConfigPath)
                    .WithMode(Mode)
                    .WithOutputDir(OutputDir)
                    .WithLogLevel(LogLevel)
                    .WithColors(EnableColors);

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
                                builder.WithEnvironmentVariable(key, value);
                            }
                        }
                    }
                }

                var command = builder.Build();
                
                // Set outputs
                CommandLine = command.ToString();
                WorkingDirectory = command.WorkingDirectory;

                Log.LogMessage(MessageImportance.Normal, "📋 Command: {0}", CommandLine);
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
                Log.LogMessage(MessageImportance.Normal, "🚀 Executing Vite build...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = command.Executable,
                    Arguments = BuildArguments(command),
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
#if NETSTANDARD2_0 || NET472
                        process.Kill();
#else
                        process.Kill(true);
#endif
                        return false;
                    }
                }

                ExitCode = process.ExitCode;
                ExecutedSuccessfully = ExitCode == 0;

                if (ExecutedSuccessfully)
                {
                    Log.LogMessage(MessageImportance.Normal, "✅ Vite build completed successfully");
                }
                else
                {
                    var errorOutput = errorBuilder.ToString();
                    Log.LogError("❌ Vite build failed with exit code {0}. Error output: {1}", 
                        ExitCode, string.IsNullOrEmpty(errorOutput) ? "No error output" : errorOutput);
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

        private string BuildArguments(Commands.ViteCommand command)
        {
            var args = new List<string> { command.Command };

            if (!string.IsNullOrEmpty(command.ConfigPath))
                args.Add($"--config \"{command.ConfigPath}\"");
            if (!string.IsNullOrEmpty(command.Mode))
                args.Add($"--mode {command.Mode}");
            if (!string.IsNullOrEmpty(command.OutputDir))
                args.Add($"--outDir \"{command.OutputDir}\"");
            if (!string.IsNullOrEmpty(command.LogLevel))
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