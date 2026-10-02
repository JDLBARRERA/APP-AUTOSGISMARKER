using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARTrackBuilder.AR
{
    public enum FloorReliefKind
    {
        Flat,
        Slope,
        Steps,
        Rough
    }

    /// <summary>
    /// Lee los planos del piso y los clasifica. Sin teléfono, el relieve se simula para el proyector.
    /// </summary>
    public class FloorReliefScanner : MonoBehaviour
    {
        private const int MaxSamples = 16;

        private readonly Vector3[] _sampleWorld = new Vector3[MaxSamples];
        private readonly Vector3[] _sampleNormal = new Vector3[MaxSamples];
        private readonly float[] _sampleRadius = new float[MaxSamples];

        private ARPlaneManager _planes;
        private FloorReliefKind _kind = FloorReliefKind.Flat;
        private int _sampleCount;
        private int _lastCount = -1;
        private float _lastRange = -1f;
        private bool _useSimulation;
        private bool _fromDevice;

        public FloorReliefKind Kind => _kind;

        public bool FromDevice => _fromDevice && !_useSimulation;

        public event Action OnReliefChanged;

        private void OnEnable()
        {
            _planes = FindObjectOfType<ARPlaneManager>();
            if (_planes != null)
            {
                _planes.planesChanged += HandlePlanesChanged;
            }
        }

        private void Start()
        {
            if (!_useSimulation)
            {
                ReadPlanes();
            }
        }

        private void OnDisable()
        {
            if (_planes != null)
            {
                _planes.planesChanged -= HandlePlanesChanged;
            }
        }

        /// <summary>
        /// Avanza plano, pendiente, escalones y rugoso sin usar la cámara.
        /// </summary>
        public void CycleSimulatedRelief()
        {
            _useSimulation = true;
            _fromDevice = false;
            _sampleCount = 0;
            _kind = (FloorReliefKind)(((int)_kind + 1) % 4);
            _lastCount = 0;
            _lastRange = 0f;
            OnReliefChanged?.Invoke();
        }

        /// <summary>
        /// Vuelve a clasificar el piso que detecte AR Foundation.
        /// </summary>
        public void UseDetectedFloor()
        {
            _useSimulation = false;
            ReadPlanes();
        }

        /// <summary>
        /// Altura local del piso, en metros, para que la pista se apoye en el relieve.
        /// </summary>
        public float SampleHeight(Transform space, float x, float z)
        {
            if (_useSimulation || _sampleCount == 0 || space == null)
            {
                return ProceduralHeight(x, z, _kind);
            }

            float weightSum = 0f;
            float heightSum = 0f;
            for (int i = 0; i < _sampleCount; i++)
            {
                Vector3 local = space.InverseTransformPoint(_sampleWorld[i]);
                float dx = local.x - x;
                float dz = local.z - z;
                float dist = Mathf.Sqrt((dx * dx) + (dz * dz));
                float radius = Mathf.Max(0.08f, _sampleRadius[i]);
                if (dist > radius * 1.5f)
                {
                    continue;
                }

                Vector3 normal = space.InverseTransformDirection(_sampleNormal[i]);
                float ny = Mathf.Abs(normal.y) < 0.2f ? Mathf.Sign(normal.y) * 0.2f : normal.y;
                if (Mathf.Abs(ny) < 0.2f)
                {
                    ny = 0.2f;
                }

                float height = local.y - ((normal.x * (x - local.x)) + (normal.z * (z - local.z))) / ny;
                float weight = 1f / ((dist * dist) + 0.0025f);
                heightSum += height * weight;
                weightSum += weight;
            }

            if (weightSum < 0.001f)
            {
                return ProceduralHeight(x, z, _kind);
            }

            return heightSum / weightSum;
        }

        public static string DisplayName(FloorReliefKind kind)
        {
            switch (kind)
            {
                case FloorReliefKind.Slope: return "PENDIENTE";
                case FloorReliefKind.Steps: return "ESCALONES";
                case FloorReliefKind.Rough: return "RUGOSO";
                default: return "PLANO";
            }
        }

        private void HandlePlanesChanged(ARPlanesChangedEventArgs args)
        {
            if (_useSimulation)
            {
                return;
            }

            ReadPlanes();
        }

        private void ReadPlanes()
        {
            int count = 0;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float maxTilt = 0f;

            if (_planes != null)
            {
                foreach (ARPlane plane in _planes.trackables)
                {
                    if (count >= MaxSamples || plane.trackingState != TrackingState.Tracking)
                    {
                        continue;
                    }

                    PlaneAlignment alignment = plane.alignment;
                    if (alignment == PlaneAlignment.Vertical || alignment == PlaneAlignment.HorizontalDown)
                    {
                        continue;
                    }

                    _sampleWorld[count] = plane.transform.position;
                    _sampleNormal[count] = plane.transform.up;
                    _sampleRadius[count] = Mathf.Max(plane.size.x, plane.size.y) * 0.5f;
                    float y = plane.transform.position.y;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    float tilt = Vector3.Angle(plane.transform.up, Vector3.up);
                    if (tilt > maxTilt) maxTilt = tilt;
                    count++;
                }
            }

            _sampleCount = count;
            _fromDevice = count > 0;
            float range = count > 0 ? maxY - minY : 0f;
            FloorReliefKind next = Classify(count, range, maxTilt);
            bool changed = next != _kind || count != _lastCount || Mathf.Abs(range - _lastRange) > 0.01f;
            _kind = next;
            _lastCount = count;
            _lastRange = range;
            if (changed)
            {
                OnReliefChanged?.Invoke();
            }
        }

        private static FloorReliefKind Classify(int count, float range, float maxTilt)
        {
            if (count == 0)
            {
                return FloorReliefKind.Flat;
            }

            if (range < 0.03f && maxTilt < 8f)
            {
                return FloorReliefKind.Flat;
            }

            if (maxTilt >= 8f && range < 0.12f)
            {
                return FloorReliefKind.Slope;
            }

            if (count >= 2 && range >= 0.05f)
            {
                return FloorReliefKind.Steps;
            }

            return FloorReliefKind.Rough;
        }

        private static float ProceduralHeight(float x, float z, FloorReliefKind kind)
        {
            switch (kind)
            {
                case FloorReliefKind.Slope:
                    return (z + 0.12f) * 0.09f;
                case FloorReliefKind.Steps:
                    return Mathf.Floor((z + 0.46f) / 0.23f) * 0.018f;
                case FloorReliefKind.Rough:
                    return (Mathf.Sin(x * 16f) * Mathf.Cos(z * 13f) * 0.016f) + (Mathf.Sin((x + z) * 9f) * 0.008f);
                default:
                    return 0.002f;
            }
        }
    }
}
