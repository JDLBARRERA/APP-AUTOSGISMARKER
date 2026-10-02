# ARTrackBuilder

Nombre temporal del producto **phygital** (tapete físico + app de realidad aumentada). Un niño juega con carritos reales sobre un tapete; la cámara del teléfono reconoce ese tapete y proyecta una pista interactiva anclada al mundo físico.

El objetivo de esta etapa es un prototipo de grado inversor: protegible, demostrable en una junta y listo para elegir entre licenciamiento (Mattel, Spin Master) o marca propia.

## Estado actual

El repositorio ya es un proyecto de Unity 2022.3 LTS con detección, proyección, pulso de neón y panel de fijación en código. La escena, la librería de imágenes, el prefab y el cableado del Inspector siguen pendientes.

| Pieza | Estado |
| --- | --- |
| Plan maestro (IP, MVP, go-to-market, nube) | Escrito en este README |
| Reglas de Cursor para C#, Unity y AR | `.cursorrules` |
| `.gitignore` de Unity | Listo |
| Proyecto Unity 2022.3.62f1 | `ProjectSettings/ProjectVersion.txt` |
| AR Foundation, ARCore y ARKit 5.2.2 | Declarados en `Packages/manifest.json` |
| Activación de ARCore (Android) y ARKit (iOS) | `Assets/Editor/XrPluginBootstrap.cs` (corre al abrir Unity) |
| Detección del tapete por eventos | `Assets/Scripts/AR/ImageTrackerManager.cs` |
| Proyección de la pista sobre el tapete | `Assets/Scripts/AR/TrackProjectorManager.cs` |
| Catálogo de pistas (siguiente / anterior) | `Assets/Scripts/Data/TrackDataManager.cs` |
| Generador de 1000 pistas | `Assets/Scripts/Editor/TrackCatalogGenerator.cs` |
| Historial local de carreras (JSON) | `Assets/Scripts/Data/RaceHistoryManager.cs` |
| Medidas reales de pista y tapete (1 unidad = 1 m) | [Assets/Art/TrackModelSpec.md](Assets/Art/TrackModelSpec.md) y `Assets/Scripts/Core/TrackPhysicalSpec.cs` |
| Pulso de neón (emisión HDR) | `Assets/Scripts/UI/NeonPulseEffect.cs` |
| Panel de opacidad y fijación | `Assets/Scripts/UI/TrackUIManager.cs` |
| Registro de jugadores y podio | `Assets/Scripts/UI/RaceUIManager.cs` |
| Escaparate virtual (catálogo + enlace web) | `Assets/Scripts/Data/StoreDataManager.cs` y `Assets/Scripts/UI/StoreUIManager.cs` |
| Reglamento oficial de carreras | `Assets/Scripts/UI/RulebookUIManager.cs` |
| Escena con AR Session y XR Origin | Pendiente en el editor |
| Librería `MatDatabase` y tamaño físico de la imagen | Pendiente en el editor |
| Prefab 3D, Canvas y cableado del Inspector | Pendiente en el editor |
| Pitch de 60 s | Pendiente |
| Patente, marca y NDA | Pendiente (fase legal) |

## Estructura del repositorio

```
ARTrackBuilder/
├── .cursorrules
├── .gitignore
├── README.md
├── Packages/
│   └── manifest.json
├── ProjectSettings/
│   └── ProjectVersion.txt
└── Assets/
    ├── Art/
    │   └── TrackModelSpec.md
    ├── Editor/
    │   └── XrPluginBootstrap.cs
    └── Scripts/
        ├── AR/
        │   ├── ImageTrackerManager.cs
        │   └── TrackProjectorManager.cs
        ├── Core/
        │   └── TrackPhysicalSpec.cs
        ├── Editor/
        │   └── TrackCatalogGenerator.cs
        ├── Data/
        │   ├── RaceHistoryManager.cs
        │   ├── StoreDataManager.cs
        │   └── TrackDataManager.cs
        └── UI/
            ├── NeonPulseEffect.cs
            ├── RaceUIManager.cs
            ├── RulebookUIManager.cs
            ├── StoreUIManager.cs
            └── TrackUIManager.cs
```

