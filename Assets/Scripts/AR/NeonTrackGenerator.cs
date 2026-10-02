using UnityEngine;
using ARTrackBuilder.Core;

namespace ARTrackBuilder.AR
{
    /// <summary>
    /// Construye en tiempo de ejecución la cinta de gis de 4.3 cm y el arco de meta anclado a la pista.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class NeonTrackGenerator : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [Header("Configuración de Pista")]
        [SerializeField] private float _trackWidth = TrackConstants.TRACK_WIDTH_M; // 0.043m (4.3 cm)
        [SerializeField] private Color _neonColor = Color.cyan;

        private void Awake()
        {
            BuildTrackMeshAndMeta();
        }

        [ContextMenu("Reconstruir Pista 3D")]
        public void BuildTrackMeshAndMeta()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer renderer = GetComponent<MeshRenderer>();

            // 1. Crear circuito en forma de 8 o curva ovalada cerrada (escala 0.8m x 0.8m)
            int segments = 64;
            Vector3[] pathPoints = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments * Mathf.PI * 2f;
                // Ecuación de Lemniscata / Ovalo adaptado al tapete (max 0.8m)
                float x = 0.35f * Mathf.Sin(t);
                float z = 0.35f * Mathf.Sin(t * 2f) / 2f;
                pathPoints[i] = new Vector3(x, 0.002f, z);
            }

            // 2. Generar malla tubular plana para simular el trazo de gis de 4.3 cm
            Mesh mesh = new Mesh();
            Vector3[] verts = new Vector3[segments * 2];
            int[] tris = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                Vector3 current = pathPoints[i];
                Vector3 next = pathPoints[(i + 1) % segments];
                Vector3 forward = (next - current).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward) * (_trackWidth * 0.5f);

                verts[i * 2] = current - right;
                verts[i * 2 + 1] = current + right;

                int nextI = (i + 1) % segments;
                int tIndex = i * 6;

                tris[tIndex] = i * 2;
                tris[tIndex + 1] = nextI * 2;
                tris[tIndex + 2] = i * 2 + 1;

                tris[tIndex + 3] = i * 2 + 1;
                tris[tIndex + 4] = nextI * 2;
                tris[tIndex + 5] = nextI * 2 + 1;
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.mesh = mesh;
            ApplyNeonMaterial(renderer);

            // 3. Crear Arco de Meta 3D Anclado (Objeto Físico Holográfico)
            BuildFinishArch(pathPoints[0]);
        }

        private void ApplyNeonMaterial(MeshRenderer renderer)
        {
            Shader shader = Shader.Find("Standard");
            Material material = shader != null ? new Material(shader) : renderer.material;
            material.color = _neonColor;
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty(EmissionColor))
            {
                material.SetColor(EmissionColor, _neonColor * 3f);
            }

            renderer.sharedMaterial = material;
        }

        private void BuildFinishArch(Vector3 position)
        {
            Transform existingArch = transform.Find("Meta_Arch_3D");
            if (existingArch != null)
            {
                if (Application.isPlaying)
                {
                    existingArch.name = "Meta_Arch_3D_Old";
                    Destroy(existingArch.gameObject);
                }
                else
                {
                    DestroyImmediate(existingArch.gameObject);
                }
            }

            GameObject metaArch = new GameObject("Meta_Arch_3D");
            metaArch.transform.SetParent(transform);
            metaArch.transform.localPosition = position;

            // Postes del arco
            GameObject leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftPillar.transform.SetParent(metaArch.transform);
            leftPillar.transform.localScale = new Vector3(0.015f, 0.08f, 0.015f);
            leftPillar.transform.localPosition = new Vector3(-0.035f, 0.08f, 0f);

            GameObject rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightPillar.transform.SetParent(metaArch.transform);
            rightPillar.transform.localScale = new Vector3(0.015f, 0.08f, 0.015f);
            rightPillar.transform.localPosition = new Vector3(0.035f, 0.08f, 0f);

            // Travesaño superior con color Fuego/Neón
            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.transform.SetParent(metaArch.transform);
            topBar.transform.localScale = new Vector3(0.08f, 0.015f, 0.015f);
            topBar.transform.localPosition = new Vector3(0f, 0.16f, 0f);

            Renderer barRenderer = topBar.GetComponent<Renderer>();
            barRenderer.material.color = Color.red;
            barRenderer.material.EnableKeyword("_EMISSION");
            if (barRenderer.material.HasProperty(EmissionColor))
            {
                barRenderer.material.SetColor(EmissionColor, Color.red * 3f);
            }
        }
    }
}
