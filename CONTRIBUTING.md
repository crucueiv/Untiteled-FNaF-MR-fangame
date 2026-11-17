# Guía de Contribución — FNaF MR

Gracias por contribuir al desarrollo de **Untiteled FNaF MR fangame**. Para mantener la coherencia y estabilidad técnica del proyecto en Unity 6 y Meta Quest 3, sigue estas directrices.

---

## 1. Flujo de Trabajo con Git

1. **Ramas:**
   - La rama `main` contiene el estado funcional estable del proyecto.
   - Crea ramas temáticas a partir de `main` siguiendo la convención:
     - `feature/nombre-mecanica` para nuevas funcionalidades.
     - `fix/descripcion-error` para correcciones de bugs.
     - `docs/nombre-tarea` para actualizaciones documentales.
2. **Commits:**
   - Escribe mensajes de commit claros en español o inglés describiendo el propósito del cambio.
   - Realiza commits atómicos que agrupen cambios lógicamente relacionados.
3. **Pull Requests:**
   - Todo cambio debe integrarse mediante Pull Request hacia `main`.
   - Verifica que el proyecto abra sin errores de compilación antes de solicitar revisión.

---

## 2. Convenciones de Código C#

1. **Espacio de Nombres y Organización:**
   - El código de gameplay reside bajo `Assets/GameStuff/Scripts/` en el espacio de nombres global.
   - Mantén la estructura de subcarpetas existente (`Characters`, `Debbug`, `DriveCharacters`, `Hands`, `Interactions`, `Inventory`, `Items`, `Managers`, `Menu`).
2. **Serialización e Inspector:**
   - Utiliza campos `[SerializeField] private` o campos `public` con atributos `[Header("...")]` y `[Tooltip("...")]` descriptivos en **español**.
   - Preserva los nombres y tipos de campos serializados para no romper referencias en prefabs y escenas existentes.
3. **Documentación:**
   - Toda clase pública o interna relevante, así como métodos públicos y propiedades, deben incluir comentarios XML (`/// <summary>`) claros en español explicando su propósito.
4. **Ciclo de Vida de Eventos:**
   - Toda suscripción a eventos de Meta XR o MRUK realizada en `OnEnable()` o `Start()` debe desuscribirse estrictamente en `OnDisable()` o `OnDestroy()`.

---

## 3. Manejo de Assets y Unity `.meta`

> [!IMPORTANT]
> **Preservación de GUIDs:**
> Unity asocia escenas, prefabs y materiales mediante los identificadores únicos (GUIDs) almacenados en los archivos `.meta`.
> - **Nunca** borres ni regeneres archivos `.meta`.
> - Siempre añade o mueve archivos junto a su `.meta` correspondiente (usa `git mv` si renombras por terminal).
> - Verifica con `git status` que no queden `.meta` huérfanos.

---

## 4. Gestión de Archivos Binarios y Git LFS

- Todos los archivos pesados (modelos 3D `*.fbx`/`*.dae`, texturas `*.png`/`*.jpg`, audios `*.mp3`/`*.wav`) están gestionados mediante **Git LFS**.
- Asegúrate de tener Git LFS inicializado (`git lfs install`) antes de añadir nuevos assets multimedia al repositorio.

---

## 5. Qué NO Subir al Repositorio

Asegúrate de que tu commit nunca incluya:
- Directorios generados por Unity: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `MemoryCaptures/`.
- Compilaciones y ejecutables: `Build/`, `Builds/`, `*.apk`, `*.aab`.
- Archivos de proyecto generados por IDEs: `*.csproj`, `*.sln`, `.vs/`.
- Claves de firma y secretos de Android: `*.keystore`, `*.jks`, `local.properties`.
- Logs locales de paquetes: `Packages/Aura/Editor/AuraLogs/`.
