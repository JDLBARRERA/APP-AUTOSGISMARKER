export function formatClock(ms: number) {
  const total = Math.max(0, Math.ceil(ms / 1000));
  const minutes = Math.floor(total / 60);
  const seconds = total % 60;
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

export function urgency(remainingMs: number, budgetMs: number) {
  if (budgetMs <= 0) return "ok" as const;
  const ratio = remainingMs / budgetMs;
  if (ratio <= 0.2) return "danger" as const;
  if (ratio <= 0.5) return "warn" as const;
  return "ok" as const;
}

export function playTimeAlarm() {
  const Context = window.AudioContext || (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
  if (!Context) return;
  const context = new Context();
  const start = context.currentTime;
  [0, 0.18, 0.36].forEach((offset, index) => {
    const tone = context.createOscillator();
    const gain = context.createGain();
    tone.type = "square";
    tone.frequency.value = index === 2 ? 880 : 640;
    gain.gain.setValueAtTime(0.0001, start + offset);
    gain.gain.exponentialRampToValueAtTime(0.07, start + offset + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + offset + 0.15);
    tone.connect(gain);
    gain.connect(context.destination);
    tone.start(start + offset);
    tone.stop(start + offset + 0.16);
  });
  window.setTimeout(() => void context.close(), 900);
}
