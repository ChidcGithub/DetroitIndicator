# Affection Wear

A minimal Wear OS controller for the Affection Indicator bridge.

It stores the bridge URL and the 10-character device ID, checks whether the
desktop is online, and sends gauge commands through the bridge API.

## Requirements

- Android SDK (compile SDK 36) and JDK 17
- Gradle wrapper included (`gradlew.bat`)
- A Wear OS 3+ device or emulator
- The bridge server and desktop app running on the same network

## Build

```powershell
$env:ANDROID_HOME = "D:\Android"          # or your SDK path
.\gradlew.bat assembleDebug
```

The APK is written to:

```
app\build\outputs\apk\debug\app-debug.apk
```

If your SDK is not found through `ANDROID_HOME`, create `local.properties`:

```
sdk.dir=C\:\\Android
```

## Install on the emulator

```powershell
adb install -r app\build\outputs\apk\debug\app-debug.apk
adb shell am start -n com.chidc.affectionwear/.MainActivity
```

The included Wear emulator configuration (`WearAI_Render_Test`) uses a
384 × 384 round screen. The default URL is
`http://10.0.2.2:5210`, which reaches the host machine from the emulator.

## Install on a physical watch

1. Enable developer mode/adb on the watch.
2. Connect it over adb and install the APK.
3. Start the bridge on the PC.
4. Enter the PC's LAN URL, for example `http://192.168.1.20:5210`.
5. Enter the device ID shown in **Affection Indicator → Settings → Bridge**.
6. Press **Save & check**; the status should become **Desktop online**.

The app allows cleartext HTTP for local development
(`android:usesCleartextTraffic="true"`). Use HTTPS if the bridge is exposed
beyond a trusted LAN.

## Controls

- Small + / Small − (`inc` / `dec`)
- Big + / Big − (`inc+` / `dec+`)
- Unlock / Lock (`unlock` / `lock`)
- Name only (`neutral`)
- Show / Hide (`show` / `hide`)

The status line reports invalid IDs, an offline desktop, and bridge
connection failures. `onResume` refreshes the online state.

## Project layout

```
app/src/main/java/com/chidc/affectionwear/MainActivity.java
app/src/main/res/layout/activity_main.xml
app/src/main/AndroidManifest.xml
```

The app uses the Android framework only (no AndroidX), so the build has no
support-library dependency.