`Assets/Prefabs/AR/Catalog/` se crea al generar el catálogo. El script que los genera está en una carpeta `Editor` y no entra en el build del teléfono.

## Qué hace el código

`XrPluginBootstrap` (`ARTrackBuilder.Editor`) se ejecuta una vez al compilar el editor. Crea `Assets/XR/XRGeneralSettings.asset`, asigna `UnityEngine.XR.ARCore.ARCoreLoader` en Android y `UnityEngine.XR.ARKit.ARKitLoader` en iOS, y marca ARKit como obligatorio. En Android deja solo OpenGLES3, IL2CPP, ARM64 y API 24. En iOS fija la versión mínima 12.0, ARM64 y el permiso de cámara: «ARTrackBuilder usa la cámara para detectar el tapete de juego.» Si el identificador sigue siendo el de Unity, lo cambia a `com.TuNombre.ARTrackBuilder` (esa constante está en el bootstrap). Después cambia la plataforma activa a Android en Windows o a iOS en Mac.

`ImageTrackerManager` (`ARTrackBuilder.AR`) vive en el mismo objeto que `ARTrackedImageManager`. No usa `Update()`. Cachea el manager en `Awake` y publica tres eventos:

- `OnMatFound` cuando el tapete entra en cámara.
- `OnMatUpdated` cuando el tracking se mueve o cambia de ángulo.
- `OnMatLost` cuando el tapete sale de vista.

Si el tracking es `Limited`, avisa de luz pobre o imagen borrosa.

`TrackDataManager` (`ARTrackBuilder.Data`) guarda el catálogo en `_availableTracks`. Al iniciar selecciona la primera pista. `SelectNextTrack` y `SelectPreviousTrack` ciclan el índice y publican `OnTrackChanged` con el prefab elegido.

`TrackCatalogGenerator` añade el menú **ARTrackBuilder > Generar Catálogo de 1000 Pistas**. Crea `TRK_0001` … `TRK_1000`, los guarda en `Assets/Prefabs/AR/Catalog/` y reemplaza la lista del `TrackDataManager` que esté en la escena.

`RaceHistoryManager` (`ARTrackBuilder.Data`) guarda `race_history.json` en el almacenamiento del teléfono. `AddRaceResult` rechaza una carrera sin participantes, recorta la lista a 10 y publica `OnHistoryUpdated`. `GetSortedHistory` devuelve el más reciente primero. `ClearHistory` vacía el archivo.

`TrackPhysicalSpec` (`ARTrackBuilder.Core`) fija las medidas en metros: ancho 0.040–0.043, carril 0.032, grosor del gis 0.005–0.010, tapete de 1 m y caja máxima de 0.95 m. La guía para modelar está en [Assets/Art/TrackModelSpec.md](Assets/Art/TrackModelSpec.md).

`TrackProjectorManager` (`ARTrackBuilder.AR`) escucha al detector y al catálogo. Guarda el prefab actual y, si el tapete ya está visible, destruye la pista anterior e instancia la nueva como hija de la imagen rastreada, siempre con escala (1, 1, 1). Si la caja del mesh supera 0.95 m en X o Z, escribe un aviso. Si el tracking pasa a `Limited` o el tapete se pierde, oculta la pista.

`NeonPulseEffect` (`ARTrackBuilder.UI`) va en el prefab. En `Awake` cachea el `Renderer` e instancia su material. Una corrutina escribe `_EmissionColor` en HDR entre la intensidad mínima y la máxima. Al desactivar, apaga la emisión.

`TrackUIManager` (`ARTrackBuilder.UI`) escucha un `Slider` y un `Button`. El slider escribe `CanvasGroup.alpha`. El botón alterna «FIJAR PISTA» y «DESBLOQUEAR PISTA» y desactiva el GameObject de la AR Session para congelar el holograma. La opacidad solo afecta a un `CanvasGroup`; el disco 3D no se vuelve transparente hasta que ese grupo exista o un cambio posterior escriba el alfa del material.

