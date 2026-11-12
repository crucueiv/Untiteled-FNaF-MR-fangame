# Untitled FNaF MR Fangame

Proyecto de **Five Nights at Freddy's en realidad mixta para Meta Quest**, todavía en desarrollo. La experiencia combina passthrough, detección de la habitación del jugador y objetos virtuales interactivos. El estado actual es **alpha/WIP** (`ALPHA 0.1`).

<!-- MANUAL:START -->
## Aviso legal y créditos

Este es un juego de fans, no oficial y sin ánimo de lucro. **Five Nights at Freddy's** es una creación de Scott Cawthon y propiedad de **Scottgames, LLC**. Este proyecto no está afiliado a Scottgames ni a Steel Wool Studios.

Los modelos de personajes proceden de [The FNaF Help Wanted MEGA FBX Pack Release](https://www.deviantart.com/jennydperiod/art/The-FNaF-Help-Wanted-MEGA-FBX-Pack-Release-983155006), de JennyDPeriod.

El proyecto es una obra de fans y no reclama la propiedad de las marcas o personajes de Five Nights at Freddy's.
<!-- MANUAL:END -->

## Características principales

- Detección de habitación mediante Meta XR MR Utility Kit (MRUK).
- Validación de puerta, mesa y cama/sofá antes de iniciar la prueba MR.
- Spawning sobre superficies detectadas, con selección de etiqueta, ajuste a superficie, margen y rotación.
- Sistema de iluminación virtual y passthrough con estados encendido/apagado y parpadeo.
- Inventario físico de cintura con slots de bolsillo/hombro y reserva de objetos al soltarlos.
- Objetos agarrables y linterna controlada por el thumbstick de la mano que la sostiene.
- Menú 3D con puntero de controlador, retícula, selección por trigger y fallback de ratón en el Editor.
- Personajes riggeados, poses y controladores Animator para la escena de showcase.
- Shaders propios para oscuridad, luces y sombras en URP.

## Requisitos

| Componente | Versión o valor |
|---|---|
| Unity Editor | `6000.3.18f1` |
| Plataforma objetivo | Android / Meta Quest |
| Arquitectura Android | ARM64 |
| Backend de scripts | IL2CPP |
| Render pipeline | Universal Render Pipeline 17.3.0 |
| Meta XR | `com.meta.xr.sdk.all` 209.0.0 |
| XR Interaction Toolkit | 3.4.1 |
| Android | Android Build Support, SDK & NDK Tools y OpenJDK incluidos con Unity |
| Control de versiones | Git y Git LFS |

El proyecto usa además OpenXR, Meta OpenXR, AR Foundation, XR Hands, Composition Layers y el plugin Aura. Las versiones exactas y las dependencias transitivas están en [`Packages/manifest.json`](Packages/manifest.json) y [`Packages/packages-lock.json`](Packages/packages-lock.json).

## Instalación y primeros pasos

1. Instala Git LFS y ejecuta `git lfs install`.
2. Clona el repositorio y ábrelo con Unity `6000.3.18f1`.
3. Espera a que Package Manager termine de resolver paquetes y a que Unity importe los assets.
4. Abre `Assets/GameStuff/TestScenes/TestMenu.unity` para probar el flujo de menú o `Assets/GameStuff/TestScenes/InitialTestRoom.unity` para probar la escena MR.
5. Para Quest, cambia a Android en **File > Build Profiles**, verifica las escenas habilitadas, conecta el dispositivo y usa **Build** o **Build And Run**.

La detección de habitación, el passthrough, los permisos y la interacción XR deben validarse en un Quest real. La guía operativa de cambio de ordenador está en [`GUIA_DESARROLLO.md`](GUIA_DESARROLLO.md).

## Estructura del proyecto

```text
Assets/
├── GameStuff/
│   ├── TestScenes/              # Menú, prueba MR y showcase de personajes
│   ├── Scripts/                 # Sistemas propios de menú, MR, inventario y objetos
│   ├── Prefabs/                 # Objetos agarrables, MRThings y personajes
│   ├── Models/                  # Modelos y texturas de personajes
│   ├── Animations/              # Poses y Animator Controllers
│   ├── Materials/               # Materiales del juego y del menú
│   ├── Shaders/                 # DarknessRevealer y HighlightsAndShadows
│   └── Textures/                # Texturas propias
├── Editor/                      # Herramientas de reconstrucción de personajes
├── MRTemplateAssets/            # Assets y scripts de plantilla MR
├── MetaAssets/                  # Prefabs y materiales de integración Meta
├── Plugins/Android/             # AndroidManifest.xml
├── Settings/, XR/, XRI/         # Configuración de render y XR
└── Samples/                     # Muestras importadas de paquetes Unity/XR
```

La descripción técnica está dividida en [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), [`docs/SCRIPTS.md`](docs/SCRIPTS.md) y [`docs/SCENES_AND_ASSETS.md`](docs/SCENES_AND_ASSETS.md).

## Controles y uso

- **Menú:** apunta con el controlador izquierdo o derecho; el puntero hace raycast sobre la capa de botones y el trigger selecciona. En el Editor puede usarse el fallback de ratón si está habilitado.
- **Inicio:** `Start Game` carga `InitialTestRoom` de forma asíncrona.
- **Linterna:** el thumbstick de la mano que la sostiene alterna el foco. La linterna se apaga al soltarse.
- **Depuración:** el botón Start del controlador izquierdo abre/cierra el panel de depuración; permite cambiar la luz, lanzar un parpadeo y crear prefabs.
- **Inventario:** los objetos con tag `KeepableObject` pueden reservarse en slots compatibles al soltarlos.

En `InitialTestRoom`, `RoomValidator` requiere una puerta, una mesa y una cama o sofá detectados por MRUK. Si no se cumplen esos elementos, la escena registra el estado de validación y no debe considerarse una prueba MR válida.

## Dependencias y paquetes

Paquetes directos relevantes:

- Meta XR SDK All 209.0.0, MR Utility Kit 209.0.0 y Meta OpenXR 2.7.0.
- XR Interaction Toolkit 3.4.1, XR Hands 1.7.3, OpenXR 1.16.1 y AR Foundation 6.6.2.
- URP 17.3.0, Animation Rigging 1.4.1 e Input System 1.19.0.
- Aura para Unity y Autopilot desde repositorios Git.
- Unity MCP desde repositorio Git.

La lista completa, incluidos módulos Unity y versiones transitivas, debe consultarse en los archivos de `Packages/`; no se mantiene una segunda lista manual.

## Roadmap y pendientes conocidos

- Completar y validar el flujo de juego más allá de las escenas de prueba.
- Definir el contenido funcional de la pantalla de ajustes del menú.
- Validar en dispositivo la detección de habitación, passthrough, permisos, manos/controladores y rendimiento.
- Revisar la organización de escenas `_Recovery` y el uso de herramientas de editor antes de convertirlas en contenido de producción.

Estos puntos son estado de desarrollo observado, no compromisos de fecha.

## Documentación adicional

- [`GUIA_DESARROLLO.md`](GUIA_DESARROLLO.md): instalación, Git LFS y despliegue en Quest.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): arquitectura y flujo entre sistemas.
- [`docs/SCRIPTS.md`](docs/SCRIPTS.md): responsabilidades y configuración de scripts propios.
- [`docs/SCENES_AND_ASSETS.md`](docs/SCENES_AND_ASSETS.md): escenas, prefabs y organización de assets.
- [`CHANGELOG.md`](CHANGELOG.md): cambios documentados por versión.
