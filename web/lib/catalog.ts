import { buildClassicTrack } from "@/lib/tracks";

export type RaceFormat = 0 | 1 | 2;

export type WaypointRule =
  | "path"
  | "finish"
  | "exact"
  | "pit"
  | "long"
  | "trap"
  | "lava"
  | "ice"
  | "rain";

export type PointRole = "start" | "finish" | "checkpoint" | "fork" | "join" | "node";

export type TrackPoint = {
  x: number;
  z: number;
  rule: WaypointRule;
  value: number;
  layer?: 1 | 2;
  role?: PointRole;
  power?: "turbo";
};

export type RallyTemplate = "POINT_TO_POINT" | "FORKED_PATHS" | "ZIG_ZAG_GRID" | "MULTI_BRIDGE_CROSS";

export type RallySpan = {
  id: string;
  kind: "spine" | "routeA" | "routeB" | "routeC";
  points: TrackPoint[];
};

export type CatalogTrack = {
  id: string;
  name: string;
  format: RaceFormat;
  formatName: string;
  moduleName: string;
  moduleIndex: number;
  raceIndex: number;
  laps: number;
  points: TrackPoint[];
  template?: RallyTemplate;
  tier?: number;
  open?: boolean;
  spans?: RallySpan[];
  zoneScale?: number;
  clockSeconds?: number;
  superPowers?: boolean;
};

export const MODULE_COUNT = 5;
export const RACES_PER_MODULE = 10;


export const FORMAT_NAMES = ["CIRCUITO", "GRAN CIRCUITO", "GRAND PRIX"] as const;

export const MODULE_NAMES = ["Óvalo", "Ocho", "Recta", "Manzanas", "Campeonato"] as const;

function wrap(value: number, length: number) {
  return ((value % length) + length) % length;
}

export function getTrack(format: RaceFormat, module: number, race: number): CatalogTrack {
  return buildClassicTrack(format, wrap(module, MODULE_COUNT), wrap(race, RACES_PER_MODULE));
}

export function tracksFor(format: RaceFormat, module: number) {
  return Array.from({ length: RACES_PER_MODULE }, (_, race) => getTrack(format, module, race));
}


export function ruleLabel(point: TrackPoint) {
  switch (point.rule) {
    case "finish":
      return "META";
    case "exact":
      return "EXACTO";
    case "pit":
      return "FOSO";
    case "long":
      return "TIRO LARGO";
    case "trap":
      return `TRAMPA ${Math.min(3, Math.max(1, point.value))}`;
    case "lava":
      return "LAVA";
    case "ice":
      return "HIELO";
    case "rain":
      return "LLUVIA";
    default:
      return "";
  }
}

export function ruleColor(rule: WaypointRule) {
  switch (rule) {
    case "finish":
      return "#ff260d";
    case "exact":
      return "#ffd91a";
    case "pit":
      return "#590505";
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
    default:
      return "#e8fbff";
  }
}

export function projectorPath(format: RaceFormat, module: number, race: number) {
  return `/proyector?formato=${format}&modulo=${module}&carrera=${race}`;
}
