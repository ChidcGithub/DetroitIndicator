# Relationship gauge — web reconstruction

A small, dependency-free browser demo of Detroit: Become Human's relationship
notification (`GLOBAL_GAUGE_BENDING`). It is driven by the decompiled gauge
ActionScript and the extracted GFX data, not by an animation approximation.

## Run it

Open `index.html` in a browser, or serve the folder:

```powershell
python -m http.server 8123
# http://127.0.0.1:8123/indicator_demo/index.html
```

The two padlock PNGs are loaded from `assets/`. No network access, fonts, or
audio are fetched.

## Controls

- **Neutral / Small increase / Small decrease / Big increase / Big decrease /
  Path unlocked / Path locked** starts that state at t = 0.
- **Replay** restarts the current state.
- **Pause / Play** freezes or resumes the 2.4 s timeline.
- **Timeline** scrubs from 0 to 2.4 s. Dragging pauses; Play resumes.
- **Speed** selects 1×, 0.5× or 0.25× playback. The elapsed time is the
  animation time, not wall-clock time.
- **Game text** chooses real pairs from `gauge_localization_keys.tsv`.
- **Stage camera:** mouse wheel zooms at the cursor, drag pans, double-click
  resets to the centered gauge framing. **Fit** frames the full gauge group and
  **Reset** returns to the default centered framing. With the canvas focused,
  arrow keys pan, `+` / `-` zoom, and `0` resets.
  The main canvas shows the complete gauge group, including the 42 px top bleed
  and the 22 px right bleed; the map shows the original 1920 × 1080 stage with
  those bleeds around it.

Deep links freeze a frame, useful for comparing against the decompiled code:

```
?state=4&t=0.30
?state=2&t=1.10&name=HANK&adj=FRIEND
?state=-1&t=0.50&adj=TENSE
```

Camera deep links use stage units and CSS pixels per stage pixel:

```
?state=4&t=1.20&zoom=4&cx=1850&cy=60
?state=4&t=1.20&zoom=0.38&cx=960&cy=540
?layout=source
```

`layout=source` switches the text to the raw QDText anchors (right edges 1862 / 1862 / 1864,
centres 26 / 84 / 122), zero fallback tracking, and the raw big-arrow height. See
`POSITION_AUDIT.md` for the one-by-one source comparison.

`state` is a `BendingState` constant, `t` is seconds, and `name` / `adj` are the
two localized strings.

## What comes from the game

| Item | Source |
|---|---|
| Stage size, 30 fps, background `#363D46` | `GLOBAL_GAUGE_BENDING.gfx` header + `SetBackgroundColor` tag |
| Name strip `TriangleBG(346, 150, 4)` | `26_TriangleBG.as` — four 173 × 150 triangles, alphas `[0.3, 0.5, 0.6, 0.3]` |
| Name at `x = 1862`, 60 px thin, `y = 26` | `32_GLOBAL_GAUGE_BENDING.as` constructor + `QDText` call; the trailing `10` is `zShift`, not tracking |
| Adjective plate `FourTrianglesAccordion(212, 32)` | `24_AdjectiveInsert.as`, `27_FourTrianglesAccordion.as` |
| State chevrons, 64 × 96 arrow, masks | `23_BendingState.as` + the GFX `DefineShape` pentagon |
| Padlock slide / fade | `28_Unlock.as` |
| `PATH UNLOCKED` caption | `$GUI_GAUGE_UNLOCK` in the localization table |
| `Lock_part1`, `Lock_part2` pixels | DDS zlib streams after the GFX, decoded to PNG |

The background is pure runtime vector geometry. The GFX has only three DDS
images — `Lock_part1`, `Lock_part2` and `Success_Arrow` — and the decompiled
relationship gauge never references `Success_Arrow`. It is included in
`assets/` for provenance but not drawn.

## Exact timings encoded

