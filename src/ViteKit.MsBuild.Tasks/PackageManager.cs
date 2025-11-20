namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Supported package managers for Vite projects
    /// </summary>
    public enum PackageManager
    {
        /// <summary>
        /// Node Package Manager (npm)
        /// </summary>
        Npm,
        
        /// <summary>
        /// Yarn package manager
        /// </summary>
        Yarn,
        
        /// <summary>
        /// PNPM package manager
        /// </summary>
        Pnpm,
        
        /// <summary>
        /// Bun package manager
        /// </summary>
        Bun
    }
}