/**
 * Sintetizador de pista. Todo el audio sale de la Web Audio API, sin archivos.
 */

const MUTE_KEY = "autosgismarker-audio-muted";
const VOLUME_KEY = "autosgismarker-audio-volume";

type Tone = OscillatorType;

class AudioManager {
  private context: AudioContext | null = null;
  private master: GainNode | null = null;
  private noise: AudioBuffer | null = null;
  private muted = false;
  private volume = 0.8;
  private urgent = false;
  private pulsing = false;
  private nextBeat = 0;
  private beatIndex = 0;
  private pulseTimer = 0;

  constructor() {
    if (typeof window === "undefined") return;
    this.muted = window.localStorage.getItem(MUTE_KEY) === "1";
    const stored = Number(window.localStorage.getItem(VOLUME_KEY));
    if (Number.isFinite(stored)) this.volume = Math.min(1, Math.max(0, stored));
  }

  /** Desbloquea el audio en el primer toque. */
  init() {
    const context = this.ensure();
    if (!context) return;
    if (context.state === "suspended") void context.resume();
  }

  isMuted() {
    return this.muted;
  }

  /** Devuelve true si el sonido quedó apagado. */
  toggleMute() {
    this.init();
    this.muted = !this.muted;
    this.applyVolume();
    if (typeof window !== "undefined") window.localStorage.setItem(MUTE_KEY, this.muted ? "1" : "0");
    return this.muted;
  }

  setVolume(level: number) {
    this.volume = Math.min(1, Math.max(0, level));
    this.applyVolume();
    if (typeof window !== "undefined") window.localStorage.setItem(VOLUME_KEY, String(this.volume));
  }

  /** 440 Hz en la cuenta. 880 Hz en ¡ARRANCA! */
  playCountdown(isFinal: boolean) {
    if (isFinal) this.tone(880, 0, 0.18, "square", 0.12);
    else this.tone(440, 0, 0.08, "sine", 0.08);
  }

  /** Fanfarria corta de victoria. */
  playVictory() {
    this.tone(523.25, 0, 0.12, "square", 0.08);
    this.tone(659.25, 0.12, 0.12, "square", 0.08);
    this.tone(783.99, 0.24, 0.14, "square", 0.09);
    this.tone(1046.5, 0.4, 0.28, "square", 0.1);
  }

