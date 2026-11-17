# Shaders e Iluminación — FNaF MR

Este documento documenta la arquitectura de renderizado, shaders personalizados en Universal Render Pipeline (URP 17.3.0) y el modelo de iluminación para realidad mixta en Meta Quest 3.

---

## 1. Filosofía del Sistema de Iluminación MR

En una experiencia MR de terror, la oscuridad no puede ser simplemente un fondo negro estático, ya que el usuario debe seguir percibiendo su entorno real a través del passthrough. El proyecto implementa un modelo de dos capas:

1. **Atenuación del Passthrough:** `RoomLightManager` reduce el brillo del mundo real mediante la capa de passthrough del SDK de Meta (`OVRPassthroughLayer.SetBrightnessContrastProperties`).
2. **Iluminación Selectiva y Revelado:** Los objetos virtuales y las superficies del entorno solo reciben luz visible mediante conos de luz virtuales (linterna) y luces puntuales, aplicando shaders personalizados que eliminan el brillo ambiental parásito.

---

## 2. Catálogo de Shaders Personalizados

### 2.1 `Custom/MR_UltimateDarknessRevealer`
- **Ubicación:** `Assets/GameStuff/Shaders/DarknessRevealer.shader`
- **Propósito:** Actúa como una máscara de penumbra sobre la habitación. Permite que la linterna y las luces puntuales "revelen" el entorno y los objetos con atenuación y tinte dinámico.
- **Render Queue y Tags:**
  - `Queue = Transparent+100`
  - `RenderType = Transparent`
  - `RenderPipeline = UniversalPipeline`
  - `LightMode = UniversalForward`
  - `Blend One OneMinusSrcAlpha`
- **Propiedades configurables:**
  - `_DarknessColor` (Color): Tono de la penumbra (negro por defecto).
  - `_LightSensitivity` (Range 0.1 a 10.0): Multiplicador de respuesta ante fuentes lumínicas.
  - `_TintStrength` (Range 0 a 1): Grado de coloración aportado por el foco de la linterna.
  - `_CookieStrength` (Range 0 a 5): Intensidad de la textura de cookie de la linterna.
  - `_NdotLInfluence` (Range 0 a 1): Control del ángulo de incidencia normal/luz. Un valor de `0.0` hace que el cono de luz se proyecte de forma homogénea sobre suelos, techos y paredes ignorando la orientación normal de la superficie.
- **Soporte de Keywords:** `_MAIN_LIGHT_SHADOWS`, `_ADDITIONAL_LIGHTS`, `_LIGHT_COOKIES`, `_SHADOWS_SOFT`.

---

### 2.2 `Custom/MR_DirectLightOnly`
- **Ubicación:** `Assets/GameStuff/Shaders/MR_DirectLightOnly.shader`
- **Propósito:** Shader opaco para objetos virtuales (piezas de animatrónicos, utilería) situados en la oscuridad MR.
  - **Sin luz ambiente:** Suprime la luz ambiental, los armónicos esféricos (Spherical Harmonics) y los reflejos del skybox. Cuando ninguna luz apunta al objeto, este se renderiza como negro puro y totalmente opaco, integrándose con el vacío de la habitación.
  - **Sombreado suavizado:** Controla la caída de la luz directa sin recurrir a luz indirecta.
- **Propiedades configurables:**
  - `_BaseMap` / `_BaseColor`: Textura de albedo y tinte principal.
  - `_LightWrap` (Range 0 a 1): Suavizado de la transición entre zona iluminada y en sombra (corte N·L).
  - `_LightContrast` (Range 0.2 a 3): Curva de contraste de la luz incidente.
  - `_LightBoost` (Range 0 a 6): Multiplicador de potencia de luz directa.
  - `_ShadowsOn` (0 o 1): Interruptor para recepción de sombras.
  - `_ShadowNormalBias`: Bias para evitar artefactos de auto-sombra (*shadow acne*).
  - `_DepthOcclusion` (0 o 1): Multiplicador de la máscara de oclusión de Meta Depth API.
