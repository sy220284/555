/** VitePress：只投影仓库根 TECHNICAL.md。 */
import { writeFileSync } from 'node:fs'
import { resolve } from 'node:path'
import type { PageData, SiteConfig } from 'vitepress'
import type { ViteDevServer } from 'vite'
import { withMermaid } from 'vitepress-plugin-mermaid'
import { docsSourceFiles, emitRawMarkdownPages, llmsTxt, projectDocs, rawMarkdownRoute } from '../../scripts/project-doc-site.ts'

projectDocs()
const base = process.env.DOCS_BASE ?? '/'
const siteIdentity = { title: '555 工作台', description: '555 工作台完整中文技术文档' }

function watchCanonicalDocs(server: ViteDevServer): void {
  const sources = docsSourceFiles()
  server.watcher.add(sources)
  server.watcher.on('change', (changed) => {
    if (sources.includes(changed)) projectDocs()
  })
}

function serveRawMarkdown(server: ViteDevServer): void {
  server.middlewares.use((req, res, next) => {
    if (req.url === undefined || (req.method !== 'GET' && req.method !== 'HEAD')) return next()
    const fetchDest = req.headers['sec-fetch-dest']
    if (fetchDest !== undefined && fetchDest !== 'document') return next()
    const pathname = req.url.split(/[?#]/, 1)[0] ?? ''
    const sitePath = pathname.startsWith(base) ? pathname.slice(base.length) : pathname.replace(/^\//, '')
    if (sitePath === 'llms.txt') {
      res.setHeader('Content-Type', 'text/plain; charset=utf-8')
      res.end(llmsTxt({ base, ...siteIdentity }))
      return
    }
    const content = sitePath.endsWith('.md') ? rawMarkdownRoute(sitePath) : undefined
    if (content === undefined) return next()
    res.setHeader('Content-Type', 'text/markdown; charset=utf-8')
    res.end(content)
  })
}

export default withMermaid({
  title: siteIdentity.title,
  description: siteIdentity.description,
  lang: 'zh-CN',
  base,
  cleanUrls: true,
  srcDir: '.generated',
  cacheDir: '.cache',
  outDir: '.dist',
  buildEnd(siteConfig: SiteConfig) {
    emitRawMarkdownPages(siteConfig.outDir)
    writeFileSync(resolve(siteConfig.outDir, 'llms.txt'), llmsTxt({ base, ...siteIdentity }))
  },
  themeConfig: {
    search: { provider: 'local' },
    outline: { label: '本页目录', level: 'deep' },
    editLink: {
      pattern: ({ frontmatter }: PageData) => {
        const source = Reflect.get(frontmatter, 'editSource')
        if (typeof source !== 'string') throw new Error('Projected documentation page has no editSource frontmatter.')
        return `https://github.com/sy220284/555/edit/main/${source}`
      },
      text: '在 GitHub 上编辑此页',
    },
    socialLinks: [{ icon: 'github', link: 'https://github.com/sy220284/555' }],
  },
  vite: {
    publicDir: resolve(import.meta.dirname, '../public'),
    plugins: [{
      name: 'workbench-single-doc-projector',
      configureServer(server) {
        watchCanonicalDocs(server)
        serveRawMarkdown(server)
      },
    }],
  },
  mermaid: {},
})
