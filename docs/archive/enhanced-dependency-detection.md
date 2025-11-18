# Enhanced Dependency Detection for Vite.MsBuild

## Current Implementation vs Industry Standard

### What We Currently Do
- **Timestamp-based**: Compare lock file modification time vs node_modules
- **Basic**: Check if files exist and when they were last modified
- **Simple**: Works but not optimal for complex dependency scenarios

### Industry Standard (Vite/Modern Build Tools)
- **Content-based**: Hash lock file contents for true change detection
- **Cache-aware**: Invalidate specific cache directories (node_modules/.vite)
- **Efficient**: Only rebuild when dependencies actually change, not just file timestamps

## Enhanced Implementation Plan

### Phase 1: Lock File Content Hashing
```xml
<!-- Instead of timestamp comparison -->
<PropertyGroup>
    <LockFileHash>$([System.IO.File]::ReadAllText('$(ViteProjectRoot)package-lock.json').GetHashCode())</LockFileHash>
    <CachedLockFileHash Condition="Exists('$(IntermediateOutputPath)lockfile.hash')">$([System.IO.File]::ReadAllText('$(IntermediateOutputPath)lockfile.hash'))</CachedLockFileHash>
    <NodeRestoreRequired Condition="'$(LockFileHash)' != '$(CachedLockFileHash)'">true</NodeRestoreRequired>
</PropertyGroup>
```

### Phase 2: Vite Cache Invalidation
```xml
<!-- Clear Vite's internal cache when dependencies change -->
<Target Name="InvalidateViteCache" Condition="'$(NodeRestoreRequired)' == 'true'">
    <RemoveDir Directories="$(ViteProjectRoot)node_modules\.vite" Condition="Exists('$(ViteProjectRoot)node_modules\.vite')" />
    <Message Text="🗑️ Invalidated Vite dependency cache" Importance="normal" />
</Target>
```

### Phase 3: Configuration Change Detection
```xml
<!-- Hash relevant Vite config fields -->
<PropertyGroup>
    <ViteConfigHash>$([System.IO.File]::ReadAllText('$(ViteConfigFile)').GetHashCode())</ViteConfigHash>
    <CachedViteConfigHash Condition="Exists('$(IntermediateOutputPath)viteconfig.hash')">$([System.IO.File]::ReadAllText('$(IntermediateOutputPath)viteconfig.hash'))</CachedViteConfigHash>
    <ViteCacheInvalidationRequired Condition="'$(ViteConfigHash)' != '$(CachedViteConfigHash)'">true</ViteCacheInvalidationRequired>
</PropertyGroup>
```

### Phase 4: Yarn PnP Specific Handling
```xml
<!-- For Yarn PnP, monitor .pnp.cjs content changes -->
<PropertyGroup Condition="Exists('$(ViteProjectRoot).pnp.cjs')">
    <PnpHash>$([System.IO.File]::ReadAllText('$(ViteProjectRoot).pnp.cjs').GetHashCode())</PnpHash>
    <CachedPnpHash Condition="Exists('$(IntermediateOutputPath)pnp.hash')">$([System.IO.File]::ReadAllText('$(IntermediateOutputPath)pnp.hash'))</CachedPnpHash>
    <NodeRestoreRequired Condition="'$(PnpHash)' != '$(CachedPnpHash)'">true</NodeRestoreRequired>
</PropertyGroup>
```

## Benefits of Enhanced Approach

### 🎯 **Accuracy**
- Detects actual dependency changes, not just file modification times
- Handles edge cases like file touches, system clock changes
- Works correctly with Git operations that change timestamps

### ⚡ **Performance** 
- Only invalidates when content actually changes
- Leverages Vite's built-in caching mechanisms
- Reduces unnecessary dependency installations

### 🔧 **Reliability**
- Aligns with how Vite itself handles dependency caching
- Follows JavaScript ecosystem best practices
- More robust across different development environments

## Migration Strategy

1. **Phase 1**: Implement content hashing alongside existing timestamp logic
2. **Phase 2**: Add Vite cache invalidation
3. **Phase 3**: Add configuration monitoring 
4. **Phase 4**: Deprecate timestamp-only approach
5. **Phase 5**: Add comprehensive testing

## Testing Requirements

```csharp
[Fact]
public void Should_Detect_Lock_File_Content_Changes()
{
    // Test that changing package.json versions triggers rebuild
    // even if lock file timestamp doesn't change
}

[Fact] 
public void Should_Invalidate_Vite_Cache_On_Dependency_Change()
{
    // Test that node_modules/.vite gets removed when dependencies change
}

[Fact]
public void Should_Handle_Yarn_PnP_Content_Changes()
{
    // Test that .pnp.cjs content changes are detected
}
```

## Backward Compatibility

- Keep existing timestamp logic as fallback
- Gradually migrate to content-based detection
- Maintain support for all package managers
- Preserve current incremental build behavior