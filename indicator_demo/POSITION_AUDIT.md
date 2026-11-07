# Position audit — source vs demo

All coordinates are in the 1920 × 1080 game-stage space. The gauge container is offset by
`safeY = -42`, so a source `y = 68` inside the container is screen `y = 26`.

Open `index.html?layout=source` to switch the demo to the raw QDText anchors, raw arrow
height and zero fallback tracking. The default view keeps the requested presentation
adjustments (optical vertical anchors for the plate/PATH text, 10 px fallback tracking,
−20 px big-arrow lift). The name uses the raw source anchor.

## Element anchors

| Element | Source expression | Source (stage px) | Demo default | `layout=source` |
|---|---|---|---:|---|---|
| Container | `_container.y = safeY` | `y = -42` | same | same |
| Name strip origin | `_bg.x = 1920 - 450` | `(1470, -42)` | same | same |
| Name strip size | `TriangleBG(346, 150, 4)` | 4 × 173 × 150, bbox 432.5 × 150 | same | same |
| Name strip alphas | `[0.3, 0.5, 0.6, 0.3]` | — | same | same |
| Name label | `x = 1920 - safeX - 100`; `y = bg.y + 68` | right edge 1862, centre y 26 | common centre x 1772, y 26 | right edge 1862, y 26 |
| Name tracking | QDText `lSpacing = 0` | 0 | 10 px fallback | 0 |
| AdjectiveInsert | `x = 1862`; `y = name.y + getTextHeight() × 0.5` | `(1862, 56)` at 60 px text box | same | same |
| Adjective plate | `_bg.x = 16`, `_bg.y = 14`, `scaleX = -1` | origin `(1878, 70)` | same | same |
| Adjective plate width | `max(212, int(textWidth + 44))` | 212 min | same formula | same formula |
| Plate serrations | first group 2 + last group 2 = 4 triangles; alphas `0.3/0.4` and `0.5/0.4`; no stroke | — | same | same |
| Adjective text | `x = -textWidth`; `y = 28`; `ALIGN_H_LEFT` + `ALIGN_CENTER` | right edge 1862, centre y 84 | common centre x 1772, y 86 | right edge 1862, y 84 |
| Adjective tracking | QDText `lSpacing = 0` | 0 | 10 px fallback | 0 |
| BendingState | `x = name.x + 40`; `y = name.y + 8` | `(1902, 34)` | same | same |
| Small chevron | path `(-32,32)→(0,0)→(32,32)`; `_jacques.x = 8` | centre x 1910; increase y 34→−6; decrease y 6→50 | same | same |
| Big arrow | `_yves.x = 8`, `_yves.y = 56 / −40` | increase `(1910, 90)`, decrease `(1910, −6)` | y −20 px from source | source y |
| Big mask | `_maskedYves.y = 120 / −136` | increase `(1910, 154)`, decrease `(1910, −102)` | −20 px from source | source y |
| Big chevron | `_jacques.y = 24 / −6` then to `−40 / 64` | increase 58→−6, decrease 28→98 | −20 px from source | source y |
| PATH plate | `_labelBG.x = −4`, `y = 74`, `scaleX = −1` | origin `(1898, 108)` | origin `(1858, 108)` (−40 px presentation shift) | same |
| PATH plate width | `textWidth + 58` | font-dependent | same formula | same formula |
| PATH text | `x = −textWidth − 38`, `y = 14` | right edge 1864, centre y 122 | common centre x 1772, y 124 | right edge 1864, y 122 |
| Padlock | `_unlock.x = −16`, `y = labelBG.y + 16`, scale 0.35 | centre `(1886, 124)` | centre `(1846, 124)` (−40 px shift) | same |
| Padlock texture offset | `addBDToClip(..., -64, -64)` | centred 128 px bitmap | same | same |
| Child order | bg → name → adjective → tendency; yves → mask → jacques → labelBG → unlock → labelText | — | same | same |

## Widths used by the demo

- Name strip: fixed source geometry, no font dependency.
- Adjective plate: `max(212, floor(measuredTextWidth + 44))`.
- PATH plate: `floor(measuredTextWidth + 58)`.
- Text widths use the fallback font; the source font is external, so absolute glyph widths and the
  resulting plate widths remain font-dependent.

## Timing anchors

- Name strobe: frames 0/2/4/≥6 on; 1/3/5 off.
- Triangle chase: triangle `i` starts at frame `2i`, locks at `2i+6`.
- Adjective starts at frame 4; plate at frame 7; flicker ends at frame 12.
- 1500 ms hold starts at 0.4 s; `_hide` at 1.9 s; 500 ms fade.
- Big arrow: 1 s mask/alpha phase; 1 s chevron phase; all tweens killed at 1.9 s.
- Unlock: 500 ms block delay; 500 ms padlock alpha (`Strong.easeIn`); 1 s slide
  (`Strong.easeOut`); padlock jumps to alpha 0.2 and fades at 1.9 s.
- PATH plate frames 1/3/≥5; PATH text frames 16/18/20, then stays on.

The source anchors in the table above are the ground truth. Any default-view difference is one of the
requested presentation adjustments, not an unverified coordinate.
