using UnityEngine;
using UnityEngine.UI;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Muestra el reglamento oficial de carreras en un panel que el jugador puede abrir en cualquier momento.
    /// </summary>
    public class RulebookUIManager : MonoBehaviour
    {
        private const string RULEBOOK_TEXT =
            "REGLAMENTO OFICIAL DE CARRERAS AR\n\n" +
            "I. Movimiento y Penalizaciones\n\n" +
            "El Tiro de Índice: El auto solo puede avanzar mediante impactos rápidos (capirotazos) con la fuerza del dedo índice. Queda estrictamente prohibido arrastrar, empujar o acompañar el vehículo con la mano.\n\n" +
            "Límite de 3 Tiros: Cada jugador tiene un máximo de 3 tiros por turno para avanzar lo más posible.\n\n" +
            "Castigo por Salida (Regreso al Punto Cero): Si tu auto toca o sale de la línea de gis en cualquier tiro (el 1, el 2 o el 3), tu turno termina inmediatamente. Castigo: Debes regresar el auto a la posición exacta en la que empezó ese turno. No hay avances parciales; si te sales de la pista, pierdes todo el terreno ganado en esa ronda.\n\n" +
            "El Pivote Estratégico: Para corregir el ángulo antes de disparar, el auto solo puede girar sobre su propio eje. Es obligatorio mantener siempre una llanta apoyada en el piso; levantarlo completamente del asfalto anula el turno.\n\n" +
            "II. Interacción entre Rivales\n\n" +
            "Tráfico Pesado: Si dos autos terminan su trayectoria tocándose (pegados), el jugador en turno pierde 1 tiro de su ronda exclusivamente para acomodar su vehículo sin mover el del rival.\n\n" +
            "Takedown (Choque Ofensivo): Si utilizas uno de tus tiros para impactar el auto de un rival y logras sacarlo de la pista de gis, el rival pierde el primer tiro de su siguiente turno y tú te ganas el derecho de dejar tu auto exactamente donde quedó tras el impacto.\n\n" +
            "Rebufo (Drafting): Si logras estacionar tu auto justo detrás del auto de un rival (a menos de 2 cm sin tocarlo), en tu siguiente turno ganas 1 tiro extra.\n\n" +
            "III. Trampas Físicas y Magia AR\n\n" +
            "Las Trampas de Tensión: Existen zonas especiales de la pista que exigen ser cruzadas en exactamente 1, 2 o 3 tiros. Si el auto se detiene dentro de la trampa al agotar esos tiros correspondientes, es \"tragado\" y debe regresar al inicio de esa sección.\n\n" +
            "Zonas de Turbo AR: Si al terminar tu turno tu auto queda estacionado sobre una flecha verde holográfica proyectada por la app, ganas un \"Boost\". Tu siguiente turno tendrá 4 tiros en lugar de 3.\n\n" +
            "Zonas de Peligro AR: Si la app proyecta lava o hielo y tu auto se detiene ahí, tu vehículo sufre daño. En tu siguiente turno, es obligatorio tirar usando tu mano no dominante.\n\n" +
            "El Árbitro de Cámara (VAR): Ante cualquier disputa sobre si un auto tocó la línea de gis o al cruzar la meta, los jugadores usarán la cámara de la app. El sistema AR dictará un veredicto imparcial sobre la posición.";

        [Header("Paneles de Interfaz")]
        [SerializeField] private GameObject _rulebookPanel;
        [SerializeField] private Text _rulebookText;

        [Header("Controles")]
        [SerializeField] private Button _openRulebookButton;
        [SerializeField] private Button _closeRulebookButton;

        private void OnEnable()
        {
            if (_openRulebookButton != null)
            {
                _openRulebookButton.onClick.AddListener(OpenRulebook);
            }

            if (_closeRulebookButton != null)
            {
                _closeRulebookButton.onClick.AddListener(CloseRulebook);
            }
        }

        private void OnDisable()
        {
            if (_openRulebookButton != null)
            {
                _openRulebookButton.onClick.RemoveListener(OpenRulebook);
            }

            if (_closeRulebookButton != null)
            {
                _closeRulebookButton.onClick.RemoveListener(CloseRulebook);
            }
        }

        private void Start()
        {
            if (_rulebookText != null)
            {
                _rulebookText.text = RULEBOOK_TEXT;
            }

            if (_rulebookPanel != null)
            {
                _rulebookPanel.SetActive(false);
            }
        }

        private void OpenRulebook()
        {
            if (_rulebookPanel != null)
            {
                _rulebookPanel.SetActive(true);
            }
        }

        private void CloseRulebook()
        {
            if (_rulebookPanel != null)
            {
                _rulebookPanel.SetActive(false);
            }
        }
    }
}
