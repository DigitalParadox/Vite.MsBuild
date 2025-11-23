using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// High-level builder for Vite commands using factory pattern
    /// Automatically determines the best command type based on project configuration
    /// Mockable via IViteBuildConfigurationProvider for testing
    /// </summary>
    public class ViteCommandBuilder
    {
        private readonly ViteBuildConfiguration _config;
        private readonly IViteCommandFactory _commandFactory;

        /// <summary>
        /// Primary constructor - accepts configuration object directly
        /// </summary>
        public ViteCommandBuilder(ViteBuildConfiguration config)
            : this(config, new ViteCommandFactory())
        {
        }

        /// <summary>
        /// Constructor with factory injection for testing
        /// </summary>
        public ViteCommandBuilder(ViteBuildConfiguration config, IViteCommandFactory commandFactory)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
        }

        /// <summary>
        /// Constructor with configuration provider for testing/mocking
        /// </summary>
        public ViteCommandBuilder(IViteBuildConfigurationProvider configProvider)
            : this(configProvider.GetConfiguration() ?? throw new ArgumentNullException(nameof(configProvider)), new ViteCommandFactory())
        {
        }

        /// <summary>
        /// Constructor with both provider and factory for full testability
        /// </summary>
        public ViteCommandBuilder(IViteBuildConfigurationProvider configProvider, IViteCommandFactory commandFactory)
        {
            _config = configProvider.GetConfiguration() ?? throw new ArgumentNullException(nameof(configProvider));
            _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
        }

        /// <summary>
        /// Builds the Vite command using the appropriate command type based on configuration
        /// </summary>
        public IViteCommand BuildCommand()
        {
            var commandType = DetermineCommandType();
            
            // Use specialized factory method for script-based with custom script name
            IViteCommandBuilder builder;
            if (commandType == ViteCommandType.ScriptBased)
            {
                var scriptName = _config.BuildScript ?? "build";
                builder = _commandFactory.CreateScriptBuilder(_config.PackageManager, scriptName);
            }
            else
            {
                builder = _commandFactory.CreateBuilder(commandType, _config.PackageManager);
            }

            return builder
                .WithConfigPath(_config.ConfigFile)
                .WithMode(_config.Mode)
                .WithOutputDir(_config.OutputDir)
                .WithLogLevel(_config.LogLevel)
                .WithColors(_config.EnableColors)
                .WithEnvironment(_config.Environment)
                .WithWorkingDirectory(_config.ProjectRoot)
                .WithCustomCommand(_config.CustomBuildCommand)
                .Build();
        }

        /// <summary>
        /// Legacy method for backward compatibility - returns the old ViteCommand format
        /// </summary>
        public Commands.ViteCommand Build()
        {
            var command = BuildCommand();
            
            // Convert IViteCommand to legacy ViteCommand format
            return new Commands.ViteCommand
            {
                Executable = command.Executable,
                Command = command.Command,
                ConfigPath = command.ConfigPath,
                Mode = command.Mode,
                OutputDir = command.OutputDir,
                LogLevel = command.LogLevel,
                Colors = command.Colors,
                Environment = command.Environment,
                WorkingDirectory = command.WorkingDirectory
            };
        }

        private ViteCommandType DetermineCommandType()
        {
            // Priority 1: Custom command override (ViteBuildCommand property)
            if (_config.CustomBuildCommand is { Length: > 0 })
                return ViteCommandType.CustomCommand;

            // Priority 2: DirectViteBuild flag - force direct vite calls
            if (_config.DirectViteBuild)
                return ViteCommandType.DirectTool;

            // Priority 3: Empty ViteBuildScript - explicit opt-out of scripts
            // Setting ViteBuildScript="" forces direct vite build
            if (_config.BuildScript is { Length: 0 })
                return ViteCommandType.DirectTool;

            // Priority 4: Script-based if build script exists (OOBE default)
            // Uses ViteBuildScript value (default: "build", future: "dev")
            var scriptName = _config.BuildScript ?? "build";
            if (HasBuildScript(scriptName))
                return ViteCommandType.ScriptBased;

            // Priority 5: Direct tool execution as fallback (no script found)
            return ViteCommandType.DirectTool;
        }

        private bool HasBuildScript(string scriptName)
        {
            try
            {
                var packageJsonPath = Path.Combine(_config.ProjectRoot, "package.json");
                if (!File.Exists(packageJsonPath))
                    return false;

                var jsonContent = File.ReadAllText(packageJsonPath);
                var packageJson = JsonSerializer.Deserialize<PackageJson>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return packageJson?.Scripts?.ContainsKey(scriptName) == true;
            }
            catch (JsonException)
            {
                // Invalid package.json - fall through to direct command
                return false;
            }
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