`RaceUIManager` (`ARTrackBuilder.UI`) abre el registro y oculta el podio. `AddPlayer` pide nombre y auto, y se detiene en 10. Iniciar carrera solo se habilita con 2 o más jugadores. El podio llena un desplegable con los nombres y `SaveRaceResult` llama a `AddRaceResult` con la pista fija «Circuito Asfalto AR». Esa pista todavía no sale de `TrackDataManager`.

`StoreDataManager` (`ARTrackBuilder.Data`) guarda el catálogo local: tapetes, autos y gises, cada uno con precio, icono y URL de compra. `StoreUIManager` (`ARTrackBuilder.UI`) arma una tarjeta por producto. Comprar abre esa URL en el navegador (Shopify o Amazon) y no cobra dentro de la app. El pago y el envío quedan en la tienda web.

`RulebookUIManager` (`ARTrackBuilder.UI`) escribe el reglamento oficial en un `Text` y lo muestra al pulsar «Reglamento». El texto vive en el script: tiro de índice, límite de 3 tiros, regreso al punto cero, pivote, tráfico, takedown, rebufo, trampas de tensión, turbo AR, peligro AR y el árbitro de cámara.

## Cómo abrir el proyecto

1. Abre esta carpeta con **Unity 2022.3 LTS**. Si tu parche no es `2022.3.62f1`, Unity actualiza `ProjectVersion.txt` al abrir.
2. Espera a que el Package Manager resuelva AR Foundation, ARCore, ARKit 5.2.2 y XR Plug-in Management 4.4.0.
3. Confirma **Edit > Project Settings > XR Plug-in Management**: ARCore en la pestaña Android y ARKit en la de iOS.
4. Revisa **Project Validation**. Si Unity pide un API level o ARM64 más altos, usa Fix.

En el primer arranque el bootstrap deja los Player Settings del prototipo y cambia la plataforma: Android en Windows, iOS en Mac. Puedes confirmarlo en **File > Build Settings** y en **Player Settings**. El identificador de ejemplo, `com.TuNombre.ARTrackBuilder`, se cambia en la constante `APPLICATION_IDENTIFIER` de `Assets/Editor/XrPluginBootstrap.cs`.

Unity genera `Library/`, `Temp/` y `packages-lock.json` en el primer arranque. Esos archivos los ignora `.gitignore`.

### Escena y tapete (todavía a mano)

Cuando el bootstrap haya corrido:

1. Borra la Main Camera de la escena.
2. En la jerarquía: **XR > AR Session**, luego **XR > XR Origin (Mobile AR)**.
3. En el XR Origin, añade **AR Tracked Image Manager** y el componente `ImageTrackerManager`. Van en el mismo objeto.
4. **Assets > Create > XR > Reference Image Library**. Nómbrala `MatDatabase`. Añade una imagen de prueba y su tamaño físico (por ejemplo 0.5 m).
5. Arrastra `MatDatabase` al campo **Serialized Library** del AR Tracked Image Manager.

Sin esa imagen y sin esa asignación, el script compila pero no puede detectar el tapete.

### Prefab, neón y panel (todavía a mano)

1. Crea un disco (Cylinder aplastado) con un material brillante, añádele `NeonPulseEffect` y marca **Emission** en el material. Guárdalo en `Assets/Prefabs/AR/` y bórralo de la escena.
2. Crea un objeto vacío `AR_Managers`. Añádele `TrackDataManager`, `TrackProjectorManager`, `RaceHistoryManager` y `StoreDataManager`.
3. En `_availableTracks` agrega una entrada: ID `curva`, nombre `Curva Extrema` y el prefab de neón. Arrastra ese `TrackDataManager` al campo `_trackData` del proyector, y el `ImageTrackerManager` a `_imageTracker`.
4. Enlaza `SelectNextTrack` y `SelectPreviousTrack` a los botones Siguiente y Anterior.
5. Crea un Canvas con Slider, Button y Text (`UnityEngine.UI.Text`).
6. Pon `TrackUIManager` en ese Canvas. Arrastra el slider, el botón, el texto, un `CanvasGroup` si lo usas, y el objeto **AR Session** al campo `_arSession`.

### Registro y podio (todavía a mano)

