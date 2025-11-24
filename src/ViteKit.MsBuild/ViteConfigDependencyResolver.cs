using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ViteKit.MsBuild.Tasks
{
    /// <summary>
    /// Resolves ViteConfig dependencies and determines the correct build order
    /// Detects circular dependencies and provides topological sorting
    /// </summary>
    public class ViteConfigDependencyResolver : Microsoft.Build.Utilities.Task
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
                Log.LogMessage(MessageImportance.Low, "[LINK] Resolving dependencies for {0} Vite configurations", ViteConfigurations.Length);

                // Build dependency graph
                var dependencyGraph = BuildDependencyGraph();
                if (dependencyGraph == null)
                {
                    return false; // Error already logged
                }
                
                // Single-pass topological sort with cycle detection and level calculation (Kahn's algorithm)
                var (orderedConfigs, levels, hasCycle) = OptimizedTopologicalSort(dependencyGraph);
                
                if (hasCycle)
                {
                    Log.LogError("Circular dependency detected in ViteConfig dependencies");
                    return false;
                }

                var dependencyGroups = GroupByDependencyLevel(levels);

                OrderedConfigurations = orderedConfigs.ToArray();
                DependencyGroups = dependencyGroups.ToArray();

                Log.LogMessage(MessageImportance.Normal, "[OK] Resolved {0} configurations in {1} dependency groups", 
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

        private Dictionary<string, ViteConfigNode>? BuildDependencyGraph()
        {
            var nodes = new Dictionary<string, ViteConfigNode>(ViteConfigurations.Length);
            var hasErrors = false;

            // First pass: Create all nodes and cache metadata
            // Avoid duplicate GetMetadata calls by caching BuildId and DependsOn
            var configMetadata = new List<(ITaskItem config, string buildId, string dependsOn)>(ViteConfigurations.Length);
            
            foreach (var config in ViteConfigurations)
            {
                var buildId = config.GetMetadata("BuildId");
                if (string.IsNullOrEmpty(buildId))
                {
                    Log.LogError("ViteConfig item '{0}' is missing BuildId metadata", config.ItemSpec);
                    hasErrors = true;
                    continue;
                }

                var dependsOn = config.GetMetadata("DependsOn") ?? string.Empty;
                configMetadata.Add((config, buildId, dependsOn));

                nodes[buildId] = new ViteConfigNode
                {
                    BuildId = buildId,
                    Item = config,
                    Dependencies = new List<string>(),
                    Dependents = new List<string>()
                };
            }

            // Second pass: Build dependency relationships using cached metadata
            foreach (var (_, buildId, dependsOn) in configMetadata)
            {
                if (string.IsNullOrEmpty(dependsOn)) continue;

                // Parse dependencies once and convert to array for faster iteration
                var dependencies = dependsOn.Split(',');
                
                foreach (var dep in dependencies)
                {
                    var dependency = dep.Trim();
                    if (string.IsNullOrEmpty(dependency)) continue;

                    if (!nodes.ContainsKey(dependency))
                    {
                        Log.LogError("ViteConfig '{0}' depends on '{1}' which does not exist", buildId, dependency);
                        hasErrors = true;
                        continue;
                    }

                    nodes[buildId].Dependencies.Add(dependency);
                    nodes[dependency].Dependents.Add(buildId);
                }
            }

            return hasErrors ? null : nodes;
        }

        /// <summary>
        /// Optimized single-pass topological sort using Kahn's algorithm
        /// Simultaneously performs: cycle detection, topological ordering, and level calculation
        /// Time complexity: O(V + E) where V = nodes, E = edges
        /// Space complexity: O(V)
        /// </summary>
        private (List<ITaskItem> sorted, Dictionary<string, int> levels, bool hasCycle) 
            OptimizedTopologicalSort(Dictionary<string, ViteConfigNode> graph)
        {
            var sorted = new List<ITaskItem>(graph.Count);
            var levels = new Dictionary<string, int>(graph.Count);
            var inDegree = new Dictionary<string, int>(graph.Count);
            
            // Initialize in-degrees: O(V + E)
            foreach (var node in graph.Values)
            {
                inDegree[node.BuildId] = node.Dependencies.Count;
                if (inDegree[node.BuildId] == 0)
                {
                    levels[node.BuildId] = 0;
                }
            }
            
            // Queue for nodes with no dependencies (zero in-degree)
            var queue = new Queue<string>(graph.Count);
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }
            
            // Process nodes in topological order: O(V + E)
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentNode = graph[current];
                sorted.Add(currentNode.Item);
                
                var currentLevel = levels[current];
                
                // Process all dependents
                foreach (var dependent in currentNode.Dependents)
                {
                    // Update in-degree
                    inDegree[dependent]--;
                    
                    // Calculate level (maximum of all dependency levels + 1)
                    if (!levels.ContainsKey(dependent))
                    {
                        levels[dependent] = currentLevel + 1;
                    }
                    else
                    {
                        levels[dependent] = Math.Max(levels[dependent], currentLevel + 1);
                    }
                    
                    // If all dependencies processed, add to queue
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }
            
            // Cycle detection: if we didn't process all nodes, there's a cycle
            bool hasCycle = sorted.Count != graph.Count;
            
            return (sorted, levels, hasCycle);
        }

        private List<ITaskItem> GroupByDependencyLevel(Dictionary<string, int> levels)
        {
            var groups = new List<ITaskItem>();
            
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

        private void LogDependencyOrder(List<ITaskItem> orderedConfigs)
        {
            if (orderedConfigs.Count == 0) return;

            Log.LogMessage(MessageImportance.High, "* ViteConfig build order:");
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