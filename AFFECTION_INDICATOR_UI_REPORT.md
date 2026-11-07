# Detroit: Become Human — Affection / Likeability Meter UI — Complete File & Code Inventory

Target install: `D:\My things\Game\Detroit Become Human`
Workspace: `D:\My things\Learn\高二\Fun\DetroitIndicator`
Extraction date: 2026-11-06

---

## 0. Naming: what the "Affection / Likeability meter" actually is

There is **no UI asset named `AFFECTION`, `Likeability`, `Likability`, or `Sympathy`** in the shipped game.
The relationship meter the player sees (HANK: HOSTILE / TENSE / NEUTRAL / WARM / FRIEND, ALICE: DISTANT → FAMILY, etc.)
is internally called:

| Layer | Internal name |
|---|---|
| Scaleform GFX bundle | `GLOBAL_GAUGE_BENDING.gfx` |
| ActionScript main class | `comData.GLOBAL_GAUGE_BENDING` |
| ActionScript state class | `comData.global_gauge_bending.BendingState` |
| Scene / UI-resource object | `JaugeBending` (alias `JaugeBending - Alias`) |
| Lua template (wrapper) | `Jauge` |
| Lua template (Scaleform host) | `UI Jauge` |
| Lua game data driver | `GD Jauge Priority`, `GD Jauge Flowchart Setup` |
| Per-character Lua drivers | `GD Connor Relation Hank`, `GD Kara Relation Alice`, `GD Kara Relation Luther`, `GD Markus Relation North/Josh/Simon`, `GD Connor Relation Amanda` |

French `jauge` = "gauge". So the user-facing "affection meter" is the **Bending gauge**.
The word `AFFECTION` does appear in the game, but only in **checkpoint / story-progress identifiers**
(see §10) — those are not the meter UI.

---

## 1. Archive format recap

The game stores assets in `BigFile_PC.idx` + `BigFile_PC.d##` pairs.

- `BigFile_PC.idx` header: 105 bytes
- Record size: 28 bytes, big-endian
- Parsed record count: **373,748**
- Extracted index written to `tools/index.csv` (`index,type,f2,id,offset,size,unk,bigfile`)

All paths below are relative to the workspace.

---

## 2. Master file table (the meter UI, end to end)

| Role | Archive type | id | Index | Bigfile | Offset | Size | Extracted path |
|---|---|---:|---:|---:|---:|---:|---|
| **Main Scaleform GFX bundle** | 18002 | 193 | 143991 | 11 | 1192593408 | 67,478 | `extract/ui/143991_t18002_id193_f11.bin` |
| **Variant GFX bundle** (`GLOBAL_BENDING_NOTOTO`) | 18002 | 214 | 317102 | 20 | 1886255104 | 66,455 | `extract/ui/317102_t18002_id214_f20.bin` |
| Scene/resource container (`JaugeBending` + deviant labels) | 4288 | 7720 | 143769 | 11 | 1157957632 | 609,885 | `extract/ui/143769_t4288_id7720_f11.bin` |
| Scene/resource container (`JaugeBending`) | 4288 | 7725 | 143995 | 11 | 1192685568 | 188,524 | `extract/ui/143995_t4288_id7725_f11.bin` |
| UI templates (`Jauge`, `UI Jauge`, HUD UUIDs) | 19000 | 6826 | 305 | 0 | 2453504 | 454,644 | `extract/ui/000305_t19000_id6826_f0.bin` |
| UI data (`GD Jauge Priority`, `GD Jauge Flowchart Setup`, 60 `GUI_GAUGE` strings) | 19000 | 7085 | 314 | 0 | 3237888 | 161,880 | `extract/ui/000314_t19000_id7085_f0.bin` |
| Localization (25 languages, 68 gauge keys each) | 1016 | 2279 | 1313 | 0 | 85788672 | 274,411 | `extract/type1016/001313_t1016_id2279_f0.bin` |
| Lua game-mode bank (`Jauge`, `UI Jauge`) | 63 | 635 | 371478 | 24 | 2127671296 | 1,544,185 | `extract/luac_gm/` |
| Lua game-data bank (`GD Jauge Priority`, `GD Jauge Flowchart Setup`, Relation drivers) | 63 | 859 | 371482 | 24 | 2130120704 | 1,437,929 | `extract/luac_gd/` |

### Carved / decoded artifacts

| Artifact | Size | Source |
|---|---:|---|
| `extract/GLOBAL_GAUGE_BENDING.gfx` | 65,463 | carved at `0x172` in `143991_t18002_id193_f11.bin` |
| `extract/GLOBAL_GAUGE_BENDING.abc` | 64,823 | `DoABC` tag at `0x209`, length 64,828 |
| `extract/GLOBAL_BENDING_NOTOTO.gfx` | 64,438 | carved at `0x174` in `317102_t18002_id214_f20.bin` |
| `extract/GLOBAL_BENDING_NOTOTO.abc` | 63,796 | `DoABC` tag at `0x20A`, length 63,801 |
| `extract/Success_Arrow.dds` | 16,512 | zlib stream at `0x10146` |
| `extract/Lock_part2.dds` | 16,512 | zlib stream at `0x10360` |
| `extract/Lock_part1.dds` | 16,512 | zlib stream at `0x1049F` |
| `extract/gauge_localization_keys.tsv` | 2,290 | 68 English `GUI_GAUGE_*` keys |

---

## 3. Main GFX bundle: `GLOBAL_GAUGE_BENDING.gfx`

### 3.1 Container header (from `143991_t18002_id193_f11.bin`)

```
bundle size : 67,478 bytes
GFX magic   : 0x172
GFX length  : 0xFFB7 = 65,463
GFX end     : 0x10129
after GFX   : three zlib-compressed DDS files
```

### 3.2 GFX header

- Magic `GFX`
- Version `0x15`
- Stage: **1920 × 1080**
- Frame rate: **30 fps**
- Frame count: **1**

### 3.3 GFX tag table

| Offset | Tag | Name | Length |
|---:|---:|---|---:|
| `0x16` | 1000 | `ExporterInfo` (`Séquence 1`) | 32 |
| `0x38` | 69 | `FileAttributes` | 4 |
| `0x3E` | 9 | `SetBackgroundColor` | 3 |
| `0x43` | 86 | `DefineSceneAndFrameLabelData` | 15 |
| `0x58` | 1009 | `DefineExternalImage` — `Lock_part2.dds` (128×128) | 36 |
| `0x7E` | 1009 | `DefineExternalImage` — `Lock_part1.dds` (128×128) | 36 |
| `0xA4` | 1009 | `DefineExternalImage` — `Success_Arrow.dds` (128×128) | 42 |
| `0xD0` | 2 | `DefineShape` (character 4) | 41 |
| `0xFF` | 39 | `DefineSprite` (character 5) | 16 |
| `0x115` | 71 | `ImportAssets2` — `fonts/FONT_MASTER_ENG.swf` | 30 |
| `0x139` | 37 | `DefineEditText` (character 6, "NormalFont") | 62 |
| `0x17D` | 74 | `CSMTextSettings` | 12 |
| `0x18F` | 37 | `DefineEditText` (character 7, "BoldFont") | 60 |
| `0x1D1` | 74 | `CSMTextSettings` | 12 |
| `0x1E3` | 39 | `DefineSprite` (character 8) | 32 |
| `0x209` | 82 | **`DoABC`** — 64,828 bytes | 64,828 |
| `0xFF4B` | 76 | **`SymbolClass`** | 98 |

