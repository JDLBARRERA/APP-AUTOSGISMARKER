import { cancelSubscription, upgradeToPro } from "@/lib/subscriptionManager";

const PIN_KEY = "autosgismarker-admin-pin";
const SESSION_KEY = "autosgismarker-admin-session";
const DEFAULT_PIN = "1234";

export const TEST_ACCOUNTS = {
  VIP_ALL_INCLUSIVE: {
    pin: "9999",
    role: "ADMIN_VIP",
    label: "👑 VIP All-Inclusive (Dev Master)",
    isPro: true,
    cloudSync: true,
    adsEnabled: false,
    maxTracksPerModule: 50,
  },
  FREE_TESTER: {
    pin: "1111",
    role: "DEMO_FREE",
    label: "🧪 Modo Gratis (Amateur Demo)",
    isPro: false,
    cloudSync: false,
    adsEnabled: true,
    maxTracksPerModule: 15,
  },
  STANDARD_ADMIN: {
    pin: "1234",
    role: "ADMIN_SETTINGS",
    label: "⚙️ Administrador de Terraza",
  },
} as const;

export type TestAccount = (typeof TEST_ACCOUNTS)[keyof typeof TEST_ACCOUNTS];

/** Tope de pistas clásicas y de rally según el plan activo. */
export function catalogLimit(isPro: boolean) {
  return isPro ? TEST_ACCOUNTS.VIP_ALL_INCLUSIVE.maxTracksPerModule : TEST_ACCOUNTS.FREE_TESTER.maxTracksPerModule;
}

/** Lugar de una pista clásica en el catálogo: 50 por formato, 10 por módulo. */
export function classicSlot(format: number, moduleIndex: number, raceIndex: number) {
  return format * 50 + moduleIndex * 10 + raceIndex;
}

const HISTORY_KEY = "phygital-history";
const RECORDS_KEY = "phygital-records";
const TOURNAMENT_KEY = "autosgismarker-tournament";

function readPin() {
  if (typeof window === "undefined") return DEFAULT_PIN;
  const stored = window.localStorage.getItem(PIN_KEY);
  if (!stored) {
    window.localStorage.setItem(PIN_KEY, DEFAULT_PIN);
    return DEFAULT_PIN;
  }
  return stored;
}

/** Compara el PIN con la clave guardada en este dispositivo. */
export function verifyAdminPin(inputPin: string) {
  return inputPin === readPin();
}

/** Cambia la clave si el PIN actual coincide. La nueva clave son 4 dígitos. */
export function updateAdminPin(oldPin: string, newPin: string) {
  if (!verifyAdminPin(oldPin)) return false;
  const next = newPin.trim();
  if (!/^\d{4}$/.test(next)) return false;
  window.localStorage.setItem(PIN_KEY, next);
  return true;
}

/** La sesión de administrador dura mientras la pestaña siga abierta. */
export function isAdminAuthenticated() {
  if (typeof window === "undefined") return false;
  return window.sessionStorage.getItem(SESSION_KEY) === "1";
}

function openSession() {
  window.sessionStorage.setItem(SESSION_KEY, "1");
}

/**
 * Entra con un PIN de prueba o con la clave de administrador.
 * 9999 activa el pase vitalicio. 1111 fuerza la demo gratis. La clave de terraza no cambia el plan.
 */
export function loginWithPin(pin: string): TestAccount | null {
  if (typeof window === "undefined") return null;
  if (pin === TEST_ACCOUNTS.VIP_ALL_INCLUSIVE.pin) {
    upgradeToPro("PRO_LIFETIME");
    openSession();
    return TEST_ACCOUNTS.VIP_ALL_INCLUSIVE;
  }
  if (pin === TEST_ACCOUNTS.FREE_TESTER.pin) {
    cancelSubscription();
    openSession();
    return TEST_ACCOUNTS.FREE_TESTER;
  }
  if (pin === readPin()) {
    openSession();
    return TEST_ACCOUNTS.STANDARD_ADMIN;
  }
  return null;
}

/** Abre la sesión solo si el PIN de terraza es correcto. */
export function startAdminSession(inputPin: string) {
  return loginWithPin(inputPin)?.role === "ADMIN_SETTINGS";
}

/** Bloquea de nuevo los ajustes protegidos. */
export function logoutAdmin() {
  if (typeof window === "undefined") return;
  window.sessionStorage.removeItem(SESSION_KEY);
}

/** Borra medallero, récords y la tabla del torneo en este dispositivo. */
export function clearMedalBoard() {
  if (!isAdminAuthenticated() || typeof window === "undefined") return false;
  window.localStorage.removeItem(HISTORY_KEY);
  window.localStorage.removeItem(RECORDS_KEY);
  window.localStorage.removeItem(TOURNAMENT_KEY);
  return true;
}