| Moment | Code | Demo |
|---|---|---|
| Name strobe | frames 0, 2, 4, ≥ 6 on; 1, 3, 5 off | 30 fps |
| Triangle chase | each triangle starts at `2i`, locks at `2i + 6` | 30 fps |
| Adjective starts | frame `_frameCountForAdj = 4` | 0.133 s |
| Adjective flicker | `frameShow(0..8)`; plate at local frame 3 | 0.133–0.400 s |
| Hold | `clik(this).to(1500, {})` | 1.500 s |
| Hide starts | `_frameCountForAdj + FLASHENDFRAME` → 1.9 s | 1.900 s |
| Fade | name / plate / state 500 ms | 2.400 s gone |
| Small chevron | y 0 → −40 (increase) / −28 → +16 (decrease), 1000 ms `Quint.easeOut`; alpha 0 in 300 ms | 0–1.0 s, fade 1.0–1.3 s |
| Big arrow | mask 120 → 56 (increase) / −136 → −40 (decrease), 1000 ms; alpha 0.8 → 0.2, 500 ms after 450 ms | 0–1.0 s |
| Big chevron | 24 → −40 (increase) / −6 → 64 (decrease), 1000 ms, then 500 ms fade | 1.0–2.0 s |
| Padlock | appears after 500 ms; alpha 0 → 1 over 500 ms `Strong.easeIn`; halves to x = ±10 over 1000 ms `Strong.easeOut` | 0.5–2.0 s |
| Path plate | frames 1, 3, ≥ 5 on | 30 fps |
| Path text | on at frames 16, 18, 20; frame 20 stops the loop with alpha 1 | 0.53–0.67 s, then on |

At 1.9 s `GLOBAL_GAUGE_BENDING._hide()` calls `BendingState.hide()`, which kills the
ongoing mask/chevron/padlock tweens. Their positions freeze and their alpha fades over 500 ms.
The big arrow's 2.0 s phase-2 hide and the padlock's 2.0 s end-of-slide tint are therefore
not reached by the normal auto-hide gauge.

## Positional anchors (source → demo)

All values are game-stage pixels. The demo keeps the sprite-relative geometry exact; only the camera
position is a presentation offset.

| Element | Source expression | Demo |
|---|---|---:|
| `_container` | `y = safeY = -42` | `-42` |
| Name strip | `TriangleBG(346,150,4).x = 1920 - 450` | `(1470, -42)` |
| Name label | `x = 1920 - safeX - 100 = 1862`; `y = bg.y + 68` | text centre x `1772`, y `26` (source right edge `1862`) |
| Adjective insert | `x = 1862`; `y = name.y + getTextHeight() × 0.5` | `(1862, 56)` using the 60 px font box |
| Adjective plate | `_bg.x = 16`, `_bg.y = 14`, `scaleX = -1` | origin `(1878, 70)` |
| Adjective text | `x = -textWidth`, `y = 28`, `ALIGN_CENTER` | text centre x `1772`, optical y `86` (source right edge `1862`, y `84`) |
| Bending state | `x = name.x + 40 = 1902`; `y = name.y + 8` | `(1902, 34)` |
| Small/big chevron | `_jacques.x = 8`; y from the state tween | centre x `1910`, y `34 + y` |
| Big arrow / mask | `_yves.x = 8`, `_maskedYves.x = 8`; y `56` / `120` increase, `-40` / `-136` decrease | centre x `1910`; demo applies a presentation-only `-20` px lift to the arrow, mask and big-state chevron |
| PATH plate | `x = -4`, `y = 74`, `scaleX = -1` | origin `(1858, 108)` default (source `1898`; presentation shift −40) |
| PATH text | `x = -textWidth - 38`, `y = labelBG.y + 14` | text centre x `1772`, optical y `124` (source right edge `1864`, y `122`) |
| Padlock | `x = -16`, `y = labelBG.y + 16`, `scale = 0.35` | centre `(1846, 124)` default (source `1886`, same −40 shift) |