`SymbolClass` mapping (tag → class):

| Tag | Class |
|---:|---|
| 0 | `comData.GLOBAL_GAUGE_BENDING` (document class) |
| 1 | `Lock_part2` |
| 2 | `Lock_part1` |
| 3 | `Success_Arrow` |
| 5 | `ArrowPlus` |
| 8 | `fontSymb` |

`ImportAssets2` pulls the shared font library `fonts/FONT_MASTER_ENG.swf` — the gauge itself contains no font
outlines, only `DefineEditText` fields bound to the imported font symbols.

### 3.4 Embedded textures

All three are 128×128 DDS, zlib-compressed, stored after the GFX blob:

| File | Bundle offset | Compressed → raw | SHA-256 |
|---|---:|---:|---|
| `Success_Arrow.dds` | `0x10146` | 16,512 B | `609753ED7D9BED91E3EBDECFD1684576572BA98A68221F16F8516597A2FD96C2` |
| `Lock_part2.dds` | `0x10360` | 16,512 B | `D127940273BA66E3B2DB6BE8A180F476C03AB8001ECEF1857D8D32F3A685D6D6` |
| `Lock_part1.dds` | `0x1049F` | 16,512 B | `8282D3717B3E65F84CD9BF54EA00CD6D0B99DEAE575ED154B028C09CAE1E64F1` |

They are referenced only by `DefineExternalImage`; the actual pixel data lives in the trailing zlib streams
(`.dds` magic `44 44 53 20`, 128×128 header `7c 00 00 00 07 10 08 00 80 00 00 00`).

---

## 4. ActionScript UI code (decompiled from `DoABC`)

Decompiler: `pyflashkit` 1.3.0 (`parse_abc` + `flashkit.decompile.decompile_class`).
33 classes / 401 methods in the ABC.

> Note: pyflashkit output contains artifacts (`_s0_bN`, `=` where `==` is expected, `Vector._x`).
> Class names, method names, field names and structure are reliable; exact operator semantics should be
> cross-checked against the disassembly if you need bit-exact behavior.

### 4.1 Class inventory

| ABC idx | Class | File | Bytes |
|---:|---|---|---:|
| 32 | `comData.GLOBAL_GAUGE_BENDING` | `extract/abc_src/32_GLOBAL_GAUGE_BENDING.as` | 13,953 |
| 23 | `comData.global_gauge_bending.BendingState` | `extract/abc_src/23_BendingState.as` | 15,754 |
| 24 | `comData.global_gauge_bending.AdjectiveInsert` | `extract/abc_src/24_AdjectiveInsert.as` | 10,233 |
| 27 | `comData.global_gauge_bending.FourTrianglesAccordion` | `extract/abc_src/27_FourTrianglesAccordion.as` | 5,776 |
| 29 | `comData.global_gauge_bending.GhostLetters` | `extract/abc_src/29_GhostLetters.as` | 4,533 |
| 26 | `comLib.QD_LIB_COMPONENTS.TriangleBG` | `extract/abc_src/26_TriangleBG.as` | 9,196 |
| 28 | `comLib.QD_LIB_MINDPANELS.visuals.Unlock` | `extract/abc_src/28_Unlock.as` | 8,168 |
| 22 | `ArrowPlus` | `extract/abc_src/22_ArrowPlus.as` | 203 |
| 14 | `comLib.QD_LIB_GLOBAL.events.SoundEvent` | `extract/abc_src/14_SoundEvent.as` | 9,554 |
| 1 | `scaleform.clik.motion.Tween` | `extract/abc_src/01_Tween.as` | 14,529 |

### 4.2 `GLOBAL_GAUGE_BENDING` (main document class)

```as3
package comData
{
    public class GLOBAL_GAUGE_BENDING extends QD_UI_DATA
    {
        private const safeX:int = -42;
        private const safeY:int = -42;
        private var _frameCountForAdj:int = 4;
        private var _bg:TriangleBG;
        private var _container:Sprite;
        private var _nameLabel:QDText;
        private var _adjectiveLabel:AdjectiveInsert;
        private var _tendancy:BendingState;
        private var _connorState:int;
        private var _state:int;
        private var _lisible:Boolean = true;
        ...
```

Construction (lines 42–70):

```as3
this._container = new Sprite();
addChild(this._container);
this._container.y = this.safeY;                 // -42
this._bg = new TriangleBG(346, 150, 4);         // 4 triangles, 173 x 150 each
this._bg.alphas = [0.3, 0.5, 0.6, 0.3];
this._bg.x = 1920 - 450;                        // 1470
this._container.addChild(this._bg);

this._nameLabel = new QDText("NAME", 60,
    QDText.FONT_THIN, QDText.ALIGN_CENTER, QDText.ALIGN_H_RIGHT,
    false, -1, 16777215, 0, 0, 10, true);
this._nameLabel.alpha = 0.5;
this._container.addChild(this._nameLabel);

this._adjectiveLabel = new AdjectiveInsert();
this._container.addChild(this._adjectiveLabel);

this._tendancy = new BendingState();
this._container.addChild(this._tendancy);
```

Layout constants:

```as3
this._nameLabel.x                       = 1920 - this.safeX - 100;  // = 1862
this._nameLabel.y                       = this._bg.y + 68;
this._adjectiveLabel.x                  = 1920 - this.safeX - 100;
this._adjectiveLabel.y                  = this._nameLabel.y + this._nameLabel.getTextHeight() * 0.5;
this._tendancy.x                        = this._nameLabel.x + 40;   // = 1902
this._tendancy.y                        = this._nameLabel.y + 8;
```

→ the meter is **right-aligned to the right edge of the 1920×1080 HUD**, name at y≈68,
state chevrons at x≈1902, adjective label centered under the name.

Public API (these are the names called from Lua `QDF_*`):

```as3
override public function QDF_OnInitialize(arg:Object):void
override public function QDF_OnRelease():void
override public function QDF_OnLanguageChanged():void
public function QDF_setText(arg:Object):void
public function QDF_show(arg:Object = null):void
public function killAllDataTweens():void
private function onSoundEvent(e:SoundEvent):void
private function onEnterFrame(e:Event):void
private function flashAnimationName(frame:int):void
private function flashAnimationAdj(frame:int):void
private function _hide(t:Tween = null):void
```

`QDF_setText` — the HUD data contract (lines 131–171):

