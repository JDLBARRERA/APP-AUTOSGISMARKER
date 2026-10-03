"use client";

import { useEffect, useMemo, useState } from "react";
import { audioManager } from "@/lib/audioManager";
import { backupToCloud, collectLocalBackup, restoreFromCloud, type CloudSyncResult } from "@/lib/cloudSync";
import {
  cancelSubscription,
  formatPlanPrice,
  getSubscriptionStatus,
  PRICING_PLANS,
  upgradeToPro,
  type Currency,
  type PlanType,
  type UserProfile,
} from "@/lib/subscriptionManager";

type Props = {
  open: boolean;
  onClose: () => void;
  onChange: (profile: UserProfile) => void;
  allowAdmin?: boolean;
  onAdminAccess?: () => void;
};

function messageFor(result: CloudSyncResult) {
  if (result.ok) return "Respaldo listo.";
  if (result.reason === "plan") return "La nube es parte del plan PRO.";
  if (result.reason === "unconfigured") return "PRO está activo. Falta conectar Supabase para subir el respaldo.";
  return "No se pudo hablar con la nube. Inténtalo de nuevo.";
}

function PlanCard({
  planId,
  currency,
  featured,
}: {
  planId: "FREE" | "PRO_MONTHLY" | "PRO_YEARLY" | "PRO_LIFETIME";
  currency: Currency;
  featured?: boolean;
}) {
  const plan = PRICING_PLANS[planId];
  const period = "period" in plan ? plan.period : "";
  return (
    <article
      className={`rounded-2xl p-3 ${
        featured
          ? "border-2 border-yellow-300 bg-yellow-300/10 shadow-[0_0_28px_rgba(245,217,10,0.45)]"
          : "border border-white/15 bg-white/5"
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <p className={`text-sm font-black ${featured ? "text-yellow-100" : "text-white"}`}>{plan.name}</p>
        <span className={`rounded-full px-2 py-0.5 text-[10px] font-black tracking-wide ${featured ? "bg-yellow-300 text-gray-950" : "bg-white/10 text-white/70"}`}>
          {plan.badge}
        </span>
      </div>
      <p className={`mt-2 text-2xl font-black ${featured ? "text-yellow-300" : "text-white"}`}>
        {formatPlanPrice(planId, currency)}
        {period ? <span className="ml-1 text-xs font-semibold text-white/55">{period}</span> : null}
      </p>
      {"savingsNote" in plan ? <p className="mt-1 text-[11px] font-semibold text-yellow-200">{plan.savingsNote}</p> : null}
      <ul className="mt-2 space-y-1 text-xs text-white/75">
        {plan.features.map((feature) => (
          <li key={feature}>{feature}</li>
        ))}
      </ul>
    </article>
  );
}

function ConfettiBurst() {
  const pieces = useMemo(
    () =>
      Array.from({ length: 42 }, (_, index) => ({
        id: index,
        left: `${(index * 17) % 100}%`,
        delay: `${(index % 7) * 0.04}s`,
        color: ["#ffd91a", "#ff2bd6", "#00f3ff", "#39ff14", "#ffffff"][index % 5],
        drift: `${((index % 5) - 2) * 28}px`,
      })),
    [],
  );
  return (
    <div className="pointer-events-none fixed inset-0 z-[60] overflow-hidden" aria-hidden>
      {pieces.map((piece) => (
        <span
          key={piece.id}
          className="absolute top-0 h-2.5 w-1.5 rounded-sm"
          style={{
            left: piece.left,
            background: piece.color,
            animation: `sandbox-confetti 1.05s ${piece.delay} ease-in forwards`,
            ["--drift" as string]: piece.drift,
          }}
        />
      ))}
      <style>{`@keyframes sandbox-confetti { to { transform: translate(var(--drift), 100vh) rotate(280deg); opacity: 0; } }`}</style>
    </div>
  );
}

/**
 * Vitrina de planes. El pago es local: activa PRO, celebra y cierra, sin cuenta ni tarjeta.
 */
export function SubscriptionModal({ open, onClose, onChange, allowAdmin = false, onAdminAccess }: Props) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [notice, setNotice] = useState("");
  const [currency, setCurrency] = useState<Currency>("MXN");
  const [celebrating, setCelebrating] = useState(false);

  useEffect(() => {
    if (!open) return;
    setProfile(getSubscriptionStatus());
    setNotice("");
    setCelebrating(false);
  }, [open]);

  if (!open || !profile) return null;

  const yearlyLabel = `⚡ SUSCRIBIRME POR ${formatPlanPrice("PRO_YEARLY", currency)}/AÑO`;
  const lifetimeLabel = `👑 COMPRAR PASE VITALICIO POR ${formatPlanPrice("PRO_LIFETIME", currency)}`;
  const monthlyLabel = `Pro mensual · ${formatPlanPrice("PRO_MONTHLY", currency)} / mes`;

  function apply(next: UserProfile, text: string) {
    setProfile(next);
    setNotice(text);
    onChange(next);
  }

  function buy(plan: Exclude<PlanType, "FREE">) {
    const next = upgradeToPro(plan);
    setProfile(next);
    onChange(next);
    setCelebrating(true);
    try {
      audioManager.init();
      audioManager.playVictory();
    } catch {
      /* El plan ya quedó activo aunque el navegador bloquee el audio. */
    }
    window.setTimeout(() => {
      setCelebrating(false);
      onClose();
    }, 1100);
  }

  async function backup() {
    const result = await backupToCloud(collectLocalBackup());
    setNotice(result.ok ? "Pistas, calibración y medallero enviados a la nube." : messageFor(result));
  }

  async function restore() {
    const result = await restoreFromCloud(profile?.userId ?? "");
    setNotice(result.ok ? "Configuración restaurada en este dispositivo. Recarga la página para verla." : messageFor(result));
  }

  return (
    <div className="fixed inset-0 z-50 grid place-items-end bg-black/70 p-3 sm:place-items-center" role="dialog" aria-label="Planes PRO">
      {celebrating ? <ConfettiBurst /> : null}
      <div className="max-h-[92dvh] w-full max-w-md overflow-y-auto rounded-3xl border border-yellow-300/70 bg-gray-950 p-4 shadow-[0_0_36px_rgba(245,217,10,0.35)]">
        <div className="mb-3 flex items-start justify-between gap-3">
          <div>
            <p className="text-[11px] font-black tracking-[0.22em] text-yellow-200">PASE DE CARRERA</p>
            <h2 className="text-xl font-black text-yellow-300">PRO / VIP CLOUD</h2>
          </div>
          <div className="flex shrink-0 rounded-full border border-white/15 p-0.5 text-[10px] font-black">
            <button
              type="button"
              onClick={() => setCurrency("MXN")}
              aria-pressed={currency === "MXN"}
              className={`rounded-full px-2 py-1 ${currency === "MXN" ? "bg-white text-gray-950" : "text-white/60"}`}
            >
              🇲🇽 MXN
            </button>
            <button
              type="button"
              onClick={() => setCurrency("USD")}
              aria-pressed={currency === "USD"}
              className={`rounded-full px-2 py-1 ${currency === "USD" ? "bg-white text-gray-950" : "text-white/60"}`}
            >
              🇺🇸 USD
            </button>
          </div>
        </div>
        <div className="grid gap-3">
          <PlanCard planId="PRO_YEARLY" currency={currency} featured />
          <PlanCard planId="PRO_LIFETIME" currency={currency} featured />
          <PlanCard planId="PRO_MONTHLY" currency={currency} />
          <PlanCard planId="FREE" currency={currency} />
        </div>
        {profile.isPro ? <p className="mt-3 text-center text-sm font-black text-yellow-200">⭐ {PRICING_PLANS[profile.planType].name} activo</p> : null}
        <div className="mt-3 grid gap-2">
          {profile.isPro ? (
            <>
              <button type="button" onClick={() => void backup()} className="rounded-xl bg-yellow-300 py-3 text-sm font-black text-gray-950">
                ☁️ Respaldar ahora
              </button>
              <button type="button" onClick={() => void restore()} className="rounded-xl border border-yellow-300/50 py-3 text-sm font-semibold text-yellow-100">
                Restaurar en este dispositivo
              </button>
              <button
                type="button"
                onClick={() => apply(cancelSubscription(), "Volviste al plan gratis. La publicidad regresa al piso.")}
                className="rounded-xl border border-red-400/50 py-3 text-sm font-semibold text-red-200"
              >
                Cancelar suscripción
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={() => buy("PRO_YEARLY")}
                className="rounded-xl bg-yellow-300 py-3 text-sm font-black text-gray-950 shadow-[0_0_18px_rgba(245,217,10,0.45)]"
              >
                {yearlyLabel}
              </button>
              <button
                type="button"
                onClick={() => buy("PRO_LIFETIME")}
                className="rounded-xl border-2 border-yellow-300 bg-yellow-300/15 py-3 text-sm font-black text-yellow-100"
              >
                {lifetimeLabel}
              </button>
              <button
                type="button"
                onClick={() => buy("PRO_MONTHLY")}
                className="rounded-xl border border-white/15 py-3 text-sm font-semibold text-white/80"
              >
                {monthlyLabel}
              </button>
            </>
          )}
        </div>
        {notice ? <p className="mt-3 text-center text-xs text-white/75">{notice}</p> : null}
        <div className="mt-4 flex items-end justify-between gap-3">
          {!profile.isPro ? (
            <button type="button" onClick={onClose} className="text-left text-[11px] text-white/45 underline-offset-2 hover:underline">
              Continuar con versión gratuita sustentada por anuncios
            </button>
          ) : (
            <span />
          )}
          {allowAdmin && !profile.isPro ? (
            <button type="button" onClick={onAdminAccess} className="shrink-0 text-[11px] font-semibold text-cyan-200/80">
              🔑 Acceso Administrador
            </button>
          ) : null}
        </div>
        <p className="mt-3 text-center text-[10px] text-white/35">Sin tarjeta y sin registro. El plan queda guardado solo en este dispositivo.</p>
      </div>
    </div>
  );
}
