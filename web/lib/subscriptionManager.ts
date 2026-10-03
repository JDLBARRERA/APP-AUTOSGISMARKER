export type PlanType = "FREE" | "PRO_MONTHLY" | "PRO_YEARLY" | "PRO_LIFETIME";

export type Currency = "MXN" | "USD";

export const PRICING_PLANS = {
  FREE: {
    id: "FREE",
    name: "Piloto Amateur",
    priceMXN: 0,
    priceUSD: 0,
    badge: "GRATIS",
    features: [
      "50 Pistas de Rally y Circuitos Base",
      "Calibración 3D para proyector en pared",
      "Consola de Árbitro y Reglas Físicas",
      "Vallas publicitarias proyectadas en pista",
    ],
  },
  PRO_MONTHLY: {
    id: "PRO_MONTHLY",
    name: "Pro Mensual",
    priceMXN: 89,
    priceUSD: 4.99,
    period: "/ mes",
    badge: "POPULAR",
    features: [
      "🚫 Cero Anuncios en piso y celular",
      "☁️ Respaldo ilimitado en la Nube",
      "🏆 Medallero de Torneo Global",
      "✨ Efectos Neón VIP en la terraza",
    ],
  },
  PRO_YEARLY: {
    id: "PRO_YEARLY",
    name: "Pro Anual",
    priceMXN: 549,
    priceUSD: 29.99,
    period: "/ año",
    badge: "50% AHORRO",
    savingsNote: "Equivalente a $45 MXN/mes",
    features: [
      "Todo lo incluido en el Plan Pro",
      "Acceso prioritario a nuevas pistas",
      "Sincronización instantánea entre proyectores",
      "2 meses gratis en pago anual",
    ],
  },
  PRO_LIFETIME: {
    id: "PRO_LIFETIME",
    name: "Pase Fundador (De por Vida)",
    priceMXN: 899,
    priceUSD: 49.99,
    period: "pago único",
    badge: "EDICIÓN LIMITADA",
    features: [
      "👑 Acceso PRO Vitalicio Sin Mensualidades",
      "☁️ Servidor de Nube reservado de por vida",
      "🎨 Creador de Pistas Avanzado desbloqueado",
      "Soporte directo para salones de eventos y casas",
    ],
  },
} as const;

/** Precio listo para el botón de checkout, en pesos o dólares. */
export function formatPlanPrice(planId: keyof typeof PRICING_PLANS, currency: Currency) {
  const plan = PRICING_PLANS[planId];
  if (currency === "USD") return `$${plan.priceUSD.toFixed(2)} USD`;
  return `$${plan.priceMXN} MXN`;
}

export interface UserProfile {
  isPro: boolean;
  planType: PlanType;
  userId: string;
  email: string;
  cloudSyncEnabled: boolean;
}

const STORAGE_KEY = "autosgismarker-subscription";
const PRO_FLAG_KEY = "autosgismarker-is-pro";

/** En modo gratis el rally deja jugar las primeras 15 pistas. De la 16 a la 50 hace falta PRO. */
export const FREE_TRACK_LIMIT = 15;

const FREE_PROFILE: UserProfile = {
  isPro: false,
  planType: "FREE",
  userId: "",
  email: "",
  cloudSyncEnabled: false,
};

function readIsPro() {
  if (typeof window === "undefined") return false;
  return window.localStorage.getItem(PRO_FLAG_KEY) === "1";
}

function writeIsPro(isPro: boolean) {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(PRO_FLAG_KEY, isPro ? "1" : "0");
}

function readPlan(isPro: boolean): PlanType {
  if (!isPro || typeof window === "undefined") return "FREE";
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return "PRO_YEARLY";
    const parsed = JSON.parse(raw) as Partial<UserProfile>;
    if (parsed.planType === "PRO_MONTHLY" || parsed.planType === "PRO_YEARLY" || parsed.planType === "PRO_LIFETIME") {
      return parsed.planType;
    }
  } catch {
    return "PRO_YEARLY";
  }
  return "PRO_YEARLY";
}

function readProfile(): UserProfile {
  const isPro = readIsPro();
  return {
    ...FREE_PROFILE,
    isPro,
    planType: readPlan(isPro),
    cloudSyncEnabled: isPro,
  };
}

function writeProfile(profile: UserProfile) {
  if (typeof window === "undefined") return;
  writeIsPro(profile.isPro);
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...FREE_PROFILE, ...profile, userId: "", email: "" }));
}

/** Lee el plan guardado en este dispositivo. */
export function getSubscriptionStatus(): UserProfile {
  return readProfile();
}

/**
 * Activa PRO en este dispositivo. No pide cuenta ni tarjeta: solo guarda la bandera local.
 */
export function upgradeToPro(plan: "PRO_MONTHLY" | "PRO_YEARLY" | "PRO_LIFETIME"): UserProfile {
  const next: UserProfile = {
    ...FREE_PROFILE,
    isPro: true,
    planType: plan,
    cloudSyncEnabled: true,
  };
  writeProfile(next);
  return next;
}

/** Alterna al instante entre modo gratis y modo PRO de prueba. */
export function toggleTestProMode(): UserProfile {
  const next = readIsPro()
    ? { ...FREE_PROFILE }
    : { ...FREE_PROFILE, isPro: true, planType: "PRO_YEARLY" as const, cloudSyncEnabled: true };
  writeProfile(next);
  return next;
}

/** Vuelve el dispositivo al plan gratuito. */
export function cancelSubscription(): UserProfile {
  writeProfile({ ...FREE_PROFILE });
  return { ...FREE_PROFILE };
}