```as3
public function QDF_setText(arg:Object):void
{
    if (arg == null) { arg = {sName: "NAME"}; }
    if (arg.sName == null)       { arg.sName = "NAME"; }
    if (arg.iConnorState == null){ arg.iConnorState = -1; }
    if (arg.iState == null)      { arg.iState = -1; }
    if (arg.bLisible == null)    { arg.bLisible = true; }

    this._nameLoc      = arg.sName;
    this._adjectiveLoc = arg.sAdjective;
    this._state        = arg.iState;

    this._adjectiveLabel.hide(false);
    this._nameLabel.resetText(arg.sName, false);
    this._tendancy.setState(this._state);
    this._tendancy.setConnorState(arg.iConnorState);
    this._adjectiveLabel.setAdjective(arg.sAdjective);
    this._adjectiveLabel.setConnorState(arg.iConnorState);
    this._adjectiveLabel.setState(this._state);
    this._connorState = this._tendancy.connorState;
    ...
}
```

> pyflashkit renders `==` as `=` in this dump; the statements above are the intended equality checks.

`QDF_show` (lines 198–215) — the entrance animation:

```as3
public function QDF_show(arg:Object = null):void
{
    QDF_Play();
    ...
    this._tendancy.show(false);
    this._nameLabel.visible = true;
    this._nameLabel.alpha   = 0;
    ...
    QDF_SendEvent(SoundEvent.BENDING_NAME_SHOW);  // "UI_Sound_Gauge_Name"
    ...
}
```

Frame loop (lines ~228–280): the name label strobes and the four background triangles run their
50 ms chase for the first 12 frames. The adjective block starts at frame `_frameCountForAdj` = 4
(0.133 s), its plate appears at frame 7 (0.233 s), and frame 12 (0.4 s) is the end of its 8-frame
flicker. At that point a **1500 ms** `Tween` starts; `_hide` fires at 1.9 s and fades the name over
500 ms, with the adjective, state icon and background fading in the same window (gone by ~2.4 s).
The NOTOTO variant drops the auto-hide timer.

State → visual mapping is delegated to `BendingState` and `AdjectiveInsert`.

### 4.3 `BendingState` — the meter proper (chevrons, arrow, padlock)

```as3
public class BendingState extends Sprite
{
    public static const NONE:int              = 0xFFFFFFFF; // -1
    public static const INCREASE:int          = 0;  // ▲ small
    public static const DECREASE:int          = 1;  // ▼ small
    public static const UNLOCKED:int          = 2;  // padlock opens, good sound
    public static const LOCKED:int            = 3;  // padlock closes, bad sound
    public static const INCREASEPLUS:int      = 4;  // big arrow + chevron
    public static const DECREASEPLUS:int      = 5;  // big arrow + chevron
    public static const NOTDEVIANT_INFECTED:int = 0;
    public static const DEVIANT_INFECTED:int    = 1;
```

Methods:

| Method | Purpose |
|---|---|
| `setState(state:int)` | selects the `BendingState` visual (arrows / lock / unlock) |
| `setConnorState(state:int)` | Connor-only software-instability override (`-1` = normal relationship) |
| `reset()` | clears tweens and returns to idle |
| `show(instant:Boolean)` | plays the entrance tween |
| `hide(instant:Boolean)` | plays the exit tween |
| `unlockAnim()` | padlock opening animation |
| `chevronAnimation()` | arrow chevron tween |
| `onEnterFrame()` | per-frame tween bookkeeping |
| `onLanguageChanged()` | rebuilds localized labels |
| `getBGWidth()` / `setBGWidth()` | dynamic background width used by `FourTrianglesAccordion` |

The padlock caption is created here:

```as3
protected const UNLOCKPATH:String = "$GUI_GAUGE_UNLOCK";   // → "PATH UNLOCKED"
this._labelText = new QDText("$GUI_GAUGE_UNLOCK", 18,
    QDText.FONT_NORMAL, QDText.ALIGN_CENTER, QDText.ALIGN_H_LEFT, false, -1, 0);
```

Sound selection is in `setState`/`unlockAnim`; see §7.

### 4.4 `AdjectiveInsert` — the word under the name

```as3
public class AdjectiveInsert extends Sprite implements IEventDispatcher
{
    public static const NONE:int                = 0xFFFFFFFF;
    public static const NOTDEVIANT_INFECTED:int = 0;
    public static const DEVIANT_INFECTED:int    = 1;
    public static const FLASHENDFRAME:int       = 8;

    protected var _bg:FourTrianglesAccordion;
    protected var _adjLabel:QDText;
    protected var _hasAdjective:Boolean;
    protected var _showBG:Boolean = true;
    protected var _ghostLetters:GhostLetters;
```

Methods: `setAdjective(String)`, `setConnorState(int)`, `setState(int)`, `setShowBG(Boolean)`,
`show(Boolean)`, `frameShow(int)`, `hide(Boolean, int)`, `getBGWidth()`, `setBGWidth(int)`,
`onLanguageChanged()`.

If the Connor state is set, the class has a deviant presentation path that uses
`"$GUI_GAUGE_DEVIANT_NAME"` (= `SOFTWARE INSTABILITY`), disables the adjective background
(`setShowBG(false)`) and raises `_frameCountForAdj` to `8`; the normal relationship path keeps the
background and uses `_frameCountForAdj = 4`.

### 4.5 `FourTrianglesAccordion` — label background geometry

```as3
public class FourTrianglesAccordion extends Sprite
{
    protected var _triangleHeight:int;
    protected var _triangleWidth:int;
    protected var _triangleWRatio:Number = 1.155;
    protected var _desiredWidth:int;
    protected var _desiredHeight:int;
    protected var _firstTriangle:Sprite;
    protected var _accordion:Sprite;
    protected var _lastTriangles:Sprite;
    protected var _container:Sprite;
    protected var _firstTriAlpha:Number = 0.3;
    protected var _accordionAlpha:Number = 0.6;
```

This is the slanted/chevron-ended plate behind the adjective. It expands horizontally with
`setDesiredWidth`-style tweening driven by `AdjectiveInsert.setBGWidth`.

Instantiated as `new FourTrianglesAccordion(212, 32, 0, 1)` in `AdjectiveInsert`, and with the same
`(minWidth, 32, 0, 1)` arguments in `BendingState`. The class defaults above are therefore overridden:
the body alpha is **1.0**, not 0.6. The two serrated end groups use hard-coded alphas
`0.3 / 0.4` (first group) and `0.5 / 0.4` (last group); the `_firstTriAlpha` argument is unused.

Geometry: `TW = 32 × 1.155 = 36.96`. The body path is `(0,0) → (D,0) → (D − TW/2,32) → (−TW/2,32)`,
the first group sits at `x = −TW`, and the last group sits at `x = D − TW + TW/2 = D − TW/2`.
`AdjectiveInsert` mirrors it with `scaleX = −1`, `x = 16`, `y = 14`; `BendingState` uses
`scaleX = −1`, `x = −4`, `y = 74`, `D = textWidth + 58`.

