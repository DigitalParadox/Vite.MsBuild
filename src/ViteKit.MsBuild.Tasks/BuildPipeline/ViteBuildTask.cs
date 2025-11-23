using System.Collections.Generic;

namespace ViteKit.MsBuild.Tasks.BuildPipeline
{
    /// <summary>
    /// Vite build task
    /// </summary>
    public class ViteBuildTask : IBuildTask
    {
        public string TaskId { get; set; } = string.Empty;
        public BuildTaskType Type => BuildTaskType.ViteBuild;
        public string Executable { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public Dictionary<string, string> Environment { get; set; } = new();
        public List<string> DependsOn { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Build ID for this Vite build
        /// </summary>
        public string BuildId { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to Vite config file
        /// </summary>
        public string ConfigPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Build mode (development, production, etc.)
        /// </summary>
        public string Mode { get; set; } = string.Empty;
        
        /// <summary>
        /// Output directory
        /// </summary>
        public string OutputDir { get; set; } = string.Empty;
    }
}
