// Omni Genesis 2D — Grundbausteine: Mathematik, Rauschen, Eingabe, Speicher.
'use strict';
const OG = window.OG = {};

OG.clamp = (v, a, b) => (v < a ? a : v > b ? b : v);
OG.clamp01 = v => (v < 0 ? 0 : v > 1 ? 1 : v);
OG.lerp = (a, b, t) => a + (b - a) * t;
OG.smooth = t => t * t * (3 - 2 * t);
OG.dist = (ax, ay, bx, by) => Math.hypot(ax - bx, ay - by);
OG.rand = (a, b) => a + Math.random() * (b - a);
OG.randInt = (a, b) => Math.floor(OG.rand(a, b + 1));
OG.pick = arr => arr[Math.floor(Math.random() * arr.length)];
OG.angleDiff = (a, b) => {
  let d = (b - a) % (Math.PI * 2);
  if (d > Math.PI) d -= Math.PI * 2;
  if (d < -Math.PI) d += Math.PI * 2;
  return d;
};
OG.fmt = (v, d = 0) => Number(v).toLocaleString('de-DE', { minimumFractionDigits: d, maximumFractionDigits: d });

// Deterministischer Zufall (gleiche Welt bei jedem Start).
OG.rng = seed => {
  let s = seed >>> 0;
  return () => {
    s = (s + 0x6d2b79f5) >>> 0;
    let t = s;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
};

// Glattes Wertrauschen (2D) und fraktale Summe.
(() => {
  const perm = new Uint8Array(512);
  const vals = new Float32Array(256);
  const r = OG.rng(1337);
  for (let i = 0; i < 256; i++) { perm[i] = i; vals[i] = r(); }
  for (let i = 255; i > 0; i--) { const j = Math.floor(r() * (i + 1)); const t = perm[i]; perm[i] = perm[j]; perm[j] = t; }
  for (let i = 0; i < 256; i++) perm[i + 256] = perm[i];
  const h = (x, y) => vals[perm[(perm[x & 255] + y) & 511]];
  OG.noise = (x, y) => {
    const xi = Math.floor(x), yi = Math.floor(y);
    const xf = x - xi, yf = y - yi;
    const u = OG.smooth(xf), v = OG.smooth(yf);
    const a = h(xi, yi), b = h(xi + 1, yi), c = h(xi, yi + 1), d = h(xi + 1, yi + 1);
    return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
  };
  OG.fbm = (x, y) => {
    let s = 0, a = 0.5, f = 1;
    for (let i = 0; i < 3; i++) { s += a * OG.noise(x * f + 31.7, y * f + 17.3); a *= 0.5; f *= 2.03; }
    return s / 0.875;
  };
})();

// ---------- Eingabe: Tastatur, Maus, Touch ----------
OG.Input = {
  held: new Set(),
  pressed: new Set(),
  mouseX: 0, mouseY: 0,
  mouseDown: false,
  wheel: 0,
  touch: false,
  stickX: 0, stickY: 0,
  touchAttack: false,
  init(canvas) {
    const typing = () => {
      const a = document.activeElement;
      return a && (a.tagName === 'INPUT' || a.tagName === 'TEXTAREA');
    };
    window.addEventListener('keydown', e => {
      if (typing()) return;
      if (!this.held.has(e.code)) this.pressed.add(e.code);
      this.held.add(e.code);
      if (['Space', 'Tab', 'F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'ArrowUp', 'ArrowDown'].includes(e.code)) e.preventDefault();
    });
    window.addEventListener('keyup', e => this.held.delete(e.code));
    window.addEventListener('blur', () => { this.held.clear(); this.mouseDown = false; });
    canvas.addEventListener('pointermove', e => { this.mouseX = e.clientX; this.mouseY = e.clientY; });
    canvas.addEventListener('pointerdown', e => {
      if (e.pointerType === 'touch') return;
      this.mouseX = e.clientX; this.mouseY = e.clientY;
      if (e.button === 0) this.mouseDown = true;
    });
    window.addEventListener('pointerup', e => { if (e.button === 0) this.mouseDown = false; });
    canvas.addEventListener('wheel', e => { this.wheel += Math.sign(e.deltaY); e.preventDefault(); }, { passive: false });
    canvas.addEventListener('contextmenu', e => e.preventDefault());
  },
  down(code) { return this.held.has(code); },
  hit(code) { return this.pressed.has(code); },
  endFrame() { this.pressed.clear(); this.wheel = 0; },
};

// ---------- Speicher (nur im eigenen Browser) ----------
OG.Store = {
  key: 'omnigenesis2d_spielstand',
  load() {
    try { const s = localStorage.getItem(this.key); return s ? JSON.parse(s) : null; } catch (e) { return null; }
  },
  save(data) {
    try { localStorage.setItem(this.key, JSON.stringify(data)); return true; } catch (e) { return false; }
  },
  clear() { try { localStorage.removeItem(this.key); } catch (e) { /* ignorieren */ } },
  exists() { try { return !!localStorage.getItem(this.key); } catch (e) { return false; } },
};

OG.esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
