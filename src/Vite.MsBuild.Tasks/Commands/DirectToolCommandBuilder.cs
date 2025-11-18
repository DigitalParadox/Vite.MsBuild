namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// Command builder for direct tool execution (npx vite, bunx vite, etc.)
    /// </summary>
    public class DirectToolCommandBuilder : ViteCommandBuilderBase
    {
        public DirectToolCommandBuilder(PackageManager packageManager) : base(packageManager)
        {
        }

        public override IViteCommand Build()
        {
            var (executable, command) = GetDirectToolCommand();
            return CreateBaseCommand(executable, command);
        }

        private (string executable, string command) GetDirectToolCommand()
        {
            return PackageManager switch
            {
                PackageManager.Npm => ("npx", "vite build"),
                PackageManager.Yarn => ("yarn dlx", "vite build"),
                PackageManager.Pnpm => ("pnpm dlx", "vite build"),
                PackageManager.Bun => ("bunx", "vite build"),
                _ => throw new System.NotSupportedException($"Package manager {PackageManager} is not supported for direct tool commands")
            };
        }
    }
}