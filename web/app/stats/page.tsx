"use client";

import { useRef, useState } from "react";
import Link from "next/link";
import { Trophy } from "lucide-react";
import { Logo } from "@/components/Logo";
import {
  exportStatsToCSV,
  exportStatsToJSON,
  favoriteTrack,
  getHistory,
  getLeaderboard,
  getRecords,
  importStats,
  totalTurbos,
  type GameSession,
} from "@/lib/statsManager";

function formatTime(ms: number) {
  const seconds = Math.max(0, Math.round(ms / 1000));
  const minutes = Math.floor(seconds / 60);
  return `${minutes}:${String(seconds % 60).padStart(2, "0")}`;
}

export default function StatsPage() {
  const fileRef = useRef<HTMLInputElement>(null);
  const [history, setHistory] = useState<GameSession[]>(() => getHistory());
  const [message, setMessage] = useState("");

  function refresh(next: GameSession[], note: string) {
    setHistory(next);
    setMessage(note);
  }

  const board = getLeaderboard();
  const leader = board[0];
  const recent = history.slice(0, 5);
  const records = getRecords();

  return (
    <main className="mx-auto min-h-dvh w-full max-w-md bg-gray-950 px-4 py-6 text-white">
      <Logo className="mb-4 h-auto w-full" />
      <header className="mb-6 flex items-end justify-between">
        <div>
          <p className="text-[11px] tracking-[0.28em] text-cyan-300/80">TELEMETRÍA</p>
          <h1 className="text-2xl font-semibold">Datos de carrera</h1>
        </div>
        <Link href="/" className="text-xs text-white/50">
          Volver
        </Link>
      </header>

      <section className="mb-4 rounded-3xl border border-cyan-300/30 bg-cyan-400/10 p-4 backdrop-blur">
        <p className="text-xs tracking-[0.18em] text-cyan-100/70">RÉCORDS</p>
        <p className="mt-2 text-lg font-semibold">{leader ? leader.name : "Sin ganadores"}</p>
        <p className="text-sm text-white/70">{leader ? `${leader.wins} victorias` : "Cierra un turno para empezar el historial."}</p>
        <div className="mt-3 grid grid-cols-2 gap-2 text-xs text-white/70">
          <p>Pista preferida</p>
          <p className="text-right text-white">{favoriteTrack()}</p>
          <p>Turbos activados</p>
          <p className="text-right text-lime-300">{totalTurbos()}</p>
        </div>
        <div className="mt-3 space-y-1 text-xs text-white/60">
          {(["1x1m", "5x5m", "10x10m"] as const).map((size) => (
            <p key={size}>
              {size}: {records[size] ? `${records[size]?.player} · ${formatTime(records[size]?.elapsedMs ?? 0)}` : "sin tiempo"}
            </p>
          ))}
        </div>
      </section>

      <section className="mb-4 rounded-3xl border border-white/10 bg-white/5 p-4 backdrop-blur">
        <p className="mb-3 text-xs tracking-[0.18em] text-white/50">ÚLTIMAS 5 CARRERAS</p>
        {recent.length === 0 ? (
          <p className="text-sm text-white/40">Todavía no hay partidas guardadas.</p>
        ) : (
          <ul className="space-y-3">
            {recent.map((session) => (
              <li key={session.id} className="flex items-center gap-3">
                <Trophy className="h-4 w-4 shrink-0 text-yellow-300" />
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium">{session.winner}</p>
                  <p className="truncate text-xs text-white/50">
                    {session.trackName} · {session.totalShots} tiros · {session.precision}%
                  </p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      {board.length > 0 ? (
        <section className="mb-4 rounded-3xl border border-white/10 bg-black/30 p-4">
          <p className="mb-2 text-xs tracking-[0.18em] text-white/50">TABLA</p>
          {board.slice(0, 5).map((row) => (
            <p key={row.name} className="flex justify-between text-sm">
              <span>{row.name}</span>
              <span className="text-white/60">{row.wins} v · {row.precision}%</span>
            </p>
          ))}
        </section>
      ) : null}

      <button
        type="button"
        onClick={() => {
          exportStatsToJSON();
          exportStatsToCSV();
          setMessage("Se descargaron el JSON y el CSV.");
        }}
        className="w-full rounded-2xl border border-cyan-300/60 bg-cyan-400/20 py-4 text-sm font-semibold tracking-wide text-cyan-100 shadow-[0_0_24px_rgba(34,211,238,0.25)]"
      >
        EXPORTAR DATOS A PC
      </button>
      <p className="mt-2 text-center text-[11px] text-white/40">.json y .csv, listos para Excel</p>

      <button
        type="button"
        onClick={() => fileRef.current?.click()}
        className="mt-6 w-full text-center text-xs text-white/45 underline decoration-white/20"
      >
        Importar Telemetría
      </button>
      <input
        ref={fileRef}
        type="file"
        accept="application/json,.json"
        className="hidden"
        onChange={async (event) => {
          const file = event.target.files?.[0];
          if (!file) return;
          try {
            refresh(importStats(await file.text()), "Telemetría cargada en esta pantalla.");
          } catch {
            setMessage("Ese archivo no se pudo leer.");
          }
          event.target.value = "";
        }}
      />
      {message ? <p className="mt-3 text-center text-xs text-cyan-200/80">{message}</p> : null}
    </main>
  );
}
