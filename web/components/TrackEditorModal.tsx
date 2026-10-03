"use client";

import { useEffect, useRef, useState } from "react";
import { ProjectedTrack } from "@/components/ProjectedTrack";
import type { CatalogTrack } from "@/lib/catalog";
import {
  freeSpot,
  loadCustom,
  nearestT,
  nextScale,
  pointAt,
  zoneColor,
  zoneLabel,
  zonesFromTrack,
  type CustomZone,
  type ZoneKind,
} from "@/lib/trackEditor";

type Props = {
  track: CatalogTrack | null;
  onClose: () => void;
  onSave: (zones: CustomZone[]) => void;
  onProject: (zones: CustomZone[]) => void;
};

const ADDERS: { kind: ZoneKind; label: string }[] = [
  { kind: "turbo", label: "🚀 + TURBO" },
  { kind: "rain", label: "🌧️ + LLUVIA" },
  { kind: "ice", label: "🧊 + HIELO/TRAMPA" },
  { kind: "exact", label: "🎯 + EXACTO" },
  { kind: "pit", label: "💥 + FOSO" },
];

function clientToTrack(svg: SVGSVGElement, clientX: number, clientY: number) {
  const matrix = svg.getScreenCTM();
  if (!matrix) return null;
  const point = svg.createSVGPoint();
  point.x = clientX;
  point.y = clientY;
  const local = point.matrixTransform(matrix.inverse());
  return { x: local.x, z: -local.y };
}

/**
 * Lienzo para mover, agrandar y agregar zonas sobre el perímetro de una pista.
 */
