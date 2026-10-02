using System;
using UnityEngine;

namespace ARTrackBuilder.AR
{
    public enum LandscapeMode
    {
        Asphalt,
        Rain,
        Ice,
        Snow,
        Lava,
        Desert,
        Forest,
        Night
    }

    /// <summary>
    /// Colores que el proyector tira al piso. El suelo es el paisaje y la cinta es el gis.
    /// </summary>
    public readonly struct LandscapePalette
    {
        public readonly LandscapeMode Mode;
        public readonly string DisplayName;
        public readonly Color Ribbon;
        public readonly Color Ground;

        public LandscapePalette(LandscapeMode mode, string displayName, Color ribbon, Color ground)
        {
            Mode = mode;
            DisplayName = displayName;
            Ribbon = ribbon;
            Ground = ground;
        }

        public static LandscapePalette For(LandscapeMode mode)
        {
            switch (mode)
            {
                case LandscapeMode.Rain:
                    return new LandscapePalette(mode, "LLUVIA", new Color(0.35f, 0.82f, 1f), new Color(0.05f, 0.14f, 0.36f));
                case LandscapeMode.Ice:
                    return new LandscapePalette(mode, "HIELO", new Color(0.92f, 0.98f, 1f), new Color(0.22f, 0.48f, 0.72f));
                case LandscapeMode.Snow:
                    return new LandscapePalette(mode, "NIEVE", new Color(0.08f, 0.38f, 1f), new Color(0.9f, 0.94f, 0.98f));
                case LandscapeMode.Lava:
                    return new LandscapePalette(mode, "LAVA", new Color(1f, 0.9f, 0.28f), new Color(0.22f, 0.03f, 0.01f));
                case LandscapeMode.Desert:
                    return new LandscapePalette(mode, "DESIERTO", new Color(1f, 0.96f, 0.75f), new Color(0.48f, 0.26f, 0.06f));
                case LandscapeMode.Forest:
                    return new LandscapePalette(mode, "BOSQUE", new Color(0.55f, 1f, 0.2f), new Color(0.04f, 0.2f, 0.05f));
                case LandscapeMode.Night:
                    return new LandscapePalette(mode, "NOCHE", new Color(1f, 0.15f, 0.85f), new Color(0.02f, 0.0f, 0.05f));
                default:
                    return new LandscapePalette(LandscapeMode.Asphalt, "ASFALTO", new Color(0.15f, 1f, 1f), new Color(0.18f, 0.18f, 0.2f));
            }
        }
    }

    /// <summary>
    /// Elige el paisaje. En automático sigue el relieve del piso; los botones lo fijan a mano.
    /// </summary>
    public class LandscapeDirector : MonoBehaviour
    {
        public static LandscapeDirector Instance { get; private set; }

        private FloorReliefScanner _scanner;
        private LandscapeMode _mode = LandscapeMode.Asphalt;
        private bool _automatic = true;
        private FloorReliefKind _loggedKind = (FloorReliefKind)(-1);
        private LandscapeMode _loggedMode = (LandscapeMode)(-1);

        public LandscapeMode Mode => _mode;

        public bool IsAutomatic => _automatic;

        public LandscapePalette Palette => LandscapePalette.For(_mode);

        public string StatusLine
        {
            get
            {
                FloorReliefKind kind = _scanner != null ? _scanner.Kind : FloorReliefKind.Flat;
                string source = _scanner != null && _scanner.FromDevice ? "PISO" : "SIMULADO";
                string auto = _automatic ? " (AUTO)" : string.Empty;
                return "PAISAJE: " + Palette.DisplayName + auto + "   ·   RELIEVE: " + FloorReliefScanner.DisplayName(kind) + "   ·   " + source;
            }
        }

        public event Action OnLandscapeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            _scanner = FindObjectOfType<FloorReliefScanner>();
            if (_scanner != null)
            {
                _scanner.OnReliefChanged += HandleRelief;
            }
        }

        private void Start()
        {
            Publish(true);
        }

        private void OnDisable()
        {
            if (_scanner != null)
            {
                _scanner.OnReliefChanged -= HandleRelief;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void UseAutomatic()
        {
            _automatic = true;
            if (_scanner != null)
            {
                _scanner.UseDetectedFloor();
            }

            _mode = ModeFor(CurrentKind());
            Publish(true);
        }

        public void SetMode(LandscapeMode mode)
        {
            _automatic = false;
            _mode = mode;
            Publish(true);
        }

        public void SetAsphalt() { SetMode(LandscapeMode.Asphalt); }
        public void SetRain() { SetMode(LandscapeMode.Rain); }
        public void SetIce() { SetMode(LandscapeMode.Ice); }
        public void SetSnow() { SetMode(LandscapeMode.Snow); }
        public void SetLava() { SetMode(LandscapeMode.Lava); }
        public void SetDesert() { SetMode(LandscapeMode.Desert); }
        public void SetForest() { SetMode(LandscapeMode.Forest); }
        public void SetNight() { SetMode(LandscapeMode.Night); }

        public void CycleMode()
        {
            if (_automatic)
            {
                SetMode(LandscapeMode.Asphalt);
                return;
            }

            int next = ((int)_mode + 1) % 8;
            if (next == 0)
            {
                UseAutomatic();
                return;
            }

            SetMode((LandscapeMode)next);
        }

        public void CycleRelief()
        {
            if (_scanner == null)
            {
                _scanner = FindObjectOfType<FloorReliefScanner>();
            }

            if (_scanner == null)
            {
                return;
            }

            _scanner.CycleSimulatedRelief();
        }

        public float SampleHeight(Transform space, float x, float z)
        {
            if (_scanner == null)
            {
                return 0.002f;
            }

            return _scanner.SampleHeight(space, x, z);
        }

        public FloorReliefKind CurrentKind()
        {
            return _scanner != null ? _scanner.Kind : FloorReliefKind.Flat;
        }

        public static LandscapeMode ModeFor(FloorReliefKind kind)
        {
            switch (kind)
            {
                case FloorReliefKind.Slope: return LandscapeMode.Desert;
                case FloorReliefKind.Steps: return LandscapeMode.Snow;
                case FloorReliefKind.Rough: return LandscapeMode.Forest;
                default: return LandscapeMode.Asphalt;
            }
        }

        private void HandleRelief()
        {
            if (_automatic)
            {
                _mode = ModeFor(CurrentKind());
            }

            Publish(false);
        }

        private void Publish(bool forceLog)
        {
            FloorReliefKind kind = CurrentKind();
            if (forceLog || kind != _loggedKind || _mode != _loggedMode)
            {
                _loggedKind = kind;
                _loggedMode = _mode;
                Debug.Log("<color=cyan>[PAISAJE] " + StatusLine + "</color>");
            }

            OnLandscapeChanged?.Invoke();
        }
    }
}
