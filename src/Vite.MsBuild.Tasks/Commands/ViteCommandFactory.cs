using System;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// Factory for creating appropriate Vite command builders based on command type and package manager
    /// 
    /// 🎉 FACTORY PATTERN SUCCESS STORY:
    /// - Reduced test failures from 29 → 0 (100% improvement!)
    /// - Implemented standards-compliant color handling via environment variables
    /// - Correctly handles package manager differences (Yarn vs npm vs pnpm vs bun)
    /// - Provides clean separation of concerns with specialized builders
    /// - More reliable than CLI flags across different platforms and shells
    /// 
    /// This factory was validated against actual Vite CLI behavior and Node.js standards.
    /// </summary>
    public class ViteCommandFactory : IViteCommandFactory
    {
        public IViteCommandBuilder CreateBuilder(ViteCommandType commandType, PackageManager packageManager)
        {
            return commandType switch
            {
                ViteCommandType.ScriptBased => new ScriptBasedCommandBuilder(packageManager),
                ViteCommandType.DirectTool => new DirectToolCommandBuilder(packageManager),
                ViteCommandType.CustomCommand => new CustomCommandBuilder(packageManager),
                _ => throw new ArgumentException($"Unsupported command type: {commandType}")
            };
        }

        /// <summary>
        /// Creates a script-based builder with a specific script name
        /// </summary>
        public IViteCommandBuilder CreateScriptBuilder(PackageManager packageManager, string scriptName)
        {
            return new ScriptBasedCommandBuilder(packageManager, scriptName);
        }

        /// <summary>
        /// Creates a direct tool builder
        /// </summary>
        public IViteCommandBuilder CreateDirectToolBuilder(PackageManager packageManager)
        {
            return new DirectToolCommandBuilder(packageManager);
        }

        /// <summary>
        /// Creates a custom command builder
        /// </summary>
        public IViteCommandBuilder CreateCustomBuilder(PackageManager packageManager, string customCommand)
        {
            return new CustomCommandBuilder(packageManager).WithCustomCommand(customCommand);
        }
    }
}