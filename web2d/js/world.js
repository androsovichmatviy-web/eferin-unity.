// Omni Genesis 2D — Kontinent: Gelände, Gebäude, Hindernisse und statische Objekte.
// Koordinaten in Metern, y zeigt nach Süden (Norden ist oben).
'use strict';

OG.L = {
  half: 200,
  city: { x: 0, y: 120 }, cityR: 44,
  village: { x: -100, y: -40 }, villageR: 70,
  hills: { x: 120, y: 20 },
  rift: { x: 0, y: -160 }, forbiddenY: -110,
  guild: { x: 68, y: 120 }, arena: { x: -72, y: 120 },
  forestEdge: { x: -138, y: -76 }, camp: { x: 142, y: -26 },
  roads: [
    [[0, 76], [0, 0], [0, -104]],
    [[0, 0], [-50, -22], [-100, -40]],
    [[0, 0], [55, 8], [112, 18]],
    [[44, 120], [60, 120]],
    [[-44, 120], [-58, 120]],
  ],
};

OG.Places = {};

OG.roadDist = (x, y) => {
  let best = 1e9;
  for (const road of OG.L.roads) {
    for (let i = 0; i < road.length - 1; i++) {
      const ax = road[i][0], ay = road[i][1], bx = road[i + 1][0], by = road[i + 1][1];
      const abx = bx - ax, aby = by - ay;
      const t = OG.clamp01(((x - ax) * abx + (y - ay) * aby) / (abx * abx + aby * aby));
      const d = Math.hypot(ax + abx * t - x, ay + aby * t - y);
      if (d < best) best = d;
    }
  }
  return best;
};

OG.region = (x, y) => {
  const L = OG.L;
  if (y < L.forbiddenY) return 'Verbotene Lande';
  if (OG.dist(x, y, L.city.x, L.city.y) < L.cityR + 6 || OG.dist(x, y, L.guild.x, L.guild.y) < 22 || OG.dist(x, y, L.arena.x, L.arena.y) < 22) return 'Kronendomäne · Grad Genesis';
  if (OG.dist(x, y, L.village.x, L.village.y) < L.villageR) return 'Provinz Grünmark · Eichenhain';
  if (OG.dist(x, y, L.hills.x, L.hills.y) < 70) return 'Provinz Grünmark · Erzhöhen';
  return 'Provinz Grünmark';
};
OG.inVillage = (x, y) => OG.dist(x, y, OG.L.village.x, OG.L.village.y) < OG.L.villageR;

// ---------- Höhe und Landschaft ----------
OG.Land = {
  hillMask(x, y) { const t = OG.clamp01(1 - OG.dist(x, y, OG.L.hills.x, OG.L.hills.y) / 75); return t * t; },
  height(x, y) {
    const L = OG.L;
    let h = (OG.fbm(x * 0.011, y * 0.011) - 0.5) * 12;
    h += this.hillMask(x, y) * 24 * (0.7 + 0.6 * OG.fbm(x * 0.03 + 50, y * 0.03));
    const edge = Math.max(Math.abs(x), Math.abs(y));
    const e = OG.clamp01((edge - 160) / 40); h += e * e * 50;
    const r = OG.clamp01(1 - OG.dist(x, y, L.rift.x, L.rift.y) / 32); h -= r * r * 10;
    const flat = (cx, cy, inner, outer, target) => {
      const d = OG.dist(x, y, cx, cy);
      const t = OG.smooth(OG.clamp01((d - inner) / (outer - inner)));
      h = OG.lerp(target, h, t);
    };
    flat(L.city.x, L.city.y, L.cityR + 6, L.cityR + 26, 2);
    flat(L.guild.x, L.guild.y, 18, 32, 2);
    flat(L.arena.x, L.arena.y, 19, 32, 2);
    flat(L.village.x, L.village.y, 26, 42, 1.5);
    return h;
  },
  forest(x, y) {
    const L = OG.L;
    if (OG.dist(x, y, L.city.x, L.city.y) < L.cityR + 14) return 0;
    if (OG.dist(x, y, L.guild.x, L.guild.y) < 24 || OG.dist(x, y, L.arena.x, L.arena.y) < 24) return 0;
    if (OG.dist(x, y, L.village.x, L.village.y) < 30) return 0;
    return OG.fbm(x * 0.022 + 300, y * 0.022 + 120);
  },
};

