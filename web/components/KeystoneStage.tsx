"use client";

import { useEffect, useRef, useState, type PointerEvent as ReactPointerEvent, type ReactNode } from "react";
import { IDENTITY_CORNERS, KEYSTONE_KEY, keystoneMatrix, readCorners, toCornerPoints, type Corner } from "@/lib/keystone";

/**
 * Deforma el cuadro de la pista con las cuatro esquinas para corregir el trapecio de la pared.
 */
export function KeystoneStage({ children }: { children: ReactNode }) {
  const frame = useRef<HTMLDivElement>(null);
  const drag = useRef<number | null>(null);
  const [corners, setCorners] = useState<Corner[]>(IDENTITY_CORNERS);
  const [open, setOpen] = useState(false);
  const [saved, setSaved] = useState(false);
  const [size, setSize] = useState({ width: 0, height: 0 });

  useEffect(() => {
    setCorners(readCorners(window.localStorage.getItem(KEYSTONE_KEY)));
    function onStorage(event: StorageEvent) {
      if (event.key !== KEYSTONE_KEY) return;
      setCorners(readCorners(event.newValue));
    }
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  useEffect(() => {
    const node = frame.current;
    if (!node) return;
    const measure = () => {
      const rect = node.getBoundingClientRect();
      setSize({ width: rect.width, height: rect.height });
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      const target = event.target;
      if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement) return;
      if (event.key === "k" || event.key === "K") {
        if (event.metaKey || event.ctrlKey || event.altKey) return;
        event.preventDefault();
        setOpen((current) => !current);
        setSaved(false);
      }
      if (event.key === "Escape") setOpen(false);
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  function moveCorner(index: number, event: ReactPointerEvent) {
    const node = frame.current;
    if (!node || drag.current !== index) return;
    const rect = node.getBoundingClientRect();
    if (rect.width < 2 || rect.height < 2) return;
    const x = Math.min(1.5, Math.max(-0.5, (event.clientX - rect.left) / rect.width));
    const y = Math.min(1.5, Math.max(-0.5, (event.clientY - rect.top) / rect.height));
    setCorners((current) => current.map((corner, cornerIndex) => (cornerIndex === index ? { x, y } : corner)));
    setSaved(false);
  }

  function save() {
    window.localStorage.setItem(KEYSTONE_KEY, JSON.stringify({ points: toCornerPoints(corners) }));
    setSaved(true);
  }

  const matrix = keystoneMatrix(corners, size.width, size.height);

  return (
    <div ref={frame} className="relative aspect-square w-[min(100%,72dvh)] overflow-visible">
      <div className="h-full w-full" style={{ transform: matrix, transformOrigin: "0 0" }}>
        {children}
        {open ? <CalibrationGrid /> : null}
      </div>
      {open
        ? corners.map((corner, index) => (
            <button
              key={index}
              type="button"
              aria-label={`Esquina ${index + 1}`}
              onPointerDown={(event) => {
                drag.current = index;
                event.currentTarget.setPointerCapture(event.pointerId);
              }}
              onPointerMove={(event) => moveCorner(index, event)}
              onPointerUp={() => {
                drag.current = null;
              }}
              className="absolute z-30 h-8 w-8 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-lime-300 bg-black/70 shadow-[0_0_16px_rgba(57,255,20,0.8)]"
              style={{ left: `${corner.x * 100}%`, top: `${corner.y * 100}%` }}
            />
          ))
        : null}
      {open ? (
        <div className="absolute top-2 left-1/2 z-30 flex w-[min(100%,20rem)] -translate-x-1/2 flex-col items-center gap-2">
          <p className="rounded-full bg-black/80 px-3 py-1 text-center text-[11px] text-lime-200">
            Mueve las esquinas hasta que la cuadrícula se vea cuadrada en el piso.
          </p>
          <button
            type="button"
            onClick={save}
            className="rounded-xl bg-lime-400 px-4 py-2 text-xs font-black tracking-wide text-gray-950"
          >
            {saved ? "CALIBRACIÓN GUARDADA" : "GUARDAR CALIBRACIÓN"}
          </button>
        </div>
      ) : null}
    </div>
  );
}

function CalibrationGrid() {
  const lines = Array.from({ length: 11 }, (_, index) => index);
  return (
    <svg className="pointer-events-none absolute inset-0 h-full w-full" viewBox="0 0 10 10" aria-hidden="true">
      {lines.map((line) => (
        <g key={line}>
          <line x1={line} y1="0" x2={line} y2="10" stroke="#39ff14" strokeWidth="0.03" />
          <line x1="0" y1={line} x2="10" y2={line} stroke="#39ff14" strokeWidth="0.03" />
        </g>
      ))}
    </svg>
  );
}