`AdjectiveInsert` calls `QDMCTools.colorChange(_bg, 0xFFFFFF)` and `BendingState` calls it with
`QDColor.RED` / `QDColor.BLUE`. The fill is drawn as black; `QDMCTools.colorChange` constructs a
`ColorTransform` and sets its `color` property, which sets the three colour multipliers to 0 and the
three offsets to the colour (a solid tint that preserves alpha). The final `QDText` color in
`AdjectiveInsert.frameShow` is 0 (black), which confirms the normal adjective is black text on a light
plate. The shared constants are `QDColor.BLUE = 0xFFFCA2FF` (`#FCA2FF`) and
`QDColor.RED = 0xFFFF3000` (`#FF3000`). The `BLUE` name is the source constant,
but its literal value is a light magenta; the shipped gauge's positive/UNLOCKED
blue is `#80EEFF` (the common blue fill in the extracted UI GFX), so the demo
uses `#80EEFF` and keeps `QDColor.RED` for negative.

### 4.6 `TriangleBG` — the 4-triangle background behind the name

```as3
public class TriangleBG extends Sprite
{
    protected var _color:uint = 0;
    protected var _triangleWidth:int = -64;
    protected var _triangleHeight:int = 128;
    protected var _triangleCount:int;
    protected var _maxWidth:int;
    protected var _container:Sprite;
    protected var _triangles:Vector.<Sprite>;
    protected var _blinkAnimationTime:int = 50;
    protected var _start:int = 0;
    protected var _alphas:Array;
```

Instantiated as `new TriangleBG(346, 150, 4)` — 4 black triangles, each with a 173 px base and 150 px height.
They do **not** form a rectangle: even triangles have their base on top, odd triangles have their base on the
bottom, so together they tile a right-leaning parallelogram band. The top edge is 346 px wide, the bottom edge
is 346 px wide and shifted 86.5 px to the right; the bounding box is 432.5 × 150 px. The per-triangle alphas
are `[0.3, 0.5, 0.6, 0.3]`.

Placed at `x = 1470`, `y = 0` inside a container offset by `safeY = -42`: on a 1920 × 1080 screen the band
occupies `x = 1470 … 1902.5`, `y = -42 … 108`. Its top 42 px are above the screen edge, so the visible part is
a 108 px tall parallelogram strip in the top-right corner.

### 4.7 `Unlock` — padlock animation

```as3
public class Unlock extends Sprite
{
    public static const LOCKSTATE_HIDE:int   = 0;
    public static const LOCKSTATE_LOCK:int   = 1;
    public static const LOCKSTATE_UNLOCK:int = 2;

    private var lockPart1:Sprite;
    private var lockPart2:Sprite;
    private var _instantUnlock:Boolean;
    private var _lockState:int = 2;
    protected var _color:uint = -220417;
    public var hideAfterUnlock:Boolean = false;
    private var _isDeviant:Boolean;
```

Visible = padlock. Closed uses `Lock_part1` + `Lock_part2` at alpha 0.5; unlock fades both to 1 and
slides them apart to `x = ±10`. (`Success_Arrow` is embedded in the same GFX, but the decompiled gauge
ActionScript never references it, so it is not part of the padlock animation.)

### 4.8 `GhostLetters` — glitch text for SOFTWARE INSTABILITY

Splits each character into its own `TextField` and tweens them with random offsets,
creating the "deviant" jitter effect. Constructor: `GhostLetters(text, fontSize, fontId)`.

### 4.9 `ArrowPlus`

Empty `Sprite` subclass used as the SymbolClass for the 64 × 96 arrow shape (tag 5), which lives in the
GFX display list rather than in AS3. `BendingState` instantiates two copies: the visible big arrow
(`_yves`) and a second copy used as its own moving mask (`_maskedYves`). Sliding the mask over the
arrow produces the vertical wipe used by INCREASEPLUS / DECREASEPLUS.

### 4.10 `SoundEvent` constants

```as3
public static const BENDING_INCREASE:String        = "UI_Sound_Gauge_Increase_Small";
public static const BENDING_INCREASEPLUS:String    = "UI_Sound_Gauge_Increase_Big";
public static const BENDING_DECREASE:String        = "UI_Sound_Gauge_Decrease_Small";
public static const BENDING_DECREASEPLUS:String    = "UI_Sound_Gauge_Decrease_Big";
public static const BENDING_NAME_SHOW:String       = "UI_Sound_Gauge_Name";
public static const BENDING_ADJ_SHOW:String        = "UI_Sound_Gauge_Adjective";
public static const BENDING_UNLOCKED:String        = "UI_Sound_Gauge_Unlock_Good";
public static const BENDING_LOCKED:String          = "UI_Sound_Gauge_Unlock_Bad";
public static const BENDING_CONNOR_DEVIANT:String  = "UI_Sound_Gauge_Deviant";
public static const BENDING_CONNOR_NOTDEVIANT:String = "UI_Sound_Gauge_SoftwareInstability";
public static const PAUSE_MENU_VALIDATE:String     = "UI_Sound_Menu_Valid";
```

---

## 5. HUD data contract (variable UUIDs)

These four variables are the entire interface between game Lua and the Scaleform gauge.
They are set on the `Jauge` game object (`GO_9e4624a2d820efab`).

| Variable | UUID | Type | Meaning |
|---|---|---|---|
| `sName` | `4891ae23-8a67-4483-a190-dffe2a746de8` | String | localized name, e.g. `GUI_GAUGE_HANK_NAME` |
| `sAdjective` | `8682fc2f-7c99-482e-b6c7-c5e039a59c2f` | String | localized relationship word, e.g. `GUI_GAUGE_HANK_WARM` |
| `iState` | `4b727b51-cb08-4ec8-ae59-dccd7e69664a` | Int | `BendingState` constant (-1/0/1/2/3/4/5) |
| `iConnorState` | `721b4e3c-8e5e-46aa-be66-a072aa67fdd8` | Int | `-1` normal, `0` NOTDEVIANT_INFECTED, `1` DEVIANT_INFECTED |

`UI Jauge.Show` explicitly resets `iConnorState` to `-1` after `QDF_show`
(`extract/luac_gm/371478_t63_id635_f24_0x68A74_e489d39d21174b6eUI Jauge.lua`, lines 31–36):

```lua
SCRIPT_BLOCK_2 = {
  Run = function(self, instance)
    CppCall(QDScript.GC_GUI_2D[""], "CallFunctionWithVariables", "QDF_setText",
            QDScript.GO_9e4624a2d820efab[""], QDScript.GO_e489d39d21174b6e[""])
    CppCall(QDScript.GC_GUI_2D[""], "CallFunction", "QDF_show",
            QDScript.GO_e489d39d21174b6e[""])
    CppCall(QDScript.GC_VARIABLE_CONTAINER[""], "SetVariableInt",
            "721b4e3c-8e5e-46aa-be66-a072aa67fdd8", -1,
            QDScript.GO_9e4624a2d820efab[""])
  end
}
```

---

## 6. Lua display pipeline

### 6.1 Template `Jauge` — UUID `9876c315-b0ac-4ded-b644-6788cd6c60a5`, object `GO_9e4624a2d820efab`

File: `extract/luac_gm/371478_t63_id635_f24_0x678E1_9e4624a2d820efabJauge.lua`

