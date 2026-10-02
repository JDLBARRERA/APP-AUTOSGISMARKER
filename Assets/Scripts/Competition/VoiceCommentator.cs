using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ARTrackBuilder.Competition
{
    /// <summary>
    /// Lee frases editables. En el teléfono usa la voz del sistema; en el editor muestra el subtítulo y un clip opcional.
    /// </summary>
    public class VoiceCommentator : MonoBehaviour
    {
        public static VoiceCommentator Instance { get; private set; }

        [SerializeField] private string _lapLine = "{piloto} vuelta {n} en {tiempo}";
        [SerializeField] private string _recordLine = "Récord de {piloto} en {tiempo}";
        [SerializeField] private string _winnerLine = "Ganador {piloto}";
        [SerializeField] private AudioClip _commentClip;

        private AudioSource _source;
        private string _subtitle = string.Empty;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidTts _tts;
#endif

        public string Subtitle => _subtitle;

        public event Action<string> OnLine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            _tts = new AndroidTts();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_tts != null)
            {
                _tts.Shutdown();
            }
#endif
        }

        public void SayLap(string pilot, int lap, long ticks)
        {
            Speak(Fill(_lapLine, pilot, lap, ticks));
        }

        public void SayRecord(string pilot, long ticks)
        {
            Speak(Fill(_recordLine, pilot, 0, ticks));
        }

        public void SayWinner(string pilot)
        {
            Speak(Fill(_winnerLine, pilot, 0, 0));
        }

        private static string Fill(string template, string pilot, int lap, long ticks)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return template
                .Replace("{piloto}", pilot ?? string.Empty)
                .Replace("{n}", lap.ToString())
                .Replace("{tiempo}", LapClock.Format(ticks));
        }

        private void Speak(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            _subtitle = line;
            OnLine?.Invoke(line);
            Debug.Log("[VOZ] " + line);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_tts != null && _tts.Speak(line))
            {
                return;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            SlotSpeechSpeak(line);
            return;
#endif
            if (_commentClip != null && _source != null)
            {
                _source.PlayOneShot(_commentClip);
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SlotSpeechSpeak(string text);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class AndroidTts
        {
            private readonly AndroidJavaObject _engine;
            private readonly InitProxy _proxy;
            private bool _ready;

            public AndroidTts()
            {
                try
                {
                    _proxy = new InitProxy(this);
                    using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                        _engine = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, _proxy);
                    }
                }
                catch (Exception error)
                {
                    Debug.LogWarning("[VOZ] TextToSpeech no arrancó: " + error.Message);
                }
            }

            public void MarkReady()
            {
                _ready = true;
                if (_engine == null)
                {
                    return;
                }

                try
                {
                    using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
                    {
                        AndroidJavaObject spanish = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", "es");
                        _engine.Call<int>("setLanguage", spanish);
                    }
                }
                catch (Exception error)
                {
                    Debug.LogWarning("[VOZ] Idioma no aplicado: " + error.Message);
                }
            }

            public bool Speak(string line)
            {
                if (!_ready || _engine == null)
                {
                    return false;
                }

                try
                {
                    _engine.Call<int>("speak", line, 0, null, "slot-line");
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public void Shutdown()
            {
                if (_engine == null)
                {
                    return;
                }

                try
                {
                    _engine.Call("shutdown");
                }
                catch (Exception)
                {
                    // El motor de voz ya estaba cerrado.
                }
            }

            private sealed class InitProxy : AndroidJavaProxy
            {
                private readonly AndroidTts _owner;

                public InitProxy(AndroidTts owner) : base("android.speech.tts.TextToSpeech$OnInitListener")
                {
                    _owner = owner;
                }

                public void onInit(int status)
                {
                    if (status == 0)
                    {
                        _owner.MarkReady();
                    }
                }
            }
        }
#endif
    }
}
