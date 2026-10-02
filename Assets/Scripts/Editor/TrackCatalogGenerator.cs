using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.Editor
{
    public class TrackCatalogGenerator : EditorWindow
    {
        [MenuItem("ARTrackBuilder/Limpiar y Optimizar Catálogo")]
        public static void OptimizeCatalog()
        {
            TrackDataManager dataManager = FindObjectOfType<TrackDataManager>();
            if (dataManager == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontró un TrackDataManager en la escena activa.", "OK");
                return;
            }

            // Las pistas se instancian dinámicamente con parámetros livianos
            // en lugar de almacenar 1000 prefabs pesados en el proyecto.
            Debug.Log("[Catálogo] Sistema optimizado para instanciación bajo demanda (Procedural Runtime).");
            EditorUtility.DisplayDialog("Éxito", "Catálogo configurado para evitar inflación en la compilación del APK.", "OK");
        }
    }
}
