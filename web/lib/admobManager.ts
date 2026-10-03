export const WEB_TEST_AD_MESSAGE =
  "📢 ANUNCIO DE PRUEBA: Hot Wheels Championship 2026 - (Cambia a MODO PRO para quitar)";

/** El navegador no carga el SDK nativo de AdMob. */
export function isBrowserRuntime() {
  return typeof window !== "undefined" && typeof document !== "undefined";
}

/**
 * En esta consola web el anuncio es el banner de prueba.
 * No llama al SDK nativo. El modo PRO lo apaga.
 */
export function webTestAdVisible(isPro: boolean) {
  return !isPro;
}
