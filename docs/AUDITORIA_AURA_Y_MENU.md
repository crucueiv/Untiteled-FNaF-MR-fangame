# Auditoría de cambios MR y centrado del menú

Fecha: 18-11-2026  
Rama: `audit/cleanup-2026-11-18`  
Restauración: etiqueta `restore-before-audit-2026-11-18` (`7b3c9fd`)

## Alcance y estado inicial

El historial atribuye al usuario los cambios de `b542142` y `1da21a0`, pero Git no identifica qué herramienta produjo cada archivo. La auditoría toma esos commits como el lote de cambios MR a revisar y contrasta sus referencias con las escenas, prefabs y scripts actuales.

Antes de editar ya había modificaciones locales en las instrucciones `.codex`, `.vscode/settings.json`, `MR_SpringBonnieRoom.unity`, ajustes XR, `Packages/`, `ProjectSettings/` y el `.slnx`, además de archivos sin seguimiento de Roslyn, XR APILayers y ProBuilder. Se conservaron fuera de los commits de esta auditoría. No se tocaron `Packages/`, `Library/`, activos Meta de terceros ni ajustes globales del proyecto.

## Inventario y veredictos

Las referencias listadas son GUID de escenas/prefabs, llamadas desde código o referencias serializadas; no son una estimación de uso basada solo en el nombre.

| Archivo / grupo | Líneas | Función y uso comprobado | Veredicto |
|---|---:|---|---|
| `Scripts/Managers/MRDepthOcclusionController.cs` | 353 | Configura `EnvironmentDepthManager`, keywords, bias/globales de shader y ambiente. Está en `MR_SpringBonnieRoom` y en `MRDepthOcclusion.prefab`, instanciado en `InitialTestRoom`. | C: conservar; afecta oclusión y render |
| `Scripts/Interactions/DepthOcclusionOnGrab.cs` | 613 | Cambia entre material original y variante oclusiva al agarrar/soltar; también trata materiales con `_DepthOcclusion`. Está en `Flashlight.prefab` y `GrabbableObject.prefab`; ambos mantienen la lista de renderers. | C: conservar; cambio de comportamiento al agarrar |
| `Scripts/Managers/MRContactShadow.cs` | 214 | Crea blobs de contacto para SpringBonnie; adjunto a `Springbonnie_MR`. Usa `MR_ContactBlob.mat`. | C: conservar; cambio visual |
| `Scripts/Managers/MRSurfacePlacement.cs` | 76 | Helper común de raycast/ajuste a superficie. Lo usan `MRSpawnerReworked` y `MRDebuggerMenu`. | B: compartir helper evita duplicar colocación |
| `Scripts/Debbug/MRDebugOverlay.cs` | 523 | Overlay de diagnóstico y toggles MR; se crea por inicialización runtime cuando hay controlador de profundidad. No está serializado en escenas. | B: ahora limitado a Editor/Development Build |
| `Scripts/Interactions/InteractableHighlight.cs` | 309 | Construye contornos y escucha el primer agarre. Está en los prefabs de linterna y bloque; carga `Resources/HighlightOutline.mat`. | C: conservar; interacción y material visual |
| `Scripts/Managers/GrabbableItemManager.cs` | 99 | Conecta agarre con los visuales de mandos. Está en linterna y bloque. | C: conservar; interacción de mandos |
| `Scripts/Items/MRSpawnerReworked.cs` | 247 | Spawner MR. Está en `SpawnerObjectManager.prefab`; `MRSpringBonniePlacement` también usa sus helpers de bounds/caras. | C: conservar; uso indirecto comprobado |
| `Scripts/Debbug/MRDebuggerMenu.cs` | 99 | Menú de diagnóstico/spawn. Está en `InitialTestRoom` y `MR_SpringBonnieRoom`; llama a `MRSurfacePlacement`. | C: conservar; herramienta de prueba |
| `Scripts/Hands/PlayerHandsVR.cs` | 216 | Muestra manos virtuales en el rig VR con el toggle existente. Está dentro de `MRThings.prefab`; no añade hand tracking. | C: conservar; forma parte del rig VR |
| `Shaders/DarknessRevealer.shader` | 187 | Capa de oscuridad MR y máscara near-field. Incluye `MR_Lighting.hlsl`. | C: conservar; orden y resultado de render |
| `Shaders/MR_DirectLightOnly.shader` | 204 | Iluminación directa, sombras virtuales y `_LightBounce`; consume las funciones comunes de oclusión. | C: conservar; shader central |
| `Shaders/MR_Lighting.hlsl` | 115 | Lógica común de oclusión/bias y máscara de sombras. Se incluye desde `DarknessRevealer`, `MR_DirectLightOnly` y `MR_LitOcclusionForwardPass`. | B: centraliza lógica compartida; conservar |
| `Shaders/MR_LitOcclusion.shader` | 403 | Shader URP para materiales oclusivos. Lo usan las variantes `*_MR`. | C: conservar; shader URP de varios pases |
| `Shaders/MR_LitOcclusionForwardPass.hlsl` | 205 | Forward pass con Depth API; incluye `MR_Lighting.hlsl`. | C: conservar; toca render y SRP Batcher |
| `Shaders/MR_ContactBlob.shader` | 94 | Shader suave de blobs de contacto. Referenciado por `Materials/MR_ContactBlob.mat`. | C: conservar; tiene un warning Vulkan anotado abajo |
| `Shaders/MR_HighlightOutline.shader` | 114 | Shader del contorno cargado por `Resources/HighlightOutline.mat`. | C: conservar; visual del interactuable |
| `Materials/MR_ContactBlob.mat` | 33 | Material del shader `Custom/MR_ContactBlob`; asignado al componente `MRContactShadow`. | C: conservar |
| `Prefabs/MRDepthOcclusion.prefab` | 72 | Empaqueta un `EnvironmentDepthManager` y un `MRDepthOcclusionController`; está instanciado en `InitialTestRoom`. | C: conservar; referencia por GUID verificada |
| `Materials/MR_Occlusion/*_MR.mat` (7 materiales) | — | Variantes de `611021_L/M/S`, `BedProxy`, `SB_Endo`, `SB_Eye` y `SB_Suit`; cada GUID aparece en escena o prefab. | C: conservar; no son duplicados huérfanos |

