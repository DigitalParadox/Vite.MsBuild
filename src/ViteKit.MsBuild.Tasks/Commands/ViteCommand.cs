using System.Collections.Generic;
using System.Linq;

namespace ViteKit.MsBuild.Tasks.Commands
{
    /// <summary>
    /// Concrete implementation of IViteCommand representing a complete Vite build command
    /// </summary>
    public class ViteCommand : IViteCommand
    {
        public string Executable { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public string? ConfigPath { get; init; }
        public string? Mode { get; init; }
        public string? OutputDir { get; init; }
        public string? LogLevel { get; init; }
        public bool? Colors { get; init; }
        public Dictionary<string, string> Environment { get; init; } = new();
        public string WorkingDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Returns the full command line string
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string> { Executable };
            
            if (!string.IsNullOrEmpty(Command))
                parts.Add(Command);
            
            // Add Vite-specific arguments
            if (!string.IsNullOrEmpty(ConfigPath))
                parts.Add($"--config \"{ConfigPath}\"");
            if (!string.IsNullOrEmpty(Mode))
                parts.Add($"--mode {Mode}");
            if (!string.IsNullOrEmpty(OutputDir))
                parts.Add($"--outDir \"{OutputDir}\"");
            if (!string.IsNullOrEmpty(LogLevel))
                parts.Add($"--logLevel {LogLevel}");
            
            // Note: Colors are handled via environment variables (FORCE_COLOR=1 or NO_COLOR=1)
            // not through command line flags
            
            return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}