The default text is centre-aligned as a column on `x = 1772` (HANK, adjective and PATH share the
same ink centreline). The source anchors are right-pivoted (`1862 / 1862 / 1864`);
`?layout=source` restores those right edges, zero fallback tracking, the raw PATH plate/padlock
positions, raw vertical anchors (`26 / 84 / 122`) and zero arrow lift. The name keeps the source
vertical anchor `26`; the adjective/PATH vertical anchors are optically centred
(`86 / 124` vs source `84 / 122`). The PATH plate and padlock are shifted −40 px in the default view
so the PATH text can share the same centreline; source mode restores them.
The `-20` px big-arrow lift is a presentation-only adjustment requested for the demo; it applies to
both the big pentagon and the big-state chevron, so their relative motion is unchanged. Set
`BIG_ARROW_LIFT = 0` in `gauge.js` to restore the raw source positions. The small increase/decrease
chevrons are untouched.

## Deliberate limitations

- **Font.** The gauge imports `fonts/FONT_MASTER_ENG.swf`; the font outlines are
  external to the extracted bundle. The demo uses Bahnschrift (falling back to
  Arial Narrow) at the decoded sizes with 10 px fallback tracking. The strings themselves are
  the extracted localized strings.
- **Colors.** Positive/UNLOCKED is the shipped UI blue `#80EEFF` (the common
  blue fill in the extracted UI GFX). The shared `QDColor.BLUE` literal is
  `0xFFFCA2FF` (`#FCA2FF`, a light magenta) and does not match the shipped
  gauge, so it is not used. `QDColor.RED = 0xFFFF3000` (`#FF3000`) matches the
  shipped red and is used. `QDMCTools.colorChange` sets
  `ColorTransform.color`, which sets the three colour multipliers to 0 and the
  offsets to the colour; the demo applies the same solid tint to plates,
  arrows and the padlock.
- **Padlock tint.** The padlock starts white from the extracted texture. The
  normal gauge auto-hides at 1.9 s; `BendingState.hide` kills the padlock slide
  before its 2.0 s `onComplete`, so `Unlock.endUnlockPadlock` and its
  `QDColor.BLUE` tint never fire. At 1.9 s the padlock jumps to alpha 0.2 and
  fades with the rest of the prompt, which is what the demo reproduces.
- **Sound.** The gauge dispatches named Scaleform events
  (`UI_Sound_Gauge_Increase_Small`, etc.); the audio bank was not located, so
  the demo shows the event name instead of playing a substitute sound.
- **Deviant/Connor branch.** `DEVIANT_INFECTED` changes the adjective color and
  adds ghost letters in `AdjectiveInsert`. This demo covers the normal
  relationship path only.
- **Clipping.** The UI is laid out for a 1920 × 1080 stage with `safeX = −42`;
  the arrow at x = 1910 and the chevron's right arm cross x = 1920. They are
  clipped by the stage exactly as the code places them.
- **Locked/unlocked caption.** Both `LOCKED` (3) and `UNLOCKED` (2) use the same
  `UNLOCKPATH = "$GUI_GAUGE_UNLOCK"` caption and `LOCKSTATE_UNLOCK` animation in
  the decompiled class; only the plate tint and sound event differ. The demo
  reproduces that quirk rather than inventing a separate "LOCKED" wording.

## Assets

The PNGs are the byte-identical conversions made earlier from the trailing DDS
zlib streams in `extract/GLOBAL_GAUGE_BENDING.gfx`:

```
A28834E8BC9BC11A946F33BCE59CF8B731676A8D04337BC7E4DC1C510C4E369D  Lock_part1.png
736DAA8FF9AD4945A507032F962C1401F1094F614B1E2DD1B1E5BE4A6B5CCEF0  Lock_part2.png
4D826D56B91C4B6CF1D12ED53F17C91503BDF658FB5A57D14260682F18FAF768  Success_Arrow.png
```

Source bundle: `extract/ui/143991_t18002_id193_f11.bin` (type 18002, id 193,
bigfile 11), GFX at `0x172`, length `0xFFB7`; DDS streams at `0x10146`,
`0x10360` and `0x1049F`.

## Files

```
index.html      page + controls
styles.css      page layout
gauge.js        canvas renderer and timeline
assets/         two padlock PNGs + unused success chevron
```