1. Crea **UI > Canvas** y pon el Canvas Scaler en Scale With Screen Size.
2. Dentro del Canvas, crea `Panel_Registro` y `Panel_Podio`.
3. En `Panel_Registro`: dos `InputField` (nombre y auto), un `Text` para la lista, y los botones Añadir Jugador e Iniciar Carrera.
4. En `Panel_Podio`: un `Dropdown` y el botón Guardar Resultado.
5. Añade `RaceUIManager` a `AR_Managers`. Arrastra cada control a su campo y el `RaceHistoryManager` al campo `_historyManager`.

### Escaparate (todavía a mano)

1. En el Canvas, añade un botón «Tienda» y un panel `Panel_Tienda` con un botón cerrar y un objeto vacío con Grid Layout Group.
2. Crea un prefab de tarjeta con tres hijos nombrados exactamente `NameText`, `PriceText` y `BuyButton`.
3. Añade `StoreUIManager` a `AR_Managers`. Arrastra el panel, el prefab, la rejilla, los botones y el `StoreDataManager`.
4. En el catálogo de `StoreDataManager`, carga al menos un producto con su `PurchaseURL`.

El gancho de mostrar la tienda al terminar una carrera todavía no está cableado al historial.

### Reglamento (todavía a mano)

1. En el Canvas, añade un botón «Reglamento» y un panel con Scroll Rect. El texto no cabe en una sola pantalla.
2. Dentro del scroll, coloca un `Text` de alto contraste.
3. Añade `RulebookUIManager` a `AR_Managers`. Arrastra el panel, el texto y los botones de abrir y cerrar.

### Catálogo de 1000 pistas

1. Abre una escena que tenga un `TrackDataManager`.
2. Menú **ARTrackBuilder > Generar Catálogo de 1000 Pistas**.
3. Cada prefab trae cuatro nodos, todos dentro de ±0.40 m (por debajo de 0.95 m):
   - `Chalk_Path_Line`: carril de gis. El ancho real es 0.043 m. Salir de esa línea en cualquier tiro devuelve el auto al punto cero.
   - `RuleNode_TiroExacto`: casilla de 8 cm. El auto debe detenerse ahí para seguir tirando.
   - `RuleNode_TrampaTension_NTiros`: la sección se cruza en 1, 2 o 3 tiros. Si no, el auto vuelve al inicio de la trampa.
   - `ARZone_TurboBoost` o `ARZone_LavaPeligro`: el siguiente turno tiene 4 tiros, o se tira con la mano no dominante.

## Reglas de desarrollo

El código nuevo va en el namespace `ARTrackBuilder` (`Core`, `AR`, `UI`, `Data`). La lógica de tracking, la de pistas y la de interfaz no comparten el mismo script. Los componentes se hablan con `Action` o `UnityEvent`, no con `GetComponent` cruzado. Los modelos y las proyecciones se anclan a un `ARAnchor`. El tracking principal es **AR Image Tracking**; **AR Plane Tracking** queda como respaldo. El detalle está en `.cursorrules`.

## Especificaciones Físicas y Modelos 3D
Para garantizar que la proyección virtual encaje milimétricamente con los carritos físicos de Hot Wheels:
- Las reglas de modelado (escala 1:1, grosor del gis, conteo de polígonos y topología hueca) están documentadas de forma aislada para artistas 3D en `Assets/Art/TrackModelSpec.md`.
- Para evitar escalados "a ojo" en el editor de Unity que rompan la simetría, las dimensiones físicas de la pista y el límite del tapete están codificadas como constantes públicas en `Assets/Scripts/Core/TrackConstants.cs`.

## Plan maestro

Para llevar el producto a escala mundial y captar la atención de gigantes como Mattel, Hasbro o Disney, el enfoque debe pasar de la lluvia de ideas a una estrategia de ejecución corporativa. Un lanzamiento global requiere proteger la idea, construir un prototipo impecable y definir un modelo de negocio escalable.

### Fase 1: Blindaje legal y propiedad intelectual (IP)

Antes de mostrarle el producto a cualquier corporativo o inversor, la idea debe ser tuya legalmente. Las grandes empresas de juguetes no compran ideas sueltas; compran tecnología protegida y ejecuciones probadas.

