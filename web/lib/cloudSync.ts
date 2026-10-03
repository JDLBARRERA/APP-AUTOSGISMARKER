import { getSubscriptionStatus } from "@/lib/subscriptionManager";

export interface CloudBackup {
  tracks: unknown;
  tournamentStats: unknown;
  keystoneCalibration: unknown;
}

export type CloudSyncResult = { ok: true } | { ok: false; reason: "plan" | "unconfigured" | "remote" };

const TRACKS_KEY = "autosgismarker-custom-tracks";
const KEYSTONE_KEY = "autosgismarker-keystone";
const HISTORY_KEY = "phygital-history";
const RECORDS_KEY = "phygital-records";
const TOURNAMENT_KEY = "autosgismarker-tournament";

const url = process.env.NEXT_PUBLIC_SUPABASE_URL;
const key = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY;

function readJson(storageKey: string) {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(storageKey);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

/** Arma el paquete local de pistas, medallero y calibración de pared. */
export function collectLocalBackup(): CloudBackup {
  return {
    tracks: readJson(TRACKS_KEY),
    tournamentStats: {
      history: readJson(HISTORY_KEY),
      records: readJson(RECORDS_KEY),
      tournament: readJson(TOURNAMENT_KEY),
    },
    keystoneCalibration: readJson(KEYSTONE_KEY),
  };
}

function assertPro() {
  const profile = getSubscriptionStatus();
  if (!profile.isPro || !profile.cloudSyncEnabled) return null;
  return profile;
}

async function remoteWrite(userId: string, data: CloudBackup): Promise<CloudSyncResult> {
  if (!url || !key) return { ok: false, reason: "unconfigured" };
  try {
    const response = await fetch(`${url}/rest/v1/pro_backups`, {
      method: "POST",
      headers: {
        apikey: key,
        Authorization: `Bearer ${key}`,
        "Content-Type": "application/json",
        Prefer: "resolution=merge-duplicates",
      },
      body: JSON.stringify({ user_id: userId, payload: data, updated_at: new Date().toISOString() }),
    });
    return response.ok ? { ok: true } : { ok: false, reason: "remote" };
  } catch {
    return { ok: false, reason: "remote" };
  }
}

async function remoteRead(userId: string): Promise<CloudBackup | CloudSyncResult> {
  if (!url || !key) return { ok: false, reason: "unconfigured" };
  try {
    const response = await fetch(`${url}/rest/v1/pro_backups?user_id=eq.${encodeURIComponent(userId)}&select=payload`, {
      headers: { apikey: key, Authorization: `Bearer ${key}` },
    });
    if (!response.ok) return { ok: false, reason: "remote" };
    const rows = (await response.json()) as { payload?: CloudBackup }[];
    if (!rows[0]?.payload) return { ok: false, reason: "remote" };
    return rows[0].payload;
  } catch {
    return { ok: false, reason: "remote" };
  }
}

function writeJson(storageKey: string, value: unknown) {
  if (typeof window === "undefined" || value == null) return;
  window.localStorage.setItem(storageKey, JSON.stringify(value));
}

/**
 * Sube pistas personalizadas, calibración Keystone y medallero.
 * El plan gratuito no puede sincronizar.
 */
export async function backupToCloud(data: CloudBackup): Promise<CloudSyncResult> {
  const profile = assertPro();
  if (!profile) return { ok: false, reason: "plan" };
  return remoteWrite(profile.userId, data);
}

/** Restaura el respaldo de la nube en este dispositivo. */
export async function restoreFromCloud(userId: string): Promise<CloudSyncResult> {
  const profile = assertPro();
  if (!profile) return { ok: false, reason: "plan" };
  const payload = await remoteRead(userId || profile.userId);
  if ("ok" in payload) return payload;
  writeJson(TRACKS_KEY, payload.tracks);
  writeJson(KEYSTONE_KEY, payload.keystoneCalibration);
  const stats = payload.tournamentStats as { history?: unknown; records?: unknown; tournament?: unknown } | null;
  if (stats && typeof stats === "object") {
    writeJson(HISTORY_KEY, stats.history);
    writeJson(RECORDS_KEY, stats.records);
    writeJson(TOURNAMENT_KEY, stats.tournament);
  }
  return { ok: true };
}
