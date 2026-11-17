# Problemas Conocidos y Deuda Técnica — FNaF MR

Este documento reúne las limitaciones observadas, olores de código (*code smells*), riesgos de licenciamiento y deuda técnica identificada en el proyecto.

---

## 1. Riesgos de Licencia y Assets de Terceros

> [!WARNING]
> **Redistribución de Modelos y Marcas de Terceros:**
> El repositorio versiona modelos FBX, texturas y audios extraídos del pack de FNaF Help Wanted compilado por JennyDPeriod. Aunque el proyecto es una obra de fans sin ánimo de lucro, la propiedad intelectual y los derechos de autor de los personajes corresponden a **Scottgames, LLC / Scott Cawthon**.
> 
> La redistribución directa de estos archivos binarios en repositorios públicos conlleva el riesgo de reclamaciones DMCA o infracción de derechos si no se gestionan como assets externos. La licencia MIT del repositorio aplica **estrictamente al código fuente C# y shaders propios**, excluyendo los modelos, texturas y sonidos de la franquicia.

---

## 2. Olores de Código y Acoplamiento Débil

### 2.1 Búsqueda Frágil por Nombre en Escena
- **Ubicación:** `Assets/GameStuff/Scripts/Managers/GrabbableItemManager.cs` (método `Start()`).
- **Problema:** El script intenta resolver la referencia a `VisualsManager` mediante:
  ```csharp
  GameObject.Find("VisualsManager")
  ```
- **Riesgo:** Si el GameObject es renombrado en la escena, o si el objeto se instancia en una escena donde `VisualsManager` no existe, la referencia será nula y arrojará excepciones en tiempo de ejecución.
- **Solución recomendada:** Serializar la referencia en el Inspector o utilizar `FindFirstObjectByType<VisualsManager>()`.

### 2.2 Clase Stub con Métodos Vacíos
- **Ubicación:** `Assets/GameStuff/Scripts/Managers/VisualsManager.cs`.
- **Problema:** Declara los métodos del ciclo de vida `Start()` y `Update()` vacíos. En Unity, dejar métodos de ciclo de vida vacíos genera una sobrecarga innecesaria en el despachador de eventos nativo de C++.
- **Solución recomendada:** Eliminar los métodos vacíos o implementar un patrón singleton limpio si actúa como almacén de referencias.

### 2.3 Tipografía en Nombre de Directorio
- **Ubicación:** `Assets/GameStuff/Scripts/Debbug/` (escrito con doble 'g').
- **Nota:** No se debe renombrar de forma imprevista para evitar romper los metadatos `.meta` asociados y las referencias serializadas en escenas y prefabs existentes.

---

## 3. Scripts de Editor sin Entrada de Menú

Los siguientes scripts estáticos de `Assets/Editor/` contienen métodos públicos de generación pero carecen del atributo `[MenuItem("...")]`:
- `DriveCharacterSetup.cs`: Métodos `GenerateAll()`, `ValidateAll()`, `AuditMaterials()`.
- `ProbeDriveTargets.cs`: Método `Execute()`.
- `RunDriveRigRebuild.cs`: Método `Execute()`.

Fueron diseñados para ejecutarse por línea de comandos (`-executeMethod`), herramientas MCP o scripts de compilación, pero resultan invisibles para un desarrollador que explore la barra de menús de Unity.

---

## 4. Limitaciones del Entorno y Renderizado

### 4.1 Meta Environment Depth API en Editor
- **Ubicación:** `MRDepthOcclusionController.cs` y shaders URP asociados.
- **Limitación:** La API de profundidad de Meta no genera texturas de oclusión en el Editor de Unity (evalúa `EnvironmentDepthManager.IsSupported == false`). Todas las pruebas de recorte por muebles reales o manos deben validarse en el visor físico Meta Quest 3.

### 4.2 Restricciones de Geometría de Habitación
- **Ubicación:** `RoomValidator.cs`.
- **Limitación:** Si la habitación física del usuario no ha sido escaneada con la herramienta de espacio del Quest 3 incluyendo al menos una puerta, una mesa y una cama o sofá, la validación fallará (`isRoomValid == false`) y el escenario de prueba MR no iniciará su ciclo normal de colocación.

---

## 5. Historial de Git y Archivos Pesados

- **Archivos históricos fuera de Git LFS:**
  En commits antiguos del repositorio se subieron archivos pesados directamente a la base de objetos de Git (por ejemplo texturas de skybox de 34 MB y 19 MB en `Assets/MRTemplateAssets/Materials/Skybox/`). Aunque las reglas de `.gitattributes` están actualmente corregidas para Git LFS, el historial del repositorio conserva estos blobs de gran tamaño al clonar sin filtros.