OG.freeLand = (x, y) => {
  const L = OG.L;
  if (OG.dist(x, y, L.city.x, L.city.y) < L.cityR + 10) return false;
  if (OG.dist(x, y, L.guild.x, L.guild.y) < 22 || OG.dist(x, y, L.arena.x, L.arena.y) < 22) return false;
  if (OG.dist(x, y, L.village.x, L.village.y) < 27) return false;
  if (OG.dist(x, y, L.camp.x, L.camp.y) < 16) return false;
  if (Math.abs(x) > 172 || Math.abs(y) > 172) return false;
  if (y < L.forbiddenY + 2) return false;
  return OG.roadDist(x, y) >= 3.5;
};

// ---------- Welt: Kollision, Gebäude, Objekte ----------
OG.World = {
  cell: 8,
  grid: new Map(),
  stamp: 0,
  colliders: [],
  buildings: [],
  low: [],      // flache Objekte unter den Figuren
  high: [],     // Dächer, Baumkronen, Banner über den Figuren
  lights: [],
  ground: null, groundRes: 3,
  fields: null,

  reset() {
    this.grid.clear(); this.colliders = []; this.buildings = []; this.low = []; this.high = []; this.lights = [];
    OG.Places = {};
  },

  insert(c) {
    this.colliders.push(c);
    const x0 = Math.floor(c.bx0 / this.cell), x1 = Math.floor(c.bx1 / this.cell);
    const y0 = Math.floor(c.by0 / this.cell), y1 = Math.floor(c.by1 / this.cell);
    for (let gx = x0; gx <= x1; gx++) for (let gy = y0; gy <= y1; gy++) {
      const k = gx * 10007 + gy;
      let arr = this.grid.get(k);
      if (!arr) { arr = []; this.grid.set(k, arr); }
      arr.push(c);
    }
    return c;
  },
  circle(x, y, r, tag) { return this.insert({ t: 'c', x, y, r, on: true, tag, bx0: x - r, bx1: x + r, by0: y - r, by1: y + r, s: 0 }); },
  rect(x0, y0, x1, y1, tag) { return this.insert({ t: 'r', x0, y0, x1, y1, on: true, tag, bx0: x0, bx1: x1, by0: y0, by1: y1, s: 0 }); },

  query(x, y, r) {
    this.stamp++;
    const out = [];
    const x0 = Math.floor((x - r) / this.cell), x1 = Math.floor((x + r) / this.cell);
    const y0 = Math.floor((y - r) / this.cell), y1 = Math.floor((y + r) / this.cell);
    for (let gx = x0; gx <= x1; gx++) for (let gy = y0; gy <= y1; gy++) {
      const arr = this.grid.get(gx * 10007 + gy);
      if (!arr) continue;
      for (const c of arr) if (c.s !== this.stamp) { c.s = this.stamp; out.push(c); }
    }
    return out;
  },

  // Schiebt einen Kreis aus allen Hindernissen heraus.
  resolve(e) {
    for (let it = 0; it < 2; it++) {
      for (const c of this.query(e.x, e.y, e.r + 0.5)) {
        if (!c.on) continue;
        if (c.t === 'c') {
          const dx = e.x - c.x, dy = e.y - c.y, d = Math.hypot(dx, dy), min = e.r + c.r;
          if (d < min) {
            if (d > 1e-4) { const p = (min - d) / d; e.x += dx * p; e.y += dy * p; } else e.x += min;
          }
        } else {
          const px = OG.clamp(e.x, c.x0, c.x1), py = OG.clamp(e.y, c.y0, c.y1);
          const dx = e.x - px, dy = e.y - py, d2 = dx * dx + dy * dy;
          if (d2 < e.r * e.r) {
            if (d2 > 1e-8) { const d = Math.sqrt(d2), p = (e.r - d) / d; e.x += dx * p; e.y += dy * p; }
            else {
              const l = e.x - c.x0, r = c.x1 - e.x, t = e.y - c.y0, b = c.y1 - e.y, m = Math.min(l, r, t, b);
              if (m === l) e.x = c.x0 - e.r; else if (m === r) e.x = c.x1 + e.r; else if (m === t) e.y = c.y0 - e.r; else e.y = c.y1 + e.r;
            }
          }
        }
      }
    }
  },

  blocked(x, y, r) {
    for (const c of this.query(x, y, r + 0.5)) {
      if (!c.on) continue;
      if (c.t === 'c') { if (Math.hypot(x - c.x, y - c.y) < r + c.r) return true; }
      else { const px = OG.clamp(x, c.x0, c.x1), py = OG.clamp(y, c.y0, c.y1); if (Math.hypot(x - px, y - py) < r) return true; }
    }
    return false;
  },

  // Begehbare Halle: Wände mit Türöffnung, Dach verschwindet, sobald man drinnen steht.
  hall(name, cx, cy, w, h, door, doorW, roof, floor, extra) {
    const x0 = cx - w / 2, x1 = cx + w / 2, y0 = cy - h / 2, y1 = cy + h / 2, t = 0.4;
    const wall = (ax0, ay0, ax1, ay1) => this.rect(ax0, ay0, ax1, ay1, 'wand');
    const gapX0 = cx - doorW / 2, gapX1 = cx + doorW / 2, gapY0 = cy - doorW / 2, gapY1 = cy + doorW / 2;
    if (door === 'N') { wall(x0, y0, gapX0, y0 + t); wall(gapX1, y0, x1, y0 + t); } else wall(x0, y0, x1, y0 + t);
    if (door === 'S') { wall(x0, y1 - t, gapX0, y1); wall(gapX1, y1 - t, x1, y1); } else wall(x0, y1 - t, x1, y1);
    if (door === 'W') { wall(x0, y0, x0 + t, gapY0); wall(x0, gapY1, x0 + t, y1); } else wall(x0, y0, x0 + t, y1);
    if (door === 'E') { wall(x1 - t, y0, x1, gapY0); wall(x1 - t, gapY1, x1, y1); } else wall(x1 - t, y0, x1, y1);
    const b = Object.assign({ name, x0, y0, x1, y1, door, doorW, roof, floor, enterable: true, furniture: [] }, extra || {});
    this.buildings.push(b);
    this.lights.push({ x: cx, y: cy, r: Math.max(w, h) * 0.75, inside: b, warm: true });
    return b;
  },

  house(cx, cy, w, h, roof, wall) {
    this.rect(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2, 'haus');
    const b = { name: 'Haus', x0: cx - w / 2, y0: cy - h / 2, x1: cx + w / 2, y1: cy + h / 2, roof, wall, enterable: false };
    this.buildings.push(b);
    return b;
  },

  add(layer, obj) { (layer === 'high' ? this.high : this.low).push(obj); return obj; },
};

