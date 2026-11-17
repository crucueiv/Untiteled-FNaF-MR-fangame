# Changelog

Todos los cambios relevantes de este proyecto se documentarán en este archivo siguiendo [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/). El proyecto aún no usa versiones de release formales (estado actual: `ALPHA 0.1`).

## [2026-11-17]

### Added
- Documentación técnica profesional ampliada: `docs/ARQUITECTURA.md`, `docs/GAMEPLAY.md`, `docs/SHADERS_Y_ILUMINACION.md`, `docs/ASSETS.md`, `docs/SETUP_LINUX_QUEST3.md`, `docs/PROBLEMAS_CONOCIDOS.md`, `docs/ROADMAP.md`, `CONTRIBUTING.md`, `LICENSE.md` y `AGENTS.md`.
- Comentarios de documentación XML (`/// <summary>`) en español añadidos a todas las clases y métodos públicos de scripts propios que carecían de ellos.

### Changed
- Actualizado `README.md` incorporando las 3 escenas habilitadas en compilación, catálogo de shaders y guía de despliegue en Arch Linux.
- Reorganizada la documentación previa: `docs/ARCHITECTURE.md` a `docs/ARQUITECTURA.md`, `docs/SCENES_AND_ASSETS.md` a `docs/ASSETS.md`, y `GUIA_DESARROLLO.md` a `docs/SETUP_LINUX_QUEST3.md`.
- Desvinculado `AGENTS.md` de `.gitignore` para su seguimiento en control de versiones.

### Fixed
- Corrección de oclusión en `DepthOcclusionOnGrab.cs`, `MR_LitOcclusion.shader` y `MR_LitOcclusionForwardPass.hlsl` para optimizar la compatibilidad con Meta Environment Depth API al sujetar y soltar objetos físicos.

---

## [2026-11-16]

### Added
- Escena de prueba `Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity` añadida y habilitada en `EditorBuildSettings.asset`.
- Script `MRSpringBonniePlacement.cs` para el posicionamiento automático de SpringBonnie en el borde de la cama detectada por MRUK.
- Script `SpringBonnieStaticPose.cs` con solucionador IK analítico de 2 huesos para asentar las piernas y pies en el suelo.
- Ventana de herramienta de editor `RigPoseWindow.cs` (`FNaF Tools/Rig Pose Editor`).
- Prefab `Characters/SpringBonnie_Suit.prefab` y modelo `HWSpringBonnieIMPROVED.fbx`.

### Changed
- Actualizaciones en `.gitignore` para refinar exclusiones de archivos generados por herramientas locales.

### Removed
- Eliminación de directorios temporales redundantes (`dd23495`).

---

## [2026-11-12]

### Added

- Documentación técnica inicial en `docs/ARCHITECTURE.md`, `docs/SCRIPTS.md` y `docs/SCENES_AND_ASSETS.md`.
- Escena de menú `TestMenu`, showcase de personajes y nuevos prefabs, materiales, animaciones y controladores de personajes detectados desde la última actualización documental.
- Créditos de modelos a JennyDPeriod y aviso legal del fangame.
- Actualizaciones en shaders URP (`Custom/MR_UltimateDarknessRevealer`, `Custom/MR_DirectLightOnly`, `Custom/MR_HighlightOutline`) y sistema de iluminación de la habitación.

### Changed

- README actualizado para reflejar Unity `6000.3.18f1`, URP 17.3.0, Meta XR 209.0.0, XR Interaction Toolkit 3.4.1 y las escenas realmente habilitadas.
- Documentada la separación entre contenido propio, plantillas Meta/XR y muestras de paquetes.

### Fixed

- Corregidas en la documentación las referencias obsoletas a Meta XR 203.0.0 y a una lista de compilación que no incluía el menú.

### Removed

- No se ha eliminado contenido del proyecto; esta entrada solo actualiza documentación.

---

## [2026-11-09] - [2026-11-11]

### Added
- Nuevo menú principal 3D interactivo en VR con selector de escenas (`TestMenu.unity`).
- Mallas y rigs de animatrónicos en `Assets/GameStuff/Models/DriveRigged/` (Freddy, Bonnie, Chica, Foxy, Endo, Golden Freddy y variantes Withered).
- Rigs procedurales y scripts de cinemática (`ProceduralIdle.cs`, `IKTargetMarker.cs`).
- Scripts de manipulación de mandos y manos virtuales (`PlayerHandsVR.cs`, `FnafHandPose.cs`).

### Changed
- Actualización de paquetes directos de Meta XR SDK a versión 209.0.0 y URP a 17.3.0.
- Preparación del entorno de desarrollo para portátiles y sistemas Linux.

---

## [2026-06-19] - [2026-06-27]

### Added
- Creación inicial del proyecto Unity MR para Meta Quest.
- Escena de prueba inicial `InitialTestRoom.unity`.
- Sistema de validación de habitación con MRUK (`RoomValidator.cs`).
- Sistema de control de iluminación y passthrough (`RoomLightManager.cs`).
- Linterna interactiva agarrable (`FlashlightController.cs`, `GrabbedObjectButton.cs`).
- Inventario corporal con slots magnéticos en cintura (`InventorySlot.cs`, `InventoryItem.cs`, `InventoryBodyFollower.cs`).
- Shaders iniciales de oscuridad y revelado MR.
