import type { CatalogTrack, TrackPoint, WaypointRule } from "@/lib/catalog";

export type ZoneKind = Exclude<WaypointRule, "path"> | "turbo";

export type CustomZone = {
  id: string;
  kind: ZoneKind;
  t: number;
  scale: number;
};

const STORAGE_KEY = "autosgismarker-custom-tracks";

export const ZONE_SCALES = [1, 1.5, 2, 3] as const;

const KINDS = new Set<ZoneKind>(["finish", "exact", "pit", "long", "trap", "lava", "ice", "rain", "turbo"]);

export function zoneLabel(kind: ZoneKind) {
  switch (kind) {
    case "finish":
      return "META";
    case "exact":
      return "EXACTO";
    case "pit":
      return "FOSO";
    case "long":
      return "TIRO LARGO";
    case "trap":
      return "TRAMPA";
    case "lava":
      return "LAVA";
    case "ice":
      return "HIELO";
    case "rain":
      return "LLUVIA";
    case "turbo":
      return "TURBO";
    default:
      return "";
  }
}

export function zoneColor(kind: ZoneKind) {
  switch (kind) {
    case "finish":
      return "#ff260d";
    case "exact":
      return "#ffd91a";
    case "pit":
      return "#c46a3a";
    case "long":
      return "#1aff4d";
    case "trap":
      return "#bf26ff";
    case "lava":
      return "#ff590d";
    case "ice":
      return "#8ce6ff";
    case "rain":
      return "#1f52d9";
    case "turbo":
      return "#39ff14";
    default:
      return "#39f3ff";
  }
}

function perimeter(points: TrackPoint[], closed = true) {
  const lengths: number[] = [];
  let total = 0;
  const segments = closed ? points.length : Math.max(0, points.length - 1);
  for (let index = 0; index < segments; index += 1) {
    const current = points[index];
    const next = points[closed ? (index + 1) % points.length : index + 1];
    const length = Math.hypot(next.x - current.x, next.z - current.z);
    lengths.push(length);
    total += length;
  }
  return { lengths, total: total || 1 };
}

export function pointAt(points: TrackPoint[], t: number, closed = true) {
  const { lengths, total } = perimeter(points, closed);
  let distance = (((t % 1) + 1) % 1) * total;
  for (let index = 0; index < lengths.length; index += 1) {
    const span = lengths[index];
    const current = points[index];
    const next = points[closed ? (index + 1) % points.length : index + 1];
    if (distance <= span || index === lengths.length - 1) {
      const mix = span === 0 ? 0 : Math.max(0, Math.min(1, distance / span));
      return {
        x: current.x + (next.x - current.x) * mix,
        z: current.z + (next.z - current.z) * mix,
      };
    }
    distance -= span;
  }
  return { x: points[0]?.x ?? 0, z: points[0]?.z ?? 0 };
}

export function nearestT(points: TrackPoint[], x: number, z: number, closed = true) {
  const { lengths, total } = perimeter(points, closed);
  let best = 0;
  let bestDistance = Number.POSITIVE_INFINITY;
  let walked = 0;
  const segments = lengths.length;
  for (let index = 0; index < segments; index += 1) {
    const current = points[index];
    const next = points[closed ? (index + 1) % points.length : index + 1];
    const dx = next.x - current.x;
    const dz = next.z - current.z;
    const lengthSquared = dx * dx + dz * dz || 1;
    const mix = Math.max(0, Math.min(1, ((x - current.x) * dx + (z - current.z) * dz) / lengthSquared));
    const px = current.x + dx * mix;
    const pz = current.z + dz * mix;
    const distance = (px - x) ** 2 + (pz - z) ** 2;
    if (distance < bestDistance) {
      bestDistance = distance;
      best = (walked + lengths[index] * mix) / total;
    }
    walked += lengths[index];
  }
  return best;
}

export function zonesFromTrack(track: CatalogTrack): CustomZone[] {
  const closed = track.open !== true;
  const { lengths, total } = perimeter(track.points, closed);
  let walked = 0;
  return track.points.flatMap((point, index) => {
    const t = walked / total;
    walked += lengths[index] ?? 0;
    if (point.rule === "path") return [];
    return [{ id: `${track.id}-${index}`, kind: point.rule, t, scale: 1 }];
  });
}

export function freeSpot(zones: CustomZone[]) {
  if (zones.length === 0) return 0.25;
  const marks = zones.map((zone) => ((zone.t % 1) + 1) % 1).sort((left, right) => left - right);
  let gap = 0;
  let spot = 0;
  for (let index = 0; index < marks.length; index += 1) {
    const start = marks[index];
    const end = index === marks.length - 1 ? marks[0] + 1 : marks[index + 1];
    if (end - start > gap) {
      gap = end - start;
      spot = (start + (end - start) / 2) % 1;
    }
  }
  return spot;
}

export function readZoneList(value: unknown): CustomZone[] | undefined {
  if (!Array.isArray(value)) return undefined;
  return value.flatMap((zone) => {
    if (!zone || typeof zone !== "object") return [];
    const kind = (zone as CustomZone).kind;
    const t = Number((zone as CustomZone).t);
    const scale = Number((zone as CustomZone).scale);
    if (!KINDS.has(kind) || !Number.isFinite(t)) return [];
    const snapped = ZONE_SCALES.reduce((best, option) => (Math.abs(option - scale) < Math.abs(best - scale) ? option : best), 1);
    return [{ id: String((zone as CustomZone).id || `z-${kind}-${t}`), kind, t: ((t % 1) + 1) % 1, scale: snapped }];
  });
}

export function loadCustom(trackId: string): CustomZone[] | null {
  if (typeof window === "undefined") return null;
  try {
    const stored = JSON.parse(window.localStorage.getItem(STORAGE_KEY) || "{}") as Record<string, unknown>;
    const zones = readZoneList(stored[trackId]);
    return zones ?? null;
  } catch {
    return null;
  }
}

export function saveCustom(trackId: string, zones: CustomZone[]) {
  if (typeof window === "undefined") return;
  const stored = (() => {
    try {
      return JSON.parse(window.localStorage.getItem(STORAGE_KEY) || "{}") as Record<string, CustomZone[]>;
    } catch {
      return {};
    }
  })();
  stored[trackId] = zones;
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
}

export function nextScale(scale: number, direction: -1 | 1) {
  const index = ZONE_SCALES.findIndex((option) => option === scale);
  const current = index < 0 ? 0 : index;
  return ZONE_SCALES[Math.max(0, Math.min(ZONE_SCALES.length - 1, current + direction))];
}
