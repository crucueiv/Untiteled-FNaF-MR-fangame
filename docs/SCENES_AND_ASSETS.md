# Escenas y assets

## Escenas propias

| Escena | Build Settings | Contenido observado |
|---|---:|---|
| `Assets/GameStuff/TestScenes/TestMenu.unity` | Sí | Escenario de menú, splash, pantallas principal/ajustes, botones, retícula, iluminación de escenario y objetos de control del menú. |
| `Assets/GameStuff/TestScenes/InitialTestRoom.unity` | Sí | Escena MR con `RoomLightManager`, `SpawnerObjectManager`, `VisualsManager`, `RoomValidationManager`, `MR_DebuggerController`, objetos de habitación y cubos de prueba. |
| `Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity` | No | Showcase de Freddy, Bonnie, Chica, Foxy, Endo y variantes Withered, con cámara, luz, suelo y etiquetas de personajes. |

La lista habilitada está definida en `ProjectSettings/EditorBuildSettings.asset`. También existen escenas de muestras de XR y archivos en `Assets/_Recovery`; no forman parte de la lista de compilación propia.

## Prefabs principales

- `GameStuff/Prefabs/Flashlight.prefab`: linterna interactiva.
- `GameStuff/Prefabs/GrabbableObject.prefab`: base de objeto agarrable.
- `GameStuff/Prefabs/MRThings.prefab`: conjunto de objetos de prueba MR.
- `GameStuff/Prefabs/DriveCharacters/*.prefab`: personajes riggeados, incluyendo Freddy, Bonnie, Chica, Foxy, Endo, Golden Freddy y variantes Withered.

## Animación y personajes

`GameStuff/Animations/DriveCharacters` contiene Animator Controllers y tres poses por personaje (`Neutral`, `ArmsExtended`, `WallContact`). Los scripts `ProceduralIdle` e `IKTargetMarker` añaden comportamiento/configuración procedural a los rigs. Los modelos y texturas de personajes están agrupados bajo `GameStuff/Models/DriveRigged`.

Los modelos de personajes proceden de [The FNaF Help Wanted MEGA FBX Pack Release](https://www.deviantart.com/jennydperiod/art/The-FNaF-Help-Wanted-MEGA-FBX-Pack-Release-983155006), de JennyDPeriod.

## Materiales y shaders

- `GameStuff/Materials`: materiales de juego, oscuridad y objetos.
- `GameStuff/Materials/Menu`: materiales de escenario, cortinas, neones, personajes y UI del menú.
- `GameStuff/Shaders/DarknessRevealer.shader`: shader de oscuridad sensible a la iluminación.
- `GameStuff/Shaders/HighlightsAndShadows.shader`: shader de realce/sombras para MR.
- `Settings` y `ProjectSettings/GraphicsSettings.asset`: configuración URP global.

## Estructura de assets

| Carpeta | Función |
|---|---|
| `GameStuff` | Contenido propio del fangame |
| `MRTemplateAssets` | Recursos/scripts de plantilla de realidad mixta |
| `MetaAssets` | Recursos de integración y prefabs Meta |
| `Samples` | Muestras importadas de XR Hands y XR Interaction Toolkit |
| `Plugins/Android` | Manifiesto Android del proyecto |
| `XR`, `XRI`, `Settings` | Loaders, simulación y configuración XR/URP |
| `TextMesh Pro` | Recursos de texto y shaders de TMP |
| `Editor` | Herramientas de preparación de contenido |

## Convenciones y mantenimiento

- Mantener cada `.meta` junto a su asset para conservar GUID y referencias.
- Usar las carpetas de `GameStuff/Scripts` por responsabilidad (`Menu`, `Managers`, `Items`, `Inventory`, `Debbug`, `DriveCharacters`).
- Mantener los nombres de escenas en `EditorBuildSettings.asset` y en `FnafMenuController.gameScenePath` sincronizados.
- Tratar `Samples`, `MRTemplateAssets` y `MetaAssets` como contenido de terceros/plantilla cuando se evalúe autoría o deuda técnica.
- Los modelos, texturas, audio y fuentes están gestionados con Git LFS según `.gitattributes`; no deben reemplazarse por punteros incompletos.
