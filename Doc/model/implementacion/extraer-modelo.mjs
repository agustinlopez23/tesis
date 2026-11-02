// Extracts the implemented data model from the live PostgreSQL schema and the C# sources.
// Output: modelo-implementado.json (input of Doc/Seminario_Entrega_2/tools/build-model.mjs)
// and diccionario-datos.md. Spanish descriptions come from descripciones.json.
//
// Usage: node Doc/model/implementacion/extraer-modelo.mjs [--container app-database-1]
//          [--user marketplace] [--db marketplace]
// The schema is read with psql inside the database container, so no password is handled here.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../..');
const argv = process.argv.slice(2);
const arg = (name, fallback) => { const i = argv.indexOf(name); return i >= 0 ? argv[i + 1] : fallback; };
const container = arg('--container', 'app-database-1');
const dbUser = arg('--user', 'marketplace');
const dbName = arg('--db', 'marketplace');

// Module split used by the thesis figures. Infrastructure tables are listed apart.
const MODULES = [
  { section: '01-negocios', title: 'Negocios, identidad y planes',
    tables: ['user_accounts', 'roles', 'user_account_roles', 'user_sessions', 'businesses',
      'business_categories', 'business_photos', 'business_subscriptions', 'admin_actions'] },
  { section: '02-comercio', title: 'Catálogo, solicitudes, ventas e inventario',
    tables: ['catalog_products', 'product_photos', 'inventory_balances', 'inventory_movements',
      'customer_orders', 'order_lines', 'order_changes', 'sales', 'sale_lines'] },
  { section: '03-ranking', title: 'Ranking, campañas y medición',
    tables: ['ranking_contexts', 'ranking_snapshots', 'ranking_opportunities', 'ranking_contributions',
      'campaigns', 'campaign_changes', 'campaign_events', 'business_activity', 'owner_activity'],
    // Tables without relations inside the module are stacked to keep the figure narrow.
    erLayout: { apilar: [['campaign_events', 'business_activity'], ['ranking_contributions', 'owner_activity']] },
    umlLayout: { apilar: [['CampaignEvent', 'BusinessActivity'], ['ExposureBalance', 'OwnerActivity']] } },
];
const EXCLUDED = {
  __EFMigrationsHistory: 'historial de migraciones de Entity Framework',
  spatial_ref_sys: 'catálogo de sistemas de referencia de PostGIS',
  data_protection_keys: 'claves de ASP.NET Core Data Protection (infraestructura)',
};

// Domain class that represents each table. Ranking tables are written with SQL and are
// represented in code by records of the business layer.
const CLASSES = {
  user_accounts: 'UserAccount', roles: 'Role', user_account_roles: 'UserAccountRole',
  user_sessions: 'UserSession', businesses: 'BusinessProfile', business_categories: 'BusinessCategory',
  business_photos: 'BusinessPhoto', business_subscriptions: 'BusinessSubscription', admin_actions: 'AdminAction',
  catalog_products: 'CatalogProduct', product_photos: 'ProductPhoto', inventory_balances: 'InventoryBalance',
  inventory_movements: 'InventoryMovement', customer_orders: 'CustomerOrder', order_lines: 'OrderLine',
  order_changes: 'OrderChange', sales: 'Sale', sale_lines: 'SaleLine',
  ranking_contexts: 'RankingContext', ranking_snapshots: 'RankingSnapshot',
  ranking_opportunities: 'RankingOpportunity', ranking_contributions: 'ExposureBalance',
  campaigns: 'Campaign', campaign_changes: 'CampaignChange', campaign_events: 'CampaignEvent',
  business_activity: 'BusinessActivity', owner_activity: 'OwnerActivity',
};
// Identifier-like members omitted from the class figures besides Guid-typed and *Id members.
const HIDDEN_MEMBERS = { RankingContext: ['Key'], RankingSnapshot: ['ContextKey'], AdminAction: ['TargetId'] };

