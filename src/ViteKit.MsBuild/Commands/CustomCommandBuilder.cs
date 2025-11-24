using System;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Command builder for user-specified custom commands
    /// </summary>
    public class CustomCommandBuilder : ViteCommandBuilderBase
    {
        public CustomCommandBuilder(PackageManager packageManager) : base(packageManager)
        {
        }

        public override IViteCommand Build()
        {
            if (string.IsNullOrEmpty(CustomCommand))
            {
                throw new InvalidOperationException("CustomCommand must be specified for CustomCommandBuilder");
            }

            // Parse the complete user command into executable and arguments
            var (executable, arguments) = ParseUserCommand(CustomCommand);
            return CreateBaseCommand(executable, arguments);
        }

        /// <summary>
        /// Parse user's complete command into executable and arguments
        /// Example: "yarn build:enterprise --mode production" → executable="yarn", arguments="build:enterprise --mode production"
        /// Example: "npx vite build" → executable="npx", arguments="vite build"
        /// </summary>
        private (string executable, string arguments) ParseUserCommand(string userCommand)
        {
            var trimmed = userCommand.Trim();
            
            if (string.IsNullOrEmpty(trimmed))
            {
                throw new ArgumentException("Custom command cannot be empty", nameof(userCommand));
            }
            
            var parts = trimmed.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 1)
            {
                // Just executable, no arguments
                return (parts[0], "");
            }
            else
            {
                // Executable + arguments
                return (parts[0], parts[1]);
            }
        }
    }
}