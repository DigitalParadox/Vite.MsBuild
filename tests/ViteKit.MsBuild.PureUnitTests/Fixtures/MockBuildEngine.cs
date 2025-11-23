using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ViteKit.MsBuild.PureUnitTests.Fixtures
{
    /// <summary>
    /// Mock implementation of IBuildEngine for testing MSBuild tasks
    /// </summary>
    public class MockBuildEngine : IBuildEngine
    {
        public bool ContinueOnError { get; set; }
        public int LineNumberOfTaskNode { get; set; }
        public int ColumnNumberOfTaskNode { get; set; }
        public string ProjectFileOfTaskNode { get; set; } = string.Empty;

        public List<BuildErrorEventArgs> LoggedErrors { get; } = new();
        public List<BuildWarningEventArgs> LoggedWarnings { get; } = new();
        public List<BuildMessageEventArgs> LoggedMessages { get; } = new();

        public bool BuildProjectFile(string projectFileName, string[] targetNames, 
            IDictionary globalProperties, 
            IDictionary targetOutputs) => true;

        public void LogCustomEvent(CustomBuildEventArgs e) { }
        
        public void LogErrorEvent(BuildErrorEventArgs e) 
        {
            LoggedErrors.Add(e);
            Console.WriteLine($"ERROR: {e.Message}");
        }
        
        public void LogMessageEvent(BuildMessageEventArgs e) 
        {
            LoggedMessages.Add(e);
            Console.WriteLine($"MESSAGE: {e.Message}");
        }
        
        public void LogWarningEvent(BuildWarningEventArgs e) 
        {
            LoggedWarnings.Add(e);
            Console.WriteLine($"WARNING: {e.Message}");
        }
    }
}

