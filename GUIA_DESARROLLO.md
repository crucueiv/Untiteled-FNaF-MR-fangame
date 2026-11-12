# Guía para continuar el proyecto en otro ordenador

Esta guía deja el entorno reproducible para abrir, editar y probar **Untitled FNaF MR Fangame** en otro portátil sin copiar las cachés del ordenador actual.

## Configuración que espera el proyecto

| Componente | Versión/valor del repositorio |
|---|---|
| Unity Editor | `6000.3.18f1` (Unity 6) |
| Plataforma | Meta Quest sobre Android, ARM64 |
| Meta XR SDK | `209.0.0` |
| URP | `17.3.0` |
| XR Interaction Toolkit | `3.4.1` |
| Escenas habilitadas en compilación | `Assets/GameStuff/TestScenes/TestMenu.unity` y `Assets/GameStuff/TestScenes/InitialTestRoom.unity` |
| Escena de showcase | `Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity` (no habilitada) |

La versión del editor está fijada en `ProjectSettings/ProjectVersion.txt`. Las dependencias directas están en `Packages/manifest.json` y las versiones resueltas en `Packages/packages-lock.json`. Conserva ambos archivos y abre el proyecto con esa versión exacta de Unity; abrirlo con otra versión puede actualizar archivos del proyecto y los paquetes.

## 1. Instalar las herramientas

