"use client";

import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { audioManager, playFinalStretchSound, playShotCountdown, playTurnStartSound } from "@/lib/audioManager";
import { CalibrationPattern } from "@/components/CalibrationPattern";
import { KeystoneStage } from "@/components/KeystoneStage";
import { Logo } from "@/components/Logo";
import { ProjectedTrack } from "@/components/ProjectedTrack";
import { getTrack, type RaceFormat } from "@/lib/catalog";
import { buildRallyTrack } from "@/lib/rallyTracks";
import { readZoneList } from "@/lib/trackEditor";
import { formatClock, urgency } from "@/lib/raceClock";
import { getScaleMetrics } from "@/lib/scaleManager";
import { useLiveLink } from "@/lib/useLiveLink";
import type { LiveState } from "@/lib/sessionBus";

function readFormat(value: string | null): RaceFormat {
  const parsed = Number(value);
  if (parsed === 1 || parsed === 2) return parsed;
  return 0;
}

function readIndex(value: string | null) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

export function ProjectorScreen() {
  const params = useSearchParams();
  const [live, setLive] = useState<LiveState | null>(null);
  const [now, setNow] = useState(0);
  const [soundOn, setSoundOn] = useState(true);
  const heardCue = useRef<number | null>(null);
  const heardPulse = useRef<number | null>(null);
  const heardStretch = useRef(false);
  const heardAlert = useRef("");
  const heardPower = useRef("");
  const heardNotice = useRef("");
  useLiveLink(setLive);

  useEffect(() => {
    setNow(Date.now());
    setSoundOn(!audioManager.isMuted());
    const unlock = () => audioManager.init();
    window.addEventListener("pointerdown", unlock);
    const timer = window.setInterval(() => setNow(Date.now()), 200);
    return () => {
      window.removeEventListener("pointerdown", unlock);
      window.clearInterval(timer);
    };
  }, []);

  const session = live?.active ? live : null;
  const format = (session ? session.format : readFormat(params.get("formato"))) as RaceFormat;
  const moduleIndex = session ? session.module : readIndex(params.get("modulo"));
  const raceIndex = session ? session.race : readIndex(params.get("carrera"));
  const rally = session?.catalog === "rally" || params.get("familia") === "rally";
  const track = rally ? buildRallyTrack(raceIndex) : getTrack(format === 1 || format === 2 ? format : 0, moduleIndex, raceIndex);
  const shot = session ? session.shot : 1;
  const preparing = session?.phase === "SETUP";
  const grid = session?.grid ?? [];
  const budget = session?.turnBudgetMs || 120000;
  const shotLive = Boolean(session?.turnEndsAt);
  const turnLeft = shotLive && now > 0 ? Math.max(0, (session?.turnEndsAt ?? now) - now) : budget;
  const raceLeft =
    now === 0 || session?.racePaused
      ? session?.raceRemainingMs ?? null
      : session?.raceEndsAt
        ? Math.max(0, session.raceEndsAt - now)
        : session?.raceRemainingMs ?? null;
  const level = shotLive ? urgency(turnLeft, budget) : "ok";
  const barColor = level === "danger" ? "#ff3355" : level === "warn" ? "#f5d90a" : "#39ff14";
  const racing = session?.phase === "RACING";
  const finished = session?.phase === "FINISHED";
  const stretch = Boolean(session?.isFinalStretch);
  const strokes = getScaleMetrics(session?.scale ?? "1:64");

  useEffect(() => {
    if (!racing || !session?.cueAt || heardCue.current === session.cueAt) return;
    heardCue.current = session.cueAt;
    playTurnStartSound();
  }, [racing, session?.cueAt]);

  useEffect(() => {
    if (!racing || !shotLive) return;
    const left = Math.ceil(turnLeft / 1000);
    if (left <= 10 && left > 0 && heardPulse.current !== left) {
      heardPulse.current = left;
      playShotCountdown();
    }
    if (left > 10) heardPulse.current = null;
  }, [racing, shotLive, turnLeft]);

  useEffect(() => {
    if (!stretch) {
      heardStretch.current = false;
      return;
    }
    if (heardStretch.current) return;
    heardStretch.current = true;
    playFinalStretchSound();
  }, [stretch]);

  useEffect(() => {
    const alert = session?.alert ?? "";
    if (!alert) {
      heardAlert.current = "";
      return;
    }
    if (heardAlert.current === alert) return;
    heardAlert.current = alert;
    const text = alert.toUpperCase();
    if (text.includes("LLUVIA")) audioManager.playRain();
    else if (text.includes("HIELO") || text.includes("LAVA") || text.includes("MANO")) audioManager.playIceLava();
    else if (text.includes("FOSO")) audioManager.playPitFoso();
    else if (text.includes("TURBO")) audioManager.playTurbo();
    else audioManager.playCrash();
  }, [session?.alert]);

  useEffect(() => {
    const notice = session?.notice ?? "";
    if (!racing || !notice) return;
    if (heardNotice.current === notice) return;
    heardNotice.current = notice;
    if (session?.alert) return;
    const text = notice.toUpperCase();
    if (text.includes("TURBO")) audioManager.playTurbo();
    else if (text.includes("LLUVIA")) audioManager.playRain();
    else if (text.includes("FOSO")) audioManager.playPitFoso();
    else if (text.includes("TAKEDOWN") || text.includes("CHOQUE")) audioManager.playCrash();
  }, [racing, session?.notice, session?.alert]);

  useEffect(() => {
    const power = session?.superPower ?? "";
    if (!power) {
      heardPower.current = "";
      return;
    }
    if (heardPower.current === power) return;
    heardPower.current = power;
    audioManager.playPowerUp();
  }, [session?.superPower]);

  useEffect(() => {
    if (!racing) {
      audioManager.stopPulse();
      return;
    }
    audioManager.startPulse();
    const raceUrgent = raceLeft !== null && raceLeft <= 30_000;
    const shotUrgent = shotLive && turnLeft <= 30_000;
    audioManager.setTension(raceUrgent || shotUrgent);
  }, [racing, raceLeft, shotLive, turnLeft]);

  return (
    <main className={`relative flex min-h-dvh text-white ${stretch ? "bg-[#1a0508]" : "bg-black"}`}>
      {stretch ? <div className="pointer-events-none absolute inset-0 z-0 animate-pulse bg-gradient-to-b from-red-600/30 via-transparent to-yellow-400/15" /> : null}
      {preparing ? (
        <aside className="absolute top-6 bottom-6 left-6 z-10 flex w-80 flex-col gap-3 overflow-y-auto rounded-3xl border border-white/10 bg-black/80 p-4 backdrop-blur">
          <p className="text-xs font-semibold tracking-[0.18em] text-lime-300">PARRILLA DE SALIDA</p>
          <p className="text-[11px] text-white/50">Alineando puestos de salida</p>
          {grid.map((card) => (
            <article
              key={card.id}
              className="rounded-2xl border bg-black/70 px-3 py-2"
              style={{ borderColor: card.color, boxShadow: `0 0 16px ${card.color}55` }}
            >
              <p className="text-[10px] font-bold tracking-[0.22em]" style={{ color: card.color }}>
                P{card.slot}
              </p>
              <p className="truncate text-sm font-black uppercase">{card.name}</p>
              <p className="truncate text-[10px] text-white/60">{card.team}</p>
              {card.sponsor ? <p className="truncate text-[10px] text-amber-200/80">{card.sponsor}</p> : null}
            </article>
          ))}
        </aside>
      ) : null}
      <section className={`flex min-h-dvh flex-1 flex-col items-center justify-center px-6 ${preparing ? "pl-96" : ""}`}>
        <div className="mb-3 flex flex-col items-center gap-1">
          <p className="text-center text-sm tracking-[0.18em] text-cyan-100/80">
            {preparing ? "PARRILLA DE SALIDA EN PREPARACIÓN" : track.name}
          </p>
          {racing ? (
            <>
              <p className="text-[11px] tracking-[0.22em] text-white/50">TIEMPO DE TIRO</p>
              <p className={`text-5xl font-black tabular-nums ${level === "danger" ? "animate-pulse text-red-400" : "text-white"}`}>
                {shotLive ? formatClock(turnLeft) : "EN PAUSA"}
              </p>
              <p className="text-xs text-white/45">
                {raceLeft === null ? "CARRERA SIN LÍMITE" : `CARRERA ${formatClock(raceLeft)}`}
                {session?.racePaused ? " · EN PAUSA" : ""} · {stretch ? "TIRO FINAL" : `Tiro ${shot} de ${session?.shotLimit ?? 3}`}
              </p>
              <div className={`mt-1 h-2 w-72 overflow-hidden rounded-full bg-white/10 ${level === "danger" ? "animate-pulse" : ""}`}>
                <div
                  className="h-full rounded-full"
                  style={{
                    width: `${shotLive ? Math.max(0, Math.min(100, (turnLeft / budget) * 100)) : 0}%`,
                    backgroundColor: barColor,
                    boxShadow: `0 0 16px ${barColor}`,
                  }}
                />
              </div>
            </>
          ) : (
            <p className={`text-center text-lg font-semibold ${preparing ? "animate-pulse text-lime-300" : "text-white"}`}>
              {preparing ? "Alineando puestos de salida" : `Tiro ${shot} de ${session?.shotLimit ?? 3}`}
            </p>
          )}
        </div>
        {session?.superPower === "turbo3x" ? (
          <p className="mb-2 text-center text-2xl font-black text-lime-300">⚡ SUPER TURBO 3X</p>
        ) : null}
        {session?.superPower === "shield" ? (
          <p className="mb-2 text-center text-2xl font-black text-lime-200">🛡️ ESCUDO NEÓN</p>
        ) : null}
        {session?.superPower === "rainbomb" ? (
          <p className="mb-2 text-center text-2xl font-black text-sky-300">💣 BOMBA DE LLUVIA</p>
        ) : null}
        {session?.superPower === "freeze" ? (
          <p className="mb-2 text-center text-2xl font-black text-cyan-100">🥶 CONGELAMIENTO EN CRUCE</p>
        ) : null}
        {stretch && !finished ? (
          <p className="mb-3 animate-pulse text-center text-4xl font-black tracking-wide text-yellow-300 drop-shadow-[0_0_18px_rgba(255,40,40,0.95)]">
            🏁 RECTA FINAL - TIROS DECISIVOS
          </p>
        ) : null}
        <KeystoneStage>
          {session?.showTestPattern ? (
            <CalibrationPattern />
          ) : (
            <ProjectedTrack
              track={track}
              mode="projector"
              clockRatio={racing && shotLive ? turnLeft / budget : undefined}
              finalStretch={stretch}
              zones={Array.isArray(session?.customZones) ? readZoneList(session.customZones) : undefined}
              effect={session?.superPower ?? ""}
              laneStroke={strokes.laneStroke}
              centerLineStroke={strokes.centerLineStroke}
              bridgeStroke={strokes.bridgeStroke}
              zoneFactor={strokes.zoneScaleFactor}
              showBanners={session?.isPro !== true}
              sponsorText={session?.sponsorText}
              sponsorLogos={session?.sponsorLogos}
              vip={session?.isPro === true}
            />
          )}
        </KeystoneStage>
        {!preparing && live?.notice ? <p className="mt-3 text-center text-sm text-cyan-200">{live.notice}</p> : null}
      </section>
      {finished && session?.winner ? (
        <div className="pointer-events-none absolute inset-0 z-20 grid place-items-center bg-black/55 px-8 text-center">
          <p className="text-5xl font-black tracking-wide text-yellow-300 drop-shadow-[0_0_18px_rgba(245,217,10,0.95)]">
            🏆 {session.winner}
          </p>
        </div>
      ) : null}
      {session?.phase === "RACING" && session.cueAt && now - session.cueAt < 4000 && !session.alert && !stretch ? (
        <div className="pointer-events-none absolute inset-0 z-20 grid place-items-center bg-lime-400/20">
          <p className="text-center text-5xl font-black tracking-wide text-lime-300 drop-shadow-[0_0_18px_rgba(57,255,20,0.95)]">
            ¡TU TURNO! ({formatClock(budget)})
          </p>
        </div>
      ) : null}
      {session?.alert ? (
        <div className="absolute inset-0 z-20 grid place-items-center bg-red-600/60 px-8 text-center animate-pulse">
          <p className="text-4xl font-black tracking-wide">{session.alert}</p>
        </div>
      ) : null}
      <button
        type="button"
        onClick={() => {
          audioManager.init();
          setSoundOn(!audioManager.toggleMute());
        }}
        className="absolute top-4 right-4 z-40 rounded-full border border-white/20 bg-black/70 px-3 py-1.5 text-[11px] font-black tracking-wide text-white"
        aria-pressed={soundOn}
      >
        {soundOn ? "🔊 SONIDO: ON" : "🔇 SONIDO: OFF"}
      </button>
      <Logo className="pointer-events-none absolute right-4 bottom-3 z-30 h-14 w-auto opacity-90" />
    </main>
  );
}
