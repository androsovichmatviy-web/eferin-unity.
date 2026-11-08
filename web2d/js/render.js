// Omni Genesis 2D — Darstellung von oben: Boden, Gebäude, Figuren, Licht und Wetter.
'use strict';

OG.cam = { x: 0, y: 100, zoom: 32, shake: 0 };
OG.shake = a => { OG.cam.shake = Math.max(OG.cam.shake, a); };
OG.screenToWorld = (sx, sy) => ({ x: OG.cam.x + (sx - OG.R.W / 2) / OG.cam.zoom, y: OG.cam.y + (sy - OG.R.H / 2) / OG.cam.zoom });

OG.R = {
  W: 0, H: 0, dpr: 1, ctx: null, canvas: null, dark: null, dctx: null, grain: null, rain: [],
  init(canvas) {
    this.canvas = canvas; this.ctx = canvas.getContext('2d');
    this.dark = document.createElement('canvas'); this.dctx = this.dark.getContext('2d');
    const g = document.createElement('canvas'); g.width = g.height = 64;
    const gc = g.getContext('2d'), img = gc.createImageData(64, 64);
    for (let i = 0; i < 64 * 64; i++) { const v = 110 + Math.random() * 40; img.data[i * 4] = img.data[i * 4 + 1] = img.data[i * 4 + 2] = v; img.data[i * 4 + 3] = 255; }
    gc.putImageData(img, 0, 0);
    this.grain = this.ctx.createPattern(g, 'repeat');
    // Pflaster der Kronendomäne: versetzte Steine, 16 px pro Meter
    const pv = document.createElement('canvas'); pv.width = 48; pv.height = 48;
    const pc = pv.getContext('2d'), rr = OG.rng(9);
    for (let row = 0; row < 4; row++) for (let col = -1; col < 3; col++) {
      const x = col * 24 + (row % 2) * 12, y = row * 12, v = 0.9 + rr() * 0.2;
      pc.fillStyle = 'rgba(' + Math.round(150 * v) + ',' + Math.round(144 * v) + ',' + Math.round(134 * v) + ',1)';
      pc.fillRect(x + 1, y + 1, 22, 10);
    }
    this.paving = this.ctx.createPattern(pv, 'repeat');
    this.resize();
    window.addEventListener('resize', () => this.resize());
  },
  resize() {
    this.dpr = Math.min(2, window.devicePixelRatio || 1);
    this.W = window.innerWidth; this.H = window.innerHeight;
    for (const c of [this.canvas, this.dark]) { c.width = Math.round(this.W * this.dpr); c.height = Math.round(this.H * this.dpr); }
    this.canvas.style.width = this.W + 'px'; this.canvas.style.height = this.H + 'px';
  },
  worldTransform() {
    const c = OG.cam, z = c.zoom * this.dpr;
    const sx = c.shake > 0 ? (Math.random() - 0.5) * c.shake * 0.4 : 0, sy = c.shake > 0 ? (Math.random() - 0.5) * c.shake * 0.4 : 0;
    this.ctx.setTransform(z, 0, 0, z, this.dpr * this.W / 2 - (c.x + sx) * z, this.dpr * this.H / 2 - (c.y + sy) * z);
  },
  view() { const c = OG.cam, hw = this.W / 2 / c.zoom + 4, hh = this.H / 2 / c.zoom + 4; return { x0: c.x - hw, x1: c.x + hw, y0: c.y - hh, y1: c.y + hh }; },
  inView(v, x, y, m) { return x > v.x0 - m && x < v.x1 + m && y > v.y0 - m && y < v.y1 + m; },

  frame() {
    const ctx = this.ctx, P = OG.player, v = this.view();
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.fillStyle = '#121a10'; ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
    this.worldTransform();
    ctx.imageSmoothingEnabled = true;
    if (OG.World.ground) ctx.drawImage(OG.World.ground, -200, -200, 400, 400);
    ctx.save(); ctx.globalAlpha = 0.07; ctx.globalCompositeOperation = 'overlay';
    ctx.fillStyle = this.grain; ctx.translate(v.x0, v.y0); ctx.scale(1 / 10, 1 / 10);
    ctx.fillRect(0, 0, (v.x1 - v.x0) * 10, (v.y1 - v.y0) * 10); ctx.restore();

    // Gebäude, in denen der Spieler steht
    for (const b of OG.World.buildings) b.inside = b.enterable && P.x > b.x0 && P.x < b.x1 && P.y > b.y0 && P.y < b.y1;

    this.shadows(v);
    for (const o of OG.World.low) if (this.inView(v, o.x != null ? o.x : o.x0, o.y != null ? o.y : o.y0, o.r || 16)) this.drawLow(o);
    for (const n of OG.Nodes.list) if (this.inView(v, n.x, n.y, 3)) this.drawNode(n, false);
    for (const s of OG.interactables) if (s.clue && this.inView(v, s.x, s.y, 2)) this.drawClue(s);
    this.drawSeal();
    for (const f of OG.drawHooks || []) f(ctx, v);
    for (const b of OG.World.buildings) if (b.inside) this.drawInterior(b);

    // Figuren nach y sortiert
    const vis = OG.ents.filter(e => !e.removed && this.inView(v, e.x, e.y, 3));
    vis.sort((a, b) => (a.corpse ? -1000 : 0) + a.y - ((b.corpse ? -1000 : 0) + b.y));
    for (const e of vis) e.draw(ctx);
    for (const p of OG.projectiles) this.drawProjectile(p);
    this.drawFx();

    // Dächer, Kronen, Banner
    for (const b of OG.World.buildings) if (!b.inside && this.inView(v, b.x0, b.y0, 20)) this.drawRoof(b);
    for (const n of OG.Nodes.list) if (n.kind === 'Baum' && n.regrow <= 0 && this.inView(v, n.x, n.y, 4)) this.drawNode(n, true);
    for (const o of OG.World.high) if (o.type === 'citywall' || this.inView(v, o.x != null ? o.x : o.x0, o.y != null ? o.y : o.y0, 8)) this.drawHigh(o);

    ctx.setTransform(1, 0, 0, 1, 0, 0);
    this.lighting();
    this.weather();
    this.overlayTexts(vis);
  },

  sunOffset() {
    const h = OG.Clock.hourF(), t = OG.clamp01((h - 6) / 12);
    return { x: OG.lerp(-1.6, 1.6, t), y: 0.9, a: 0.28 * OG.Clock.daylight() };
  },
  shadows(v) {
    const ctx = this.ctx, s = this.sunOffset();
    if (s.a < 0.03) return;
    ctx.fillStyle = 'rgba(10,15,25,' + s.a + ')';
    for (const b of OG.World.buildings) if (!b.inside && this.inView(v, b.x0, b.y0, 20)) ctx.fillRect(b.x0 + s.x, b.y0 + s.y, b.x1 - b.x0, b.y1 - b.y0);
    for (const n of OG.Nodes.list) if (n.kind === 'Baum' && n.regrow <= 0 && this.inView(v, n.x, n.y, 4)) { ctx.beginPath(); ctx.ellipse(n.x + s.x * 1.4, n.y + s.y * 1.4, 2.2 * n.scale, 1.8 * n.scale, 0, 0, Math.PI * 2); ctx.fill(); }
  },

  drawLow(o) {
    const c = this.ctx;
    switch (o.type) {
      case 'tuft':
        if (o.dead) { c.strokeStyle = 'rgba(70,55,80,0.6)'; } else c.strokeStyle = o.v % 2 ? 'rgba(60,110,40,0.7)' : 'rgba(80,130,50,0.7)';
        c.lineWidth = 0.06; c.beginPath();
        for (let i = -1; i <= 1; i++) { c.moveTo(o.x + i * 0.12, o.y); c.lineTo(o.x + i * 0.2, o.y - 0.35); }
        c.stroke();
        if (o.flower) { c.fillStyle = ['#f2e66b', '#f2a6d9', '#ffffff', '#a6c8ff'][o.v]; c.beginPath(); c.arc(o.x + 0.15, o.y - 0.35, 0.08, 0, Math.PI * 2); c.fill(); }
        break;
      case 'stall': c.fillStyle = '#6b4a2e'; c.fillRect(o.x - 0.6, o.y - 1.5, 1.2, 3); c.fillStyle = '#d9c9a3'; for (let i = 0; i < 3; i++) { c.beginPath(); c.arc(o.x, o.y - 1 + i, 0.22, 0, Math.PI * 2); c.fill(); } break;
      case 'well': c.fillStyle = '#7a7670'; c.beginPath(); c.arc(o.x, o.y, o.r, 0, Math.PI * 2); c.fill(); c.fillStyle = '#2a4d73'; c.beginPath(); c.arc(o.x, o.y, o.r * 0.65, 0, Math.PI * 2); c.fill(); break;
      case 'forge': c.fillStyle = '#4d4a47'; c.fillRect(o.x - 1, o.y - 0.7, 2, 1.4); c.fillStyle = 'rgba(255,120,30,' + (0.75 + Math.sin(OG.time * 9) * 0.2) + ')'; c.fillRect(o.x - 0.6, o.y - 0.4, 1.2, 0.8); break;
      case 'anvil': c.fillStyle = '#3d3d42'; c.fillRect(o.x - 0.5, o.y - 0.25, 1, 0.5); c.fillStyle = '#5c5c63'; c.fillRect(o.x - 0.6, o.y - 0.15, 0.3, 0.3); break;
      case 'board': c.fillStyle = '#5c4026'; c.fillRect(o.x - 1.2, o.y - 0.2, 2.4, 0.4); c.fillStyle = '#efe6cc'; for (let i = 0; i < 4; i++) c.fillRect(o.x - 1 + i * 0.55, o.y - 0.15, 0.4, 0.3); break;
      case 'arena':
        c.strokeStyle = '#8c857a'; c.lineWidth = 1.6; c.beginPath(); c.arc(o.x, o.y, o.r, 0.13, Math.PI * 2 - 0.13); c.stroke();
        c.strokeStyle = '#6b4a2e'; c.lineWidth = 1.8; c.beginPath(); c.arc(o.x, o.y, o.r + 2, 0.13, Math.PI * 2 - 0.13); c.stroke();
        c.fillStyle = 'rgba(200,180,140,0.25)'; c.beginPath(); c.arc(o.x, o.y, o.r - 1, 0, Math.PI * 2); c.fill();
        if (OG.Trial.active) { c.strokeStyle = 'rgba(90,170,255,0.9)'; c.lineWidth = 0.6; c.beginPath(); c.arc(o.x, o.y, o.r, -0.14, 0.14); c.stroke(); }
        break;
      case 'site':
        if (OG.Autonomy.townHall) break;
        c.strokeStyle = '#8c6a40'; c.lineWidth = 0.2; c.setLineDash([0.6, 0.4]); c.strokeRect(o.x - 5, o.y - 2, 10, 7); c.setLineDash([]);
        c.fillStyle = '#a07a4a'; c.fillRect(o.x - 1.2, o.y + 0.5, 2.4, 0.5); c.fillRect(o.x + 1.5, o.y + 1.5, 0.6, 1.6);
        break;
      case 'campfire':
        c.fillStyle = '#2a1f1a'; c.beginPath(); c.arc(o.x, o.y, 1, 0, Math.PI * 2); c.fill();
        c.fillStyle = 'rgba(190,80,255,' + (0.7 + Math.sin(OG.time * 7) * 0.2) + ')'; c.beginPath(); c.arc(o.x, o.y, 0.55, 0, Math.PI * 2); c.fill();
        break;
      case 'paving': {
        const v = this.view();
        c.save(); c.beginPath(); c.arc(o.x, o.y, o.r, 0, Math.PI * 2); c.clip();
        c.globalAlpha = 0.55; c.scale(1 / 16, 1 / 16); c.fillStyle = this.paving;
        c.fillRect(Math.max(v.x0, o.x - o.r) * 16, Math.max(v.y0, o.y - o.r) * 16, (Math.min(v.x1, o.x + o.r) - Math.max(v.x0, o.x - o.r)) * 16, (Math.min(v.y1, o.y + o.r) - Math.max(v.y0, o.y - o.r)) * 16);
        c.restore(); break;
      }
      case 'column': c.fillStyle = '#e6e1d6'; c.beginPath(); c.arc(o.x, o.y, 0.5, 0, Math.PI * 2); c.fill(); break;
    }
  },

  drawNode(n, canopy) {
    const c = this.ctx, s = n.scale;
    if (n.kind === 'Baum') {
      if (!canopy) {
        c.fillStyle = n.dead ? '#3a2f38' : '#5c4026';
        c.beginPath(); c.arc(n.x, n.y, (n.regrow > 0 ? 0.35 : 0.45) * s, 0, Math.PI * 2); c.fill();
        if (n.regrow > 0) { c.fillStyle = '#a07a4a'; c.beginPath(); c.arc(n.x, n.y, 0.25 * s, 0, Math.PI * 2); c.fill(); }
        return;
      }
      const P = OG.player, under = Math.hypot(P.x - n.x, P.y - n.y) < 2.2 * s;
      c.globalAlpha = under ? 0.35 : 1;
      if (n.dead) {
        c.strokeStyle = '#3a2f38'; c.lineWidth = 0.18 * s; c.beginPath();
        for (let i = 0; i < 4; i++) { const a = n.variant + i * 1.6; c.moveTo(n.x, n.y); c.lineTo(n.x + Math.cos(a) * 1.4 * s, n.y + Math.sin(a) * 1.4 * s - 0.4); }
        c.stroke();
      } else {
        const cols = [['#2e5c2a', '#3f7a35'], ['#3a6b2e', '#4f8a3d'], ['#285430', '#386e3d']][n.variant % 3];
        const r = 2 * s;
        c.fillStyle = cols[0];
        if (n.variant % 2 === 0) {
          c.beginPath(); c.arc(n.x, n.y - 0.5, r, 0, Math.PI * 2); c.fill();
          c.fillStyle = cols[1]; c.beginPath(); c.arc(n.x - 0.4 * s, n.y - 0.9, r * 0.6, 0, Math.PI * 2); c.fill();
        } else {
          for (let i = 0; i < 3; i++) { c.fillStyle = i % 2 ? cols[1] : cols[0]; c.beginPath(); c.arc(n.x, n.y - 0.4 - i * 0.3, r * (1 - i * 0.28), 0, Math.PI * 2); c.fill(); }
        }
        c.fillStyle = 'rgba(255,255,220,0.08)'; c.beginPath(); c.arc(n.x - 0.6 * s, n.y - 1.1, r * 0.35, 0, Math.PI * 2); c.fill();
      }
      c.globalAlpha = 1;
      return;
    }
    if (n.regrow > 0) return;
    if (n.kind === 'Erz' || n.kind === 'Fels') {
      c.fillStyle = n.kind === 'Erz' ? '#6b625c' : '#858079';
      c.beginPath(); c.ellipse(n.x, n.y, 0.95, 0.75, 0.3, 0, Math.PI * 2); c.fill();
      c.fillStyle = 'rgba(255,255,255,0.12)'; c.beginPath(); c.ellipse(n.x - 0.25, n.y - 0.25, 0.45, 0.3, 0.3, 0, Math.PI * 2); c.fill();
      if (n.kind === 'Erz') { c.fillStyle = '#c4733f'; c.fillRect(n.x + 0.1, n.y - 0.2, 0.25, 0.22); c.fillRect(n.x - 0.4, n.y + 0.15, 0.2, 0.2); }
    } else if (n.kind === 'Kraut') {
      c.fillStyle = '#4fa63d';
      for (let i = 0; i < 5; i++) { const a = i * 1.25; c.beginPath(); c.ellipse(n.x + Math.cos(a) * 0.22, n.y + Math.sin(a) * 0.22, 0.2, 0.1, a, 0, Math.PI * 2); c.fill(); }
      c.fillStyle = '#f2d94d'; c.beginPath(); c.arc(n.x, n.y, 0.1, 0, Math.PI * 2); c.fill();
    } else {
      c.fillStyle = '#73bfff'; c.beginPath(); c.moveTo(n.x, n.y - 0.9); c.lineTo(n.x + 0.4, n.y); c.lineTo(n.x, n.y + 0.4); c.lineTo(n.x - 0.4, n.y); c.fill();
      c.fillStyle = 'rgba(200,235,255,0.7)'; c.beginPath(); c.moveTo(n.x, n.y - 0.7); c.lineTo(n.x + 0.15, n.y - 0.1); c.lineTo(n.x - 0.1, n.y - 0.1); c.fill();
    }
  },

  drawClue(s) {
    const c = this.ctx, pulse = 0.6 + Math.sin(OG.time * 4) * 0.3;
    c.strokeStyle = 'rgba(242,193,77,' + pulse + ')'; c.lineWidth = 0.08; c.beginPath(); c.arc(s.x, s.y, 0.9, 0, Math.PI * 2); c.stroke();
    if (s.clue === 'runensplitter') { c.fillStyle = '#b340ff'; c.beginPath(); c.moveTo(s.x, s.y - 0.5); c.lineTo(s.x + 0.2, s.y + 0.3); c.lineTo(s.x - 0.2, s.y + 0.3); c.fill(); }
    else if (s.clue === 'brandzeichen') { c.fillStyle = '#1a1414'; c.beginPath(); c.arc(s.x, s.y, 0.7, 0, Math.PI * 2); c.fill(); c.strokeStyle = '#ff7326'; c.lineWidth = 0.1; c.beginPath(); c.moveTo(s.x - 0.5, s.y); c.lineTo(s.x + 0.5, s.y); c.moveTo(s.x, s.y - 0.5); c.lineTo(s.x, s.y + 0.5); c.stroke(); }
    else if (s.clue === 'brief') { c.fillStyle = '#2a1f1a'; c.beginPath(); c.arc(s.x, s.y, 0.6, 0, Math.PI * 2); c.fill(); c.fillStyle = '#ff8c33'; c.beginPath(); c.arc(s.x, s.y, 0.3, 0, Math.PI * 2); c.fill(); c.fillStyle = '#efe6cc'; c.fillRect(s.x + 0.6, s.y - 0.2, 0.4, 0.5); }
    else { c.fillStyle = '#5c4026'; c.fillRect(s.x - 0.6, s.y - 0.35, 1.2, 0.7); c.fillStyle = '#efe6cc'; c.fillRect(s.x - 0.25, s.y - 0.2, 0.5, 0.4); }
  },

  drawSeal() {
    const c = this.ctx, P = OG.Places.seal; if (!P) return;
    const t = OG.Rift.strength / 100;
    const col = 'rgb(' + Math.round(OG.lerp(180, 77, t)) + ',' + Math.round(OG.lerp(40, 217, t)) + ',' + Math.round(OG.lerp(230, 255, t)) + ')';
    c.fillStyle = '#3d3d47'; c.fillRect(P.x - 1.4, P.y - 1.4, 2.8, 2.8);
    c.fillStyle = '#26262e'; c.fillRect(P.x - 0.7, P.y - 0.7, 1.4, 1.4);
    c.strokeStyle = col; c.lineWidth = 0.18; c.beginPath(); c.arc(P.x, P.y, 1.9, OG.time * 0.5, OG.time * 0.5 + Math.PI * 2 * Math.max(0.05, t)); c.stroke();
  },

  drawInterior(b) {
    const c = this.ctx;
    c.fillStyle = b.floor; c.fillRect(b.x0, b.y0, b.x1 - b.x0, b.y1 - b.y0);
    c.strokeStyle = 'rgba(0,0,0,0.12)'; c.lineWidth = 0.03;
    for (let x = b.x0 + 1; x < b.x1; x += 1) { c.beginPath(); c.moveTo(x, b.y0); c.lineTo(x, b.y1); c.stroke(); }
    for (const f of b.furniture) {
      if (f.type === 'carpet') { c.fillStyle = '#7a1f26'; c.fillRect(f.x0, f.y0, f.x1 - f.x0, f.y1 - f.y0); c.strokeStyle = '#d9a640'; c.lineWidth = 0.08; c.strokeRect(f.x0 + 0.15, f.y0, f.x1 - f.x0 - 0.3, f.y1 - f.y0); }
      else if (f.type === 'dais') { c.fillStyle = '#5c5c6b'; c.fillRect(f.x0, f.y0, f.x1 - f.x0, f.y1 - f.y0); }
      else if (f.type === 'throne') { c.fillStyle = '#f2c14d'; c.fillRect(f.x - 1, f.y - 0.6, 2, 1.2); c.fillStyle = '#b3862d'; c.fillRect(f.x - 1.1, f.y + 0.4, 2.2, 0.4); OG.drawStar(c, f.x, f.y - 0.05, 0.35, '#fff3cc'); }
      else if (f.type === 'column') { c.fillStyle = '#dcd6c9'; c.beginPath(); c.arc(f.x, f.y, 0.45, 0, Math.PI * 2); c.fill(); }
      else if (f.type === 'desk') { c.fillStyle = '#6b4a2e'; c.fillRect(f.x0, f.y0, f.x1 - f.x0, f.y1 - f.y0); c.fillStyle = '#efe6cc'; c.fillRect(f.x0 + 0.1, f.y0 + 0.4, 0.4, 0.3); }
      else if (f.type === 'bed') { c.fillStyle = '#c9c2b3'; c.fillRect(f.x0, f.y0, f.x1 - f.x0, f.y1 - f.y0); c.fillStyle = '#ffffff'; c.fillRect(f.x0, f.y0, 0.5, f.y1 - f.y0); c.fillStyle = '#6b7fa6'; c.fillRect(f.x0 + 0.8, f.y0, f.x1 - f.x0 - 0.8, f.y1 - f.y0); }
      else if (f.type === 'chest') { c.fillStyle = '#7a5530'; c.fillRect(f.x0, f.y0, f.x1 - f.x0, f.y1 - f.y0); c.fillStyle = '#c9a640'; c.fillRect(f.x0, (f.y0 + f.y1) / 2 - 0.05, f.x1 - f.x0, 0.1); }
      else if (f.type === 'table') { c.fillStyle = '#7a5530'; c.beginPath(); c.arc(f.x, f.y, 0.8, 0, Math.PI * 2); c.fill(); }
    }
    // Wände
    c.strokeStyle = '#2e2a26'; c.lineWidth = 0.4;
    const g = b.doorW / 2, cx = (b.x0 + b.x1) / 2, cy = (b.y0 + b.y1) / 2, i = 0.2;
    c.beginPath();
    const seg = (x0, y0, x1, y1) => { c.moveTo(x0, y0); c.lineTo(x1, y1); };
    if (b.door === 'N') { seg(b.x0, b.y0 + i, cx - g, b.y0 + i); seg(cx + g, b.y0 + i, b.x1, b.y0 + i); } else seg(b.x0, b.y0 + i, b.x1, b.y0 + i);
    if (b.door === 'S') { seg(b.x0, b.y1 - i, cx - g, b.y1 - i); seg(cx + g, b.y1 - i, b.x1, b.y1 - i); } else seg(b.x0, b.y1 - i, b.x1, b.y1 - i);
    if (b.door === 'W') { seg(b.x0 + i, b.y0, b.x0 + i, cy - g); seg(b.x0 + i, cy + g, b.x0 + i, b.y1); } else seg(b.x0 + i, b.y0, b.x0 + i, b.y1);
    if (b.door === 'E') { seg(b.x1 - i, b.y0, b.x1 - i, cy - g); seg(b.x1 - i, cy + g, b.x1 - i, b.y1); } else seg(b.x1 - i, b.y0, b.x1 - i, b.y1);
    c.stroke();
  },

  drawRoof(b) {
    const c = this.ctx, w = b.x1 - b.x0, h = b.y1 - b.y0, horiz = w >= h;
    const light = OG.shadeCol(b.roof, 1.18), dark = OG.shadeCol(b.roof, 0.82);
    if (horiz) { c.fillStyle = light; c.fillRect(b.x0, b.y0, w, h / 2); c.fillStyle = dark; c.fillRect(b.x0, b.y0 + h / 2, w, h / 2); }
    else { c.fillStyle = light; c.fillRect(b.x0, b.y0, w / 2, h); c.fillStyle = dark; c.fillRect(b.x0 + w / 2, b.y0, w / 2, h); }
    c.strokeStyle = 'rgba(0,0,0,0.18)'; c.lineWidth = 0.05;
    if (horiz) for (let y = b.y0 + 0.6; y < b.y1; y += 0.6) { c.beginPath(); c.moveTo(b.x0, y); c.lineTo(b.x1, y); c.stroke(); }
    else for (let x = b.x0 + 0.6; x < b.x1; x += 0.6) { c.beginPath(); c.moveTo(x, b.y0); c.lineTo(x, b.y1); c.stroke(); }
    c.strokeStyle = b.palace ? '#f2c14d' : OG.shadeCol(b.roof, 0.6); c.lineWidth = b.palace ? 0.3 : 0.15;
    c.beginPath(); if (horiz) { c.moveTo(b.x0, b.y0 + h / 2); c.lineTo(b.x1, b.y0 + h / 2); } else { c.moveTo(b.x0 + w / 2, b.y0); c.lineTo(b.x0 + w / 2, b.y1); } c.stroke();
    c.strokeStyle = 'rgba(0,0,0,0.45)'; c.lineWidth = 0.12; c.strokeRect(b.x0, b.y0, w, h);
    if (b.palace) OG.drawStar(c, (b.x0 + b.x1) / 2, (b.y0 + b.y1) / 2, 2.2, '#f2c14d');
    if (b.enterable) {
      c.fillStyle = 'rgba(30,20,10,0.85)';
      const cx = (b.x0 + b.x1) / 2, cy = (b.y0 + b.y1) / 2, g = b.doorW / 2;
      if (b.door === 'N') c.fillRect(cx - g, b.y0 - 0.15, b.doorW, 0.3);
      if (b.door === 'S') c.fillRect(cx - g, b.y1 - 0.15, b.doorW, 0.3);
      if (b.door === 'W') c.fillRect(b.x0 - 0.15, cy - g, 0.3, b.doorW);
      if (b.door === 'E') c.fillRect(b.x1 - 0.15, cy - g, 0.3, b.doorW);
    }
  },

  drawHigh(o) {
    const c = this.ctx;
    switch (o.type) {
      case 'citywall':
        c.strokeStyle = '#a39e94'; c.lineWidth = 1.8;
        for (let i = 0; i < 3; i++) {
          const g = [o.gates[0], o.gates[1], o.gates[2]];
          const sorted = g.map(a => (a + Math.PI * 2) % (Math.PI * 2)).sort((a, b) => a - b);
          const a0 = sorted[i] + 0.12, a1 = (i < 2 ? sorted[i + 1] : sorted[0] + Math.PI * 2) - 0.12;
          c.beginPath(); c.arc(o.x, o.y, o.r, a0, a1); c.stroke();
        }
        c.strokeStyle = '#7a766e'; c.lineWidth = 0.25; c.beginPath(); c.arc(o.x, o.y, o.r + 0.9, 0, Math.PI * 2); c.stroke();
        break;
      case 'tower': c.fillStyle = '#8a8579'; c.beginPath(); c.arc(o.x, o.y, o.r, 0, Math.PI * 2); c.fill(); c.fillStyle = '#6b665d'; c.beginPath(); c.arc(o.x, o.y, o.r * 0.6, 0, Math.PI * 2); c.fill(); break;
      case 'banner': {
        c.fillStyle = '#4d3d2e'; c.beginPath(); c.arc(o.x, o.y, 0.15, 0, Math.PI * 2); c.fill();
        const sway = Math.sin(OG.time * 2 + o.x) * 0.12;
        c.fillStyle = o.color || '#0f1f52'; c.beginPath(); c.moveTo(o.x, o.y - 0.1); c.lineTo(o.x + 1.4, o.y - 0.1 + sway); c.lineTo(o.x + 1.4, o.y + 0.9 + sway); c.lineTo(o.x, o.y + 0.9); c.fill();
        if (!o.color) OG.drawStar(c, o.x + 0.7, o.y + 0.4 + sway / 2, 0.3, '#f2c14d');
        break;
      }
      case 'awning': c.fillStyle = o.color; c.globalAlpha = 0.95; c.fillRect(o.x - 1.1, o.y - 1.7, 2.2, 3.4); c.fillStyle = 'rgba(255,255,255,0.2)'; for (let i = 0; i < 3; i++) c.fillRect(o.x - 1.1 + i * 0.8, o.y - 1.7, 0.4, 3.4); c.globalAlpha = 1; break;
      case 'pavilion': {
        const P = OG.player, under = P.x > o.x0 && P.x < o.x1 && P.y > o.y0 && P.y < o.y1;
        c.globalAlpha = under ? 0.3 : 1; c.fillStyle = '#5c5953'; c.fillRect(o.x0, o.y0, o.x1 - o.x0, (o.y1 - o.y0) / 2); c.fillStyle = '#4a4743'; c.fillRect(o.x0, (o.y0 + o.y1) / 2, o.x1 - o.x0, (o.y1 - o.y0) / 2);
        c.globalAlpha = 1; break;
      }
      case 'gate': {
        c.fillStyle = '#5c5953'; c.beginPath(); c.arc(o.x - 3, o.y, 0.8, 0, Math.PI * 2); c.arc(o.x + 3, o.y, 0.8, 0, Math.PI * 2); c.fill();
        c.fillStyle = '#4d4a45'; c.fillRect(o.x - 3.6, o.y - 0.5, 7.2, 1);
        const a = 0.55 + Math.sin(OG.time * 3) * 0.2;
        c.fillStyle = 'rgba(90,180,255,' + a + ')'; c.fillRect(o.x - 2.3, o.y - 0.3, 4.6, 0.6);
        OG.drawStar(c, o.x, o.y, 0.35, '#f2c14d');
        break;
      }
      case 'marker': c.fillStyle = '#4d4a52'; c.beginPath(); c.arc(o.x, o.y, 0.7, 0, Math.PI * 2); c.fill(); c.fillStyle = '#a64dff'; c.beginPath(); c.arc(o.x, o.y, 0.35, 0, Math.PI * 2); c.fill(); break;
      case 'borderpost': c.fillStyle = '#4d4a52'; c.beginPath(); c.arc(o.x - 4, o.y, 1.6, 0, Math.PI * 2); c.arc(o.x + 4, o.y, 1.6, 0, Math.PI * 2); c.fill(); c.fillRect(o.x - 5, o.y - 0.6, 10, 1.2); break;
      case 'crystal': {
        c.save(); c.translate(o.x, o.y); c.rotate(o.rot);
        c.fillStyle = '#5a1a80'; c.beginPath(); c.moveTo(0, -1.4 * o.s); c.lineTo(0.6 * o.s, 0); c.lineTo(0, 0.6 * o.s); c.lineTo(-0.6 * o.s, 0); c.fill();
        c.fillStyle = 'rgba(210,140,255,' + (0.5 + Math.sin(OG.time * 2 + o.x) * 0.25) + ')'; c.beginPath(); c.moveTo(0, -1.1 * o.s); c.lineTo(0.2 * o.s, 0); c.lineTo(-0.2 * o.s, 0); c.fill();
        c.restore(); break;
      }
      case 'tent': c.fillStyle = '#40264d'; c.beginPath(); c.moveTo(o.x - 1.6, o.y + 1.3); c.lineTo(o.x, o.y - 1.4); c.lineTo(o.x + 1.6, o.y + 1.3); c.fill(); c.fillStyle = '#2e1a38'; c.beginPath(); c.moveTo(o.x, o.y - 1.4); c.lineTo(o.x + 1.6, o.y + 1.3); c.lineTo(o.x, o.y + 1.3); c.fill(); break;
    }
  },

  drawProjectile(p) {
    const c = this.ctx, sp = Math.hypot(p.vx, p.vy) || 1, nx = p.vx / sp, ny = p.vy / sp;
    if (p.arrow) {
      const lift = p.z * 0.25;
      c.strokeStyle = 'rgba(0,0,0,0.25)'; c.lineWidth = 0.06; c.beginPath(); c.moveTo(p.x, p.y); c.lineTo(p.x - nx * 0.7, p.y - ny * 0.7); c.stroke();
      c.strokeStyle = p.color; c.lineWidth = 0.07; c.beginPath(); c.moveTo(p.x, p.y - lift); c.lineTo(p.x - nx * 0.8, p.y - ny * 0.8 - lift); c.stroke();
      return;
    }
    c.fillStyle = p.color; c.globalAlpha = 0.35; c.beginPath(); c.arc(p.x - nx * 0.4, p.y - ny * 0.4, p.r * 0.8, 0, Math.PI * 2); c.fill();
    c.globalAlpha = 1; c.beginPath(); c.arc(p.x, p.y, p.r, 0, Math.PI * 2); c.fill();
    c.fillStyle = '#ffffff'; c.beginPath(); c.arc(p.x, p.y, p.r * 0.45, 0, Math.PI * 2); c.fill();
  },

  drawFx() {
    const c = this.ctx, F = OG.FX;
    for (const d of F.discs) { const t = d.t / d.life; c.fillStyle = d.color; c.globalAlpha = OG.lerp(0.15, 0.5, t); c.beginPath(); c.arc(d.x, d.y, d.r * OG.lerp(0.3, 1, t), 0, Math.PI * 2); c.fill(); }
    for (const r of F.rings) { const t = r.t / r.life; c.strokeStyle = r.color; c.globalAlpha = 1 - t; c.lineWidth = OG.lerp(0.3, 0.05, t); c.beginPath(); c.arc(r.x, r.y, r.r * OG.lerp(0.3, 1, Math.sqrt(t)), 0, Math.PI * 2); c.stroke(); }
    for (const s of F.slashes) {
      const t = s.t / s.life, src = OG.player; const x = src.x, y = src.y;
      c.strokeStyle = s.color; c.globalAlpha = 0.8 * (1 - t); c.lineWidth = 0.25;
      c.beginPath(); c.arc(x, y, s.r * 0.85, s.ang - s.arc / 2, s.ang - s.arc / 2 + s.arc * Math.min(1, t * 2.5)); c.stroke();
    }
    for (const p of F.parts) { c.globalAlpha = 1 - p.t / p.life; c.fillStyle = p.color; c.fillRect(p.x - p.size / 2, p.y - p.size / 2, p.size, p.size); }
    c.globalAlpha = 1;
  },

  lighting() {
    const W = this.canvas.width, H = this.canvas.height, d = this.dctx, z = OG.cam.zoom * this.dpr;
    const day = OG.Clock.daylight(), wl = OG.Weather.light;
    const darkness = OG.clamp((1 - day) * 0.8 + (1 - wl) * 0.3, 0, 0.86);
    const hour = OG.Clock.hourF(), dusk = Math.max(0, 1 - Math.abs(hour - 19) / 1.5) + Math.max(0, 1 - Math.abs(hour - 6) / 1.5);
    if (darkness > 0.02) {
      d.setTransform(1, 0, 0, 1, 0, 0); d.globalCompositeOperation = 'source-over'; d.clearRect(0, 0, W, H);
      d.fillStyle = 'rgba(8,12,34,' + darkness + ')'; d.fillRect(0, 0, W, H);
      d.globalCompositeOperation = 'destination-out';
      const toS = (x, y) => [(x - OG.cam.x) * z + W / 2, (y - OG.cam.y) * z + H / 2];
      const hole = (x, y, r, k) => {
        const [sx, sy] = toS(x, y), rr = r * z;
        if (sx < -rr || sy < -rr || sx > W + rr || sy > H + rr) return;
        const g = d.createRadialGradient(sx, sy, 0, sx, sy, rr);
        g.addColorStop(0, 'rgba(0,0,0,' + k + ')'); g.addColorStop(1, 'rgba(0,0,0,0)');
        d.fillStyle = g; d.fillRect(sx - rr, sy - rr, rr * 2, rr * 2);
      };
      const P = OG.player;
      hole(P.x, P.y, 9, 0.85);
      for (const l of OG.World.lights) if (!l.inside || l.inside.inside) hole(l.x, l.y, l.r * (l.fire ? 1 + Math.sin(OG.time * 9 + l.x) * 0.05 : 1), l.inside ? 1 : 0.9);
      for (const p of OG.projectiles) if (p.glow) hole(p.x, p.y, 3, 0.7);
      hole(OG.Places.seal.x, OG.Places.seal.y, 6, 0.8);
      this.ctx.drawImage(this.dark, 0, 0);
    }
    const c = this.ctx;
    if (dusk > 0.01 && darkness < 0.8) { c.fillStyle = 'rgba(255,120,50,' + (dusk * 0.1) + ')'; c.fillRect(0, 0, W, H); }
    // farbige Lichter (Feuer, Riss) leuchten zusätzlich
    if (darkness > 0.15) {
      c.globalCompositeOperation = 'lighter';
      for (const l of OG.World.lights) {
        if (!(l.fire || l.violet || l.cyan)) continue;
        const sx = (l.x - OG.cam.x) * z + W / 2, sy = (l.y - OG.cam.y) * z + H / 2, rr = l.r * z * 0.8;
        if (sx < -rr || sy < -rr || sx > W + rr || sy > H + rr) continue;
        const g = c.createRadialGradient(sx, sy, 0, sx, sy, rr);
        const col = l.violet ? '150,60,255' : l.cyan ? '60,150,255' : '255,130,40';
        g.addColorStop(0, 'rgba(' + col + ',' + (0.22 * darkness) + ')'); g.addColorStop(1, 'rgba(' + col + ',0)');
        c.fillStyle = g; c.fillRect(sx - rr, sy - rr, rr * 2, rr * 2);
      }
      c.globalCompositeOperation = 'source-over';
    }
  },

  weather() {
    const c = this.ctx, W = this.canvas.width, H = this.canvas.height, w = OG.Weather;
    if (w.fog > 0.01) { c.fillStyle = 'rgba(190,200,210,' + (w.fog * 0.45) + ')'; c.fillRect(0, 0, W, H); }
    const rate = w.cur === 2 ? 220 : w.cur === 3 ? 420 : 0;
    while (this.rain.length < rate) this.rain.push({ x: Math.random() * W, y: Math.random() * H, s: 0.6 + Math.random() * 0.6 });
    if (this.rain.length > rate) this.rain.length = rate;
    if (rate) {
      c.strokeStyle = 'rgba(180,200,255,0.45)'; c.lineWidth = this.dpr; c.beginPath();
      const dt = OG.lastDt || 0.016;
      for (const r of this.rain) {
        r.x -= 120 * this.dpr * dt * r.s; r.y += 900 * this.dpr * dt * r.s;
        if (r.y > H) { r.y = -20; r.x = Math.random() * (W + 100); }
        c.moveTo(r.x, r.y); c.lineTo(r.x + 3 * this.dpr, r.y - 18 * this.dpr * r.s);
      }
      c.stroke();
    }
    if (w.flash > 0) { c.fillStyle = 'rgba(230,235,255,' + (w.flash * 0.35) + ')'; c.fillRect(0, 0, W, H); }
  },

  // Namen, Lebensbalken, Auftragszeichen, Schadenszahlen (Bildschirmkoordinaten)
  overlayTexts(vis) {
    const c = this.ctx, z = OG.cam.zoom * this.dpr, W = this.canvas.width, H = this.canvas.height, P = OG.player, dpr = this.dpr;
    const toS = (x, y) => [(x - OG.cam.x) * z + W / 2, (y - OG.cam.y) * z + H / 2];
    c.textAlign = 'center'; c.textBaseline = 'middle';
    for (const e of vis) {
      if (e === P || e.removed) continue;
      const npc = e instanceof OG.Npc, d = Math.hypot(e.x - P.x, e.y - P.y);
      if (e.dead && !npc) continue;
      const [sx, sy] = toS(e.x, e.y - (e.r + 0.9));
      if (npc && d < 14 && !e.ko) {
        c.font = '600 ' + (12 * dpr) + 'px "Barlow Semi Condensed", sans-serif';
        c.fillStyle = 'rgba(0,0,0,0.6)'; c.fillText(e.npcName, sx + dpr, sy + dpr);
        c.fillStyle = e.faction === 'Imperial' ? '#f2c14d' : e.faction === 'Ally' ? '#7deb8f' : '#eef3f7'; c.fillText(e.npcName, sx, sy);
        const mark = OG.QuestSys.hasTurnIn(e.npcName) ? '?' : OG.QuestSys.hasOffer(e.npcName) ? '!' : null;
        if (mark) { c.font = '700 ' + (26 * dpr) + 'px "Grenze Gotisch", serif'; c.fillStyle = 'rgba(0,0,0,0.6)'; c.fillText(mark, sx + dpr, sy - 22 * dpr + dpr); c.fillStyle = '#f2c14d'; c.fillText(mark, sx, sy - 22 * dpr); }
      }
      if (e instanceof OG.Rival && d < 22) {
        const col = e.duel ? '#f2c14d' : e.outlaw ? '#ff6b5e' : '#9fc8ff';
        c.font = '600 ' + (12 * dpr) + 'px "Barlow Semi Condensed", sans-serif';
        const label = e.name + ' · ' + e.level + (e.outlaw ? ' · Gesetzlos' : '');
        c.fillStyle = 'rgba(0,0,0,0.6)'; c.fillText(label, sx + dpr, sy + dpr);
        c.fillStyle = col; c.fillText(label, sx, sy);
      }
      const show = !e.invulnerable && (e.frac < 1 || (e instanceof OG.Creature && e.hostile && d < 28) || (e instanceof OG.Rival && (e.outlaw || e.duel || e.target === P) && d < 22));
      if (show && d < 30) {
        const bw = 44 * dpr, bh = 5 * dpr;
        c.fillStyle = 'rgba(0,0,0,0.6)'; c.fillRect(sx - bw / 2, sy + 8 * dpr, bw, bh);
        c.fillStyle = e.faction === 'Ally' ? '#5fd17a' : '#e35b5b'; c.fillRect(sx - bw / 2, sy + 8 * dpr, bw * e.frac, bh);
        if (e instanceof OG.Creature && (e.def.size > 1.3 || e.questTag)) { c.font = '600 ' + (11 * dpr) + 'px "Barlow Semi Condensed", sans-serif'; c.fillStyle = '#ffd9d9'; c.fillText(e.name, sx, sy - 2 * dpr); }
      }
    }
    for (const f of OG.FX.floaters) {
      const t = f.t / 1.1, [sx, sy] = toS(f.x, f.y - t * 1.2);
      c.globalAlpha = 1 - t * t;
      c.font = '700 ' + ((f.big ? 26 : 17) * dpr) + 'px "Barlow Semi Condensed", sans-serif';
      c.fillStyle = 'rgba(0,0,0,0.7)'; c.fillText(f.text, sx + 1.5 * dpr, sy + 1.5 * dpr);
      c.fillStyle = f.color; c.fillText(f.text, sx, sy);
    }
    c.globalAlpha = 1;
  },
};

// Achtzackiger Stern der Genesis (Art. 20)
OG.drawStar = (c, x, y, r, color) => {
  c.fillStyle = color; c.beginPath();
  for (let i = 0; i < 16; i++) { const a = i * Math.PI / 8 - Math.PI / 2, rr = i % 2 ? r * 0.45 : r; c.lineTo(x + Math.cos(a) * rr, y + Math.sin(a) * rr); }
  c.closePath(); c.fill();
};
