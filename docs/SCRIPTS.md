# Referencia de Scripts — FNaF MR

Este documento cataloga los **35 scripts C# propios** del proyecto (30 en `Assets/GameStuff/Scripts/` y 5 en `Assets/Editor/`), detallando su responsabilidad, dependencias, atributos de componente y campos serializados en el Inspector.

---

## 1. Personajes y Cinemática MR (`GameStuff/Scripts/Characters/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `MRSpringBonniePlacement.cs`<br>`MRSpringBonniePlacement` | Coloca a SpringBonnie en el borde de la cama detectada por MRUK orientándolo hacia la zona despejada de paredes. | `[DisallowMultipleComponent]`<br>`[DefaultExecutionOrder(-200)]` | `SpringBonnieStaticPose`<br>`MRSpawnerReworked`<br>`Meta.XR.MRUtilityKit` | `pose` (SpringBonnieStaticPose), `cadera` (Transform), `camara` (Camera), `usarAnclaMRUK` (bool), `etiquetaCama` (string), `separacionDelBorde` (float), `alturaCaderaSobreCama` (float), `margenPared` (float), `proxyCama` (GameObject), `mostrarProxySiempre` (bool). |
| `SpringBonnieStaticPose.cs`<br>`SpringBonnieStaticPose` | Solucionador IK analítico de 2 huesos para hacer que las piernas de SpringBonnie apoyen los pies planos en el suelo. | `[ExecuteAlways]`<br>`[DisallowMultipleComponent]` | Ninguna (Unity Core) | `alturaSuelo` (float), `desfaseTobilloPlanta` (float), `poloRodilla` (Vector3), nombres de huesos (`huesoPelvis`, `sufijoDerecha`, `sufijoIzquierda`, `baseCadera`, `baseRodilla`, `baseTobillo`, `sufijoPuntaPie`), `aplicarEnEditor` (bool), referencias a huesos Transform. |

---

## 2. Diagnóstico y Depuración (`GameStuff/Scripts/Debbug/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `DebuggerHandToggle.cs`<br>`DebuggerHandToggle` | Alterna la visibilidad del Canvas de depuración al pulsar el botón Start del mando izquierdo. | Ninguno | `OVRInput` | `debuggerCanvas` (GameObject). |
| `MRDebuggerMenu.cs`<br>`MRDebuggerMenu` | Menú flotante para probar encendido, apagado, parpadeo de luces y spawn dinámico de prefabs. | Ninguno | `RoomLightManager` | `roomLightManager` (RoomLightManager), `spawnablePrefabs` (GameObject[]), `spawnPoint` (Transform). |

---

## 3. Rigs y Animación Procedural (`GameStuff/Scripts/DriveCharacters/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `IKTargetMarker.cs`<br>`IKTargetMarker` | Dibuja una esfera de gizmo coloreada en la posición del target IK para facilitar su edición en la vista de escena. | `[ExecuteAlways]` | Ninguna (Unity Core) | `color` (Color), `radius` (float). |
| `ProceduralIdle.cs`<br>`ProceduralIdle` | Genera una animación procedural de respiración en columna, oscilación de cabeza y fijación de pies al suelo con raycast. | `[DefaultExecutionOrder(-100)]` | Ninguna (Unity Physics) | `hips`, `spine`, `chest`, `neck`, `head` (Transforms), `leftFootTarget`, `rightFootTarget` (Transforms), `enableBreathing`, `breathOffset`, `breathFrequency`, `enableHeadSway`, `headSwayAngle`, `headSwayFrequency`, `enableFootPlanting`, `groundMask`, `castHeight`, `footOffset`, `maxSnapDistance`. |

---

