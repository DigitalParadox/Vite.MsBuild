using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// Custom task factory that enables namespaced Vite configuration syntax
    /// like <Vite.MsBuild.ViteConfig Name="..." Config="..." />
    /// </summary>
    public class ViteTaskFactory : ITaskFactory
    {
        private TaskPropertyInfo[] _parameters = Array.Empty<TaskPropertyInfo>();

        public string FactoryName => "Vite.MsBuild.ViteConfig";

        public Type TaskType => typeof(ViteConfig);

        public TaskPropertyInfo[] GetTaskParameters()
        {
            if (_parameters.Length == 0)
            {
                _parameters = ExtractParametersFromTask(typeof(ViteConfig));
            }
            return _parameters;
        }

        public bool Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, 
                              string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            // Store task name and parameters for later use
            return true;
        }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return new ViteConfig { BuildEngine = taskFactoryLoggingHost };
        }

        public void CleanupTask(ITask task)
        {
            // Nothing to clean up
        }

        private TaskPropertyInfo[] ExtractParametersFromTask(Type taskType)
        {
            var parameters = new List<TaskPropertyInfo>();

            foreach (var property in taskType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanWrite)
                {
                    var isRequired = property.GetCustomAttribute<RequiredAttribute>() != null;
                    var isOutput = property.GetCustomAttribute<OutputAttribute>() != null;

                    parameters.Add(new TaskPropertyInfo(
                        property.Name,
                        property.PropertyType,
                        isOutput,
                        isRequired));
                }
            }

            return parameters.ToArray();
        }
    }

    /// <summary>
    /// Alternative approach: Direct namespaced task registration
    /// This allows for <Vite.MsBuild.ViteConfig> syntax without custom factory
    /// </summary>
    public class NamespacedViteConfig : ViteConfig
    {
        // Inherits all functionality from ViteConfig
        // but can be registered with a different name in MSBuild
    }

    /// <summary>
    /// Namespaced build command task
    /// </summary>
    public class NamespacedViteBuild : BuildViteCommand
    {
        // Inherits all functionality from BuildViteCommand
        // but provides namespaced syntax
    }
}