`MR_ShadowReceiver` no existe con ese nombre. `FnafGazeFollower` no está en las escenas activas, pero su GUID aparece en cuatro escenas de recuperación bajo `Assets/_Recovery`; no es seguro borrarlo. `MRSpawnerReworked` parece desconectado de algunas escenas actuales, pero sí está en el prefab del spawner y lo llama `MRSpringBonniePlacement`, por lo que no está muerto.

## Hallazgos

- La escena con `MENÚ PRINCIPAL / INICIAR JUEGO / SELECCIONAR ESCENA` es `TestMenu`; `InitialTestRoom` no tiene ese menú.
- `TestMenu` usa `OVRCameraRig` dentro de `FnafMenuRoot/Menu XR Rig/Player`. El `OVRManager` serializa `_trackingOriginType: 1`, que en el SDK instalado corresponde a `FloorLevel`.
- El `OVRCameraRig` está desplazado `+0.90491 m` en X respecto al padre del rig. En la sala MR, el prefab `MRThings` se coloca en `-0.90491 m` y compensa ese desplazamiento; en `TestMenu`, el objeto `Menu XR Rig` está en cero. Ese origen de escena explica el desplazamiento lateral inicial, al que se suma la posición física de la cabeza.
- El menú no modifica el modo de entrada. Los punteros izquierdo y derecho del láser son anclas descendientes del rig que se alinea, de modo que se trasladan y giran junto con la cámara.
- No se encontraron materiales MR sin referencias. `MRDepthOcclusion.prefab` está usado y los pares de la linterna están serializados; el bloque usa la ruta `_DepthOcclusion` del shader y por eso su lista de pares puede estar vacía.
- `MR_DirectLightOnly` declara el mismo `UnityPerMaterial` en `ForwardLit` y `ShadowCaster`, con las mismas propiedades y orden. Los shaders MR incluyen macros de instancing/estéreo en los pases revisados.
- No se tocaron los shaders de alto riesgo ni se intentó reducir variantes: cambiar keywords, passes o el shader URP puede cambiar oclusión, sombras o color en Vulkan/Quest.
- La carpeta `Debbug` está mal escrita, pero renombrarla no aporta una corrección funcional y puede alterar rutas visibles; queda para revisión. Se retiraron solo las carpetas vacías `GameScenes` y `Poses`, tras comprobar que no tenían archivos ni referencias a sus GUID.

## Cambios y commits

1. `fc7f6ef` — limita `MRDebugOverlay` a `DEVELOPMENT_BUILD || UNITY_EDITOR`; no cambia su comportamiento durante desarrollo.
2. `3a11e78` — añade `MenuPlayerAlignment.cs`, su `.meta` y el componente a `FnafMenuRoot` en `TestMenu`.
3. `580de6b` — elimina los `.meta` de las carpetas vacías `GameScenes` y `Poses`.

