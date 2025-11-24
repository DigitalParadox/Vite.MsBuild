namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Command builder for script-based Vite commands (npm run build, yarn build, etc.)
    /// </summary>
    public class ScriptBasedCommandBuilder : ViteCommandBuilderBase
    {
        private readonly string _scriptName;

        public ScriptBasedCommandBuilder(PackageManager packageManager, string scriptName = "build") 
            : base(packageManager)
        {
            _scriptName = scriptName;
        }

        public override IViteCommand Build()
        {
            var (executable, command) = GetScriptCommand();
            return CreateBaseCommand(executable, command);
        }

        private (string executable, string command) GetScriptCommand()
        {
            return PackageManager switch
            {
                PackageManager.Npm => ("npm", $"run {_scriptName}"),
                PackageManager.Yarn => ("yarn", _scriptName), // Yarn can run scripts directly
                PackageManager.Pnpm => ("pnpm", $"run {_scriptName}"),
                PackageManager.Bun => ("bun", $"run {_scriptName}"),
                _ => throw new System.NotSupportedException($"Package manager {PackageManager} is not supported for script-based commands")
            };
        }
    }
}