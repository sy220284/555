import { spawnSync } from 'node:child_process'
import { resolve } from 'node:path'

function run(command, args, options = {}) {
  const result = spawnSync(command, args, {
    stdio: 'inherit',
    ...options,
  })
  if (result.error) throw result.error
  if (result.status !== 0) {
    throw new Error(`${command} ${args.join(' ')} failed with exit code ${result.status ?? 'unknown'}`)
  }
}

function bwrapUsable() {
  const result = spawnSync('bwrap', [
    '--ro-bind', '/', '/',
    '--dev', '/dev',
    '--proc', '/proc',
    '--die-with-parent',
    '--', 'true',
  ], {
    stdio: 'ignore',
    timeout: 5_000,
  })
  return result.status === 0
}

function landlockBinary() {
  return resolve(
    import.meta.dirname,
    '..',
    'native',
    'landlock-run',
    'packages',
    `linux-${process.arch}`,
    'bin',
    'landlock-run',
  )
}

function landlockUsable(binary) {
  const result = spawnSync(binary, ['--probe'], {
    stdio: 'ignore',
    timeout: 5_000,
  })
  return result.status === 0
}

if (process.platform === 'linux' && !bwrapUsable()) {
  if (process.env.CI !== 'true') {
    throw new Error('Web tests require a usable Linux sandbox backend. Install bubblewrap or build native/landlock-run, then retry.')
  }

  run('sudo', ['apt-get', 'install', '-y', '--no-install-recommends', 'musl-tools'])
  run('pnpm', ['--dir', 'native/landlock-run', 'run', 'build:native'])

  const binary = landlockBinary()
  if (!landlockUsable(binary)) {
    throw new Error(`Landlock launcher was built but failed its functional probe: ${binary}`)
  }
}
