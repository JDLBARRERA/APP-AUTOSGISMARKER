import { isToyScale, type ToyScale } from "@/lib/scaleManager";

export type { ToyScale };

export type GridCard = {
  id: number;
  name: string;
  team: string;
  sponsor: string;
  color: string;
  slot: number;
};

export type RacePhase = "SETUP" | "RACING" | "FINISHED";

export type PenaltyMode = "shot" | "turn";

export type SuperPower = "" | "turbo3x" | "shield" | "rainbomb" | "freeze";

export type LiveState = {
  format: number;
  module: number;
  race: number;
  catalog?: "classic" | "rally";
  shot: number;
  shotLimit?: number;
  notice: string;
  active?: boolean;
  phase?: RacePhase;
  grid?: GridCard[];
  raceEndsAt?: number | null;
  turnEndsAt?: number | null;
  turnBudgetMs?: number;
  penalty?: PenaltyMode;
  alert?: string;
  cueAt?: number | null;
  isFinalStretch?: boolean;
  racePaused?: boolean;
  raceRemainingMs?: number | null;
  winner?: string;
  customZones?: { id: string; kind: string; t: number; scale: number }[] | null;
  energy?: number;
  superPower?: SuperPower;
  scale?: ToyScale;
  showTestPattern?: boolean;
  sponsorText?: string;
  sponsorLogos?: string[];
  showBanners?: boolean;
  isPro?: boolean;
};

export const DEFAULT_SPONSOR_TEXT = "GRAN PREMIO TERRAZA 2026";

export const SPONSOR_BRANDS = [
  "AUTOSGISMARKER",
  "NITRO ENERGY",
  "CHALK MASTERS",
  "TURBO X",
  "10x10 LABS",
  "TURBO VAR",
  "NEON POWER",
  "Chord electronics",
] as const;

export const DEFAULT_SPONSOR_LOGOS = ["AUTOSGISMARKER", "NITRO ENERGY", "CHALK MASTERS", "TURBO X"];

export const EMPTY_SESSION: LiveState = {
  format: 0,
  module: 0,
  race: 0,
  shot: 1,
  notice: "",
};

type Bus = {
  state: LiveState;
  listeners: Set<(state: LiveState) => void>;
};

const globalBus = globalThis as typeof globalThis & { __phygital?: Bus };

function bus() {
  if (!globalBus.__phygital) {
    globalBus.__phygital = { state: { ...EMPTY_SESSION }, listeners: new Set() };
  }
  return globalBus.__phygital;
}

export function readSession() {
  return bus().state;
}

function readGrid(grid: LiveState["grid"]): GridCard[] {
  if (!Array.isArray(grid)) return [];
  return grid
    .filter((card) => card && typeof card.name === "string")
    .map((card, index) => ({
      id: Number.isFinite(card.id) ? card.id : index + 1,
      name: card.name,
      team: card.team ?? "",
      sponsor: card.sponsor ?? "",
      color: card.color || "#39FF14",
      slot: Number.isFinite(card.slot) ? card.slot : index + 1,
    }));
}

export function writeSession(next: LiveState) {
  const previous = bus().state;
  const state: LiveState = {
    format: next.format === 1 || next.format === 2 ? next.format : 0,
    module: Number.isFinite(next.module) ? next.module : 0,
    race: Number.isFinite(next.race) ? next.race : 0,
    catalog: next.catalog === "rally" ? "rally" : next.catalog === "classic" ? "classic" : previous.catalog ?? "classic",
    shotLimit: Number.isFinite(next.shotLimit) ? Math.max(1, Math.min(8, Number(next.shotLimit))) : previous.shotLimit ?? 3,
    shot: Math.min(
      Number.isFinite(next.shotLimit) ? Math.max(1, Math.min(8, Number(next.shotLimit))) : previous.shotLimit ?? 3,
      Math.max(1, next.shot || 1),
    ),
    notice: next.notice ?? "",
    active: true,
    phase: next.phase === "RACING" || next.phase === "SETUP" || next.phase === "FINISHED" ? next.phase : previous.phase,
    grid: Array.isArray(next.grid) ? readGrid(next.grid) : previous.grid,
    raceEndsAt: next.raceEndsAt === null ? null : Number.isFinite(next.raceEndsAt) ? next.raceEndsAt : previous.raceEndsAt ?? null,
    turnEndsAt: next.turnEndsAt === null ? null : Number.isFinite(next.turnEndsAt) ? next.turnEndsAt : previous.turnEndsAt ?? null,
    turnBudgetMs: Number.isFinite(next.turnBudgetMs) ? next.turnBudgetMs : previous.turnBudgetMs ?? 30000,
    penalty: next.penalty === "shot" || next.penalty === "turn" ? next.penalty : previous.penalty ?? "shot",
    alert: typeof next.alert === "string" ? next.alert : previous.alert ?? "",
    cueAt: next.cueAt === null ? null : Number.isFinite(next.cueAt) ? next.cueAt : previous.cueAt ?? null,
    isFinalStretch: typeof next.isFinalStretch === "boolean" ? next.isFinalStretch : previous.isFinalStretch ?? false,
    racePaused: typeof next.racePaused === "boolean" ? next.racePaused : previous.racePaused ?? false,
    raceRemainingMs:
      next.raceRemainingMs === null
        ? null
        : Number.isFinite(next.raceRemainingMs)
          ? next.raceRemainingMs
          : previous.raceRemainingMs ?? null,
    winner: typeof next.winner === "string" ? next.winner : previous.winner ?? "",
    customZones:
      next.customZones === null
        ? undefined
        : Array.isArray(next.customZones)
          ? next.customZones.flatMap((zone) => {
              const t = Number(zone?.t);
              const scale = Number(zone?.scale);
              if (!zone || typeof zone.kind !== "string" || !Number.isFinite(t) || !Number.isFinite(scale)) return [];
              return [{ id: String(zone.id), kind: zone.kind, t, scale }];
            })
          : previous.customZones,
    energy: Number.isFinite(next.energy) ? Math.max(0, Math.min(100, Number(next.energy))) : previous.energy ?? 0,
    superPower:
      next.superPower === "" || next.superPower === "turbo3x" || next.superPower === "shield" || next.superPower === "rainbomb" || next.superPower === "freeze"
        ? next.superPower
        : previous.superPower ?? "",
    scale: isToyScale(next.scale) ? next.scale : previous.scale ?? "1:64",
    showTestPattern: typeof next.showTestPattern === "boolean" ? next.showTestPattern : previous.showTestPattern ?? false,
    sponsorText: typeof next.sponsorText === "string" ? next.sponsorText : previous.sponsorText ?? DEFAULT_SPONSOR_TEXT,
    sponsorLogos: Array.isArray(next.sponsorLogos)
      ? next.sponsorLogos.filter((logo): logo is string => typeof logo === "string" && logo.trim().length > 0)
      : previous.sponsorLogos ?? DEFAULT_SPONSOR_LOGOS,
    showBanners: typeof next.showBanners === "boolean" ? next.showBanners : previous.showBanners ?? true,
    isPro: typeof next.isPro === "boolean" ? next.isPro : previous.isPro ?? false,
  };
  bus().state = state;
  for (const listener of bus().listeners) {
    listener(state);
  }
  return state;
}

export function listenSession(listener: (state: LiveState) => void) {
  bus().listeners.add(listener);
  return () => {
    bus().listeners.delete(listener);
  };
}
