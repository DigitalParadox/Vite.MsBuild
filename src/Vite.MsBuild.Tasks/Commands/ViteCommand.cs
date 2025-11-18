using System.Collections.Generic;
using System.Linq;

namespace Vite.MsBuild.Tasks.Commands
{
    /// <summary>
    /// Concrete implementation of IViteCommand representing a complete Vite build command
    /// </summary>
    public class ViteCommand : IViteCommand
    {
#if NET6_0_OR_GREATER
        // Modern C# features for newer frameworks
        public string Executable { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public string? ConfigPath { get; init; }
        public string? Mode { get; init; }
        public string? OutputDir { get; init; }
        public string? LogLevel { get; init; }
        public bool? Colors { get; init; }
        public Dictionary<string, string> Environment { get; init; } = new();
        public string WorkingDirectory { get; init; } = string.Empty;
#else
        // Backwards-compatible properties for older frameworks
        public string Executable { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string ConfigPath { get; set; }
        public string Mode { get; set; }
        public string OutputDir { get; set; }
        public string LogLevel { get; set; }
        public bool? Colors { get; set; }
        public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
        public string WorkingDirectory { get; set; } = string.Empty;
#endif

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