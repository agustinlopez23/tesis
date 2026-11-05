# UI/UX redesign of the Cerca frontend

Objective: make the web app intuitive, easy to understand and modern-looking, borrowing proven patterns from comparable products. Branch `worktree-ui-ux-redesign` (worktree `.claude/worktrees/ui-ux-redesign`, based on `main`). Delivery: `ask-on-risk`. Test runner: `cd App/frontend; npm test; npm run lint; npm run build` (Vitest + ESLint + Next build). Baseline suite: green on 2026-11-05.

## Specs

- S1. "quiero que en un worktree separado mejoremos la ui y la ux": all work happens in the separate worktree, never in the main checkout where another agent works.
- S2. "hagamos un research de software similares y copiemos cosas que ya funcionan": run research on comparable products (local-business discovery, store pages with WhatsApp ordering, owner dashboards, admin tables) and apply concrete patterns observed there; the applied patterns are recorded in the Log with their source.
- S3. "quiero que la interfaz sea intuitiva, facil de entender, y se vea moderna": every screen must keep its current functionality and accessible names (tests query by Spanish role names and labels) while improving hierarchy, feedback, navigation and visual consistency.
- S4. Assumption (mine): no new runtime dependencies (plain CSS stays; the repo pins exact versions and an npm audit policy). Fonts, if changed, are self-hosted or system stacks, not fetched at build time.
- S5. Assumption (mine): existing Vitest suite, ESLint and `next build` must stay green; Playwright flows in `App/tools/browser-flow.mjs` depend on `.order-checkout [role=alert]` and `.inventory-list`, which must survive.

## Tasks

- T1 (S2) Research comparable products and record applied patterns — route: delegated explorer (read-only web research) — done 2026-11-05, findings in L3.
- T2 (S3, S4) Design foundation: tokens (color, spacing, radius, shadow, type scale), bring the unlayered CSS tail into layers, shared primitives in `ui.tsx` (Alert, EmptyState, Skeleton, Badge, PageHeader, SectionCard), header with mobile menu and account-aware "Mi negocio" — route: inline.
- T3 (S3) Public discovery: unified filter bar on `/explorar` and `/productos` (search + rubro chips + location in one block), consistent product cards and skeletons, clearer ranking copy — route: inline.
- T4 (S3) Business profile and ordering: catalog closer to the top, sticky order summary with item count and feedback on "Agregar", clearer checkout steps — route: inline.
- T5 (S3) Owner dashboard: section navigation (Resumen, Negocio, Catálogo, Solicitudes, Ventas, Campañas, Fotos), setup checklist ("Próximos pasos"), business switcher, panels open by section instead of stacked toggles — route: inline.
- T6 (S3) Admin: shared admin layout with side navigation, page headers, status badges and table-like lists — route: inline.
- T7 (S5) Verification: `npm test`, `npm run lint`, `npm run build`, browser walkthrough with screenshots — route: inline.

## Log

- L1 (2026-11-05, user, verbatim): "quiero que en un worktree separado mejoremos la ui y la ux, hagamos un research de software similares y copiemos cosas que ya funcionan. quiero que la interfaz sea intuitiva, facil de entender, y se vea moderna. Hay otro agente trabajando en la sesion continuemos con el software"
- L3 (2026-11-05) Research (web, read-only) — patterns adopted and their source: sticky search with instant filter chips and removable applied-filter chips (Google Maps local pack, eBay, Baymard); sponsored label in the same position on every sponsored card (FTC/CHI'18 recognition research); profile hero + action row "WhatsApp · Cómo llegar · Compartir" (Google Business Profile, Yelp); floating cart bar with count and total that appears after the first add, cart as a sheet, editable preview before WhatsApp (PedidosYa/Rappi, WhatsApp Business carts, Tiendanube Botão Zap); stock shown inline, never hidden (Tiendanube); owner home = setup guide "X de Y pasos" + KPI row + one callout, grouped sidebar/section nav, empty states with one CTA (Shopify App Home, Square); admin as resource index: title, filter tabs by status, search, status badges with success/warning/critical/neutral tones, audit as dated list (Shopify Polaris); skeletons shaped like the real card, toasts bottom-centre one at a time (2026 roundups); mobile bottom navigation with 3-4 destinations (Material, PedidosYa). Visual: one warm accent (kept: terracotta on ink green), WhatsApp green only on WhatsApp CTAs, radius 4/8/12/pill, two shadow levels, 4-based spacing scale, 44px tap targets. Fonts stay system stacks (S4). Sources: shopify.dev App Home setup-guide/homepage/resource-index, polaris.shopify.com IndexTable, baymard.com filter UI, playbook.ebay.com filtering, developers.facebook.com WhatsApp catalogs, tiendanube.com WhatsApp help, support.google.com/business/answer/9274959, material.io bottom-navigation.
- L2 (2026-11-05) Current state mapped: 8 color variables, no spacing/type scale, unlayered CSS after `globals.css:571` overriding layered rules; `ui.tsx` only has Icon/SiteHeader/SiteFooter/categoryStyle; owner dashboard is one long page with collapsible panels; admin has no layout; checkout renders inline at the bottom of the profile. Baseline `npm test` green.
