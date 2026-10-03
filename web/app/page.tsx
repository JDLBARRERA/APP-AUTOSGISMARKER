"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { ChevronDown, ChevronUp, Dices, Flag, Pencil, Plus, Settings, Shield, Trash2, Zap } from "lucide-react";
import { HowToPlayModal } from "@/components/HowToPlayModal";
import { Logo } from "@/components/Logo";
import { RosterCatalog, type TeamEntry } from "@/components/RosterCatalog";
import { TrackEditorModal } from "@/components/TrackEditorModal";
import { ProjectedTrack } from "@/components/ProjectedTrack";
import { FORMAT_NAMES, MODULE_NAMES, type RaceFormat } from "@/lib/catalog";
import { allRallyTracks } from "@/lib/rallyTracks";
import { tracksFor } from "@/lib/tracks";
import { audioManager, playCrash, playFinalStretchSound, playShotCountdown, playTrap, playTurnStartSound } from "@/lib/audioManager";
import { formatClock, urgency } from "@/lib/raceClock";
import { TOY_SCALES, type ToyScale } from "@/lib/scaleManager";
import { AdminAuthModal } from "@/components/AdminAuthModal";
import { SubscriptionModal } from "@/components/SubscriptionModal";
import { catalogLimit, classicSlot, clearMedalBoard, isAdminAuthenticated, loginWithPin, logoutAdmin, updateAdminPin } from "@/lib/adminManager";
import { resetKeystone } from "@/lib/keystone";
import { DEFAULT_SPONSOR_LOGOS, DEFAULT_SPONSOR_TEXT, SPONSOR_BRANDS, type LiveState, type PenaltyMode, type RacePhase, type SuperPower } from "@/lib/sessionBus";
import { WEB_TEST_AD_MESSAGE, webTestAdVisible } from "@/lib/admobManager";
import { getSubscriptionStatus, type UserProfile } from "@/lib/subscriptionManager";
import { addPilotExcess, beginLive, commitLive, getLiveSession, recordShot, setPlayerNames, sizeForFormat } from "@/lib/statsManager";
import { loadCustom, saveCustom, type CustomZone } from "@/lib/trackEditor";
import { publishSession, useLiveLink } from "@/lib/useLiveLink";

interface Pilot {
  id: number;
  name: string;
  team: string;
  sponsor: string;
  color: string;
  active: boolean;
}

const DEFAULT_TEAMS: TeamEntry[] = [
  { name: "Neon Cyber GP", color: "#39FF14" },
  { name: "Viper Engineering", color: "#00F3FF" },
  { name: "CyberGis Motorsport", color: "#FF007A" },
  { name: "Corvey Corv", color: "#FFD91A" },
];

const DEFAULT_SPONSORS = ["10x10 LABS", "TURBO VAR", "NEON POWER", "Chord electronics"];

const TEAM_PALETTE = ["#39FF14", "#00F3FF", "#FF007A", "#FFD91A", "#BF26FF", "#FF590D", "#8CE6FF", "#1AFF4D"];

const ROSTER_KEY = "autosgismarker-roster-catalog";

const STARTER_PILOTS: Pilot[] = [
  { id: 1, name: "Diego Speed", team: "Neon Cyber GP", sponsor: "10x10 LABS", color: "#39FF14", active: true },
  { id: 2, name: "Kathy Dee", team: "Viper Engineering", sponsor: "TURBO VAR", color: "#00F3FF", active: true },
  { id: 3, name: 'Mateo "Rayos"', team: "CyberGis Motorsport", sponsor: "NEON POWER", color: "#FF007A", active: true },
  { id: 4, name: "William Force", team: "Corvey Corv", sponsor: "Chord electronics", color: "#FFD91A", active: true },
];

function arrange(list: Pilot[]) {
  return [...list.filter((pilot) => pilot.active), ...list.filter((pilot) => !pilot.active)];
}

const FINAL_SHOT_SECONDS = 60;

function pickWinner(pilots: { name: string }[]) {
  const excess = getLiveSession()?.excessByPilot ?? {};
  return [...pilots].sort((left, right) => (excess[left.name] ?? 0) - (excess[right.name] ?? 0))[0]?.name ?? pilots[0]?.name ?? "";
}

