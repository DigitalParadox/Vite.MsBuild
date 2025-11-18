# ViteCommand Factory Architecture Design

## 🎯 **Proposed Factory Pattern for Command Construction**

### **Current Problem:**
- Mixed command construction logic in single `ViteCommandBuilder`
- Inconsistent Executable vs Command patterns:
  - Scripts: `Executable='npm'`, `Command='run build'`
  - Direct: `Executable='npx'`, `Command='vite build'` 
  - But tests expect: `Executable='bunx vite'`, `Command='build'`
- Hard to maintain and extend

### **Proposed Solution: IViteCommand Factory Pattern**

```csharp
// Base interfaces
public interface IViteCommand
{
    string Executable { get; }
    string Command { get; }
    string ConfigPath { get; }
    string Mode { get; }
    string OutputDir { get; }
    string LogLevel { get; }
    bool? Colors { get; }
    Dictionary<string, string> Environment { get; }
    string WorkingDirectory { get; }
    string ToString(); // Full command line
}

public interface IViteCommandBuilder
{
    IViteCommand Build();
}

public interface IViteCommandFactory
{
    IViteCommandBuilder CreateBuilder(ViteCommandType commandType, PackageManager packageManager);
}

public enum ViteCommandType
{
    ScriptBased,    // npm run build, yarn build, etc.
    DirectTool,     // npx vite, bunx vite, etc. 
    CustomCommand   // User-specified command
}
```

---

## 🏗️ **Command Builder Implementations**

### **1. ScriptBasedCommandBuilder**
```csharp
public class ScriptBasedCommandBuilder : IViteCommandBuilder
{
    private readonly PackageManager _packageManager;
    private readonly ViteCommandParameters _parameters;
    
    public IViteCommand Build()
    {
        return _packageManager switch
        {
            PackageManager.Npm => new ViteCommand
            {
                Executable = "npm",
                Command = "run build",  // Assumes "build" script exists
                // ... other properties
            },
            PackageManager.Yarn => new ViteCommand
            {
                Executable = "yarn", 
                Command = "build",      // Yarn direct script execution
                // ... other properties
            },
            PackageManager.Pnpm => new ViteCommand
            {
                Executable = "pnpm",
                Command = "run build",
                // ... other properties  
            },
            _ => throw new NotSupportedException()
        };
    }
}
```

### **2. DirectToolCommandBuilder**
```csharp
public class DirectToolCommandBuilder : IViteCommandBuilder
{
    private readonly PackageManager _packageManager;
    private readonly ViteCommandParameters _parameters;
    
    public IViteCommand Build()
    {
        return _packageManager switch
        {
            PackageManager.Npm => new ViteCommand
            {
                Executable = "npx",
                Command = "vite build",
                // ... other properties
            },
            PackageManager.Yarn => new ViteCommand
            {
                Executable = "yarn dlx",
                Command = "vite build", 
                // ... other properties
            },
            PackageManager.Pnpm => new ViteCommand
            {
                Executable = "pnpm dlx",
                Command = "vite build",
                // ... other properties
            },
            PackageManager.Bun => new ViteCommand
            {
                Executable = "bunx",
                Command = "vite build",
                // ... other properties
            }
        };
    }
}
```

### **3. CustomCommandBuilder**
```csharp
public class CustomCommandBuilder : IViteCommandBuilder
{
    private readonly string _customCommand;
    private readonly ViteCommandParameters _parameters;
    
    public IViteCommand Build()
    {
        // Parse custom command like "yarn build:production"
        var parts = _customCommand.Split(' ', 2);
        
        return new ViteCommand
        {
            Executable = parts[0],               // "yarn"
            Command = parts.Length > 1 ? parts[1] : "", // "build:production"
            // ... other properties
        };
    }
}
```

---

## 🏭 **Factory Implementation**

```csharp
public class ViteCommandFactory : IViteCommandFactory
{
    public IViteCommandBuilder CreateBuilder(ViteCommandType commandType, PackageManager packageManager)
    {
        return commandType switch
        {
            ViteCommandType.ScriptBased => new ScriptBasedCommandBuilder(packageManager),
            ViteCommandType.DirectTool => new DirectToolCommandBuilder(packageManager),
            ViteCommandType.CustomCommand => new CustomCommandBuilder(),
            _ => throw new ArgumentException($"Unsupported command type: {commandType}")
        };
    }
}
```

---

## 🎯 **Usage in ViteCommandBuilder (Refactored)**

```csharp
public class ViteCommandBuilder
{
    private readonly IViteCommandFactory _commandFactory;
    
    public ViteCommandBuilder() : this(new ViteCommandFactory()) { }
    
    public ViteCommandBuilder(IViteCommandFactory commandFactory)
    {
        _commandFactory = commandFactory;
    }
    
    public ViteCommand BuildCommand()
    {
        // Determine command type based on configuration
        var commandType = DetermineCommandType();
        
        // Create appropriate builder
        var builder = _commandFactory.CreateBuilder(commandType, _packageManager);
        
        // Build and return command
        return builder.Build();
    }
    
    private ViteCommandType DetermineCommandType()
    {
        if (!string.IsNullOrEmpty(_customCommand))
            return ViteCommandType.CustomCommand;
            
        if (HasBuildScript())
            return ViteCommandType.ScriptBased;
            
        return ViteCommandType.DirectTool;
    }
}
```

---

## ✅ **Benefits of Factory Pattern**

### **🎯 Separation of Concerns**
- **ScriptBasedCommandBuilder** - Handles npm scripts only
- **DirectToolCommandBuilder** - Handles direct tool execution only  
- **CustomCommandBuilder** - Handles user-defined commands only

### **🧪 Testability**
```csharp
[Fact]
public void DirectToolCommandBuilder_Should_Build_Correct_Npm_Command()
{
    var builder = new DirectToolCommandBuilder(PackageManager.Npm, parameters);
    var command = builder.Build();
    
    command.Executable.Should().Be("npx");
    command.Command.Should().Be("vite build");
}

[Fact] 
public void DirectToolCommandBuilder_Should_Build_Correct_Bun_Command()
{
    var builder = new DirectToolCommandBuilder(PackageManager.Bun, parameters);
    var command = builder.Build();
    
    command.Executable.Should().Be("bunx");
    command.Command.Should().Be("vite build");
}
```

### **🔧 Maintainability**
- **Easy to extend** - Add new command types without modifying existing code
- **Easy to modify** - Change one command type without affecting others
- **Clear contracts** - `IViteCommandBuilder` defines expected behavior

### **🎨 Consistency**  
- **Predictable patterns** per command type
- **No mixed logic** in single class
- **Clear responsibility** boundaries

---

## 🚀 **Implementation Plan**

### **Phase 1: Create Interfaces & Base Classes**
1. Define `IViteCommand`, `IViteCommandBuilder`, `IViteCommandFactory`
2. Create `ViteCommandParameters` value object
3. Create abstract base classes if needed

### **Phase 2: Implement Command Builders**
1. `ScriptBasedCommandBuilder`
2. `DirectToolCommandBuilder` 
3. `CustomCommandBuilder`

### **Phase 3: Implement Factory**
1. `ViteCommandFactory` with command type routing
2. Integration with existing `ViteCommandBuilder`

### **Phase 4: Update Tests**
1. Fix failing tests to match new command patterns
2. Add comprehensive tests for each builder type
3. Add integration tests for factory

### **Phase 5: Integration**
1. Update `BuildViteCommand` task to use factory
2. Update MSBuild targets if needed
3. Validate all scenarios work

This factory pattern would solve our current **Executable vs Command** inconsistencies and make the codebase much more maintainable! 🎯