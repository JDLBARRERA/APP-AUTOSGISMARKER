using UnityEngine;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.AR
{
    /// <summary>
    /// Suelo que el proyector pinta según el relieve: plano, pendiente, escalones o rugoso.
    /// </summary>
    public class LandscapeGround : MonoBehaviour
    {
        private const int Grid = 14;
        private const float HalfExtent = 0.46f;
        private static readonly Vector2[] DressingSpots =
        {
            new Vector2(0.36f, 0.16f),
            new Vector2(-0.37f, 0.14f),
            new Vector2(0.2f, -0.37f),
            new Vector2(-0.22f, -0.34f),
            new Vector2(0.38f, -0.1f),
            new Vector2(-0.1f, 0.38f)
        };

        private TrackWaypointProjection _projection;
        private LandscapeDirector _director;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Material _material;
        private Material _dressingMaterial;
        private Transform _dressingRoot;
        private readonly Vector3[] _vertices = new Vector3[(Grid + 1) * (Grid + 1)];
        private readonly Color[] _colors = new Color[(Grid + 1) * (Grid + 1)];
        private readonly int[] _triangles = new int[Grid * Grid * 6];

        private void OnEnable()
        {
            _projection = GetComponent<TrackWaypointProjection>();
            _director = LandscapeDirector.Instance != null ? LandscapeDirector.Instance : FindObjectOfType<LandscapeDirector>();
            EnsureMesh();
            if (_director != null)
            {
                _director.OnLandscapeChanged += Rebuild;
            }

            if (_projection != null)
            {
                _projection.OnTrackProjected += Rebuild;
            }

            Rebuild();
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.OnLandscapeChanged -= Rebuild;
            }

            if (_projection != null)
            {
                _projection.OnTrackProjected -= Rebuild;
            }
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
            if (_dressingMaterial != null) Destroy(_dressingMaterial);
            if (_mesh != null) Destroy(_mesh);
        }

        private void EnsureMesh()
        {
            Transform existing = transform.Find("LandscapeGround");
            GameObject host = existing != null ? existing.gameObject : new GameObject("LandscapeGround");
            host.transform.SetParent(transform, false);
            host.transform.localPosition = Vector3.zero;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one;

            MeshFilter filter = host.GetComponent<MeshFilter>();
            if (filter == null) filter = host.AddComponent<MeshFilter>();
            _renderer = host.GetComponent<MeshRenderer>();
            if (_renderer == null) _renderer = host.AddComponent<MeshRenderer>();
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "LandscapeGround";
                _mesh.MarkDynamic();
            }

            filter.sharedMesh = _mesh;
            if (_material == null)
            {
                _material = ProjectorPaint.CreateVertexColor();
                _material.renderQueue = 2400;
            }

            _renderer.sharedMaterial = _material;
            ProjectorPaint.SetTint(_material, Color.white);
        }

        private void Rebuild()
        {
            if (_mesh == null)
            {
                EnsureMesh();
            }

            if (_director == null)
            {
                _director = LandscapeDirector.Instance != null ? LandscapeDirector.Instance : FindObjectOfType<LandscapeDirector>();
            }

            LandscapePalette palette = _director != null ? _director.Palette : LandscapePalette.For(LandscapeMode.Asphalt);
            FloorReliefKind kind = _director != null ? _director.CurrentKind() : FloorReliefKind.Flat;
            Transform space = transform;
            int side = Grid + 1;

            for (int j = 0; j < side; j++)
            {
                float v = j / (float)Grid;
                float z = Mathf.Lerp(-HalfExtent, HalfExtent, v);
                for (int i = 0; i < side; i++)
                {
                    float u = i / (float)Grid;
                    float x = Mathf.Lerp(-HalfExtent, HalfExtent, u);
                    float height = _director != null ? _director.SampleHeight(space, x, z) : 0.002f;
                    int index = (j * side) + i;
                    _vertices[index] = new Vector3(x, height, z);
                    _colors[index] = ColorAt(x, z, height, palette, kind);
                }
            }

            int t = 0;
            for (int j = 0; j < Grid; j++)
            {
                for (int i = 0; i < Grid; i++)
                {
                    int a = (j * side) + i;
                    int b = a + 1;
                    int c = a + side;
                    int d = c + 1;
                    _triangles[t++] = a;
                    _triangles[t++] = c;
                    _triangles[t++] = b;
                    _triangles[t++] = b;
                    _triangles[t++] = c;
                    _triangles[t++] = d;
                }
            }

            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
            bool vertexColor = _material.shader != null && _material.shader.name.IndexOf("ProjectorVertexColor", System.StringComparison.Ordinal) >= 0;
            ProjectorPaint.SetTint(_material, vertexColor ? Color.white : palette.Ground);
            _renderer.enabled = true;
            RebuildDressing(palette, kind);
        }

        private Color ColorAt(float x, float z, float height, LandscapePalette palette, FloorReliefKind kind)
        {
            Color color = palette.Ground;
            switch (kind)
            {
                case FloorReliefKind.Slope:
                    color *= Mathf.Lerp(0.55f, 1.45f, Mathf.InverseLerp(-HalfExtent, HalfExtent, z));
                    break;
                case FloorReliefKind.Steps:
                    float band = Mathf.Floor((z + HalfExtent) / 0.23f);
                    color *= 0.62f + (band * 0.22f);
                    break;
                case FloorReliefKind.Rough:
                    float blotch = 0.72f + (Mathf.Sin(x * 22f) * Mathf.Cos(z * 18f) * 0.28f);
                    color *= blotch;
                    break;
                default:
                    bool seam = Mathf.Repeat(x + HalfExtent, 0.115f) < 0.012f || Mathf.Repeat(z + HalfExtent, 0.115f) < 0.012f;
                    if (seam) color *= 0.45f;
                    break;
            }

            switch (palette.Mode)
            {
                case LandscapeMode.Rain:
                    if (Mathf.Abs(Mathf.Sin(x * 46f)) < 0.12f) color = Color.Lerp(color, palette.Ribbon, 0.35f);
                    if (height < 0.004f) color = Color.Lerp(color, new Color(0.15f, 0.35f, 0.7f), 0.45f);
                    break;
                case LandscapeMode.Ice:
                    if (Mathf.Abs(Mathf.Sin((x + z) * 34f)) > 0.9f) color = Color.Lerp(color, Color.white, 0.75f);
                    break;
                case LandscapeMode.Snow:
                    color = Color.Lerp(new Color(0.62f, 0.7f, 0.78f), new Color(0.97f, 0.98f, 1f), Mathf.Clamp01((height * 28f) + 0.35f));
                    break;
                case LandscapeMode.Lava:
                    if (Mathf.Abs(Mathf.Sin(x * 30f) * Mathf.Sin(z * 26f)) < 0.08f) color = new Color(0.55f, 0.08f, 0.01f);
                    break;
                case LandscapeMode.Desert:
                    color *= 0.85f + (Mathf.Sin((x * 14f) + (z * 4f)) * 0.15f);
                    break;
                case LandscapeMode.Forest:
                    float canopy = Mathf.PerlinNoise((x + 3f) * 3.5f, (z + 3f) * 3.5f);
                    color = Color.Lerp(new Color(0.02f, 0.1f, 0.02f), new Color(0.1f, 0.35f, 0.08f), canopy);
                    break;
                case LandscapeMode.Night:
                    color *= 0.35f + (Mathf.Sin(x * 20f) * Mathf.Sin(z * 20f) * 0.08f);
                    break;
            }

            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = 1f;
            return color;
        }

        private void RebuildDressing(LandscapePalette palette, FloorReliefKind kind)
        {
            if (_dressingRoot != null)
            {
                Destroy(_dressingRoot.gameObject);
                _dressingRoot = null;
            }

            if (_dressingMaterial != null)
            {
                Destroy(_dressingMaterial);
                _dressingMaterial = null;
            }

            if (!HasDressing(palette.Mode))
            {
                return;
            }

            GameObject root = new GameObject("LandscapeDressing");
            root.transform.SetParent(transform, false);
            _dressingRoot = root.transform;
            Color color = DressingColor(palette);
            _dressingMaterial = ProjectorPaint.CreateUnlit(color);
            TrackWaypoint[] points = _projection != null && _projection.CurrentTrack != null ? _projection.CurrentTrack.Waypoints : null;

            for (int i = 0; i < DressingSpots.Length; i++)
            {
                float x = DressingSpots[i].x;
                float z = DressingSpots[i].y;
                if (kind == FloorReliefKind.Slope)
                {
                    z = Mathf.Lerp(-0.2f, 0.4f, i / (float)(DressingSpots.Length - 1));
                    x = (i % 2 == 0 ? -1f : 1f) * 0.28f;
                }

                if (!ClearOfTrack(points, x, z))
                {
                    continue;
                }

                float height = _director != null ? _director.SampleHeight(transform, x, z) : 0.002f;
                SpawnProp(palette.Mode, new Vector3(x, height, z), i);
            }
        }

        private void SpawnProp(LandscapeMode mode, Vector3 position, int index)
        {
            PrimitiveType primitive = PrimitiveType.Sphere;
            Vector3 scale = Vector3.one * 0.04f;
            float lift = 0.02f;
            switch (mode)
            {
                case LandscapeMode.Rain:
                    primitive = PrimitiveType.Cylinder;
                    scale = new Vector3(0.07f, 0.003f, 0.05f);
                    lift = 0.004f;
                    break;
                case LandscapeMode.Snow:
                    primitive = PrimitiveType.Sphere;
                    scale = new Vector3(0.06f, 0.025f, 0.045f);
                    lift = 0.012f;
                    break;
                case LandscapeMode.Ice:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(0.08f, 0.004f, 0.05f);
                    lift = 0.006f;
                    break;
                case LandscapeMode.Lava:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(0.11f, 0.003f, 0.018f);
                    lift = 0.006f;
                    break;
                case LandscapeMode.Desert:
                    primitive = PrimitiveType.Sphere;
                    scale = new Vector3(0.12f, 0.02f, 0.08f);
                    lift = 0.01f;
                    break;
                case LandscapeMode.Forest:
                    primitive = PrimitiveType.Cylinder;
                    scale = new Vector3(0.055f, 0.002f, 0.055f);
                    lift = 0.006f;
                    break;
            }

            GameObject prop = GameObject.CreatePrimitive(primitive);
            prop.name = mode + "_" + index;
            prop.transform.SetParent(_dressingRoot, false);
            prop.transform.localPosition = position + new Vector3(0f, lift, 0f);
            prop.transform.localScale = scale;
            Collider collider = prop.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = prop.GetComponent<Renderer>();
            renderer.sharedMaterial = _dressingMaterial;
        }

        private static bool HasDressing(LandscapeMode mode)
        {
            return mode == LandscapeMode.Rain
                || mode == LandscapeMode.Snow
                || mode == LandscapeMode.Ice
                || mode == LandscapeMode.Lava
                || mode == LandscapeMode.Desert
                || mode == LandscapeMode.Forest;
        }

        private static Color DressingColor(LandscapePalette palette)
        {
            switch (palette.Mode)
            {
                case LandscapeMode.Rain: return new Color(0.1f, 0.28f, 0.62f);
                case LandscapeMode.Snow: return new Color(0.95f, 0.97f, 1f);
                case LandscapeMode.Ice: return new Color(0.8f, 0.95f, 1f);
                case LandscapeMode.Lava: return new Color(1f, 0.35f, 0.02f);
                case LandscapeMode.Desert: return new Color(0.72f, 0.5f, 0.18f);
                case LandscapeMode.Forest: return new Color(0.05f, 0.28f, 0.06f);
                default: return palette.Ground;
            }
        }

        private static bool ClearOfTrack(TrackWaypoint[] points, float x, float z)
        {
            if (points == null || points.Length < 2)
            {
                return true;
            }

            const float Limit = 0.07f * 0.07f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 a = points[i].Position;
                Vector3 b = points[(i + 1) % points.Length].Position;
                float dx = b.x - a.x;
                float dz = b.z - a.z;
                float length = (dx * dx) + (dz * dz);
                float t = length < 0.000001f ? 0f : Mathf.Clamp01((((x - a.x) * dx) + ((z - a.z) * dz)) / length);
                float px = a.x + (dx * t);
                float pz = a.z + (dz * t);
                float ox = x - px;
                float oz = z - pz;
                if ((ox * ox) + (oz * oz) < Limit)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
