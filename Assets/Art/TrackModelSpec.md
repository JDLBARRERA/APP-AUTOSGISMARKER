# Especificaciones de Modelado 3D (ARTrackBuilder)

Este documento define las reglas estrictas para la creación de modelos 3D que se proyectarán como plantillas de trazado (gis) en Realidad Aumentada. Los modelos deben coincidir matemáticamente con los juguetes físicos Hot Wheels.

## 1. Escala y Dimensiones (Regla 1:1)
- **1 Unidad en Blender/Maya = 1 Metro real.**
- **Ancho Total de Pista:** 0.043m (4.3 cm).
- **Carril Interior (Llantas):** 0.032m (3.2 cm).
- **Grosor de Línea de Trazado (Gis):** 0.01m (1 cm).
- **Área Máxima (Bounding Box):** 0.95m x 0.95m (para evitar que la proyección exceda un tapete físico de 1x1m).

## 2. Pivote y Topología
- **Origen (Pivote):** Exactamente en la base inferior y centrado en la geometría (X=0, Y=0, Z=0). Esto evita que el holograma "flote" o tiemble al rotar.
- **Conteo de Polígonos:** Máximo 500 triángulos por pieza de pista.
- **Planimetría:** Solo se modela el contorno/esqueleto. Sin caras de relleno para no obstruir la visión de la cámara hacia el suelo. Eje Y siempre al ras (sin elevación, a menos que se indique como modelo de rampa).

## 3. Exportación y Materiales
- **Materiales:** 1 solo Slot de material. Usar nombres genéricos (ej. `Mat_Neon`). No incluir texturas PBR, solo color plano para shaders de Emisión.
- **Formato:** `.FBX`. Aplicar siempre la escala y rotación antes de exportar (`Ctrl+A > All Transforms` en Blender).
