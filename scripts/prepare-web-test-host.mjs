import { spawnSync } from 'node:child_process'

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

if (process.platform === 'linux' && !bwrapUsable()) {
  if (process.env.CI !== 'true') {
    throw new Error('Web tests require a usable bubblewrap sandbox on Linux. Install bubblewrap, then retry.')
  }

  run('sudo', ['apt-get', 'install', '-y', '--no-install-recommends', 'bubblewrap'])

  if (!bwrapUsable()) {
    throw new Error('bubblewrap was installed but failed the sandbox functional probe')
  }
}