- `SCRIPT_BLOCK_2.Run`: `ActivateObject(GO_e489d39d21174b6e)` then `GAME_OBJECT_NODE:Show`
- `GAME_OBJECT_NODE.Show`: activates `GO_e489d39d21174b6e` and calls the `UI Jauge` template's `Show`
- `USER_FUNCTION_2.Hide`: `DeactivateObject(GO_e489d39d21174b6e)`

Call graph:

```
Jauge.Show
  → ActivateObject(UI Jauge object)
  → UI Jauge.Show
       → QDF_setText(Jauge object, UI Jauge object)   -- reads sName/sAdjective/iState/iConnorState
       → QDF_show(UI Jauge object)                    -- entrance anim + sound
       → iConnorState = -1
Jauge.Hide
  → DeactivateObject(UI Jauge object)
```

### 6.2 Template `UI Jauge` — UUID `4e9b0795-9915-4e78-bedd-233bda4d3a6c`, object `GO_e489d39d21174b6e`

File: `extract/luac_gm/371478_t63_id635_f24_0x68A74_e489d39d21174b6eUI Jauge.lua`

- `GC_GUI_2D.OnScriptEvent`: receives the UI event name and runs a switch
- `SWITCH.Test`: `UI_DATA_KILL` → `SCRIPT_BLOCK.Run` → `DeactivateObject(GO_e489d39d21174b6e)`
- `USER_FUNCTION.Show` → `SCRIPT_BLOCK_2` (above)
- `GC_GUI_2D.OnInit` raises `USER_EVENT.Activated`

### 6.3 `GD Jauge Priority` — UUID `b1a34902-390d-478b-a5fb-008e28dfe4fc`, object `GO_9494ac0ff3da2456`

File: `extract/luac_gd/371482_t63_id859_f24_0xF6BAB_9494ac0ff3da2456GD Jauge Priority.lua`

This is the generic driver: every gauge participant has a `M_Display` node that:
1. activates the `Jauge` object `GO_9e4624a2d820efab`
2. calls `Jauge.Show`
3. sets the four HUD variables from a per-character metrics container

Representative block (lines 1501–1515, Hank):

```lua
CppCall(..., "SetVariableString", "4891ae23-8a67-4483-a190-dffe2a746de8",
        CppCall(..., "GetVariableString", "151ecec7-2e6e-4bb5-b04c-d4bb146b0d1c", GO_c368f8ca69bd9565), game_object)
CppCall(..., "SetVariableString", "8682fc2f-7c99-482e-b6c7-c5e039a59c2f", "", game_object)
CppCall(..., "SetVariableInt",    "4b727b51-cb08-4ec8-ae59-dccd7e69664a",
        CppCall(..., "GetVariableInt", "c5624f11-7b95-4d77-ab5a-2bdf7607801a", GO_c368f8ca69bd9565), game_object)
CppCall(..., "SetVariableInt",    "721b4e3c-8e5e-46aa-be66-a072aa67fdd8", -1, game_object)
```

Other participants wired the same way (different metric containers):

| Character | Metrics object | Name var | Adjective var | State var |
|---|---|---|---|---|
| Hank | `GO_c368f8ca69bd9565` | `151ecec7-…` | `35161beb-…` | `c5624f11-…` |
| Alice | `GO_b6f235d10252856a` | `5cd03eb7-…` | `b6bf7686-…` | `236c730d-…` |
| Luther | `GO_cdf9039ae99e5882` | `cb90058e-…` | `4a7762eb-…` | `13b678b7-…` |
| Jericho | `GO_dff4a6963f8bce41` | `2eeeac39-…` | `90534432-…` | `a29b56a2-…` |
| North | `GO_c44205f6fd2c91af` | `3a292263-…` | `f9cafecb-…` | `2600c217-…` |
| Josh | `GO_155349ad80e9b79f` | `592f5209-…` | `5c5e21ec-…` | `569db1e3-…` |
| Simon | `GO_33accccf113d52ff` | `44722ebb-…` | `3d37bb23-…` | `6bc940b5-…` |
| Amanda | `GO_ed22cabff9c55e97` | `fa0ceac4-…` | — | `7fc58b64-…` |
| Public Opinion | `GO_15d77ed32e94a855` | `173686f9-…` | — | — |

Only Connor/Amanda paths set `iConnorState` from a game variable
(`654a3595-3553-4285-923e-43ea667fff10`) — that is the SOFTWARE INSTABILITY mode.

#### Proof that these localization keys are the adjective text

The `GD Jauge Priority` payload in `000314_t19000_id7085_f0.bin` contains the literal value-table that
maps internal `_str<Name>` variables to `$GUI_GAUGE_*` keys. Example (offset `0x9D18`):

```
_strHANK_NEUTRAL   →  $GUI_GAUGE_HANK_NEUTRAL   →  UUID 86485cfb-de0e-4903-952f-97eb037f7562
_strHANK_FRIEND    →  $GUI_GAUGE_HANK_FRIEND    →  UUID f6f3e416-7b83-4652-aef2-d203186a774f
_strAdjectiveHank  →                              UUID 35161beb-9af6-47c8-aabf-bd27788640ab
_strPastAdjectiveHank →                           UUID c7bd6576-974b-4f13-b431-8c6400e7a6b8
```

`35161beb-9af6-47c8-aabf-bd27788640ab` is exactly the UUID read into the `sAdjective` HUD variable by the
Hank `Jauge` driver (§6.4). This closes the loop:
**localized `GUI_GAUGE_*` string → `_strAdjective<Name>` → `sAdjective` → `AdjectiveInsert` → on-screen word.**

The container holds 60 distinct `GUI_GAUGE_*` keys (the full adjective vocabulary); the localization
resource holds 68. The 8 extra are `GUI_GAUGE_LEADER_NEGATIVE`, `GUI_GAUGE_UNLOCK`,
`GUI_GAUGE_STRESS_ALICE`, `GUI_GAUGE_ZLATKO_PREPARING`, `GUI_GAUGE_ZLATKO_PROGRESS`,
`GUI_GAUGE_ZLATKO_RESET`, `GUI_GAUGE_UNIT_M`, `GUI_GAUGE_UNIT_POURCENT` — i.e. the padlock label,
one unused legacy adjective, the sibling stress/Zlatko gauges, and unit glyphs.

### 6.4 Per-character relation drivers

These are the scripts that actually call `Jauge.Show` with `iState = 2` (UNLOCKED) or `3` (LOCKED)
at the moment a relationship threshold changes:

