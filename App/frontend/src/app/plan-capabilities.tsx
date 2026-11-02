// Capabilities as enforced by the API: an active Impulso period unlocks the catalog, product requests,
// product promotions, in-person sales and catalog assistance. Prices are intentionally not shown.
const capabilities: { label: string; presence: boolean }[] = [
  { label: "Perfil público con fotos y contacto por WhatsApp", presence: true },
  { label: "Métricas de actividad del perfil", presence: true },
  { label: "Promociones con una foto del negocio", presence: true },
  { label: "Catálogo público de productos", presence: false },
  { label: "Solicitudes de productos de los clientes", presence: false },
  { label: "Promociones de productos del catálogo", presence: false },
  { label: "Registro de ventas presenciales", presence: false },
  { label: "Asistencia para preparar productos", presence: false },
];
const mark = (enabled: boolean) => enabled ? <><span aria-hidden="true">✓</span><span className="sr-only">Incluido</span></> : <><span aria-hidden="true">—</span><span className="sr-only">No incluido</span></>;

export default function PlanCapabilities({ current }: { current: string }) {
  return <details className="plan-capabilities">
    <summary>Comparar planes Presencia e Impulso</summary>
    <p>Tu plan actual: {current}. La administración gestiona la activación de Impulso.</p>
    <div className="activity-table"><div role="region" aria-label="Comparación de planes" tabIndex={0}><table aria-label="Capacidades de los planes">
      <thead><tr><th scope="col">Capacidad</th><th scope="col">Presencia</th><th scope="col">Impulso</th></tr></thead>
      <tbody>{capabilities.map((item) => <tr key={item.label}><th scope="row">{item.label}</th><td>{mark(item.presence)}</td><td>{mark(true)}</td></tr>)}</tbody>
    </table></div></div>
  </details>;
}
