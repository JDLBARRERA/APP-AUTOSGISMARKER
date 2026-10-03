import type { CatalogTrack, RaceFormat, TrackPoint, WaypointRule } from "@/lib/catalog";

const FORMAT_NAMES = ["CIRCUITO", "GRAN CIRCUITO", "GRAND PRIX"] as const;
const MODULE_NAMES = ["Óvalo", "Ocho", "Recta", "Manzanas", "Campeonato"] as const;

const HALF_MAT = 0.42;

type RulePlan = {
  exact: number;
  tension: number;
  pit: number;
  long: number;
  ice: number;
  lava: number;
  rain: number;
  exact2: number;
  pit2: number;
  long2: number;
  tension2: number;
  tension3: number;
};

type RawPoint = { x: number; y: number; z: number };

function wrap(value: number, length: number) {
  return ((value % length) + length) % length;
}

function planFor(module: number): RulePlan {
  switch (module) {
    case 1:
      return { exact: 0.1, tension: 0.18, pit: 0.4, long: 0.5, ice: 0.62, lava: 0.88, rain: 0.54, exact2: 0.15, pit2: 0.42, long2: 0.58, tension2: 0.36, tension3: 0.68 };
    case 2:
      return { exact: 0.06, tension: 0.76, pit: 0.88, long: 0.22, ice: 0.63, lava: 0.38, rain: 0.48, exact2: 0.12, pit2: 0.48, long2: 0.18, tension2: 0.32, tension3: 0.7 };
    case 3:
      return { exact: 0.22, tension: 0.74, pit: 0.86, long: 0.1, ice: 0.59, lava: 0.34, rain: 0.46, exact2: 0.16, pit2: 0.42, long2: 0.68, tension2: 0.28, tension3: 0.52 };
    case 4:
      return { exact: 0.05, tension: 0.56, pit: 0.63, long: 0.2, ice: 0.82, lava: 0.38, rain: 0.48, exact2: 0.12, pit2: 0.88, long2: 0.16, tension2: 0.3, tension3: 0.74 };
    default:
      return { exact: 0.16, tension: 0.34, pit: 0.5, long: 0.66, ice: 0.8, lava: 0.92, rain: 0.08, exact2: 0.28, pit2: 0.42, long2: 0.74, tension2: 0.22, tension3: 0.58 };
  }
}

function placeRules(points: TrackPoint[], format: RaceFormat, plan: RulePlan) {
  const used = points.map(() => false);
  used[0] = true;
  points[0].rule = "finish";
  place(points, used, plan.long, "long", 0, true);
  place(points, used, plan.exact, "exact", 0, false);
  place(points, used, plan.pit, "pit", 0, true);
  place(points, used, plan.rain, "rain", 0, true);
  if (format === 2) {
    place(points, used, plan.tension, "trap", 1, true);
    place(points, used, plan.tension2, "trap", 2, true);
    place(points, used, plan.tension3, "trap", 3, true);
    place(points, used, plan.exact2, "exact", 0, false);
    place(points, used, plan.pit2, "pit", 0, true);
    place(points, used, plan.long2, "long", 0, true);
    place(points, used, plan.ice, "ice", 0, true);
    place(points, used, plan.lava, "lava", 0, true);
  } else if (format === 1) {
    place(points, used, plan.tension, "trap", 2, true);
    place(points, used, plan.exact2, "exact", 0, false);
    place(points, used, plan.lava, "lava", 0, true);
  } else {
    place(points, used, plan.tension, "trap", 1, true);
    place(points, used, plan.ice, "ice", 0, true);
  }
}

function place(points: TrackPoint[], used: boolean[], fraction: number, rule: WaypointRule, value: number, avoidCenter: boolean) {
  const count = points.length;
  let ideal = Math.round(fraction * count) % count;
  if (ideal < 0) ideal += count;
  let best = -1;
  let bestScore = Number.NEGATIVE_INFINITY;
  for (let delta = 0; delta < count; delta += 1) {
    const index = (ideal + delta) % count;
    if (index === 0 || used[index]) continue;
    const position = points[index];
    const crowdedCenter = avoidCenter && position.x * position.x + position.z * position.z < 0.006;
    let score = crowdedCenter ? -1 : separation(points, used, index) - delta * 0.012;
    if (neighborIsRule(points, used, index)) score -= 0.05;
    if (score > bestScore) {
      bestScore = score;
      best = index;
    }
  }
  if (best < 0) return;
  used[best] = true;
  points[best].rule = rule;
  points[best].value = value;
}

function separation(points: TrackPoint[], used: boolean[], index: number) {
  let nearest = 2;
  const position = points[index];
  for (let i = 0; i < points.length; i += 1) {
    if (!used[i]) continue;
    const distance = Math.hypot(position.x - points[i].x, position.z - points[i].z);
    if (distance < nearest) nearest = distance;
  }
  return nearest;
}

function neighborIsRule(points: TrackPoint[], used: boolean[], index: number) {
  const previous = (index - 1 + points.length) % points.length;
  const next = (index + 1) % points.length;
  return (used[previous] && points[previous].rule !== "path") || (used[next] && points[next].rule !== "path");
}

