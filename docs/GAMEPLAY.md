# Documento de Diseño de Juego (Mini GDD Técnico) — FNaF MR

Este documento describe las mecánicas de juego, el comportamiento de los personajes, el estado de la progresión y los sistemas implementados en el código de **Untiteled FNaF MR fangame** para Meta Quest 3.

---

## 1. Resumen de la Experiencia

FNaF MR traslada la tensión y atmósfera del universo *Five Nights at Freddy's* a la habitación real del jugador mediante Realidad Mixta (Passthrough estéreo en color). El espacio físico del usuario se transforma en el entorno de juego mediante el oscurecimiento del passthrough y la inserción de elementos virtuales reactivos a la iluminación.

---

## 2. Mecánicas Implementadas en Código

### 2.1 Validación del Entorno Real
- **Clase responsable:** `RoomValidator` (`Assets/GameStuff/Scripts/Managers/RoomValidator.cs`).
- **Comportamiento:**
  - Al iniciar la escena MR (`InitialTestRoom`), el sistema analiza los anclajes provistos por MRUK (`MRUK.Instance.RoomCreatedEvent`).
  - Para considerar la habitación válida para jugar (`isRoomValid == true`), se exige que el escaneo del usuario contenga al menos:
    1. Una puerta (`MRUKAnchor.SceneLabels.DOOR_FRAME`).
    2. Una mesa (`MRUKAnchor.SceneLabels.TABLE`).
    3. Una cama (`MRUKAnchor.SceneLabels.BED`) o un sofá (`MRUKAnchor.SceneLabels.COUCH`).
  - El sistema calcula y almacena la mesa más cercana a la cabeza del jugador (`closestTable`).

### 2.2 Sistema de Iluminación y Penumbra
- **Clases responsables:** `RoomLightManager` (`Assets/GameStuff/Scripts/Managers/RoomLightManager.cs`), shaders `DarknessRevealer.shader` y `MR_DirectLightOnly.shader`.
- **Comportamiento:**
  - **Oscurecimiento ambiental:** El passthrough de la habitación real se atenúa reduciendo el brillo de la capa de passthrough (`OVRPassthroughLayer.SetBrightnessContrastProperties`).
  - **Luz virtual:** La luz virtual principal (`mainVirtualLight`) conmuta entre estados de encendido (`virtualLightOnIntensity = 0.8`) y apagado (`virtualLightOffIntensity = 0.05`).
  - **Parpadeo (*flicker*):** Corrutina estroboscópica que altera rápidamente la iluminación antes de apagarse o estabilizarse.
  - **Revelado por linterna:** Los objetos en penumbra solo se vuelven visibles al ser iluminados directamente por un foco de luz virtual que penetre la máscara de oscuridad.

### 2.3 Linterna y Objetos Agarrables
- **Clases responsables:** `FlashlightController`, `GrabbedObjectButton`, `GrabbableItemManager`, `DepthOcclusionOnGrab`, `InteractableHighlight`.
- **Comportamiento:**
  - La linterna es un prefab interactivo con `Oculus.Interaction.GrabInteractable`.
  - Al agarrarla con cualquier mano, `GrabbedObjectButton` detecta qué mando la sujeta y escucha la pulsación del botón configurado (`OVRInput.Button.PrimaryThumbstick` / secundario).
  - Al pulsar el botón, `FlashlightController.ToggleFlashlight()` conmuta la luz puntual (`spotlight`).
  - Al soltar la linterna, la luz se apaga automáticamente por seguridad energética.
  - Los objetos interactuables muestran un contorno luminoso unlit aditivo (`InteractableHighlight`) que pulsa en la penumbra y se desactiva al primer agarre.
  - Al agarrar un objeto, `DepthOcclusionOnGrab` desactiva la oclusión por profundidad para que las manos reales del usuario no recorten la malla del objeto.

### 2.4 Inventario Físico de Cintura
- **Clases responsables:** `InventoryBodyFollower`, `InventorySlot`, `InventoryItem`.
- **Comportamiento:**
  - Los slots de inventario siguen la rotación en el plano horizontal y la posición de la cabeza del jugador a una altura fija bajo la cintura.
  - Los objetos con la etiqueta `KeepableObject` poseen una máscara de compatibilidad (`InventorySlotMask.Pockets`, `InventorySlotMask.Shoulder`).
  - Al acercar un objeto a un slot y soltarlo, el sistema espera un ciclo de físicas (`WaitForFixedUpdate`), busca el slot compatible más cercano disponible, reserva el espacio y ancla el objeto en su `snapPoint` desactivando temporalmente sus físicas dinámicas.