No se eliminaron scripts, shaders, materiales ni prefabs. No hubo scripts fusionados. El helper común `MR_Lighting.hlsl` ya centraliza la lógica de oclusión existente.

## Verificación

| Comprobación | Resultado |
|---|---|
| Compilación C# en Unity 6000.3.18f1 | Finalizó sin errores de compilador tras ambos cambios de código. |
| Validación de escenas, sin reparación automática | `TestMenu`, `InitialTestRoom` y `MR_SpringBonnieRoom`: 0 issues, 0 scripts faltantes, 0 prefabs rotos en cada escena. |
| Un manager de profundidad | Un `EnvironmentDepthManager` por escena MR comprobado en YAML: instancia de `MRDepthOcclusion` en `InitialTestRoom` y componente único en `MR_SpringBonnieRoom`; ninguno en `TestMenu`. |
| Play Mode | `TestMenu` entró y salió de Play Mode. El Editor reportó que no había dispositivo XR disponible; Meta OpenXR devolvió avisos `ErrorFormFactorUnavailable`/funciones no soportadas. No se pudo validar input real del mando ni la Depth API. |
| Simulación de centrado | Rig desplazado `0.65 m` a la derecha y girado `18°`; resultado: error horizontal `0.0000 m`, error de yaw `0°`, cambio de altura `0.0000 m`. La prueba invocó el alineador en Editor y restauró el transform original. |
| Keywords y variantes de shader | El build Android inició la compilación Vulkan y reportó un warning de `MR_ContactBlob` para la variante `HARD_OCCLUSION`; no se ejecutó una prueba independiente forzando HARD y SOFT y apagándolas después. |
| CBUFFER ForwardLit/ShadowCaster | Comparación estática: mismos campos y orden. |
| Capturas comparables | No realizadas: no había captura previa del mismo punto de cámara y el Editor no tenía HMD activo. |
| Android IL2CPP | No se completó durante la auditoría. Unity sigue reportando `building` desde hace más de 15 minutos y no creó el APK en la ruta temporal. El intento de cancelación fue rechazado porque `BuildPipeline.BuildPlayer` bloquea el Editor de forma síncrona. |

Durante el build Unity avisó que dos recursos de XR Simulation no podían moverse a `Assets/XR/Temp` porque el destino ya existía. También mostró recomendaciones de diagnóstico de Meta. El warning Vulkan de `MR_ContactBlob.shader` apunta a `pow(f, e)` con base potencialmente negativa. No lo cambié porque una modificación del término de sombra requiere comparar imagen y probar en Quest.

El build añadió dos entradas temporales a `PlayerSettings.preloadedAssets` y generó `PerformanceTestRunInfo.json`, `PerformanceTestRunSettings.json` y `StreamingAssets/RuntimeActionBindings.json`. Esos cambios no forman parte de ningún commit de auditoría; se restaurarán/eliminarán cuando el proceso de build deje de usar el proyecto.

## Corrección del menú

`MenuPlayerAlignment` está en el objeto raíz `FnafMenuRoot` de `TestMenu`. Al iniciar, espera a `OVRManager.isHmdPresent`, `OVRPlugin.userPresent` y un dispositivo Head con `isTracked`; reintenta cada `0.5 s` si el visor llega tarde. Cuando hay pose, alinea el centro de la cabeza a `1.6 m` del centro del panel, con offset lateral `0 m`, solo con yaw y manteniendo la altura real de la cabeza. El rig permanece en `FloorLevel`. El gizmo verde marca el punto ideal y el log registra el desplazamiento y yaw aplicados.

Al deshabilitarse o descargarse la escena, el componente restaura la pose previa del rig si este sigue vivo. Como vive en `TestMenu`, no recoloca el jugador en `InitialTestRoom` ni en la sala MR.

## Prueba pendiente en Quest 3/3S

- Arrancar `TestMenu` de pie a la derecha, a la izquierda y lejos; confirmar que los ojos quedan centrados a `1.6 m` del panel y a la altura del suelo/usuario correcta.
- Confirmar que los rayos de ambos mandos siguen apuntando y activan «Iniciar juego» y el selector tras el realineado.
- Probar que al volver desde cada escena el menú se vuelve a centrar.
- Comprobar en MR la oclusión de manos, brazos y mandos; recoger y soltar bloques/linterna; verificar sombras sin silueta sobre el brazo.
- Revisar SpringBonnie, sombras de contacto, ambiente cálido y que `TestMenu`/las escenas VR sigan iguales.
- Medir grano, profundidad y rendimiento a 72/90 Hz.