function shape(module: number, count: number) {
  switch (module) {
    case 1:
      return figureEight(count, 0.34, 0.17);
    case 2:
      return stadium(count, 0.26, 0.15);
    case 3:
      return stadium(count, 0.16, 0.18);
    case 4:
      return stadium(count, 0.28, 0.15);
    default:
      return oval(count, 0.36, 0.22);
  }
}

function oval(count: number, radiusX: number, radiusZ: number) {
  return Array.from({ length: count }, (_, index) => {
    const t = (index / count) * Math.PI * 2;
    return { x: radiusX * Math.cos(t), y: 0.002, z: radiusZ * Math.sin(t) };
  });
}

function figureEight(count: number, radiusX: number, radiusZ: number) {
  return Array.from({ length: count }, (_, index) => {
    const t = Math.PI * 0.5 + (index / count) * Math.PI * 2;
    return { x: radiusX * Math.sin(t), y: 0.002, z: radiusZ * Math.sin(t * 2) };
  });
}

function stadium(count: number, halfLength: number, radius: number) {
  const straight = halfLength * 2;
  const arc = Math.PI * radius;
  const perimeter = (straight + arc) * 2;
  return Array.from({ length: count }, (_, index) => pointOnStadium((index / count) * perimeter, halfLength, radius, straight, arc));
}

function pointOnStadium(distance: number, halfLength: number, radius: number, straight: number, arc: number): RawPoint {
  let d = distance;
  if (d < straight) return { x: -halfLength + (halfLength * 2 * d) / straight, y: 0.002, z: -radius };
  d -= straight;
  if (d < arc) {
    const angle = -Math.PI * 0.5 + (d / arc) * Math.PI;
    return { x: halfLength + Math.cos(angle) * radius, y: 0.002, z: Math.sin(angle) * radius };
  }
  d -= arc;
  if (d < straight) return { x: halfLength - (halfLength * 2 * d) / straight, y: 0.002, z: radius };
  d -= straight;
  const back = Math.PI * 0.5 + (d / Math.max(arc, 0.0001)) * Math.PI;
  return { x: -halfLength + Math.cos(back) * radius, y: 0.002, z: Math.sin(back) * radius };
}

function dentTopSide(points: RawPoint[]) {
  let tip = 0;
  let best = Number.POSITIVE_INFINITY;
  for (let index = 0; index < points.length; index += 1) {
    if (points[index].z <= 0) continue;
    const score = Math.abs(points[index].x) - points[index].z;
    if (score < best) {
      best = score;
      tip = index;
    }
  }
  points[tip].z *= 0.42;
  const previous = (tip - 1 + points.length) % points.length;
  const next = (tip + 1) % points.length;
  points[previous].z += (points[tip].z - points[previous].z) * 0.35;
  points[next].z += (points[tip].z - points[next].z) * 0.35;
}

function applyLayout(points: RawPoint[], scale: number, degrees: number, mirror: boolean) {
  const radians = (degrees * Math.PI) / 180;
  const cos = Math.cos(radians);
  const sin = Math.sin(radians);
  for (const point of points) {
    if (mirror) point.x = -point.x;
    point.x *= scale;
    point.z *= scale;
    const x = point.x * cos + point.z * sin;
    const z = -point.x * sin + point.z * cos;
    point.x = Math.min(HALF_MAT, Math.max(-HALF_MAT, x));
    point.z = Math.min(HALF_MAT, Math.max(-HALF_MAT, z));
    point.y = 0.002;
  }
}

/** Las 10 carreras de un formato y un módulo clásico. */
export function tracksFor(format: RaceFormat, module: number) {
  return Array.from({ length: 10 }, (_, race) => buildClassicTrack(format, module, race));
}

/**
 * Réplica de las 150 pistas clásicas: óvalo, ocho, recta, manzanas y campeonato.
 */
export function buildClassicTrack(format: RaceFormat, module: number, race: number): CatalogTrack {
  const moduleIndex = wrap(module, MODULE_NAMES.length);
  const raceIndex = wrap(race, 10);
  const pointCount = format === 0 ? 10 : format === 1 ? 12 : 16;
  const formatScale = format === 0 ? 0.78 : format === 1 ? 0.9 : 1;
  const raceScale = 0.94 + (raceIndex % 5) * 0.012;
  const raw = shape(moduleIndex, pointCount);
  if (moduleIndex === 4) dentTopSide(raw);
  applyLayout(raw, formatScale * raceScale, (raceIndex % 5) * 3, raceIndex >= 5);
  const points: TrackPoint[] = raw.map((point) => ({ x: point.x, z: point.z, rule: "path" as const, value: 0 }));
  placeRules(points, format, planFor(moduleIndex));
  return {
    id: `pkg-${format}-${moduleIndex}-${raceIndex}`,
    name: `${FORMAT_NAMES[format]} · ${MODULE_NAMES[moduleIndex]} · Carrera ${raceIndex + 1}`,
    format,
    formatName: FORMAT_NAMES[format],
    moduleName: MODULE_NAMES[moduleIndex],
    moduleIndex,
    raceIndex,
    laps: format === 0 ? 1 : format === 1 ? 2 : 3,
    points,
    open: false,
  };
}
