/* Reconstruction of comData.GLOBAL_GAUGE_BENDING.
   All coordinates are in the game's 1920 x 1080 stage space.
   Source: decompiled GLOBAL_GAUGE_BENDING.abc and the extracted GFX. */

(() => {
  const STAGE_W = 1920;
  const STAGE_H = 1080;

  const C = {
    stage: '#808080',       // presentation override (source SetBackgroundColor: #363D46)
    white: '#ffffff',
    black: '#000000',
    blue: '#80eeff',        // shipped gauge blue (common UI-GFX blue fill)
    red: '#ff3000',         // QDColor.RED = 0xFFFF3000
  };

  const SAFE_Y = -42;
  const BG = { x: 1470, y: SAFE_Y, w: 346, h: 150, count: 4, tw: 173, alphas: [0.3, 0.5, 0.6, 0.3] };
  const NAME = { x: 1862, cx: 1772, cy: 26, size: 60, tracking: 10 };
  const ADJ = { x: 1862, originY: 56, textY: 30, plateX: 16, plateY: 14, h: 32, minW: 212, tracking: 10 };
  const BEND = { x: 1902, y: 34 };
  const PATH = { plateY: 74, unlockX: -16, unlockY: 90, scale: 0.35, presentShiftX: -40 };

  const STATE = {
    NONE: -1,
    INCREASE: 0,
    DECREASE: 1,
    UNLOCKED: 2,
    LOCKED: 3,
    INCREASEPLUS: 4,
    DECREASEPLUS: 5,
  };

  const STATE_LABEL = {
    '-1': 'NONE',
    '0': 'INCREASE',
    '1': 'DECREASE',
    '2': 'UNLOCKED',
    '3': 'LOCKED',
    '4': 'INCREASEPLUS',
    '5': 'DECREASEPLUS',
  };

  const SOUND = {
    '-1': '—',
    '0': 'UI_Sound_Gauge_Increase_Small',
    '1': 'UI_Sound_Gauge_Decrease_Small',
    '2': 'UI_Sound_Gauge_Unlock_Good',
    '3': 'UI_Sound_Gauge_Unlock_Bad',
    '4': 'UI_Sound_Gauge_Increase_Big',
    '5': 'UI_Sound_Gauge_Decrease_Big',
  };

  // Localized strings from extract/gauge_localization_keys.tsv
  const PRESETS = [
    { name: 'HANK', adjective: 'HOSTILE' },
    { name: 'HANK', adjective: 'TENSE' },
    { name: 'HANK', adjective: 'NEUTRAL' },
    { name: 'HANK', adjective: 'WARM' },
    { name: 'HANK', adjective: 'FRIEND' },
    { name: 'ALICE', adjective: 'NEUTRAL' },
    { name: 'LUTHER', adjective: 'NEUTRAL' },
    { name: 'NORTH', adjective: 'FRIEND' },
    { name: 'JOSH', adjective: 'FRIEND' },
    { name: 'SIMON', adjective: 'FRIEND' },
    { name: 'AMANDA', adjective: 'NEUTRAL' },
  ];

  const FONT_STACK = '"Bahnschrift", "Arial Narrow", "Segoe UI", sans-serif';
  const SOURCE_LAYOUT = new URLSearchParams(location.search).get('layout') === 'source';

  const clamp01 = (x) => Math.max(0, Math.min(1, x));
  const lerp = (a, b, x) => a + (b - a) * x;
  const easeOutQuint = (x) => 1 - Math.pow(1 - clamp01(x), 5);
  const easeInQuint = (x) => Math.pow(clamp01(x), 5);

  const measureCache = new Map();
  function measure(text, font, tracking) {
    const key = font + '|' + tracking + '|' + text;
    if (measureCache.has(key)) return measureCache.get(key);
    const c = measure.ctx;
    c.save();
    c.font = font;
    try { c.fontStretch = 'condensed'; } catch (e) { /* older canvas */ }
    let w = 0;
    for (const ch of text) w += c.measureText(ch).width + tracking;
    c.restore();
    w = Math.max(0, w - tracking);
    measureCache.set(key, w);
    return w;
  }
  measure.ctx = document.createElement('canvas').getContext('2d');

  function fontOf(size, weight) {
    return `${weight} ${size}px ${FONT_STACK}`;
  }

  function fitSize(text, base, maxW, min, weight, tracking) {
    let size = base;
    while (size > min) {
      if (measure(text, fontOf(size, weight), tracking) <= maxW) break;
      size -= 1;
    }
    return size;
  }

  const inkOffsetCache = new Map();
  function inkCenterOffset(text, font, tracking) {
    const key = 'ink|' + font + '|' + tracking + '|' + text;
    if (inkOffsetCache.has(key)) return inkOffsetCache.get(key);
    const c = measure.ctx;
    c.save();
    c.font = font;
    try { c.fontStretch = 'condensed'; } catch (e) { /* older canvas */ }
    let cursor = 0;
    let lo = Infinity;
    let hi = -Infinity;
    let hasMetrics = true;
    for (const ch of text) {
      const m = c.measureText(ch);
      if (m.actualBoundingBoxLeft == null || m.actualBoundingBoxRight == null) {
        hasMetrics = false;
        break;
      }
      lo = Math.min(lo, cursor - m.actualBoundingBoxLeft);
      hi = Math.max(hi, cursor + m.actualBoundingBoxRight);
      cursor += m.width + tracking;
    }
    c.restore();
    const total = Math.max(0, cursor - tracking);
    const off = hasMetrics ? (lo + hi) / 2 - total / 2 : 0;
    inkOffsetCache.set(key, off);
    return off;
  }

  function drawTracked(ctx, text, x, baseline, font, color, tracking, align, alpha) {
    if (alpha <= 0.001) return;
    ctx.save();
    ctx.font = font;
    try { ctx.fontStretch = 'condensed'; } catch (e) { /* older canvas */ }
    ctx.fillStyle = color;
    ctx.globalAlpha = alpha;
    ctx.textBaseline = 'alphabetic';
    const total = measure(text, font, tracking);
    let cursor;
    if (align === 'right') cursor = x - total;
    else if (align === 'center') cursor = x - total / 2 - inkCenterOffset(text, font, tracking);
    else cursor = x;
    for (const ch of text) {
      ctx.fillText(ch, cursor, baseline);
      cursor += ctx.measureText(ch).width + tracking;
    }
    ctx.restore();
  }

  function polygon(ctx, pts) {
    ctx.beginPath();
    ctx.moveTo(pts[0][0], pts[0][1]);
    for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i][0], pts[i][1]);
    ctx.closePath();
  }

  function tri(ctx, a, b, c, color, alpha) {
    if (alpha <= 0.001) return;
    ctx.save();
    ctx.globalAlpha = alpha;
    ctx.fillStyle = color;
    polygon(ctx, [a, b, c]);
    ctx.fill();
    ctx.restore();
  }

  /* ---- TriangleBG(346, 150, 4) ---- */
  function triAlpha(index, frame) {
    if (frame < index * 2) return 0;
    if (frame >= index * 2 + 6) return BG.alphas[index];
    return frame % 2 === 0 ? 1 : 0;
  }

  function drawNameStrip(ctx, frame, fade) {
    const TW = BG.tw;
    const TH = BG.h;
    ctx.save();
    ctx.translate(BG.x, BG.y);
    for (let i = 0; i < 4; i++) {
      const a = triAlpha(i, frame) * fade;
      if (i % 2 === 0) {
        const x0 = i * TW * 0.5;
        tri(ctx, [x0, 0], [x0 + TW * 0.5, TH], [x0 + TW, 0], C.black, a);
      } else {
        const x0 = (i + 1) * TW * 0.5;
        tri(ctx, [x0, 0], [x0 - TW * 0.5, TH], [x0 + TW * 0.5, TH], C.black, a);
      }
    }
    ctx.restore();
  }

  /* ---- QDText name ---- */
  function drawName(ctx, frame, fade, demo) {
    const a = (frame === 0 || frame === 2 || frame === 4 || frame >= 6) ? 1 : 0;
    if (a <= 0) return;
    const x = SOURCE_LAYOUT ? NAME.x : NAME.cx;
    const cy = NAME.cy;
    const tracking = SOURCE_LAYOUT ? 0 : NAME.tracking;
    drawTracked(ctx, demo.name, x, cy + NAME.size * 0.36,
      fontOf(NAME.size, 300), C.white, tracking, SOURCE_LAYOUT ? 'right' : 'center', a * fade);
  }

  /* ---- FourTrianglesAccordion + AdjectiveInsert ---- */
  function drawPlate(ctx, D, h, x, y, scaleX, color, bodyAlpha, alpha) {
    if (alpha <= 0.001) return;
    const TW = h * 1.155;
    ctx.save();
    ctx.translate(x, y);
    ctx.scale(scaleX, 1);
    ctx.globalAlpha = alpha;
    ctx.fillStyle = color;

    // first triangle group at x = -TW (alphas 0.3 / 0.4)
    ctx.save();
    ctx.translate(-TW, 0);
    ctx.globalAlpha = alpha * 0.3;
    polygon(ctx, [[0, 0], [TW, 0], [TW * 0.5, h]]);
    ctx.fill();
    ctx.globalAlpha = alpha * 0.4;
    polygon(ctx, [[-TW * 0.5, h], [TW * 0.5, h], [0, 0]]);
    ctx.fill();
    ctx.restore();

    // last triangle group at x = D - TW + TW/2 (alphas 0.5 / 0.4)
    const lx = D - TW + TW * 0.5;
    ctx.save();
    ctx.translate(lx, 0);
    ctx.globalAlpha = alpha * 0.5;
    polygon(ctx, [[0, h], [TW * 0.5, 0], [TW, h]]);
    ctx.fill();
    ctx.globalAlpha = alpha * 0.4;
    polygon(ctx, [[TW * 0.5, 0], [TW, h], [TW * 1.5, 0]]);
    ctx.fill();
    ctx.restore();

    // accordion body
    ctx.globalAlpha = alpha * bodyAlpha;
    polygon(ctx, [[0, 0], [D, 0], [D - TW * 0.5, h], [-TW * 0.5, h]]);
    ctx.fill();
    ctx.restore();
  }

  function drawAdjective(ctx, frame, fade, demo) {
    const local = frame - 4;
    if (local < 0) return;
    const textA = local === 0 || local === 2 ? 0 : 1;
    const plateA = local >= 3 ? 1 : 0;
    const adjFade = 1 - easeOutQuint(1 - fade);

    const text = demo.adjective;
    const tracking = SOURCE_LAYOUT ? 0 : ADJ.tracking;
    const size = fitSize(text, 24, 320, 10, 700, tracking);
    const font = fontOf(size, 700);
    const width = measure(text, font, tracking);
    const D = Math.max(ADJ.minW, Math.floor(width + 44));
    // QDText starts white after setConnorState(-1). AdjectiveInsert.frameShow
    // flips it white -> black on frames 4/5/6/7, settling on black at frame 7.
    const textColor = (local === 3 || local === 4 || local === 6) ? C.white : C.black;

    if (plateA) {
      drawPlate(ctx, D, ADJ.h, ADJ.x + ADJ.plateX, ADJ.originY + ADJ.plateY,
        -1, C.white, 1, plateA * adjFade);
    }
    if (textA) {
      // QDText y = 28 inside AdjectiveInsert; text is vertically centred there.
      const textX = SOURCE_LAYOUT ? ADJ.x : ADJ.x + ADJ.plateX - D / 2;
      const textY = SOURCE_LAYOUT ? 28 : ADJ.textY;
      drawTracked(ctx, text, textX, ADJ.originY + textY + size * 0.36,
        font, textColor, tracking, SOURCE_LAYOUT ? 'right' : 'center', textA * adjFade);
    }
  }

  /* ---- BendingState ---- */
  function chevron(ctx, cx, cy, flipY, color, alpha) {
    if (alpha <= 0.001) return;
    ctx.save();
    ctx.translate(cx, cy);
    ctx.scale(1, flipY);
    ctx.globalAlpha = alpha;
    ctx.strokeStyle = color;
    ctx.lineWidth = 3;
    ctx.lineCap = 'butt';
    ctx.lineJoin = 'miter';
    ctx.beginPath();
    ctx.moveTo(-32, 32);
    ctx.lineTo(0, 0);
    ctx.lineTo(32, 32);
    ctx.stroke();
    ctx.restore();
  }

  const ARROW_PTS = [[0, 32], [-32, 0], [-32, 64], [0, 96], [32, 64], [32, 0]];

  function arrowPath(ctx, cx, cy, sy) {
    ctx.beginPath();
    for (let i = 0; i < ARROW_PTS.length; i++) {
      const px = cx + ARROW_PTS[i][0];
      const py = cy + sy * ARROW_PTS[i][1];
      if (i === 0) ctx.moveTo(px, py);
      else ctx.lineTo(px, py);
    }
    ctx.closePath();
  }

  function drawBigArrow(ctx, t, state, fade) {
    const inc = state === STATE.INCREASEPLUS;
    // Presentation-only nudge: the big arrow sits a little higher.
    const BIG_ARROW_LIFT = SOURCE_LAYOUT ? 0 : -20;
    // BendingState.hide() kills the mask/chevron tweens at 1.9 s; freeze motion there.
    const tm = Math.min(t, 1.9);
    const p0 = clamp01(tm / 1);
    let maskY = inc
      ? lerp(120, 56, easeOutQuint(p0))
      : lerp(-136, -40, easeOutQuint(p0));

    if (tm >= 1) {
      const p1 = clamp01((tm - 1) / 1);
      maskY = inc
        ? lerp(56, -8, easeOutQuint(p1))
        : lerp(-40, 32, easeOutQuint(p1));
    }
    maskY += BIG_ARROW_LIFT;

    const arrowY = (inc ? 56 : -40) + BIG_ARROW_LIFT;
    const sy = inc ? -1 : 1;
    let arrowA;
    if (tm < 0.45) arrowA = 0.8;
    else if (tm < 0.95) arrowA = lerp(0.8, 0.2, (tm - 0.45) / 0.5);
    else if (tm < 1) arrowA = 0.2;
    else arrowA = 0.5;
    arrowA *= fade;

    if (arrowA > 0.001) {
      ctx.save();
      arrowPath(ctx, BEND.x + 8, BEND.y + maskY, sy);
      ctx.clip();
      ctx.globalAlpha = arrowA;
      ctx.fillStyle = inc ? C.blue : C.red;
      arrowPath(ctx, BEND.x + 8, BEND.y + arrowY, sy);
      ctx.fill();
      ctx.restore();
    }

    if (tm >= 1) {
      const p1 = clamp01((tm - 1) / 1);
      const y = inc ? lerp(24, -40, easeOutQuint(p1)) : lerp(-6, 64, easeOutQuint(p1));
      chevron(ctx, BEND.x + 8, BEND.y + y + BIG_ARROW_LIFT, inc ? 1 : -1,
        inc ? C.blue : C.red, fade);
    }
  }

  function drawSmallChevron(ctx, t, state, fade) {
    const inc = state === STATE.INCREASE;
    let y;
    let a = 1;
    if (t < 1) {
      const e = easeOutQuint(t);
      y = inc ? lerp(0, -40, e) : lerp(-28, 16, e);
    } else {
      y = inc ? -40 : 16;
      a = Math.max(0, 1 - (t - 1) / 0.3);
    }
    chevron(ctx, BEND.x + 8, BEND.y + y, inc ? 1 : -1,
      inc ? C.blue : C.red, a * fade);
  }

  /* ---- Unlock / PATH UNLOCKED ---- */
  const images = {
    part1: new Image(),
    part2: new Image(),
  };
  let imagesReady = false;
  let loaded = 0;
  for (const key of Object.keys(images)) {
    images[key].onload = () => {
      loaded++;
      if (loaded === 2) imagesReady = true;
    };
    images[key].src = `assets/Lock_${key === 'part1' ? 'part1' : 'part2'}.png`;
  }

  function drawPadlock(ctx, t, state, fade) {
    if (!imagesReady || t < 0.5) return;
    const tm = Math.min(t, 1.9);
    const alpha = t >= 1.9 ? 0.2 * fade : easeInQuint(clamp01((tm - 0.5) / 0.5));
    const slide = easeOutQuint(clamp01((tm - 1) / 1));
    const dx = 10 * slide;

    ctx.save();
    ctx.translate(BEND.x + PATH.unlockX + (SOURCE_LAYOUT ? 0 : PATH.presentShiftX),
      BEND.y + PATH.unlockY);
    ctx.scale(PATH.scale, PATH.scale);
    ctx.globalAlpha = alpha;
    ctx.drawImage(images.part1, -64 + dx, -64, 128, 128);
    ctx.drawImage(images.part2, -64 - dx, -64, 128, 128);
    ctx.restore();
  }

  function drawPathPrompt(ctx, t, state, fade) {
    if (state !== STATE.UNLOCKED && state !== STATE.LOCKED) return;
    if (t < 0.5) return;

    const localFrame = Math.floor((t - 0.5) * 30 + 1e-6);
    const plateA = (localFrame === 1 || localFrame === 3 || localFrame >= 5) ? 1 : 0;
    // frame 20 is the last ENTER_FRAME update; it sets alpha 1 and removes the listener,
    // so the caption stays on from frame 20 onward.
    const textA = (localFrame === 16 || localFrame === 18 || localFrame >= 20) ? 1 : 0;
    const color = state === STATE.UNLOCKED ? C.blue : C.red;

    const text = 'PATH UNLOCKED';
    const font = fontOf(18, 400);
    const width = measure(text, font, 0);
    const D = Math.floor(width + 58);

    if (plateA) {
      const shift = SOURCE_LAYOUT ? 0 : PATH.presentShiftX;
      drawPlate(ctx, D, 32, BEND.x - 4 + shift, BEND.y + PATH.plateY,
        -1, color, 1, plateA * fade);
    }

    drawPadlock(ctx, t, state, fade);

    if (textA) {
      // _labelText.x = -textWidth - 38 relative to BendingState at x = 1902
      const shift = SOURCE_LAYOUT ? 0 : PATH.presentShiftX;
      const textX = SOURCE_LAYOUT ? BEND.x - 38 : BEND.x - 4 + shift - D / 2;
      const textY = SOURCE_LAYOUT ? 14 : 16;
      drawTracked(ctx, text, textX, BEND.y + PATH.plateY + textY + 18 * 0.36,
        font, C.black, 0, SOURCE_LAYOUT ? 'right' : 'center', textA * fade);
    }
  }

  function drawGauge(ctx, t, demo) {
    if (t < 0) return;
    const frame = Math.floor(t * 30 + 1e-6);
    const hide = clamp01((t - 1.9) / 0.5);
    const fade = 1 - hide;

    drawNameStrip(ctx, frame, fade);
    drawName(ctx, frame, fade, demo);
    drawAdjective(ctx, frame, fade, demo);

    if (demo.state === STATE.INCREASE || demo.state === STATE.DECREASE) {
      drawSmallChevron(ctx, t, demo.state, fade);
    } else if (demo.state === STATE.INCREASEPLUS || demo.state === STATE.DECREASEPLUS) {
      drawBigArrow(ctx, t, demo.state, fade);
    } else if (demo.state === STATE.UNLOCKED || demo.state === STATE.LOCKED) {
      drawPathPrompt(ctx, t, demo.state, fade);
    }
  }

  /* ---- Viewer ---- */
  const stageCanvas = document.getElementById('stage');
  const mapCanvas = document.getElementById('map');
  const sctx = stageCanvas.getContext('2d');
  const mctx = mapCanvas.getContext('2d');
  const zoomLabel = document.getElementById('zoomLabel');

  const MAP_SCALE = Math.min(mapCanvas.width / STAGE_W, mapCanvas.height / STAGE_H) * 0.86;
  const MAP_OFF_X = (mapCanvas.width - STAGE_W * MAP_SCALE) / 2;
  const MAP_OFF_Y = (mapCanvas.height - STAGE_H * MAP_SCALE) / 2;
  const GAUGE_BOUNDS = { x: 1470, y: -42, w: 472, h: 188 };
  const GAUGE_CX = 1706;
  const GAUGE_CY = 52;
  const HOME = { cx: GAUGE_CX, cy: GAUGE_CY, cropW: 520, cropH: 240 };
  const MIN_ZOOM = 0.2;
  const MAX_ZOOM = 16;
  const PAN_MARGIN = 100;
  const camera = { zoom: 2, cx: HOME.cx, cy: HOME.cy };
  let cameraInitialized = false;

  function homeView(w, h) {
    return {
      zoom: Math.min(w / HOME.cropW, h / HOME.cropH),
      cx: HOME.cx,
      cy: HOME.cy,
    };
  }

  // Camera deep links, e.g. ?zoom=4&cx=1850&cy=50
  const camParams = new URLSearchParams(location.search);
  if (camParams.has('zoom')) camera.zoom = Number(camParams.get('zoom'));
  if (camParams.has('cx')) camera.cx = Number(camParams.get('cx'));
  if (camParams.has('cy')) camera.cy = Number(camParams.get('cy'));
  if (camParams.has('zoom') || camParams.has('cx') || camParams.has('cy')) {
    cameraInitialized = true;
  }
  camera.zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, camera.zoom));

  function clampAxis(value, half, size) {
    const lo = half - PAN_MARGIN;
    const hi = size - half + PAN_MARGIN;
    if (lo > hi) return size / 2;
    return Math.max(lo, Math.min(hi, value));
  }

  function clampCamera(w, h) {
    camera.cx = clampAxis(camera.cx, w / (2 * camera.zoom), STAGE_W);
    camera.cy = clampAxis(camera.cy, h / (2 * camera.zoom), STAGE_H);
  }

  function updateZoomLabel() {
    zoomLabel.textContent = camera.zoom.toFixed(2);
  }

  function renderStage(t, demo) {
    const rect = stageCanvas.getBoundingClientRect();
    const w = Math.max(1, rect.width);
    const h = Math.max(1, rect.height);
    const bs = stageCanvas.width / w;

    if (!cameraInitialized) {
      const home = homeView(w, h);
      camera.zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, home.zoom));
      camera.cx = home.cx;
      camera.cy = home.cy;
      cameraInitialized = true;
      updateZoomLabel();
    }

    clampCamera(w, h);

    sctx.setTransform(1, 0, 0, 1, 0, 0);
    sctx.clearRect(0, 0, stageCanvas.width, stageCanvas.height);
    sctx.fillStyle = C.stage;
    sctx.fillRect(0, 0, stageCanvas.width, stageCanvas.height);

    const a = bs * camera.zoom;
    const e = bs * (w / 2 - camera.cx * camera.zoom);
    const f = bs * (h / 2 - camera.cy * camera.zoom);

    sctx.setTransform(a, 0, 0, a, e, f);
    drawGauge(sctx, t, demo);
  }

  function renderMap(t, demo) {
    const rect = stageCanvas.getBoundingClientRect();
    const vw = Math.max(1, rect.width) / camera.zoom;
    const vh = Math.max(1, rect.height) / camera.zoom;
    const vx = camera.cx - vw / 2;
    const vy = camera.cy - vh / 2;

    mctx.setTransform(1, 0, 0, 1, 0, 0);
    mctx.clearRect(0, 0, mapCanvas.width, mapCanvas.height);
    mctx.fillStyle = '#0a0d11';
    mctx.fillRect(0, 0, mapCanvas.width, mapCanvas.height);

    mctx.save();
    mctx.setTransform(MAP_SCALE, 0, 0, MAP_SCALE, MAP_OFF_X, MAP_OFF_Y);
    mctx.fillStyle = C.stage;
    mctx.fillRect(0, 0, STAGE_W, STAGE_H);
    mctx.strokeStyle = 'rgba(0,0,0,0.18)';
    mctx.lineWidth = 2 / MAP_SCALE;
    mctx.strokeRect(0, 0, STAGE_W, STAGE_H);
    drawGauge(mctx, t, demo);

    mctx.strokeStyle = '#1b6f8a';
    mctx.lineWidth = 5 / MAP_SCALE;
    mctx.globalAlpha = 0.95;
    mctx.strokeRect(vx, vy, vw, vh);
    mctx.restore();
  }

  function setView(next) {
    camera.zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, next.zoom));
    camera.cx = next.cx;
    camera.cy = next.cy;
    cameraInitialized = true;
    const rect = stageCanvas.getBoundingClientRect();
    clampCamera(Math.max(1, rect.width), Math.max(1, rect.height));
    updateZoomLabel();
  }

  function resetView() {
    const rect = stageCanvas.getBoundingClientRect();
    setView(homeView(Math.max(1, rect.width), Math.max(1, rect.height)));
  }

  function fitView() {
    const rect = stageCanvas.getBoundingClientRect();
    const w = Math.max(1, rect.width);
    const h = Math.max(1, rect.height);
    setView({
      zoom: Math.max(MIN_ZOOM, Math.min(MAX_ZOOM,
        Math.min(w / GAUGE_BOUNDS.w, h / GAUGE_BOUNDS.h) * 0.94)),
      cx: GAUGE_CX,
      cy: GAUGE_CY,
    });
  }

  function zoomAt(factor, px, py) {
    cameraInitialized = true;
    const rect = stageCanvas.getBoundingClientRect();
    const w = Math.max(1, rect.width);
    const h = Math.max(1, rect.height);
    const sx = camera.cx + (px - w / 2) / camera.zoom;
    const sy = camera.cy + (py - h / 2) / camera.zoom;
    const zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, camera.zoom * factor));
    camera.zoom = zoom;
    camera.cx = sx - (px - w / 2) / zoom;
    camera.cy = sy - (py - h / 2) / zoom;
    clampCamera(w, h);
    updateZoomLabel();
  }

  stageCanvas.addEventListener('wheel', (event) => {
    event.preventDefault();
    const rect = stageCanvas.getBoundingClientRect();
    const px = event.clientX - rect.left;
    const py = event.clientY - rect.top;
    const unit = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? 100 : 1;
    const factor = Math.exp(-event.deltaY * unit * 0.0015);
    zoomAt(factor, px, py);
  }, { passive: false });

  let drag = null;

  stageCanvas.addEventListener('pointerdown', (event) => {
    if (event.button !== 0) return;
    drag = { id: event.pointerId, x: event.clientX, y: event.clientY };
    stageCanvas.focus({ preventScroll: true });
    stageCanvas.setPointerCapture(event.pointerId);
    stageCanvas.classList.add('dragging');
    event.preventDefault();
  });

  stageCanvas.addEventListener('pointermove', (event) => {
    if (!drag || event.pointerId !== drag.id) return;
    const rect = stageCanvas.getBoundingClientRect();
    camera.cx -= (event.clientX - drag.x) / camera.zoom;
    camera.cy -= (event.clientY - drag.y) / camera.zoom;
    drag.x = event.clientX;
    drag.y = event.clientY;
    clampCamera(Math.max(1, rect.width), Math.max(1, rect.height));
  });

  function endDrag(event) {
    if (!drag || event.pointerId !== drag.id) return;
    drag = null;
    stageCanvas.classList.remove('dragging');
    if (stageCanvas.hasPointerCapture(event.pointerId)) {
      stageCanvas.releasePointerCapture(event.pointerId);
    }
  }

  stageCanvas.addEventListener('pointerup', endDrag);
  stageCanvas.addEventListener('pointercancel', endDrag);

  stageCanvas.addEventListener('dblclick', resetView);

  stageCanvas.addEventListener('keydown', (event) => {
    const step = 42 / camera.zoom;
    if (event.key === 'ArrowLeft') camera.cx -= step;
    else if (event.key === 'ArrowRight') camera.cx += step;
    else if (event.key === 'ArrowUp') camera.cy -= step;
    else if (event.key === 'ArrowDown') camera.cy += step;
    else if (event.key === '+' || event.key === '=') {
      const rect = stageCanvas.getBoundingClientRect();
      zoomAt(1.25, rect.width / 2, rect.height / 2);
      event.preventDefault();
      return;
    } else if (event.key === '-' || event.key === '_') {
      const rect = stageCanvas.getBoundingClientRect();
      zoomAt(0.8, rect.width / 2, rect.height / 2);
      event.preventDefault();
      return;
    } else if (event.key === '0') {
      resetView();
      event.preventDefault();
      return;
    } else {
      return;
    }
    const rect = stageCanvas.getBoundingClientRect();
    clampCamera(Math.max(1, rect.width), Math.max(1, rect.height));
    event.preventDefault();
  });

  document.getElementById('resetView').addEventListener('click', resetView);
  document.getElementById('fitView').addEventListener('click', fitView);
  updateZoomLabel();

  /* ---- Controls ---- */
  const demo = window.__demo = {
    state: STATE.INCREASE,
    name: 'HANK',
    adjective: 'HOSTILE',
    t: 0,
    speed: 1,
    last: performance.now(),
    running: true,
  };

  // Deep-link / still-frame controls, e.g.
  // ?state=4&t=0.7  ?state=2&t=1.05&name=HANK&adj=FRIEND
  const params = new URLSearchParams(location.search);
  if (params.has('state')) demo.state = Number(params.get('state'));
  if (params.has('name')) demo.name = params.get('name');
  if (params.has('adj')) demo.adjective = params.get('adj');
  if (params.has('t')) {
    demo.t = Number(params.get('t'));
    demo.running = false;
  }

  const stateGrid = document.getElementById('stateGrid');
  const buttons = [...stateGrid.querySelectorAll('.state-btn')];
  const presetSelect = document.getElementById('textPreset');
  presetSelect.innerHTML = PRESETS.map((p, i) =>
    `<option value="${i}">${p.name} · ${p.adjective}</option>`).join('');

  const roState = document.getElementById('roState');
  const roSound = document.getElementById('roSound');
  const roTime = document.getElementById('roTime');
  const roPhase = document.getElementById('roPhase');
  const scrub = document.getElementById('scrub');
  const pauseBtn = document.getElementById('pause');

  function phaseOf(t, state) {
    if (t < 0.4) return 'reveal / strobe';
    if (state === STATE.UNLOCKED || state === STATE.LOCKED) {
      if (t < 1.0) return 'padlock fade-in';
      if (t < 1.9) return 'padlock slide';
      if (t < 2.4) return 'fade 500 ms';
      return 'gone';
    }
    if (state === STATE.INCREASEPLUS || state === STATE.DECREASEPLUS) {
      if (t < 1.0) return 'arrow wipe';
      if (t < 1.9) return 'chevron + arrow';
      if (t < 2.4) return 'fade 500 ms';
      return 'gone';
    }
    if (state === STATE.INCREASE || state === STATE.DECREASE) {
      if (t < 1.0) return 'chevron move';
      if (t < 1.3) return 'chevron fade';
      if (t < 1.9) return 'hold 1500 ms';
      if (t < 2.4) return 'fade 500 ms';
      return 'gone';
    }
    if (t < 1.9) return 'hold 1500 ms';
    if (t < 2.4) return 'fade 500 ms';
    return 'gone';
  }

  function refreshReadout() {
    roState.textContent = `${demo.state === -1 ? '−1' : demo.state} · ${STATE_LABEL[String(demo.state)]}`;
    roSound.textContent = SOUND[String(demo.state)];
    roTime.textContent = `${Math.min(demo.t, 2.4).toFixed(2)} s`;
    roPhase.textContent = phaseOf(demo.t, demo.state);
    if (document.activeElement !== scrub) {
      scrub.value = Math.min(demo.t, 2.4).toFixed(2);
    }
  }

  function setActive(state) {
    for (const b of buttons) b.classList.toggle('active', Number(b.dataset.state) === state);
  }

  function trigger(state) {
    demo.state = state;
    demo.t = 0;
    demo.last = performance.now();
    demo.running = true;
    pauseBtn.textContent = 'Pause';
    setActive(state);
    refreshReadout();
  }

  for (const b of buttons) {
    b.addEventListener('click', () => trigger(Number(b.dataset.state)));
  }

  document.getElementById('replay').addEventListener('click', () => trigger(demo.state));

  pauseBtn.addEventListener('click', () => {
    if (demo.running) {
      demo.running = false;
      pauseBtn.textContent = 'Play';
    } else {
      if (demo.t >= 2.4) demo.t = 0;
      demo.running = true;
      demo.last = performance.now();
      pauseBtn.textContent = 'Pause';
    }
  });

  scrub.addEventListener('input', () => {
    demo.t = Number(scrub.value);
    demo.running = false;
    pauseBtn.textContent = 'Play';
    refreshReadout();
  });

  presetSelect.addEventListener('change', () => {
    const p = PRESETS[Number(presetSelect.value)];
    demo.name = p.name;
    demo.adjective = p.adjective;
    trigger(demo.state);
  });

  document.getElementById('speed').addEventListener('change', (e) => {
    demo.speed = Number(e.target.value);
  });

  function loop(now) {
    const dt = Math.min(0.05, (now - demo.last) / 1000);
    demo.last = now;
    if (demo.running) demo.t += dt * demo.speed;
    if (demo.running && demo.t >= 2.4) {
      demo.t = 2.4;
      demo.running = false;
      pauseBtn.textContent = 'Play';
    }
    renderStage(demo.t, demo);
    renderMap(demo.t, demo);
    refreshReadout();
    requestAnimationFrame(loop);
  }

  setActive(demo.state);
  if (!demo.running) pauseBtn.textContent = 'Play';
  refreshReadout();
  requestAnimationFrame(loop);
})();
