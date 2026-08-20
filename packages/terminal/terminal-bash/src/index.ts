/**
 * Persistent shell PTY backend over the subprocess terminal primitive, shared
 * sandbox policy, bounded output, and provider-owned session cleanup.
 * @module @deepseek-ai/dsh-terminal-bash
 */

import { Context } from '@deepseek-ai/cordis'
import type { Agent } from '@deepseek-ai/dsh-agent'
import type { Session, SessionEvent } from '@deepseek-ai/dsh-session'
import { TerminalBackendCleanupError } from '@deepseek-ai/dsh-terminal'
import type { TerminalBackend, TerminalBackendSpawnSpec } from '@deepseek-ai/dsh-terminal'
import type { SubprocessTerminalHandle, SubprocessTerminalSpawnSpec } from '@deepseek-ai/dsh-subprocess'
import type { SandboxExecutionPolicy } from '@deepseek-ai/dsh-sandbox'
import { effectiveSandboxMode } from '@deepseek-ai/dsh-sandbox-policy'
import { ENCODING_PREAMBLE } from '@deepseek-ai/dsh-pwsh-local'
import { type Config, type ResolvedConfig, resolveConfig, type ShellDialect, validateConfig } from './config.ts'
import { LocalPtySession } from './session.ts'
import { CONTROLLED_PROMPT } from './sanitize.ts'

export { Config } from './config.ts'
export type { Config as TerminalLocalConfig } from './config.ts'

/** Cordis plugin name. */
export const name = 'terminal-bash'
/** Required services: PTY registry, shared confinement policy, and process substrate. */
export const inject = ['terminals', 'sandboxPolicy', 'subprocess']

interface SandboxModeFenceState {
  pty: Context['terminals']
  sandboxPolicy: Context['sandboxPolicy']
}

const sandboxModeFences = new WeakMap<Agent, SandboxModeFenceState>()

function ensureSandboxModeFence(ctx: Context, owner: Agent): void {
  const existing = sandboxModeFences.get(owner)
  if (existing !== undefined) {
    existing.pty = ctx.terminals
    existing.sandboxPolicy = ctx.sandboxPolicy
    return
  }
  const state: SandboxModeFenceState = { pty: ctx.terminals, sandboxPolicy: ctx.sandboxPolicy }
  sandboxModeFences.set(owner, state)
  owner.ctx.on('internal/dispatch', (_mode, eventName, args) => {
    if (eventName !== 'session/event') return
    const [session, event] = args as [Session, SessionEvent]
    if (session !== owner.session || event.type !== 'sandbox/mode') return
    const currentMode = effectiveSandboxMode(session.events) ?? state.sandboxPolicy.defaultMode
    if (event.data.mode === currentMode || !state.pty.hasOwnerActivity(owner)) return
    throw new Error(
      `cannot change sandbox mode from "${currentMode}" to "${event.data.mode}" while persistent terminal sessions are open or being created; wait for creation to settle and close them first`,
    )
  }, { global: true })
}

function childEnvironment(spec: TerminalBackendSpawnSpec, dialect: ShellDialect): Record<string, string> {
  // The subprocess provider supplies its own scrubbed ambient base; these are
  // deliberate terminal-specific overrides layered after it.
  const common = {
    TERM: 'dumb',
    PAGER: 'cat',
    GIT_PAGER: 'cat',
    DSH_SHELL: '1',
    DSH_SESSION_ID: spec.owner.id,
    DSH_PTY_SESSION_ID: spec.sessionId,
  }
  if (dialect === 'pwsh') {
    // pwsh ignores PS1/PROMPT_COMMAND; its prompt is installed by the startup
    // bootstrap instead, and NO_COLOR keeps the renderer quiet.
    return { ...common, NO_COLOR: '1' }
  }
  return {
    ...common,
    PS1: CONTROLLED_PROMPT,
    // Re-asserting PS1 after the marker keeps prompt readiness working when a
    // command overwrote the shell variable: bash runs PROMPT_COMMAND before
    // rendering each prompt, so an override never survives to the next prompt.
    PROMPT_COMMAND: `printf "\\033]133;D;%s\\007" "$?"; PS1='${CONTROLLED_PROMPT}'`,
    BASH_SILENCE_DEPRECATION_WARNING: '1',
  }
}

/**
 * The managed pwsh startup installs its global prompt before ConsoleHost begins
 * its first read. The explicit global scope survives the temporary `-Command`
 * pipeline. This avoids submitting bootstrap text into PSReadLine while that
 * module is still taking ownership of the terminal. The prompt emits the
 * shared OSC `133;D;` + BEL marker; `[char]27`/`[char]7` build those control
 * bytes at runtime. Build the printable prompt in two pieces as an additional
 * invariant: neither argv nor an echoed command contains a complete readiness
 * token that could impersonate the installed prompt.
 */
