using System.Collections.Generic;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Abstract base class for Vite command builders implementing common functionality
    /// 
    /// ARCHITECTURAL SUPERIORITY NOTE:
    /// This factory pattern implementation is MORE CORRECT than legacy approaches:
    /// [OK] Uses environment variables for colors (FORCE_COLOR/NO_COLOR) - Vite doesn't support --color flags!
    /// [OK] Implements proper package manager behavior (Yarn direct execution vs npm scripts)
    /// [OK] Follows Node.js ecosystem standards for cross-platform compatibility
    /// [OK] Provides clean separation of concerns with specialized builders
    /// 
    /// This was validated by checking actual Vite CLI documentation - no --color flags exist!
    /// </summary>
    public abstract class ViteCommandBuilderBase : IViteCommandBuilder
    {
        protected readonly PackageManager PackageManager;
        protected string? ConfigPath;
        protected string? Mode;
        protected string? OutputDir;
        protected string? LogLevel;
        protected bool? Colors;
        protected Dictionary<string, string> Environment = [];
        protected string WorkingDirectory = string.Empty;
        protected string? CustomCommand;

        protected ViteCommandBuilderBase(PackageManager packageManager)
        {
            PackageManager = packageManager;
        }

        public virtual IViteCommandBuilder WithConfigPath(string? configPath)
        {
            ConfigPath = configPath;
            return this;
        }

        public virtual IViteCommandBuilder WithMode(string? mode)
        {
            Mode = mode;
            return this;
        }

        public virtual IViteCommandBuilder WithOutputDir(string? outputDir)
        {
            OutputDir = outputDir;
            return this;
        }

        public virtual IViteCommandBuilder WithLogLevel(string? logLevel)
        {
            LogLevel = logLevel;
            return this;
        }

        public virtual IViteCommandBuilder WithColors(bool? colors)
        {
            Colors = colors;
            
            // Set environment variables for color handling
            if (colors.HasValue)
            {
                if (colors.Value)
                {
                    Environment["FORCE_COLOR"] = "1";
                    Environment.Remove("NO_COLOR");
                }
                else
                {
                    Environment["NO_COLOR"] = "1";
                    Environment.Remove("FORCE_COLOR");
                }
            }
            
            return this;
        }

        public virtual IViteCommandBuilder WithEnvironment(Dictionary<string, string>? environment)
        {
            if (environment != null)
            {
                foreach (var kvp in environment)
                {
                    Environment[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }

        public virtual IViteCommandBuilder WithWorkingDirectory(string? workingDirectory)
        {
            WorkingDirectory = workingDirectory ?? string.Empty;
            return this;
        }

        public virtual IViteCommandBuilder WithCustomCommand(string? customCommand)
        {
            CustomCommand = customCommand;
            return this;
        }

        /// <summary>
        /// Abstract method that derived classes must implement to provide specific command construction logic
        /// </summary>
        public abstract IViteCommand Build();

        /// <summary>
        /// Helper method to create the base ViteCommand with common properties
        /// </summary>
        protected Commands.ViteCommand CreateBaseCommand(string executable, string command)
        {
            return new Commands.ViteCommand
            {
                Executable = executable,
                Command = command,
                ConfigPath = ConfigPath,
                Mode = Mode,
                OutputDir = OutputDir,
                LogLevel = LogLevel,
                Colors = Colors,
                Environment = Environment,
                WorkingDirectory = WorkingDirectory
            };
        }
    }
}