// ---------- Bodentextur ----------
OG.buildFields = () => {
  const N = 401, H = new Float32Array(N * N), F = new Float32Array(N * N), Rd = new Float32Array(N * N);
  for (let j = 0; j < N; j++) for (let i = 0; i < N; i++) {
    const x = i - 200, y = j - 200, k = j * N + i;
    H[k] = OG.Land.height(x, y);
    F[k] = OG.Land.forest(x, y);
    Rd[k] = OG.roadDist(x, y);
  }
  OG.World.fields = { N, H, F, Rd };
};

OG.sampleField = (arr, x, y) => {
  const N = 401;
  const fx = OG.clamp(x + 200, 0, 399.999), fy = OG.clamp(y + 200, 0, 399.999);
  const i = Math.floor(fx), j = Math.floor(fy), u = fx - i, v = fy - j, k = j * N + i;
  const a = arr[k], b = arr[k + 1], c = arr[k + N], d = arr[k + N + 1];
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
};
OG.heightAt = (x, y) => OG.sampleField(OG.World.fields.H, x, y);

OG.groundColor = (x, y, out) => {
  const f = OG.World.fields, L = OG.L;
  const h = OG.sampleField(f.H, x, y);
  const n = OG.noise(x * 0.35 + 7, y * 0.35 + 3) * 0.6 + OG.noise(x * 1.7, y * 1.7) * 0.4;
  let r = 0.34, g = 0.52, b = 0.22;
  const k = 0.85 + 0.3 * n; r *= k; g *= k; b *= k;
  const mix = (cr, cg, cb, t) => { r += (cr - r) * t; g += (cg - g) * t; b += (cb - b) * t; };

  const forest = OG.sampleField(f.F, x, y);
  if (forest > 0.5) mix(0.2, 0.34, 0.15, OG.clamp01((forest - 0.5) * 6));
  const hill = OG.Land.hillMask(x, y) * OG.clamp01((h - 5) / 10);
  mix(0.46 * (0.9 + 0.2 * n), 0.43 * (0.9 + 0.2 * n), 0.38 * (0.9 + 0.2 * n), hill);
  const edge = Math.max(Math.abs(x), Math.abs(y));
  if (edge > 162) mix(0.5, 0.5, 0.5, OG.clamp01((h - 10) / 10));
  if (h > 30) mix(0.92, 0.94, 0.97, OG.clamp01((h - 30) / 6));
  if (y < -98) {
    mix(0.3 * (0.85 + 0.3 * n), 0.27 * (0.85 + 0.3 * n), 0.33 * (0.85 + 0.3 * n), OG.clamp01((-98 - y) / 14));
    const dr = OG.dist(x, y, L.rift.x, L.rift.y);
    if (dr < 34) {
      const vein = Math.abs(OG.noise(x * 0.25, y * 0.25) - 0.5);
      if (vein < 0.05) mix(0.55, 0.2, 0.75, 1 - dr / 34);
    }
  }
  const road = OG.sampleField(f.Rd, x, y);
  if (road < 2.8) mix(0.52 * (0.9 + 0.2 * n), 0.43 * (0.9 + 0.2 * n), 0.3 * (0.9 + 0.2 * n), 1 - OG.clamp01((road - 2) / 0.8));
  const dv = OG.dist(x, y, L.village.x, L.village.y);
  if (dv < 25) mix(0.48 * (0.9 + 0.2 * n), 0.4 * (0.9 + 0.2 * n), 0.28 * (0.9 + 0.2 * n), 1 - OG.clamp01((dv - 22) / 3));
  const dc = OG.dist(x, y, L.city.x, L.city.y);
  if (dc < L.cityR + 1 || OG.dist(x, y, L.guild.x, L.guild.y) < 17 || OG.dist(x, y, L.arena.x, L.arena.y) < 18) {
    const tile = ((Math.floor(x * 1.5) + Math.floor(y * 1.5)) & 1) === 0;
    const t = (tile ? 1 : 0.93) * (0.95 + 0.1 * n);
    r = 0.58 * t; g = 0.56 * t; b = 0.52 * t;
  }
  // Schummerung: Licht von Nordwesten
  const dx = OG.sampleField(f.H, x + 1, y) - OG.sampleField(f.H, x - 1, y);
  const dy = OG.sampleField(f.H, x, y + 1) - OG.sampleField(f.H, x, y - 1);
  const shade = OG.clamp(1 - (dx + dy) * 0.09, 0.62, 1.3);
  out[0] = Math.min(255, r * shade * 255);
  out[1] = Math.min(255, g * shade * 255);
  out[2] = Math.min(255, b * shade * 255);
};

