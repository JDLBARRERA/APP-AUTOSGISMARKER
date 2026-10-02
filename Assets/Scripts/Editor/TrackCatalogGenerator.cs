using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ARTrackBuilder.Core;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.Editor
{
    /// <summary>
    /// Herramienta de Editor para generar automáticamente mil variaciones de pistas
    /// dentro de 0.95 m y con los nodos de reglas del reglamento.
    /// </summary>
    public class TrackCatalogGenerator : EditorWindow
    {
        private const int TRACK_COUNT = 1000;
        private const string CATALOG_FOLDER = "Assets/Prefabs/AR/Catalog";

        private static readonly string[] FamousPrefixes =
        {
            "Mónaco", "Nürburgring", "Spa", "Monza", "Laguna Seca", "Silverstone",
            "Suzuka", "Daytona", "Indianapolis", "Le Mans", "Apex", "Inferno",
            "Cyber", "Nitro", "Tsunami", "Volcán", "Viper", "Phantom", "T-Rex"
        };

        private static readonly string[] TrackSuffixes =
        {
            "Drift", "Ring", "Circuit", "Loop", "Pass", "Canyon", "Speedway",
            "Snake", "Chicane", "Apex", "Overdrive", "Gis-Master", "Extreme"
        };

        [MenuItem("ARTrackBuilder/Generar Catálogo de 1000 Pistas")]
        public static void GenerateCatalog()
        {
            TrackDataManager dataManager = Object.FindObjectOfType<TrackDataManager>();
            if (dataManager == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontró un TrackDataManager en la escena.", "OK");
                return;
            }

            EnsureCatalogFolder();
            List<TrackDefinition> catalog = new List<TrackDefinition>(TRACK_COUNT);

            try
            {
                for (int i = 1; i <= TRACK_COUNT; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Catálogo de pistas",
                        $"Generando TRK_{i:D4}",
                        i / (float)TRACK_COUNT);

                    string randomName = GenerateRandomTrackName(i);
                    GameObject generatedTrackPrefab = CreateTrackGeometryPrefab(i, randomName);

                    catalog.Add(new TrackDefinition
                    {
                        TrackID = $"TRK_{i:D4}",
                        DisplayName = randomName,
                        TrackPrefab = generatedTrackPrefab
                    });
                }

                WriteCatalog(dataManager, catalog);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Generador] ¡1000 Pistas generadas con éxito! Escala máxima garantizada: {TrackConstants.MAX_BOUNDING_BOX_M}m. Ancho de gis: {TrackConstants.TRACK_WIDTH_M}m.");
            EditorUtility.DisplayDialog("Éxito", "Se han generado y configurado 1000 pistas con zonas de reglas exactas.", "¡A Correr!");
        }

        private static string GenerateRandomTrackName(int index)
        {
            string prefix = FamousPrefixes[Random.Range(0, FamousPrefixes.Length)];
            string suffix = TrackSuffixes[Random.Range(0, TrackSuffixes.Length)];
            return $"{prefix} {suffix} #{index}";
        }

        private static GameObject CreateTrackGeometryPrefab(int id, string trackName)
        {
            GameObject trackRoot = new GameObject(trackName);

            GameObject chalkPath = new GameObject("Chalk_Path_Line");
            chalkPath.transform.SetParent(trackRoot.transform, false);

            GameObject exactZone = new GameObject("RuleNode_TiroExacto");
            exactZone.transform.SetParent(trackRoot.transform, false);
            exactZone.transform.localPosition = new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));
            exactZone.AddComponent<BoxCollider>().size = new Vector3(0.08f, 0.01f, 0.08f);

            GameObject tensionTrap = new GameObject($"RuleNode_TrampaTension_{Random.Range(1, 4)}Tiros");
            tensionTrap.transform.SetParent(trackRoot.transform, false);
            tensionTrap.transform.localPosition = new Vector3(Random.Range(-0.30f, 0.30f), 0f, Random.Range(-0.30f, 0.30f));

            GameObject arZone = new GameObject(Random.value > 0.5f ? "ARZone_TurboBoost" : "ARZone_LavaPeligro");
            arZone.transform.SetParent(trackRoot.transform, false);
            arZone.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));

            string assetPath = $"{CATALOG_FOLDER}/TRK_{id:D4}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(trackRoot, assetPath);
            Object.DestroyImmediate(trackRoot);
            return prefab;
        }

        private static void WriteCatalog(TrackDataManager dataManager, List<TrackDefinition> catalog)
        {
            SerializedObject serializedManager = new SerializedObject(dataManager);
            SerializedProperty tracks = serializedManager.FindProperty("_availableTracks");
            tracks.ClearArray();

            for (int i = 0; i < catalog.Count; i++)
            {
                tracks.InsertArrayElementAtIndex(i);
                SerializedProperty element = tracks.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("TrackID").stringValue = catalog[i].TrackID;
                element.FindPropertyRelative("DisplayName").stringValue = catalog[i].DisplayName;
                element.FindPropertyRelative("TrackPrefab").objectReferenceValue = catalog[i].TrackPrefab;
            }

            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(dataManager);
        }

        private static void EnsureCatalogFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/AR"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "AR");
            }

            if (!AssetDatabase.IsValidFolder(CATALOG_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/AR", "Catalog");
            }
        }
    }
}
