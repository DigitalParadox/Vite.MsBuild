using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// MSBuild task for defining Vite configurations using clean task syntax
    /// instead of verbose ItemGroup definitions
    /// </summary>
    public class DefineViteConfig : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Input: Unique name/ID for this configuration
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Input: Path to the Vite config file
        /// </summary>
        [Required]
        public string Config { get; set; } = string.Empty;

        /// <summary>
        /// Input: Output directory for built assets
        /// </summary>
        public string? OutputDir { get; set; }

        /// <summary>
        /// Input: Build mode (development, production, staging, etc.)
        /// </summary>
        public string? Mode { get; set; }

        /// <summary>
        /// Input: Custom package manager for this config
        /// </summary>
        public string? PackageManager { get; set; }

        /// <summary>
        /// Input: Custom log level for this config
        /// </summary>
        public string? LogLevel { get; set; }

        /// <summary>
        /// Input: Enable/disable colors for this config
        /// </summary>
        public bool? EnableColors { get; set; }

        /// <summary>
        /// Input: Custom build command for this config
        /// </summary>
        public string? CustomCommand { get; set; }

        /// <summary>
        /// Output: The created configuration as an item for use by build targets
        /// </summary>
        [Output]
        public ITaskItem[]? CreatedConfigurations { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "🔧 Defining Vite config: {0} -> {1}", Name, Config);

                // Create MSBuild item with all metadata
                var configItem = new TaskItem(Config);
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

                // Output the created configuration
                CreatedConfigurations = new[] { configItem };

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

    /// <summary>
    /// MSBuild task for bulk configuration of multiple Vite configs
    /// </summary>
    public class ConfigureViteConfigs : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Input: Multiple configuration definitions
        /// </summary>
        [Required]
        public ITaskItem[] ConfigDefinitions { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Input: Global defaults to apply to all configs
        /// </summary>
        public ITaskItem? GlobalDefaults { get; set; }

        /// <summary>
        /// Output: All configured Vite configurations ready for build
        /// </summary>
        [Output]
        public ITaskItem[]? ConfiguredViteConfigs { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Normal, "🔧 Configuring {0} Vite configurations", ConfigDefinitions.Length);

                var configuredItems = new List<ITaskItem>();

                // Apply global defaults if provided
                var globalDefaults = ExtractDefaults(GlobalDefaults);

                foreach (var configDef in ConfigDefinitions)
                {
                    var configPath = configDef.ItemSpec;
                    var buildId = configDef.GetMetadata("BuildId");

                    if (string.IsNullOrEmpty(buildId))
                    {
                        Log.LogError("BuildId is required for config: {0}", configPath);
                        return false;
                    }

                    // Create configured item
                    var configuredItem = new TaskItem(configPath);
                    configuredItem.SetMetadata("BuildId", buildId);

                    // Apply global defaults first
                    ApplyDefaults(configuredItem, globalDefaults);

                    // Apply specific config metadata (overrides defaults)
                    foreach (string metadataName in configDef.MetadataNames)
                    {
                        if (metadataName != "BuildId") // Already set
                        {
                            var value = configDef.GetMetadata(metadataName);
                            if (!string.IsNullOrEmpty(value))
                            {
                                configuredItem.SetMetadata(metadataName, value);
                            }
                        }
                    }

                    configuredItems.Add(configuredItem);
                    Log.LogMessage(MessageImportance.Low, "✅ Configured: {0} ({1})", buildId, configPath);
                }

                ConfiguredViteConfigs = configuredItems.ToArray();
                Log.LogMessage(MessageImportance.Normal, "✅ Successfully configured {0} Vite configurations", configuredItems.Count);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to configure Vite configs: {0}", ex.Message);
                return false;
            }
        }

        private Dictionary<string, string> ExtractDefaults(ITaskItem? defaultsItem)
        {
            var defaults = new Dictionary<string, string>();
            
            if (defaultsItem != null)
            {
                foreach (string metadataName in defaultsItem.MetadataNames)
                {
                    var value = defaultsItem.GetMetadata(metadataName);
                    if (!string.IsNullOrEmpty(value))
                    {
                        defaults[metadataName] = value;
                    }
                }
            }

            return defaults;
        }

        private void ApplyDefaults(ITaskItem item, Dictionary<string, string> defaults)
        {
            foreach (var kvp in defaults)
            {
                if (string.IsNullOrEmpty(item.GetMetadata(kvp.Key)))
                {
                    item.SetMetadata(kvp.Key, kvp.Value);
                }
            }
        }
    }
}