# Hoja de Ruta Técnica (Roadmap) — FNaF MR

Este documento define la planificación técnica del proyecto, deduciendo las próximas fases a partir de los sistemas implementados en el código y las mecánicas pendientes.

---

## Estado Actual: Alpha 0.1 (Sandbox de Realidad Mixta)
Actualmente el proyecto cuenta con:
- Validación y detección de superficies de la habitación real mediante Meta XR MRUK.
- Colocación física de SpringBonnie en camas reales con cinemática inversa (IK) de 2 huesos contra el suelo.
- Sistema de iluminación virtual y passthrough con shaders de penumbra (`Custom/MR_UltimateDarknessRevealer`).
- Linterna interactiva con encendido/apagado, cookie de luz y descarte de sombras parásitas.
- Inventario corporal magnético en cintura (`InventorySlot`, `InventoryItem`).
- Soporte para manos esqueléticas de FNaF Help Wanted con animación de dedos.
- Oclusión por profundidad de entorno en tiempo real con Meta Depth API.

---

## Fase 1: IA y Navegación Espacial de Animatrónicos
- [ ] **Navegación sobre geometría MRUK:** Integrar un sistema de navegación (Unity NavMesh generado dinámicamente sobre la malla de suelo de MRUK o waypoints basados en anclajes).
- [ ] **Comportamiento en Esquinas y Puertas:**
  - Implementar lógica de acecho donde los animatrónicos detecten marcos de puerta (`DOOR_FRAME`) y se detengan o agachen para ingresar.
  - Lógica para asomarse por las esquinas físicas de los muros de la habitación (`WALL_FACE` / `INNER_WALL_FACE`).
- [ ] **Transición entre estados de animación:** Conectar el Animator Controller con los marcadores procedurales (`ProceduralIdle` y poses `WallContact` / `ArmsExtended`).

---

## Fase 2: Ciclo de Juego y Mecánicas de Supervivencia
- [ ] **Gestor de Noches y Tiempo:**
  - Crear un controlador de partida (`GameManager` / `NightCycleController`) con reloj de noche (12 AM a 6 AM).
  - Curvas de agresividad y dificultad configurable por noche para los animatrónicos.
- [ ] **Gestión de Recursos (Batería / Energía):**
  - Consumo de batería en la linterna durante su uso continuo.
  - Sistema de recarga o límite de tiempo en penumbra.
- [ ] **Condiciones de Derrota y Victoria:**
  - Detección de proximidad letal del animatrónico al jugador cuando no es iluminado a tiempo.
  - Secuencia de *jumpscare* adaptada a realidad mixta respetando la posición de la cabeza en el espacio real.
  - Pantalla de victoria al alcanzar las 6:00 AM y retorno ordenado al menú principal.

---

## Fase 3: Audio Espacial y Efectos Inmersivos
- [ ] **Integración de Meta XR Audio:** Configurar fuentes de audio espacializadas en 3D para pasos, respiración de animatrónicos y golpes en paredes.
- [ ] **Mecánica de Máscara (FNaF 2):** Integrar el sonido existente `fnaf2maskon.mp3` con una mecánica interactiva de máscara colocable sobre la cabeza del usuario.

---

## Fase 4: Optimización y Pulido para Meta Quest 3
- [ ] **Ajustes del Menú:** Completar el panel de opciones de `FnafMenuController` (calibración de altura, volumen de audio, modo de manos vs mandos).
- [ ] **Perfilado de Rendimiento:** Auditoría con OVR Metrics Tool para asegurar tasa de refresco estable a 72 Hz o 90 Hz en Quest 3.
- [ ] **Limpieza de Paquetes y Assets:** Aislar assets de plantilla de `MRTemplateAssets` y muestras no utilizadas de `Samples/` para reducir el tamaño final del APK.
