using UnityEngine;
using ARTrackBuilder.AR;

namespace ARTrackBuilder.Debugging
{
    /// <summary>
    /// Dibuja en el Editor los botones que disparan el simulador local de tapete, tiros y VAR.
    /// </summary>
    public class LocalTestOverlay : MonoBehaviour
    {
        [SerializeField] private ARDebugMockController _mockController;
        private LandscapeDirector _landscape;

        private void Awake()
        {
            if (_mockController == null)
            {
                _mockController = GetComponent<ARDebugMockController>();
            }
        }

        private void Start()
        {
            _landscape = LandscapeDirector.Instance != null ? LandscapeDirector.Instance : FindObjectOfType<LandscapeDirector>();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 240, 390), " PANEL DE PRUEBAS LOCAL", GUI.skin.window);

            if (GUILayout.Button("Simular Tapete (Tecla M)"))
            {
                if (_mockController != null) _mockController.ToggleSimulatedMat();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Registrar Tiro (Espacio)"))
            {
                if (_mockController != null) _mockController.SimulateShot();
            }

            if (GUILayout.Button("Fuera de Gis (Tecla O)"))
            {
                if (_mockController != null) _mockController.SimulateOutOfBounds();
            }

            if (GUILayout.Button("Caí en el Foso (Tecla F)"))
            {
                if (_mockController != null) _mockController.SimulatePit();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Abrir/Cerrar VAR (Tecla V)"))
            {
                if (_mockController != null) _mockController.ToggleVAR();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Siguiente paisaje"))
            {
                if (_landscape == null) _landscape = LandscapeDirector.Instance;
                if (_landscape != null) _landscape.CycleMode();
            }

            if (GUILayout.Button("Relieve del piso"))
            {
                if (_landscape == null) _landscape = LandscapeDirector.Instance;
                if (_landscape != null) _landscape.CycleRelief();
            }

            GUILayout.EndArea();
        }
#endif
    }
}
