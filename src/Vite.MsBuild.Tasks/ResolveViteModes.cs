using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// Resolves effective Vite modes for multi-config scenarios with override hierarchy
    /// </summary>
    public class ResolveViteModes : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Input: ViteConfigs ItemGroup with BuildId and Mode metadata
        /// </summary>
        [Required]
        public ITaskItem[] ViteConfigs { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Input: Global ViteMode property (optional)
        /// </summary>
        public string? ViteMode { get; set; }

        /// <summary>
        /// Input: MSBuild properties for config-specific overrides
        /// Format: PropertyName=PropertyValue (e.g., "AdminViteMode=production")
        /// </summary>
        public ITaskItem[] Properties { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Input: Default mode for fallback (usually from Configuration)
        /// </summary>
        public string DefaultMode { get; set; } = "development";

        /// <summary>
        /// Output: ViteConfigs with resolved EffectiveMode metadata
        /// </summary>
        [Output]
        public ITaskItem[]? ResolvedConfigs { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "🔧 Resolving Vite modes for {0} configurations", ViteConfigs.Length);

                var resolvedConfigs = new List<ITaskItem>();
                var propertyMap = BuildPropertyMap();

                foreach (var config in ViteConfigs)
                {
                    var buildId = config.GetMetadata("BuildId");
                    var itemGroupMode = config.GetMetadata("Mode");
                    
                    var effectiveMode = ResolveMode(buildId, itemGroupMode, propertyMap);
                    
                    Log.LogMessage(MessageImportance.Normal, 
                        "📋 Config '{0}' → Mode: {1}", buildId, effectiveMode);

                    // Clone the item and add EffectiveMode metadata
                    var resolvedConfig = new TaskItem(config);
                    resolvedConfig.SetMetadata("EffectiveMode", effectiveMode);
                    resolvedConfigs.Add(resolvedConfig);
                }

                ResolvedConfigs = resolvedConfigs.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to resolve Vite modes: {0}", ex.Message);
                return false;
            }
        }

        private Dictionary<string, string> BuildPropertyMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var prop in Properties)
            {
                var name = prop.ItemSpec;
                var value = prop.GetMetadata("Value");
                if (!string.IsNullOrEmpty(value))
                {
                    map[name] = value;
                }
            }
            
            return map;
        }

        private string ResolveMode(string buildId, string itemGroupMode, Dictionary<string, string> propertyMap)
        {
            // Override Hierarchy (highest to lowest priority):
            
            // 1. Config-specific property (e.g., AdminViteMode, CustomerViteMode)
            var configSpecificProperty = $"{buildId}ViteMode";
            if (propertyMap.TryGetValue(configSpecificProperty, out var configSpecificMode) && 
                !string.IsNullOrEmpty(configSpecificMode))
            {
                Log.LogMessage(MessageImportance.Low, 
                    "✅ Using config-specific property '{0}' = {1}", configSpecificProperty, configSpecificMode);
                return configSpecificMode;
            }

            // 2. Global ViteMode property
            if (!string.IsNullOrEmpty(ViteMode))
            {
                Log.LogMessage(MessageImportance.Low, 
                    "✅ Using global ViteMode = {0}", ViteMode);
                return ViteMode;
            }

            // 3. ItemGroup Mode metadata
            if (!string.IsNullOrEmpty(itemGroupMode))
            {
                Log.LogMessage(MessageImportance.Low, 
                    "✅ Using ItemGroup Mode = {0}", itemGroupMode);
                return itemGroupMode;
            }

            // 4. Fallback to default
            Log.LogMessage(MessageImportance.Low, 
                "✅ Using default mode = {0}", DefaultMode);
            return DefaultMode;
        }
    }
}