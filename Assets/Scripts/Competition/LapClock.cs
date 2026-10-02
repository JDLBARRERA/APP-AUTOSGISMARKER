using System;
using System.Diagnostics;

namespace ARTrackBuilder.Competition
{
    /// <summary>
    /// Estado de un carril después de cada paso por meta.
    /// </summary>
    public struct LaneSnapshot
    {
        public int Lane;
        public int Laps;
        public long LastLapTicks;
        public long BestTicks;
        public long TotalTicks;
        public bool Running;
        public bool Finished;
    }

    /// <summary>
    /// Cuatro cronómetros de vuelta. El primer pulso de un carril solo arranca; el siguiente cierra la vuelta.
    /// </summary>
    public sealed class LapClock
    {
        public const int LaneCount = 4;

        private readonly Stopwatch[] _watches = new Stopwatch[LaneCount];
        private readonly long[] _splitTicks = new long[LaneCount];
        private readonly int[] _laps = new int[LaneCount];
        private readonly long[] _lastLapTicks = new long[LaneCount];
        private readonly long[] _bestTicks = new long[LaneCount];
        private readonly bool[] _running = new bool[LaneCount];
        private readonly bool[] _finished = new bool[LaneCount];

        public LapClock()
        {
            for (int i = 0; i < LaneCount; i++)
            {
                _watches[i] = new Stopwatch();
            }
        }

        /// <summary>
        /// Deja todos los carriles en cero.
        /// </summary>
        public void ResetAll()
        {
            for (int i = 0; i < LaneCount; i++)
            {
                _watches[i].Reset();
                _splitTicks[i] = 0;
                _laps[i] = 0;
                _lastLapTicks[i] = 0;
                _bestTicks[i] = 0;
                _running[i] = false;
                _finished[i] = false;
            }
        }

        /// <summary>
        /// Registra un paso por meta. Devuelve false si ese pulso solo puso el reloj en marcha.
        /// </summary>
        public bool TryCompleteLap(int lane, out long lapTicks)
        {
            lapTicks = 0;
            if (lane < 0 || lane >= LaneCount || _finished[lane])
            {
                return false;
            }

            if (!_running[lane])
            {
                _watches[lane].Restart();
                _running[lane] = true;
                _splitTicks[lane] = 0;
                return false;
            }

            long elapsed = _watches[lane].ElapsedTicks;
            lapTicks = elapsed - _splitTicks[lane];
            if (lapTicks < 0)
            {
                lapTicks = 0;
            }

            _splitTicks[lane] = elapsed;
            _lastLapTicks[lane] = lapTicks;
            _laps[lane]++;
            if (_bestTicks[lane] == 0 || lapTicks < _bestTicks[lane])
            {
                _bestTicks[lane] = lapTicks;
            }

            return true;
        }

        /// <summary>
        /// Tiempo transcurrido del carril, incluida la vuelta que todavía no cierra.
        /// </summary>
        public long LiveTicks(int lane)
        {
            if (lane < 0 || lane >= LaneCount || !_running[lane])
            {
                return 0;
            }

            return _watches[lane].ElapsedTicks;
        }

        public LaneSnapshot Snapshot(int lane)
        {
            return new LaneSnapshot
            {
                Lane = lane,
                Laps = _laps[lane],
                LastLapTicks = _lastLapTicks[lane],
                BestTicks = _bestTicks[lane],
                TotalTicks = _splitTicks[lane],
                Running = _running[lane],
                Finished = _finished[lane]
            };
        }

        /// <summary>
        /// Para el carril y deja fijo el tiempo que llevaba.
        /// </summary>
        public void Finish(int lane)
        {
            if (lane < 0 || lane >= LaneCount || _finished[lane])
            {
                return;
            }

            if (_running[lane])
            {
                _watches[lane].Stop();
            }

            _finished[lane] = true;
            _running[lane] = false;
        }

        public void StopAll()
        {
            for (int i = 0; i < LaneCount; i++)
            {
                Finish(i);
            }
        }

        /// <summary>
        /// Presenta ticks como m:ss.fff.
        /// </summary>
        public static string Format(long ticks)
        {
            if (ticks <= 0)
            {
                return "--:--.---";
            }

            TimeSpan span = TimeSpan.FromTicks(ticks);
            int minutes = (int)span.TotalMinutes;
            return minutes + ":" + span.Seconds.ToString("00") + "." + span.Milliseconds.ToString("000");
        }
    }
}