const PWSH_PROMPT_HEAD = CONTROLLED_PROMPT.slice(0, 1)
const PWSH_PROMPT_TAIL = CONTROLLED_PROMPT.slice(1)
export const PWSH_PROMPT_SETUP =
  `function global:prompt { [Console]::Write([string]::Concat([char]27, ']133;D;', [int]$LASTEXITCODE, [char]7)); ('${PWSH_PROMPT_HEAD}' + '${PWSH_PROMPT_TAIL}') }`

/**
 * Line-oriented ConsoleHost reader for the automation-owned PTY. Defining the
 * hook before the first interactive read prevents PSReadLine auto-loading and
 * its first-read input flush without unloading a module that already owns a
 * read. `-NoEnumerate` preserves an empty submitted line as one return value.
 */
const PWSH_READLINE_SETUP =
  'function global:PSConsoleHostReadLine { Write-Output -NoEnumerate ([Console]::ReadLine()) }; '

/** Complete PowerShell startup command passed as one argv element. */
export const PWSH_STARTUP_COMMAND = ENCODING_PREAMBLE + PWSH_READLINE_SETUP + PWSH_PROMPT_SETUP

function spawnArgv(ctx: Context, config: ResolvedConfig, policy: SandboxExecutionPolicy): string[] {
  const shellArgs = config.shellDialect === 'pwsh'
    ? [...config.shellArgs, '-NoExit', '-Command', PWSH_STARTUP_COMMAND]
    : config.shellArgs
  const argv = [config.shellPath, ...shellArgs]
  if (policy.mode === 'danger-full-access') return argv
  const sandbox = ctx.get('sandbox')
  if (sandbox === undefined) {
    throw new Error(`terminal-bash: sandbox mode "${policy.mode}" requires a ctx.sandbox provider in the execution world`)
  }
  // Re-state the discriminant because object spread does not preserve its narrowed type.
  return sandbox.confine(argv, { ...policy, mode: policy.mode }).argv
}

// TODO(pty-initialize-race-home): Fold this outer abort race into
// LocalPtySession.initialize when the send-state consolidation lands; the
// session already owns the send lifecycle the race protects.
async function startupSession(
  session: LocalPtySession,
  signal?: AbortSignal,
): Promise<void> {
  const start = async (): Promise<void> => { await session.initialize(signal) }
  if (signal === undefined) {
    await start()
    return
  }
  const aborted = Promise.withResolvers<never>()
  const onAbort = (): void => { aborted.reject(signal.reason) }
  signal.addEventListener('abort', onAbort, { once: true })
  try {
    signal.throwIfAborted()
    await Promise.race([start(), aborted.promise])
  } finally {
    signal.removeEventListener('abort', onAbort)
  }
}

/** Local shell backend registered under the configured type. */
export class BashTerminalBackend implements TerminalBackend {
  readonly type: string

  constructor(
    private readonly ctx: Context,
    private readonly config: ResolvedConfig,
    private readonly spawnTerminal: (
      spec: SubprocessTerminalSpawnSpec,
    ) => Promise<SubprocessTerminalHandle> = spec => ctx.subprocess.spawnTerminal(spec),
    private readonly createSession: (
      terminal: SubprocessTerminalHandle,
      config: ResolvedConfig,
    ) => LocalPtySession = (terminal, config) => new LocalPtySession(terminal, config),
  ) {
    this.type = config.backendType
  }

  async spawn(spec: TerminalBackendSpawnSpec): Promise<LocalPtySession> {
    spec.signal?.throwIfAborted()
    ensureSandboxModeFence(this.ctx, spec.owner)
    const policy = this.ctx.sandboxPolicy.resolve({ session: spec.owner.session })
    const argv = spawnArgv(this.ctx, this.config, policy)
    if (argv[0] === undefined) throw new Error('terminal-bash: sandbox returned empty argv')
    const terminal = await this.spawnTerminal({
      argv,
      cwd: spec.cwd ?? policy.workspaceRoot,
      env: childEnvironment(spec, this.config.shellDialect),
      rows: this.config.rows,
      cols: this.config.cols,
      graceMs: this.config.disposeGraceMs,
      signal: spec.signal,
    })
    const session = this.createSession(terminal, this.config)
    try {
      await startupSession(session, spec.signal)
      return session
    } catch (error) {
      try {
        await session.close('PTY startup failed')
      } catch (closeError: unknown) {
        throw new TerminalBackendCleanupError(error, closeError)
      }
      throw error
    }
  }
}

/** Register the local PTY backend. */
export function apply(ctx: Context, config: Config): void {
  const resolved = resolveConfig(config)
  validateConfig(resolved)
  ctx.terminals.registerBackend(new BashTerminalBackend(ctx, resolved))
}