1. Instala **Git**, **Git LFS** y **Unity Hub** para tu sistema operativo. Git LFS es obligatorio: modelos, imágenes, fuentes y audios del proyecto están almacenados como objetos LFS. Instálalo desde [git-lfs.com](https://git-lfs.com/) y, en una terminal, inicialízalo una vez para tu usuario:

   ```sh
   git lfs install
   ```
2. Desde Unity Hub instala el editor **Unity 6000.3.18f1**. Si no aparece en la lista normal, busca esa versión en el [archivo oficial de Unity](https://unity.com/releases/editor/archive).
3. En la instalación del editor añade estos módulos:
   - **Android Build Support**
   - **Android SDK & NDK Tools**
   - **OpenJDK**
4. No hace falta instalar Android Studio para compilar este proyecto: usa las versiones de SDK, NDK y JDK incluidas con ese editor de Unity. Evita cambiar manualmente sus rutas en `Preferences > External Tools`.
5. Deja espacio libre para el editor, los módulos Android, el caché de paquetes y la carpeta `Library` que Unity generará al importar el proyecto. El tamaño depende de la instalación y de la plataforma.

Para editar C#, **Visual Studio Code** es opcional. El repositorio ya recomienda la extensión de Unity en `.vscode/extensions.json` y tiene una configuración de depuración para adjuntarse al Editor. También se puede usar Visual Studio o Rider.

La documentación actual de Meta enumera los tres módulos Android anteriores como requisitos de desarrollo para Quest: [Software Setup de Meta](https://developers.meta.com/vr/documentation/unity/unity-software-setup/). Mantén la versión de Unity que fija este proyecto aunque Meta también documente versiones más nuevas.

## 2. Clonar y abrir

Abre una terminal en la carpeta donde guardas proyectos y ejecuta:

```sh
git clone https://github.com/crucueiv/Untiteled-FNaF-MR-fangame.git
cd Untiteled-FNaF-MR-fangame
```

En Unity Hub, pulsa **Add/Open** y selecciona la carpeta clonada (la que contiene `Assets`, `Packages` y `ProjectSettings`). Confirma que Unity Hub muestra **6000.3.18f1** antes de abrir.

En la primera apertura Unity importará los assets y descargará las dependencias que figuran en el manifiesto. Espera a que termine la importación y a que desaparezcan los errores de Package Manager antes de editar escenas o scripts. El primer arranque puede tardar bastante más que los siguientes.

Abre `Assets/GameStuff/TestScenes/TestMenu.unity` para probar el flujo de menú o `Assets/GameStuff/TestScenes/InitialTestRoom.unity` para probar la escena MR. Ambas están habilitadas en **File > Build Profiles / Scene List**. `DriveCharacterShowcase.unity` existe como showcase, pero no está habilitada para compilación.

## 3. Preparar el Quest para pruebas

Para ejecutar la experiencia en el visor hacen falta un Meta Quest compatible, la aplicación móvil Meta Horizon y una cuenta de desarrollador de Meta. En la aplicación móvil activa **Developer Mode** para el visor. Conecta el Quest al portátil con un cable USB-C de datos, acepta dentro del visor el permiso de depuración USB y elige **Always allow from this computer**.

Instrucciones oficiales de Meta:

- [Configurar el software y el dispositivo para desarrollo](https://developers.meta.com/vr/documentation/unity/unity-software-setup/)
- [Compilar, desplegar y probar en el visor](https://developers.meta.com/vr/documentation/unity/unity-deploy-and-test/)

Si el dispositivo no aparece en Unity, comprueba primero el modo desarrollador, el permiso USB, que el cable admita datos y que el visor esté desbloqueado. En Windows puede hacer falta el controlador USB ADB de Meta que indica su guía.

## 4. Compilar y ejecutar en el Quest

1. Con el proyecto abierto, ve a **File > Build Profiles** y activa/cambia a **Android** si no es la plataforma activa.
2. Comprueba en la lista de escenas que `InitialTestRoom.unity` está añadida y habilitada.
3. Conecta el Quest y selecciónalo en **Run Device** (usa **Refresh** si hace falta).
4. Para una compilación de desarrollo, activa **Development Build** y pulsa **Build And Run**. Elige una carpeta fuera de `Assets` y guarda ahí el APK.
5. Ponte el visor y acepta los permisos de la aplicación. La escena usa MR Utility Kit y necesita una habitación configurada en el visor. El validador de esta escena busca al menos un marco de puerta, una mesa y una cama o sofá.

Los ajustes Android ya se guardan en `ProjectSettings`: identificador `com.ivcrucue.UntiteledFNAFMRfangame`, ARM64 y backend IL2CPP. No cambies ajustes de XR, OpenXR ni del manifiesto Android sólo para resolver una importación; primero comprueba que el editor y los paquetes coinciden con las versiones fijadas.

La prueba en el Editor puede servir para editar y revisar la escena, pero la detección de habitación, el passthrough y los permisos MR deben verificarse en un Quest real.

## 5. Trabajar alternando entre sobremesa y portátil

Git es el medio de sincronización; no copies la carpeta completa entre equipos ni sincronices la caché con OneDrive, Dropbox u otro servicio.

Antes de terminar en un equipo:

```sh
git status
git add -A
git status
git commit -m "Describe el cambio"
git push
```

Al empezar en el otro equipo:

```sh
git pull --ff-only
```

Haz `commit` y `push` antes de cambiar de equipo; después haz `pull` antes de seguir. Si Git muestra cambios locales que no reconoces, no uses `reset --hard` ni descartes archivos hasta revisarlos. Si prefieres separar trabajo, crea una rama por tarea y súbela a GitHub antes de cambiar de ordenador.

Configura una vez tu identidad de Git en cada equipo (`git config --global user.name` y `git config --global user.email`) y autentícate con GitHub para hacer `push`. No incluyas contraseñas, tokens ni claves en comandos guardados en el repositorio.

## 6. Archivos que deben viajar y archivos que Unity recrea

**Versiona y conserva** `Assets` (incluidos todos sus `.meta`), `Packages/manifest.json`, `Packages/packages-lock.json` y `ProjectSettings`. Esos archivos contienen los recursos, identificadores, paquetes y ajustes necesarios para reabrir el proyecto.

**No copies ni subas** `Library`, `Temp`, `Obj`, `Logs`, `Build`/`Builds`, `UserSettings`, soluciones/proyectos generados ni los APK. Unity los recrea y el `.gitignore` ya excluye los más comunes. No borres, renombres o regeneres archivos `.meta`: las escenas y prefabs enlazan assets mediante esos GUID.

El `.gitattributes` del repositorio marca formatos binarios para Git LFS y el repositorio ya contiene punteros LFS para modelos, imágenes, fuentes y audio. Por eso, instala Git LFS y ejecuta `git lfs install` antes de clonar. Si ya clonaste sin Git LFS, instálalo y ejecuta `git lfs pull` dentro de la carpeta del proyecto para descargar los assets reales; sin ese paso Unity sólo verá archivos de texto diminutos en lugar de esos recursos. Repite la instalación en cada ordenador.

Las claves de firma Android son privadas. El `.gitignore` excluye `*.keystore`, `*.jks` y `local.properties`; no las fuerces al repositorio. Para pruebas locales no necesitas configurar una clave de publicación.

## 7. Si algo no arranca

- **Unity muestra otra versión:** cierra el proyecto sin aceptar una actualización y vuelve a abrirlo seleccionando `6000.3.18f1` en Unity Hub.
- **Packages no se resuelven:** deja que Unity Package Manager termine, comprueba conexión a Internet y revisa Console/Package Manager. El manifiesto contiene las versiones; evita borrar `packages-lock.json`.
- **Falta Android como plataforma:** en Unity Hub abre **Installs > Manage > Add modules** para `6000.3.18f1` e instala Android Build Support, Android SDK & NDK Tools y OpenJDK.
- **No detecta la habitación:** completa la configuración de habitación del visor y comprueba que MR Utility Kit tenga acceso a la escena del entorno. La habitación necesita marco de puerta, mesa y cama/sofá para que pase el `RoomValidator`.
- **No detecta el Quest:** vuelve a aceptar USB debugging en el visor; usa un cable de datos y habilita Developer Mode desde Meta Horizon.
- **Aparecen muchos cambios tras abrir Unity:** revisa que se abrió con `6000.3.18f1`. No reviertas a ciegas archivos de `ProjectSettings`, escenas o `.meta`; inspecciona el diff primero.

## Referencias

- [Repositorio en GitHub](https://github.com/crucueiv/Untiteled-FNaF-MR-fangame)
- [Versiones y archivo de Unity](https://unity.com/releases/editor/archive)
- [Requisitos de desarrollo para Meta Quest en Unity](https://developers.meta.com/vr/documentation/unity/unity-development-requirements/)
