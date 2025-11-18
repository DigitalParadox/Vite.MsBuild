using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vite.MsBuild.Tasks
{
    /// <summary>
    /// Resolves ViteConfig dependencies and determines the correct build order
    /// Detects circular dependencies and provides topological sorting
    /// </summary>
    public class ResolveViteConfigDependencies : Microsoft.Build.Utilities.Task
    {
        /// <summary>
        /// Input: All ViteConfig items with their dependencies
        /// </summary>
        [Required]
        public ITaskItem[] ViteConfigurations { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Output: ViteConfig items ordered by dependencies (dependees first)
        /// </summary>
        [Output]
        public ITaskItem[]? OrderedConfigurations { get; set; }

        /// <summary>
        /// Output: Dependency groups for parallel execution
        /// Items in the same group can build in parallel
        /// </summary>
        [Output]
        public ITaskItem[]? DependencyGroups { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "🔗 Resolving dependencies for {0} Vite configurations", ViteConfigurations.Length);

                // Build dependency graph
                var dependencyGraph = BuildDependencyGraph();
                
                // Detect circular dependencies
                if (HasCircularDependencies(dependencyGraph))
                {
                    return false; // Error already logged
                }

                // Perform topological sort
                var orderedConfigs = TopologicalSort(dependencyGraph);
                var dependencyGroups = GroupByDependencyLevel(dependencyGraph);

                OrderedConfigurations = orderedConfigs.ToArray();
                DependencyGroups = dependencyGroups.ToArray();

                Log.LogMessage(MessageImportance.Normal, "✅ Resolved {0} configurations in {1} dependency groups", 
                    orderedConfigs.Count, dependencyGroups.Count);

                LogDependencyOrder(orderedConfigs);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to resolve ViteConfig dependencies: {0}", ex.Message);
                return false;
            }
        }

        private Dictionary<string, ViteConfigNode> BuildDependencyGraph()
        {
            var nodes = new Dictionary<string, ViteConfigNode>();

            // Create nodes for all configurations
            foreach (var config in ViteConfigurations)
            {
                var buildId = config.GetMetadata("BuildId");
                if (string.IsNullOrEmpty(buildId))
                {
                    Log.LogError("ViteConfig item '{0}' is missing BuildId metadata", config.ItemSpec);
                    continue;
                }

                nodes[buildId] = new ViteConfigNode
                {
                    BuildId = buildId,
                    Item = config,
                    Dependencies = new List<string>(),
                    Dependents = new List<string>()
                };
            }

            // Build dependency relationships
            foreach (var config in ViteConfigurations)
            {
                var buildId = config.GetMetadata("BuildId");
                var dependsOn = config.GetMetadata("DependsOn");

                if (!string.IsNullOrEmpty(dependsOn) && nodes.ContainsKey(buildId))
                {
                    var dependencies = dependsOn.Split(',')
                        .Select(dep => dep.Trim())
                        .Where(dep => !string.IsNullOrEmpty(dep));

                    foreach (var dependency in dependencies)
                    {
                        if (!nodes.ContainsKey(dependency))
                        {
                            Log.LogError("ViteConfig '{0}' depends on '{1}' which does not exist", buildId, dependency);
                            continue;
                        }

                        nodes[buildId].Dependencies.Add(dependency);
                        nodes[dependency].Dependents.Add(buildId);
                    }
                }
            }

            return nodes;
        }

        private bool HasCircularDependencies(Dictionary<string, ViteConfigNode> graph)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in graph.Keys)
            {
                if (HasCircularDependencyDfs(graph, node, visited, recursionStack))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCircularDependencyDfs(Dictionary<string, ViteConfigNode> graph, string node, 
            HashSet<string> visited, HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(node))
            {
                Log.LogError("Circular dependency detected involving ViteConfig '{0}'", node);
                return true;
            }

            if (visited.Contains(node))
            {
                return false;
            }

            visited.Add(node);
            recursionStack.Add(node);

            foreach (var dependency in graph[node].Dependencies)
            {
                if (HasCircularDependencyDfs(graph, dependency, visited, recursionStack))
                {
                    return true;
                }
            }

            recursionStack.Remove(node);
            return false;
        }

        private List<ITaskItem> TopologicalSort(Dictionary<string, ViteConfigNode> graph)
        {
            var result = new List<ITaskItem>();
            var visited = new HashSet<string>();

            // Find nodes with no dependencies (starting points)
            var noDependencies = graph.Values
                .Where(node => node.Dependencies.Count == 0)
                .Select(node => node.BuildId)
                .ToList();

            // Perform DFS for topological sorting
            foreach (var startNode in noDependencies)
            {
                TopologicalSortDfs(graph, startNode, visited, result);
            }

            // Handle any remaining nodes (shouldn't happen if no cycles)
            foreach (var node in graph.Keys.Where(k => !visited.Contains(k)))
            {
                TopologicalSortDfs(graph, node, visited, result);
            }

            return result;
        }

        private void TopologicalSortDfs(Dictionary<string, ViteConfigNode> graph, string node, 
            HashSet<string> visited, List<ITaskItem> result)
        {
            if (visited.Contains(node))
            {
                return;
            }

            visited.Add(node);

            // Visit all dependencies first
            foreach (var dependency in graph[node].Dependencies)
            {
                TopologicalSortDfs(graph, dependency, visited, result);
            }

            // Add this node after its dependencies
            result.Add(graph[node].Item);
        }

        private List<ITaskItem> GroupByDependencyLevel(Dictionary<string, ViteConfigNode> graph)
        {
            var groups = new List<ITaskItem>();
            var levels = new Dictionary<string, int>();

            // Calculate dependency levels
            foreach (var node in graph.Keys)
            {
                CalculateDependencyLevel(graph, node, levels);
            }

            // Group by level
            var groupedByLevel = levels.GroupBy(kvp => kvp.Value)
                .OrderBy(g => g.Key);

            int groupIndex = 0;
            foreach (var levelGroup in groupedByLevel)
            {
                var groupItem = new TaskItem($"Group{groupIndex}");
                var configs = string.Join(",", levelGroup.Select(kvp => kvp.Key));
                groupItem.SetMetadata("Level", levelGroup.Key.ToString());
                groupItem.SetMetadata("Configurations", configs);
                groups.Add(groupItem);
                groupIndex++;
            }

            return groups;
        }

        private int CalculateDependencyLevel(Dictionary<string, ViteConfigNode> graph, string node, 
            Dictionary<string, int> levels)
        {
            if (levels.ContainsKey(node))
            {
                return levels[node];
            }

            var maxDependencyLevel = -1;
            foreach (var dependency in graph[node].Dependencies)
            {
                var depLevel = CalculateDependencyLevel(graph, dependency, levels);
                maxDependencyLevel = Math.Max(maxDependencyLevel, depLevel);
            }

            levels[node] = maxDependencyLevel + 1;
            return levels[node];
        }

        private void LogDependencyOrder(List<ITaskItem> orderedConfigs)
        {
            if (orderedConfigs.Count == 0) return;

            Log.LogMessage(MessageImportance.High, "📋 ViteConfig build order:");
            for (int i = 0; i < orderedConfigs.Count; i++)
            {
                var buildId = orderedConfigs[i].GetMetadata("BuildId");
                var dependencies = orderedConfigs[i].GetMetadata("DependsOn");
                var depInfo = string.IsNullOrEmpty(dependencies) ? "(no dependencies)" : $"(depends on: {dependencies})";
                Log.LogMessage(MessageImportance.High, "  {0}. {1} {2}", i + 1, buildId, depInfo);
            }
        }

        private class ViteConfigNode
        {
            public string BuildId { get; set; } = string.Empty;
            public ITaskItem Item { get; set; } = null!;
            public List<string> Dependencies { get; set; } = new();
            public List<string> Dependents { get; set; } = new();
        }
    }
}