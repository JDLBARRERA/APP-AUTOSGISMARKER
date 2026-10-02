namespace ARTrackBuilder.Core
{
    /// <summary>
    /// Orden de tiro. La salida se mantiene mientras el primero siga líder.
    /// Al cerrar la vuelta de turnos, tira primero quien va primero en la carrera.
    /// </summary>
    public sealed class RaceThrowOrder
    {
        private string[] _names = new string[0];
        private int[] _place = new int[0];
        private int[] _queue = new int[0];
        private int _throwIndex;
        private int _turnsThisRound;

        public int Count => _names.Length;

        public string Current => Count == 0 ? string.Empty : _names[_queue[_throwIndex]];

        public string Next => Count == 0 ? string.Empty : _names[_queue[(_throwIndex + 1) % Count]];

        public string Leader
        {
            get
            {
                for (int i = 0; i < _place.Length; i++)
                {
                    if (_place[i] == 1)
                    {
                        return _names[i];
                    }
                }

                return Count == 0 ? string.Empty : _names[0];
            }
        }

        public int CurrentPlace => Count == 0 ? 0 : _place[_queue[_throwIndex]];

        /// <summary>
        /// La parrilla de salida: el primero de la lista tira primero y sale en el puesto 1.
        /// </summary>
        public void SetGrid(string[] players)
        {
            int count = players == null ? 0 : players.Length;
            _names = new string[count];
            _place = new int[count];
            _queue = new int[count];
            for (int i = 0; i < count; i++)
            {
                _names[i] = players[i];
                _place[i] = i + 1;
                _queue[i] = i;
            }

            _throwIndex = 0;
            _turnsThisRound = 0;
        }

        /// <summary>
        /// Ese jugador pasa a ir primero en la pista. El orden de tiro cambia al cerrar la vuelta.
        /// </summary>
        public void DeclareLeader(string playerName)
        {
            int index = IndexOf(playerName);
            if (index < 0 || _place[index] == 1)
            {
                return;
            }

            int previous = _place[index];
            for (int i = 0; i < _place.Length; i++)
            {
                if (_place[i] < previous)
                {
                    _place[i]++;
                }
            }

            _place[index] = 1;
        }

        /// <summary>
        /// Cierra el turno de quien tiraba. Si ya tiraron todos, el primero de la carrera abre la siguiente vuelta.
        /// </summary>
        public bool CompleteTurn()
        {
            if (Count == 0)
            {
                return false;
            }

            _turnsThisRound++;
            if (_turnsThisRound >= Count)
            {
                AlignQueueToRace();
                _throwIndex = 0;
                _turnsThisRound = 0;
                return true;
            }

            _throwIndex = (_throwIndex + 1) % Count;
            return false;
        }

        private void AlignQueueToRace()
        {
            for (int i = 0; i < _queue.Length; i++)
            {
                _queue[i] = i;
            }

            for (int i = 0; i < _queue.Length; i++)
            {
                for (int j = i + 1; j < _queue.Length; j++)
                {
                    int left = _queue[i];
                    int right = _queue[j];
                    if (_place[right] < _place[left])
                    {
                        _queue[i] = right;
                        _queue[j] = left;
                    }
                }
            }
        }

        private int IndexOf(string playerName)
        {
            for (int i = 0; i < _names.Length; i++)
            {
                if (_names[i] == playerName)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