### 2.5 Generación de Elementos en el Cuarto
- **Clase responsable:** `MRSpawnerReworked` (`Assets/GameStuff/Scripts/Items/MRSpawnerReworked.cs`).
- **Comportamiento:**
  - Instancia prefabs (como la linterna o elementos interactivos) directamente sobre la cara superior de volúmenes MRUK (por ejemplo, sobre la superficie de la mesa detectada).
  - Utiliza márgenes de borde (`borderMargin`) y desfase de altura (`surfacePadding`) para asegurar que los objetos queden apoyados de forma creíble.

### 2.6 Manos Virtuales FNaF
- **Clases responsables:** `PlayerHandsVR`, `FnafHandPose`.
- **Comportamiento:**
  - Sustituye los modelos genéricos de mandos de Quest por mallas tridimensionales de manos de FNaF Help Wanted (`RIG_Right_PlayerHands_10k`).
  - Mapea de forma continua los valores de grip, gatillo e índice a la rotación de las falanges esqueléticas de la mano.

---

## 3. Personajes y Comportamiento Animatrónico

### 3.1 SpringBonnie en Habitación Real
- **Escena:** `Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity`.
- **Clases responsables:** `MRSpringBonniePlacement`, `SpringBonnieStaticPose`.
- **Comportamiento implementado:**
  - Localiza el ancla de cama (`BED`) de la habitación real.
  - Posiciona la figura de SpringBonnie sentada en el borde de la cama, orientada hacia el jugador y hacia la zona despejada de paredes.
  - Un solucionador IK de dos huesos (`SpringBonnieStaticPose`) ajusta automáticamente la altura de tobillos y rodillas para que las plantas de los pies apoyen con precisión sobre el suelo real detectado.

### 3.2 Showcase de Modelos y Cinemática Procedural
- **Escena:** `Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity`.
- **Clases responsables:** `ProceduralIdle`, `IKTargetMarker`.
- **Modelos disponibles:** Freddy, Bonnie, Chica, Foxy, Endo, Golden Freddy, y sus versiones Withered (Freddy, Bonnie, Chica, Foxy).
- **Comportamiento implementado:**
  - `ProceduralIdle` genera una simulación continua de respiración en el torso (`chest`/`spine`), oscilación sutil de cabeza (`headSwayAngle`) y comprobación de suelo mediante raycast hacia los pies (`groundMask`).
  - El rig se apoya en restricciones de Unity Animation Rigging (`TwoBoneIKConstraint`, `MultiAimConstraint`, `MultiRotationConstraint`).

### 3.3 Comportamientos Pendientes de Implementación
- **Movimiento de animatrónicos en el espacio físico:** Pendiente (no hay sistemas de navegación NavMesh adaptados a MRUK ni controladores de movimiento en runtime).
- **Asomarse o agarrarse a esquinas:** Pendiente (no implementado en código; únicamente existen poses estáticas de animación exportadas).
- **Agacharse al cruzar puertas:** Pendiente (no implementado en código; no existe lógica de detección de marcos de puerta combinada con altura de animatrónicos).

---

## 4. Progresión, Victoria y Derrota

| Sistema | Estado en Código | Detalle |
|---|---|---|
| Rondas nocturnas / Horas (12 AM - 6 AM) | **No implementado** | No existe en el repositorio ninguna clase o contador de tiempo que gestione progresión por noches u horas. |
| Condiciones de Derrota (Jumpscare / Game Over) | **No implementado** | No existen scripts de daño al jugador, colisión letal de animatrónicos ni pantallas de fin de juego. |
| Condiciones de Victoria | **No implementado** | No existen scripts ni comprobaciones de victoria. |

Actualmente, las escenas operan como **entornos de pruebas técnicas (alpha/sandbox)** enfocados en la validación de MRUK, la interacción táctil en VR y el renderizado en tiempo real.