export function TrackEditorModal({ track, onClose, onSave, onProject }: Props) {
  const svgRef = useRef<SVGSVGElement>(null);
  const [zones, setZones] = useState<CustomZone[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const dragId = useRef<string | null>(null);

  useEffect(() => {
    if (!track) return;
    const seed = loadCustom(track.id) ?? zonesFromTrack(track);
    setZones(seed);
    setSelectedId(seed[0]?.id ?? null);
  }, [track]);

  useEffect(() => {
    if (!track) return;
    function onKey(event: KeyboardEvent) {
      if (event.key === "Escape") onClose();
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [track, onClose]);

  if (!track) return null;
  const activeTrack = track;

  const selected = zones.find((zone) => zone.id === selectedId) ?? null;

  function moveZone(id: string, clientX: number, clientY: number) {
    const svg = svgRef.current;
    if (!svg) return;
    const spot = clientToTrack(svg, clientX, clientY);
    if (!spot) return;
    const t = nearestT(activeTrack.points, spot.x, spot.z, activeTrack.open !== true);
    setZones((current) => current.map((zone) => (zone.id === id ? { ...zone, t } : zone)));
  }

  function addZone(kind: ZoneKind) {
    const zone: CustomZone = {
      id: `z-${Date.now().toString(36)}-${kind}`,
      kind,
      t: freeSpot(zones),
      scale: 1,
    };
    setZones((current) => [...current, zone]);
    setSelectedId(zone.id);
  }

  function resize(direction: -1 | 1) {
    if (!selected) return;
    setZones((current) => current.map((zone) => (zone.id === selected.id ? { ...zone, scale: nextScale(zone.scale, direction) } : zone)));
  }

  function removeSelected() {
    if (!selected || selected.kind === "finish") return;
    setZones((current) => current.filter((zone) => zone.id !== selected.id));
    setSelectedId(null);
  }

  return (
    <div className="fixed inset-0 z-50 bg-black/80 p-2 sm:p-4" onClick={onClose}>
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="track-editor-title"
        onClick={(event) => event.stopPropagation()}
        className="mx-auto flex h-full max-w-5xl flex-col overflow-hidden rounded-3xl border border-cyan-300/40 bg-gray-950 text-white shadow-[0_0_40px_rgba(0,243,255,0.25)]"
      >
        <header className="flex items-center justify-between gap-3 border-b border-white/10 px-4 py-3">
          <h2 id="track-editor-title" className="text-sm font-black tracking-wide sm:text-base">
            ✏️ EDITOR DE PISTA: {track.name}
          </h2>
          <button type="button" onClick={onClose} className="rounded-full border border-white/15 px-3 py-1 text-xs font-black text-white/70">
            ✖ CERRAR
          </button>
        </header>

        <div className="flex gap-2 overflow-x-auto border-b border-white/10 px-3 py-2">
          {ADDERS.map((adder) => (
            <button
              key={adder.kind}
              type="button"
              onClick={() => addZone(adder.kind)}
              className="shrink-0 rounded-full border border-white/15 bg-white/5 px-3 py-2 text-xs font-black"
            >
              {adder.label}
            </button>
          ))}
        </div>

        <div className="relative min-h-0 flex-1 bg-black">
          <div className="pointer-events-none absolute inset-3">
            <ProjectedTrack track={track} mode="projector" zones={zones} />
          </div>
          <svg
            ref={svgRef}
            viewBox="-0.72 -0.72 1.44 1.44"
            className="absolute inset-3 h-[calc(100%-1.5rem)] w-[calc(100%-1.5rem)] touch-none"
          >
            {zones.map((zone) => {
              const spot = pointAt(track.points, zone.t, track.open !== true);
              const active = zone.id === selectedId;
              return (
                <circle
                  key={zone.id}
                  role="button"
                  aria-label={`Mover ${zoneLabel(zone.kind)}`}
                  cx={spot.x}
                  cy={-spot.z}
                  r={0.045 * zone.scale}
                  fill="transparent"
                  stroke={active ? "#ffffff" : "transparent"}
                  strokeWidth="0.008"
                  style={{ cursor: "grab" }}
                  onPointerDown={(event) => {
                    dragId.current = zone.id;
                    setSelectedId(zone.id);
                    try {
                      event.currentTarget.setPointerCapture(event.pointerId);
                    } catch {
                      /* El puntero ya no está activo. */
                    }
                  }}
                  onPointerMove={(event) => {
                    if (dragId.current !== zone.id) return;
                    moveZone(zone.id, event.clientX, event.clientY);
                  }}
                  onPointerUp={() => {
                    dragId.current = null;
                  }}
                >
                  <title>{zoneLabel(zone.kind)}</title>
                </circle>
              );
            })}
          </svg>
        </div>

        <footer className="grid gap-2 border-t border-white/10 px-3 py-3">
          <div className="flex flex-wrap items-center gap-2 text-xs">
            <p className="font-black" style={{ color: selected ? zoneColor(selected.kind) : "#ffffff" }}>
              {selected ? zoneLabel(selected.kind) : "Toca una zona"}
            </p>
            <button type="button" onClick={() => resize(-1)} disabled={!selected} className="rounded-lg border border-white/15 px-3 py-2 font-black disabled:opacity-30">
              − REDUCIR
            </button>
            <p className="min-w-10 text-center font-black text-cyan-200">{selected ? `${selected.scale}x` : "—"}</p>
            <button type="button" onClick={() => resize(1)} disabled={!selected} className="rounded-lg border border-cyan-300/40 bg-cyan-400/10 px-3 py-2 font-black text-cyan-100 disabled:opacity-30">
              + AUMENTAR
            </button>
            <button
              type="button"
              onClick={removeSelected}
              disabled={!selected || selected.kind === "finish"}
              className="rounded-lg border border-red-400/40 bg-red-500/10 px-3 py-2 font-black text-red-200 disabled:opacity-30"
            >
              🗑️ ELIMINAR TRAMPA
            </button>
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <button type="button" onClick={() => onSave(zones)} className="rounded-2xl border border-lime-300/50 bg-lime-400/10 py-3 text-sm font-black text-lime-200">
              💾 GUARDAR PISTA CUSTOM
            </button>
            <button type="button" onClick={() => onProject(zones)} className="rounded-2xl bg-cyan-400 py-3 text-sm font-black text-gray-950 shadow-[0_0_22px_rgba(0,243,255,0.4)]">
              📡 PROYECTAR ESTA PISTA
            </button>
          </div>
        </footer>
      </section>
    </div>
  );
}
