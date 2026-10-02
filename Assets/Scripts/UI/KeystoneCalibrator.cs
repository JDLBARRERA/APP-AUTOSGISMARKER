using UnityEngine;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Ajusta escala y rotación de la pista para encajar la proyección sobre una mesa inclinada.
    /// </summary>
    public class KeystoneCalibrator : MonoBehaviour
    {
        [Header("Ajuste de Proyección")]
        [SerializeField] private Transform _trackContainerTransform;
        [SerializeField] private float _moveStep = 0.01f;

        public void WidenTrack()
        {
            ScaleTrackWidth(_moveStep);
        }

        public void NarrowTrack()
        {
            ScaleTrackWidth(-_moveStep);
        }

        public void LengthenTrack()
        {
            ScaleTrackHeight(_moveStep);
        }

        public void ShortenTrack()
        {
            ScaleTrackHeight(-_moveStep);
        }

        public void ScaleTrackWidth(float delta)
        {
            if (_trackContainerTransform == null) return;
            Vector3 currentScale = _trackContainerTransform.localScale;
            currentScale.x = Mathf.Clamp(currentScale.x + delta, 0.5f, 2.0f);
            _trackContainerTransform.localScale = currentScale;
        }

        public void ScaleTrackHeight(float delta)
        {
            if (_trackContainerTransform == null) return;
            Vector3 currentScale = _trackContainerTransform.localScale;
            currentScale.z = Mathf.Clamp(currentScale.z + delta, 0.5f, 2.0f);
            _trackContainerTransform.localScale = currentScale;
        }

        public void RotateTrack(float angle)
        {
            if (_trackContainerTransform == null) return;
            _trackContainerTransform.Rotate(Vector3.up, angle);
        }
    }
}