// Business-layer operations that act on each class; every entry is checked against the source.
const OPERATIONS = {
  UserAccount: ['AuthenticationService.RegisterAsync', 'AuthenticationService.LoginAsync', 'AdminService.ModerateAsync'],
  UserSession: ['AuthenticationService.GetSessionAsync', 'AuthenticationService.LogoutAsync'],
  BusinessProfile: ['BusinessService.CreateAsync', 'BusinessService.UpdateAsync', 'BusinessService.GetPublishedAsync'],
  BusinessCategory: ['AdminService.CreateCategoryAsync', 'AdminService.UpdateCategoryAsync', 'AdminService.DeleteCategoryAsync'],
  BusinessPhoto: ['BusinessProfileService.SavePhotoAsync', 'BusinessProfileService.DeletePhotoAsync'],
  BusinessSubscription: ['CatalogService.PlanAsync', 'CatalogService.GrantAsync', 'CatalogService.RevokeAsync'],
  AdminAction: ['AdminService.AuditAsync'],
  CatalogProduct: ['CatalogService.SaveAsync', 'CatalogService.PublicAsync', 'CatalogDraftService.Generate', 'ProductDiscoveryService.Read'],
  ProductPhoto: ['CatalogService.PhotoAsync', 'CatalogService.ReadPhotoAsync'],
  InventoryBalance: ['SalesService.Inventory', 'SalesService.Adjust'],
  InventoryMovement: ['SalesService.Movements'],
  CustomerOrder: ['OrderService.CreateAsync', 'OrderService.GuestAsync', 'OrderService.ListAsync', 'OrderService.UpdateAsync'],
  Sale: ['SalesService.Create', 'SalesService.List', 'SalesService.Void'],
  RankingContext: ['MagazineRankingCandidates.PrepareAsync'],
  RankingSnapshot: ['RankingSnapshotService.CreateAsync', 'RankingSnapshotPages.ReadAsync'],
  RankingOpportunity: ['RankingOpportunityBuilder.Build'],
  ExposureBalance: ['ExposureLedger.Targets', 'ExposureLedger.Observe'],
  Campaign: ['CampaignService.Save', 'CampaignService.Act', 'CampaignService.Public', 'CampaignService.Metrics'],
  CampaignEvent: ['CampaignService.Record', 'CampaignService.Purge'],
  BusinessActivity: ['ActivityService.RecordAsync', 'ActivityService.ReadAsync', 'ActivityService.PurgeAsync'],
  OwnerActivity: ['OwnerActivityService.RecordAsync', 'OwnerActivitySettings.Signal'],
};

const SQL = `select json_build_object(
  'columns', (select json_agg(json_build_object('table', c.table_name, 'name', c.column_name,
      'nullable', c.is_nullable = 'YES', 'type', format_type(a.atttypid, a.atttypmod)) order by c.table_name, c.ordinal_position)
    from information_schema.columns c
    join pg_attribute a on a.attrelid = ('public.' || quote_ident(c.table_name))::regclass and a.attname = c.column_name
    join pg_class t on t.oid = a.attrelid and t.relkind = 'r'
    where c.table_schema = 'public'),
  'constraints', (select json_agg(json_build_object('table', conrelid::regclass::text, 'type', contype::text,
      'columns', (select json_agg(attname order by k.ord) from unnest(conkey) with ordinality k(n, ord)
                  join pg_attribute on attrelid = conrelid and attnum = k.n),
      'ref', confrelid::regclass::text,
      'definition', pg_get_constraintdef(oid)))
    from pg_constraint where connamespace = 'public'::regnamespace and contype in ('p', 'f', 'u', 'c')),
  'unique_indexes', (select json_agg(json_build_object('table', t.relname,
      'columns', (select json_agg(attname order by k.ord) from unnest(ix.indkey) with ordinality k(n, ord)
                  join pg_attribute on attrelid = t.oid and attnum = k.n)))
    from pg_index ix join pg_class t on t.oid = ix.indrelid
    where t.relnamespace = 'public'::regnamespace and ix.indisunique and not ix.indisprimary))`;