- **VR y Estéreo:** Compilado con `multi_compile_instancing` y macros estéreo para **Single Pass Instanced**.

---

### 2.3 `Custom/MR_HighlightOutline`
- **Ubicación:** `Assets/GameStuff/Shaders/MR_HighlightOutline.shader`
- **Propósito:** Dibuja un contorno exterior (*silhouette outline*) mediante una pasada de casco invertido (*inverted hull pass*, `Cull Front`).
- **Render Queue y Tags:**
  - `Queue = Transparent+50`
  - `Blend One One` (Aditivo unlit, visible sin necesidad de luces en escena).
  - `ZWrite Off`, `ZTest LEqual`.
- **Propiedades configurables:**
  - `_OutlineColor` (Color HDR): Color del borde luminoso.
  - `_OutlineWidth` (Range 0.001 a 0.05): Grosor del contorno escalado en espacio de proyección.
  - `_Pulsar` (0 o 1): Modulación senoidal temporal ejecutada directamente en GPU (coste CPU nulo).
  - `_PulseSpeed` y `_PulseMin`: Velocidad y umbral mínimo de brillo del pulso.
  - `_DepthOcclusion` (0 o 1): Recorta el contorno si el mundo real está delante del objeto.

---

### 2.4 `Custom/MR_LitOcclusion` y `MR_LitOcclusionForwardPass.hlsl`
- **Ubicación:** `Assets/GameStuff/Shaders/MR_LitOcclusion.shader` y `MR_LitOcclusionForwardPass.hlsl`
- **Propósito:** Adaptación del shader SimpleLit de URP recableado para operar directamente con la **Meta Environment Depth API** (`Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl`) en lugar de AR Foundation.
- **Mecanismo técnico:**
  - Utiliza la macro `META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS` con las matrices de reproyección de profundidad de Meta (`_EnvironmentDepthReprojectionMatrices`).
  - Permite que las manos reales del usuario y los muebles de la habitación física tapen de forma realista la geometría virtual.
  - Consume el uniforme global `_EnvironmentDepthBias` gestionado por `MRDepthOcclusionController`.

---

### 2.5 `Meta/MRUK/Scene/HighlightsAndShadows`
- **Ubicación:** `Assets/GameStuff/Shaders/HighlightsAndShadows.shader`
- **Propósito:** Shader proporcionado por Meta XR MRUK aplicado a las mallas de plano y volumen de la habitación física.
- **Función:** Permite que las sombras virtuales arrojadas por los personajes y los reflejos virtuales se proyecten de forma transparente sobre las paredes, suelos y muebles reales del passthrough.
- **Propiedades configurables:** `_ShadowIntensity`, `_HighLightAttenuation`, `_HighlightOpacity`, `_EnvironmentDepthBias`.

---

## 3. Limitaciones Técnicas y Puntos de Atención

1. **Meta Depth API en Editor:**
   La oclusión por profundidad del entorno (`MRDepthOcclusionController`) solo está disponible en hardware real (Meta Quest 3) con passthrough activo. En el Editor de Unity, `EnvironmentDepthManager.IsSupported` evalúa a `false`, por lo que los shaders renderizan los objetos sin oclusión ambiental.
2. **Auto-oclusión en Agarre:**
   Al agarrar un objeto físico virtual con las manos reales, la profundidad calculada por el visor incluye la mano del usuario, lo que provocaría que el objeto parezca parcialmente "comido" por los dedos virtuales. Para resolver esto, `DepthOcclusionOnGrab` desactiva selectivamente `_DepthOcclusion` mientras el objeto permanezca en la mano.
3. **Cookies de Luz en URP:**
   La textura de linterna requiere que la opción de *Light Cookies* esté habilitada en el asset de pipeline URP (`UniversalRenderPipelineAsset`).
