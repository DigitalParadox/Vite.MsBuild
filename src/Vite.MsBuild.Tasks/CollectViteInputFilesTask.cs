using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// High-performance C# task to replace 100+ lines of XML file collection logic
    /// Collects Vite input files using efficient directory traversal instead of XML ItemGroups
    /// </summary>
    public class CollectViteInputFilesTask : Task
    {
        [Required]
        public string ViteProjectRoot { get; set; } = string.Empty;

        public string? ViteConfigFile { get; set; }

        public string? ViteOutputDir { get; set; }

        public string ExcludePatterns { get; set; } = "node_modules;.git;obj;bin";

        [Output]
        public ITaskItem[] ViteInputFiles { get; set; } = Array.Empty<ITaskItem>();

        [Output]
        public ITaskItem[] ConfigurationFiles { get; set; } = Array.Empty<ITaskItem>();

        [Output]
        public int TotalFilesFound { get; set; }

        // High-performance file patterns - covers all major frontend frameworks
        private static readonly string[] SourceFileExtensions = {
            // TypeScript/JavaScript
            "*.ts", "*.tsx", "*.js", "*.jsx", "*.mts", "*.mjs", "*.cts", "*.cjs",
            // Framework-specific  
            "*.vue", "*.svelte", "*.solid", "*.astro",
            // Styles
            "*.css", "*.scss", "*.sass", "*.less", "*.styl", "*.stylus", "*.postcss",
            // Assets (often imported)
            "*.svg", "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.ico", "*.woff", "*.woff2",
            // Config/Data
            "*.json", "*.yaml", "*.yml", "*.toml", "*.md", "*.html"
        };

        private static readonly string[] ConfigFilePatterns = {
            "vite.config.*", "vitest.config.*", "eslint.config.*", "prettier.config.*",
            "tailwind.config.*", "postcss.config.*", "tsconfig.json", ".env*"
        };

        private readonly HashSet<string> _excludeDirectories;

        public CollectViteInputFilesTask()
        {
            _excludeDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public override bool Execute()
        {
            try
            {
                if (string.IsNullOrEmpty(ViteProjectRoot) || !Directory.Exists(ViteProjectRoot))
                {
                    Log.LogError($"ViteProjectRoot '{ViteProjectRoot}' does not exist");
                    return false;
                }

                // Parse exclude patterns
                _excludeDirectories.Clear();
                if (!string.IsNullOrEmpty(ExcludePatterns))
                {
                    foreach (var pattern in ExcludePatterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _excludeDirectories.Add(pattern.Trim());
                    }
                }

                // Add output directory to exclusions
                if (!string.IsNullOrEmpty(ViteOutputDir))
                {
                    var outputDirName = Path.GetFileName(ViteOutputDir.TrimEnd('\\', '/'));
                    if (!string.IsNullOrEmpty(outputDirName))
                    {
                        _excludeDirectories.Add(outputDirName);
                    }
                }

                Log.LogMessage(MessageImportance.Low, $"Collecting Vite input files from: {ViteProjectRoot}");
                Log.LogMessage(MessageImportance.Low, $"Excluding directories: {string.Join(", ", _excludeDirectories)}");

                var sourceFiles = new List<FileInfo>();
                var configFiles = new List<FileInfo>();

                // High-performance directory traversal (much faster than XML globbing)
                CollectFilesRecursive(new DirectoryInfo(ViteProjectRoot), sourceFiles, configFiles);

                // Convert to MSBuild task items
                ViteInputFiles = sourceFiles
                    .Select(f => CreateTaskItem(f, ViteProjectRoot))
                    .ToArray();

                ConfigurationFiles = configFiles
                    .Select(f => CreateTaskItem(f, ViteProjectRoot))
                    .ToArray();

                TotalFilesFound = ViteInputFiles.Length + ConfigurationFiles.Length;

                Log.LogMessage(MessageImportance.Normal, 
                    $"📁 Collected {ViteInputFiles.Length} source files and {ConfigurationFiles.Length} config files");

                if (Log.HasLoggedErrors == false && TotalFilesFound == 0)
                {
                    Log.LogWarning("No Vite input files found. Ensure your project has frontend source files.");
                }

                return !Log.HasLoggedErrors;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to collect Vite input files: {ex.Message}");
                return false;
            }
        }

        private void CollectFilesRecursive(DirectoryInfo directory, List<FileInfo> sourceFiles, List<FileInfo> configFiles)
        {
            try
            {
                // Skip excluded directories (much faster than XML Condition checks)
                if (_excludeDirectories.Contains(directory.Name))
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipping excluded directory: {directory.Name}");
                    return;
                }

                // Process files in current directory
                foreach (var file in directory.GetFiles())
                {
                    // Check config files first - they take priority over generic source patterns
                    if (IsConfigFile(file))
                    {
                        configFiles.Add(file);
                    }
                    else if (IsSourceFile(file))
                    {
                        sourceFiles.Add(file);
                    }
                }

                // Recurse into subdirectories
                foreach (var subdirectory in directory.GetDirectories())
                {
                    CollectFilesRecursive(subdirectory, sourceFiles, configFiles);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping inaccessible directory: {directory.FullName}");
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Error processing directory {directory.FullName}: {ex.Message}");
            }
        }

        private bool IsSourceFile(FileInfo file)
        {
            var extension = file.Extension.ToLowerInvariant();
            
            return SourceFileExtensions.Any(pattern => 
            {
                var patternExt = pattern.Substring(1); // Remove *
                return extension == patternExt;
            });
        }

        private bool IsConfigFile(FileInfo file)
        {
            var fileName = file.Name.ToLowerInvariant();
            
            return ConfigFilePatterns.Any(pattern => 
            {
                if (pattern.EndsWith("*"))
                {
                    var prefix = pattern.Substring(0, pattern.Length - 1);
                    return fileName.StartsWith(prefix);
                }
                return fileName == pattern;
            });
        }

        private TaskItem CreateTaskItem(FileInfo file, string projectRoot)
        {
            var relativePath = GetRelativePathCompat(projectRoot, file.FullName);
            var taskItem = new TaskItem(file.FullName);
            
            taskItem.SetMetadata("RelativePath", relativePath);
            taskItem.SetMetadata("FileExtension", file.Extension);
            taskItem.SetMetadata("LastWriteTime", file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
            taskItem.SetMetadata("Length", file.Length.ToString());
            
            return taskItem;
        }

        private string GetRelativePathCompat(string basePath, string fullPath)
        {
            // .NET Standard 2.0 compatible relative path
            var baseUri = new Uri(basePath.TrimEnd('\\') + "\\");
            var fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', '\\'));
        }
    }
}