## 4. Manos y Poses VR (`GameStuff/Scripts/Hands/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `FnafHandPose.cs`<br>`FnafHandPose` | Controla la flexión progresiva de los dedos esqueléticos de animatrónico a partir de entradas analógicas. | `[DisallowMultipleComponent]` | Ninguna (Unity Core) | `lado` (enum LadoMano), `sufijoHuesos` (string), flexión por falange (`proximal`, `media`, `distal`), `pesoGripEnIndice`, `aperturaDedos`, `invertirFlexion`, `invertirPulgar`, `velocidadDedos`. |
| `PlayerHandsVR.cs`<br>`PlayerHandsVR` | Sustituye los visuales del mando por prefabs de manos de FNaF, leyendo inputs de OVRInput para animar los dedos. | `[DisallowMultipleComponent]`<br>`[DefaultExecutionOrder(100)]` | `FnafHandPose`<br>`OVRInput` | `usarManosFNaF` (bool), `permitirCambioEnEjecucion` (bool), `raizManoDerecha`/`Izquierda` (Transforms), `poseDerecha`/`Izquierda` (FnafHandPose), offsets pos/rot, visuales de mandos originales. |

---

## 5. Interacciones y Oclusión (`GameStuff/Scripts/Interactions/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `DepthOcclusionOnGrab.cs`<br>`DepthOcclusionOnGrab` | Desactiva temporalmente la oclusión por profundidad del objeto mientras el jugador lo sostiene para evitar recortes erróneos con las manos. | `[DisallowMultipleComponent]`<br>`[DefaultExecutionOrder(50)]` | `MRDepthOcclusionController`<br>`Oculus.Interaction.GrabInteractable` | `renderers` (Renderer[]), `pares` (List<MaterialPair>), `releaseDelay` (float), `desactivado` (bool), `logsDetallados` (bool). |
| `InteractableHighlight.cs`<br>`InteractableHighlight` | Genera una cáscara con shader de contorno invertido aditivo unlit para resaltar objetos interactuables en la oscuridad. | `[DefaultExecutionOrder(-20)]` | `Oculus.Interaction.GrabInteractable` | `outlineColor` (Color HDR), `outlineWidth` (float), `pulsar` (bool), `pulseSpeed` (float), `pulseMin` (float), `mostrarAlActivar` (bool), `ocultarAlPrimerAgarre` (bool), `outlineMaterial` (Material). |

---

## 6. Inventario Físico (`GameStuff/Scripts/Inventory/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `InventoryBodyFollower.cs`<br>`InventoryBodyFollower` | Hace que la raíz del inventario siga la posición horizontal de la cabeza manteniendo una altura y suavizado constantes. | Ninguno | Ninguna (Unity Core) | `headTransform` (Transform), `heightOffset` (float), `smoothSpeed` (float). |
| `InventoryItem.cs`<br>`InventoryItem` | Define en qué tipos de slots (`Pocket`, `Shoulder`) puede guardarse el objeto y controla la reserva activa. | `[DisallowMultipleComponent]` | `InventorySlot` | `allowedSlots` (InventorySlotMask: Pockets, Shoulder, All). |
| `InventorySlot.cs`<br>`InventorySlot` | Detecta objetos con tag `KeepableObject` mediante triggers físicos, reserva el slot más cercano y fija el objeto al soltarlo. | `[RequireComponent(typeof(Collider))]` | `InventoryItem`<br>`Oculus.Interaction.GrabInteractable` | `slotKind` (InventorySlotKind), `snapPoint` (Transform), `snapPositionOffset` (Vector3), `snapRotationOffset` (Vector3), `keepableTag` (string). |

---

## 7. Objetos Interactivos y Spawning (`GameStuff/Scripts/Items/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `FlashlightController.cs`<br>`FlashlightController` | Conmuta el foco de luz de la linterna y garantiza que se apague automáticamente al soltarse. | Ninguno | Ninguna (Unity Light) | `spotlight` (Light). |
| `GrabbedObjectButton.cs`<br>`GrabbedObjectButton` | Mapea un botón de OVRInput (en la mano que sostiene el objeto) para disparar un evento de Unity (`UnityEvent`). | Ninguno | `Oculus.Interaction.GrabInteractable`<br>`OVRInput` | `button` (OVRInput.Button), `onButtonPressed` (UnityEvent). |
| `MRSpawnerReworked.cs`<br>`MRSpawnerReworked` | Instancia un prefab sobre la cara correcta de un volumen o plano de MRUK aplicando padding, margen y rotación. | Ninguno | `Meta.XR.MRUtilityKit` | `prefabToSpawn` (GameObject), `targetSurface` (MRUKAnchor.SceneLabels), `sliderX`/`Y`/`Z` (floats), `snapToSurface` (bool), `surfacePadding` (float), `borderMargin` (float), `rotationOffset` (Vector3). |

