# Directrices de Agentes y Asistentes IA — FNaF MR

Este documento sirve como referencia rápida y guía operativa para asistentes de IA y agentes automatizados que interactúen con el repositorio **Untiteled FNaF MR fangame**.

---

## 1. Perfil del Proyecto

- **Motor:** Unity 6 (`6000.3.18f1`).
- **Plataforma:** Android / Meta Quest 3 (ARM64, IL2CPP).
- **Entorno de desarrollo:** Arch Linux.
- **Render Pipeline:** Universal Render Pipeline (URP `17.3.0`).
- **SDKs Principales:** Meta XR SDK All (`209.0.0`), MR Utility Kit (MRUK `209.0.0`), Meta Interaction SDK, XR Interaction Toolkit (`3.4.1`).

---

## 2. Reglas Estrictas para Agentes

1. **Preservación de Metadatos `.meta`:**
   - Todo asset en Unity depende de su archivo `.meta` con GUID único.
   - **Nunca** borres, regeneres ni muevas un archivo sin su respectivo `.meta`.
2. **Espacio de Nombres y Estructura:**
   - El código propio del juego reside en `Assets/GameStuff/Scripts/` bajo el espacio de nombres global.
   - Los campos serializados en el Inspector utilizan `[Header]` y `[Tooltip]` descriptivos en **español**.
3. **No tocar assets de terceros ni plantillas:**
   - `Assets/MRTemplateAssets/`, `Assets/MetaAssets/` y `Assets/Samples/` son dependencias externas o plantillas. No introduzcas modificaciones salvo requerimiento explícito.
4. **Escenas Habilitadas en Build:**
   - `Assets/GameStuff/TestScenes/TestMenu.unity` (Índice 0)
   - `Assets/GameStuff/TestScenes/InitialTestRoom.unity` (Índice 1)
   - `Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity` (Índice 2)
   - `Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity` (Showcase de animatrónicos, excluida de build).

---

## 3. Índice de Documentación Técnica

Para información detallada del sistema, consulta:
- Arquitectura general y diagramas: [`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)
- Diseño de juego y mecánicas: [`docs/GAMEPLAY.md`](docs/GAMEPLAY.md)
- Referencia de scripts propios: [`docs/SCRIPTS.md`](docs/SCRIPTS.md)
- Shaders e iluminación URP: [`docs/SHADERS_Y_ILUMINACION.md`](docs/SHADERS_Y_ILUMINACION.md)
- Assets, modelos y licencias: [`docs/ASSETS.md`](docs/ASSETS.md)
- Configuración en Arch Linux y Quest 3: [`docs/SETUP_LINUX_QUEST3.md`](docs/SETUP_LINUX_QUEST3.md)
- Problemas conocidos y deuda técnica: [`docs/PROBLEMAS_CONOCIDOS.md`](docs/PROBLEMAS_CONOCIDOS.md)
- Hoja de ruta: [`docs/ROADMAP.md`](docs/ROADMAP.md)
- Guía de contribución: [`CONTRIBUTING.md`](CONTRIBUTING.md)
- Registro de cambios: [`CHANGELOG.md`](CHANGELOG.md)