| File | Character |
|---|---|
| `extract/luac_gd/371482_t63_id859_f24_0x2BE02_5fcc3acc339eb1ccGD Connor Relation Hank.lua` | Hank |
| `extract/luac_gd/371482_t63_id859_f24_0x25_850bb6c766de3e21GD Kara Relation Alice.lua` | Alice |
| `extract/luac_gd/371482_t63_id859_f24_0x1C2A5_bb70d41d36162c10GD Kara Relation Luther.lua` | Luther |
| `extract/luac_gd/371482_t63_id859_f24_0x91B7D_9298d65fc95272e6GD Markus Relation North.lua` | North |
| `extract/luac_gd/371482_t63_id859_f24_0xB2C66_2811180b9add72a6GD Markus Relation Josh.lua` | Josh |
| `extract/luac_gd/371482_t63_id859_f24_0xCC4DA_870fdb706687343cGD Markus Relation Simon.lua` | Simon |
| `extract/luac_gd/371482_t63_id859_f24_0x147332_4fa8b374f89af864GD Connor Relation Amanda.lua` | Amanda |

Example — Hank, `GAME_OBJECT_NODE_4.Show` (lines 2193–2211):

```lua
GAME_OBJECT_NODE_4 = {
  Show = function(self, instance)
    local instance_context = ""
    local game_object_instance = QDScript.GO_9e4624a2d820efab[instance_context]
    if game_object_instance then
      self.__init_object_instance__(instance, game_object_instance)
      CppCall(QDScript.GAME_ENGINE_SERVICE[""], "ActivateObject", game_object_instance)
    end
    local template_instance = QDR.Luna._get_template_instance("9876c315-b0ac-4ded-b644-6788cd6c60a5", instance_context)
    if template_instance then
      template_instance.functions.Show(template_instance)
    end
  end,
  __init_object_instance__ = function(instance, game_object)
    CppCall(..., "SetVariableString", "4891ae23-8a67-4483-a190-dffe2a746de8",
            CppCall(..., "GetVariableString", "151ecec7-2e6e-4bb5-b04c-d4bb146b0d1c", GO_c368f8ca69bd9565), game_object)
    CppCall(..., "SetVariableString", "8682fc2f-7c99-482e-b6c7-c5e039a59c2f",
            CppCall(..., "GetVariableString", "35161beb-9af6-47c8-aabf-bd27788640ab", GO_c368f8ca69bd9565), game_object)
    CppCall(..., "SetVariableInt",    "4b727b51-cb08-4ec8-ae59-dccd7e69664a", 2,  game_object)  -- UNLOCKED
    CppCall(..., "SetVariableInt",    "721b4e3c-8e5e-46aa-be66-a072aa67fdd8", -1, game_object)
  end
}
```

`GAME_OBJECT_NODE_7` is the mirror case with `iState = 3` (LOCKED).
`UpdateAdjective*` nodes refresh the adjective without re-showing the whole gauge.

### 6.5 `GD Jauge Flowchart Setup`

File: `extract/luac_gd/371482_t63_id859_f24_0x1190EF_83cc2b3a63ab7f5aGD Jauge Flowchart Setup.lua`

This script owns the *chart* presentation of relationships (the branch diagram), not the HUD meter.
It sets `6aa5ce06-ee82-40d8-b2e5-c757d01cbfeb` to `1/3/4/0` to mark node states, and uses
`IsObjectRegistered` gating. It does **not** reference `Jauge`, `UI Jauge`, `QDF_setText`, or the four
HUD UUIDs — keep it separate from the meter UI.

---

## 7. Sound events (UI dispatch)

The gauge emits ten distinct sound events. They are dispatched through
`SoundEvent.DISPATCH_UI_SOUND` and handled by `GLOBAL_GAUGE_BENDING.onSoundEvent`, which calls
`QDF_SendEvent(name)`.

| Constant | Event string | Trigger |
|---|---|---|
| `BENDING_INCREASE` | `UI_Sound_Gauge_Increase_Small` | small positive change |
| `BENDING_INCREASEPLUS` | `UI_Sound_Gauge_Increase_Big` | large positive change |
| `BENDING_DECREASE` | `UI_Sound_Gauge_Decrease_Small` | small negative change |
| `BENDING_DECREASEPLUS` | `UI_Sound_Gauge_Decrease_Big` | large negative change |
| `BENDING_NAME_SHOW` | `UI_Sound_Gauge_Name` | name revealed |
| `BENDING_ADJ_SHOW` | `UI_Sound_Gauge_Adjective` | adjective revealed |
| `BENDING_UNLOCKED` | `UI_Sound_Gauge_Unlock_Good` | path unlocked |
| `BENDING_LOCKED` | `UI_Sound_Gauge_Unlock_Bad` | path locked |
| `BENDING_CONNOR_DEVIANT` | `UI_Sound_Gauge_Deviant` | Connor deviant mode |
| `BENDING_CONNOR_NOTDEVIANT` | `UI_Sound_Gauge_SoftwareInstability` | Connor software-instability mode |

> The event names are confirmed in the ABC. The actual Wwise/FMOD bank files that implement them were
> not located in the extracted set — the UI sends the event names only. This is the one gap in the
> inventory.

---

## 8. Localization

Resource: `extract/type1016/001313_t1016_id2279_f0.bin`
Format: `LOCALIZ_` v6, data size 274,359, **25 languages × 163 entries**, **68 `GUI_GAUGE_*` keys per language**.

Languages: `FRE ENG GER ITA SPA DUT POR SWE DAN NOR FIN RUS POL JPN KOR CHI GRE CZE HUN CRO MEX BRA TUR ARA SCH`.

Exported English table: `extract/gauge_localization_keys.tsv` (68 rows).

Key groups:

| Group | Keys |
|---|---|
| Hank | `GUI_GAUGE_HANK_NAME`, `_HOSTILE`, `_TENSE`, `_NEUTRAL`, `_WARM`, `_FRIEND` |
| Alice | `GUI_GAUGE_ALICE_NAME`, `_DISTANT`, `_CLOSE`, `_TRUSTED`, `_WARM`, `_NEUTRAL`, `_FAMILY` |
| Luther | `GUI_GAUGE_LUTHER_NAME`, `_WARY`, `_NEUTRAL`, `_TRUSTED`, `_CLOSE`, `_FAMILY` |
| Jericho | `GUI_GAUGE_LEADER_NAME`, `_REJECTED`, `_UNPOPULAR`, `_NEGATIVE`, `_NEUTRAL`, `_ADMIRED`, `_RESPECTED`, `_LEADER` |
| North | `GUI_GAUGE_NORTH_NAME`, `_HOSTILE`, `_RESENTFUL`, `_NEUTRAL`, `_FRIEND`, `_COMPANION`, `_LOVER` |
| Josh | `GUI_GAUGE_JOSH_NAME`, `_HOSTILE`, `_RESENTFUL`, `_NEUTRAL`, `_FRIEND`, `_COMPANION` |
| Simon | `GUI_GAUGE_SIMON_NAME`, `_HOSTILE`, `_RESENTFUL`, `_NEUTRAL`, `_FRIEND`, `_COMPANION` |
| Amanda | `GUI_GAUGE_AMANDA_NAME`, `_DISTRUSTED`, `_NEUTRAL`, `_TRUSTED`, `_BETRAYED` |
| Public Opinion | `GUI_GAUGE_PUBLIC_NAME`, `_HATED`, `_HOSTILE`, `_SCEPTICAL`, `_NEUTRAL`, `_SYMPATHETIC`, `_SUPPORTIVE` |
| Connor (software instability) | `GUI_GAUGE_DEVIANT_NAME` = `<QD_THIN>SOFTWARE <QD_NORMAL>INSTABILITY`, `_MACHINE`, `_DEVIANT` |
| Shared | `GUI_GAUGE_UNLOCK` = `PATH UNLOCKED`, `GUI_GAUGE_UNIT_M`, `GUI_GAUGE_UNIT_POURCENT` |
| Sibling gauges (same table) | `GUI_GAUGE_STRESS_ALICE`, `GUI_GAUGE_ZLATKO_PREPARING`, `_PROGRESS`, `_RESET` |