- **Patente de utilidad.** Debes patentar el método de interacción. No puedes patentar "un tapete", pero sí puedes patentar "el sistema y método para utilizar marcadores físicos bidimensionales para calibrar y proyectar rutas de juego interactivas mediante realidad aumentada".
- **Registro de marca (trademark).** Crear un nombre comercial fuerte e independiente (por ejemplo, HoloTrack, ARena o GisPlay) y registrarlo a nivel internacional mediante el Protocolo de Madrid, cubriendo al menos Norteamérica, Europa y Asia.
- **Acuerdos de confidencialidad (NDA).** Cualquier ingeniero, diseñador o fábrica en China que toque tu prototipo debe firmar un contrato estricto de confidencialidad y cesión de derechos de autor.

### Fase 2: Desarrollo del prototipo "grado inversor" (MVP)

No necesitas la aplicación completa con 500 pistas, sino una demostración vertical perfecta (proof of concept) que funcione sin fallas durante una junta de negocios.

**El hardware (el tapete inteligente).** Fabricar 10 unidades de un tapete de neopreno oscuro o lona plastificada. El borde del tapete debe tener un patrón geométrico sutil (marcas fiduciarias) que la cámara detecte instantáneamente en cualquier condición de luz.

**El software (app demo).** Una aplicación en Unity con AR Foundation que haga tres cosas perfectas:

1. Reconocer el tapete en menos de 1 segundo.
2. Proyectar una pista de carreras básica con líneas de neón.
3. Mostrar un modelo 3D (un holograma de fuego o un aro de meta) anclado a la pista.

**El pitch video de 60 segundos.** Un video comercial de alta calidad de producción mostrando a un niño real jugando. Este video es el que se enviará a directivos e inversores. Muestra el problema (aburrimiento y espacio), la solución mágica (proyección) y la diversión física (los carritos).

### Fase 3: Rutas de go-to-market global

Para escalar mundialmente existen dos caminos principales. La estrategia inicial define cómo conseguir el capital para la producción en masa.

| Estrategia | Descripción | Ventajas | Desventajas |
| --- | --- | --- | --- |
| **Licenciamiento a terceros (B2B)** | Vender la tecnología y el concepto exclusivamente a marcas como Mattel (Hot Wheels) o Spin Master. Ellos se encargan de fabricar y distribuir con sus logos. | Cero costos de manufactura o logística. Acceso inmediato a distribución global y retail físico. | Cedes el control creativo. El margen de ganancia por unidad es menor (cobras regalías del 5% al 10%). |
| **Marca propia (direct-to-consumer)** | Lanzar el producto bajo tu propia marca mediante una campaña global de financiamiento (Kickstarter / Indiegogo) y vender en línea. | Retienes el 100% de la empresa, el control de la marca y márgenes de ganancia mucho más altos. | Requiere gestionar fábricas en Asia, logística mundial, aduanas y marketing internacional. |

### Fase 4: Infraestructura de la aplicación global

Una vez que el producto físico se distribuye, la aplicación debe soportar millones de usuarios simultáneos en diferentes idiomas.

- **Arquitectura en la nube.** Alojar el catálogo de pistas en servidores como AWS o Google Cloud. Esto permite actualizar el juego, añadir nuevos diseños o eventos de temporada (por ejemplo, pistas de Halloween) sin que el usuario tenga que actualizar la app completa desde la tienda.
- **Localización dinámica.** La interfaz y los tutoriales de la app deben detectar automáticamente el idioma del teléfono del usuario y adaptarse a inglés, español, mandarín, francés y alemán desde el día uno.
- **Economía digital integrada.** Si decides ir por el camino independiente, la app debe incluir una tienda digital (in-app purchases). El tapete básico es gratuito, pero descargar el diseño de la "Pista de Hielo" o efectos especiales para los videos cuesta dinero, generando ingresos pasivos recurrentes a nivel global.

Desarrollar esto a escala mundial une software de realidad aumentada con manufactura de juguetes. El siguiente paso tangible en Unity es armar la escena, la librería `MatDatabase` y enlazar el prefab con el panel.
