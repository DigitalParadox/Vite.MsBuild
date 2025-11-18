using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// High-level builder for Vite commands using factory pattern
    /// Automatically determines the best command type based on project configuration
    /// </summary>
    public class ViteCommandBuilder
    {
        private readonly string _projectRoot;
        private readonly PackageManager _packageManager;
        private readonly IViteCommandFactory _commandFactory;
        
        private string? _customCommand;
        private string? _configPath;
        private string? _mode;
        private string? _outputDir;
        private string? _logLevel;
        private bool? _enableColors;
        private readonly Dictionary<string, string> _environment = new Dictionary<string, string>();

        public ViteCommandBuilder(string projectRoot, PackageManager packageManager)
            : this(projectRoot, packageManager, new ViteCommandFactory())
        {
        }

        public ViteCommandBuilder(string projectRoot, PackageManager packageManager, IViteCommandFactory commandFactory)
        {
            _projectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
            _packageManager = packageManager;
            _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
        }

        public ViteCommandBuilder WithCustomCommand(string? command)
        {
            _customCommand = command;
            return this;
        }

        public ViteCommandBuilder WithConfig(string? configPath)
        {
            _configPath = configPath;
            return this;
        }

        public ViteCommandBuilder WithMode(string? mode)
        {
            _mode = mode;
            return this;
        }

        public ViteCommandBuilder WithOutputDir(string? outputDir)
        {
            _outputDir = outputDir;
            return this;
        }

        public ViteCommandBuilder WithLogLevel(string? logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        public ViteCommandBuilder WithColors(bool? enableColors)
        {
            _enableColors = enableColors;
            return this;
        }

        public ViteCommandBuilder WithEnvironmentVariable(string key, string value)
        {
            _environment[key] = value;
            return this;
        }

        /// <summary>
        /// Builds the Vite command using the appropriate command type based on project configuration
        /// </summary>
        public IViteCommand BuildCommand()
        {
            var commandType = DetermineCommandType();
            var builder = _commandFactory.CreateBuilder(commandType, _packageManager);

            return builder
                .WithConfigPath(_configPath)
                .WithMode(_mode)
                .WithOutputDir(_outputDir)
                .WithLogLevel(_logLevel)
                .WithColors(_enableColors)
                .WithEnvironment(_environment)
                .WithWorkingDirectory(_projectRoot)
                .WithCustomCommand(_customCommand)
                .Build();
        }

        /// <summary>
        /// Legacy method for backward compatibility - returns the old ViteCommand format
        /// </summary>
        public Commands.ViteCommand Build()
        {
            var command = BuildCommand();
            
            // Convert IViteCommand to legacy ViteCommand format for backward compatibility
            return new Commands.ViteCommand
            {
#if NET6_0_OR_GREATER
                Executable = command.Executable,
                Command = command.Command,
                ConfigPath = command.ConfigPath,
                Mode = command.Mode,
                OutputDir = command.OutputDir,
                LogLevel = command.LogLevel,
                Colors = command.Colors,
                Environment = command.Environment,
                WorkingDirectory = command.WorkingDirectory
#else
                Executable = command.Executable,
                Command = command.Command,
                ConfigPath = command.ConfigPath,
                Mode = command.Mode,
                OutputDir = command.OutputDir,
                LogLevel = command.LogLevel,
                Colors = command.Colors,
                Environment = command.Environment,
                WorkingDirectory = command.WorkingDirectory
#endif
            };
        }

        private ViteCommandType DetermineCommandType()
        {
            // Priority 1: Custom command override
            if (!string.IsNullOrEmpty(_customCommand))
                return ViteCommandType.CustomCommand;

            // Priority 2: Script-based if build script exists
            if (HasBuildScript())
                return ViteCommandType.ScriptBased;

            // Priority 3: Direct tool execution as fallback
            return ViteCommandType.DirectTool;
        }

        private bool HasBuildScript()
        {
            try
            {
                var packageJsonPath = Path.Combine(_projectRoot, "package.json");
                if (!File.Exists(packageJsonPath))
                    return false;

                var jsonContent = File.ReadAllText(packageJsonPath);
                var packageJson = JsonSerializer.Deserialize<PackageJson>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return packageJson?.Scripts?.ContainsKey("build") == true;
            }
            catch (JsonException)
            {
                // Invalid package.json - fall through to direct command
                return false;
            }
        }

        // Keep the remaining methods from the original ViteCommandBuilder for compatibility
        // These will be removed/refactored in a future iteration

        public ViteCommandBuilder WithEnvironmentVariables(Dictionary<string, string>? environmentVariables)
        {
            if (environmentVariables != null)
            {
                foreach (var kvp in environmentVariables)
                {
                    _environment[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }
    }

    /// <summary>
    /// Internal class for deserializing package.json files
    /// </summary>
    internal class PackageJson
    {
        [JsonPropertyName("scripts")]
        public Dictionary<string, string>? Scripts { get; set; }
    }
}