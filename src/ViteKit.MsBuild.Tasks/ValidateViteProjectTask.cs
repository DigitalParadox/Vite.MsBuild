using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Comprehensive C# task to replace multiple XML validation targets
    /// Replaces: ValidateViteSetup, ValidatePackageManager, ValidateViteEnvironmentFiles, etc.
    /// </summary>
    public class ValidateViteProjectTask : Task
    {
        [Required]
        public string ViteProjectRoot { get; set; } = string.Empty;

        public string? ViteConfigFile { get; set; }

        public string ViteMissingEnvAction { get; set; } = "silent"; // silent, warn, error

        public string PackageManager { get; set; } = "npm";

        public bool EnableViteBuild { get; set; } = true;

        public bool ShowWelcomeMessage { get; set; } = true;

        [Output]
        public bool IsValid { get; set; }

        [Output]
        public bool HasPackageJson { get; set; }

        [Output]
        public bool HasViteConfig { get; set; }

        [Output]
        public bool HasNodeModules { get; set; }

        [Output]
        public string[] MissingEnvFiles { get; set; } = Array.Empty<string>();

        private static readonly string[] CommonEnvFiles = { ".env", ".env.local", ".env.development", ".env.production" };

        public override bool Execute()
        {
            try
            {
                if (!EnableViteBuild)
                {
                    Log.LogMessage(MessageImportance.Low, "Vite build is disabled, skipping validation");
                    IsValid = true;
                    return true;
                }

                if (string.IsNullOrEmpty(ViteProjectRoot) || !Directory.Exists(ViteProjectRoot))
                {
                    Log.LogError($"ViteProjectRoot '{ViteProjectRoot}' does not exist");
                    return false;
                }

                Log.LogMessage(MessageImportance.Low, $"🔍 Validating Vite project at: {ViteProjectRoot}");

                // Perform all validations
                ValidatePackageJson();
                ValidateViteConfig();
                ValidateNodeModules();
                ValidateEnvironmentFiles();
                ValidatePackageManager();

                // Show welcome message for new projects
                if (ShowWelcomeMessage && HasPackageJson && HasViteConfig)
                {
                    ShowWelcomeMessageOnce();
                }

                IsValid = !Log.HasLoggedErrors;

                if (IsValid)
                {
                    Log.LogMessage(MessageImportance.Normal, "[OK] Vite project validation completed successfully");
                }

                return IsValid;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to validate Vite project: {ex.Message}");
                return false;
            }
        }

        private void ValidatePackageJson()
        {
            var packageJsonPath = Path.Combine(ViteProjectRoot, "package.json");
            HasPackageJson = File.Exists(packageJsonPath);

            if (!HasPackageJson)
            {
                Log.LogError($"[ERROR] No package.json found in {ViteProjectRoot}. Run 'npm init' to create one.");
                return;
            }

            Log.LogMessage(MessageImportance.Low, $"[OK] Found package.json: {packageJsonPath}");

            // Validate package.json content
            try
            {
                var content = File.ReadAllText(packageJsonPath);
                if (string.IsNullOrWhiteSpace(content) || !content.Trim().StartsWith("{"))
                {
                    Log.LogWarning("[WARN] package.json appears to be invalid or empty");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[WARN] Could not read package.json: {ex.Message}");
            }
        }

        private void ValidateViteConfig()
        {
            var possibleConfigs = new[]
            {
                "vite.config.ts", "vite.config.js", "vite.config.mts", "vite.config.mjs"
            };

            string? foundConfig = null;

            // Check user-specified config first
            if (!string.IsNullOrEmpty(ViteConfigFile))
            {
                var userConfigPath = Path.IsPathRooted(ViteConfigFile) ? 
                    ViteConfigFile : 
                    Path.Combine(ViteProjectRoot, ViteConfigFile);

                if (File.Exists(userConfigPath))
                {
                    foundConfig = userConfigPath;
                    Log.LogMessage(MessageImportance.Low, $"[OK] Found user-specified Vite config: {ViteConfigFile}");
                }
                else
                {
                    Log.LogWarning($"[WARN] User-specified Vite config not found: {ViteConfigFile}");
                }
            }

            // Auto-detect if no user config or user config not found
            if (foundConfig == null)
            {
                foreach (var configName in possibleConfigs)
                {
                    var configPath = Path.Combine(ViteProjectRoot, configName);
                    if (File.Exists(configPath))
                    {
                        foundConfig = configPath;
                        Log.LogMessage(MessageImportance.Low, $"[OK] Auto-detected Vite config: {configName}");
                        break;
                    }
                }
            }

            HasViteConfig = foundConfig != null;

            if (!HasViteConfig)
            {
                Log.LogMessage(MessageImportance.Normal, 
                    "[INFO] No vite.config file found, but Vite can work with defaults. " +
                    "Consider creating one for customization: npm create vite@latest");
            }
        }

        private void ValidateNodeModules()
        {
            var nodeModulesPath = Path.Combine(ViteProjectRoot, "node_modules");
            HasNodeModules = Directory.Exists(nodeModulesPath);

            if (!HasNodeModules)
            {
                Log.LogMessage(MessageImportance.Normal, 
                    $"[PKG] node_modules not found. Will run '{PackageManager} install' during build.");
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, "[OK] node_modules directory found");
                
                // Check for Vite dependency
                var vitePath = Path.Combine(nodeModulesPath, "vite");
                if (Directory.Exists(vitePath))
                {
                    Log.LogMessage(MessageImportance.Low, "[OK] Vite dependency found in node_modules");
                }
                else
                {
                    Log.LogMessage(MessageImportance.Normal, 
                        "[INFO] Vite not found in node_modules. Ensure it's listed in your dependencies.");
                }
            }
        }

        private void ValidateEnvironmentFiles()
        {
            List<string> missingFiles = [];

            foreach (var envFile in CommonEnvFiles)
            {
                var envPath = Path.Combine(ViteProjectRoot, envFile);
                if (!File.Exists(envPath))
                {
                    missingFiles.Add(envFile);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, $"[OK] Found environment file: {envFile}");
                }
            }

            MissingEnvFiles = missingFiles.ToArray();

            if (MissingEnvFiles.Length > 0)
            {
                var missingList = string.Join(", ", MissingEnvFiles);

                switch (ViteMissingEnvAction.ToLowerInvariant())
                {
                    case "error":
                        Log.LogError($"[ERROR] Missing environment files: {missingList}");
                        break;
                    case "warn":
                        Log.LogWarning($"[WARN] Missing environment files: {missingList}");
                        break;
                    case "silent":
                    default:
                        Log.LogMessage(MessageImportance.Low, $"[INFO] Optional environment files not found: {missingList}");
                        break;
                }
            }
        }

        private void ValidatePackageManager()
        {
            var supportedManagers = new[] { "npm", "yarn", "pnpm", "bun" };

            if (!supportedManagers.Contains(PackageManager.ToLowerInvariant()))
            {
                Log.LogWarning($"[WARN] Unsupported package manager '{PackageManager}'. Supported: {string.Join(", ", supportedManagers)}");
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, $"[OK] Using package manager: {PackageManager}");
            }
        }

        private void ShowWelcomeMessageOnce()
        {
            // Simple heuristic: if we have both package.json and vite.config but no node_modules,
            // this is likely a first-time setup
            if (HasPackageJson && HasViteConfig && !HasNodeModules)
            {
                Log.LogMessage(MessageImportance.High, "");
                Log.LogMessage(MessageImportance.High, "[OK] Welcome to ViteKit.Msbuild!");
                Log.LogMessage(MessageImportance.High, "");
                Log.LogMessage(MessageImportance.High, "[OK] Your project is configured for automatic Vite builds");
                Log.LogMessage(MessageImportance.High, $"[PKG] Package manager: {PackageManager}");
                
                if (HasViteConfig)
                {
                    Log.LogMessage(MessageImportance.High, $"[CONFIG]  Vite config: Found");
                }

                Log.LogMessage(MessageImportance.High, "");
                Log.LogMessage(MessageImportance.High, ">> Run 'dotnet build' to build your frontend automatically!");
                Log.LogMessage(MessageImportance.High, "");
            }
        }
    }
}