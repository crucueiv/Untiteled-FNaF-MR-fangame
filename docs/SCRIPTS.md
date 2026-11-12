# Scripts y sistemas

Los scripts propios de `Assets/GameStuff/Scripts` usan el espacio de nombres global y se asignan directamente a escenas o prefabs. Los campos indicados son los públicos o serializados relevantes observados en el código; los valores concretos se configuran por escena/prefab.

## Menú

| Script | Propósito y configuración |
|---|---|
| `Menu/FnafMenuController.cs` | Máquina de estados de splash, menú y ajustes. Recibe `CanvasGroup`, overlay de fade, textos y botones; `gameScenePath` apunta por defecto a `Assets/GameStuff/TestScenes/InitialTestRoom`. |
| `Menu/FnafMenuInput.cs` | Raycast de punteros izquierdo/derecho, retícula, laser y selección por trigger. Usa cámara, transforms de puntero/cabeza, capa de botones, distancia y fallback de ratón del Editor. |
| `Menu/FnafGazeFollower.cs` | Limita la rotación de cuerpo, cabeza y ojos hacia el jugador. |
| `Menu/FnafPlayerTracker.cs` | Sigue la cabeza del jugador para orientar torso y objetivo visual. |
| `Menu/FnafMenuCharacterPose.cs` | Aplica poses a un `Animator` usando targets de manos, codos, pies y torso. |
| `Menu/FnafMenuHands.cs` | Orienta manos y dedos hacia un objetivo configurable. |
| `Menu/FnafMenuReach.cs` | Calcula el alcance de brazos y manos del personaje de menú. |
| `Menu/FnafMenuPalette.cs` | Define colores estáticos de la identidad visual del menú. |

## MR y managers

| Script | Propósito y configuración |
|---|---|
| `Managers/RoomValidator.cs` | Escucha `MRUK.Instance.RoomCreatedEvent`, comprueba puerta, mesa y cama/sofá, y expone `isRoomValid` y `closestTable`. |
| `Managers/RoomLightManager.cs` | Ajusta passthrough y luz virtual, ejecuta parpadeos y emite `OnRoomLightStateChanged(bool)`. Configura brillo oscuro, luz principal, intensidades y duración/conteo de parpadeo. |
| `Managers/VisualsManager.cs` | Conserva las referencias visuales de los controladores izquierdo y derecho. |
| `Managers/GrabbableItemManager.cs` | Se suscribe a `GrabInteractable.WhenStateChanged`, oculta el visual del controlador de la mano que agarra y lo restaura al liberar. |
| `Debbug/MRDebuggerMenu.cs` | Panel de prueba para luz, parpadeo y spawning de prefabs. Recibe `RoomLightManager`, prefabs y un punto de spawn opcional. |
| `Debbug/DebuggerHandToggle.cs` | Abre o cierra el Canvas de depuración con el botón Start del controlador izquierdo. |

## Objetos e inventario

| Script | Propósito y configuración |
|---|---|
| `Items/MRSpawnerReworked.cs` | Selecciona una etiqueta de superficie MRUK, por defecto `TABLE`, y coloca `prefabToSpawn` con offsets X/Y/Z, ajuste a superficie, padding, margen y rotación. |
| `Items/FlashlightController.cs` | Gestiona el foco de una linterna agarrable. El botón se determina según la mano que sostiene el objeto y la luz se apaga al liberar. |
| `Items/GrabbedObjectButton.cs` | Mapea un botón `OVRInput.Button` a un `UnityEvent` mientras el objeto está agarrado. |
| `Inventory/InventoryBodyFollower.cs` | Sigue horizontalmente la cabeza para mantener el inventario a la altura de la cintura; usa `headTransform`, `heightOffset` y `smoothSpeed`. |
| `Inventory/InventorySlot.cs` | Detecta objetos con el tag `KeepableObject`, espera al ciclo de física tras liberar, elige el slot activo compatible y hace snap al `snapPoint`. |
| `Inventory/InventoryItem.cs` | Declara `InventorySlotKind`/`InventorySlotMask`, controla compatibilidad y evita reservas duplicadas. |

## Personajes

| Script | Propósito y configuración |
|---|---|
| `DriveCharacters/ProceduralIdle.cs` | Idle procedural con respiración, balanceo de cabeza y plantado de pies mediante targets y `groundMask`. |
| `DriveCharacters/IKTargetMarker.cs` | Marcador visual de targets IK con color y radio configurables. |

## Herramientas de Editor

`Assets/Editor/DriveCharacterCleanup.cs`, `DriveCharacterRebuild.cs`, `DriveCharacterSetup.cs`, `ProbeDriveTargets.cs` y `RunDriveRigRebuild.cs` son herramientas auxiliares para preparar o reconstruir los personajes de showcase. No forman parte del flujo runtime documentado.

## Uso y límites

La configuración recomendada es asignar referencias desde el Inspector y conservar los nombres de tags, escenas y rutas existentes. No se debe asumir que una referencia encontrada por búsqueda runtime sustituye la configuración serializada. No hay ensamblados propios de `GameStuff` ni tests automatizados versionados; la validación de interacción MR requiere el Editor y, para funciones específicas del dispositivo, un Quest real.
