# Untitled FNaF MR Fangame

A **Five Nights at Freddy's** fan game built for **Meta Quest** using **Mixed Reality**. The game uses your real room as the environment, blending physical and virtual elements through passthrough AR.

## Features

- **Mixed Reality Room Detection** — Scans your real room using Meta's MR Utility Kit (MRUK) and validates it has the required furniture (door, table, bed/couch)
- **Smart Object Spawning** — Automatically places virtual objects on the nearest detected surface (e.g. table) with configurable positioning
- **Passthrough Lighting System** — Controls room brightness and flickering effects via OVRPassthroughLayer for horror atmosphere
- **Physical Inventory System** — Waist-mounted inventory that follows the player, with snap-to-slot item storage and release
- **Hand Tracking & Controller Support** — Grabbable items that hide the visual controller model while held

## Requirements

- **Unity** 6000.3.18f1 (Unity 6)
- **Meta Quest** headset (Quest 3 / Quest 3S recommended for MR)
- **Meta XR SDK** v203.0.0
- **URP** (Universal Render Pipeline) 17.3.0
- **XR Interaction Toolkit** 3.4.1

## Project Structure

```
Assets/
├── GameStuff/
│   ├── GameScenes/          # Main game scenes
│   ├── TestScenes/          # InitialTestRoom (development scene)
│   ├── Scripts/
│   │   ├── RoomValidator.cs         # Validates MR room has required furniture
│   │   ├── MRSpawnerReworked.cs     # Spawns objects on MR surfaces with precision
│   │   ├── RoomLightManager.cs      # Passthrough brightness & flicker effects
│   │   ├── InventorySlot.cs         # Snap-to-slot inventory with grab/release
│   │   ├── InventoryBodyFollower.cs# Waist inventory that smoothly follows the head
│   │   ├── GrabbableItemManager.cs # Handles grab events & controller visual hiding
│   │   └── VisualsManager.cs        # Controller visual references
│   ├── Prefabs/
│   │   └── GrabbableObject.prefab
│   ├── Materials/
│   │   └── InventoryMatirialALPHA.mat
│   └── Textures/
├── MRTemplateAssets/       # Meta MR template assets
├── Settings/               # URP & XR settings
├── XR/                     # XR configuration
└── XRI/                    # XR Interaction setup
```

## Core Systems

### Room Validation (`RoomValidator`)
Scans the MR room at startup and checks for minimum requirements: at least 1 door, 1 table, and 1 bed/couch. Finds and stores a reference to the closest table for object placement.

### MR Spawning (`MRSpawnerReworked`)
Finds the nearest surface matching a target label (TABLE by default) and spawns a prefab at a configurable position. Supports `snapToSurface` mode that automatically places objects on top of furniture or against walls.

### Room Lighting (`RoomLightManager`)
Controls OVRPassthroughLayer brightness and contrast to simulate lights on/off. Includes a flicker coroutine for horror effects (configurable duration and frequency).

### Inventory System (`InventorySlot` + `InventoryBodyFollower`)
A waist-mounted inventory that follows the player's head with smooth interpolation. Items tagged `KeepableObject` can be snapped into inventory slots and retrieved by grabbing them again.

## Setup

1. Clone this repository
2. Open the project in Unity 6000.3.18f1
3. Ensure Meta Quest Link is set up and Passthrough is enabled
4. Open `Assets/GameStuff/TestScenes/InitialTestRoom.unity`
5. Build and run on your Quest headset

## Room Setup

For the best experience, set up your Meta Quest guardian in a room that has:
- At least one table
- A bed or couch
- A door frame

The game validates these elements on startup and will warn if the room doesn't meet requirements.

## License

This is a fan-made project. **Five Nights at Freddy's** is owned by Scott Cawthon. This project is not affiliated with or endorsed by Scott Cawthon.
