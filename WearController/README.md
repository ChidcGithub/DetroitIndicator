# Affection Wear

Wear OS controller for the Affection Indicator bridge, built with **Wear Compose
Material3 `1.7.1`** and **Material 3 Expressive**.

## Features

- **Nested menus**:
  - Main → Gauge states → Small / Big / Path / Visibility
  - Main → Custom buttons → New button → State picker
  - Main → Pairing
- **Multiple saved custom buttons** with a name, adjective and state.
  Tap a saved button to send it; swipe left to reveal the Delete action, then
  tap Delete or complete the full swipe to remove it.
- Pairing screen for the bridge URL and 10-character device ID.
- Online/offline status and command feedback.
- Dynamic color theming, expressive shapes, motion and compact Material 3
  controls.

## Requirements

- Android SDK 37 (compile SDK)
- JDK 17
- Gradle 9.8.2 (the included wrapper downloads it)
- A Wear OS 3+ device or emulator

The build uses AGP `9.1.0`, Kotlin `2.2.10`, Compose `1.10.0`, and Wear Compose
Material3 `1.7.1`. `targetSdk` is 36.

## Build

```powershell
$env:ANDROID_HOME = "D:\Android"          # or your SDK path
.\gradlew.bat assembleDebug
```

If your SDK is not found through `ANDROID_HOME`, create `local.properties`:

```
sdk.dir=C\:\\Android
```

The APK is written to:

```
app\build\outputs\apk\debug\app-debug.apk
```

## Install and run

```powershell
adb install -r app\build\outputs\apk\debug\app-debug.apk
adb shell am start -n com.chidc.affectionwear/.MainActivity
```

The emulator reaches the host at `http://10.0.2.2:5210`. For a physical watch,
start the bridge on the PC, allow TCP 5210 on the private network, and enter the
PC's LAN address, for example `http://192.168.1.20:5210`.

Pair with the device ID shown in **Affection Indicator → Settings → Bridge**.

## Custom buttons

1. Open **Custom buttons → New button**.
2. Enter a name and adjective.
3. Choose the state the button should send.
4. Tap **Save button**.

Saved buttons are stored as a JSON list in the watch's SharedPreferences. Tap a
saved button to send its state and both text values through the bridge. Swipe it
left to reveal the Material 3 Delete action; tap the action or complete the
swipe to remove it. The old single-preset preferences are migrated automatically.

The bridge sends `name:...`, `adj:...`, and the state to the desktop, so the
custom values become the desktop's current name/adjective.

## Project layout

```
app/src/main/java/com/chidc/affectionwear/
  MainActivity.kt
  ui/
    AffectionApp.kt        nested screens, saved buttons, swipe-to-delete
    theme/Theme.kt         Material 3 Expressive theme + dynamic color
  data/
    BridgeApi.kt           status and command HTTP calls
    PresetStore.kt         SharedPreferences and preset JSON list
```

## Notes

The app allows cleartext HTTP for local bridge development
(`android:usesCleartextTraffic="true"`). Use HTTPS if the bridge is exposed
beyond a trusted LAN. This is a fan-made experiment and is not affiliated with
Quantic Dream.
