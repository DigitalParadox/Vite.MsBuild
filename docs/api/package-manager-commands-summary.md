# Package Manager Native Commands for Dependency Validation

## Summary of Available Commands

Based on research into each package manager's official documentation, here's what each provides for checking dependency synchronization:

### **NPM** 📦
```bash
# 1. Check if dependencies match lockfile (best for our use case)
npm ls --depth=0 --json --silent
# Exit code: 0 = dependencies satisfy lockfile, non-zero = issues

# 2. Dry-run CI install (strict lockfile validation)  
npm ci --dry-run
# Exit code: 0 = lockfile matches package.json, non-zero = mismatch

# 3. Check for outdated packages (different purpose)
npm outdated --json
# Lists packages with newer versions available
```

### **PNPM** ⚡
```bash
# 1. Frozen lockfile check (best for our use case)
pnpm install --frozen-lockfile --dry-run --reporter=silent
# Exit code: 0 = lockfile is in sync, non-zero = needs install

# 2. Check for outdated packages
pnpm outdated --format json
# Lists packages with newer versions available
```

### **Yarn Classic (v1)** 🧶
```bash
# 1. Check integrity (DEPRECATED but still works)
yarn check --integrity --silent
# Exit code: 0 = integrity intact, non-zero = issues

# 2. Better alternative: frozen lockfile install
yarn install --frozen-lockfile --silent
# Exit code: 0 = in sync, non-zero = needs update

# 3. Check for outdated packages  
yarn outdated --json
```

### **Yarn Berry (v2+)** 🫐
```bash
# 1. Immutable install (best for our use case)
yarn install --immutable --silent
# Exit code: 0 = lockfile is current, non-zero = needs update

# 2. Check for outdated packages
yarn outdated --json
```

### **Bun** 🥐
```bash
# 1. Frozen lockfile check (best for our use case)
bun install --frozen-lockfile --dry-run
# Exit code: 0 = lockfile matches, non-zero = needs install

# 2. Check for outdated packages
bun outdated
```

## **Key Insights** 💡

### ✅ **All Package Managers Provide Dependency Sync Checking**
- Every package manager has commands to check if dependencies need updating
- All use **exit codes** to indicate sync status (0 = good, non-zero = needs action)
- Most support **dry-run** modes for validation without actual changes

### 🎯 **Best Commands for Our Use Case**

**For dependency sync validation** (what we need):
```bash
npm ls --depth=0 --json --silent              # NPM
pnpm install --frozen-lockfile --dry-run      # PNPM  
yarn install --frozen-lockfile --silent       # Yarn v1
yarn install --immutable --silent             # Yarn v2+
bun install --frozen-lockfile --dry-run       # Bun
```

**For outdated package detection** (different use case):
```bash
npm outdated --json       # NPM
pnpm outdated --json      # PNPM
yarn outdated --json      # Yarn
bun outdated             # Bun
```

### 🔍 **What These Commands Check**

1. **Lockfile vs package.json consistency**
2. **Installed dependencies vs lockfile synchronization** 
3. **Integrity/checksum verification** (for supported package managers)
4. **Platform-specific dependency availability**

### 🚀 **Implementation Strategy**

Instead of our custom timestamp comparison, we should:

1. **Use package manager native commands** for dependency validation
2. **Check exit codes** to determine if restore is needed
3. **Fall back to timestamp logic** only if native commands fail
4. **Leverage package manager intelligence** for edge cases we might miss

This approach:
- ✅ **More accurate** than timestamp comparison
- ✅ **Handles complex scenarios** (PnP, workspaces, integrity)
- ✅ **Future-proof** as package managers evolve
- ✅ **Simpler code** - let experts handle dependency logic
- ✅ **Better error messages** from package managers themselves

## **Next Steps**

1. Implement native package manager checks as primary detection method
2. Keep timestamp logic as fallback for offline/error scenarios  
3. Add comprehensive tests for each package manager's validation
4. Update diagnostics to show which validation method was used