---

## 8. Orquestadores y Managers (`GameStuff/Scripts/Managers/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `GrabbableItemManager.cs`<br>`GrabbableItemManager` | Oculta el visual del mando correspondiente cuando el objeto es agarrado y despierta la física del Rigidbody al soltarlo. | Ninguno | `VisualsManager`<br>`Oculus.Interaction.GrabInteractable` | `grabAnchor` (Transform), `mandoIzquierdoVisual` (GameObject), `mandoDerechoVisual` (GameObject). |
| `MRDepthOcclusionController.cs`<br>`MRDepthOcclusionController` | Gestiona el ciclo de vida de la Meta Depth API, fijando el valor global `_EnvironmentDepthBias` y emitiendo eventos de estado. | `[DisallowMultipleComponent]` | `Meta.XR.EnvironmentDepth`<br>`OVRPassthroughLayer` | `depthManager` (EnvironmentDepthManager), `occlusionMode` (OcclusionShadersMode), `depthBias` (float), `disableWithoutPassthrough` (bool), `logState` (bool). |
| `RoomLightManager.cs`<br>`RoomLightManager` | Controla el oscurecimiento del passthrough real y la intensidad de la luz virtual principal; gestiona rutinas de parpadeo. | Ninguno | `OVRPassthroughLayer` | `darkBrightness` (float), `mainVirtualLight` (Light), `virtualLightOnIntensity` (float), `virtualLightOffIntensity` (float), `testFlickerDuration` (float), `testFlickerCount` (int). |
| `RoomValidator.cs`<br>`RoomValidator` | Escucha la carga de MRUK y valida la presencia obligatoria de puerta, mesa y cama o sofá antes de iniciar la experiencia. | Ninguno | `Meta.XR.MRUtilityKit` | `playerHead` (Transform), `isRoomValid` (bool), `closestTable` (MRUKAnchor). |
| `VisualsManager.cs`<br>`VisualsManager` | Almacén de referencias para los GameObjects visuales de los mandos izquierdo y derecho. | Ninguno | Ninguna | `leftVisual` (GameObject), `rightVisual` (GameObject). |

---

## 9. Menú e Interfaz 3D (`GameStuff/Scripts/Menu/`)

