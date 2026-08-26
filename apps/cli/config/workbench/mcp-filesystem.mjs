#!/usr/bin/env node
import { createRequire } from 'node:module'
import { pathToFileURL } from 'node:url'
const require = createRequire(import.meta.url)
const entry = new URL('../../node_modules/@modelcontextprotocol/server-filesystem/dist/index.js', import.meta.url)
await import(entry.href)
