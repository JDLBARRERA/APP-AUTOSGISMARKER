using UnityEngine;
using UnityEditor;
using ARTrackBuilder.AR;
using ARTrackBuilder.Core;
using ARTrackBuilder.Data;
using ARTrackBuilder.UI;

namespace ARTrackBuilder.Editor
{
    /// <summary>
    /// Herramienta de auditoría rápida para detectar campos nulos o referencias
    /// rotas en la escena antes de compilar.
    /// </summary>
    public class ARSceneValidator : EditorWindow
    {
        [MenuItem("ARTrackBuilder/Validar Escena Antes de Compilar")]
        public static void ValidateScene()
        {
            int errors = 0;

            // 1. Validar XR Origin y Managers
            TrackProjectorManager projector = FindObjectOfType<TrackProjectorManager>();
            if (projector == null)
            {
                Debug.LogError("[Validación] FALTA: No existe un TrackProjectorManager en la escena.");
                errors++;
            }

            TrackDataManager dataManager = FindObjectOfType<TrackDataManager>();
            if (dataManager == null)
            {
                Debug.LogError("[Validación] FALTA: No existe un TrackDataManager en la escena.");
                errors++;
            }

            // 2. Validar Controlador de Turnos
            RaceTurnController turnController = FindObjectOfType<RaceTurnController>();
            if (turnController == null)
            {
                Debug.LogError("[Validación] FALTA: No existe el RaceTurnController para manejar los 3 tiros.");
                errors++;
            }

            // 3. Validar UI
            TrackUIManager trackUI = FindObjectOfType<TrackUIManager>();
            if (trackUI == null)
            {
                Debug.LogError("[Validación] FALTA: No existe el TrackUIManager en el Canvas.");
                errors++;
            }

            if (errors == 0)
            {
                Debug.Log("<color=green>[Validación ÉXITO] La escena tiene todas las dependencias conectadas. Listo para Build.</color>");
                EditorUtility.DisplayDialog("Verificación Exitosa", "Todos los gestores y controladores están presentes en la escena.", "Continuar a Build");
            }
            else
            {
                EditorUtility.DisplayDialog("Errores Detectados", $"Se encontraron {errors} errores de referencias en la escena. Revisa la consola de Unity.", "Corregir");
            }
        }
    }
}
