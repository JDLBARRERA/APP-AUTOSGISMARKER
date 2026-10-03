export type TrackSize = "1x1m" | "5x5m" | "10x10m";

export type SessionEvents = {
  turbos: number;
  trampas: number;
  choques: number;
  exactos: number;
};

export type GameSession = {
  id: string;
  date: string;
  trackName: string;
  trackSize: TrackSize;
  winner: string;
  players: string[];
  totalShots: number;
  shotsPerTurn: number;
  precision: number;
  events: SessionEvents;
  elapsedMs: number;
  excessByPilot?: Record<string, number>;
};

export type LeaderRow = {
  name: string;
  wins: number;
  shots: number;
  precision: number;
};

export type TrackRecord = {
  player: string;
  trackName: string;
  elapsedMs: number;
};

const HISTORY_KEY = "phygital-history";
const LIVE_KEY = "phygital-live";
const NAMES_KEY = "phygital-players";
const RECORDS_KEY = "phygital-records";

const EMPTY_EVENTS: SessionEvents = { turbos: 0, trampas: 0, choques: 0, exactos: 0 };

function canStore() {
  return typeof window !== "undefined";
}

function readJson<T>(key: string, fallback: T): T {
  if (!canStore()) return fallback;
  try {
    const raw = window.localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : fallback;
  } catch {
    return fallback;
  }
}

function writeJson(key: string, value: unknown) {
  if (!canStore()) return;
  window.localStorage.setItem(key, JSON.stringify(value));
}

export function sizeForFormat(format: number): TrackSize {
  if (format === 2) return "10x10m";
  if (format === 1) return "5x5m";
  return "1x1m";
}

export function getPlayerNames() {
  const names = readJson<string[]>(NAMES_KEY, ["Jugador 1", "Jugador 2"]);
  return names.length > 0 ? names : ["Jugador 1", "Jugador 2"];
}

export function setPlayerNames(names: string[]) {
  writeJson(
    NAMES_KEY,
    names.map((name) => name.trim()).filter(Boolean),
  );
}

export function getHistory() {
  return readJson<GameSession[]>(HISTORY_KEY, []);
}

export function getLeaderboard(): LeaderRow[] {
  const totals = new Map<string, { wins: number; shots: number; precision: number; races: number }>();
  for (const session of getHistory()) {
    const row = totals.get(session.winner) ?? { wins: 0, shots: 0, precision: 0, races: 0 };
    row.wins += 1;
    row.shots += session.totalShots;
    row.precision += session.precision;
    row.races += 1;
    totals.set(session.winner, row);
  }

  return [...totals.entries()]
    .map(([name, row]) => ({
      name,
      wins: row.wins,
      shots: row.shots,
      precision: row.races > 0 ? Math.round(row.precision / row.races) : 0,
    }))
    .sort((a, b) => b.wins - a.wins || a.shots - b.shots);
}

export function getRecords() {
  return readJson<Partial<Record<TrackSize, TrackRecord>>>(RECORDS_KEY, {});
}

export function getLiveSession() {
  return readJson<GameSession | null>(LIVE_KEY, null);
}

function blankSession(trackName: string, trackSize: TrackSize): GameSession {
  return {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    date: new Date().toISOString(),
    trackName,
    trackSize,
    winner: getPlayerNames()[0] ?? "Jugador 1",
    players: getPlayerNames(),
    totalShots: 0,
    shotsPerTurn: 3,
    precision: 100,
    events: { ...EMPTY_EVENTS },
    elapsedMs: 0,
  };
}

function withPrecision(session: GameSession): GameSession {
  const misses = session.events.choques + session.events.trampas;
  const precision = session.totalShots === 0 ? 100 : Math.max(0, Math.round(((session.totalShots - misses) / session.totalShots) * 100));
  return { ...session, precision };
}

export function ensureLive(trackName: string, trackSize: TrackSize) {
  const current = getLiveSession();
  if (current && current.trackName === trackName) return current;
  const next = blankSession(trackName, trackSize);
  writeJson(LIVE_KEY, next);
  return next;
}

/** Empieza una carrera nueva y olvida las penalizaciones de la sesión anterior. */
export function beginLive(trackName: string, trackSize: TrackSize) {
  const next = blankSession(trackName, trackSize);
  writeJson(LIVE_KEY, next);
  return next;
}

export function recordShot(trackName: string, trackSize: TrackSize) {
  const session = withPrecision({
    ...ensureLive(trackName, trackSize),
    totalShots: ensureLive(trackName, trackSize).totalShots + 1,
    elapsedMs: Date.now() - new Date(ensureLive(trackName, trackSize).date).getTime(),
  });
  const fresh = getLiveSession() ?? blankSession(trackName, trackSize);
  const next = withPrecision({
    ...fresh,
    trackName,
    trackSize,
    totalShots: fresh.totalShots + 1,
    elapsedMs: Math.max(0, Date.now() - new Date(fresh.date).getTime()),
  });
  writeJson(LIVE_KEY, next);
  return next;
}

