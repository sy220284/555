import { spawn } from 'node:child_process'
import { access, rename } from 'node:fs/promises'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const workspaceRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const disabledSuffix = '.repository-scope-disabled'
const gateDisabledSuffix = '.repository-gate-disabled'

// These suites validate examples/docs that repository-policy intentionally keeps
// out of this product-source repository. Keep the test sources checked in, but
// exclude them from every repository test invocation through this single entry.
const repositoryScopeExcludes = [
  'apps/cli/tests/memory-mcp-configs.spec.ts',
  'packages/shell/tool-pwsh/tests/loader.spec.ts',
  'packages/typert/generator/tests/cordis-catalog.spec.ts',
]

const coverage = process.argv.slice(2)
if (coverage.some(argument => argument !== '--coverage')) {
  throw new Error(`unsupported repository test argument: ${coverage.join(' ')}`)
}

async function exists(path) {
  try {
    await access(path)
    return true
  } catch (error) {
    if (error?.code === 'ENOENT') return false
    throw error
  }
}

const ownedRenames = []

try {
  for (const relativePath of repositoryScopeExcludes) {
    const source = resolve(workspaceRoot, relativePath)
    const disabled = `${source}${disabledSuffix}`
    const gateDisabled = `${source}${gateDisabledSuffix}`
    const sourceExists = await exists(source)
    const disabledExists = await exists(disabled)
    const gateDisabledExists = await exists(gateDisabled)

    if (disabledExists) {
      throw new Error(`repository test scope stale disabled copy: ${relativePath}${disabledSuffix}`)
    }
    if (sourceExists && gateDisabledExists) {
      throw new Error(`repository test scope conflict: both active and gate-disabled copies exist for ${relativePath}`)
    }
    if (!sourceExists && !gateDisabledExists) {
      throw new Error(`repository test scope inventory drift: missing ${relativePath}`)
    }

    // Existing CI lanes may already have isolated the file with the
    // .repository-gate-disabled suffix. In that case the outer lane owns
    // restoration; otherwise this shared runner owns the temporary rename.
    if (sourceExists) {
      await rename(source, disabled)
      ownedRenames.push([disabled, source])
    }
  }

  const vitestCli = resolve(workspaceRoot, 'node_modules/vitest/vitest.mjs')
  const args = [vitestCli, 'run', ...(coverage.includes('--coverage') ? ['--coverage'] : [])]
  const exitCode = await new Promise((resolveExit, reject) => {
    const child = spawn(process.execPath, args, {
      cwd: workspaceRoot,
      env: process.env,
      stdio: 'inherit',
    })
    child.once('error', reject)
    child.once('exit', (code, signal) => {
      if (signal) {
        console.error(`repository tests terminated by signal ${signal}`)
        resolveExit(1)
        return
      }
      resolveExit(code ?? 1)
    })
  })
  process.exitCode = exitCode
} finally {
  for (const [disabled, source] of ownedRenames.reverse()) {
    await rename(disabled, source)
  }
}
