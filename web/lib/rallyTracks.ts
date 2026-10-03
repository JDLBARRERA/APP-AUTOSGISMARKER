import type { CatalogTrack, TrackPoint, WaypointRule } from "@/lib/catalog";

const NAMES = [
  "Sprint Coloso A-B",
  "Zig-Zag de Neón",
  "Chicana del Relámpago",
  "Recta Quebrada",
  "Tres Horquillas",
  "Rayo de Salida",
  "Tramo del Umbral",
  "Horquilla 180",
  "Corte en Z",
  "Sprint de Terraza",
  "Puente del Riesgo",
  "Cruce en X 10x10",
  "Impacto Central",
  "Tijera de Asfalto",
  "Equis de Neón",
  "Paso Elevado",
  "X de la Terraza",
  "Puente Corto",
  "Cruce Luminoso",
  "Tache Norte",
  "El Laberinto en W",
  "Tres Puentes",
  "Lluvia en la W",
  "Horquilla Mortífera",
  "Malla Resbalosa",
  "W de Medianoche",
  "Puentes Gemelos",
  "Laberinto Neón",
  "Triple Nivel",
  "W del Abismo",
  "Megatramo Extremo",
  "Terraza Completa",
  "Zona de Desplome",
  "Todas las Trampas",
  "Checkpoint Infinito",
  "Malla Estelar",
  "Coloso Final",
  "Rally Sin Retorno",
  "Línea de Fuego",
  "Tramo 10x10",
  "Giro de Puente",
  "X Sobre el Foso",
  "Etapa del Reloj",
  "Camino Único",
  "Sobre el Cruce",
  "Recta Cruzada",
  "Neón de Punta",
  "Salida Verde",
  "Meta Roja",
  "Rally Extremo",
] as const;

const TRAPS: WaypointRule[] = ["ice", "pit", "trap", "rain", "lava"];

function dot(x: number, z: number, rule: WaypointRule = "path", role: TrackPoint["role"] = "node", layer: 1 | 2 = 1, value = 1): TrackPoint {
  return {
    x: Math.max(-0.48, Math.min(0.48, x)),
    z: Math.max(-0.4, Math.min(0.4, z)),
    rule,
    value,
    layer,
    role,
  };
}

/**
 * Un solo trazo de salida a meta. La diagonal de ida y la del puente se cruzan en X.
 * No hay ruta alterna: el auto sigue el mismo camino.
 */
function continuous(stage: number): TrackPoint[] {
  const flip = stage % 2 === 0 ? 1 : -1;
  const amp = 0.16 + (stage % 5) * 0.018;
  const slide = ((stage % 7) - 3) * 0.015;
  const trap = TRAPS[stage % TRAPS.length];
  return [
    dot(-0.46, slide * flip, "path", "start"),
    dot(-0.3, slide * flip),
    dot(-0.16, -amp * flip),
    dot(0.16, amp * flip, trap, "checkpoint"),
    dot(0.3, amp * flip),
    dot(0.3, -amp * flip),
    dot(0.16, -amp * flip, "path", "node", 2),
    dot(-0.16, amp * flip, "path", "node", 2),
    dot(-0.28, amp * 0.35 * flip),
    dot(0.08, amp * 0.55 * flip, "long"),
    dot(0.46, -slide * flip, "finish", "finish"),
  ];
}

/** Los 50 tramos abiertos de Rally Extremo. Cada uno es un camino continuo. */
export function allRallyTracks(): CatalogTrack[] {
  return Array.from({ length: NAMES.length }, (_, stage) => buildRallyTrack(stage));
}

export function buildRallyTrack(stage: number): CatalogTrack {
  const index = ((stage % NAMES.length) + NAMES.length) % NAMES.length;
  const points = continuous(index);
  return {
    id: `rally-${index}`,
    name: NAMES[index],
    format: index >= 40 ? 2 : index >= 20 ? 1 : 0,
    formatName: "RALLY",
    moduleName: "Rally Extremo",
    moduleIndex: 0,
    raceIndex: index,
    laps: 0,
    points,
    template: "MULTI_BRIDGE_CROSS",
    tier: Math.floor(index / 10) + 1,
    open: true,
    spans: [{ id: "spine", kind: "spine", points }],
    zoneScale: index >= 30 ? 2 : 1,
    clockSeconds: 120,
    superPowers: index >= 30,
  };
}
