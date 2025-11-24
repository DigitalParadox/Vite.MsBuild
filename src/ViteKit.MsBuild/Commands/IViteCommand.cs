using System.Collections.Generic;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Represents a complete Vite command with all parameters
    /// </summary>
    public interface IViteCommand
    {
        /// <summary>
        /// The executable to run (e.g., "npm", "npx", "bunx")
        /// </summary>
        string Executable { get; }
        
        /// <summary>
        /// The command arguments (e.g., "run build", "vite build")
        /// </summary>
        string Command { get; }
        
        /// <summary>
        /// Path to the Vite configuration file
        /// </summary>
        string? ConfigPath { get; }
        
        /// <summary>
        /// Vite build mode (development, production, etc.)
        /// </summary>
        string? Mode { get; }
        
        /// <summary>
        /// Output directory for build artifacts
        /// </summary>
        string? OutputDir { get; }
        
        /// <summary>
        /// Vite log level (silent, error, warn, info, debug)
        /// </summary>
        string? LogLevel { get; }
        
        /// <summary>
        /// Whether to enable colored output (handled via environment variables)
        /// </summary>
        bool? Colors { get; }
        
        /// <summary>
        /// Environment variables to set for the command
        /// </summary>
        Dictionary<string, string> Environment { get; }
        
        /// <summary>
        /// Working directory for command execution
        /// </summary>
        string WorkingDirectory { get; }
        
        /// <summary>
        /// Returns the full command line string
        /// </summary>
        string ToString();
    }

    /// <summary>
    /// Factory for creating specific types of Vite command builders
    /// </summary>
    public interface IViteCommandFactory
    {
        /// <summary>
        /// Creates a command builder for the specified command type and package manager
        /// </summary>
        IViteCommandBuilder CreateBuilder(ViteCommandType commandType, PackageManager packageManager);
        
        /// <summary>
        /// Creates a script-based builder with a specific script name
        /// </summary>
        IViteCommandBuilder CreateScriptBuilder(PackageManager packageManager, string scriptName);
    }

    /// <summary>
    /// Builder pattern interface for constructing Vite commands
    /// </summary>
    public interface IViteCommandBuilder
    {
        /// <summary>
        /// Sets the Vite configuration file path
        /// </summary>
        IViteCommandBuilder WithConfigPath(string? configPath);
        
        /// <summary>
        /// Sets the build mode
        /// </summary>
        IViteCommandBuilder WithMode(string? mode);
        
        /// <summary>
        /// Sets the output directory
        /// </summary>
        IViteCommandBuilder WithOutputDir(string? outputDir);
        
        /// <summary>
        /// Sets the log level
        /// </summary>
        IViteCommandBuilder WithLogLevel(string? logLevel);
        
        /// <summary>
        /// Sets whether to enable colored output
        /// </summary>
        IViteCommandBuilder WithColors(bool? colors);
        
        /// <summary>
        /// Sets environment variables
        /// </summary>
        IViteCommandBuilder WithEnvironment(Dictionary<string, string>? environment);
        
        /// <summary>
        /// Sets the working directory
        /// </summary>
        IViteCommandBuilder WithWorkingDirectory(string? workingDirectory);
        
        /// <summary>
        /// Sets a custom command override (for CustomCommandBuilder)
        /// </summary>
        IViteCommandBuilder WithCustomCommand(string? customCommand);
        
        /// <summary>
        /// Builds the final ViteCommand
        /// </summary>
        IViteCommand Build();
    }

    /// <summary>
    /// Types of Vite commands that can be built
    /// </summary>
    public enum ViteCommandType
    {
        /// <summary>
        /// Uses package.json scripts (npm run build, yarn build)
        /// </summary>
        ScriptBased,
        
        /// <summary>
        /// Uses direct tool execution (npx vite, bunx vite)
        /// </summary>
        DirectTool,
        
        /// <summary>
        /// Uses user-specified custom command
        /// </summary>
        CustomCommand
    }
}