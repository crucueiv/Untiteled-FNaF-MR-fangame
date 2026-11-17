# Untiteled FNaF MR Fangame

Experiencia de **Five Nights at Freddy's en Realidad Mixta (MR)** desarrollada en **Unity 6 (6000.3.18f1)** para **Meta Quest 3**. El proyecto combina passthrough a color en tiempo real, detección y reconstrucción espacial de la habitación física mediante Meta XR MR Utility Kit (MRUK), shaders personalizados de penumbra y colocación cinemática de animatrónicos en el entorno del jugador.

El proyecto se encuentra en fase **Alpha 0.1** (Sandbox de pruebas técnicas y mecánicas).

---

## Aviso Legal y Términos de Licencia

Este es un proyecto no oficial, desarrollado por fans y sin ánimo de lucro. 
- **Five Nights at Freddy's** es una creación de **Scott Cawthon** y propiedad intelectual de **Scottgames, LLC**. Este fangame no está afiliado, respaldado ni asociado con Scottgames ni con Steel Wool Studios.
- Los modelos tridimensionales de personajes fueron compilados por **JennyDPeriod** ([The FNaF Help Wanted MEGA FBX Pack Release](https://www.deviantart.com/jennydperiod/art/The-FNaF-Help-Wanted-MEGA-FBX-Pack-Release-983155006)).
- El código fuente original C# y los shaders desarrollados específicamente para este repositorio están licenciados bajo los términos de la **Licencia MIT** (ver [`LICENSE.md`](LICENSE.md)). Dicha licencia **no se extiende** a las marcas, personajes, modelos 3D, texturas ni efectos de sonido de la franquicia original.

---

## Estado Actual del Desarrollo (Alpha 0.1)

### ✅ Funcional y Verificado en Código
- **Menú principal 3D interactivo:** Puntero láser VR, retícula con retroalimentación háptica/visual, selección por gatillo y selector de escenas (`TestMenu.unity`).
- **Validación del entorno real:** `RoomValidator` verifica la presencia mínima de una puerta (`DOOR_FRAME`), una mesa (`TABLE`) y una cama/sofá (`BED`/`COUCH`) antes de activar el juego.
- **Posicionamiento cinemático de SpringBonnie:** `MRSpringBonniePlacement` y `SpringBonnieStaticPose` calculan el anclaje a la cama real y resuelven las piernas mediante cinemática inversa (IK) de 2 huesos contra el suelo.
- **Iluminación y Penumbra MR:** Oscurecimiento dinámico del passthrough real (`RoomLightManager`) y revelado de objetos virtuales mediante linterna con cookies de luz (`Custom/MR_UltimateDarknessRevealer`).
- **Oclusión por Profundidad en Tiempo Real:** Integración de la Meta Depth API con recortes precisos y desactivación temporal al sujetar objetos (`DepthOcclusionOnGrab`).
- **Inventario corporal de cintura:** Cinturón virtual con seguimiento de cabeza (`InventoryBodyFollower`) y acople magnético por slots (`InventorySlot`).
- **Manos esqueléticas FNaF:** Opción de sustituir los mandos por mallas articuladas de animatrónico con flexión analógica de dedos (`PlayerHandsVR`, `FnafHandPose`).

### ⏳ Pendiente de Implementación
- Navegación autónoma de animatrónicos sobre la malla de la habitación.
- Comportamientos reactivos de acecho en esquinas físicas y cruce agachado de puertas.
- Ciclo de noches con progresión horaria (12 AM a 6 AM), gasto de energía y condiciones de derrota (*jumpscares*).

---

## Requisitos del Proyecto

| Requisito | Valor / Versión Exacta |
|---|---|
| Motor de Desarrollo | Unity `6000.3.18f1` (Unity 6) |
| Sistema Operativo de Desarrollo | Arch Linux (o cualquier distribución Linux / Windows compatible) |
| Dispositivo de Destino | Meta Quest 3 |
| Plataforma de Compilación | Android (ARM64, Scripting Backend: IL2CPP) |
| Render Pipeline | Universal Render Pipeline (URP) `17.3.0` |
| SDK de Meta XR | Meta XR SDK All `209.0.0` / MR Utility Kit `209.0.0` |
| Interacción VR | XR Interaction Toolkit `3.4.1` / Meta Interaction SDK |
| Control de Versiones | Git con Git LFS obligatorio |

Las dependencias exactas se encuentran fijadas en [`Packages/manifest.json`](Packages/manifest.json) y [`Packages/packages-lock.json`](Packages/packages-lock.json).

---

## Instalación y Primeros Pasos

### 1. Clonar el Repositorio con Git LFS
```bash
git clone https://github.com/crucueiv/Untiteled-FNaF-MR-fangame.git
cd Untiteled-FNaF-MR-fangame
git lfs install
git lfs pull
```

### 2. Apertura en Unity Hub
1. Abre **Unity Hub** y añade la carpeta clonada.
2. Asegúrate de abrirlo con el editor **Unity 6000.3.18f1**.
3. Espera a que el Package Manager resuelva las dependencias e importe los assets.

### 3. Escenas del Proyecto
- `Assets/GameStuff/TestScenes/TestMenu.unity`: Menú principal y selector de niveles.
- `Assets/GameStuff/TestScenes/InitialTestRoom.unity`: Escena MR de supervivencia, linterna e inventario.
- `Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity`: Escena MR con SpringBonnie posado en la cama real.
- `Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity`: Galería para auditar los modelos de personajes y sus rigs (no incluida en la compilación final).

---

## Compilación y Despliegue en Meta Quest 3 (Arch Linux)

1. Conecta el visor Meta Quest 3 al equipo mediante cable USB-C con modo desarrollador habilitado.
2. Verifica la conexión por consola:
   ```bash
   adb devices
   ```
3. En Unity, abre **File > Build Profiles**, activa la plataforma **Android** y confirma que las 3 escenas principales estén marcadas.
4. Selecciona tu Quest 3 en el selector **Run Device** y pulsa **Build And Run** (o genera el archivo `.apk` y ejecuta `adb install -r juego.apk`).

Para una guía detallada con reglas de `udev` y depuración en Arch Linux, consulta [`docs/SETUP_LINUX_QUEST3.md`](docs/SETUP_LINUX_QUEST3.md).

---

## Controles de Juego

- **Navegación de Menú:** Apunta con el puntero láser de cualquier mando hacia los botones flotantes y presiona el gatillo (*Index Trigger*) para seleccionar.
- **Linterna:** Agarra la linterna con el botón de grip (*Hand Trigger*). Pulsa el thumbstick (*Primary/Secondary Thumbstick*) de la mano que la sujeta para conmutar el foco de luz. La linterna se apaga al soltarse.
- **Inventario:** Acerca cualquier objeto con la etiqueta `KeepableObject` a los slots situados en la cintura y suéltalo; el objeto quedará magnéticamente acoplado.
- **Menú de Diagnóstico:** Pulsa el botón `Start` del mando izquierdo para abrir o cerrar el panel flotante de depuración (permite alternar iluminación, provocar parpadeos y spawnear prefabs).

---

## Estructura del Repositorio

```text
├── Assets/
│   ├── GameStuff/               # Contenido original del juego
│   │   ├── Animations/          # Poses y Animator Controllers de animatrónicos
│   │   ├── Materials/           # Materiales URP y de menú
│   │   ├── Models/              # Mallas FBX de animatrónicos y manos
│   │   ├── Prefabs/             # Linterna, objetos interactivos y personajes
│   │   ├── Scenes/              # Escena MR de SpringBonnie
│   │   ├── Scripts/             # Código fuente C# propio del juego
│   │   ├── Shaders/             # Shaders URP propios de penumbra y oclusión
│   │   ├── Sounds/              # Efectos de sonido
│   │   ├── TestRoom/            # Malla y prefab de habitación para pruebas en Editor
│   │   └── TestScenes/          # Menú y sala inicial de pruebas MR
│   ├── Editor/                  # Herramientas de automatización de rigs
│   ├── MRTemplateAssets/        # Assets y scripts de plantilla MR de Meta
│   ├── MetaAssets/              # Prefabs y recursos del SDK de Meta
│   └── Plugins/Android/         # AndroidManifest.xml optimizado para Quest 3
├── docs/                        # Suite documental técnica completa
├── Packages/                    # Manifiesto y bloqueo de paquetes de Unity
└── ProjectSettings/             # Configuración del proyecto, URP y XR
```

---

## Suite Documental

- 🏛️ **[Arquitectura del Sistema](docs/ARQUITECTURA.md):** Flujo de ejecución, comunicación entre subsistemas y diagramas Mermaid.
- 🎮 **[Mini GDD Técnico](docs/GAMEPLAY.md):** Especificación de mecánicas implementadas vs pendientes.
- 📜 **[Referencia de Scripts](docs/SCRIPTS.md):** Catálogo de los 35 scripts C# propios con dependencias y serialización.
- 💡 **[Shaders e Iluminación](docs/SHADERS_Y_ILUMINACION.md):** Modelado de penumbra, revelado y Meta Depth API.
- 📦 **[Catálogo de Assets y Licencias](docs/ASSETS.md):** Inventario de modelos, texturas, audios y atribución.
- 🐧 **[Configuración en Arch Linux](docs/SETUP_LINUX_QUEST3.md):** Guía paso a paso para Unity Hub, adb y Meta Quest 3.
- ⚠️ **[Problemas Conocidos y Deuda Técnica](docs/PROBLEMAS_CONOCIDOS.md):** Limitaciones, riesgos de licencia y olores de código.
- 🗺️ **[Hoja de Ruta (Roadmap)](docs/ROADMAP.md):** Fases futuras de desarrollo planificadas.
- 🛠️ **[Guía de Contribución](CONTRIBUTING.md):** Estándares de desarrollo y directrices de Git.
- 📝 **[Historial de Cambios](CHANGELOG.md):** Registro de cambios por versión según Keep a Changelog.
- ⚖️ **[Términos de Licencia](LICENSE.md):** Licencia MIT para código original y descargo de responsabilidad.
- 🤖 **[Directrices para Agentes](AGENTS.md):** Guía operativa para asistentes de IA y herramientas automatizadas.
