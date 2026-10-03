"use client";

import { useEffect, useState } from "react";
import { loginWithPin } from "@/lib/adminManager";

type Props = {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
};

function toastFor(role: string) {
  if (role === "ADMIN_VIP") return "👑 MODO VIP ALL-INCLUSIVE ACTIVADO";
  if (role === "DEMO_FREE") return "🧪 MODO GRATIS ACTIVADO";
  return "⚙️ ADMINISTRADOR DE TERRAZA";
}

const KEYS = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "OK"] as const;

/**
 * Teclado de 4 dígitos. La clave correcta abre la sesión de administrador de esta pestaña.
 */
export function AdminAuthModal({ open, onClose, onSuccess }: Props) {
  const [pin, setPin] = useState("");
  const [wrong, setWrong] = useState(false);
  const [toast, setToast] = useState("");

  useEffect(() => {
    if (!open) return;
    setPin("");
    setWrong(false);
    setToast("");
  }, [open]);

  if (!open) return null;

  function reject() {
    setWrong(true);
    setPin("");
    if (typeof navigator !== "undefined" && typeof navigator.vibrate === "function") navigator.vibrate(180);
    window.setTimeout(() => setWrong(false), 450);
  }

  function submit(next: string) {
    const account = loginWithPin(next);
    if (!account) {
      reject();
      return;
    }
    setPin("");
    setToast(toastFor(account.role));
    window.setTimeout(() => onSuccess(), 900);
  }

  function press(key: (typeof KEYS)[number]) {
    if (key === "⌫") {
      setPin((current) => current.slice(0, -1));
      setWrong(false);
      return;
    }
    if (key === "OK") {
      if (pin.length === 4) submit(pin);
      else reject();
      return;
    }
    const next = `${pin}${key}`.slice(0, 4);
    setPin(next);
    setWrong(false);
    if (next.length === 4) submit(next);
  }

  return (
    <div className="fixed inset-0 z-[60] grid place-items-end bg-black/75 p-3 sm:place-items-center" role="dialog" aria-label="PIN de administrador">
      <div className={`w-full max-w-xs rounded-3xl border border-cyan-300/70 bg-gray-950 p-4 shadow-[0_0_28px_rgba(0,243,255,0.35)] ${wrong ? "animate-[admin-shake_0.4s_ease-in-out]" : ""}`}>
        <div className="mb-3 flex items-start justify-between gap-3">
          <div>
            <p className="text-[11px] font-black tracking-[0.22em] text-cyan-200">ACCESO</p>
            <h2 className="text-lg font-black text-cyan-100">PIN DE ADMINISTRADOR</h2>
          </div>
          <button type="button" onClick={onClose} className="rounded-full border border-white/15 px-3 py-1 text-xs text-white/70" aria-label="Cerrar PIN">
            Cerrar
          </button>
        </div>
        <div className="mb-3 flex justify-center gap-3" aria-label="PIN">
          {[0, 1, 2, 3].map((index) => (
            <span
              key={index}
              className={`h-3.5 w-3.5 rounded-full border ${pin.length > index ? "border-cyan-200 bg-cyan-300 shadow-[0_0_10px_rgba(0,243,255,0.8)]" : "border-white/25 bg-transparent"}`}
            />
          ))}
        </div>
        {wrong ? <p className="mb-3 text-center text-sm font-black text-red-300">❌ PIN INCORRECTO</p> : <p className="mb-3 text-center text-[11px] text-white/40">4 dígitos</p>}
        <div className="grid grid-cols-3 gap-2">
          {KEYS.map((key) => (
            <button
              key={key}
              type="button"
              onClick={() => press(key)}
              className="rounded-2xl border border-cyan-300/40 bg-cyan-400/10 py-3 text-lg font-black text-cyan-50 shadow-[0_0_12px_rgba(0,243,255,0.2)]"
            >
              {key}
            </button>
          ))}
        </div>
        <p className="mt-3 text-center text-[10px] leading-relaxed text-white/40">
          PINs de Prueba: 9999 (VIP Todo Incluido) | 1111 (Modo Free con Anuncios)
        </p>
        {toast ? (
          <p className="pointer-events-none fixed inset-x-3 top-4 z-[70] rounded-2xl border border-cyan-200 bg-cyan-300 px-3 py-3 text-center text-sm font-black text-gray-950 shadow-[0_0_24px_rgba(0,243,255,0.75)]">
            {toast}
          </p>
        ) : null}
        <style>{`@keyframes admin-shake { 0%, 100% { transform: translateX(0); } 25% { transform: translateX(-8px); } 75% { transform: translateX(8px); } }`}</style>
      </div>
    </div>
  );
}
