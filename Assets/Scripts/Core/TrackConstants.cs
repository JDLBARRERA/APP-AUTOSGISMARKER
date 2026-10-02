namespace ARTrackBuilder.Core
{
    /// <summary>
    /// Contiene las dimensiones matemáticas exactas del mundo físico para validaciones.
    /// 1 unidad flotante = 1 metro en AR Foundation.
    /// </summary>
    public static class TrackConstants
    {
        // Ancho total de la pista de plástico estándar de Hot Wheels
        public const float TRACK_WIDTH_M = 0.043f;
        
        // Espacio interno donde corren las llantas del carrito
        public const float INNER_LANE_WIDTH_M = 0.032f;
        
        // Grosor simulado del trazo de gis/tiza en la calle
        public const float CHALK_THICKNESS_M = 0.010f;
        
        // Límite del tapete físico base (1 metro cuadrado con margen de seguridad)
        public const float MAX_BOUNDING_BOX_M = 0.950f;
    }
}
