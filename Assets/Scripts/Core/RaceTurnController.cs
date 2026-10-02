using System;
using UnityEngine;

namespace ARTrackBuilder.Core
{
    /// <summary>
    /// Cuenta los 3 tiros del turno y aplica el regreso al punto cero si el auto sale del gis.
    /// </summary>
    public class RaceTurnController : MonoBehaviour
    {
        public static RaceTurnController Instance { get; private set; }

        [Header("Estado de la Partida")]
        public int CurrentShot = 1; // 1, 2 o 3
        public string CurrentPlayerName = "";

        public event Action<int> OnShotChanged;
        public event Action<string> OnRuleAlertTriggered;

        private int _shotLimit = 3;
        private int _baseNextLimit = 3;
        private int _bonusNextShots;
        private bool _loseNextTurn;
        private bool _skipFirstShotNextTurn;
        private bool _nonDominantNextTurn;

        public int ShotLimit => _shotLimit;
        public bool CurrentTurnLost { get; private set; }
        public bool MustUseNonDominantHand { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterShot()
        {
            if (CurrentTurnLost)
            {
                OnRuleAlertTriggered?.Invoke("TURNO PERDIDO POR EL FOSO. Pasas este turno.");
                ResetTurn();
                return;
            }

            if (CurrentShot < _shotLimit)
            {
                CurrentShot++;
                OnShotChanged?.Invoke(CurrentShot);
            }
            else
            {
                OnRuleAlertTriggered?.Invoke($"¡{_shotLimit} TIROS COMPLETADOS! CAMBIO DE TURNO");
                ResetTurn();
            }
        }

        /// <summary>
        /// El auto quedó en un tiro largo: el siguiente turno tiene 4 tiros.
        /// </summary>
        public void GrantLongShot()
        {
            _baseNextLimit = 4;
            OnRuleAlertTriggered?.Invoke("TIRO LARGO: el siguiente turno tiene 4 tiros.");
        }

        /// <summary>
        /// El auto se detuvo en la zona de tiro exacto. La posición se mantiene.
        /// </summary>
        public void ConfirmExactShot()
        {
            OnRuleAlertTriggered?.Invoke("TIRO EXACTO: el auto quedó en la zona. La posición se mantiene.");
        }

        /// <summary>
        /// El auto se detuvo dentro de una trampa que exigía cruzarse en un número exacto de tiros.
        /// </summary>
        public void ApplyTensionTrap(int requiredShots)
        {
            int shots = Mathf.Clamp(requiredShots, 1, 3);
            OnRuleAlertTriggered?.Invoke($"TRAMPA DE {shots} TIROS: el auto quedó dentro y regresa al inicio de esa sección.");
        }

        /// <summary>
        /// El auto está en el tramo de lluvia: este turno solo tiene un tiro.
        /// El siguiente turno recupera su límite, salvo que ese auto también se marque en la lluvia.
        /// </summary>
        public void ApplyRain()
        {
            _shotLimit = 1;
            OnRuleAlertTriggered?.Invoke("LLUVIA: en este tramo solo vale un tiro.");
            OnShotChanged?.Invoke(CurrentShot);
        }

        /// <summary>
        /// Lava o hielo: el siguiente turno se tira con la mano no dominante.
        /// </summary>
        public void ApplyDanger(bool lava)
        {
            _nonDominantNextTurn = true;
            string zone = lava ? "LAVA" : "HIELO";
            OnRuleAlertTriggered?.Invoke($"{zone}: en el siguiente turno tiras con la mano no dominante.");
        }

        /// <summary>
        /// Dos autos quedaron pegados: un tiro del turno solo sirve para separarlos.
        /// </summary>
        public void ApplyTraffic()
        {
            if (CurrentTurnLost || CurrentShot >= _shotLimit)
            {
                OnRuleAlertTriggered?.Invoke("TRÁFICO PESADO: no quedan tiros para acomodar el auto.");
                return;
            }

            CurrentShot++;
            OnRuleAlertTriggered?.Invoke("TRÁFICO PESADO: un tiro se usa solo para separar los autos.");
            OnShotChanged?.Invoke(CurrentShot);
        }

        /// <summary>
        /// El rival salió de la pista por un choque: pierde el primer tiro de su siguiente turno.
        /// </summary>
        public void ApplyTakedown()
        {
            _skipFirstShotNextTurn = true;
            OnRuleAlertTriggered?.Invoke("TAKEDOWN: el rival pierde el primer tiro de su siguiente turno.");
        }

        /// <summary>
        /// El auto quedó a menos de 2 cm detrás de un rival, sin tocarlo.
        /// </summary>
        public void ApplyDraft()
        {
            _bonusNextShots += 1;
            OnRuleAlertTriggered?.Invoke("REBUFO: el siguiente turno gana 1 tiro extra.");
        }

        /// <summary>
        /// El auto cayó en un foso. Esa zona está prohibida y el turno siguiente se pierde.
        /// </summary>
        public void ApplyPit()
        {
            if (CurrentTurnLost || _loseNextTurn)
            {
                OnRuleAlertTriggered?.Invoke("¡CAÍSTE EN EL FOSO! El turno siguiente ya está perdido.");
                return;
            }

            _loseNextTurn = true;
            OnRuleAlertTriggered?.Invoke("¡CAÍSTE EN EL FOSO! Zona prohibida: pierdes el turno siguiente.");
        }

        public void ReportOutOfBounds()
        {
            // Regla oficial: Salir de la línea en Tiro 1, 2 o 3 pierde el resto de tiros y regresa al origen
            OnRuleAlertTriggered?.Invoke($"¡FUERA DE GIS EN TIRO {CurrentShot}! REGRESA AL PUNTO INICIAL DE ESTE TURNO.");
            ResetTurn();
        }

        public void ResetTurn()
        {
            CurrentShot = 1;
            if (_loseNextTurn)
            {
                _loseNextTurn = false;
                _skipFirstShotNextTurn = false;
                CurrentTurnLost = true;
                OnRuleAlertTriggered?.Invoke("TURNO PERDIDO: caíste en el foso.");
                OnShotChanged?.Invoke(CurrentShot);
                return;
            }

            CurrentTurnLost = false;
            MustUseNonDominantHand = _nonDominantNextTurn;
            _nonDominantNextTurn = false;
            _shotLimit = _baseNextLimit + _bonusNextShots;
            _baseNextLimit = 3;
            _bonusNextShots = 0;
            CurrentShot = _skipFirstShotNextTurn ? 2 : 1;
            _skipFirstShotNextTurn = false;
            if (MustUseNonDominantHand)
            {
                OnRuleAlertTriggered?.Invoke("MANO NO DOMINANTE: este turno se tira con la otra mano.");
            }

            OnShotChanged?.Invoke(CurrentShot);
        }
    }
}
