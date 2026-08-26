/** 单一中文技术文档的公开站点清单。 */

export type DocsLocale = 'root' | 'en'
export type DocsSidebar = 'zh-reference' | 'en-reference'

export interface DocsPage {
  locale: DocsLocale
  contentLocale: 'zh-CN' | 'en-US'
  source: string
  route: string
  label: string
  sidebar: DocsSidebar | null
  section: string
  order: number
  outline?: number | readonly [number, number] | 'deep' | false
  sourceAliases?: string[]
}

export const docsPages: DocsPage[] = [{
  locale: 'root',
  contentLocale: 'zh-CN',
  source: 'README.md',
  route: 'index.md',
  label: '555 工作台完整技术文档',
  sidebar: 'zh-reference',
  section: '技术文档',
  order: 0,
  outline: 'deep',
}]

export const localeCollections: Record<DocsLocale, DocsSidebar[]> = {
  root: ['zh-reference'],
  en: [],
}

const sections: Record<DocsLocale, readonly { label: string; collapsed?: boolean }[]> = {
  root: [{ label: '技术文档' }],
  en: [],
}

export function sectionSpec(locale: DocsLocale, label: string): { label: string; collapsed?: boolean; index: number } {
  const declared = sections[locale]
  const index = declared.findIndex(item => item.label === label)
  if (index < 0) throw new Error(`Sidebar section ${JSON.stringify(label)} has no placement in ${locale}.`)
  const item = declared[index]
  if (item === undefined) throw new Error('Sidebar section disappeared during lookup.')
  return { ...item, index }
}

export function orderedPages(locale: DocsLocale, collection: DocsSidebar): DocsPage[] {
  return docsPages
    .filter(page => page.locale === locale && page.sidebar === collection)
    .sort((a, b) => a.order - b.order)
}

export function routeLink(route: string): string {
  return `/${route.replace(/(?:index)?\.md$/, '')}`
}

export function landingLink(locale: DocsLocale, collection: DocsSidebar): string {
  const first = orderedPages(locale, collection)[0]
  if (first === undefined) throw new Error(`Documentation collection ${collection} is empty.`)
  return routeLink(first.route)
}
