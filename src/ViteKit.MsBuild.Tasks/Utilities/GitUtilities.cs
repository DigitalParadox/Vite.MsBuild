using System.IO;

namespace ViteKit.MsBuild.Tasks.Utilities
{
    /// <summary>
    /// Utilities for working with git repositories
    /// </summary>
    public static class GitUtilities
    {
        /// <summary>
        /// Finds the git repository root by walking up from start directory
        /// </summary>
        /// <param name="startDirectory">Directory to start search from</param>
        /// <returns>Path to .git directory's parent, or startDirectory if not found</returns>
        public static string FindGitRoot(string startDirectory)
        {
            var current = new DirectoryInfo(startDirectory);
            
            while (current != null)
            {
                var gitDir = Path.Combine(current.FullName, ".git");
                if (Directory.Exists(gitDir) || File.Exists(gitDir)) // .git can be a file in submodules
                {
                    return current.FullName;
                }
                
                current = current.Parent;
            }
            
            // No .git found, return start directory as boundary
            return startDirectory;
        }
    }
}