export function addPilotExcess(name: string, ms: number, trackName: string, trackSize: TrackSize) {
  if (!name || ms <= 0) return null;
  const fresh = ensureLive(trackName, trackSize);
  const excessByPilot = { ...(fresh.excessByPilot ?? {}) };
  excessByPilot[name] = (excessByPilot[name] ?? 0) + ms;
  const players = fresh.players.includes(name) ? fresh.players : [...fresh.players, name];
  const next = { ...fresh, excessByPilot, players };
  writeJson(LIVE_KEY, next);
  return next;
}

export function recordEvent(kind: keyof SessionEvents, trackName: string, trackSize: TrackSize) {
  const fresh = ensureLive(trackName, trackSize);
  const next = withPrecision({
    ...fresh,
    events: { ...fresh.events, [kind]: fresh.events[kind] + 1 },
    elapsedMs: Math.max(0, Date.now() - new Date(fresh.date).getTime()),
  });
  writeJson(LIVE_KEY, next);
  return next;
}

export function saveSession(data: GameSession) {
  const history = getHistory();
  const next = [data, ...history.filter((session) => session.id !== data.id)];
  writeJson(HISTORY_KEY, next);
  rememberRecord(data);
  return data;
}

function rememberRecord(session: GameSession) {
  if (session.elapsedMs <= 0 || session.totalShots === 0) return;
  const records = getRecords();
  const current = records[session.trackSize];
  if (!current || session.elapsedMs < current.elapsedMs) {
    records[session.trackSize] = {
      player: session.winner,
      trackName: session.trackName,
      elapsedMs: session.elapsedMs,
    };
    writeJson(RECORDS_KEY, records);
  }
}

export function commitLive(winner?: string) {
  const live = getLiveSession();
  if (!live) return null;
  if (live.totalShots === 0 && !winner?.trim()) return null;
  const saved = saveSession({
    ...withPrecision(live),
    winner: winner?.trim() || live.winner,
    players: getPlayerNames(),
    date: new Date().toISOString(),
    elapsedMs: Math.max(live.elapsedMs, Date.now() - new Date(live.date).getTime()),
  });
  window.localStorage.removeItem(LIVE_KEY);
  return saved;
}

export function favoriteTrack() {
  const counts = new Map<string, number>();
  for (const session of getHistory()) {
    counts.set(session.trackName, (counts.get(session.trackName) ?? 0) + 1);
  }
  let name = "Sin pistas";
  let best = 0;
  for (const [track, count] of counts) {
    if (count > best) {
      best = count;
      name = track;
    }
  }
  return name;
}

export function totalTurbos() {
  return getHistory().reduce((sum, session) => sum + session.events.turbos, 0);
}

function stamp() {
  return new Date().toISOString().slice(0, 10);
}

function download(filename: string, contents: string, type: string) {
  const blob = new Blob([contents], { type });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

function sessionsForExport() {
  const history = getHistory();
  const live = getLiveSession();
  if (!live || history.some((session) => session.id === live.id)) return history;
  return [live, ...history];
}

function excessLabel(session: GameSession) {
  return Object.entries(session.excessByPilot ?? {})
    .map(([name, ms]) => `${name}:${Math.round(ms / 1000)}s`)
    .join(" | ");
}

export function exportStatsToJSON() {
  download(`autosgismarker_stats_${stamp()}.json`, JSON.stringify(sessionsForExport(), null, 2), "application/json");
}

export function exportStatsToCSV() {
  const header = ["fecha", "pista", "tamano", "ganador", "jugadores", "tiros", "tiros_por_turno", "precision", "turbos", "trampas", "choques", "exactos", "tiempo_ms", "exceso_por_piloto"];
  const lines = sessionsForExport().map((session) =>
    [
      session.date,
      session.trackName,
      session.trackSize,
      session.winner,
      session.players.join(" | "),
      session.totalShots,
      session.shotsPerTurn,
      session.precision,
      session.events.turbos,
      session.events.trampas,
      session.events.choques,
      session.events.exactos,
      session.elapsedMs,
      excessLabel(session),
    ]
      .map((value) => `"${String(value).replaceAll('"', '""')}"`)
      .join(","),
  );
  download(`autosgismarker_stats_${stamp()}.csv`, [header.join(","), ...lines].join("\n"), "text/csv");
}

export function importStats(jsonData: string) {
  const parsed = JSON.parse(jsonData) as GameSession[];
  if (!Array.isArray(parsed)) {
    throw new Error("El archivo no trae una lista de partidas.");
  }
  const incoming = parsed.filter((session) => session && typeof session.id === "string" && typeof session.trackName === "string");
  const merged = [...incoming, ...getHistory()];
  const unique = new Map<string, GameSession>();
  for (const session of merged) unique.set(session.id, session);
  const history = [...unique.values()].sort((a, b) => b.date.localeCompare(a.date));
  writeJson(HISTORY_KEY, history);
  for (const session of history) rememberRecord(session);
  return history;
}