function readSchema() {
  const out = execFileSync('docker', ['exec', container, 'psql', '-U', dbUser, '-d', dbName, '-At', '-c', SQL],
    { encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
  const data = JSON.parse(out);
  data.constraints = data.constraints.map(c => ({ ...c, table: c.table.replaceAll('"', ''), ref: c.ref === '-' ? null : c.ref }));
  return data;
}

// Short SQL type for figures (no spaces) and storage size for the dictionary.
function shortType(type) {
  return type.replace(/^character varying/, 'varchar').replace('timestamp with time zone', 'timestamptz')
    .replace('double precision', 'float8');
}
function storage(table, column, type, checks) {
  const varchar = type.match(/^character varying\((\d+)\)$/);
  if (varchar) return `${varchar[1]} caracteres`;
  const numeric = type.match(/^numeric\((\d+),(\d+)\)$/);
  if (numeric) return `${numeric[1]} dígitos, ${numeric[2]} decimales`;
  const fixed = { uuid: '16 bytes', smallint: '2 bytes', integer: '4 bytes', bigint: '8 bytes', boolean: '1 byte',
    'double precision': '8 bytes', 'timestamp with time zone': '8 bytes' };
  if (fixed[type]) return fixed[type];
  if (type.startsWith('geography(Point')) return '1 punto, 2 coordenadas';
  if (type === 'bytea') return checks.some(d => d.includes(`octet_length(${column})`) && d.includes('1048576')) ? 'hasta 1 MB' : 'Variable';
  if (type === 'text' || type === 'jsonb') return 'Variable';
  throw new Error(`Unknown storage for ${table}.${column}: ${type}`);
}

// --- C# sources -------------------------------------------------------------------------
const src = (dir) => fs.readdirSync(dir).filter(f => f.endsWith('.cs'))
  .map(f => ({ file: path.join(dir, f), text: fs.readFileSync(path.join(dir, f), 'utf8') }));
const modelsDir = path.join(root, 'App/backend/src/Marketplace.Models');
const bllDir = path.join(root, 'App/backend/src/Marketplace.BLL');
const sources = [...src(modelsDir), ...src(bllDir)];

function splitTopLevel(text) {
  const parts = []; let depth = 0; let current = '';
  for (const ch of text) {
    if (ch === '<' || ch === '(') depth++;
    if (ch === '>' || ch === ')') depth--;
    if (ch === ',' && depth === 0) { parts.push(current); current = ''; } else current += ch;
  }
  if (current.trim()) parts.push(current);
  return parts.map(p => p.trim()).filter(Boolean);
}

// Returns [{ name, type }] for a class with auto-properties or a positional record.
function members(className) {
  for (const { text } of sources) {
    const record = text.match(new RegExp(`public sealed record ${className}\\(([\\s\\S]*?)\\)\\s*[;{]`));
    if (record) {
      return splitTopLevel(record[1]).map(p => p.replace(/\s*=.*$/, '').trim())
        .map(p => { const i = p.lastIndexOf(' '); return { type: p.slice(0, i).trim(), name: p.slice(i + 1) }; });
    }
    const start = text.search(new RegExp(`public sealed class ${className}\\b`));
    if (start < 0) continue;
    let i = text.indexOf('{', start); let depth = 0; const begin = i;
    for (; i < text.length; i++) { if (text[i] === '{') depth++; if (text[i] === '}' && --depth === 0) break; }
    const body = text.slice(begin + 1, i);
    return [...body.matchAll(/public (?:required )?([\w<>\[\]?, ]+?) (\w+) \{ get;/g)].map(x => ({ type: x[1], name: x[2] }));
  }
  throw new Error(`Class or record not found: ${className}`);
}

function operation(ref) {
  const [owner, method] = ref.split('.');
  const file = sources.find(s => new RegExp(`(class|record) ${owner}\\b`).test(s.text));
  if (!file) throw new Error(`Owner not found for operation ${ref}`);
  // Search inside the owner's declaration so interface members declared earlier do not match.
  const body = file.text.slice(file.text.search(new RegExp(`(class|record) ${owner}\\b`)));
  const line = body.split('\n').find(l => !l.trim().startsWith('//') && new RegExp(`\\s${method}\\(`).test(l));
  if (!line) throw new Error(`Operation not found: ${ref}`);
  const sig = line.match(new RegExp(`^\\s*(?:public\\s+)?(?:static\\s+)?(?:async\\s+)?(.+?)\\s+${method}\\(`));
  if (!sig) throw new Error(`Signature not parsed: ${ref}`);
  let ret = sig[1].trim();
  ret = ret === 'Task' ? 'void' : ret.replace(/^Task<(.+)>$/, '$1');
  ret = ret.replace(/\?/g, '').replace(/^IReadOnlyList<(.+)>$/, '$1[]').replace(/^IReadOnlyDictionary<(.+)>$/, 'Dictionary<$1>');
  return `${method}(): ${ret}`;
}

const umlType = (t) => t.replace(/^IReadOnlyList<(.+)>$/, '$1[]');

// --- Build ---------------------------------------------------------------------------------
const schema = readSchema();
const text = JSON.parse(fs.readFileSync(path.join(here, 'descripciones.json'), 'utf8'));
const tables = [...new Set(schema.columns.map(c => c.table))];
const assigned = MODULES.flatMap(g => g.tables);
const unassigned = tables.filter(t => !assigned.includes(t) && !EXCLUDED[t]);
if (unassigned.length) throw new Error(`Tables without module: ${unassigned.join(', ')}`);
const missing = assigned.filter(t => !tables.includes(t));
if (missing.length) throw new Error(`Tables not found in the database: ${missing.join(', ')}`);

const cons = (table, type) => schema.constraints.filter(c => c.table === table && c.type === type);
const uniqueSets = (table) => [...cons(table, 'u').map(c => c.columns),
  ...(schema.unique_indexes || []).filter(i => i.table === table).map(i => i.columns)];
const pkOf = (table) => cons(table, 'p')[0]?.columns || [];
const fkOf = (table) => cons(table, 'f');

const describe = (table, column) => {
  const d = text.columns[`${table}.${column}`];
  if (!d) throw new Error(`Missing description: ${table}.${column}`);
  return d;
};

const dictionary = [];
const erGroups = MODULES.map(g => ({
  section: g.section, title: g.title, ...(g.erLayout ? { layout: g.erLayout } : {}),
  entities: g.tables.map(table => {
    if (!text.tables[table]) throw new Error(`Missing table description: ${table}`);
    const cols = schema.columns.filter(c => c.table === table);
    const pk = pkOf(table); const fks = fkOf(table).flatMap(f => f.columns);
    const uks = new Set(uniqueSets(table).flat());
    const checks = cons(table, 'c').map(c => c.definition);
    const fields = cols.map(c => {
      const flags = [pk.includes(c.name) && 'PK', fks.includes(c.name) && 'FK', uks.has(c.name) && 'UK'].filter(Boolean).join(',');
      dictionary.push({ table, name: c.name, type: c.type, length: storage(table, c.name, c.type, checks),
        nullable: c.nullable, key: flags.replaceAll(',', ', ') || '—', description: describe(table, c.name) });
      return [shortType(c.type), c.name, flags].filter(Boolean).join(' ');
    });
    return [table, text.tables[table], fields.join(';')];
  }),
}));

// Relations: [parent, child, multiplicity at parent, multiplicity at child, label].
const relations = MODULES.flatMap(g => g.tables).flatMap(table => fkOf(table).map(f => {
  const key = `${table}.${f.columns.join(',')}`;
  const label = text.relations[key];
  if (!label) throw new Error(`Missing relation label: ${key}`);
  const nullable = f.columns.some(col => schema.columns.find(c => c.table === table && c.name === col).nullable);
  const unique = [pkOf(table), ...uniqueSets(table)].some(set => set.length === f.columns.length && set.every(x => f.columns.includes(x)));
  return { parent: f.ref, child: table, parentEnd: nullable ? '0..1' : '1', childEnd: unique ? '0..1' : '0..*', label, column: f.columns.join(',') };
}));

const classGroups = MODULES.map(g => ({
  section: g.section, title: g.title, ...(g.umlLayout ? { layout: g.umlLayout } : {}),
  entities: g.tables.map(table => {
    const name = CLASSES[table];
    if (!text.classes[name]) throw new Error(`Missing class description: ${name}`);
    const navigation = new Set(Object.values(CLASSES));
    const attrs = members(name).filter(({ type, name: member }) => {
      const base = type.replace(/[?]/g, '').replace(/^(ICollection|List|IReadOnlyList)<(.+)>$/, '$2');
      const identifier = /^Guid\??$/.test(type) || member === 'Id' || /Id$/.test(member);
      return !identifier && !navigation.has(base) && !(HIDDEN_MEMBERS[name] || []).includes(member);
    });
    return [name, text.classes[name], attrs.map(a => `${umlType(a.type)} ${a.name}`).join(';')];
  }),
}));
const operations = Object.fromEntries(Object.entries(OPERATIONS).map(([cls, refs]) => [cls, refs.map(operation)]));

const model = {
  source: { database: `${dbName} (PostgreSQL/PostGIS, contenedor ${container})`, excluded: EXCLUDED },
  groups: erGroups,
  rels: relations.map(r => [r.parent, r.child, r.parentEnd, r.childEnd, r.label]),
  operations: {},
  classes: {
    groups: classGroups,
    rels: relations.map(r => [CLASSES[r.parent], CLASSES[r.child], r.parentEnd, r.childEnd, r.label]),
    operations,
  },
  dictionary,
};
fs.writeFileSync(path.join(here, 'modelo-implementado.json'), JSON.stringify(model, null, 2) + '\n', 'utf8');

// --- Data dictionary (Markdown) -----------------------------------------------------------
let md = '# Diccionario de datos del modelo implementado\n\n'
  + 'Generado por `extraer-modelo.mjs` desde el esquema de la base de datos migrada. Los identificadores '
  + 'están en inglés, como en el código; las descripciones, en español. «Clave» indica clave primaria (PK), '
  + 'foránea (FK) o única (UK). Las restricciones de control (CHECK) se listan al pie de cada tabla.\n\n'
  + 'Tablas excluidas por no pertenecer al dominio: '
  + Object.entries(EXCLUDED).map(([t, why]) => `\`${t}\` (${why})`).join('; ') + '.\n';
for (const g of MODULES) {
  md += `\n## ${g.title}\n`;
  for (const table of g.tables) {
    md += `\n### ${table}: ${text.tables[table]}\n\n| Campo | Tipo | Longitud | Nulo | Clave | Descripción |\n| --- | --- | --- | --- | --- | --- |\n`;
    for (const f of dictionary.filter(d => d.table === table)) {
      md += `| ${f.name} | ${f.type} | ${f.length} | ${f.nullable ? 'Sí' : 'No'} | ${f.key} | ${f.description} |\n`;
    }
    const fks = fkOf(table).map(f => `\`${f.columns.join(', ')}\` → \`${f.ref}\``);
    if (fks.length) md += `\nClaves foráneas: ${fks.join('; ')}.\n`;
    const checks = cons(table, 'c').map(c => `\`${c.definition.replaceAll('|', '\\|')}\``);
    if (checks.length) md += `\nRestricciones: ${checks.join('; ')}.\n`;
  }
}
fs.writeFileSync(path.join(here, 'diccionario-datos.md'), md, 'utf8');

const fkCount = relations.length;
console.log(`Esquema leído: ${assigned.length} tablas de dominio, ${dictionary.length} columnas, ${fkCount} claves foráneas; `
  + `${classGroups.flatMap(g => g.entities).length} clases, ${Object.values(operations).flat().length} operaciones. `
  + `Excluidas: ${Object.keys(EXCLUDED).filter(t => tables.includes(t)).join(', ')}.`);