// Erzeugt die Bodentextur schrittweise (für den Ladebildschirm).
OG.buildGround = function* () {
  const res = OG.World.groundRes, size = 400 * res;
  const canvas = document.createElement('canvas');
  canvas.width = size; canvas.height = size;
  const ctx = canvas.getContext('2d');
  const img = ctx.createImageData(size, size);
  const px = img.data, c = [0, 0, 0];
  for (let j = 0; j < size; j++) {
    const y = -200 + (j + 0.5) / res;
    for (let i = 0; i < size; i++) {
      OG.groundColor(-200 + (i + 0.5) / res, y, c);
      const k = (j * size + i) * 4;
      px[k] = c[0]; px[k + 1] = c[1]; px[k + 2] = c[2]; px[k + 3] = 255;
    }
    if (j % 60 === 0) yield j / size;
  }
  ctx.putImageData(img, 0, 0);
  OG.World.ground = canvas;
  // kleine Karte für das Kartenfenster
  const mini = document.createElement('canvas');
  mini.width = 400; mini.height = 400;
  mini.getContext('2d').drawImage(canvas, 0, 0, 400, 400);
  OG.World.mini = mini;
};

// ---------- Weltbau ----------
OG.buildWorld = () => {
  const W = OG.World, P = OG.Places, L = OG.L;
  const stone = '#b8b3a8', dark = '#726f6a', plaster = '#dcd1b8', roofRed = '#8c3829', roofBlue = '#1f3366', wood = '#735030', gold = '#f2c14d';

  // Ränder der Welt
  W.rect(-260, -260, 260, -196, 'rand'); W.rect(-260, 196, 260, 260, 'rand');
  W.rect(-260, -260, -196, 260, 'rand'); W.rect(196, -260, 260, 260, 'rand');

  // ---------- Grad Genesis ----------
  const C = L.city;
  W.add('low', { type: 'paving', x: C.x, y: C.y, r: L.cityR + 1 });
  W.add('low', { type: 'paving', x: L.guild.x, y: L.guild.y, r: 17 });
  W.add('low', { type: 'paving', x: L.arena.x, y: L.arena.y, r: 18 });
  const gates = [-Math.PI / 2, 0, Math.PI];
  W.add('high', { type: 'citywall', x: C.x, y: C.y, r: L.cityR, gates });
  for (let a = 0; a < Math.PI * 2; a += 0.035) {
    if (gates.some(g => Math.abs(OG.angleDiff(a, g)) < 0.12)) continue;
    W.circle(C.x + Math.cos(a) * L.cityR, C.y + Math.sin(a) * L.cityR, 0.9, 'mauer');
  }
  for (const g of gates) for (const s of [-1, 1]) {
    const a = g + s * 0.13, x = C.x + Math.cos(a) * L.cityR, y = C.y + Math.sin(a) * L.cityR;
    W.circle(x, y, 1.7, 'turm');
    W.add('high', { type: 'tower', x, y, r: 1.7 });
    W.add('high', { type: 'banner', x: x + Math.cos(g) * 2.5, y: y + Math.sin(g) * 2.5 });
  }

  P.plaza = { x: 0, y: 112 };
  const stalls = [[-7, 108, '#bf3333'], [7, 108, '#3373bf'], [-7, 118, '#d9b333'], [7, 118, '#4d994d']];
  for (const [sx, sy, col] of stalls) {
    W.rect(sx - 0.6, sy - 1.5, sx + 0.6, sy + 1.5, 'stand');
    W.add('low', { type: 'stall', x: sx, y: sy });
    W.add('high', { type: 'awning', x: sx, y: sy, color: col });
  }
  P.merchantA = { x: -4.6, y: 108 }; P.merchantB = { x: 4.6, y: 108 };
  W.circle(0, 113, 1.2, 'brunnen');
  W.add('low', { type: 'well', x: 0, y: 113, r: 1.2 });
  W.lights.push({ x: -7, y: 113, r: 7, warm: true }, { x: 7, y: 113, r: 7, warm: true });

  P.cityGate = { x: 0, y: 90 };
  OG.buildGate('Grad Genesis', 0, 90, 'h');

  // Thronsaal
  const pal = W.hall('Thronsaal', 0, 146, 26, 18, 'N', 4, roofBlue, '#4a4f66', { palace: true });
  pal.furniture.push({ type: 'carpet', x0: -1.5, y0: 137.5, x1: 1.5, y1: 151 });
  pal.furniture.push({ type: 'dais', x0: -3, y0: 150.5, x1: 3, y1: 154.6 });
  pal.furniture.push({ type: 'throne', x: 0, y: 153.2 });
  W.rect(-1, 152.6, 1, 153.8, 'thron');
  for (const i of [-2, -1, 1, 2]) {
    W.circle(i * 4.5, 142, 0.45, 'säule'); W.circle(i * 4.5, 148, 0.45, 'säule');
    pal.furniture.push({ type: 'column', x: i * 4.5, y: 142 }, { type: 'column', x: i * 4.5, y: 148 });
    if (Math.abs(i) === 2) { W.circle(i * 4.5, 135.5, 0.5, 'säule'); W.add('low', { type: 'column', x: i * 4.5, y: 135.5 }); }
  }
  W.add('high', { type: 'banner', x: -6, y: 135.6 }); W.add('high', { type: 'banner', x: 6, y: 135.6 });
  P.emperor = { x: 0, y: 151.6 }; P.emperorFace = -Math.PI / 2;

  // Kanzlei und Gericht
  const k = W.hall('Kanzlei und Gericht', -26, 126, 10, 12, 'E', 2.4, roofBlue, '#665240');
  k.furniture.push({ type: 'desk', x0: -28.1, y0: 124.2, x1: -27.3, y1: 127.8 });
  W.rect(-28.1, 124.2, -27.3, 127.8, 'pult');
  P.judge = { x: -29.2, y: 126 }; P.judgeFace = 0;
  P.court = { x: -18.5, y: 126 };
  W.add('high', { type: 'banner', x: -20.4, y: 122.6 });

  // Mietshaus (Art. 116)
  const m = W.hall('Mietshaus', -26, 104, 8, 10, 'E', 2, roofRed, '#735438');
  m.furniture.push({ type: 'bed', x0: -29.3, y0: 100.2, x1: -27.1, y1: 101.6 });
  m.furniture.push({ type: 'chest', x0: -29.3, y0: 106.6, x1: -28.3, y1: 107.8 });
  W.rect(-29.3, 100.2, -27.1, 101.6, 'bett'); W.rect(-29.3, 106.6, -28.3, 107.8, 'truhe');
  P.bed = { x: -28.2, y: 100.9 }; P.chest = { x: -28.8, y: 107.2 };
  P.landlord = { x: -19.8, y: 101.8 };

  // Schmiede (offener Pavillon)
  W.add('low', { type: 'forge', x: 22.2, y: 102.2 });
  W.rect(21.2, 101.5, 23.2, 102.9, 'esse');
  W.circle(25, 104.5, 0.6, 'amboss');
  W.add('low', { type: 'anvil', x: 25, y: 104.5 });
  for (const [px, py] of [[21, 101.4], [27, 101.4], [21, 106.6], [27, 106.6]]) W.circle(px, py, 0.2, 'pfosten');
  W.add('high', { type: 'pavilion', x0: 20.6, y0: 101, x1: 27.4, y1: 107, color: dark });
  W.lights.push({ x: 22.2, y: 102.2, r: 6, warm: true, fire: true });
  P.anvil = { x: 25, y: 104.5 }; P.smith = { x: 25.9, y: 102.6 };

  // Kaiserliches Lazarett (Art. 45)
  const laz = W.hall('Kaiserliches Lazarett', 26, 128, 9, 12, 'W', 2.4, roofRed, '#c9c9c4');
  for (const by of [123.5, 128, 132.5]) { laz.furniture.push({ type: 'bed', x0: 27.8, y0: by - 0.7, x1: 30, y1: by + 0.7 }); W.rect(27.8, by - 0.7, 30, by + 0.7, 'bett'); }
  P.healer = { x: 23.6, y: 126.4 }; P.lazarett = { x: 18.5, y: 128 };

  // Wohnhäuser
  for (const deg of [15, 345, 50, 75, 105, 130, 165, 205, 225, 315]) {
    const a = -deg * Math.PI / 180, x = C.x + Math.cos(a) * 37, y = C.y + Math.sin(a) * 37;
    W.house(x, y, 6, 5.5, deg % 2 ? roofBlue : roofRed, plaster);
  }

  P.cityPatrol = []; P.cityWalk = [];
  for (let i = 0; i < 10; i++) { const a = i * Math.PI / 5; P.cityPatrol.push({ x: C.x + Math.cos(a) * 19, y: C.y - 4 + Math.sin(a) * 15 }); }
  for (let i = 0; i < 12; i++) { const a = i * Math.PI / 6; P.cityWalk.push({ x: C.x + Math.cos(a) * 11, y: C.y - 8 + Math.sin(a) * 9 }); }

  // ---------- Gildenhaus (Art. 121) ----------
  const G = L.guild;
  const gh = W.hall('Gildenhaus der Abenteurer', G.x, G.y, 11, 14, 'W', 2.6, '#40593d', '#6b4d33');
  gh.furniture.push({ type: 'desk', x0: 69.5, y0: 117.5, x1: 71, y1: 122.5 });
  W.rect(69.5, 117.5, 71, 122.5, 'theke');
  for (const ty of [115.5, 124.5]) { gh.furniture.push({ type: 'table', x: 65.5, y: ty }); W.circle(65.5, ty, 0.8, 'tisch'); }
  W.add('low', { type: 'board', x: 60.4, y: 116.4 });
  W.rect(59.2, 116.2, 61.6, 116.6, 'brett');
  W.add('high', { type: 'banner', x: 60.6, y: 124.2 });
  P.guildMaster = { x: 60.2, y: 121.2 }; P.guildHall = { x: G.x, y: G.y };

  // ---------- Arena der Prüfung ----------
  const Ar = L.arena;
  P.arena = { x: Ar.x, y: Ar.y };
  W.add('low', { type: 'arena', x: Ar.x, y: Ar.y, r: 15 });
  P.arenaGate = [];
  for (let a = 0; a < Math.PI * 2; a += 0.05) {
    const x = Ar.x + Math.cos(a) * 15, y = Ar.y + Math.sin(a) * 15;
    const c = W.circle(x, y, 0.8, 'arena');
    if (Math.abs(OG.angleDiff(a, 0)) < 0.13) { c.on = false; P.arenaGate.push(c); }
  }
  P.trialMaster = { x: Ar.x + 17.5, y: Ar.y - 3 };
  W.add('high', { type: 'banner', x: Ar.x + 16.5, y: Ar.y + 3.5 });

  // ---------- Eichenhain ----------
  const V = L.village;
  P.village = { x: V.x, y: V.y };
  P.villageHomes = []; P.villageWalk = [];
  for (const deg of [25, 70, 115, 160, 205, 250, 295]) {
    const a = -deg * Math.PI / 180, x = V.x + Math.cos(a) * 15, y = V.y + Math.sin(a) * 15;
    W.house(x, y, 5.5, 5, '#664d33', '#c7b38c');
    const dx = (V.x - x) / 15, dy = (V.y - y) / 15;
    P.villageHomes.push({ x: x + dx * 3.6, y: y + dy * 3.6 }, { x: x + dx * 3.6 - dy * 1.5, y: y + dy * 3.6 + dx * 1.5 });
  }
  for (let i = 0; i < 10; i++) { const a = i * Math.PI / 5; P.villageWalk.push({ x: V.x + Math.cos(a) * 8, y: V.y + Math.sin(a) * 8 }); }
  W.circle(V.x + 6, V.y + 6, 1, 'brunnen'); W.add('low', { type: 'well', x: V.x + 6, y: V.y + 6, r: 1 });
  P.townHallSite = { x: V.x, y: V.y };
  W.add('low', { type: 'site', x: V.x, y: V.y });
  P.villageTrader = { x: V.x - 7, y: V.y + 5 };
  W.rect(V.x - 9, V.y + 4.2, V.x - 8, V.y + 5.8, 'stand'); W.add('high', { type: 'awning', x: V.x - 8.5, y: V.y + 5, color: '#997333' });
  P.mercenary = { x: V.x + 7, y: V.y - 4 };
  P.villageGate = { x: V.x + 14, y: V.y - 22 };
  OG.buildGate('Eichenhain', P.villageGate.x, P.villageGate.y, 'h');
  for (const [lx, ly] of [[-8, -8], [8, 8], [-8, 8], [8, -8]]) W.lights.push({ x: V.x + lx, y: V.y + ly, r: 6, warm: true });

  // ---------- Grenze und Riss ----------
  for (let x = -170; x <= 170; x += 24) {
    if (Math.abs(x) < 8) continue;
    W.circle(x, L.forbiddenY, 0.7, 'grenzstein');
    W.add('high', { type: 'marker', x, y: L.forbiddenY });
    W.lights.push({ x, y: L.forbiddenY, r: 4, violet: true });
  }
  W.circle(-4, -108, 1.6, 'pfeiler'); W.circle(4, -108, 1.6, 'pfeiler');
  W.add('high', { type: 'borderpost', x: 0, y: -108 });
  P.borderL = { x: -2.2, y: -106.5 }; P.borderR = { x: 2.2, y: -106.5 };
  P.borderGate = { x: 16, y: -94 };
  OG.buildGate('Grenzposten', 16, -94, 'h');
  P.rift = { x: L.rift.x, y: L.rift.y };
  const rr = OG.rng(77);
  for (let i = 0; i < 14; i++) {
    const a = rr() * Math.PI * 2, d = rr() * 9, x = L.rift.x + Math.cos(a) * d, y = L.rift.y + Math.sin(a) * d;
    const s = 0.6 + rr() * 0.8;
    W.circle(x, y, s * 0.7, 'kristall');
    W.add('high', { type: 'crystal', x, y, s, rot: rr() * Math.PI });
  }
  W.lights.push({ x: L.rift.x, y: L.rift.y, r: 30, violet: true, always: true });
  P.seal = { x: 0, y: -136 };
  W.circle(0, -136, 0.9, 'siegel');
  P.craterRim = { x: 0, y: -128 };

  // ---------- Orte der Geschichte ----------
  P.forestEdge = { x: L.forestEdge.x, y: L.forestEdge.y };
  P.camp = { x: L.camp.x, y: L.camp.y };
  for (let i = 0; i < 5; i++) {
    const a = i * Math.PI * 2 / 5, x = L.camp.x + Math.cos(a) * 9, y = L.camp.y + Math.sin(a) * 9;
    W.rect(x - 1.5, y - 1.2, x + 1.5, y + 1.2, 'zelt');
    W.add('high', { type: 'tent', x, y });
  }
  W.add('low', { type: 'campfire', x: L.camp.x, y: L.camp.y, violet: true });
  W.add('high', { type: 'banner', x: L.camp.x + 3, y: L.camp.y + 3, color: '#4d1420' });
  W.lights.push({ x: L.camp.x, y: L.camp.y, r: 12, violet: true, fire: true });
};