The `GUI_GAUGE_STRESS_ALICE` / `ZLATKO_*` keys belong to the stress / memory-wipe gauges that share the
same localization table, not to the affection meter.

---

## 9. Variant: `GLOBAL_BENDING_NOTOTO`

File: `extract/ui/317102_t18002_id214_f20.bin` (type 18002, id 214, bigfile 20, offset 1,886,255,104, size 66,455)
Carved: `extract/GLOBAL_BENDING_NOTOTO.gfx` (offset `0x174`, length 64,438) and `extract/GLOBAL_BENDING_NOTOTO.abc`.

Differences vs. `GLOBAL_GAUGE_BENDING` (from `difflib` on the decompiled classes):

| Aspect | `GLOBAL_GAUGE_BENDING` | `GLOBAL_BENDING_NOTOTO` |
|---|---|---|
| Document class | `comData.GLOBAL_GAUGE_BENDING` | `comData.GLOBAL_BENDING_NOTOTO` |
| `_frameCountForAdj` | `4` | `8` |
| Auto-hide after adjective | `Tween(this).to(1500, {}).onComplete(this._hide)` | **absent** |
| Extra method | — | `QDF_hide` |
| `_hide` | does not kill tweens | kills `_nameLabel` / `_adjectiveLabel` / `_tendancy` tweens |
| `SymbolClass` | same 6 symbols | same 6 symbols |
| Textures | same 3 DDS | same 3 DDS |

`NOTOTO` appears to be the "no auto-timeout" version used in scenes where the meter must stay on screen
until explicitly hidden (e.g. the flowchart / investigation views).

---

## 10. Literal `AFFECTION` strings — NOT the meter

Searching the whole extracted tree for `AFFECTION|Affection|LIKEABILITY|Likeability|SYMPATHY|RELATIONSHIP`
yields only checkpoint/story data:

- `extract/ui/343388_t4288_id15157_f21.bin` (type 4288, id 15157, index 343388, bigfile 21,
  offset 2,117,326,848, size 285,282) contains:
  - `X0914K_CHECKPOINT_LUTHERFREE_PC_X09KKARA_AFFECTION`
  - `X0914K_CHECKPOINT_LUTHERFREE_PC_X09KLUTHER_AFFECTION`
  - `X0914K_CHECKPOINT_LUTHERFREE_FA_X09KALICE_AFFECTIONALICE`
- `extract/type1016/340223_t1016_id2078_f21.bin` (type 1016, id 2078, index 340223, bigfile 21,
  offset 1,927,831,552, size 20,202) is the localized checkpoint table for those keys.

Neither file contains a GFX blob, `JaugeBending`, `QDF_setText`, or any of the four HUD UUIDs.
They are **story checkpoints named after affection**, not the meter.

The only other `RELATION` references are the per-character `GD * Relation *` Lua drivers listed in §6.4,
which is exactly the code that feeds the gauge.

---

## 11. Sibling gauges — explicitly excluded

The same UI family produces other meters. These are **not** the affection/likeability meter and were
not included in the inventory above:

| GFX | Archive bundle | Type / id | Bigfile | Offset | Size |
|---|---|---|---:|---:|---:|---:|
| `GLOBAL_GAUGE_FOLLOWERS.gfx` | `extract/ui/278626_t18002_id132_f18.bin` | 18002 / 132 | 18 | 2,117,177,344 | 66,281 |
| `GLOBAL_GAUGE_SUSPICION.gfx` | `extract/ui/261069_t18002_id190_f17.bin` | 18002 / 190 | 17 | 1,931,026,432 | 124,706 |
| `GLOBAL_GAUGE_VICTORY.gfx` | `extract/ui/353505_t18002_id130_f22.bin` | 18002 / 130 | 22 | 1,416,347,648 | 89,021 |
| `GLOBAL_STRESS_GAUGE.gfx` | `extract/ui/187176_t18002_id110_f14.bin` | 18002 / 110 | 14 | 144,545,792 | 136,048 |
| `ALICE_STRESS_GAUGE.gfx` | `extract/ui/325875_t18002_id201_f21.bin` | 18002 / 201 | 21 | 691,212,288 | 139,031 |
| `ZLATKO_GAUGE_RESET.gfx` | `extract/ui/205606_t18002_id170_f15.bin` | 18002 / 170 | 15 | 273,025,024 | 48,491 |
| `ONTHERUN_GAUGE_RISK.gfx` | `extract/ui/195856_t18002_id164_f14.bin` | 18002 / 164 | 14 | 1,107,070,976 | 137,355 |
| `THENEST_GAUGE_DISTANCE.gfx` | `extract/ui/202731_t18002_id165_f14.bin` | 18002 / 165 | 14 | 1,773,142,016 | 152,001 |
| `WARHORROR_GAUGE_RISK.gfx` | `extract/ui/209750_t18002_id192_f15.bin` | 18002 / 192 | 15 | 843,509,760 | 134,100 |

`GLOBAL_GAUGE_SUSPICION` is the closest sibling in purpose (the suspicion meter), but it is a separate
GFX bundle with its own document class — it is **not** the affection meter.

---

## 12. Scene bindings

`JaugeBending` lives in the UI resource containers, not in the GFX bundle:

| Container | `JaugeBending - Alias` offset | `JaugeBending` offset | Deviant-label bindings |
|---|---:|---:|---|
| `extract/ui/143769_t4288_id7720_f11.bin` | `0x921B3` | `0x94A45` | 5 × `GUI_GAUGE_DEVIANT_NAME` / `_MACHINE` / `iConnorState` / `sAdjective` |
| `extract/ui/143995_t4288_id7725_f11.bin` | `0x25B6F` | `0x2DC54` | 1 × `GUI_GAUGE_DEVIANT_NAME` / `_MACHINE` / `iConnorState` / `sAdjective` |

Each container also carries the full `QDF_setText` + `GUI_GAUGE_*` binding block, confirming that the
scene instantiates the gauge as a normal UI object and drives it through the same four variables.

The `000305_t19000_id6826_f0.bin` container holds the `Jauge` / `UI Jauge` template definitions and all
four HUD UUIDs (QZIP-compressed; raw strings confirmed at the offsets reported in §2).
The `000314_t19000_id7085_f0.bin` container holds `GD Jauge Priority` / `GD Jauge Flowchart Setup`
and 60 `GUI_GAUGE` string literals.

