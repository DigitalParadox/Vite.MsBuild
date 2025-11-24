using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Orchestrates Vite builds by generating a pipeline of tasks for MSBuild to execute
    /// NEW ARCHITECTURE: Only generates task pipeline, MSBuild handles execution via targets file
    /// </summary>
    public class OrchestrateBuildTask : Task
    {
        [Required]
        public string ViteProjectRoot { get; set; } = string.Empty;

        [Required]
        public string PackageManager { get; set; } = "npm";

        [Required]
        public string ViteMode { get; set; } = "development";

        public string ViteOutputDir { get; set; } = "wwwroot/dist";

        public bool ViteEnableColors { get; set; } = true;

        public string ViteLogLevel { get; set; } = "info";

        public ITaskItem[]? ViteConfigurations { get; set; }

        [Output]
        public ITaskItem[]? RestoreTasks { get; set; }

        [Output]
        public ITaskItem[]? BuildTasks { get; set; }

        [Output]
        public string[] OutputDirectories { get; set; } = Array.Empty<string>();

        [Output]
        public int ConfigurationsBuilt { get; set; }

        [Output]
        public bool BuildSucceeded { get; set; }

        public override bool Execute()
        {
            try
            {
                // Validate inputs
                if (ViteConfigurations == null || ViteConfigurations.Length == 0)
                {
                    Log.LogMessage(MessageImportance.Normal, "[SKIP] No ViteConfig items specified - nothing to build");
                    RestoreTasks = Array.Empty<ITaskItem>();
                    BuildTasks = Array.Empty<ITaskItem>();
                    BuildSucceeded = true;
                    return true;
                }

                // Convert ViteConfig items to ViteConfigInfo objects
                var configs = ViteConfigurations.Select(config => {
                    var effectiveMode = config.GetMetadata("EffectiveMode");
                    var configMode = config.GetMetadata("Mode");
                    var mode = !string.IsNullOrEmpty(effectiveMode) ? effectiveMode 
                             : !string.IsNullOrEmpty(configMode) ? configMode 
                             : ViteMode;
                    
                    return new ViteConfigInfo
                    {
                        ConfigFile = config.ItemSpec,
                        BuildId = config.GetMetadata("BuildId") ?? "default",
                        OutputDir = config.GetMetadata("OutputDir") ?? string.Empty, // Only use if explicitly set
                        Mode = mode,
                        PackageManager = config.GetMetadata("PackageManager") ?? PackageManager,
                        DependsOn = config.GetMetadata("DependsOn") ?? string.Empty,
                        LinkDependencies = bool.TryParse(config.GetMetadata("LinkDependencies"), out var linkDeps) && linkDeps
                    };
                }).ToList();

                // Generate task pipeline using BuildPipelineOrchestrator
                var orchestrator = new BuildPipelineOrchestrator(ViteProjectRoot);
                var (restoreTasks, viteBuildTasks) = orchestrator.BuildCompletePipeline(
                    configs,
                    PackageManager,
                    ViteLogLevel,
                    ViteEnableColors);

                // Convert tasks to MSBuild ITaskItem format
                RestoreTasks = ConvertRestoreTasksToItems(restoreTasks);
                BuildTasks = ConvertBuildTasksToItems(viteBuildTasks);

                // Collect output directories
                var outputDirs = new HashSet<string>();
                foreach (var config in configs)
                {
                    outputDirs.Add(config.OutputDir);
                }
                OutputDirectories = outputDirs.ToArray();
                ConfigurationsBuilt = configs.Count;
                BuildSucceeded = true;

                Log.LogMessage(MessageImportance.High, 
                    $"[ORCHESTRATE] Generated {RestoreTasks.Length} restore task(s) and {BuildTasks.Length} build task(s)");

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to orchestrate Vite build: {ex.Message}");
                BuildSucceeded = false;
                return false;
            }
        }

        private ITaskItem[] ConvertRestoreTasksToItems(List<BuildPipeline.PackageRestoreTask> tasks)
        {
            return tasks.Select(task => {
                var item = new TaskItem(task.TaskId);
                item.SetMetadata("Type", task.Type.ToString());
                item.SetMetadata("Command", $"{task.Executable} {task.Arguments}".Trim());
                item.SetMetadata("WorkingDirectory", task.WorkingDirectory);
                item.SetMetadata("Description", task.Description);
                
                // Environment variables as semicolon-separated key=value pairs
                if (task.Environment.Count > 0)
                {
                    var envVars = string.Join(";", task.Environment.Select(kv => $"{kv.Key}={kv.Value}"));
                    item.SetMetadata("Environment", envVars);
                }
                
                return (ITaskItem)item;
            }).ToArray();
        }

        private ITaskItem[] ConvertBuildTasksToItems(List<BuildPipeline.ViteBuildTask> tasks)
        {
            return tasks.Select(task => {
                var item = new TaskItem(task.TaskId);
                item.SetMetadata("Type", task.Type.ToString());
                item.SetMetadata("Command", $"{task.Executable} {task.Arguments}".Trim());
                item.SetMetadata("WorkingDirectory", task.WorkingDirectory);
                item.SetMetadata("Description", task.Description);
                item.SetMetadata("BuildId", task.BuildId);
                item.SetMetadata("ConfigPath", task.ConfigPath);
                item.SetMetadata("Mode", task.Mode);
                item.SetMetadata("OutputDir", task.OutputDir);
                
                // Environment variables as semicolon-separated key=value pairs
                if (task.Environment.Count > 0)
                {
                    var envVars = string.Join(";", task.Environment.Select(kv => $"{kv.Key}={kv.Value}"));
                    item.SetMetadata("Environment", envVars);
                }
                
                return (ITaskItem)item;
            }).ToArray();
        }
    }
}
