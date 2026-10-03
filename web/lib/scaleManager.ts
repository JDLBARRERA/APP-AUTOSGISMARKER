/**
 * Ancho de carril para que quepan dos autos de juguete lado a lado.
 * El piso de referencia mide 2.5 m y ocupa el viewBox de 1.44 del proyector.
 */

export type ToyScale = "1:64" | "1:43" | "1:32" | "1:24";

export const TOY_SCALES: { id: ToyScale; label: string; laneCm: number }[] = [
  { id: "1:64", label: "1:64 · Hot Wheels / Matchbox · 10 cm", laneCm: 10 },
  { id: "1:43", label: "1:43 · Carrera Go / Compact · 15 cm", laneCm: 15 },
  { id: "1:32", label: "1:32 · Scalextric Standard · 20 cm", laneCm: 20 },
  { id: "1:24", label: "1:24 · Modelos Grandes · 26 cm", laneCm: 26 },
];

const FLOOR_METERS = 2.5;
const VIEWBOX_SPAN = 1.44;
const BASE_LANE_CM = 10;
const BASE_ROAD = 0.07;

export function isToyScale(value: unknown): value is ToyScale {
  return value === "1:64" || value === "1:43" || value === "1:32" || value === "1:24";
}

export function laneCmFor(scale: ToyScale) {
  return TOY_SCALES.find((entry) => entry.id === scale)?.laneCm ?? BASE_LANE_CM;
}

/** Grosor del asfalto en unidades del viewBox del proyector. */
export function laneStrokeFor(scale: ToyScale) {
  return (laneCmFor(scale) / 100 / FLOOR_METERS) * VIEWBOX_SPAN;
}

/** Las zonas crecen con el carril. 1:64 queda en 1. */
export function zoneFactorFor(scale: ToyScale) {
  return laneCmFor(scale) / BASE_LANE_CM;
}

export type ScaleMetrics = {
  laneStroke: number;
  centerLineStroke: number;
  bridgeStroke: number;
  zoneScaleFactor: number;
};

/** Grosor del carril, la línea, el puente y el factor de las zonas. Una clave desconocida usa 1:64. */
export function getScaleMetrics(scaleKey: string): ScaleMetrics {
  const scale = isToyScale(scaleKey) ? scaleKey : "1:64";
  const laneStroke = laneStrokeFor(scale);
  return {
    laneStroke,
    centerLineStroke: laneStroke * (0.03 / BASE_ROAD),
    bridgeStroke: laneStroke * (0.09 / BASE_ROAD),
    zoneScaleFactor: zoneFactorFor(scale),
  };
}