---

## 13. Complete file list (copy-paste)

```
# Archive bundles (in extract/ui/)
143991_t18002_id193_f11.bin     main GFX bundle (GLOBAL_GAUGE_BENDING.gfx)
317102_t18002_id214_f20.bin     variant GFX bundle (GLOBAL_BENDING_NOTOTO.gfx)
143769_t4288_id7720_f11.bin     JaugeBending scene/resource container
143995_t4288_id7725_f11.bin     JaugeBending scene/resource container
000305_t19000_id6826_f0.bin     Jauge / UI Jauge templates + HUD UUIDs
000314_t19000_id7085_f0.bin     GD Jauge Priority + GD Jauge Flowchart Setup

# Localization
extract/type1016/001313_t1016_id2279_f0.bin     25-language GUI_GAUGE_* table
extract/gauge_localization_keys.tsv             68 English keys

# Carved binaries
extract/GLOBAL_GAUGE_BENDING.gfx
extract/GLOBAL_GAUGE_BENDING.abc
extract/GLOBAL_BENDING_NOTOTO.gfx
extract/GLOBAL_BENDING_NOTOTO.abc

# Textures
extract/Success_Arrow.dds
extract/Lock_part1.dds
extract/Lock_part2.dds

# ActionScript (main variant)
extract/abc_src/32_GLOBAL_GAUGE_BENDING.as
extract/abc_src/23_BendingState.as
extract/abc_src/24_AdjectiveInsert.as
extract/abc_src/27_FourTrianglesAccordion.as
extract/abc_src/29_GhostLetters.as
extract/abc_src/26_TriangleBG.as
extract/abc_src/28_Unlock.as
extract/abc_src/22_ArrowPlus.as
extract/abc_src/14_SoundEvent.as
extract/abc_src/01_Tween.as

# ActionScript (NOTOTO variant)
extract/abc_src_nototo/32_GLOBAL_BENDING_NOTOTO.as
extract/abc_src_nototo/23_BendingState.as
extract/abc_src_nototo/24_TriangleBG.as
extract/abc_src_nototo/25_AdjectiveInsert.as
extract/abc_src_nototo/27_Unlock.as
extract/abc_src_nototo/28_FourTrianglesAccordion.as
extract/abc_src_nototo/29_GhostLetters.as

# Lua — templates
extract/luac_gm/371478_t63_id635_f24_0x678E1_9e4624a2d820efabJauge.lua
extract/luac_gm/371478_t63_id635_f24_0x68A74_e489d39d21174b6eUI Jauge.lua

# Lua — drivers
extract/luac_gd/371482_t63_id859_f24_0xF6BAB_9494ac0ff3da2456GD Jauge Priority.lua
extract/luac_gd/371482_t63_id859_f24_0x1190EF_83cc2b3a63ab7f5aGD Jauge Flowchart Setup.lua
extract/luac_gd/371482_t63_id859_f24_0x2BE02_5fcc3acc339eb1ccGD Connor Relation Hank.lua
extract/luac_gd/371482_t63_id859_f24_0x25_850bb6c766de3e21GD Kara Relation Alice.lua
extract/luac_gd/371482_t63_id859_f24_0x1C2A5_bb70d41d36162c10GD Kara Relation Luther.lua
extract/luac_gd/371482_t63_id859_f24_0x91B7D_9298d65fc95272e6GD Markus Relation North.lua
extract/luac_gd/371482_t63_id859_f24_0xB2C66_2811180b9add72a6GD Markus Relation Josh.lua
extract/luac_gd/371482_t63_id859_f24_0xCC4DA_870fdb706687343cGD Markus Relation Simon.lua
extract/luac_gd/371482_t63_id859_f24_0x147332_4fa8b374f89af864GD Connor Relation Amanda.lua
```

---

## 14. Verification performed

- `parse_abc` on both ABC blobs: 33 instances, 33 classes, 401 methods (main).
- `decompile_class` on every gauge class: all 9 main + 7 variant classes produced without error.
- GFX tag walk: 17 tags, `DoABC` length 64,828, `SymbolClass` 6 entries.
- `SymbolClass` mapping printed directly from the tag.
- `DefineExternalImage` records printed: `Lock_part2.dds`, `Lock_part1.dds`, `Success_Arrow.dds`, all 128×128.
- zlib carve: three 16,512-byte DDS blobs, distinct SHA-256 hashes.
- `LOCALIZ_` parse: 25 languages, 163 entries each, 68 `GUI_GAUGE_*` keys each.
- Lua templates read in full; `Jauge.Show → UI Jauge.Show → QDF_setText/QDF_show → iConnorState=-1` confirmed.
- `GD Jauge Priority` grep: all 9 participant blocks set the same four UUIDs; Connor/Amanda branch sets
  `iConnorState` from `654a3595-3553-4285-923e-43ea667fff10`; all others set `-1`.
- `GD Connor Relation Hank` lines 2193–2211 and 2240–2257: explicit `iState = 2` and `iState = 3` calls.
- Whole-tree scan for `AFFECTION` / `Likeability` / `Sympathy`: only checkpoint/story identifiers, no GFX.

---

## 15. Known gaps

1. **Audio bank files** for the ten `UI_Sound_Gauge_*` events were not located. The UI-side event
   dispatch is fully mapped; the Wwise/FMOD bank that contains the actual waveforms is outside the
   extracted UI set.
2. `000305_t19000_id6826_f0.bin` and `000314_t19000_id7085_f0.bin` are QZIP containers that could not be
   fully decompressed with a standard zlib call. Their relevant template names, UUIDs and 60 `GUI_GAUGE`
   strings are confirmed by raw scan, which is sufficient to establish the file roles.

---

## 16. Interactive browser reconstruction

`indicator_demo/index.html` is a dependency-free canvas reconstruction of the notification, built from
the coordinate, alpha, frame and tween values in this report. It includes:

- the four-triangle name strip, the white adjective plate and the 64 × 96 masked arrow pentagon, all
  drawn from the decompiled geometry rather than bitmap stand-ins;
- the two extracted padlock textures, loaded as PNG (`indicator_demo/assets/`);
- all seven `BendingState` states, with pause, replay, 0.25×–1× speed and a 0–2.4 s timeline scrubber;
- a free gauge camera: wheel zoom at the cursor, drag pan, Fit / Reset, and camera deep links
  (`?zoom=4&cx=1850&cy=60`). The default framing centers the complete gauge group; Fit frames the
  group and Reset returns to that centered view. The main canvas deliberately shows the full group —
  including the 42 px top bleed and 22 px right bleed — while the map retains the original 1920 × 1080
  stage position;
- real localized name/adjective pairs from `extract/gauge_localization_keys.tsv`;
- the shipped positive blue `#80EEFF` and `QDColor.RED = 0xFFFF3000`;
- the sound event names as text (the audio bank is not available).

The page documents its unavoidable approximation in the footer and in `indicator_demo/README.md`:
the external `FONT_MASTER_ENG.swf` typeface. No animation curve or element-to-element position is
invented.
