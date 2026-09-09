import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const dir = path.join(root, 'Doc/Entrega2');
const require = createRequire(path.join(root, '.tools/documentation/package.json'));
const { Resvg } = require('@resvg/resvg-js');
const esc = s => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
const nodes = [
  ['start','startEvent','Interés en producto','consumer',70,165,36,36],
  ['choose','userTask','Revisar productos','consumer',155,155,160,58],
  ['request','serviceTask','Crear solicitud sin reservar stock','platform',375,335,180,64],
  ['sendDecision','exclusiveGateway','¿Envía mensaje?','consumer',610,160,50,50],
  ['send','userTask','Enviar por WhatsApp (externo)','consumer',745,150,180,64],
  ['pending','endEvent','Solicitud pendiente','consumer',765,235,36,36],
  ['attend','userTask','Atender consulta y acordar venta','business',990,515,180,64],
  ['saleDecision','exclusiveGateway','¿Se concreta?','business',1235,520,50,50],
  ['confirm','userTask','Confirmar artículos y cantidades','business',1380,510,180,64],
  ['commit','serviceTask','Registrar venta y stock atómicamente','platform',1620,330,185,70],
  ['sold','endEvent','Venta registrada','platform',1880,345,36,36],
  ['cancel','userTask','Cancelar solicitud con motivo','business',1380,625,180,64],
  ['cancelled','endEvent','Solicitud cancelada','business',1665,640,36,36]
];
const flows = [
  ['f1','start','choose',''],['f2','choose','request',''],['f3','request','sendDecision',''],
  ['f4','sendDecision','send','Sí'],['f5','sendDecision','pending','No'],['f6','send','attend',''],
  ['f7','attend','saleDecision',''],['f8','saleDecision','confirm','Sí'],['f9','saleDecision','cancel','No'],
  ['f10','confirm','commit',''],['f11','commit','sold',''],['f12','cancel','cancelled','']
];
const lanes = [['consumer','Consumidor',100,180],['platform','Plataforma',280,180],['business','Negocio',460,260]];
const byId = Object.fromEntries(nodes.map(n => [n[0],n]));
const waypoints = (a,b) => { const x=byId[a], y=byId[b]; return [[x[4]+x[6],x[5]+x[7]/2],[y[4],y[5]+y[7]/2]]; };
let xml = '<?xml version="1.0" encoding="UTF-8"?>\n<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI" xmlns:dc="http://www.omg.org/spec/DD/20100524/DC" xmlns:di="http://www.omg.org/spec/DD/20100524/DI" id="Definitions" targetNamespace="https://tesis.local/model">\n<bpmn:collaboration id="Collaboration"><bpmn:participant id="Pool" name="Solicitud y venta — proceso propuesto" processRef="Process"/></bpmn:collaboration>\n<bpmn:process id="Process" isExecutable="false"><bpmn:laneSet id="LaneSet">';
for(const [id,name] of lanes) xml+=`<bpmn:lane id="${id}" name="${name}">${nodes.filter(n=>n[3]===id).map(n=>`<bpmn:flowNodeRef>${n[0]}</bpmn:flowNodeRef>`).join('')}</bpmn:lane>`;
xml+='</bpmn:laneSet>';
for(const [id,type,name] of nodes) xml+=`<bpmn:${type} id="${id}" name="${esc(name)}">${flows.filter(f=>f[2]===id).map(f=>`<bpmn:incoming>${f[0]}</bpmn:incoming>`).join('')}${flows.filter(f=>f[1]===id).map(f=>`<bpmn:outgoing>${f[0]}</bpmn:outgoing>`).join('')}</bpmn:${type}>`;
for(const [id,a,b,name] of flows) xml+=`<bpmn:sequenceFlow id="${id}" sourceRef="${a}" targetRef="${b}" name="${name}"/>`;
xml+='</bpmn:process><bpmndi:BPMNDiagram id="Diagram"><bpmndi:BPMNPlane id="Plane" bpmnElement="Collaboration"><bpmndi:BPMNShape id="Pool_di" bpmnElement="Pool" isHorizontal="true"><dc:Bounds x="10" y="100" width="1980" height="620"/></bpmndi:BPMNShape>';
for(const [id,,y,h] of lanes) xml+=`<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}" isHorizontal="true"><dc:Bounds x="40" y="${y}" width="1950" height="${h}"/></bpmndi:BPMNShape>`;
for(const [id,,, ,x,y,w,h] of nodes) xml+=`<bpmndi:BPMNShape id="${id}_di" bpmnElement="${id}"><dc:Bounds x="${x}" y="${y}" width="${w}" height="${h}"/></bpmndi:BPMNShape>`;
for(const [id,a,b] of flows) xml+=`<bpmndi:BPMNEdge id="${id}_di" bpmnElement="${id}">${waypoints(a,b).map(([x,y])=>`<di:waypoint x="${x}" y="${y}"/>`).join('')}</bpmndi:BPMNEdge>`;
xml+='</bpmndi:BPMNPlane></bpmndi:BPMNDiagram></bpmn:definitions>';
fs.writeFileSync(path.join(dir,'diagramas/proceso-solicitud-venta.bpmn'),xml);
let svg='<svg xmlns="http://www.w3.org/2000/svg" width="2020" height="800" viewBox="0 0 2020 800"><defs><marker id="arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0 0 L8 4 L0 8 Z"/></marker></defs><rect width="2020" height="800" fill="white"/><text x="30" y="45" font-family="Arial" font-size="25">BPMN · Solicitud y venta — proceso propuesto</text>';
for(const [,name,y,h] of lanes)svg+=`<rect x="10" y="${y}" width="1980" height="${h}" fill="none" stroke="#72897c"/><text x="20" y="${y+25}" font-family="Arial" font-size="15">${name}</text>`;
for(const [,a,b,name] of flows){const [[x1,y1],[x2,y2]]=waypoints(a,b);svg+=`<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="#617a6b" marker-end="url(#arrow)"/><text x="${(x1+x2)/2}" y="${(y1+y2)/2-8}" font-family="Arial" font-size="13">${name}</text>`;}
for(const [id,type,name,,x,y,w,h] of nodes){
  if(type.endsWith('Event'))svg+=`<circle cx="${x+w/2}" cy="${y+h/2}" r="17" fill="white" stroke="#203d2c" stroke-width="${type==='endEvent'?4:1.5}"/>`;
  else if(type==='exclusiveGateway')svg+=`<polygon points="${x+w/2},${y} ${x+w},${y+h/2} ${x+w/2},${y+h} ${x},${y+h/2}" fill="#fff5df" stroke="#203d2c"/><text x="${x+18}" y="${y+32}" font-family="Arial" font-size="22">×</text>`;
  else svg+=`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="10" fill="#e2eee9" stroke="#203d2c"/>`;
  const words=name.split(' ');const rows=[];let row='';for(const word of words){if((row+word).length>23&&row){rows.push(row.trim());row='';}row+=word+' ';}rows.push(row.trim());
  const tx=type.endsWith('Event')?x-20:type==='exclusiveGateway'?x-15:x+10;const ty=type.endsWith('Event')?y+h+20:type==='exclusiveGateway'?y-22:y+25;
  svg+=`<text x="${tx}" y="${ty}" font-family="Arial" font-size="13">${rows.map((r,i)=>`<tspan x="${tx}" dy="${i?17:0}">${esc(r)}</tspan>`).join('')}</text>`;
}
svg+='<text x="30" y="765" font-family="Arial" font-size="15">Fuente: elaboración propia. La plataforma no observa el envío real en WhatsApp. Fallos de stock e idempotencia: S04.</text></svg>';
fs.writeFileSync(path.join(dir,'figuras/proceso-solicitud-venta.svg'),svg);
fs.writeFileSync(path.join(dir,'figuras/proceso-solicitud-venta.png'),new Resvg(svg).render().asPng());
console.log('BPMN no ejecutable con coordenadas de importación y figura SVG/PNG generado.');
