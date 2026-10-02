# ARTrackBuilder

Producto **phygital**: un tapete físico y una app de realidad aumentada. El niño corre carritos reales; el teléfono reconoce el tapete y proyecta la pista anclada al piso. El mismo proyecto también cronometra una competición slot por carriles.

El código está en [github.com/JDLBARRERA/APP-AUTOSGISMARKER](https://github.com/JDLBARRERA/APP-AUTOSGISMARKER).

Hay que abrirlo con **Unity 2022.3.62f1**. Unity 6 cambia la versión del proyecto. El editor correcto está en `C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe`.

## Qué hay hoy

El juego de gis y la competición slot conviven. El primero usa tiros de índice, fosos y zonas. El segundo usa vueltas y tiempo en hasta cuatro carriles. Ninguno de los dos detecta el auto físico: el jugador marca el tiro, la vuelta o la falta.

| Pieza | Dónde está |
| --- | --- |
| Unity 2022.3.62f1 | `ProjectSettings/ProjectVersion.txt` |
| AR Foundation, ARCore y ARKit 5.2.2 | `Packages/manifest.json` |
| 150 pistas (3 dificultades, 5 módulos, 10 carreras) | `Assets/Scripts/Data/RaceSeasonCatalog.cs` |
| Proyección de puntos, reglas y cinta de gis | `Assets/Scripts/AR/TrackWaypointProjection.cs`, `TrackChalkRibbon.cs` |
| Paisaje según el relieve y climas | `FloorReliefScanner.cs`, `LandscapeDirector.cs`, `LandscapeGround.cs` |
| Turno de 3 tiros, foso, tiro largo, mano no dominante | `Assets/Scripts/Core/RaceTurnController.cs` |
| Tablero de carrera y botones de reglas | `Assets/Scripts/UI/RaceDashboardUIManager.cs` |
| Reglamento | `Assets/Scripts/UI/RulebookUIManager.cs` |
| Competición slot: cronómetro, sesiones, récords, voz y pantalla | `Assets/Scripts/Competition/`, `Assets/Scripts/UI/SlotBroadcastUI.cs` |
| Modo celular o proyector de mesa | `Assets/Scripts/Core/DisplayModeManager.cs` |
| Historial de carreras | `race_history.json` vía `RaceHistoryManager.cs` |
| Pilotos, autos y récords de slot | `slot_archive.json` vía `SlotArchive.cs` |
| Escena, `MatDatabase` y APK en un teléfono | Todavía no probados en dispositivo |

`com.unity.xr.simulation` no va en el manifiesto: la versión 1.0.7 no está en el registro y Unity se queda en el error del Package Manager.

## Estructura

```
ARTrackBuilder/
├── .cursorrules
├── Packages/manifest.json
├── ProjectSettings/ProjectVersion.txt
├── Assets/
│   ├── Art/TrackModelSpec.md
│   ├── Editor/XrPluginBootstrap.cs
│   ├── Shaders/ProjectorVertexColor.shader
│   ├── Resources/ProjectorVertexColor.mat
│   ├── Plugins/iOS/SlotSpeech.mm
│   └── Scripts/
│       ├── AR/          tracking, proyección, cinta, paisaje
│       ├── Competition/ cronómetro, sesiones, archivo, voz
│       ├── Core/        turno, medidas, modo de pantalla
│       ├── Data/        150 pistas, historial, tienda
│       ├── Debugging/   simulador local
│       ├── Editor/      menús para armar la escena
│       └── UI/          tablero, reglamento, paisaje, slot
```

Los scripts bajo una carpeta `Editor` no entran en el build del teléfono. `Assets/Scripts/Debugging` sí entra; el teclado y el panel `OnGUI` van dentro de `UNITY_EDITOR`.

## Menús de Unity

| Menú | Qué deja listo |
| --- | --- |
| **ARTrackBuilder > Preparar Escena de Prueba Local** | Escena con cámara de proyector, tablero, paisajes y slot. La guarda en `Assets/Scenes/SampleScene.unity`. |
| **ARTrackBuilder > Configurar Escena Completa (1-Click)** | Enlaza la escena abierta: gestores, tablero, reglas, paisaje y slot. |
| **ARTrackBuilder > Generar UI Dashboard** | Solo el tablero de tiros. |
| **ARTrackBuilder > Validar Escena Antes de Compilar** | Revisa que los gestores estén en la escena. |
| **ARTrackBuilder > Limpiar y Optimizar Catálogo** | No fabrica prefabs. Las 150 pistas se arman en código al correr. |

Al pulsar Play, si la escena ya tiene `TrackWaypointProjection`, el juego carga solo la primera pista, la columna de reglas, la barra de climas y el tablero slot.

## Carrera de gis

Tres formatos. El nombre en pantalla es el de la izquierda; el enum interno sigue siendo `City`, `Medium` y `GrandPrix`.

| Formato | Vueltas | Qué lleva la pista |
| --- | --- | --- |
| CIRCUITO | 1 | Un exacto, una trampa de 1 tiro, un foso, un tiro largo, hielo y lluvia |
| GRAN CIRCUITO | 2 | Dos exactos, una trampa de 2 tiros, un foso, un tiro largo, lava y lluvia |
| GRAND PRIX | 3 | Dos exactos, trampas de 1, 2 y 3 tiros, dos fosos, dos tiros largos, lava, hielo y lluvia |

Módulos, en este orden: Óvalo, Ocho, Recta, Manzanas, Campeonato. Cada uno tiene 10 carreras. **SIGUIENTE CARRERA** avanza dentro del módulo y, al terminar, pasa al siguiente de la misma dificultad.

La cinta de gis mide 4,3 cm (`TrackConstants.TRACK_WIDTH_M`). Los puntos caben en ±0,42 m. 1 unidad de Unity es 1 metro. El tapete no pasa de 0,95 m.

Reglas que el tablero aplica a mano, porque la app no ve el carrito:

- Tres tiros por turno. Un tiro largo deja el siguiente en cuatro. El rebufo suma uno más.
- Al salir, el primero de la parrilla tira primero y lo sigue haciendo mientras siga líder. Al cerrar la vuelta de turnos, tira primero quien va primero en la carrera. **VA PRIMERO** marca un adelantamiento; el nuevo líder abre la vuelta siguiente.
- Salir del gis devuelve el auto al inicio del turno.
- El foso es zona prohibida: se pierde el turno siguiente.
- La trampa de tensión (1, 2 o 3 tiros) es otra regla. Si se falla, el auto vuelve al inicio de esa sección.
- Lava o hielo: el siguiente turno se tira con la mano no dominante.
- Lluvia: si el auto está en ese tramo, el turno queda en un tiro. El siguiente vuelve a tres, salvo que se marque otra vez.
- Tráfico gasta un tiro. El takedown quita el primer tiro del rival.

**¡YA TIRÉ!** avanza el tiro. Al completar el límite cambia el jugador. **PASAR TURNO** cierra antes. Los botones de la derecha solo aparecen si esa pista tiene la zona.

## Paisaje y proyector

**AUTO** lee el piso si la escena tiene `ARPlaneManager`:

- Plano: asfalto
- Pendiente: desierto
- Escalones: nieve
- Rugoso: bosque

**LLUVIA**, **HIELO**, **NIEVE**, **LAVA**, **DESIERTO**, **BOSQUE** y **NOCHE** fijan el color. **RELIEVE** recorre los cuatro suelos sin teléfono.

El modo proyector deja la cámara ortográfica con fondo negro. Lo que se ve es la cinta de color y el suelo. AirPlay y Chromecast, en esta entrega, son el espejo de pantalla del sistema.

## Competición slot

Es otro modo. No usa los tiros de índice.

1. Escribe piloto y auto en cada carril. Quedan en `slot_archive.json`.
2. **ENTRENO** guarda la mejor vuelta. **CLASIFICACION** ordena la parrilla por esa mejor vuelta. **CARRERA** corre hasta el número de vueltas (10 por defecto).
3. **V1**–**V4**, o las teclas 1–4 en el editor, marcan el paso por meta. El primer pulso solo arranca el cronómetro. El siguiente cierra la vuelta como `m:ss.fff`.
4. La posición de carrera sale de las vueltas completadas y, a igual vuelta, del tiempo al cerrar la última. Cuando un carril llega al límite, se cierra la sesión.
5. El resultado entra en `race_history.json`. El récord de la pista guarda el `TrackID` del catálogo.
6. **TORRE**, **PARRILLA** y **LIDER** cambian el tablero. Si Windows ve un segundo monitor, esa vista pasa a la pantalla 2.
7. Las frases de voz están en `VoiceCommentator` (`{piloto}`, `{n}`, `{tiempo}`). En el editor se lee el subtítulo. En Android e iOS las dice la voz del sistema.

## Cómo probarlo en el editor

1. Cierra cualquier diálogo de error del Package Manager y abre la carpeta con Unity 2022.3.62f1.
2. Espera a que importe AR Foundation 5.2.2.
3. Menú **ARTrackBuilder > Preparar Escena de Prueba Local**.
4. Pulsa Play. La cámara mira hacia abajo, el fondo es negro y sale **CIRCUITO · Óvalo · Carrera 1**.
5. **¡YA TIRÉ!** recorre los tiros y cambia de jugador. **SIGUIENTE CARRERA** cambia la pista y los botones de reglas.
6. En el tablero slot, **ENTRENO** y luego **V1** dos veces: la segunda marca la primera vuelta.

El tapete impreso, `MatDatabase` y la detección del auto siguen pendientes. Sin esa imagen de referencia, el teléfono no ancla la pista al tapete real. El simulador local (tecla M, panel de pruebas) no sustituye esa prueba.

## Reglas de código

Todo el C# va en el namespace `ARTrackBuilder` (`Core`, `AR`, `UI`, `Data`, `Competition`). La lógica de tracking, la de carrera y la de interfaz no comparten un solo script. Los componentes se avisan con `Action` o `UnityEvent`. No hay `Update()` salvo el mock del editor (teclas del slot y del simulador). Los modelos se anclan a un `ARAnchor`. El detalle está en `.cursorrules`.

Las medidas para modelar el gis y el tapete están en [Assets/Art/TrackModelSpec.md](Assets/Art/TrackModelSpec.md) y en `Assets/Scripts/Core/TrackConstants.cs`.
