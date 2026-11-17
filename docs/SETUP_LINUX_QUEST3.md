# Configuración y Despliegue en Arch Linux y Meta Quest 3

Esta guía detalla los pasos para configurar el entorno de desarrollo en **Arch Linux**, abrir el proyecto en Unity 6 y compilar/desplegar la aplicación en un visor **Meta Quest 3**.

---

## 1. Requisitos del Sistema y del Proyecto

| Parámetro | Valor / Versión requerida |
|---|---|
| Sistema operativo de desarrollo | Arch Linux (x86_64) |
| Unity Editor | `6000.3.18f1` (Unity 6) |
| Plataforma de destino | Android (Meta Quest 3) |
| Arquitectura objetivo | ARM64 |
| Backend de scripting | IL2CPP |
| Render Pipeline | Universal Render Pipeline (URP) `17.3.0` |
| Meta XR SDK | `com.meta.xr.sdk.all` `209.0.0` |
| XR Interaction Toolkit | `3.4.1` |
| Escenas activas en compilación | `Assets/GameStuff/TestScenes/TestMenu.unity`<br>`Assets/GameStuff/TestScenes/InitialTestRoom.unity`<br>`Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity` |
| Escena de showcase | `Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity` (no incluida en compilación) |

Las dependencias exactas y paquetes transitivos están bloqueados en `Packages/manifest.json` y `Packages/packages-lock.json`. La versión del motor se encuentra en `ProjectSettings/ProjectVersion.txt`.

---

## 2. Instalación de Herramientas en Arch Linux

### 2.1 Paquetes del Sistema
Instala Git, Git LFS y las herramientas de Android desde los repositorios oficiales de Arch:

```bash
sudo pacman -S git git-lfs android-tools
git lfs install
```

### 2.2 Reglas de Udev para Meta Quest
Para que `adb` reconozca el visor por USB sin requerir privilegios de superusuario:

1. Crea o edita `/etc/udev/rules.d/51-android.rules`:
   ```udev
   # Meta Quest 3 / Oculus
   SUBSYSTEM=="usb", ATTR{idVendor}=="2833", MODE="0666", GROUP="adbusers"
   ```
2. Asegúrate de que tu usuario pertenezca al grupo `adbusers`:
   ```bash
   sudo usermod -aG adbusers $USER
   ```
3. Recarga las reglas de udev:
   ```bash
   sudo udevadm control --reload-rules
   sudo udevadm trigger
   ```

### 2.3 Unity Hub
Instala Unity Hub (por ejemplo desde AUR mediante `yay -S unity-hub` o AppImage oficial).
Desde Unity Hub, instala la versión exacta: **Unity 6000.3.18f1**.

Al instalar el editor, añade obligatoriamente estos módulos:
- **Android Build Support**
- **Android SDK & NDK Tools**
- **OpenJDK**

> [!NOTE]
> No es necesario instalar Android Studio para compilar. Unity utiliza internamente las rutas del SDK, NDK e OpenJDK instaladas en su propio directorio de módulos.

---

## 3. Clonar y Abrir el Repositorio

```bash
git clone https://github.com/crucueiv/Untiteled-FNaF-MR-fangame.git
cd Untiteled-FNaF-MR-fangame
git lfs pull
```

1. Abre **Unity Hub**, pulsa en **Add** y selecciona la carpeta del proyecto.
2. Comprueba que el editor seleccionado sea `6000.3.18f1`.
3. Abre el proyecto y permite que el Package Manager resuelva dependencias e importe los assets en la primera ejecución.

---

## 4. Configuración del Meta Quest 3

1. **Modo Desarrollador:**
   - Abre la app móvil **Meta Horizon** en tu teléfono emparejado.
   - Ve a Menú > Dispositivos > Configuración del visor > Modo desarrollador y actívalo (requiere cuenta de organización verificada en Meta).
2. **Conexión USB:**
   - Conecta el Quest 3 al PC mediante cable USB-C compatible con datos.
   - Ponte el visor y acepta el diálogo: *"Permitir depuración USB"* y marca *"Permitir siempre desde este equipo"*.
3. **Verificación desde terminal:**
   ```bash
   adb devices
   ```
   Debe mostrar el identificador del dispositivo seguido de `device` (no `unauthorized`).

4. **Depuración inalámbrica (opcional):**
   ```bash
   adb tcpip 5555
   adb connect <IP_DEL_QUEST>:5555
   ```

---

## 5. Compilación y Despliegue

### 5.1 Desde el Editor de Unity
1. Ve a **File > Build Profiles**.
2. Selecciona la plataforma **Android** (pulsa *Switch Platform* si no está activa).
3. Verifica que la lista de escenas incluya y mantenga marcadas:
   - `Assets/GameStuff/TestScenes/TestMenu.unity`
   - `Assets/GameStuff/TestScenes/InitialTestRoom.unity`
   - `Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity`
4. Selecciona tu Quest 3 en **Run Device**.
5. Pulsa **Build And Run** (o genera el archivo `.apk` mediante **Build** y guárdalo fuera de `Assets/`).

### 5.2 Despliegue manual por ADB
Si compilas generando un APK local:
```bash
adb install -r nombre_compilacion.apk
```
Para ver los registros en tiempo real:
```bash
adb logcat -s Unity ActivityManager
```

---

## 6. Simulación y Herramientas XR en Linux

> [!NOTE]
> TODO: verificar soporte oficial de Meta XR Simulator en distribuciones Linux. Oficialmente Meta documenta compatibilidad completa de Meta XR Simulator en entornos Windows. En Linux, la verificación de Room Data, Passthrough y Depth API debe realizarse directamente sobre el dispositivo físico Meta Quest 3 conectado vía USB o Wi-Fi.

---

## 7. Resolución de Problemas Frecuentes

- **El dispositivo aparece como `unauthorized` en `adb devices`:**
  Ponte el visor y acepta el diálogo de depuración RSA. Si no aparece, desconecta y reconecta el cable USB.
- **Unity no detecta el dispositivo en Run Device:**
  Verifica que `adb devices` responda correctamente en la terminal del sistema. Reinicia el servidor ADB con `adb kill-server && adb start-server`.
- **Error de compilación Gradle o NDK:**
  En Unity, abre **Preferences > External Tools** y confirma que los checks de *Android SDK Tools Installed with Unity*, *Android NDK* y *JDK Installed with Unity* estén activos.
- **MRUK no genera la habitación:**
  Asegúrate de haber configurado el espacio físico previamente en el visor (**Configuración > Espacio > Configuración de habitación**). El validador requiere al menos puerta, mesa y cama/sofá reconocidos.
