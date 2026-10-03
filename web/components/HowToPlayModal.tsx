"use client";

import { useEffect, useState } from "react";

type Props = {
  open: boolean;
  onClose: () => void;
};

const ZONES = [
  { icon: "🚀", name: "TURBO", tone: "Verde", detail: "Si te detienes en la casilla Turbo, ganas 1 tiro extra de inmediato en ese mismo turno.", className: "border-lime-400/70 bg-lime-400/15 text-lime-200" },
  { icon: "🚀", name: "TIRO LARGO", tone: "Verde lima", detail: "Si tu turno se acaba sobre la flecha verde, tu siguiente turno tendrá 4 tiros en lugar de 3.", className: "border-lime-300/70 bg-lime-300/10 text-lime-100" },
  { icon: "🎯", name: "TIRO EXACTO", tone: "Amarillo", detail: "Clavas la casilla y continúas si te quedan tiros, sin penalización.", className: "border-yellow-300/70 bg-yellow-300/15 text-yellow-100" },
  { icon: "⚡", name: "TRAMPA DE TENSIÓN", tone: "Púrpura", detail: "Tramo que debes cruzar en el número de tiros marcados (1, 2 o 3). Si fallas, regresas al inicio del tramo.", className: "border-violet-400/70 bg-indigo-500/20 text-violet-100" },
  { icon: "💥", name: "FOSO", tone: "Rojo / Marrón", detail: "Si caes aquí, pierdes el turno siguiente por completo.", className: "border-red-500/70 bg-red-950/80 text-red-100" },
  { icon: "🌋", name: "LAVA Y HIELO", tone: "Naranja / Cian", detail: "El siguiente turno lo tiras con tu mano no dominante (la mano con la que no escribes).", className: "border-orange-400/70 bg-orange-500/15 text-orange-100" },
  { icon: "🌧️", name: "LLUVIA", tone: "Azul", detail: "Mientras estés en el tramo azul, tu turno se reduce a 1 solo tiro.", className: "border-cyan-300/70 bg-cyan-400/15 text-cyan-100" },
] as const;

const CONTACTS = [
  { icon: "🚗", name: "TRÁFICO", detail: "Carritos pegados: gastas 1 tiro solo para despegarte, sin mover al rival.", className: "border-white/20 bg-white/10 text-white" },
  { icon: "🥊", name: "TAKEDOWN", detail: "Sacar al rival del gis le hace perder el 1° tiro de su siguiente turno.", className: "border-red-400/60 bg-red-500/15 text-red-100" },
  { icon: "💨", name: "REBUFO", detail: "Quedar a 2 cm o menos detrás del rival, sin tocarlo, otorga +1 tiro extra.", className: "border-cyan-300/60 bg-cyan-400/10 text-cyan-100" },
] as const;

const SHOTS = [
  "Colócate detrás de tu carrito físico.",
  "Tienes 2 minutos en el reloj para pensar y lanzar.",
  "Das tu impulso con el dedo.",
  "El Árbitro presiona el botón gigante ¡YA TIRÉ!.",
];

/**
 * Manual visual de 5 pasos para explicar la carrera en medio minuto.
 */