export default function Home() {
  const [catalog, setCatalog] = useState<"classic" | "rally">("classic");
  const [format, setFormat] = useState<RaceFormat>(0);
  const [moduleIndex, setModuleIndex] = useState(0);
  const [raceIndex, setRaceIndex] = useState(0);
  const [rallyIndex, setRallyIndex] = useState(0);
  const [projectedName, setProjectedName] = useState<string | null>(null);
  const [shot, setShot] = useState(1);
  const [log, setLog] = useState<string[]>([]);
  const [pilots, setPilots] = useState<Pilot[]>(STARTER_PILOTS);
  const [turn, setTurn] = useState(0);
  const [raceStatus, setRaceStatus] = useState<RacePhase>("SETUP");
  const [editingPilotId, setEditingPilotId] = useState<number | null>(null);
  const [formName, setFormName] = useState("");
  const [formTeam, setFormTeam] = useState("");
  const [formSponsor, setFormSponsor] = useState("");
  const [teams, setTeams] = useState<TeamEntry[]>(DEFAULT_TEAMS);
  const [sponsors, setSponsors] = useState<string[]>(DEFAULT_SPONSORS);
  const [rosterReady, setRosterReady] = useState(false);
  const [raceMinutes, setRaceMinutes] = useState(10);
  const [turnSeconds, setTurnSeconds] = useState(120);
  const [penalty, setPenalty] = useState<PenaltyMode>("turn");
  const [toyScale, setToyScale] = useState<ToyScale>("1:64");
  const [showTestPattern, setShowTestPattern] = useState(false);
  const [sponsorText, setSponsorText] = useState(DEFAULT_SPONSOR_TEXT);
  const [sponsorLogos, setSponsorLogos] = useState<string[]>(DEFAULT_SPONSOR_LOGOS);
  const [profile, setProfile] = useState<UserProfile>({ isPro: false, planType: "FREE", userId: "", email: "", cloudSyncEnabled: false });
  const [proOpen, setProOpen] = useState(false);
  const [salesAdmin, setSalesAdmin] = useState(false);
  const [authOpen, setAuthOpen] = useState(false);
  const [adminOpen, setAdminOpen] = useState(false);
  const [adminAuthed, setAdminAuthed] = useState(false);
  const [adminIntent, setAdminIntent] = useState<"panel" | "unlock" | "focus">("panel");
  const [oldPin, setOldPin] = useState("");
  const [newPin, setNewPin] = useState("");
  const [adminNotice, setAdminNotice] = useState("");
  const [profileReady, setProfileReady] = useState(false);
  const [raceEndsAt, setRaceEndsAt] = useState<number | null>(null);
  const [turnEndsAt, setTurnEndsAt] = useState<number | null>(null);
  const [now, setNow] = useState(() => Date.now());
  const [alert, setAlert] = useState("");
  const [cueAt, setCueAt] = useState<number | null>(null);
  const [isFinalStretch, setIsFinalStretch] = useState(false);
  const [shotLive, setShotLive] = useState(false);
  const [raceRemainingMs, setRaceRemainingMs] = useState<number | null>(null);
  const [windowMs, setWindowMs] = useState(120_000);
  const [finalShots, setFinalShots] = useState<Record<number, number>>({});
  const [winnerName, setWinnerName] = useState("");
  const [howToOpen, setHowToOpen] = useState(false);
  const [raceConfigOpen, setRaceConfigOpen] = useState(false);
  const [projectorConfigOpen, setProjectorConfigOpen] = useState(false);
  const [soundOn, setSoundOn] = useState(true);
  const [editorTrack, setEditorTrack] = useState<ReturnType<typeof tracksFor>[number] | null>(null);
  const [customZones, setCustomZones] = useState<CustomZone[] | null>(null);
  const [energy, setEnergy] = useState(0);
  const [superPower, setSuperPower] = useState<SuperPower>("");
  const energyRef = useRef(0);
  const superRef = useRef<SuperPower>("");
  const [shotBudget, setShotBudget] = useState(3);
  const shotBudgetRef = useRef(3);
  const nextQueueRef = useRef<Record<number, { base: number; extra: number }>>({});
  const skipFirstRef = useRef<Record<number, boolean>>({});
  const handNextRef = useRef<Record<number, boolean>>({});
  const penaltyLock = useRef(false);
  const alertToken = useRef(0);
  const lastPulse = useRef<number | null>(null);
  const connected = useLiveLink();

  const classicRaces = useMemo(() => tracksFor(format, moduleIndex), [format, moduleIndex]);
  const rallyRaces = useMemo(() => allRallyTracks(), []);
  const races = catalog === "rally" ? rallyRaces : classicRaces;
  const selected = (catalog === "rally" ? rallyRaces[rallyIndex] : classicRaces[raceIndex]) ?? races[0];
  const grid = useMemo(() => pilots.filter((pilot) => pilot.active), [pilots]);
  const currentPilot = raceStatus === "RACING" && grid.length > 0 ? grid[turn % grid.length] : null;
  const nextPilot = grid.length > 0 ? grid[(turn + 1) % grid.length] : null;
  const turnBudget = windowMs;
  const turnLeft = shotLive && turnEndsAt ? Math.max(0, turnEndsAt - now) : windowMs;
  const turnLevel = shotLive ? urgency(turnLeft, turnBudget) : "ok";
  const raceClockText =
    shotLive && raceEndsAt !== null
      ? formatClock(Math.max(0, raceEndsAt - now))
      : raceRemainingMs === null
        ? "SIN LÍMITE"
        : formatClock(raceRemainingMs);
  const turnBar = turnLevel === "danger" ? "bg-red-500 animate-pulse" : turnLevel === "warn" ? "bg-yellow-300" : "bg-lime-400";

  function livePayload(
    nextShot: number,
    notice: string,
    phase: RacePhase,
    clock?: {
      raceEndsAt?: number | null;
      turnEndsAt?: number | null;
      alert?: string;
      cueAt?: number | null;
      racePaused?: boolean;
      raceRemainingMs?: number | null;
      isFinalStretch?: boolean;
      winner?: string;
      turnBudgetMs?: number;
    },
  ): LiveState {
    const stretch = clock?.isFinalStretch ?? isFinalStretch;
    return {
      format: selected.format,
      module: selected.moduleIndex,
      race: selected.raceIndex,
      shot: nextShot,
      notice,
      phase,
      grid: grid.map((pilot, index) => ({
        id: pilot.id,
        name: pilot.name,
        team: pilot.team,
        sponsor: pilot.sponsor,
        color: pilot.color,
        slot: index + 1,
      })),
      raceEndsAt: clock && "raceEndsAt" in clock ? clock.raceEndsAt ?? null : raceEndsAt,
      turnEndsAt: clock && "turnEndsAt" in clock ? clock.turnEndsAt ?? null : turnEndsAt,
      turnBudgetMs: clock?.turnBudgetMs ?? (phase === "RACING" ? windowMs : turnSeconds * 1000),
      penalty,
      alert: clock?.alert ?? alert,
      cueAt: clock && "cueAt" in clock ? clock.cueAt ?? null : cueAt,
      isFinalStretch: stretch,
      racePaused: clock && "racePaused" in clock ? Boolean(clock.racePaused) : phase === "RACING" && !shotLive && !stretch,
      raceRemainingMs: clock && "raceRemainingMs" in clock ? clock.raceRemainingMs ?? null : raceRemainingMs,
      winner: clock?.winner ?? winnerName,
      customZones,
      energy: energyRef.current,
      superPower: superRef.current,
      shotLimit: shotBudgetRef.current,
      catalog,
      scale: toyScale,
      showTestPattern,
      sponsorText,
      sponsorLogos,
      showBanners: !profile.isPro,
      isPro: profile.isPro,
    };
  }

  useEffect(() => {
    setCustomZones(loadCustom(selected.id));
  }, [selected.id]);

  useEffect(() => {
    setSoundOn(!audioManager.isMuted());
    const unlock = () => audioManager.init();
    window.addEventListener("pointerdown", unlock);
    return () => window.removeEventListener("pointerdown", unlock);
  }, []);

  useEffect(() => {
    if (raceStatus !== "RACING") {
      audioManager.stopPulse();
      return;
    }
    audioManager.startPulse();
    const raceLeft = shotLive && raceEndsAt !== null ? Math.max(0, raceEndsAt - now) : raceRemainingMs;
    const shotLeft = shotLive && turnEndsAt !== null ? Math.max(0, turnEndsAt - now) : windowMs;
    audioManager.setTension((raceLeft !== null && raceLeft <= 30_000) || (shotLive && shotLeft <= 30_000));
  }, [raceStatus, now, raceEndsAt, raceRemainingMs, shotLive, turnEndsAt, windowMs]);

  useEffect(() => {
    const raw = window.localStorage.getItem(ROSTER_KEY);
    if (raw) {
      try {
        const saved = JSON.parse(raw) as { teams?: TeamEntry[]; sponsors?: string[] };
        const nextTeams = saved.teams?.filter((team) => team.name?.trim() && team.color) ?? [];
        const nextSponsors = saved.sponsors?.map((sponsor) => sponsor.trim()).filter(Boolean) ?? [];
        for (const team of DEFAULT_TEAMS) {
          if (!nextTeams.some((entry) => entry.name.toLowerCase() === team.name.toLowerCase())) nextTeams.push(team);
        }
        for (const sponsor of DEFAULT_SPONSORS) {
          if (!nextSponsors.some((entry) => entry.toLowerCase() === sponsor.toLowerCase())) nextSponsors.push(sponsor);
        }
        if (nextTeams.length > 0) setTeams(nextTeams);
        if (nextSponsors.length > 0) setSponsors(nextSponsors);
      } catch {
        window.localStorage.removeItem(ROSTER_KEY);
      }
    }
    setRosterReady(true);
  }, []);

  useEffect(() => {
    if (!rosterReady) return;
    window.localStorage.setItem(ROSTER_KEY, JSON.stringify({ teams, sponsors }));
  }, [teams, sponsors, rosterReady]);

  useEffect(() => {
    if (raceStatus !== "SETUP") return;
    void publishSession({
      ...livePayload(1, "PARRILLA DE SALIDA EN PREPARACIÓN", "SETUP"),
      customZones: loadCustom(selected.id),
    });
  }, [raceStatus, grid, selected, raceMinutes, turnSeconds, penalty, customZones, toyScale, showTestPattern, sponsorText, sponsorLogos, profile.isPro]);

  useEffect(() => {
    setProfile(getSubscriptionStatus());
    setAdminAuthed(isAdminAuthenticated());
    setProfileReady(true);
  }, []);

  function finishAdmin(intent: "panel" | "unlock" | "focus") {
    setAdminAuthed(true);
    setAuthOpen(false);
    setProOpen(false);
    setSalesAdmin(false);
    setProfile(getSubscriptionStatus());
    if (intent === "focus") setShowTestPattern(true);
    setAdminOpen(true);
    setAdminNotice("");
  }

  function requestAdmin(intent: "panel" | "unlock" | "focus") {
    setAdminIntent(intent);
    if (isAdminAuthenticated()) {
      finishAdmin(intent);
      return;
    }
    setAuthOpen(true);
  }

  useEffect(() => {
    if (!profileReady) return;
    void publishSession(livePayload(shot, profile.isPro ? "CUENTA PRO" : "PLAN GRATIS", raceStatus));
  }, [profile.isPro, profileReady]);

  const trackCap = catalogLimit(profile.isPro);

  useEffect(() => {
    if (raceStatus !== "SETUP") return;
    if (rallyIndex >= trackCap) setRallyIndex(trackCap - 1);
  }, [trackCap, rallyIndex, raceStatus]);

  useEffect(() => {
    if (raceStatus !== "SETUP" || catalog !== "classic") return;
    const first = classicSlot(format, moduleIndex, 0);
    if (first >= trackCap) return;
    if (classicSlot(format, moduleIndex, raceIndex) < trackCap) return;
    setRaceIndex(trackCap - first - 1);
  }, [trackCap, format, moduleIndex, raceIndex, raceStatus, catalog]);

  const runtime = useRef({
    shot,
    turn,
    penalty,
    windowMs,
    raceEndsAt,
    isFinalStretch,
    shotBudget,
    finalShots,
    pilotId: currentPilot?.id ?? 0,
    pilotName: currentPilot?.name ?? grid[0]?.name ?? "",
    pilots: grid.map((pilot) => ({ id: pilot.id, name: pilot.name })),
    gridCards: grid.map((pilot, index) => ({
      id: pilot.id,
      name: pilot.name,
      team: pilot.team,
      sponsor: pilot.sponsor,
      color: pilot.color,
      slot: index + 1,
    })),
    format: selected.format,
    module: selected.moduleIndex,
    race: selected.raceIndex,
    trackName: selected.name,
    trackSize: sizeForFormat(selected.format),
  });
  runtime.current = {
    shot,
    turn,
    penalty,
    windowMs,
    raceEndsAt,
    isFinalStretch,
    shotBudget,
    finalShots,
    pilotId: currentPilot?.id ?? 0,
    pilotName: currentPilot?.name ?? grid[turn % Math.max(grid.length, 1)]?.name ?? "",
    pilots: grid.map((pilot) => ({ id: pilot.id, name: pilot.name })),
    gridCards: grid.map((pilot, index) => ({
      id: pilot.id,
      name: pilot.name,
      team: pilot.team,
      sponsor: pilot.sponsor,
      color: pilot.color,
      slot: index + 1,
    })),
    format: selected.format,
    module: selected.moduleIndex,
    race: selected.raceIndex,
    trackName: selected.name,
    trackSize: sizeForFormat(selected.format),
  };
  const turnEndsAtRef = useRef(turnEndsAt);
  turnEndsAtRef.current = turnEndsAt;

  useEffect(() => {
    if (raceStatus !== "RACING") return;
    const timer = window.setInterval(() => {
      const time = Date.now();
      setNow(time);
      const deadline = turnEndsAtRef.current;
      if (deadline !== null && time < deadline) {
        const left = Math.ceil((deadline - time) / 1000);
        if (left <= 10 && left > 0 && lastPulse.current !== left) {
          lastPulse.current = left;
          playShotCountdown();
        }
        if (left > 10) lastPulse.current = null;
      }
      if (penaltyLock.current || deadline === null || time < deadline) return;
      const snap = runtime.current;
      if (snap.pilots.length === 0) return;
      penaltyLock.current = true;
      const token = Date.now();
      alertToken.current = token;
      const budget = snap.isFinalStretch ? FINAL_SHOT_SECONDS * 1000 : snap.windowMs;
      const message = "TIEMPO AGOTADO - PENALIZACIÓN";
      setAlert(message);
      addPilotExcess(snap.pilotName, budget, snap.trackName, snap.trackSize);
      lastPulse.current = null;
      const base = {
        format: snap.format,
        module: snap.module,
        race: snap.race,
        grid: snap.gridCards,
        penalty: snap.penalty,
      };
      if (snap.isFinalStretch) {
        playTrap();
        const shots = { ...snap.finalShots, [snap.pilotId]: 0 };
        setFinalShots(shots);
        const pending = snap.pilots.some((pilot) => (shots[pilot.id] ?? 0) > 0);
        if (!pending) {
          const winner = pickWinner(snap.pilots);
          const notice = `${message} · ${snap.pilotName} · GANADOR: ${winner}`;
          turnEndsAtRef.current = null;
          setWinnerName(winner);
          setRaceStatus("FINISHED");
          setShotLive(false);
          setTurnEndsAt(null);
          setRaceEndsAt(null);
          setCueAt(null);
          commitLive(winner);
          setLog((current) => [notice, ...current].slice(0, 4));
          void publishSession({
            ...base,
            shot: 1,
            notice,
            phase: "FINISHED",
            raceEndsAt: null,
            turnEndsAt: null,
            turnBudgetMs: budget,
            alert: message,
            cueAt: null,
            isFinalStretch: true,
            racePaused: true,
            raceRemainingMs: snap.raceEndsAt === null ? null : Math.max(0, snap.raceEndsAt - time),
            winner,
          });
          return;
        }
        let index = (snap.turn + 1) % snap.pilots.length;
        for (let step = 0; step < snap.pilots.length; step += 1) {
          if ((shots[snap.pilots[index].id] ?? 0) > 0) break;
          index = (index + 1) % snap.pilots.length;
        }
        const incoming = snap.pilots[index]?.name ?? snap.pilotName;
        const endsAt = time + FINAL_SHOT_SECONDS * 1000;
        const notice = `${message} · ${snap.pilotName} · En pista: ${incoming}`;
        turnEndsAtRef.current = endsAt;
        setTurn(index);
        setShot(1);
        setShotLive(true);
        setWindowMs(FINAL_SHOT_SECONDS * 1000);
        setTurnEndsAt(endsAt);
        setCueAt(time);
        window.setTimeout(() => playTurnStartSound(), 420);
        setLog((current) => [notice, ...current].slice(0, 4));
        void publishSession({
          ...base,
          shot: 1,
          notice,
          phase: "RACING",
          raceEndsAt: snap.raceEndsAt,
          turnEndsAt: endsAt,
          turnBudgetMs: FINAL_SHOT_SECONDS * 1000,
          alert: message,
          cueAt: time,
          isFinalStretch: true,
          racePaused: false,
          winner: "",
        });
        window.setTimeout(() => {
          if (alertToken.current !== token) return;
          setAlert("");
          penaltyLock.current = false;
          void publishSession({
            ...base,
            shot: 1,
            notice,
            phase: "RACING",
            raceEndsAt: snap.raceEndsAt,
            turnEndsAt: endsAt,
            turnBudgetMs: FINAL_SHOT_SECONDS * 1000,
            alert: "",
            cueAt: time,
            isFinalStretch: true,
            racePaused: false,
            winner: "",
          });
        }, 2500);
        return;
      }
      const passTurn = snap.penalty === "turn" || snap.shot >= snap.shotBudget;
      if (passTurn) playTrap();
      else playCrash();
      let nextIndex = passTurn ? (snap.turn + 1) % snap.pilots.length : snap.turn;
      let nextShot = passTurn ? 1 : snap.shot + 1;
      let nextLimit = snap.shotBudget;
      let handNote = "";
      if (passTurn) {
        for (let step = 0; step < snap.pilots.length; step += 1) {
          const pilot = snap.pilots[nextIndex];
          const queued = nextQueueRef.current[pilot.id] ?? { base: 3, extra: 0 };
          delete nextQueueRef.current[pilot.id];
          const skip = Boolean(skipFirstRef.current[pilot.id]);
          delete skipFirstRef.current[pilot.id];
          const hand = Boolean(handNextRef.current[pilot.id]);
          delete handNextRef.current[pilot.id];
          const limit = queued.base + queued.extra;
          const start = skip ? 2 : 1;
          if (start <= limit) {
            nextShot = start;
            nextLimit = limit;
            if (hand) handNote = ` · MANO CAMBIADA: ${pilot.name}`;
            break;
          }
          nextIndex = (nextIndex + 1) % snap.pilots.length;
        }
      }
      shotBudgetRef.current = nextLimit;
      setShotBudget(nextLimit);
      const incoming = snap.pilots[nextIndex]?.name ?? snap.pilotName;
      const remaining = snap.raceEndsAt === null ? null : Math.max(0, snap.raceEndsAt - time);
      const notice = passTurn
        ? `${message} · ${snap.pilotName} · En pista: ${incoming}${handNote}`
        : `${message} · ${snap.pilotName} pierde el tiro ${snap.shot}`;
      turnEndsAtRef.current = null;
      setTurn(nextIndex);
      setShot(nextShot);
      setShotLive(false);
      setTurnEndsAt(null);
      setRaceEndsAt(null);
      setRaceRemainingMs(remaining);
      setCueAt(null);
      setLog((current) => [notice, ...current].slice(0, 4));
      void publishSession({
        ...base,
        shot: nextShot,
        shotLimit: nextLimit,
        notice,
        phase: "RACING",
        raceEndsAt: null,
        turnEndsAt: null,
        turnBudgetMs: budget,
        alert: message,
        cueAt: null,
        isFinalStretch: false,
        racePaused: true,
        raceRemainingMs: remaining,
        winner: "",
      });
      window.setTimeout(() => {
        if (alertToken.current !== token) return;
        setAlert("");
        penaltyLock.current = false;
        void publishSession({
          ...base,
          shot: nextShot,
          shotLimit: nextLimit,
          notice,
          phase: "RACING",
          raceEndsAt: null,
          turnEndsAt: null,
          turnBudgetMs: budget,
          alert: "",
          cueAt: null,
          isFinalStretch: false,
          racePaused: true,
          raceRemainingMs: remaining,
          winner: "",
        });
      }, 2500);
    }, 200);
    return () => window.clearInterval(timer);
  }, [raceStatus]);

  function pushLog(line: string) {
    setLog((current) => [line, ...current].slice(0, 4));
  }

  function projectTrack() {
    setProjectedName(selected.name);
    const notice = raceStatus === "SETUP" ? "PARRILLA DE SALIDA EN PREPARACIÓN" : selected.name;
    pushLog(`Proyectando ${selected.name}`);
    void publishSession(livePayload(raceStatus === "SETUP" ? 1 : shot, notice, raceStatus));
    window.open("/proyector", "phygital-proyector");
  }

  function backToSetup() {
    setRaceStatus("SETUP");
    setIsFinalStretch(false);
    setShotLive(false);
    setTurnEndsAt(null);
    setRaceEndsAt(null);
    setRaceRemainingMs(null);
    setFinalShots({});
    setWinnerName("");
    setAlert("");
    setCueAt(null);
    energyRef.current = 0;
    superRef.current = "";
    setEnergy(0);
    setSuperPower("");
    resetShotRules();
    turnEndsAtRef.current = null;
    penaltyLock.current = false;
  }

  function parkTurn(nextIndex: number, nextShot: number, notice: string, zoneAlert = "") {
    const time = Date.now();
    const remaining = raceEndsAt === null ? raceRemainingMs : Math.max(0, raceEndsAt - time);
    turnEndsAtRef.current = null;
    setTurn(nextIndex);
    setShot(nextShot);
    setShotLive(false);
    setTurnEndsAt(null);
    setRaceEndsAt(null);
    setRaceRemainingMs(remaining);
    setCueAt(null);
    setAlert(zoneAlert);
    penaltyLock.current = false;
    alertToken.current = time;
    lastPulse.current = null;
    pushLog(notice);
    void publishSession(
      livePayload(nextShot, notice, "RACING", {
        raceEndsAt: null,
        turnEndsAt: null,
        alert: zoneAlert,
        cueAt: null,
        racePaused: true,
        raceRemainingMs: remaining,
      }),
    );
  }

  function rememberBudget(value: number) {
    shotBudgetRef.current = Math.max(1, Math.min(8, value));
    setShotBudget(shotBudgetRef.current);
  }

  function resetShotRules() {
    nextQueueRef.current = {};
    skipFirstRef.current = {};
    handNextRef.current = {};
    rememberBudget(3);
  }

  function pullQueue(pilotId: number) {
    const queued = nextQueueRef.current[pilotId] ?? { base: 3, extra: 0 };
    delete nextQueueRef.current[pilotId];
    const skip = Boolean(skipFirstRef.current[pilotId]);
    delete skipFirstRef.current[pilotId];
    const hand = Boolean(handNextRef.current[pilotId]);
    delete handNextRef.current[pilotId];
    const budget = queued.base + queued.extra;
    const start = skip ? 2 : 1;
    return { budget, start, hand, open: start <= budget };
  }

  function handoff(nextIndex: number, notice: string) {
    if (grid.length === 0) return;
    let index = ((nextIndex % grid.length) + grid.length) % grid.length;
    for (let step = 0; step < grid.length; step += 1) {
      const pilot = grid[index];
      const queued = pullQueue(pilot.id);
      if (queued.open) {
        rememberBudget(queued.budget);
        const hand = queued.hand ? `🖐️ MANO CAMBIADA: ${pilot.name} tira con la mano con la que no escribe` : "";
        parkTurn(index, queued.start, hand ? `${notice} · ${hand}` : notice, hand);
        return;
      }
      index = (index + 1) % grid.length;
    }
    rememberBudget(3);
    parkTurn(nextIndex % grid.length, 1, notice);
  }

  function armShot(seconds: number, nextIndex: number, nextShot: number, notice: string, stretch = isFinalStretch) {
    const time = Date.now();
    const remaining = shotLive && raceEndsAt !== null ? Math.max(0, raceEndsAt - time) : raceRemainingMs;
    const nextRaceEnd = remaining === null ? null : time + remaining;
    const endsAt = time + seconds * 1000;
    turnEndsAtRef.current = endsAt;
    setTurn(nextIndex);
    setShot(nextShot);
    setShotLive(true);
    setWindowMs(seconds * 1000);
    setTurnEndsAt(endsAt);
    setRaceEndsAt(nextRaceEnd);
    setRaceRemainingMs(remaining);
    setCueAt(time);
    setAlert("");
    penaltyLock.current = false;
    alertToken.current = time;
    lastPulse.current = null;
    playTurnStartSound();
    pushLog(notice);
    void publishSession(
      livePayload(nextShot, notice, "RACING", {
        raceEndsAt: nextRaceEnd,
        turnEndsAt: endsAt,
        alert: "",
        cueAt: time,
        racePaused: false,
        raceRemainingMs: remaining,
        isFinalStretch: stretch,
        turnBudgetMs: seconds * 1000,
        winner: "",
      }),
    );
  }

  function nextFinalIndex(shots: Record<number, number>, from: number) {
    let index = from % grid.length;
    for (let step = 0; step < grid.length; step += 1) {
      if ((shots[grid[index].id] ?? 0) > 0) return index;
      index = (index + 1) % grid.length;
    }
    return -1;
  }

  function finishWith(notice: string) {
    const winner = pickWinner(grid);
    turnEndsAtRef.current = null;
    setWinnerName(winner);
    setRaceStatus("FINISHED");
    setShotLive(false);
    setTurnEndsAt(null);
    setRaceEndsAt(null);
    setCueAt(null);
    setAlert("");
    penaltyLock.current = true;
    commitLive(winner);
    pushLog(`${notice} Ganador: ${winner}`);
    void publishSession(
      livePayload(1, `GANADOR: ${winner}`, "FINISHED", {
        raceEndsAt: null,
        turnEndsAt: null,
        alert: "",
        cueAt: null,
        racePaused: true,
        isFinalStretch: true,
        winner,
      }),
    );
  }

  function confirmGrid() {
    if (grid.length === 0) return;
    const startedAt = Date.now();
    const remaining = raceMinutes > 0 ? raceMinutes * 60_000 : null;
    const budget = turnSeconds * 1000;
    const first = grid[0];
    const notice = `EN PISTA: ${first.name} · esperando posición`;
    turnEndsAtRef.current = null;
    setRaceEndsAt(null);
    setRaceRemainingMs(remaining);
    setTurnEndsAt(null);
    setWindowMs(budget);
    setShotLive(false);
    setIsFinalStretch(false);
    setFinalShots({});
    setWinnerName("");
    setCueAt(null);
    setTurn(0);
    setShot(1);
    setRaceStatus("RACING");
    audioManager.init();
    audioManager.playCountdown(true);
    setAlert("");
    penaltyLock.current = false;
    lastPulse.current = null;
    setNow(startedAt);
    energyRef.current = 0;
    superRef.current = "";
    setEnergy(0);
    setSuperPower("");
    resetShotRules();
    setPlayerNames(grid.map((pilot) => pilot.name));
    beginLive(selected.name, sizeForFormat(selected.format));
    pushLog(notice);
    void publishSession(
      livePayload(1, notice, "RACING", {
        raceEndsAt: null,
        turnEndsAt: null,
        alert: "",
        cueAt: null,
        racePaused: true,
        raceRemainingMs: remaining,
        isFinalStretch: false,
        winner: "",
        turnBudgetMs: budget,
      }),
    );
  }

  function togglePilot(id: number) {
    setPilots((current) => {
      const pilot = current.find((entry) => entry.id === id);
      if (!pilot) return current;
      return arrange(current.map((entry) => (entry.id === id ? { ...entry, active: !entry.active } : entry)));
    });
  }

  function movePilot(id: number, direction: -1 | 1) {
    setPilots((current) => {
      const active = current.filter((pilot) => pilot.active);
      const index = active.findIndex((pilot) => pilot.id === id);
      const target = index + direction;
      if (index < 0 || target < 0 || target >= active.length) return current;
      const next = [...active];
      const [moved] = next.splice(index, 1);
      next.splice(target, 0, moved);
      return [...next, ...current.filter((pilot) => !pilot.active)];
    });
  }

  function shuffleGrid() {
    setPilots((current) => {
      const active = current.filter((pilot) => pilot.active);
      for (let index = active.length - 1; index > 0; index -= 1) {
        const swap = Math.floor(Math.random() * (index + 1));
        const held = active[index];
        active[index] = active[swap];
        active[swap] = held;
      }
      return [...active, ...current.filter((pilot) => !pilot.active)];
    });
  }

  function clearPilotForm() {
    setEditingPilotId(null);
    setFormName("");
    setFormTeam("");
    setFormSponsor("");
  }

  function colorFor(team: string) {
    return teams.find((entry) => entry.name === team)?.color ?? teams[0]?.color ?? "#39FF14";
  }

  function addTeam(raw: string) {
    const name = raw.trim();
    if (!name || teams.some((team) => team.name.toLowerCase() === name.toLowerCase())) return;
    const color = TEAM_PALETTE.find((swatch) => !teams.some((team) => team.color === swatch)) ?? TEAM_PALETTE[teams.length % TEAM_PALETTE.length];
    setTeams((current) => [...current, { name, color }]);
    setFormTeam(name);
  }

  function renameTeam(previous: string, raw: string) {
    const name = raw.trim();
    if (!name || name === previous) return;
    if (teams.some((team) => team.name.toLowerCase() === name.toLowerCase())) return;
    setTeams((current) => current.map((team) => (team.name === previous ? { ...team, name } : team)));
    setPilots((current) => current.map((pilot) => (pilot.team === previous ? { ...pilot, team: name } : pilot)));
    if (formTeam === previous) setFormTeam(name);
  }

  function removeTeam(name: string) {
    if (teams.length <= 1) return;
    const fallback = teams.find((team) => team.name !== name) ?? teams[0];
    setTeams((current) => current.filter((team) => team.name !== name));
    setPilots((current) =>
      current.map((pilot) => (pilot.team === name ? { ...pilot, team: fallback.name, color: fallback.color } : pilot)),
    );
    if (formTeam === name) setFormTeam("");
  }

  function addSponsor(raw: string) {
    const name = raw.trim();
    if (!name || sponsors.some((sponsor) => sponsor.toLowerCase() === name.toLowerCase())) return;
    setSponsors((current) => [...current, name]);
    setFormSponsor(name);
  }

  function renameSponsor(previous: string, raw: string) {
    const name = raw.trim();
    if (!name || name === previous) return;
    if (sponsors.some((sponsor) => sponsor.toLowerCase() === name.toLowerCase())) return;
    setSponsors((current) => current.map((sponsor) => (sponsor === previous ? name : sponsor)));
    setPilots((current) => current.map((pilot) => (pilot.sponsor === previous ? { ...pilot, sponsor: name } : pilot)));
    if (formSponsor === previous) setFormSponsor(name);
  }

  function removeSponsor(name: string) {
    if (sponsors.length <= 1) return;
    const fallback = sponsors.find((sponsor) => sponsor !== name) ?? sponsors[0];
    setSponsors((current) => current.filter((sponsor) => sponsor !== name));
    setPilots((current) => current.map((pilot) => (pilot.sponsor === name ? { ...pilot, sponsor: fallback } : pilot)));
    if (formSponsor === name) setFormSponsor("");
  }

  function savePilotChanges() {
    if (editingPilotId === null) return;
    const name = formName.trim();
    if (!name) return;
    const team = formTeam || teams[0]?.name || "";
    const sponsor = formSponsor || sponsors[0] || "";
    setPilots((current) =>
      current.map((pilot) =>
        pilot.id === editingPilotId ? { ...pilot, name, team, sponsor, color: colorFor(team) } : pilot,
      ),
    );
    clearPilotForm();
  }

  function addPilot() {
    const name = formName.trim();
    if (!name) return;
    const team = formTeam || teams[0]?.name || "";
    const sponsor = formSponsor || sponsors[0] || "";
    const pilot: Pilot = {
      id: Date.now(),
      name,
      team,
      sponsor,
      color: colorFor(team),
      active: true,
    };
    setPilots((current) => arrange([...current, pilot]));
    clearPilotForm();
  }

  function editPilot(pilot: Pilot) {
    setEditingPilotId(pilot.id);
    setFormName(pilot.name);
    setFormTeam(pilot.team);
    setFormSponsor(pilot.sponsor);
  }

  function removePilot(id: number) {
    setPilots((current) => current.filter((pilot) => pilot.id !== id));
    if (editingPilotId === id) clearPilotForm();
  }

  function readyForShot() {
    if (raceStatus !== "RACING" || isFinalStretch || shotLive || !currentPilot) return;
    armShot(turnSeconds, turn, shot, `EN POSICIÓN: ${currentPilot.name}`);
  }

  function activateFinalStretch() {
    if (raceStatus !== "RACING" || isFinalStretch || grid.length === 0 || !currentPilot) return;
    setIsFinalStretch(true);
    setFinalShots(Object.fromEntries(grid.map((pilot) => [pilot.id, 1])));
    playFinalStretchSound();
    rememberBudget(1);
    armShot(FINAL_SHOT_SECONDS, turn, 1, "RECTA FINAL - ÚLTIMO TIRO POR PILOTO", true);
  }

  function gainEnergy() {
    const next = Math.min(100, energyRef.current + 25);
    energyRef.current = next;
    setEnergy(next);
  }

  function castSuper(power: Exclude<SuperPower, "">) {
    if (raceStatus !== "RACING" || energyRef.current < 100) return;
    const notices: Record<Exclude<SuperPower, "">, string> = {
      turbo3x: "⚡ SUPER TURBO 3X: ráfaga al siguiente checkpoint",
      shield: "🛡️ ESCUDO NEÓN: inmunidad a fosos y hielo",
      rainbomb: "💣 BOMBA DE LLUVIA en la bifurcación rival",
      freeze: "🥶 CONGELAMIENTO EN CRUCE: puente bloqueado",
    };
    energyRef.current = 0;
    superRef.current = power;
    setEnergy(0);
    setSuperPower(power);
    audioManager.playPowerUp();
    pushLog(notices[power]);
    void publishSession(livePayload(shot, notices[power], "RACING"));
  }

  function registerShot() {
    if (raceStatus !== "RACING" || !currentPilot || grid.length === 0) return;
    gainEnergy();
    superRef.current = "";
    setSuperPower("");
    recordShot(selected.name, sizeForFormat(selected.format));
    if (isFinalStretch) {
      if (shot < shotBudgetRef.current) {
        parkTurn(turn, shot + 1, `${currentPilot.name} sigue con el tiro extra.`);
        return;
      }
      const nextShots = { ...finalShots, [currentPilot.id]: 0 };
      setFinalShots(nextShots);
      const index = nextFinalIndex(nextShots, turn + 1);
      if (index < 0) {
        finishWith(`${currentPilot.name} cierra la recta final.`);
        return;
      }
      rememberBudget(1);
      armShot(FINAL_SHOT_SECONDS, index, 1, `Tiro final de ${currentPilot.name} · En pista: ${grid[index].name}`, true);
      return;
    }
    if (shot >= shotBudgetRef.current) {
      const nextIndex = (turn + 1) % grid.length;
      handoff(nextIndex, `Tiro ${shot} de ${currentPilot.name} · En pista: ${grid[nextIndex].name}`);
      return;
    }
    parkTurn(turn, shot + 1, `${currentPilot.name} · Tiro ${shot} registrado.`);
  }

  function skipTurn() {
    if (raceStatus !== "RACING" || !currentPilot || grid.length === 0) return;
    if (isFinalStretch) {
      const nextShots = { ...finalShots, [currentPilot.id]: 0 };
      setFinalShots(nextShots);
      const index = nextFinalIndex(nextShots, turn + 1);
      if (index < 0) {
        finishWith(`Turno saltado: ${currentPilot.name}`);
        return;
      }
      armShot(FINAL_SHOT_SECONDS, index, 1, `Turno saltado: ${currentPilot.name} · En pista: ${grid[index].name}`, true);
      return;
    }
    const nextIndex = (turn + 1) % grid.length;
    handoff(nextIndex, `Turno saltado / penalización: ${currentPilot.name} · En pista: ${grid[nextIndex].name}`);
  }

  function publishRule(notice: string, zoneAlert = "") {
    pushLog(notice);
    setAlert(zoneAlert);
    void publishSession(livePayload(shot, notice, "RACING", { alert: zoneAlert }));
    if (!zoneAlert) return;
    const token = Date.now();
    alertToken.current = token;
    window.setTimeout(() => {
      if (alertToken.current !== token) return;
      setAlert("");
      void publishSession(livePayload(shot, notice, "RACING", { alert: "" }));
    }, 4000);
  }

  function callTurbo() {
    if (!currentPilot) return;
    rememberBudget(shotBudgetRef.current + 1);
    audioManager.playTurbo();
    publishRule(`🚀 TURBO: ${currentPilot.name} gana 1 tiro ya (${shotBudgetRef.current} tiros)`);
  }

  function callLong() {
    if (!currentPilot) return;
    const queued = nextQueueRef.current[currentPilot.id] ?? { base: 3, extra: 0 };
    nextQueueRef.current[currentPilot.id] = { base: 4, extra: queued.extra };
    publishRule(`🚀 TIRO LARGO: el siguiente turno de ${currentPilot.name} tendrá ${4 + queued.extra} tiros`);
  }

  function callRain() {
    if (!currentPilot) return;
    audioManager.playRain();
    rememberBudget(1);
    if (shot > 1) {
      const nextIndex = (turn + 1) % grid.length;
      handoff(nextIndex, `🌧️ LLUVIA: ${currentPilot.name} ya usó su único tiro · En pista: ${grid[nextIndex].name}`);
      return;
    }
    publishRule(`🌧️ LLUVIA: el turno de ${currentPilot.name} queda en 1 tiro`);
  }

  function callHand() {
    if (!currentPilot) return;
    audioManager.playIceLava();
    handNextRef.current[currentPilot.id] = true;
    publishRule(
      `🖐️ LAVA / HIELO: ${currentPilot.name} tira el siguiente turno con la otra mano`,
      `🖐️ MANO CAMBIADA: ${currentPilot.name}`,
    );
  }

  function callTakedown() {
    if (!currentPilot || !nextPilot) return;
    skipFirstRef.current[nextPilot.id] = true;
    audioManager.playCrash();
    publishRule(`🥊 TAKEDOWN: ${nextPilot.name} pierde el 1° tiro de su siguiente turno`);
  }

  function callDraft() {
    if (!currentPilot) return;
    const queued = nextQueueRef.current[currentPilot.id] ?? { base: 3, extra: 0 };
    nextQueueRef.current[currentPilot.id] = { base: queued.base, extra: queued.extra + 1 };
    audioManager.playPowerUp();
    publishRule(`💨 REBUFO: el siguiente turno de ${currentPilot.name} gana +1 tiro`);
  }

  return (
    <main className={`mx-auto flex min-h-dvh w-full min-w-0 max-w-md flex-col overflow-x-hidden bg-gray-950 px-4 text-white ${isFinalStretch && raceStatus === "RACING" ? "pt-16" : "pt-6"} ${raceStatus === "RACING" ? "pb-44" : "pb-8"}`}>
      {alert ? (
        <div className="pointer-events-none fixed inset-x-0 top-0 z-30 animate-pulse bg-red-600 px-4 py-3 text-center text-sm font-black tracking-wide text-white">
          {alert}
        </div>
      ) : isFinalStretch && raceStatus === "RACING" ? (
        <div className="pointer-events-none fixed inset-x-0 top-0 z-30 animate-pulse bg-gradient-to-r from-red-600 to-yellow-400 px-4 py-3 text-center text-sm font-black tracking-wide text-gray-950">
          ⚠️ RECTA FINAL - ÚLTIMO TIRO POR PILOTO
        </div>
      ) : null}
      <header className="mb-6 flex flex-col gap-3">
        <button
          type="button"
          onClick={() => requestAdmin("panel")}
          className="w-full rounded-2xl border border-cyan-300/70 bg-cyan-400/10 px-3 py-2 text-left text-[11px] font-black tracking-wide text-cyan-100"
        >
          🔒 MODO ADMIN
        </button>
        <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex w-fit items-center gap-2 rounded-full border border-white/10 bg-white/5 px-3 py-1.5 text-xs backdrop-blur">
          <span className="relative flex h-2.5 w-2.5">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-lime-400 opacity-75" />
            <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-lime-400" />
          </span>
          {connected ? "Proyector Conectado" : "Buscando proyector"}
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => {
              setSalesAdmin(false);
              setProOpen(true);
            }}
            className={`rounded-full px-2.5 py-1.5 text-[10px] font-black tracking-wide ${
              profile.isPro
                ? "border border-yellow-300/80 bg-yellow-300/20 text-yellow-100"
                : "animate-pulse border border-yellow-200 bg-yellow-300 text-gray-950"
            }`}
          >
            {profile.isPro ? "⭐ CUENTA PRO ACTIVA" : "👑 OBTENER PRO / CLOUD"}
          </button>
          <button
            type="button"
            onClick={() => {
              audioManager.init();
              setSoundOn(!audioManager.toggleMute());
            }}
            className="rounded-full border border-white/15 bg-white/5 px-2.5 py-1.5 text-[10px] font-black tracking-wide text-white"
            aria-pressed={soundOn}
          >
            {soundOn ? "🔊 SONIDO: ON" : "🔇 SONIDO: OFF"}
          </button>
          <button
            type="button"
            onClick={() => setHowToOpen(true)}
            className="relative overflow-hidden rounded-full border border-cyan-300/80 bg-cyan-400/10 px-2.5 py-1.5 text-[10px] font-black tracking-wide text-cyan-100"
          >
            <span className="pointer-events-none absolute inset-0 animate-pulse bg-cyan-400/20" />
            <span className="relative">📖 ¿CÓMO JUGAR?</span>
          </button>
          {raceStatus === "RACING" ? (
            <button
              type="button"
              onClick={backToSetup}
              className="inline-flex items-center gap-1 rounded-full border border-white/10 px-2.5 py-1.5 text-[10px] tracking-wide text-white/55"
            >
              <Settings className="h-3.5 w-3.5" />
              RECONFIGURAR
            </button>
          ) : null}
        </div>
        </div>
        <div className="border-b border-white/10 pb-4">
          <h1>
            <Logo className="h-auto w-full" />
          </h1>
          <p className="mt-1 text-[11px] tracking-[0.14em] text-cyan-300/80">Control & Arbitraje de Carreras Phygital</p>
          {webTestAdVisible(profile.isPro) ? (
            <p className="mt-3 rounded-2xl border border-[#ff2bd6] bg-[#ff2bd6]/15 px-3 py-2 text-[11px] font-black leading-snug text-[#ffe6fb] shadow-[0_0_16px_rgba(255,43,214,0.55)]">
              {WEB_TEST_AD_MESSAGE}
            </p>
          ) : null}
          {raceStatus === "RACING" ? (
            <div className="mt-3 flex items-end justify-between">
              <p className="text-[10px] font-semibold tracking-[0.18em] text-white/45">
                TIEMPO CARRERA{!shotLive ? " · EN PAUSA" : ""}
              </p>
              <p className={`text-3xl font-black tabular-nums ${shotLive && raceEndsAt !== null && raceEndsAt - now <= 0 ? "text-red-400" : "text-cyan-100"}`}>
                {raceClockText}
              </p>
            </div>
          ) : null}
        </div>
      </header>

      {raceStatus === "FINISHED" ? (
        <section className="mb-6 rounded-3xl border border-yellow-300/50 bg-yellow-300/10 p-5 text-center shadow-[0_0_28px_rgba(245,217,10,0.18)]">
          <p className="text-[11px] font-semibold tracking-[0.22em] text-yellow-200">CARRERA CERRADA</p>
          <p className="mt-2 text-3xl font-black text-yellow-300">🏆 {winnerName}</p>
          <p className="mt-2 text-sm text-white/70">Gana quien acumuló menos tiempo de penalización. Si empatan, manda el orden de la parrilla.</p>
          <div className="mt-4 grid gap-2">
            <Link href="/stats" className="rounded-2xl bg-yellow-300 py-3 text-sm font-black tracking-wide text-gray-950">
              Ver telemetría
            </Link>
            <button type="button" onClick={backToSetup} className="rounded-xl border border-white/15 py-3 text-sm font-semibold text-white/80">
              Nueva carrera
            </button>
          </div>
        </section>
      ) : raceStatus === "RACING" && currentPilot && nextPilot ? (
        <section
          className="mb-6 overflow-hidden rounded-3xl border bg-white/[0.06] shadow-[inset_0_1px_0_rgba(255,255,255,0.14),0_0_32px_rgba(0,243,255,0.08)] backdrop-blur-xl"
          style={{ borderColor: `${currentPilot.color}66` }}
        >
          <div className="p-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="flex items-center gap-1.5 text-[10px] font-semibold tracking-[0.22em] text-white/55">
                  <Flag className="h-3.5 w-3.5" style={{ color: currentPilot.color }} />
                  EN PISTA · TURNO ACTUAL
                </p>
                <p
                  className="mt-1 text-2xl font-black tracking-tight uppercase"
                  style={{ color: currentPilot.color, textShadow: `0 0 16px ${currentPilot.color}` }}
                >
                  {currentPilot.name}
                </p>
              </div>
              <p className="shrink-0 rounded-full border border-white/15 bg-black/40 px-3 py-1.5 text-xs font-semibold">
                {isFinalStretch ? "TIRO FINAL" : `Tiro ${shot} de ${shotBudget}`}
              </p>
            </div>
            <div className="mt-3 flex flex-wrap gap-2">
              <span
                className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold"
                style={{ borderColor: currentPilot.color, color: currentPilot.color, backgroundColor: `${currentPilot.color}18` }}
              >
                <Shield className="h-3.5 w-3.5" />
                Escudería: {currentPilot.team}
              </span>
              <span className="inline-flex items-center gap-1.5 rounded-full border border-amber-300/40 bg-amber-300/10 px-2.5 py-1 text-[11px] font-semibold text-amber-200">
                <Zap className="h-3.5 w-3.5" />
                Sponsor: {currentPilot.sponsor}
              </span>
            </div>
            <div className="mt-3">
              <div className="mb-1 flex items-center justify-between text-[11px]">
                <span className="tracking-[0.14em] text-white/50">TIEMPO DE TIRO</span>
                <span className={`font-black tabular-nums ${turnLevel === "danger" ? "text-red-300" : turnLevel === "warn" ? "text-yellow-200" : "text-lime-300"}`}>
                  {shotLive ? formatClock(turnLeft) : "EN PAUSA"}
                </span>
              </div>
              <div className="h-2.5 overflow-hidden rounded-full bg-white/10">
                <div
                  className={`h-full rounded-full ${turnBar}`}
                  style={{ width: `${shotLive ? Math.max(0, Math.min(100, (turnLeft / turnBudget) * 100)) : 0}%` }}
                />
              </div>
              {!shotLive && !isFinalStretch ? (
                <p className="mt-2 text-[11px] text-amber-200/80">El reloj de carrera está detenido. Coloca el auto y pulsa piloto en posición.</p>
              ) : null}
            </div>
            <div className="mt-3 grid grid-cols-3 gap-2">
              {[1, 2, 3].map((index) => (
                <p
                  key={index}
                  className={`rounded-lg border py-1.5 text-center text-[11px] font-semibold ${
                    index === shot
                      ? "border-cyan-300/70 bg-cyan-400/20 text-cyan-50"
                      : index < shot
                        ? "border-white/10 bg-white/5 text-white/35"
                        : "border-white/10 text-white/55"
                  }`}
                >
                  Tiro {index}
                </p>
              ))}
            </div>
          </div>
          <div className="flex items-center justify-between gap-3 border-t border-white/10 bg-black/35 px-4 py-2.5">
            <p className="text-xs tracking-wide text-white/60">
              <span className="font-semibold text-white/80">EN BOXES · </span>
              Próximo tiro: {nextPilot.name} ({nextPilot.team})
            </p>
            <p className="shrink-0 text-[11px] font-semibold text-white/45">P{grid.findIndex((pilot) => pilot.id === currentPilot.id) + 1}</p>
          </div>
        </section>
      ) : (
        <section className="mb-6 min-w-0 rounded-3xl border border-lime-300/30 bg-white/[0.06] p-4 shadow-[0_0_32px_rgba(57,255,20,0.08)] backdrop-blur-xl">
          <h2 className="text-sm font-semibold tracking-[0.16em] text-lime-200">PARRILLA DE SALIDA</h2>
          <p className="mt-1 text-[11px] text-white/45">Activa quién corre y define el orden. P1 tira primero.</p>
          <ul className="mt-3 space-y-2">
            {pilots.map((pilot) => {
              const slot = grid.findIndex((entry) => entry.id === pilot.id) + 1;
              return (
                <li
                  key={pilot.id}
                  className={`flex items-center gap-2 rounded-xl border px-2.5 py-2 ${
                    editingPilotId === pilot.id
                      ? "border-lime-300/70 bg-lime-400/10"
                      : pilot.active
                        ? "border-white/15 bg-black/40"
                        : "border-white/5 bg-black/20 opacity-60"
                  }`}
                  style={pilot.active ? { boxShadow: `inset 3px 0 0 ${pilot.color}` } : undefined}
                >
                  <span className="w-8 shrink-0 text-center text-xs font-black" style={{ color: pilot.active ? pilot.color : "#94a3b8" }}>
                    {pilot.active ? `P${slot}` : "—"}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm font-semibold">{pilot.name}</span>
                    <span className="block truncate text-[10px] text-white/45">
                      {pilot.team} · {pilot.sponsor}
                    </span>
                  </span>
                  {pilot.active ? (
                    <span className="flex flex-col">
                      <button
                        type="button"
                        onClick={() => movePilot(pilot.id, -1)}
                        disabled={slot === 1}
                        className="text-white/70 disabled:opacity-25"
                        aria-label={`Subir ${pilot.name}`}
                      >
                        <ChevronUp className="h-4 w-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => movePilot(pilot.id, 1)}
                        disabled={slot === grid.length}
                        className="text-white/70 disabled:opacity-25"
                        aria-label={`Bajar ${pilot.name}`}
                      >
                        <ChevronDown className="h-4 w-4" />
                      </button>
                    </span>
                  ) : null}
                  <button
                    type="button"
                    role="switch"
                    aria-checked={pilot.active}
                    aria-label={`${pilot.active ? "Sacar" : "Meter"} a ${pilot.name}`}
                    onClick={() => togglePilot(pilot.id)}
                    className={`relative h-6 w-11 shrink-0 rounded-full ${pilot.active ? "bg-lime-400" : "bg-white/15"}`}
                  >
                    <span className={`absolute top-0.5 h-5 w-5 rounded-full bg-gray-950 transition-all ${pilot.active ? "left-5" : "left-0.5"}`} />
                  </button>
                  <button type="button" onClick={() => editPilot(pilot)} className="text-white/50" aria-label={`Editar ${pilot.name}`}>
                    <Pencil className="h-4 w-4" />
                  </button>
                  <button type="button" onClick={() => removePilot(pilot.id)} className="text-white/50" aria-label={`Quitar ${pilot.name}`}>
                    <Trash2 className="h-4 w-4" />
                  </button>
                </li>
              );
            })}
          </ul>
          <div className="mt-3 grid min-w-0 gap-2">
            {editingPilotId !== null ? (
              <p className="text-[11px] font-semibold tracking-[0.14em] text-lime-300">EDITANDO PILOTO</p>
            ) : null}
            <input
              value={formName}
              onChange={(event) => setFormName(event.target.value)}
              placeholder="Nombre del piloto"
              className="rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-sm outline-none placeholder:text-white/30"
            />
            <div className="grid grid-cols-2 gap-2">
              <select
                value={formTeam}
                onChange={(event) => setFormTeam(event.target.value)}
                className="rounded-xl border border-white/10 bg-black/40 px-2 py-2 text-xs"
              >
                <option value="">Escudería</option>
                {teams.map((team) => (
                  <option key={team.name} value={team.name}>
                    {team.name}
                  </option>
                ))}
              </select>
              <select
                value={formSponsor}
                onChange={(event) => setFormSponsor(event.target.value)}
                className="rounded-xl border border-white/10 bg-black/40 px-2 py-2 text-xs"
              >
                <option value="">Patrocinador</option>
                {sponsors.map((sponsor) => (
                  <option key={sponsor} value={sponsor}>
                    {sponsor}
                  </option>
                ))}
              </select>
            </div>
            <RosterCatalog
              teams={teams}
              sponsors={sponsors}
              onAddTeam={addTeam}
              onRenameTeam={renameTeam}
              onRemoveTeam={removeTeam}
              onAddSponsor={addSponsor}
              onRenameSponsor={renameSponsor}
              onRemoveSponsor={removeSponsor}
            />
            {editingPilotId !== null ? (
              <div className="grid grid-cols-2 gap-2">
                <button
                  type="button"
                  onClick={savePilotChanges}
                  className="rounded-xl bg-lime-400 py-3 text-xs font-black tracking-wide text-gray-950 shadow-[0_0_18px_rgba(57,255,20,0.45)]"
                >
                  💾 GUARDAR CAMBIOS
                </button>
                <button
                  type="button"
                  onClick={clearPilotForm}
                  className="rounded-xl border border-red-400/50 bg-red-500/15 py-3 text-xs font-black tracking-wide text-red-200"
                >
                  ✖ CANCELAR
                </button>
              </div>
            ) : (
              <button
                type="button"
                onClick={addPilot}
                className="inline-flex items-center justify-center gap-2 rounded-xl border border-cyan-300/50 bg-cyan-400/15 py-3 text-sm font-black tracking-wide text-cyan-100 shadow-[0_0_18px_rgba(0,243,255,0.25)]"
              >
                <Plus className="h-4 w-4" />
                AGREGAR NUEVO PILOTO
              </button>
            )}
            <div className="grid min-w-0 gap-2 rounded-2xl border border-white/10 bg-black/30 p-3">
              <button
                type="button"
                onClick={() => setRaceConfigOpen((current) => !current)}
                aria-expanded={raceConfigOpen}
                className="flex w-full items-center justify-between gap-2 text-left"
              >
                <span className="text-[10px] font-semibold tracking-[0.18em] text-white/50">CONFIGURAR CARRERA</span>
                <ChevronDown className={`h-4 w-4 shrink-0 text-white/70 transition ${raceConfigOpen ? "rotate-180" : ""}`} />
              </button>
              {raceConfigOpen ? (
              <>
              <label className="grid gap-1 text-[11px] text-white/55">
                Tiempo límite de carrera
                <select
                  value={raceMinutes}
                  onChange={(event) => setRaceMinutes(Number(event.target.value))}
                  className="rounded-xl border border-white/10 bg-black/40 px-2 py-2 text-xs text-white"
                >
                  <option value={5}>5 min</option>
                  <option value={10}>10 min</option>
                  <option value={15}>15 min</option>
                  <option value={0}>Sin límite</option>
                </select>
              </label>
              <label className="grid gap-1 text-[11px] text-white/55">
                Tiempo máximo por tiro
                <select
                  value={turnSeconds}
                  onChange={(event) => setTurnSeconds(Number(event.target.value))}
                  className="rounded-xl border border-white/10 bg-black/40 px-2 py-2 text-xs text-white"
                >
                  <option value={120}>2 Minutos (120s)</option>
                  <option value={90}>1:30 min (90s)</option>
                  <option value={30}>30s</option>
                  <option value={45}>45s</option>
                  <option value={60}>60s</option>
                </select>
              </label>
              <label className="grid gap-1 text-[11px] text-white/55">
                Penalización al llegar a cero
                <select
                  value={penalty}
                  onChange={(event) => setPenalty(event.target.value as PenaltyMode)}
                  className="rounded-xl border border-white/10 bg-black/40 px-2 py-2 text-xs text-white"
                >
                  <option value="shot">Pérdida de tiro</option>
                  <option value="turn">Pasar turno automático</option>
                </select>
              </label>
              <div className="mt-2 min-w-0 border-t border-white/10 pt-3">
                <div className="mb-3 flex items-end justify-between">
                  <h2 className="text-sm font-medium text-white/80">{catalog === "rally" ? "Rally Extremo" : "Pistas clásicas"}</h2>
                  <p className="text-xs text-white/45">
                    {catalog === "rally"
                      ? profile.isPro
                        ? "50 tramos A→B"
                        : "15 de 50 tramos"
                      : profile.isPro
                        ? "50 de 150 circuitos"
                        : "15 de 150 circuitos"}
                  </p>
                </div>
                <div className="mb-2 flex min-w-0 gap-2 overflow-x-auto pb-1">
                  {FORMAT_NAMES.map((name, index) => (
                    <button
                      key={name}
                      type="button"
                      onClick={() => {
                        setCatalog("classic");
                        setFormat(index as RaceFormat);
                      }}
                      className={`shrink-0 rounded-full border px-3 py-1.5 text-xs ${
                        catalog === "classic" && format === index ? "border-cyan-300/70 bg-cyan-400/15 text-cyan-100" : "border-white/10 text-white/60"
                      }`}
                    >
                      {name}
                    </button>
                  ))}
                  <button
                    type="button"
                    onClick={() => setCatalog("rally")}
                    className={`shrink-0 rounded-full border px-3 py-1.5 text-xs ${
                      catalog === "rally" ? "border-fuchsia-300/70 bg-fuchsia-400/15 text-fuchsia-100" : "border-white/10 text-white/60"
                    }`}
                  >
                    🏎️ RALLY EXTREMO
                  </button>
                </div>
                {catalog === "classic" ? (
                  <div className="mb-3 flex min-w-0 gap-2 overflow-x-auto pb-1">
                    {MODULE_NAMES.map((name, index) => (
                      <button
                        key={name}
                        type="button"
                        onClick={() => setModuleIndex(index)}
                        className={`shrink-0 rounded-full border px-3 py-1.5 text-xs ${
                          moduleIndex === index ? "border-white/40 bg-white/10 text-white" : "border-white/10 text-white/50"
                        }`}
                      >
                        {name}
                      </button>
                    ))}
                  </div>
                ) : null}
                <div className="flex min-w-0 gap-3 overflow-x-auto pb-2">
                  {races.map((track) => {
                    const active = catalog === "rally" ? track.raceIndex === rallyIndex : track.raceIndex === raceIndex;
                    const slot = catalog === "rally" ? track.raceIndex : classicSlot(format, moduleIndex, track.raceIndex);
                    const locked = slot >= trackCap;
                    return (
                      <button
                        key={track.id}
                        type="button"
                        onClick={() => {
                          if (locked) {
                            if (!profile.isPro) {
                              setSalesAdmin(true);
                              setProOpen(true);
                            }
                            return;
                          }
                          if (catalog === "rally") setRallyIndex(track.raceIndex);
                          else setRaceIndex(track.raceIndex);
                          setEditorTrack(track);
                        }}
                        className={`w-56 shrink-0 rounded-2xl border p-3 text-left backdrop-blur transition ${
                          locked
                            ? "cursor-not-allowed border-white/10 bg-black/50 opacity-60"
                            : active
                              ? "border-cyan-300/70 bg-cyan-400/10 shadow-[0_0_24px_rgba(34,211,238,0.18)]"
                              : "border-white/10 bg-white/5"
                        }`}
                      >
                        <ProjectedTrack track={track} zones={active ? customZones : undefined} />
                        <p className="mt-2 line-clamp-2 text-sm font-semibold">{track.name}</p>
                        <p className="text-xs text-cyan-200/80">
                          {locked
                            ? `Pista ${slot + 1} · PRO`
                            : catalog === "rally"
                              ? "Camino continuo A→B"
                              : `${track.laps} ${track.laps === 1 ? "vuelta" : "vueltas"}`}
                        </p>
                      </button>
                    );
                  })}
                </div>
                <button
                  type="button"
                  onClick={() => setEditorTrack(selected)}
                  className="mt-3 w-full rounded-xl border border-fuchsia-300/50 bg-fuchsia-400/10 py-3 text-sm font-black tracking-wide text-fuchsia-100"
                >
                  ✏️ PERSONALIZAR PISTA
                </button>
                <div className="mt-2 overflow-hidden rounded-2xl px-1 py-4">
                  <button
                    type="button"
                    onClick={projectTrack}
                    className="w-full rounded-xl border border-[#ffb0f4] bg-[#ff2bd6]/25 py-3 text-sm font-black tracking-wide text-[#ffe6fb] shadow-[inset_0_0_14px_rgba(255,43,214,0.5),0_0_16px_rgba(255,43,214,0.9),0_0_32px_rgba(255,43,214,0.55)]"
                  >
                    Proyectar Pista
                  </button>
                </div>
                {projectedName ? (
                  <p className="mt-2 text-center text-xs text-cyan-200/80">En el proyector: {projectedName}</p>
                ) : (
                  <p className="mt-2 text-center text-xs text-white/40">Abre /proyector en el navegador del proyector.</p>
                )}
              </div>
              </>
              ) : null}
            </div>
            <div className="grid gap-2 rounded-2xl border border-white/10 bg-black/30 p-3">
              <button
                type="button"
                onClick={() => setProjectorConfigOpen((current) => !current)}
                aria-expanded={projectorConfigOpen}
                className="flex w-full items-center justify-between gap-2 text-left"
              >
                <span className="text-[10px] font-semibold tracking-[0.18em] text-white/50">CONFIGURAR PROYECTOR</span>
                <ChevronDown className={`h-4 w-4 shrink-0 text-white/70 transition ${projectorConfigOpen ? "rotate-180" : ""}`} />
              </button>
              {projectorConfigOpen ? (
              <>
              <label className="grid gap-1 text-[11px] text-white/55">
                Tamaño de los Autos (Escala)
                <select
                  value={toyScale}
                  onChange={(event) => setToyScale(event.target.value as ToyScale)}
                  aria-label="Tamaño de los Autos (Escala)"
                  className="rounded-xl border border-white/10 bg-black/40 px-2 py-2 text-xs text-white"
                >
                  {TOY_SCALES.map((entry) => (
                    <option key={entry.id} value={entry.id}>
                      {entry.label}
                    </option>
                  ))}
                </select>
              </label>
              <p className="text-[10px] leading-relaxed text-white/40">El carril proyectado cabe dos autos de esa escala, uno al lado del otro.</p>
              <div className="grid min-w-0 gap-2 border-t border-white/10 pt-3">
                <p className="text-[10px] font-semibold tracking-[0.16em] text-white/55">PUBLICIDAD Y PATROCINADORES</p>
                <label className="grid gap-1 text-[11px] text-white/55">
                  ✍️ Nombre del Gran Premio / Banner Principal
                  <input
                    value={sponsorText}
                    onChange={(event) => setSponsorText(event.target.value)}
                    placeholder={DEFAULT_SPONSOR_TEXT}
                    aria-label="Nombre del Gran Premio / Banner Principal"
                    className="rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-xs text-white outline-none placeholder:text-white/30"
                  />
                </label>
                <div className="grid gap-1.5">
                  {SPONSOR_BRANDS.map((brand) => {
                    const active = sponsorLogos.includes(brand);
                    return (
                      <button
                        key={brand}
                        type="button"
                        onClick={() =>
                          setSponsorLogos((current) =>
                            current.includes(brand) ? current.filter((logo) => logo !== brand) : [...current, brand],
                          )
                        }
                        aria-pressed={active}
                        className={`flex items-center justify-between rounded-xl border px-3 py-2 text-left text-xs font-semibold ${
                          active ? "border-lime-300/70 bg-lime-400/15 text-lime-100" : "border-white/10 bg-black/30 text-white/45"
                        }`}
                      >
                        <span className="min-w-0 truncate">{brand}</span>
                        <span>{active ? "ON" : "OFF"}</span>
                      </button>
                    );
                  })}
                </div>
                <button
                  type="button"
                  onClick={() => {
                    if (profile.isPro) return;
                    setSalesAdmin(false);
                    setProOpen(true);
                  }}
                  aria-pressed={!profile.isPro}
                  className={`rounded-xl border-2 py-3 text-sm font-black tracking-wide ${
                    profile.isPro ? "border-yellow-300/70 bg-yellow-300/15 text-yellow-100" : "border-cyan-200 bg-cyan-300 text-gray-950"
                  }`}
                >
                  {profile.isPro ? "📺 BANNERS LED: OFF · PRO" : "📺 BANNERS LED: ON"}
                </button>
              </div>
              <button
                type="button"
                onClick={() => {
                  if (!adminAuthed) {
                    requestAdmin("focus");
                    return;
                  }
                  setShowTestPattern((current) => !current);
                }}
                aria-pressed={showTestPattern}
                className={`rounded-xl border-2 py-3 text-sm font-black tracking-wide ${
                  showTestPattern
                    ? "animate-pulse border-black bg-white text-black"
                    : "border-yellow-200 bg-yellow-300 text-gray-950"
                }`}
              >
                {showTestPattern ? "❌ APAGAR PATRÓN DE ENFOQUE" : "🎯 ACTIVAR PATRÓN DE ENFOQUE"}
              </button>
              </>
              ) : null}
            </div>
            <button
              type="button"
              onClick={shuffleGrid}
              disabled={grid.length < 2}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-fuchsia-400/40 bg-fuchsia-400/10 py-3 text-sm font-semibold text-fuchsia-100 disabled:opacity-40"
            >
              <Dices className="h-4 w-4" />
              SORTEAR ORDEN
            </button>
            <button
              type="button"
              onClick={confirmGrid}
              disabled={grid.length === 0}
              className="rounded-2xl bg-lime-400 py-4 text-sm font-black tracking-wide text-gray-950 shadow-[0_0_28px_rgba(57,255,20,0.45)] disabled:opacity-40"
            >
              CONFIRMAR PARRILLA Y ARRANCAR CARRERA
            </button>
          </div>
        </section>
      )}

      {raceStatus === "RACING" ? (
      <section className="mb-6 rounded-3xl border border-white/10 bg-white/5 p-4 backdrop-blur">
        <h2 className="mb-3 text-sm font-medium text-white/80">Panel de árbitro</h2>
        <div className="mb-3 rounded-2xl border border-lime-300/30 bg-black/40 p-3">
          <div className="mb-2 flex items-center justify-between text-[11px] font-black tracking-wide">
            <span className="text-lime-200">ENERGÍA {energy}%</span>
            <span className="text-white/45">{selected.superPowers ? "SUPER-PODERES HABILITADOS" : "CADA TIRO +25%"}</span>
          </div>
          <div className="mb-3 h-2 overflow-hidden rounded-full bg-white/10">
            <div className="h-full rounded-full bg-lime-400" style={{ width: `${energy}%`, boxShadow: "0 0 12px #39ff14" }} />
          </div>
          <div className="grid grid-cols-2 gap-2">
            {(
              [
                ["turbo3x", "⚡ SUPER TURBO 3X"],
                ["shield", "🛡️ ESCUDO NEÓN"],
                ["rainbomb", "💣 BOMBA DE LLUVIA"],
                ["freeze", "🥶 CONGELAMIENTO EN CRUCE"],
              ] as const
            ).map(([id, label]) => (
              <button
                key={id}
                type="button"
                disabled={energy < 100}
                onClick={() => castSuper(id)}
                className={`rounded-xl border px-2 py-2 text-[10px] font-black disabled:opacity-30 ${
                  superPower === id ? "border-white bg-lime-400/30 text-white" : "border-lime-300/40 bg-lime-400/10 text-lime-100"
                }`}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
        <button
          type="button"
          onClick={activateFinalStretch}
          disabled={isFinalStretch}
          className={`mb-3 w-full rounded-2xl border py-4 text-sm font-black tracking-wide ${
            isFinalStretch
              ? "border-yellow-300/40 bg-yellow-300/10 text-yellow-100"
              : "animate-pulse border-red-400 bg-red-500/20 text-yellow-200 shadow-[0_0_24px_rgba(255,40,40,0.55)]"
          }`}
        >
          {isFinalStretch ? "RECTA FINAL ACTIVA" : "🏁 RECTA FINAL / ÚLTIMOS TIROS"}
        </button>
        <div className="grid grid-cols-2 gap-3">
          {(
            [
              ["turbo", "🚀 TURBO", "+1 TIRO YA", "border-lime-400/50 bg-lime-400/15 text-lime-200", callTurbo],
              ["long", "🚀 TIRO LARGO", "4 TIROS SIG.", "border-lime-300/40 bg-lime-300/10 text-lime-100", callLong],
              ["rain", "🌧️ LLUVIA", "1 TIRO", "border-cyan-300/50 bg-cyan-400/15 text-cyan-100", callRain],
              ["hand", "🖐️ LAVA / HIELO", "MANO CAMBIADA", "border-orange-400/50 bg-orange-500/15 text-orange-100", callHand],
              ["takedown", "🥊 TAKEDOWN", "RIVAL PIERDE 1°", "border-red-400/50 bg-red-500/15 text-red-100", callTakedown],
              ["draft", "💨 REBUFO", "+1 TIRO SIG.", "border-sky-300/50 bg-sky-400/10 text-sky-100", callDraft],
            ] as const
          ).map(([id, label, detail, className, action]) => (
            <button
              key={id}
              type="button"
              onClick={action}
              className={`flex min-h-24 flex-col items-start justify-between rounded-2xl border p-3 text-left backdrop-blur ${className}`}
            >
              <span>
                <span className="block text-sm font-black tracking-wide">{label}</span>
                <span className="block text-[11px] text-white/70">{detail}</span>
              </span>
            </button>
          ))}
        </div>
        <div className="mt-3 min-h-16 rounded-2xl border border-white/10 bg-black/30 p-3 backdrop-blur">
          {log.length === 0 ? (
            <p className="text-xs text-white/40">El historial del árbitro aparece aquí.</p>
          ) : (
            <ul className="space-y-1">
              {log.map((line, index) => (
                <li key={`${line}-${index}`} className="text-xs text-white/80">
                  {line}
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
      ) : null}

      {raceStatus === "RACING" ? (
      <div className="fixed inset-x-0 bottom-0 z-20 mx-auto w-full max-w-md space-y-2 bg-gradient-to-t from-gray-950 via-gray-950 to-transparent px-4 pt-8 pb-4">
        {!shotLive && !isFinalStretch ? (
          <button
            type="button"
            onClick={readyForShot}
            className="w-full rounded-2xl bg-lime-400 py-4 text-lg font-black tracking-wide text-gray-950 shadow-[0_0_28px_rgba(57,255,20,0.45)]"
          >
            PILOTO EN POSICIÓN
          </button>
        ) : null}
        <button
          type="button"
          onClick={skipTurn}
          disabled={grid.length === 0}
          className="w-full rounded-xl border border-amber-300/40 bg-amber-400/10 py-2.5 text-xs font-semibold tracking-wide text-amber-100 backdrop-blur disabled:opacity-40"
        >
          SALTAR TURNO / PENALIZACIÓN
        </button>
        <button
          type="button"
          onClick={registerShot}
          className="w-full rounded-2xl bg-gradient-to-r from-blue-600 to-cyan-400 py-4 text-lg font-bold text-white shadow-[0_10px_30px_rgba(37,99,235,0.35)]"
        >
          ¡YA TIRÉ!
        </button>
      </div>
      ) : null}
      {adminOpen && adminAuthed ? (
        <div className="fixed inset-0 z-40 grid place-items-end bg-black/50 p-3 sm:place-items-center">
          <div className="max-h-[92dvh] w-full max-w-md overflow-y-auto rounded-3xl border border-cyan-300/70 bg-gray-950 p-4 shadow-[0_0_28px_rgba(0,243,255,0.35)]" role="dialog" aria-label="Configuración de administrador">
            <div className="mb-3 flex items-start justify-between gap-3">
              <div>
                <p className="text-[11px] font-black tracking-[0.22em] text-cyan-200">SESIÓN ACTIVA</p>
                <h2 className="text-lg font-black text-cyan-100">CONFIGURACIÓN DE ADMINISTRADOR</h2>
              </div>
              <button type="button" onClick={() => setAdminOpen(false)} className="rounded-full border border-white/15 px-3 py-1 text-xs text-white/70" aria-label="Cerrar administrador">
                Cerrar
              </button>
            </div>
            <div className="grid gap-2">
              <button
                type="button"
                onClick={() => {
                  const account = loginWithPin("9999");
                  if (!account) return;
                  setProfile(getSubscriptionStatus());
                  setAdminNotice("👑 MODO VIP ALL-INCLUSIVE ACTIVADO");
                }}
                className="w-full rounded-2xl border border-yellow-300 bg-yellow-300 px-3 py-3 text-left text-sm font-black text-gray-950"
              >
                👑 MODO ALL-INCLUSIVE (PIN 9999)
              </button>
              <button
                type="button"
                onClick={() => {
                  const account = loginWithPin("1111");
                  if (!account) return;
                  setProfile(getSubscriptionStatus());
                  setAdminNotice("🧪 MODO GRATIS ACTIVADO");
                }}
                className="w-full rounded-2xl border border-fuchsia-300/80 bg-fuchsia-400/15 px-3 py-3 text-left text-sm font-black text-fuchsia-100"
              >
                🧪 FORZAR MODO FREE (PIN 1111)
              </button>
            </div>
            <button
              type="button"
              onClick={() => {
                if (!window.confirm("¿Borrar el historial de medallas y las tablas de posiciones de este dispositivo?")) return;
                setAdminNotice(clearMedalBoard() ? "Medallero borrado." : "No se pudo borrar el medallero.");
              }}
              className="mt-2 w-full rounded-2xl border border-red-400/70 bg-red-500/20 py-3 text-sm font-black text-red-100"
            >
              🏆 BORRAR HISTORIAL DE MEDALLAS
            </button>
            <div className="mt-2 grid gap-2">
              <button
                type="button"
                onClick={() => {
                  setShowTestPattern(true);
                  setAdminNotice("Patrón de enfoque enviado al proyector.");
                }}
                className="rounded-2xl border border-yellow-200 bg-yellow-300 py-3 text-sm font-black text-gray-950"
              >
                🎯 ABRIR PATRÓN DE ENFOQUE
              </button>
              <button
                type="button"
                onClick={() => {
                  resetKeystone();
                  setAdminNotice("Keystone de la pared reiniciado. El proyector lo toma al momento.");
                }}
                className="rounded-2xl border border-white/15 py-3 text-sm font-semibold text-white/80"
              >
                Reiniciar Keystone de pared
              </button>
            </div>
            <form
              className="mt-3 grid gap-2 rounded-2xl border border-white/10 p-3"
              onSubmit={(event) => {
                event.preventDefault();
                const ok = updateAdminPin(oldPin, newPin);
                setAdminNotice(ok ? "PIN de administrador actualizado." : "No se cambió el PIN. Revisa la clave actual y usa 4 dígitos.");
                if (ok) {
                  setOldPin("");
                  setNewPin("");
                }
              }}
            >
              <p className="text-[10px] font-semibold tracking-[0.16em] text-white/50">🔑 CAMBIAR PIN DE ADMINISTRADOR</p>
              <input
                value={oldPin}
                onChange={(event) => setOldPin(event.target.value)}
                inputMode="numeric"
                type="password"
                maxLength={4}
                placeholder="PIN actual"
                aria-label="PIN actual"
                className="rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-sm text-white outline-none"
              />
              <input
                value={newPin}
                onChange={(event) => setNewPin(event.target.value)}
                inputMode="numeric"
                type="password"
                maxLength={4}
                placeholder="PIN nuevo de 4 dígitos"
                aria-label="PIN nuevo"
                className="rounded-xl border border-white/10 bg-black/40 px-3 py-2 text-sm text-white outline-none"
              />
              <button type="submit" className="rounded-xl border border-cyan-300/50 py-2 text-sm font-semibold text-cyan-100">
                Guardar PIN
              </button>
            </form>
            {adminNotice ? <p className="mt-3 text-center text-xs text-white/75">{adminNotice}</p> : null}
            <button
              type="button"
              onClick={() => {
                logoutAdmin();
                setAdminAuthed(false);
                setAdminOpen(false);
                setAdminNotice("");
              }}
              className="mt-3 w-full rounded-2xl border border-white/15 py-3 text-sm font-semibold text-white/70"
            >
              🚪 CERRAR SESIÓN DE ADMIN
            </button>
          </div>
        </div>
      ) : null}
      <HowToPlayModal open={howToOpen} onClose={() => setHowToOpen(false)} />
      <SubscriptionModal
        open={proOpen}
        allowAdmin={salesAdmin}
        onClose={() => {
          setProOpen(false);
          setSalesAdmin(false);
        }}
        onChange={setProfile}
        onAdminAccess={() => {
          setProOpen(false);
          requestAdmin("unlock");
        }}
      />
      <AdminAuthModal open={authOpen} onClose={() => setAuthOpen(false)} onSuccess={() => finishAdmin(adminIntent)} />
      <TrackEditorModal
        track={editorTrack}
        onClose={() => setEditorTrack(null)}
        onSave={(zones) => {
          if (!editorTrack) return;
          saveCustom(editorTrack.id, zones);
          setRaceIndex(editorTrack.raceIndex);
          setCustomZones(zones);
          setEditorTrack(null);
        }}
        onProject={(zones) => {
          if (!editorTrack) return;
          saveCustom(editorTrack.id, zones);
          setRaceIndex(editorTrack.raceIndex);
          setCustomZones(zones);
          setProjectedName(editorTrack.name);
          const phase = raceStatus === "SETUP" ? "SETUP" : raceStatus;
          void publishSession({
            ...livePayload(phase === "SETUP" ? 1 : shot, editorTrack.name, phase),
            format: editorTrack.format,
            module: editorTrack.moduleIndex,
            race: editorTrack.raceIndex,
            customZones: zones,
          });
          window.open("/proyector", "phygital-proyector");
          setEditorTrack(null);
        }}
      />
    </main>
  );
}
