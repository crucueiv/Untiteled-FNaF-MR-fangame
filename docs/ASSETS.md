# Catálogo de Assets y Licencias — FNaF MR

Este documento cataloga los modelos tridimensionales, texturas, audios, prefabs y paquetes de terceros utilizados en **Untiteled FNaF MR fangame**, junto con su procedencia y términos de uso.

---

## 1. Modelos 3D y Rigging

### 1.1 Animatrónicos Principales (`Assets/GameStuff/Models/DriveRigged/`)
Todos los modelos de personajes de esta carpeta proceden del pack público para la comunidad:
- **Origen:** [The FNaF Help Wanted MEGA FBX Pack Release](https://www.deviantart.com/jennydperiod/art/The-FNaF-Help-Wanted-MEGA-FBX-Pack-Release-983155006), compilado por **JennyDPeriod**.
- **Propiedad Intelectual:** Los personajes y diseños pertenecen a **Scott Cawthon / Scottgames, LLC** y Steel Wool Studios.
- **Formato:** Modelos FBX con mallas esqueléticas (*skinned meshes*) y jerarquías estandarizadas de huesos `bip_*`.

| Personaje | Archivos de Malla | Texturas Asociadas | Prefab Generado |
|---|---|---|---|
| **Freddy** | `Freddy/HWFreddy.fbx` | `FreddyDiffuse.jpg`, `FreddyEyeDiffuse.jpg`, `body_n.jpg`, `mic_d.jpg`, `mic_n.jpg` | `DriveCharacters/Freddy.prefab` |
| **Golden Freddy** | `Freddy/HWFreddy.fbx` | `GFreddyDiffuse.jpg`, `GFreddyEye.jpg`, `body_n.jpg` | `DriveCharacters/GoldenFreddy.prefab` |
| **Bonnie** | `Bonnie/HWBonnieis cool.fbx` | `BonnieDiffuse.jpg`, `BonnieNormal.jpg`, `BEyeDiffuse.jpg` | `DriveCharacters/Bonnie.prefab` |
| **Chica** | `Chica/HWChica.fbx` | `chicabody_d.jpg`, `body_n.jpg`, `chicaeye_d.jpg` | `DriveCharacters/Chica.prefab` |
| **Foxy** | `Foxy/HWFoxy.fbx` | `FoxyDiffuse.jpg`, `Foxyeye_d.jpg`, `battery_d.jpg`, `battery_n.jpg`, `body_n.jpg` | `DriveCharacters/Foxy.prefab` |
| **Endo (Endoesqueleto)** | `Endo/HWEndo.fbx` | `endo_d.jpg`, `body_n.jpg`, `body_e.jpg`, `EndoEyeDiffuse.jpg` | `DriveCharacters/Endo.prefab` |
| **SpringBonnie** | `SpringBonnieVR/HWSpringBonnie.fbx`<br>`HWSpringBonnieIMPROVED.fbx` | `SB-SUIT.jpg`, `SB-SUIT-N.jpg`, `SB-ENDO.jpg`, `SB-ENDO-N.jpg`, `SB-EYE-Custom.png`, `SB-EYE-bad.jpg` | `Characters/SpringBonnie_Suit.prefab` |
| **Withered Freddy** | `WitheredFreddy/HWWitheredFreddy.fbx` | Texturas difusas y normales Withered | `DriveCharacters/WitheredFreddy.prefab` |
| **Withered Bonnie** | `WitheredBonnie/HWWitheredBonnie.fbx` | Texturas difusas y normales Withered | `DriveCharacters/WitheredBonnie.prefab` |
| **Withered Chica** | `WitheredChica/HWWitheredChica.fbx` | Texturas difusas y normales Withered | `DriveCharacters/WitheredChica.prefab` |
| **Withered Foxy** | `WitheredFoxy/HWWitheredFoxy.fbx` | Texturas difusas y normales Withered | `DriveCharacters/WitheredFoxy.prefab` |

### 1.2 Manos del Jugador (`Assets/GameStuff/Models/Player Hands/`)
- **Archivos:** `RIG_Right_PlayerHands_10k.dae` y `RIG_Right_PlayerHands_10k.fbx`, textura `TEX_Scanlines_DIF.png`.
- **Origen:** Extraído del paquete de assets de FNaF Help Wanted (JennyDPeriod).
- **Uso:** Utilizado por `PlayerHandsVR` y `FnafHandPose` para sustituir los mandos de Quest por manos virtuales con seguimiento de dedos.

---

## 2. Audio (`Assets/GameStuff/Sounds/`)

| Archivo | Formato | Duración / Tamaño | Origen | Licencia / Uso |
|---|---|---|---|---|
| `fnaf2maskon.mp3` | MP3 (LFS) | ~62 KB | Efecto sonoro de colocación de la máscara de Freddy en *Five Nights at Freddy's 2*. | Obra de fans sin ánimo de lucro. Propiedad de Scott Cawthon. |

---

## 3. Entornos de Prueba y Salas Simuladas (`Assets/GameStuff/TestRoom/`)

- `MeshBedroom1.json`: Malla de habitación escaneada y exportada desde Meta Quest mediante MRUK. Permite simular y probar la detección de habitación en el Editor de Unity sin necesidad de conectar el visor.
- `Room - ff178190-a724-22af-a1a6-9fcdd7d66778.prefab`: Prefab de habitación física serializada con anclajes de pared, suelo, cama y mesa.

---

## 4. Prefabs Propios Principales (`Assets/GameStuff/Prefabs/`)

- `Flashlight.prefab`: Linterna interactiva con `GrabInteractable`, `FlashlightController`, `GrabbedObjectButton`, `DepthOcclusionOnGrab`, `InteractableHighlight` y fuente de luz Spotlight con cookie.
- `GrabbableObject.prefab`: Cubo básico para pruebas de agarre físico y oclusión.
- `MRThings.prefab`: Colección de objetos de prueba para instanciar en tiempo de ejecución.
- `SpawnerObjectManager.prefab`: Orquestador de generación de objetos en superficies MRUK (`MRSpawnerReworked`).
- `Hands/PlayerHand_Right.prefab` y `PlayerHand_Left.prefab`: Prefabs de manos animables del jugador.

---

## 5. Paquetes y Dependencias de Terceros

| Paquete | Versión | Origen | Licencia |
|---|---|---|---|
| `com.meta.xr.sdk.all` (Meta XR SDK) | `209.0.0` | Meta Platforms, Inc. | [Oculus SDK License](https://developer.oculus.com/licenses/oculussdk/) |
| `com.unity.xr.interaction.toolkit` | `3.4.1` | Unity Technologies | Unity Companion License |
| `com.unity.xr.hands` | `1.7.3` | Unity Technologies | Unity Companion License |
| `com.unity.render-pipelines.universal` | `17.3.0` | Unity Technologies | Unity Companion License |
| `com.unity.animation.rigging` | `1.4.1` | Unity Technologies | Unity Companion License |
| `com.coplaydev.unity-mcp` | Git (`main`) | CoplayDev | Repositorio público / Open Source |
| `com.ramenvr.aura-unity` | Git (`main`) | RamenVR | Repositorio público / Open Source |
| `com.ramenvr.autopilot` | Git (`9e0fbb0`) | RamenVR | Repositorio público / Open Source |

---

## 6. Alcance de la Licencia del Repositorio

> [!IMPORTANT]
> La licencia libre (MIT) del repositorio aplica **única y exclusivamente** al código fuente original C# (`Assets/GameStuff/Scripts/`, `Assets/Editor/`) y a los shaders personalizados (`Assets/GameStuff/Shaders/`).
> 
> **NO aplica a:**
> - Modelos 3D, mallas y texturas de personajes de FNaF (propiedad de Scottgames, LLC / Scott Cawthon).
> - Efectos sonoros y audio de la saga FNaF.
> - Paquetes y SDKs de terceros (Meta XR SDK, Unity Technologies).
