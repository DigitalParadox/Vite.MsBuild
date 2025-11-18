using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// Validates Vite configuration files and extracts metadata
    /// </summary>
    public class ValidateViteConfig : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Input: Path to vite.config.js/ts file
        /// </summary>
        [Required]
        public string ConfigPath { get; set; } = string.Empty;

        /// <summary>
        /// Input: Expected output directory for validation
        /// </summary>
        public string? ExpectedOutputDir { get; set; }

        /// <summary>
        /// Output: Whether the config file is valid
        /// </summary>
        [Output]
        public bool IsValid { get; set; }

        /// <summary>
        /// Output: Detected output directory from config
        /// </summary>
        [Output]
        public string? DetectedOutputDir { get; set; }

        /// <summary>
        /// Output: Any validation warnings
        /// </summary>
        [Output]
        public string[]? Warnings { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "🔍 Validating Vite config: {0}", ConfigPath);

                if (!File.Exists(ConfigPath))
                {
                    Log.LogError("Vite configuration file not found: {0}", ConfigPath);
                    IsValid = false;
                    return true; // Don't fail the build, just report invalid
                }

                var warnings = new List<string>();

                // Basic syntax validation (simple heuristics)
                var content = File.ReadAllText(ConfigPath);
                
                if (content.Contains("export default") || content.Contains("module.exports"))
                {
                    Log.LogMessage(MessageImportance.Low, "✅ Config file appears to have valid export syntax");
                }
                else
                {
                    warnings.Add("Config file may be missing export statement");
                }

                // Try to detect output directory from config content
                DetectedOutputDir = ExtractOutputDirectory(content);
                
                if (!string.IsNullOrEmpty(ExpectedOutputDir) && 
                    !string.IsNullOrEmpty(DetectedOutputDir) &&
                    !string.Equals(ExpectedOutputDir, DetectedOutputDir, StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"Output directory mismatch: expected '{ExpectedOutputDir}', detected '{DetectedOutputDir}'");
                }

                IsValid = true;
                Warnings = warnings.ToArray();

                Log.LogMessage(MessageImportance.Normal, 
                    "📋 Validated config '{0}': {1} warnings", 
                    Path.GetFileName(ConfigPath), warnings.Count);

                foreach (var warning in warnings)
                {
                    Log.LogWarning("Vite config warning: {0}", warning);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to validate Vite config '{0}': {1}", ConfigPath, ex.Message);
                IsValid = false;
                return true; // Don't fail build on validation errors
            }
        }

        private string? ExtractOutputDirectory(string content)
        {
            // Simple regex-based extraction for common patterns
            // This is a basic implementation - could be enhanced with proper parsing
            
            var patterns = new[]
            {
                @"outDir\s*:\s*['""]([^'""]+)['""]",  // outDir: 'path'
                @"build\s*:\s*\{[^}]*outDir\s*:\s*['""]([^'""]+)['""]", // build: { outDir: 'path' }
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }
    }
}