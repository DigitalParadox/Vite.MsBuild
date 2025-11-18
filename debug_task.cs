using System;
using System.IO;
using System.Linq;
using Vite.MsBuild.Tasks;
using Vite.MsBuild.PureUnitTests.Helpers;

var tempDir = Path.Combine(Path.GetTempPath(), "ViteTest_Debug");
Directory.CreateDirectory(tempDir);

try
{
    // Create a test file
    var testFile = Path.Combine(tempDir, "app.ts");
    File.WriteAllText(testFile, "console.log('test')");

    // Create and run the task
    var task = new CollectViteInputFilesTask
    {
        BuildEngine = new MockBuildEngine(),
        ViteProjectRoot = tempDir,
        ExcludePatterns = "node_modules;.git"
    };

    Console.WriteLine($"ViteProjectRoot: {task.ViteProjectRoot}");
    Console.WriteLine($"Directory exists: {Directory.Exists(tempDir)}");
    Console.WriteLine($"Files in directory: {string.Join(", ", Directory.GetFiles(tempDir).Select(Path.GetFileName))}");

    var result = task.Execute();

    Console.WriteLine($"Task execute result: {result}");
    Console.WriteLine($"TotalFilesFound: {task.TotalFilesFound}");
    Console.WriteLine($"ViteInputFiles count: {task.ViteInputFiles.Length}");
    Console.WriteLine($"ConfigurationFiles count: {task.ConfigurationFiles.Length}");
    
    if (task.ViteInputFiles.Length > 0)
    {
        Console.WriteLine($"Found files: {string.Join(", ", task.ViteInputFiles.Select(f => f.ItemSpec))}");
    }
}
finally
{
    if (Directory.Exists(tempDir))
    {
        Directory.Delete(tempDir, true);
    }
}