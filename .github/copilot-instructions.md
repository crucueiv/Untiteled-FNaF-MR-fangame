# Instrucciones para Asistentes y GitHub Copilot

Este proyecto sigue directrices arquitectónicas y operativas estrictas para desarrollo en Unity 6 y Meta Quest 3.

> [!IMPORTANT]
> La referencia completa, restricciones de modificación y convenciones para agentes de IA se encuentran centralizadas en:
> 👉 [`AGENTS.md`](../AGENTS.md)

### Resumen Rápido:
1. **Versión de Unity:** `6000.3.18f1` con URP `17.3.0`.
2. **Plataforma:** Meta Quest 3 (Android ARM64, IL2CPP).
3. **Escenas en Build Settings:**
   - `Assets/GameStuff/TestScenes/TestMenu.unity`
   - `Assets/GameStuff/TestScenes/InitialTestRoom.unity`
   - `Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity`
4. **Metadatos:** No alteres, renombres ni elimines archivos `.meta`.
5. **Código Propio:** Ubicado en `Assets/GameStuff/Scripts/` (espacio de nombres global, tooltips y comentarios en español).
6. **Documentación:** Consulta la suite documental en la carpeta `docs/` (`docs/ARQUITECTURA.md`, `docs/GAMEPLAY.md`, `docs/SCRIPTS.md`, etc.).