  /** Aceleración de 300 Hz a 1200 Hz. */
  playTurbo() {
    const context = this.ensure();
    const master = this.master;
    if (!context || !master || this.muted) return;
    const start = context.currentTime;
    const tone = context.createOscillator();
    const gain = context.createGain();
    tone.type = "sawtooth";
    tone.frequency.setValueAtTime(300, start);
    tone.frequency.exponentialRampToValueAtTime(1200, start + 0.42);
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(0.1 * this.volume, start + 0.05);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.5);
    tone.connect(gain);
    gain.connect(master);
    tone.start(start);
    tone.stop(start + 0.52);
  }

  /** Modulador en anillo: hielo o lava. */
  playIceLava() {
    const context = this.ensure();
    const master = this.master;
    if (!context || !master || this.muted) return;
    const start = context.currentTime;
    const carrier = context.createOscillator();
    const modulator = context.createOscillator();
    const depth = context.createGain();
    const amp = context.createGain();
    carrier.type = "square";
    carrier.frequency.setValueAtTime(190, start);
    carrier.frequency.linearRampToValueAtTime(70, start + 0.08);
    carrier.frequency.linearRampToValueAtTime(240, start + 0.16);
    carrier.frequency.linearRampToValueAtTime(90, start + 0.36);
    modulator.type = "sine";
    modulator.frequency.value = 28;
    depth.gain.value = 160;
    amp.gain.setValueAtTime(0.0001, start);
    amp.gain.exponentialRampToValueAtTime(0.07 * this.volume, start + 0.02);
    amp.gain.exponentialRampToValueAtTime(0.0001, start + 0.42);
    modulator.connect(depth);
    depth.connect(carrier.frequency);
    carrier.connect(amp);
    amp.connect(master);
    carrier.start(start);
    modulator.start(start);
    carrier.stop(start + 0.44);
    modulator.stop(start + 0.44);
  }

  /** Ruido blanco corto, como un charco. */
  playRain() {
    const context = this.ensure();
    const master = this.master;
    if (!context || !master || this.muted) return;
    const source = context.createBufferSource();
    const filter = context.createBiquadFilter();
    const gain = context.createGain();
    source.buffer = this.noiseBuffer(context);
    filter.type = "bandpass";
    filter.frequency.setValueAtTime(1400, context.currentTime);
    filter.frequency.exponentialRampToValueAtTime(500, context.currentTime + 0.28);
    filter.Q.value = 0.7;
    const start = context.currentTime;
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(0.16 * this.volume, start + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.32);
    source.connect(filter);
    filter.connect(gain);
    gain.connect(master);
    source.start(start);
    source.stop(start + 0.34);
  }

  /** Caída de 400 Hz a 100 Hz. */
  playPitFoso() {
    const context = this.ensure();
    const master = this.master;
    if (!context || !master || this.muted) return;
    const start = context.currentTime;
    const tone = context.createOscillator();
    const gain = context.createGain();
    tone.type = "triangle";
    tone.frequency.setValueAtTime(400, start);
    tone.frequency.exponentialRampToValueAtTime(100, start + 0.46);
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(0.12 * this.volume, start + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.5);
    tone.connect(gain);
    gain.connect(master);
    tone.start(start);
    tone.stop(start + 0.52);
  }

  /** Sirena de tres tonos para la recta final. */
  playFinalStretchSiren() {
    const pitches = [520, 780, 640, 900, 520, 780];
    pitches.forEach((frequency, index) => this.tone(frequency, index * 0.16, 0.15, "sawtooth", 0.07));
  }

  /** Cuatro notas neón ascendentes. */
  playPowerUp() {
    [523, 659, 784, 1046].forEach((frequency, index) => this.tone(frequency, index * 0.09, 0.12, "square", 0.07));
  }

  /** Impacto grave al cerrar el tiempo. */
  playCrash() {
    this.tone(220, 0, 0.16, "sawtooth", 0.08);
    this.tone(140, 0.14, 0.22, "square", 0.07);
    this.tone(90, 0.28, 0.28, "sawtooth", 0.06);
  }

  /** Por debajo de 30 s el bajo pasa de 120 a 145 BPM. */
  setTension(urgent: boolean) {
    this.urgent = urgent;
  }

  startPulse() {
    if (this.pulsing) return;
    const context = this.ensure();
    if (!context) return;
    this.pulsing = true;
    this.nextBeat = context.currentTime + 0.05;
    this.beatIndex = 0;
    this.schedulePulse();
  }

  stopPulse() {
    this.pulsing = false;
    window.clearTimeout(this.pulseTimer);
  }

  private schedulePulse() {
    if (!this.pulsing) return;
    const context = this.context;
    const master = this.master;
    if (!context || !master) return;
    const step = 60 / (this.urgent ? 145 : 120);
    const pitches = [55, 55, 82.41, 55];
    while (this.nextBeat < context.currentTime + 0.25) {
      if (!this.muted) this.bass(this.nextBeat, pitches[this.beatIndex % pitches.length], master, context);
      this.beatIndex += 1;
      this.nextBeat += step;
    }
    this.pulseTimer = window.setTimeout(() => this.schedulePulse(), 80);
  }

  private bass(when: number, frequency: number, master: GainNode, context: AudioContext) {
    const tone = context.createOscillator();
    const gain = context.createGain();
    tone.type = "sawtooth";
    tone.frequency.setValueAtTime(frequency, when);
    gain.gain.setValueAtTime(0.0001, when);
    gain.gain.exponentialRampToValueAtTime(0.045 * this.volume, when + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, when + 0.18);
    const filter = context.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.value = 240;
    tone.connect(filter);
    filter.connect(gain);
    gain.connect(master);
    tone.start(when);
    tone.stop(when + 0.2);
  }

  private tone(frequency: number, offset: number, duration: number, type: Tone, peak: number) {
    const context = this.ensure();
    const master = this.master;
    if (!context || !master || this.muted || peak <= 0 || this.volume <= 0) return;
    const start = context.currentTime + offset;
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.type = type;
    oscillator.frequency.value = frequency;
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(peak * this.volume, start + 0.012);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
    oscillator.connect(gain);
    gain.connect(master);
    oscillator.start(start);
    oscillator.stop(start + duration + 0.02);
  }

  private ensure() {
    if (typeof window === "undefined") return null;
    const Context = window.AudioContext || (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Context) return null;
    if (!this.context || this.context.state === "closed") {
      this.context = new Context();
      this.master = this.context.createGain();
      this.master.connect(this.context.destination);
      this.noise = null;
      this.applyVolume();
    }
    return this.context;
  }

  private applyVolume() {
    if (!this.master || !this.context) return;
    this.master.gain.setValueAtTime(this.muted ? 0 : 1, this.context.currentTime);
  }

  private noiseBuffer(context: AudioContext) {
    if (this.noise) return this.noise;
    const buffer = context.createBuffer(1, context.sampleRate, context.sampleRate);
    const data = buffer.getChannelData(0);
    for (let index = 0; index < data.length; index += 1) data[index] = Math.random() * 2 - 1;
    this.noise = buffer;
    return buffer;
  }
}

export const audioManager = new AudioManager();

/** Doble aviso de salida. El tono agudo es ¡ARRANCA! */
export function playTurnStartSound() {
  audioManager.playCountdown(true);
}

/** Pulso de la cuenta regresiva. */
export function playShotCountdown() {
  audioManager.playCountdown(false);
}

/** Alarma grave cuando el tiempo cierra el tiro. */
export function playCrash() {
  audioManager.playCrash();
}

/** Caída de foso cuando el tiempo pasa el turno. */
export function playTrap() {
  audioManager.playPitFoso();
}

/** Sirena de la recta final. */
export function playFinalStretchSound() {
  audioManager.playFinalStretchSiren();
}
