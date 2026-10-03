const GRAY_STEPS = [0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
const COLOR_BARS = [
  { name: "Cian", color: "#00ffff" },
  { name: "Magenta", color: "#ff00ff" },
  { name: "Amarillo", color: "#ffff00" },
  { name: "Rojo", color: "#ff0000" },
  { name: "Verde", color: "#00ff00" },
  { name: "Azul", color: "#0000ff" },
  { name: "Blanco", color: "#ffffff" },
];
const TEXT_SIZES = [12, 16, 24, 32];

/**
 * Carta de ajuste para enfocar el lente y revisar negros, color y refresco sobre el piso.
 */
export function CalibrationPattern() {
  return (
    <div className="absolute inset-0 z-50 overflow-hidden bg-black text-white" aria-label="Patrón de enfoque">
      <style>{`
        @keyframes focus-drift {
          0% { left: 6%; top: 18%; opacity: 1; }
          50% { left: 82%; top: 58%; opacity: 0.3; }
          100% { left: 6%; top: 18%; opacity: 1; }
        }
      `}</style>
      <div className="absolute inset-x-0 top-0 z-10 flex h-[9%]">
        {GRAY_STEPS.map((step) => (
          <div
            key={step}
            className="flex h-full flex-1 items-end justify-center pb-0.5 text-[9px] font-bold"
            style={{
              backgroundColor: `rgb(${Math.round(step * 2.55)} ${Math.round(step * 2.55)} ${Math.round(step * 2.55)})`,
              color: step > 50 ? "#000" : "#fff",
            }}
          >
            {step}%
          </div>
        ))}
      </div>
      <Crosshair className="top-[16%] left-[7%]" />
      <Crosshair className="top-[16%] left-[93%]" />
      <Crosshair className="top-1/2 left-1/2" />
      <Crosshair className="top-[84%] left-[7%]" />
      <Crosshair className="top-[84%] left-[93%]" />
      <div className="absolute inset-x-[8%] top-[30%] z-10 grid gap-1">
        {TEXT_SIZES.map((size) => (
          <p key={size} className="text-center font-semibold leading-tight tracking-wide text-white" style={{ fontSize: size }}>
            {size}px · AJUSTE EL ENFOQUE HASTA PODER LEER ESTA LÍNEA
          </p>
        ))}
      </div>
      <div
        className="absolute z-20 h-8 w-8 rounded-full bg-[#ff00ff] shadow-[0_0_16px_#ff00ff]"
        style={{ animation: "focus-drift 2.4s ease-in-out infinite" }}
      />
      <div className="absolute inset-x-0 bottom-0 z-10 flex h-[12%]">
        {COLOR_BARS.map((bar) => (
          <div
            key={bar.name}
            className="flex h-full flex-1 items-center justify-center text-[10px] font-black"
            style={{
              backgroundColor: bar.color,
              color: bar.name === "Amarillo" || bar.name === "Blanco" || bar.name === "Cian" ? "#000" : "#fff",
            }}
          >
            {bar.name}
          </div>
        ))}
      </div>
    </div>
  );
}

function Crosshair({ className }: { className: string }) {
  return (
    <div className={`absolute z-30 h-16 w-16 -translate-x-1/2 -translate-y-1/2 ${className}`} aria-hidden="true">
      <span className="absolute top-1/2 left-0 h-[2px] w-full -translate-y-1/2 bg-white" />
      <span className="absolute top-0 left-1/2 h-full w-[2px] -translate-x-1/2 bg-white" />
      <span className="absolute top-1/2 left-[10%] h-px w-4/5 -translate-y-[4px] bg-[#39ff14]" />
      <span className="absolute top-[10%] left-1/2 h-4/5 w-px -translate-x-[4px] bg-[#39ff14]" />
      <span className="absolute inset-[22%] rounded-full border border-white" />
    </div>
  );
}
