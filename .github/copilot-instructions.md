# Copilot instructions for Untitled FNaF MR Fangame

## Project and workflow

- This is a Unity 6 project targeting Meta Quest mixed reality. Use the exact editor version from `ProjectSettings/ProjectVersion.txt`: **Unity 6000.3.18f1**.
- The project uses Android/ARM64, IL2CPP, URP, Meta XR SDK packages, OpenXR, MR Utility Kit, and XR Interaction Toolkit. Treat `Packages/manifest.json` and `Packages/packages-lock.json` as the authoritative dependency versions; do not infer versions from older prose in `README.md` or `GUIA_DESARROLLO.md`.
- Install Git LFS before working with the repository. Models, textures, audio, video, fonts, and other binary assets are LFS-managed. Keep every Unity `.meta` file with its asset because scene and prefab references depend on GUIDs.
- Do not commit generated Unity output (`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, builds, generated IDE projects) or Android signing files. The root `.gitignore` is the source of truth for ignored generated files.

## Build, test, and lint

- There are no repository build scripts, lint scripts, or tracked test assemblies. Build and test through the Unity Editor.
- Open the repository root in Unity Hub with `6000.3.18f1`. Wait for Package Manager/import errors to clear before editing.
- For a Quest build, open **File > Build Profiles**, switch to **Android**, verify the enabled scene list, select the Quest run device, and use **Build** or **Build And Run**. The current `ProjectSettings/EditorBuildSettings.asset` enables:
  - `Assets/GameStuff/TestScenes/TestMenu.unity`
  - `Assets/GameStuff/TestScenes/InitialTestRoom.unity`
- The game menu loads `Assets/GameStuff/TestScenes/InitialTestRoom` asynchronously, so keep that scene path and its Build Profiles entry synchronized if scenes are renamed.
- There are currently no project tests to run. If Unity tests are added, use the Test Runner's EditMode/PlayMode filters for a single test rather than assuming a full-suite command; no custom test runner or CI command exists in this repository.
- Validate gameplay changes on a real Quest when they involve passthrough, MRUK room data, controller/hand input, or permissions. Editor play mode is useful for menu and non-device logic but does not replace device validation.

## Architecture

- `Assets/GameStuff/TestScenes/TestMenu.unity` is the front-end scene. `FnafMenuController` owns the splash/main/settings/transition state machine, fades between panels, and loads the game scene. `FnafMenuInput` performs controller-pointer raycasts, trigger selection, reticle feedback, and an optional editor mouse fallback.
- `Assets/GameStuff/TestScenes/InitialTestRoom.unity` is the MR gameplay/test scene. Meta XR/MR Utility Kit creates the room anchors; `RoomValidator` listens for `MRUK.Instance.RoomCreatedEvent`, requires a door frame, table, and bed/couch, and records the closest table.
- MR spawning is event-driven from the same room-created callback. `MRSpawnerReworked` selects the nearest anchor matching a `SceneLabels` mask and places a prefab on a volume or plane, with surface snapping, border padding, face selection, and rotation offsets.
- `RoomLightManager` coordinates real-world passthrough brightness/contrast, the main Unity light, flickering coroutines, and the static `OnRoomLightStateChanged` event. `MRDebuggerMenu` exposes runtime light and spawn controls; `DebuggerHandToggle` opens it with the left controller Start button.
- Grabbable gameplay uses Meta/Oculus `GrabInteractable` components. `GrabbableItemManager` hides the corresponding controller visual and restores physics when an object is grabbed/released. `GrabbedObjectButton` determines the grabbing hand and maps a configured `OVRInput.Button` to a UnityEvent.
- The inventory is a physical trigger-slot system. `InventoryBodyFollower` follows the head horizontally around the waist; `InventorySlot` detects tagged objects, waits one fixed update after release, chooses the nearest compatible active slot, reserves the item, and snaps it; `InventoryItem` controls pocket/shoulder compatibility and prevents double reservation.
- Rendering is URP-based. Darkness and MR appearance are implemented in `Assets/GameStuff/Shaders` and scene/prefab materials; preserve the URP pipeline rather than introducing HDRP-specific solutions.

## Repository-specific conventions

- Gameplay scripts under `Assets/GameStuff/Scripts` are in the global namespace and are attached directly to scene or prefab objects. Follow the existing folder grouping (`Managers`, `Items`, `Inventory`, `Menu`, `Debbug`) instead of introducing a parallel architecture.
- Prefer Inspector-assigned references for scene and prefab dependencies. Existing scripts use `Camera.main`, `FindAnyObjectByType`/`FindFirstObjectByType`, and named-object lookup only as fallbacks or for runtime singletons; do not replace serialized references with fragile scene searches without a concrete reason.
- Unity-facing configuration is commonly public or `[SerializeField]` with Spanish `[Header]`/`[Tooltip]` text. Preserve serialized field names and types when possible so existing prefab and scene data is not lost.
- Subscribe to Meta/XR events in `OnEnable`/`Start` and unsubscribe in `OnDisable`/`OnDestroy` when the component owns the subscription. Room callbacks must tolerate missing MRUK/camera references and log a useful warning or error rather than silently failing.
- Use `OVRInput` for controller-specific button reads and `GrabInteractable.WhenStateChanged` for grab lifecycle. Determine left/right hand from the interactor hierarchy as the existing menu and grabbable systems do; do not assume one controller.
- Keep item storage behavior compatible with the `KeepableObject` tag, `InventoryItem` slot masks, and `Rigidbody` kinematic/velocity handling. Changes to inventory parenting or release timing can affect both physical grabbing and slot reservation.
- Existing code and comments mix English API identifiers with Spanish inspector text/comments. Preserve that style in `GameStuff` unless a broader localization change is explicitly requested.
- Keep scene, prefab, shader, and script changes surgical. Unity serializes references into YAML; never regenerate or rename assets casually, and inspect `.meta`/GUID impact when moving files.
