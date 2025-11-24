namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Configuration information for a single Vite build
    /// </summary>
    public class ViteConfigInfo
    {
        public string ConfigFile { get; set; } = string.Empty;
        public string BuildId { get; set; } = string.Empty;
        public string OutputDir { get; set; } = string.Empty;
        public string Mode { get; set; } = "development";
        public string PackageManager { get; set; } = string.Empty;
        public string DependsOn { get; set; } = string.Empty;
        public bool LinkDependencies { get; set; } = false;
        public string? BuildScript { get; set; }
    }
}
