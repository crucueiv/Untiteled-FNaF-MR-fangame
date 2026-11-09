# Untitled FNaF MR Fangame

A **Five Nights at Freddy's** fan game built for **Meta Quest** using **Mixed Reality**. The game uses your real room as the environment, blending physical and virtual elements through passthrough AR.

> **¿Vas a continuar el proyecto en otro ordenador?** Sigue la [guía de desarrollo y puesta en marcha](GUIA_DESARROLLO.md) (español), con las versiones, módulos de Unity, Quest, Git y pasos de compilación de este proyecto.

## Features

- **Mixed Reality Room Detection** — Scans your real room using Meta's MR Utility Kit (MRUK) and validates it has the required furniture (door, table, bed/couch)
- **Smart Object Spawning** — Automatically places virtual objects on the nearest detected surface (e.g. table) with configurable positioning
- **Passthrough Lighting System** — Controls room brightness and flickering effects via OVRPassthroughLayer for horror atmosphere
- **Physical Inventory System** — Waist-mounted inventory that follows the player, with snap-to-slot item storage and release
- **Hand Tracking & Controller Support** — Grabbable items that hide the visual controller model while held
- **Grabbable Flashlight** — Controller-attached flashlight toggleable via thumbstick with grab detection using Meta's GrabInteractable
- **MR Debugger Menu** — In-game debug panel for testing light states, flicker effects, and spawning prefabs at runtime
- **Dynamic Darkness Revealer Shader** — Custom HLSL shader that reveals darkness where light doesn't reach, with shadow casting and light cookie support

## Requirements

- **Git + Git LFS** — models, textures, fonts and audio are stored in Git LFS
- **Unity Hub** with Android Build Support, Android SDK & NDK Tools, and OpenJDK installed for the project editor
- **Unity** 6000.3.18f1 (Unity 6)
- **Meta Quest** headset (Quest 3 / Quest 3S recommended for MR)
- **Meta XR SDK** v203.0.0
- **URP** (Universal Render Pipeline) 17.3.0
- **XR Interaction Toolkit** 3.4.1

## Project Structure

```
Assets/
├── GameStuff/
│   ├── GameScenes/              # Main game scenes
│   ├── TestScenes/              # InitialTestRoom (development scene)
│   ├── Scripts/
│   │   ├── Managers/
│   │   │   ├── RoomLightManager.cs      # Passthrough brightness & flicker effects
│   │   │   ├── VisualsManager.cs        # Controller visual references
│   │   │   ├── GrabbableItemManager.cs  # Handles grab events & controller visual hiding
│   │   │   └── RoomValidator.cs         # Validates MR room has required furniture
│   │   ├── Inventory/
│   │   │   ├── InventorySlot.cs          # Snap-to-slot inventory with grab/release
│   │   │   └── InventoryBodyFollower.cs # Waist inventory that smoothly follows the head
│   │   ├── Items/
│   │   │   ├── MRSpawnerReworked.cs     # Spawns objects on MR surfaces with precision
│   │   │   └── FlashlightController.cs  # Grabbable flashlight with thumbstick toggle
│   │   └── Debbug/
│   │       ├── MRDebuggerMenu.cs       # Debug panel for lights & spawning
│   │       └── DebuggerHandToggle.cs   # Toggles debugger menu via Start button
│   ├── Shaders/
│   │   ├── DarknessRevealer.shader      # Reveals darkness where light doesn't reach
│   │   └── HighlightsAndShadows.shader  # Meta MR highlights and shadows
│   ├── Prefabs/
│   │   └── GrabbableObject.prefab
│   ├── Materials/
│   └── Textures/
├── MRTemplateAssets/            # Meta MR template assets
├── Settings/                   # URP & XR settings
├── XR/                         # XR configuration
└── XRI/                        # XR Interaction setup
```

## Core Systems

### Room Validation (`RoomValidator`)
Scans the MR room at startup and checks for minimum requirements: at least 1 door, 1 table, and 1 bed/couch. Finds and stores a reference to the closest table for object placement.

### MR Spawning (`MRSpawnerReworked`)
Finds the nearest surface matching a target label (TABLE by default) and spawns a prefab at a configurable position. Supports `snapToSurface` mode that automatically places objects on top of furniture or against walls.

### Room Lighting (`RoomLightManager`)
Controls OVRPassthroughLayer brightness and contrast to simulate lights on/off. Includes a flicker coroutine for horror effects (configurable duration and frequency). Provides `OnRoomLightStateChanged(bool isOn)` event for other scripts to react.

### Inventory System (`InventorySlot` + `InventoryBodyFollower`)
A waist-mounted inventory that follows the player's head with smooth interpolation. Items tagged `KeepableObject` can be snapped into inventory slots and retrieved by grabbing them again.

### Grabbable Items (`GrabbableItemManager`)
Handles grab events from XR Interaction Toolkit and hides the controller visual model while an item is held.

### Grabbable Flashlight (`FlashlightController`)
Uses Meta's GrabInteractable to detect when the player picks up a flashlight. Toggles the spotlight on/off via thumbstick press (Left thumbstick for left hand, Right thumbstick for right hand). Automatically turns off when released to prevent lighting bugs.

### MR Debugger Menu (`MRDebuggerMenu` + `DebuggerHandToggle`)
Runtime debug panel for testing: toggles room lights on/off, triggers flicker effects, and spawns prefabs 1.5m in front of the player. Toggle visibility with the Start button on the left controller.

### Darkness Revealer Shader (`DarknessRevealer`)
Custom HLSL shader for URP with the following behavior:
- **Reveals darkness** in areas not reached by light (inverse of traditional transparency)
- **Light cookie support** — projects flashlight/spotlight patterns onto the darkness
- **Shadow casting and receiving** — properly integrates with Unity's shadow system
- **Premultiplied alpha** — correctly blends with the real world passthrough
- **Supports all light types** (directional, point, spot) with proper attenuation

**Shader Properties:**
| Property | Type | Description |
|----------|------|-------------|
| `_DarknessColor` | Color | Base color of the darkness (default black) |
| `_LightSensitivity` | Range(0.1, 10) | How sensitive the reveal is to light intensity |
| `_TintStrength` | Range(0, 1) | Subtle color variation based on nearby light color |
| `_CookieStrength` | Range(0, 5) | Intensity multiplier for light cookies |

## Setup

For a reproducible setup on a new computer, including Android build modules and headset deployment, follow [`GUIA_DESARROLLO.md`](GUIA_DESARROLLO.md).

Quick start: clone this repository, open it with Unity **6000.3.18f1**, wait for Package Manager to finish resolving the locked dependencies, then open `Assets/GameStuff/TestScenes/InitialTestRoom.unity`.

## Room Setup

For the best experience, set up your Meta Quest guardian in a room that has:
- At least one table
- A bed or couch
- A door frame

The game validates these elements on startup and will warn if the room doesn't meet requirements.

## Architecture Notes

- **Engine**: Unity 6000.3.18f1 (Unity 6) with URP 17.3.0
- **Platform**: Meta Quest (MR passthrough AR) — Meta XR SDK v203.0.0
- **XR**: XR Interaction Toolkit 3.4.1, OpenXR, AR Foundation
- **Dev scene**: `Assets/GameStuff/TestScenes/InitialTestRoom.unity`
- Spanish variable/comment naming in GameStuff scripts
- Meta XR SDK packages under `com.meta.xr.*`
- URP render pipeline (not HDRP)

## License

This is a fan-made project. **Five Nights at Freddy's** is owned by Scott Cawthon. This project is not affiliated with or endorsed by Scott Cawthon.
