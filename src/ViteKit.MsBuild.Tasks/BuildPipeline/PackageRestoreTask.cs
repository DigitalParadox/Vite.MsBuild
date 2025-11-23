using System.Collections.Generic;

namespace ViteKit.MsBuild.Tasks.BuildPipeline
{
    /// <summary>
    /// Package manager restore task (npm install, pnpm install, etc.)
    /// </summary>
    public class PackageRestoreTask : IBuildTask
    {
        public string TaskId { get; set; } = string.Empty;
        public BuildTaskType Type => BuildTaskType.PackageRestore;
        public string Executable { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public Dictionary<string, string> Environment { get; set; } = new();
        public List<string> DependsOn { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Package manager name (npm, pnpm, yarn, bun)
        /// </summary>
        public string PackageManager { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to package.json being restored
        /// </summary>
        public string PackageJsonPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether lock file exists
        /// </summary>
        public bool HasLockFile { get; set; }
    }
}
