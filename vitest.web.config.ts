import tsconfigPaths from 'vite-tsconfig-paths'
import { defineConfig } from 'vitest/config'
import { standardDecoratorPlugin, vitestExecArgv } from './vitest.shared.ts'

// Web browser lane: real host entry points, built-client interaction snapshots,
// and replayed keyless e2e scenarios outside the unit/e2e includes. Linux PR CI
// pins DSH_SNAPSHOT=replay and compares committed goldens; record/refresh remain
// explicit local workflows. Real-model cases self-skip without DEEPSEEK_API_KEY.
try {
  // Node >= 21.7 native; throws when the file does not exist.
  process.loadEnvFile(new URL('.env', import.meta.url).pathname)
} catch {
  // No .env — fine, the environment may already carry the variables.
}

const repositoryScopeExcludes = [
  'apps/web/tests/bash-abort-row.e2e.ts',
  'apps/web/tests/schedule-after.e2e.ts',
  'apps/web/tests/skill-tool-row.e2e.ts',
  'apps/web/tests/workflow-run.e2e.ts',
]

export default defineConfig({
  // Same resolution note as vitest.config.ts: the tsconfig.base.json paths
  // facade has no include (match-all), so apps/web/tests resolves bare
  // workspace imports to source like every other lane.
  plugins: [
    tsconfigPaths({ projects: ['./tsconfig.base.json'] }),
    standardDecoratorPlugin(),
  ],
  test: {
    execArgv: vitestExecArgv,
    include: [
      'apps/web/tests/**/*.e2e.ts',
      'apps/web/tests/**/*.snapshot.ts',
    ],
    // These suites consume root examples that repository-policy intentionally
    // excludes from this product-source repository. Keep the test sources but
    // exclude them from this repository's Web execution scope.
    exclude: repositoryScopeExcludes,
    // Browser boot + real-model turns are slow; files share one browser, run serial.
    testTimeout: 180_000,
    hookTimeout: 120_000,
    fileParallelism: false,
  },
})
