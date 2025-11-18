using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// Simple, intuitive MSBuild task for defining Vite build configurations.
    /// Can be used as either &lt;ViteConfig&gt; or &lt;Vite.MsBuild.Tasks.ViteConfig&gt;
    /// </summary>
    public class ViteConfig : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Input: Unique name/ID for this configuration
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Input: Path to the Vite config file (vite.config.ts/js)
        /// </summary>
        [Required]
        public string ConfigFile { get; set; } = string.Empty;

        /// <summary>
        /// Input: Output directory for built assets
        /// </summary>
        public string? OutputDir { get; set; }

        /// <summary>
        /// Input: Build mode (development, production, staging, etc.)
        /// </summary>
        public string? Mode { get; set; }

        /// <summary>
        /// Input: Package manager for this config (npm, yarn, pnpm, bun)
        /// </summary>
        public string? PackageManager { get; set; }

        /// <summary>
        /// Input: Log level (silent, error, warn, info, debug)
        /// </summary>
        public string? LogLevel { get; set; }

        /// <summary>
        /// Input: Enable/disable colors in output
        /// </summary>
        public bool? EnableColors { get; set; }

        /// <summary>
        /// Input: Custom build command override
        /// </summary>
        public string? CustomCommand { get; set; }

        /// <summary>
        /// Input: Comma-separated list of other ViteConfig names this build depends on
        /// Build order will be automatically determined based on dependencies
        /// </summary>
        public string? DependsOn { get; set; }

        /// <summary>
        /// Input: Whether to automatically link dependency packages for this build
        /// When true, dependencies that produce npm packages will be linked using file: protocol
        /// </summary>
        public bool? LinkDependencies { get; set; }

        /// <summary>
        /// Output: The created configuration as an ItemGroup item
        /// </summary>
        [Output]
        public ITaskItem? CreatedConfig { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "🔧 Defining Vite config: {0} -> {1}", Name, ConfigFile);

                // Create MSBuild item with all metadata
                var configItem = new TaskItem(ConfigFile);
                configItem.SetMetadata("BuildId", Name);
                
                if (!string.IsNullOrEmpty(OutputDir))
                    configItem.SetMetadata("OutputDir", OutputDir);
                
                if (!string.IsNullOrEmpty(Mode))
                    configItem.SetMetadata("Mode", Mode);
                
                if (!string.IsNullOrEmpty(PackageManager))
                    configItem.SetMetadata("PackageManager", PackageManager);
                
                if (!string.IsNullOrEmpty(LogLevel))
                    configItem.SetMetadata("LogLevel", LogLevel);
                
                if (EnableColors.HasValue)
                    configItem.SetMetadata("EnableColors", EnableColors.Value.ToString().ToLower());
                
                if (!string.IsNullOrEmpty(CustomCommand))
                    configItem.SetMetadata("CustomCommand", CustomCommand);

                if (!string.IsNullOrEmpty(DependsOn))
                    configItem.SetMetadata("DependsOn", DependsOn);

                if (LinkDependencies.HasValue)
                    configItem.SetMetadata("LinkDependencies", LinkDependencies.Value.ToString().ToLower());

                // Output the created configuration
                CreatedConfig = configItem;

                Log.LogMessage(MessageImportance.Normal, "✅ Vite config '{0}' defined successfully", Name);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to define Vite config '{0}': {1}", Name, ex.Message);
                return false;
            }
        }
    }
}