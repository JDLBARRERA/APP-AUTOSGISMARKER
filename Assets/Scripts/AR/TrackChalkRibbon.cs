using UnityEngine;
using ARTrackBuilder.Core;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.AR
{
    /// <summary>
    /// Cinta de gis del ancho real de la pista. El proyector la tira en el color del paisaje.
    /// </summary>
    public class TrackChalkRibbon : MonoBehaviour
    {
        private const int MaxPoints = 16;

        private TrackWaypointProjection _projection;
        private LandscapeDirector _director;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Material _material;
        private Vector3[] _vertices;
        private Color[] _colors;
        private int[] _triangles;
        private int _builtCount = -1;

        private void OnEnable()
        {
            _projection = GetComponent<TrackWaypointProjection>();
            _director = LandscapeDirector.Instance != null ? LandscapeDirector.Instance : FindObjectOfType<LandscapeDirector>();
            EnsureMesh();
            if (_projection != null)
            {
                _projection.OnTrackProjected += Rebuild;
            }

            if (_director != null)
            {
                _director.OnLandscapeChanged += Rebuild;
            }

            Rebuild();
        }

        private void OnDisable()
        {
            if (_projection != null)
            {
                _projection.OnTrackProjected -= Rebuild;
            }

            if (_director != null)
            {
                _director.OnLandscapeChanged -= Rebuild;
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        private void EnsureMesh()
        {
            Transform existing = transform.Find("ChalkRibbon");
            GameObject host = existing != null ? existing.gameObject : new GameObject("ChalkRibbon");
            host.transform.SetParent(transform, false);
            host.transform.localPosition = Vector3.zero;
            host.transform.localRotation = Quaternion.identity;
            host.transform.localScale = Vector3.one;

            MeshFilter filter = host.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = host.AddComponent<MeshFilter>();
            }

            _renderer = host.GetComponent<MeshRenderer>();
            if (_renderer == null)
            {
                _renderer = host.AddComponent<MeshRenderer>();
            }

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "ChalkRibbon";
                _mesh.MarkDynamic();
            }

            filter.sharedMesh = _mesh;
            if (_material == null)
            {
                _material = ProjectorPaint.CreateVertexColor();
                _material.renderQueue = 2500;
            }

            _renderer.sharedMaterial = _material;
        }

        private void Rebuild()
        {
            if (_mesh == null)
            {
                EnsureMesh();
            }

            DefinedTrack track = _projection != null ? _projection.CurrentTrack : null;
            TrackWaypoint[] points = track != null ? track.Waypoints : null;
            if (points == null || points.Length < 3)
            {
                _renderer.enabled = false;
                return;
            }

            int count = Mathf.Min(points.Length, MaxPoints);
            LandscapePalette palette = _director != null ? _director.Palette : LandscapePalette.For(LandscapeMode.Asphalt);
            Transform space = transform;
            float half = TrackConstants.TRACK_WIDTH_M * 0.5f;
            Color edge = palette.Ribbon * 0.45f;
            edge.a = 1f;
            Color center = palette.Ribbon;
            center.a = 1f;

            if (_builtCount != count)
            {
                _vertices = new Vector3[count * 3];
                _colors = new Color[count * 3];
                _triangles = new int[count * 12];
                _builtCount = count;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 current = points[i].Position;
                Vector3 previous = points[(i + count - 1) % count].Position;
                Vector3 next = points[(i + 1) % count].Position;
                Vector3 forward = next - previous;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.000001f)
                {
                    forward = Vector3.forward;
                }

                forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward) * half;
                float height = _director != null ? _director.SampleHeight(space, current.x, current.z) : current.y;
                Vector3 position = new Vector3(current.x, height + 0.008f, current.z);
                int v = i * 3;
                _vertices[v] = position - right;
                _vertices[v + 1] = position;
                _vertices[v + 2] = position + right;
                _colors[v] = edge;
                _colors[v + 1] = center;
                _colors[v + 2] = edge;
            }

            for (int i = 0; i < count; i++)
            {
                int n = (i + 1) % count;
                int t = i * 12;
                int a = i * 3;
                int b = n * 3;
                _triangles[t] = a;
                _triangles[t + 1] = b;
                _triangles[t + 2] = a + 1;
                _triangles[t + 3] = a + 1;
                _triangles[t + 4] = b;
                _triangles[t + 5] = b + 1;
                _triangles[t + 6] = a + 1;
                _triangles[t + 7] = b + 1;
                _triangles[t + 8] = a + 2;
                _triangles[t + 9] = a + 2;
                _triangles[t + 10] = b + 1;
                _triangles[t + 11] = b + 2;
            }

            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
            bool vertexColor = _material.shader != null && _material.shader.name.IndexOf("ProjectorVertexColor", System.StringComparison.Ordinal) >= 0;
            ProjectorPaint.SetTint(_material, vertexColor ? Color.white : center);
            _renderer.enabled = true;
        }
    }

    internal static class ProjectorPaint
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static Material CreateVertexColor()
        {
            Material source = Resources.Load<Material>("ProjectorVertexColor");
            if (source != null)
            {
                Material copy = new Material(source);
                SetTint(copy, Color.white);
                return copy;
            }

            Shader shader = Shader.Find("ARTrackBuilder/ProjectorVertexColor");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            SetTint(material, Color.white);
            return material;
        }

        public static Material CreateUnlit(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            SetTint(material, color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color);
            }

            return material;
        }

        public static void SetTint(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }
        }
    }
}