export function HowToPlayModal({ open, onClose }: Props) {
  const [step, setStep] = useState(0);

  useEffect(() => {
    if (open) setStep(0);
  }, [open]);

  useEffect(() => {
    if (!open) return;
    function onKey(event: KeyboardEvent) {
      if (event.key === "Escape") onClose();
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  const last = step === 4;

  return (
    <div className="fixed inset-0 z-50 grid place-items-end bg-black/75 p-3 sm:place-items-center" onClick={onClose}>
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="pilot-manual-title"
        onClick={(event) => event.stopPropagation()}
        className="flex max-h-[92dvh] w-full max-w-md flex-col overflow-hidden rounded-3xl border border-cyan-300/40 bg-gray-950 shadow-[0_0_40px_rgba(0,243,255,0.28)]"
      >
        <header className="flex shrink-0 items-center justify-between gap-3 border-b border-white/10 px-4 py-3">
          <p id="pilot-manual-title" className="text-xs font-black tracking-[0.12em] text-cyan-100">
            📖 MANUAL DE PILOTO · PASO {step + 1} DE 5
          </p>
          <button type="button" onClick={onClose} className="rounded-full border border-white/15 px-2.5 py-1 text-xs font-black text-white/70" aria-label="Cerrar manual">
            ✖
          </button>
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-5">
          {step === 0 ? (
            <article className="rounded-3xl border border-cyan-300/40 bg-cyan-400/10 p-5 text-center">
              <p className="text-6xl" aria-hidden="true">🖍️</p>
              <h2 className="mt-3 text-2xl font-black text-cyan-100">DIBUJA LA PISTA</h2>
              <p className="mt-1 text-xs font-semibold tracking-[0.16em] text-cyan-200/70">EL MAPA DE LUZ</p>
              <p className="mt-4 text-base leading-relaxed text-white/90">
                El proyector dibuja la pista de luz sobre tu tapete o piso. ¡Toma tus gises neón y calca las líneas brillantes para darle vida a la pista!
              </p>
            </article>
          ) : null}

          {step === 1 ? (
            <article className="rounded-3xl border border-fuchsia-400/40 bg-fuchsia-400/10 p-5 text-center">
              <p className="text-6xl" aria-hidden="true">🏎️</p>
              <h2 className="mt-3 text-2xl font-black text-fuchsia-100">ALINÉATE EN META</h2>
              <p className="mt-4 text-base leading-relaxed text-white/90">
                Coloca tu carrito físico en la línea de salida. Revisa la pantalla de tu teléfono: cuando veas tu nombre en grande, ¡es tu turno de correr!
              </p>
            </article>
          ) : null}

          {step === 2 ? (
            <article className="rounded-3xl border border-amber-300/40 bg-amber-300/10 p-5">
              <p className="text-center text-6xl" aria-hidden="true">⏱️</p>
              <h2 className="mt-3 text-center text-2xl font-black text-amber-100">3 TIROS Y 2 MINUTOS</h2>
              <ol className="mt-4 space-y-2">
                {SHOTS.map((line, index) => (
                  <li key={line} className="flex items-start gap-3 rounded-2xl border border-white/10 bg-black/30 px-3 py-2.5">
                    <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-amber-300 text-sm font-black text-gray-950">{index + 1}</span>
                    <span className="pt-1 text-sm leading-snug text-white/90">{line}</span>
                  </li>
                ))}
              </ol>
            </article>
          ) : null}

          {step === 3 ? (
            <article>
              <h2 className="text-center text-2xl font-black text-white">⚡ ZONAS Y PODERES</h2>
              <ul className="zone-scroll mt-4 max-h-[60vh] space-y-2 overflow-y-auto overscroll-contain pr-2">
                {ZONES.map((zone) => (
                  <li key={zone.name} className={`rounded-2xl border px-3 py-3 ${zone.className}`}>
                    <p className="flex items-center gap-2 text-lg font-black">
                      <span className="text-3xl" aria-hidden="true">{zone.icon}</span>
                      <span className="min-w-0">{zone.name}</span>
                      <span className="ml-auto shrink-0 text-[10px] tracking-[0.14em]">{zone.tone}</span>
                    </p>
                    <p className="mt-1 text-sm leading-snug text-white/90">{zone.detail}</p>
                  </li>
                ))}
              </ul>
            </article>
          ) : null}

          {step === 4 ? (
            <article>
              <h2 className="text-center text-2xl font-black text-white">🚗 REGLAS DE CONTACTO</h2>
              <ul className="mt-4 space-y-2">
                {CONTACTS.map((rule) => (
                  <li key={rule.name} className={`rounded-2xl border px-3 py-3 ${rule.className}`}>
                    <p className="flex items-center gap-2 text-lg font-black">
                      <span className="text-3xl" aria-hidden="true">{rule.icon}</span>
                      <span>{rule.name}</span>
                    </p>
                    <p className="mt-1 text-sm leading-snug text-white/90">{rule.detail}</p>
                  </li>
                ))}
              </ul>
              <button
                type="button"
                onClick={onClose}
                className="mt-4 w-full animate-pulse rounded-2xl bg-lime-400 py-4 text-base font-black tracking-wide text-gray-950 shadow-[0_0_24px_rgba(57,255,20,0.55)]"
              >
                🚀 ¡ENTENDIDO, A CORRER!
              </button>
            </article>
          ) : null}
        </div>

        <footer className="grid shrink-0 grid-cols-[1fr_auto_1fr] items-center gap-2 border-t border-white/10 px-3 py-3">
          <button
            type="button"
            onClick={() => setStep((current) => Math.max(0, current - 1))}
            disabled={step === 0}
            className="rounded-xl border border-white/15 px-2 py-2 text-[11px] font-black tracking-wide text-white/80 disabled:opacity-30"
          >
            ◀ ANTERIOR
          </button>
          <div className="flex items-center gap-1.5" aria-label={`Paso ${step + 1} de 5`}>
            {[0, 1, 2, 3, 4].map((index) => (
              <button
                key={index}
                type="button"
                onClick={() => setStep(index)}
                aria-label={`Ir al paso ${index + 1}`}
                aria-current={index === step ? "step" : undefined}
                className={`h-2.5 w-2.5 rounded-full ${index === step ? "bg-cyan-300 shadow-[0_0_10px_rgba(0,243,255,0.9)]" : "bg-white/25"}`}
              />
            ))}
          </div>
          <button
            type="button"
            onClick={() => setStep((current) => Math.min(4, current + 1))}
            disabled={last}
            className="rounded-xl border border-cyan-300/50 bg-cyan-400/15 px-2 py-2 text-[11px] font-black tracking-wide text-cyan-100 disabled:opacity-30"
          >
            SIGUIENTE ▶
          </button>
        </footer>
      </section>
    </div>
  );
}
