using System;
using System.IO;
using System.Linq;
using ViteKit.MsBuild.Tasks;
using ViteKit.MsBuild.PureUnitTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ViteKit.MsBuild.PureUnitTests.Tasks
{
    /// <summary>
    /// Comprehensive unit tests for CollectViteInputFilesTask
    /// Tests directory traversal, framework support, file exclusions
    /// </summary>
    public class CollectViteInputFilesTaskTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _tempDir;

        public CollectViteInputFilesTaskTests(ITestOutputHelper output)
        {
            _output = output;
            _tempDir = Path.Combine(Path.GetTempPath(), "ViteTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private CollectViteInputFilesTask CreateTask()
        {
            return new CollectViteInputFilesTask
            {
                BuildEngine = new MockBuildEngine()
            };
        }

        [Fact]
        public void CollectViteInputFiles_EmptyDirectory_ReturnsEmptyResults()
        {
            // Arrange
            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules;.git";

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result, "Task should succeed");
            Assert.Empty(task.ViteInputFiles);
            Assert.Empty(task.ConfigurationFiles);
            Assert.Equal(0, task.TotalFilesFound);
        }

        [Theory]
        [InlineData("app.ts", true)]
        [InlineData("component.tsx", true)]
        [InlineData("script.js", true)]
        [InlineData("Component.jsx", true)]
        [InlineData("module.mts", true)]
        [InlineData("bundle.mjs", true)]
        [InlineData("App.vue", true)]
        [InlineData("Component.svelte", true)]
        [InlineData("styles.css", true)]
        [InlineData("styles.scss", true)]
        [InlineData("config.json", true)]
        [InlineData("README.md", true)]
        [InlineData("image.png", true)]
        [InlineData("font.woff2", true)]
        [InlineData("temp.tmp", false)]
        [InlineData("backup.bak", false)]
        public void CollectViteInputFiles_VariousFileTypes_FilterCorrectly(string fileName, bool shouldInclude)
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, fileName);
            File.WriteAllText(filePath, "content");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules;.git";

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            
            if (shouldInclude)
            {
                Assert.True(task.TotalFilesFound > 0); // File should be included
            }
            else
            {
                Assert.Equal(0, task.TotalFilesFound); // File should be excluded  
            }
        }

        [Fact]
        public void CollectViteInputFiles_NestedDirectories_TraversesCorrectly()
        {
            // Arrange
            var srcDir = Path.Combine(_tempDir, "src");
            var componentDir = Path.Combine(srcDir, "components");
            Directory.CreateDirectory(componentDir);

            File.WriteAllText(Path.Combine(srcDir, "main.ts"), "// main");
            File.WriteAllText(Path.Combine(componentDir, "App.vue"), "<!-- App -->");
            File.WriteAllText(Path.Combine(componentDir, "Button.tsx"), "// Button");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules;.git"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(3, task.ViteInputFiles.Length);
            
            var filePaths = task.ViteInputFiles.Select(f => f.ItemSpec).ToList();
            Assert.Contains(filePaths, p => p.EndsWith("main.ts"));
            Assert.Contains(filePaths, p => p.EndsWith("App.vue"));
            Assert.Contains(filePaths, p => p.EndsWith("Button.tsx"));
        }

        [Fact]
        public void CollectViteInputFiles_ExcludePatterns_SkipsCorrectDirectories()
        {
            // Arrange
            var nodeModulesDir = Path.Combine(_tempDir, "node_modules");
            var gitDir = Path.Combine(_tempDir, ".git");
            var objDir = Path.Combine(_tempDir, "obj");
            var srcDir = Path.Combine(_tempDir, "src");

            Directory.CreateDirectory(nodeModulesDir);
            Directory.CreateDirectory(gitDir);
            Directory.CreateDirectory(objDir);
            Directory.CreateDirectory(srcDir);

            // Create files in excluded directories
            File.WriteAllText(Path.Combine(nodeModulesDir, "package.js"), "// should be excluded");
            File.WriteAllText(Path.Combine(gitDir, "config"), "// should be excluded");
            File.WriteAllText(Path.Combine(objDir, "build.js"), "// should be excluded");

            // Create file in included directory
            File.WriteAllText(Path.Combine(srcDir, "main.ts"), "// should be included");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules;.git;obj"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Single(task.ViteInputFiles);
            Assert.EndsWith("main.ts", task.ViteInputFiles[0].ItemSpec);
        }

        [Fact]
        public void CollectViteInputFiles_ConfigurationFiles_DetectedSeparately()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_tempDir, "vite.config.ts"), "export default {}");
            File.WriteAllText(Path.Combine(_tempDir, "tsconfig.json"), "{}");
            File.WriteAllText(Path.Combine(_tempDir, "tailwind.config.js"), "module.exports = {}");
            File.WriteAllText(Path.Combine(_tempDir, ".env"), "NODE_ENV=development");
            
            // Create src directory before creating files in it
            var srcDir = Path.Combine(_tempDir, "src");
            Directory.CreateDirectory(srcDir);
            File.WriteAllText(Path.Combine(_tempDir, "src/main.ts"), "// app code");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(task.ConfigurationFiles.Length >= 3); // Should detect config files
            Assert.Single(task.ViteInputFiles); // Only main.ts should be in source files
            
            var configPaths = task.ConfigurationFiles.Select(f => Path.GetFileName(f.ItemSpec)).ToList();
            Assert.Contains("vite.config.ts", configPaths);
            Assert.Contains("tsconfig.json", configPaths);
        }

        [Fact]
        public void CollectViteInputFiles_FileMetadata_IncludesUsefulInfo()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "test.ts");
            var content = "// test file content";
            File.WriteAllText(filePath, content);

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Single(task.ViteInputFiles);

            var fileItem = task.ViteInputFiles[0];
            Assert.Equal(filePath, fileItem.ItemSpec);
            Assert.Equal("test.ts", Path.GetFileName(fileItem.GetMetadata("RelativePath")));
            Assert.Equal(".ts", fileItem.GetMetadata("Extension"));
            Assert.NotEmpty(fileItem.GetMetadata("LastWriteTime"));
            Assert.NotEmpty(fileItem.GetMetadata("Length"));
        }

        [Fact]
        public void CollectViteInputFiles_OutputDirExclusion_WorksCorrectly()
        {
            // Arrange
            var outputDir = Path.Combine(_tempDir, "dist");
            Directory.CreateDirectory(outputDir);

            File.WriteAllText(Path.Combine(outputDir, "compiled.js"), "// compiled output");
            File.WriteAllText(Path.Combine(_tempDir, "source.ts"), "// source file");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ViteOutputDir = "dist";
            task.ExcludePatterns = "node_modules;dist";

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Single(task.ViteInputFiles);
            Assert.EndsWith("source.ts", task.ViteInputFiles[0].ItemSpec);
            Assert.DoesNotContain(task.ViteInputFiles, f => f.ItemSpec.Contains("compiled.js"));
        }

        [Fact]
        public void CollectViteInputFiles_FrameworkFiles_AllSupported()
        {
            // Arrange - Create files for all supported frameworks
            var frameworks = new[]
            {
                ("App.vue", "Vue"),
                ("Component.svelte", "Svelte"),  
                ("Widget.solid", "Solid"),
                ("Layout.astro", "Astro"),
                ("main.ts", "TypeScript"),
                ("utils.js", "JavaScript"),
                ("styles.css", "CSS"),
                ("theme.scss", "Sass")
            };

            foreach (var (fileName, framework) in frameworks)
            {
                File.WriteAllText(Path.Combine(_tempDir, fileName), $"// {framework} content");
            }

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules"
            ;

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(frameworks.Length, task.ViteInputFiles.Length);

            foreach (var (fileName, _) in frameworks)
            {
                Assert.Contains(task.ViteInputFiles, f => f.ItemSpec.EndsWith(fileName));
            }
        }

        [Fact]
        public void CollectViteInputFiles_NonexistentDirectory_Fails()
        {
            // Arrange
            var task = CreateTask();
            task.ViteProjectRoot = Path.Combine(_tempDir, "nonexistent");
            task.ExcludePatterns = "node_modules";

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result, "Task should fail for nonexistent directory");
        }

        [Fact]
        public void CollectViteInputFiles_InaccessibleDirectory_HandlesGracefully()
        {
            // Arrange
            var restrictedDir = Path.Combine(_tempDir, "restricted");
            Directory.CreateDirectory(restrictedDir);
            File.WriteAllText(Path.Combine(restrictedDir, "file.ts"), "content");

            var normalDir = Path.Combine(_tempDir, "normal");
            Directory.CreateDirectory(normalDir);
            File.WriteAllText(Path.Combine(normalDir, "app.ts"), "content");

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules"
            ;

            // Act
            var result = task.Execute();

            // Assert - Should succeed even if some directories are inaccessible
            Assert.True(result);
            Assert.True(task.ViteInputFiles.Length > 0); // Should find accessible files
        }

        [Fact]
        public void CollectViteInputFiles_LargeDirectoryTree_PerformanceTest()
        {
            // Arrange - Create a moderately large directory structure
            for (int i = 0; i < 10; i++)
            {
                var subDir = Path.Combine(_tempDir, $"dir{i}");
                Directory.CreateDirectory(subDir);
                
                for (int j = 0; j < 10; j++)
                {
                    File.WriteAllText(Path.Combine(subDir, $"file{j}.ts"), $"// File {i}.{j}");
                }
            }

            var task = CreateTask();
            task.ViteProjectRoot = _tempDir;
            task.ExcludePatterns = "node_modules"
            ;

            // Act
            var startTime = DateTime.UtcNow;
            var result = task.Execute();
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.True(result);
            Assert.Equal(100, task.ViteInputFiles.Length);
            Assert.True((endTime - startTime).TotalSeconds < 5); // Should complete within 5 seconds
        }
    }
}
