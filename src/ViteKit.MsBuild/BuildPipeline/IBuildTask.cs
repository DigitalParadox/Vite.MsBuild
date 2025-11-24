using System.Collections.Generic;

namespace ViteKit.MsBuild.Tasks.BuildPipeline
{
    /// <summary>
    /// Represents a single build task in the pipeline
    /// Can be package restore, Vite build, or custom command
    /// </summary>
    public interface IBuildTask
    {
        /// <summary>
        /// Unique identifier for this task
        /// </summary>
        string TaskId { get; }
        
        /// <summary>
        /// Type of task
        /// </summary>
        BuildTaskType Type { get; }
        
        /// <summary>
        /// Executable to run (npm, vite, custom command)
        /// </summary>
        string Executable { get; }
        
        /// <summary>
        /// Command arguments
        /// </summary>
        string Arguments { get; }
        
        /// <summary>
        /// Working directory for execution
        /// </summary>
        string WorkingDirectory { get; }
        
        /// <summary>
        /// Environment variables for this task
        /// </summary>
        Dictionary<string, string> Environment { get; }
        
        /// <summary>
        /// Task IDs this task depends on (must complete before this runs)
        /// </summary>
        List<string> DependsOn { get; }
        
        /// <summary>
        /// Description for logging
        /// </summary>
        string Description { get; }
    }
    
    /// <summary>
    /// Types of build tasks
    /// </summary>
    public enum BuildTaskType
    {
        /// <summary>
        /// Package manager restore (npm install, pnpm install, etc.)
        /// </summary>
        PackageRestore,
        
        /// <summary>
        /// Vite build command
        /// </summary>
        ViteBuild,
        
        /// <summary>
        /// Custom user-defined command
        /// </summary>
        Custom
    }
}
