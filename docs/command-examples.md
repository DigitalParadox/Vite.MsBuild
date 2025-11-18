# Package Manager Command Examples

## How `--` Works in Package Managers

### NPM
```bash
# package.json: "build": "vite build"
npm run build -- --config custom.config.js --mode production
# Executes: vite build --config custom.config.js --mode production

# Without -- (WRONG):
npm run build --config custom.config.js  # npm thinks --config is for npm!
```

### PNPM
```bash
# package.json: "build": "vite build" 
pnpm run build -- --config custom.config.js --mode production
# Executes: vite build --config custom.config.js --mode production
```

### Yarn (Special Case!)
```bash
# Yarn has TWO equivalent forms:
yarn run build -- --config custom.config.js --mode production  # explicit
yarn build -- --config custom.config.js --mode production      # shorthand

# Both execute: vite build --config custom.config.js --mode production
```

### Bun
```bash
# package.json: "build": "vite build"
bun run build -- --config custom.config.js --mode production
# Executes: vite build --config custom.config.js --mode production
```

## Direct Execution (No `--` needed)

When using dlx/npx (direct execution), no `--` is needed:

```bash
npx vite build --config custom.config.js --mode production
pnpm dlx vite build --config custom.config.js --mode production
yarn dlx vite build --config custom.config.js --mode production
bun x vite build --config custom.config.js --mode production
```

## Why Our Implementation is Correct

Our 3-tier system handles this perfectly:

1. **Custom commands**: User controls everything
2. **Package manager scripts**: Uses `--` to pass args correctly  
3. **Direct execution (dlx)**: No `--` needed since we call vite directly