namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Provides Vite build configuration - mockable for testing
    /// </summary>
    public interface IViteBuildConfigurationProvider
    {
        ViteBuildConfiguration GetConfiguration();
    }
}