OG.buildGate = (name, x, y) => {
  OG.World.circle(x - 3, y, 0.7, 'tor'); OG.World.circle(x + 3, y, 0.7, 'tor');
  OG.World.add('high', { type: 'gate', x, y, name });
  OG.World.lights.push({ x, y, r: 6, cyan: true });
};

// ---------- Wälder und Ressourcen ----------
OG.plantWorld = () => {
  const W = OG.World, rng = OG.rng(4242);
  for (let x = -185; x <= 185; x += 7) {
    for (let y = -185; y <= 185; y += 7) {
      const px = x + (rng() - 0.5) * 5, py = y + (rng() - 0.5) * 5;
      const roll = rng(), scale = 0.8 + rng() * 0.5, variant = Math.floor(rng() * 6);
      const mask = OG.Land.forest(px, py), h = OG.heightAt(px, py);
      if (mask <= 0 || h > 18 || OG.roadDist(px, py) < 5) continue;
      const forbidden = py < OG.L.forbiddenY + 4;
      let chance = mask > 0.56 ? 0.8 : 0.035;
      if (OG.Land.hillMask(px, py) > 0.3) chance *= 0.3;
      if (forbidden) chance *= 0.35;
      if (roll > chance) continue;
      OG.Nodes.add('Baum', px, py, { scale, variant, dead: forbidden });
    }
  }
  const disc = (cx, cy, r) => { const a = rng() * Math.PI * 2, d = Math.sqrt(rng()) * r; return [cx + Math.cos(a) * d, cy + Math.sin(a) * d]; };
  for (let i = 0; i < 32; i++) { const [x, y] = disc(OG.L.hills.x, OG.L.hills.y, 58); if (OG.freeLand(x, y)) OG.Nodes.add('Erz', x, y); }
  for (let i = 0; i < 40; i++) { const x = (rng() - 0.5) * 330, y = (rng() - 0.5) * 200 + 10; if (OG.freeLand(x, y)) OG.Nodes.add('Fels', x, y); }
  for (let i = 0; i < 80; i++) { const x = (rng() - 0.5) * 330, y = (rng() - 0.5) * 210 + 10; if (OG.freeLand(x, y)) OG.Nodes.add('Kraut', x, y); }
  for (let i = 0; i < 10; i++) { const x = (rng() - 0.5) * 220, y = OG.L.forbiddenY - 10 - rng() * 50; OG.Nodes.add('Aether', x, y); }
  // Gräser und Blumen als reine Zierde
  for (let i = 0; i < 2600; i++) {
    const x = (rng() - 0.5) * 370, y = (rng() - 0.5) * 370;
    if (!OG.freeLand(x, y) && y > OG.L.forbiddenY) continue;
    const forest = OG.Land.forest(x, y);
    W.add('low', { type: 'tuft', x, y, v: Math.floor(rng() * 4), dead: y < OG.L.forbiddenY, flower: forest < 0.45 && rng() < 0.25 });
  }
};
