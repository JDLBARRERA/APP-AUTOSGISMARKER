using UnityEngine;

namespace ARTrackBuilder.Core
{
    /// <summary>
    /// Medidas reales de la pista y del tapete. Una unidad de Unity equivale a un metro.
    /// Los prefabs se instancian en <see cref="PREFAB_SCALE"/>; no se reescalan en código.
    /// </summary>
    public static class TrackPhysicalSpec
    {
        public const float TRACK_WIDTH_MIN_METERS = 0.040f;
        public const float TRACK_WIDTH_MAX_METERS = 0.043f;
        public const float LANE_WIDTH_METERS = 0.032f;
        public const float LINE_THICKNESS_MIN_METERS = 0.005f;
        public const float LINE_THICKNESS_MAX_METERS = 0.010f;
        public const float MAT_SIZE_METERS = 1f;
        public const float TRACK_BOUNDS_MAX_METERS = 0.95f;
        public const int MAX_TRIANGLES_PER_PIECE = 500;

        public static readonly Vector3 PREFAB_SCALE = Vector3.one;
    }
}