| Script / Clase | Responsabilidad | Atributos / Requisitos | Dependencias | Campos Principales en Inspector |
|---|---|---|---|---|
| `FnafGazeFollower.cs`<br>`FnafGazeFollower` | Orienta los ojos, cabeza y torso del personaje de menú suavemente hacia la posición de la cabeza del jugador. | `sealed` | Ninguna (Unity Core) | `playerTarget` (Transform), `headJoint` (Transform), `eyeJoints` (Transform[]), límites yaw/pitch, `responseSpeed` (float). |
| `FnafMenuCharacterPose.cs`<br>`FnafMenuCharacterPose` | Posiciona targets de cinemática inversa en el personaje del menú para fijar posturas de brazos, codos y piernas. | `sealed`<br>`[DefaultExecutionOrder(-60)]` | `Animator` | `animator`, targets de extremidades, `poseIndex`, parámetros de caída y apertura. |
| `FnafMenuController.cs`<br>`FnafMenuController` | Máquina de estados de UI del menú (`Splash`, `Main`, `Settings`, `SceneSelect`, `Transition`) y carga asíncrona de escenas. | `sealed` | `TMPro`<br>`UnityEngine.UI`<br>`SceneManagement` | Paneles `CanvasGroup`, `fadeOverlay` (Image), `pressPrompt` (TMP), botones UI, rutas de escenas (`testRoomScenePath`, `springBonnieScenePath`, `gameScenePath`). |
| `FnafMenuHands.cs`<br>`FnafMenuHands` | Apunta las manos y dedos del personaje del menú hacia un objetivo configurable. | `sealed`<br>`[DefaultExecutionOrder(100)]` | Ninguna (Unity Core) | `leftHand`, `leftMiddleFinger`, `rightHand`, `rightMiddleFinger`, flags de objetivo, `strength`. |
| `FnafMenuInput.cs`<br>`FnafMenuInput` | Genera punteros láser y retículas desde los controladores VR, con selección por gatillo y soporte de ratón en Editor. | `sealed` | `FnafMenuController`<br>`OVRInput`<br>`InputSystem` | `menu` (FnafMenuController), `viewCamera`, `leftPointer`, `rightPointer`, `playerHead`, `reticle`, `buttonLayer`, distancias y colores de láser. |
| `FnafMenuPalette.cs`<br>`FnafMenuPalette` | Clase estática con constantes cromáticas para la identidad visual de la interfaz. | `static class` | Ninguna | Constantes `Color` públicas (`Void`, `Wall`, `Curtain`, `StageMetal`, `Scarlet`, `Ember`, etc.). |
| `FnafMenuReach.cs`<br>`FnafMenuReach` | Cinemática para simular el alcance de las extremidades superiores hacia el jugador en el menú. | `sealed`<br>`[DefaultExecutionOrder(-45)]` | Ninguna (Unity Core) | `upperArm`, `lowerArm`, `hand`, `handTarget`, `playerHead`, offsets y velocidad. |
| `FnafPlayerTracker.cs`<br>`FnafPlayerTracker` | Rastrea la posición horizontal de la cabeza del usuario para guiar la orientación del torso del personaje. | `sealed`<br>`[DefaultExecutionOrder(-50)]` | Ninguna (Unity Core) | `playerHead`, `lookAtTarget`, `torsoRotateTarget`, límites angulares y velocidad. |

---

## 10. Herramientas de Editor (`Assets/Editor/` y `GameStuff/Scripts/Editor/`)

| Script / Clase | Responsabilidad | Menú / Invocación | Dependencias |
|---|---|---|---|
| `RigPoseWindow.cs`<br>`RigPoseWindow` | EditorWindow personalizada para previsualizar y ajustar poses e IK de SpringBonnie en el Editor. | `FNaF Tools/Rig Pose Editor` | `SpringBonnieStaticPose`<br>`UnityEditor` |
| `DriveCharacterCleanup.cs`<br>`DriveCharacterCleanup` | Elimina mallas y prefabs temporales generados durante el montaje de animatrónicos. | `Tools/Drive Characters/9 - Clean Temp Meshes` | `UnityEditor` |
| `DriveCharacterRebuild.cs`<br>`DriveCharacterRebuild` | Script maestro (1259 líneas) para reconstruir materiales, prefabs, rigs IK y escena showcase de animatrónicos. | `Tools/Drive Characters/...`<br>(1 a 6 y Rebuild All) | `IKTargetMarker`<br>`ProceduralIdle`<br>`UnityEngine.Animations.Rigging` |
| `DriveCharacterSetup.cs`<br>`DriveCharacterSetup` | Métodos estáticos de generación y auditoría de prefabs (`GenerateAll`, `ValidateAll`, `AuditMaterials`). | Invocación programática / CLI / MCP | `UnityEditor` |
| `ProbeDriveTargets.cs`<br>`ProbeDriveTargets` | Utilidad de diagnóstico para sondear targets IK en la escena showcase. | Invocación programática (`Execute()`) | Ninguna |
| `RunDriveRigRebuild.cs`<br>`RunDriveRigRebuild` | Entrypoint simplificado para reconstruir prefabs y reasociar rigs procedurales. | Invocación programática (`Execute()`) | `DriveCharacterRebuild` |
