// Omni Genesis 2D — Figuren, Physik des Kampfes (F = m · a), Geschosse und Effekte.
'use strict';

OG.time = 0;
OG.ents = [];
OG.projectiles = [];
OG.interactables = [];
OG.FX = { rings: [], parts: [], discs: [], slashes: [], floaters: [] };

// ---------- Physik ----------
OG.Phys = {
  fromForce(m, a, mult, perN) {
    const F = m * a;
    return { m, a, F, dmg: F * perN * mult, J: F * OG.C.impactTime, f: 'F = ' + OG.fmt(m, 2) + ' kg · ' + OG.fmt(a) + ' m/s² = ' + OG.fmt(F) + ' N' };
  },
  // Nahkampf: v = θ · L / t, a = v / t (+ Anlauf / t), F = m · a
  melee(m, L, t, approach, mult) {
    t = Math.max(0.08, t);
    const a = (2.4 * L / t) / t + Math.max(0, approach) / t;
    return this.fromForce(m, a, mult, OG.C.damagePerNewton);
  },
  spell(m, a, mult) { return this.fromForce(m, a, mult, OG.C.damagePerNewton); },
  body(m, a, mult) { return this.fromForce(m, a, mult, OG.C.damagePerNewton); },
  // Bogen: a = F / m, v = √(2 · F · d / m)
  arrowSpeed(F, d, m) { return Math.sqrt(2 * F * d / m); },
  // Treffer: E = ½ · m · v²
  arrowImpact(m, v, mult) {
    const E = 0.5 * m * v * v;
    return { m, a: 0, F: E, dmg: E * OG.C.damagePerJoule * mult, J: m * v, f: 'E = ½ · ' + OG.fmt(m, 3) + ' kg · (' + OG.fmt(v) + ' m/s)² = ' + OG.fmt(E) + ' J' };
  },
};

// ---------- Effekte ----------
OG.fx = {
  ring(x, y, r, color, life) { OG.FX.rings.push({ x, y, r, color, t: 0, life }); },
  disc(x, y, r, color, life) { OG.FX.discs.push({ x, y, r, color, t: 0, life }); },
  slash(x, y, ang, arc, r, color, life) { OG.FX.slashes.push({ x, y, ang, arc, r, color, t: 0, life: life || 0.18 }); },
  burst(x, y, color, n) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * Math.PI * 2, s = 2 + Math.random() * 4;
      OG.FX.parts.push({ x, y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, color, t: 0, life: 0.3 + Math.random() * 0.4, size: 0.08 + Math.random() * 0.1 });
    }
  },
  float(x, y, text, color, big) { OG.FX.floaters.push({ x: x + OG.rand(-0.3, 0.3), y, text, color, t: 0, big }); },
  update(dt) {
    for (const k of ['rings', 'discs', 'slashes', 'floaters']) {
      const arr = OG.FX[k];
      for (let i = arr.length - 1; i >= 0; i--) { arr[i].t += dt; if (arr[i].t >= (arr[i].life || 1.1)) arr.splice(i, 1); }
    }
    const p = OG.FX.parts;
    for (let i = p.length - 1; i >= 0; i--) {
      const q = p[i]; q.t += dt; q.x += q.vx * dt; q.y += q.vy * dt; q.vx *= 0.92; q.vy *= 0.92;
      if (q.t > q.life) p.splice(i, 1);
    }
  },
};

OG.log = line => { OG.combatLog.push(line); if (OG.combatLog.length > 6) OG.combatLog.shift(); };
OG.combatLog = [];

// ---------- Basisfigur ----------
class Entity {
  constructor(x, y, r) {
    this.x = x; this.y = y; this.r = r;
    this.vx = 0; this.vy = 0;
    this.mass = 70; this.hp = 100; this.maxHp = 100; this.armor = 0;
    this.faction = 'Wild'; this.name = ''; this.dead = false; this.invulnerable = false;
    this.face = 0; this.flash = 0; this.height = 1.8; this.lastAttacker = null; this.lastHit = -99;
  }
  get frac() { return this.maxHp > 0 ? this.hp / this.maxHp : 0; }
  damage(info) {
    if (this.dead) return 0;
    if (this.modify) info = this.modify(info);
    let d = Math.max(0, info.amount) * 60 / (60 + Math.max(0, this.armor));
    if (this.invulnerable) d = 0; else if (info.amount > 0 && d < 1) d = 1;
    this.hp -= d; this.flash = 1; this.lastAttacker = info.source || null; this.lastHit = OG.time;
    this.onDamaged(info, d);
    if (this.hp <= 0 && !this.invulnerable) { this.hp = 0; this.dead = true; this.onDeath(info); }
    return d;
  }
  heal(a) { if (!this.dead) this.hp = Math.min(this.maxHp, this.hp + a); }
  onDamaged() {}
  onDeath() {}
  // Bewegung in kleinen Schritten mit Kollision.
  move(dx, dy) {
    const len = Math.hypot(dx, dy), steps = Math.max(1, Math.ceil(len / 0.3));
    for (let i = 0; i < steps; i++) { this.x += dx / steps; this.y += dy / steps; OG.World.resolve(this); }
  }
  knock(jx, jy) { this.vx += jx / this.mass; this.vy += jy / this.mass; }
}
OG.Entity = Entity;

OG.nearest = (x, y, r, filter) => {
  let best = null, bd = r * r;
  for (const e of OG.ents) {
    if (e.dead) continue;
    const d = (e.x - x) ** 2 + (e.y - y) ** 2;
    if (d < bd && filter(e)) { bd = d; best = e; }
  }
  return best;
};

// ---------- Inventar ----------
class Inv {
  constructor(data) { this.items = Object.assign({}, data || {}); }
  count(id) { return this.items[id] || 0; }
  add(id, n) { if (n > 0 && OG.Items[id]) this.items[id] = this.count(id) + n; }
  remove(id, n) { if (this.count(id) < n) return false; this.items[id] -= n; if (this.items[id] <= 0) delete this.items[id]; return true; }
  has(reqs) { return reqs.every(r => this.count(r.id) >= r.n); }
  get mass() { let m = 0; for (const id in this.items) m += (OG.Items[id] ? OG.Items[id].mass : 0) * this.items[id]; return m; }
  list() { return Object.keys(this.items).map(id => ({ id, n: this.items[id] })); }
}
OG.Inv = Inv;

// ---------- Geschosse ----------
class Projectile {
  constructor(owner, x, y, z, vx, vy, opts) {
    Object.assign(this, { owner, x, y, z, vx, vy, vz: 0, r: 0.2, life: 3, pierce: 0, gravity: false, color: '#9fd0ff', hit: new Set(), homing: null, explodeR: 0 }, opts || {});
    OG.projectiles.push(this);
  }
  update(dt) {
    this.life -= dt;
    if (this.life <= 0) return this.finish(false);
    if (this.homing && !this.homing.dead) {
      const want = Math.atan2(this.homing.y - this.y, this.homing.x - this.x), cur = Math.atan2(this.vy, this.vx);
      const na = cur + OG.clamp(OG.angleDiff(cur, want), -4 * dt, 4 * dt), sp = Math.hypot(this.vx, this.vy);
      this.vx = Math.cos(na) * sp; this.vy = Math.sin(na) * sp;
    }
    if (this.gravity) { this.vz -= 9.81 * dt; this.z += this.vz * dt; if (this.z <= 0) return this.finish(true); }
    const steps = Math.max(1, Math.ceil(Math.hypot(this.vx, this.vy) * dt / 0.4));
    for (let s = 0; s < steps; s++) {
      this.x += this.vx * dt / steps; this.y += this.vy * dt / steps;
      if (OG.World.blocked(this.x, this.y, 0.05)) return this.finish(true);
      for (const e of OG.ents) {
        if (e.dead || e === this.owner || this.hit.has(e)) continue;
        if (Math.hypot(e.x - this.x, e.y - this.y) > e.r + this.r) continue;
        if (this.canHit && !this.canHit(e)) continue;
        this.hit.add(e);
        if (this.onHit) this.onHit(e, this);
        if (this.pierce > 0) { this.pierce--; continue; }
        return this.finish(true);
      }
    }
  }
  finish(impact) {
    this.done = true;
    if (this.explodeR > 0 && this.onExplode) this.onExplode(this.x, this.y);
    else if (impact) OG.fx.burst(this.x, this.y, '#ddd', 3);
  }
}
OG.Projectile = Projectile;

// ---------- Spieler ----------
class Player extends Entity {
  constructor(d) {
    super(d.x != null ? d.x : 0, d.y != null ? d.y : 97, 0.4);
    this.faction = 'Player';
    this.charName = d.name || 'Reisender';
    this.race = OG.race(d.race); this.cls = OG.cls(d.classId);
    this.second = d.secondClassId ? OG.cls(d.secondClassId) : null;
    this.hybrid = this.second ? OG.hybrid(this.cls.id, this.second.id) : null;
    if (!this.hybrid) this.second = null;
    this.level = d.level || 1; this.exp = d.exp || 0;
    this.alloc = d.alloc || [0, 0, 0, 0]; this.free = d.free || 0;
    this.taler = d.taler != null ? d.taler : 150;
    this.inv = new Inv(d.inventory); this.storage = new Inv(d.storage);
    this.weapon = d.weapon != null ? d.weapon : this.cls.weapon;
    this.armorItem = d.armor != null ? d.armor : this.cls.armor;
    this.rank = d.rank != null ? d.rank : -1;
    this.permit = !!d.permit; this.demo = !!d.demo;
    this.buffs = []; this.cooldowns = [0, 0, 0];
    this.swing = null; this.attackReady = 0; this.nextCrit = 0; this.bounty = null; this.bountyUntil = 0;
    this.companion = null; this.stamina = 0; this.downed = false; this.faceAimUntil = 0;
    this.refresh();
    this.hp = d.hp > 0 ? Math.min(d.hp, this.maxHp) : this.maxHp;
    this.aether = d.aether >= 0 ? Math.min(d.aether, this.maxAether) : this.maxAether;
    this.stamina = this.maxStamina;
    this.walk = 0;
  }
  statMult() { return this.buffs.reduce((m, b) => m * (b.statMult || 1), 1); }
  stat(i) {
    const c = this.cls, r = this.race, base = [c.str, c.agi, c.vit, c.int][i], rb = [r.str, r.agi, r.vit, r.int][i];
    const g1 = c.g[i], g2 = this.second ? this.second.g[i] : 0;
    const first = Math.min(this.level, OG.C.secondClassLevel) - 1, later = Math.max(0, this.level - OG.C.secondClassLevel);
    const gl = this.second ? (g1 + g2) / 2 : g1;
    return Math.round((base + rb + this.alloc[i] + Math.floor(g1 * first + gl * later)) * this.statMult());
  }
  get str() { return this.stat(0); } get agi() { return this.stat(1); } get vit() { return this.stat(2); } get int() { return this.stat(3); }
  get maxAether() { return 40 + this.int * 6 + this.level * 2; }
  get maxStamina() { return 100 + this.agi * 2; }
  get crit() { return Math.min(0.5, 0.05 + this.agi * 0.003); }
  get carry() { return 30 + this.str * 1.2 + this.race.carry; }
  get bodyMass() { return 72 * this.race.scale; }
  get wpn() { return OG.Items[this.weapon] || OG.FISTS; }
  get arm() { return OG.Items[this.armorItem] || null; }
  get equipMass() { return this.wpn.mass + (this.arm ? this.arm.mass : 0); }
  get carried() { return this.inv.mass + this.equipMass; }
  get encumbrance() { const o = this.carried - this.carry; return o <= 0 ? 1 : OG.clamp(1 - o / this.carry, 0.35, 1); }
  get armorValue() { return (this.arm ? this.arm.armor : 0) + this.vit * 0.2; }
  buffMult(k, def) { return this.buffs.reduce((m, b) => m * (b[k] != null ? b[k] : def), 1); }
  hasBuff(flag) { return this.buffs.some(b => b[flag]); }
  get classTitle() { return this.hybrid ? this.hybrid.name + ' (' + this.cls.name + ' + ' + this.second.name + ')' : this.cls.name; }
  swingTime(w) { return Math.max(0.12, w.swing / (1 + this.str * 0.01) / this.buffMult('atkSpeed', 1)); }
  static expToNext(l) { return Math.round(40 * Math.pow(l, 1.6)); }

  refresh() {
    const f = this.maxHp > 0 ? this.hp / this.maxHp : 1;
    this.maxHp = 100 + this.vit * 10 + this.level * 5;
    this.hp = OG.clamp(f * this.maxHp, this.dead ? 0 : 1, this.maxHp);
    this.armor = this.armorValue; this.mass = this.bodyMass + this.equipMass;
    this.aether = Math.min(this.aether || 0, this.maxAether);
    this.r = 0.4 * this.race.scale;
  }

  gainExp(a) {
    if (this.level >= OG.C.maxLevel || a <= 0) return;
    this.exp += a * (1 + this.race.expBonus + (OG.Secrets ? OG.Secrets.expBonus() : 0));
    while (this.level < OG.C.maxLevel && this.exp >= Player.expToNext(this.level)) {
      this.exp -= Player.expToNext(this.level); this.level++; this.free += OG.C.statPointsPerLevel;
      this.refresh(); this.hp = this.maxHp; this.aether = this.maxAether;
      OG.sys('Stufe ' + this.level + ' erreicht! +' + OG.C.statPointsPerLevel + ' Attributpunkte (Taste C).');
      OG.UI.banner('STUFE ' + this.level, '#7fe0ff');
      if (this.level === OG.C.secondClassLevel && !this.second) OG.sys('Stufe 50: Wähle eine zweite Klasse im Charakterfenster (C).');
      if (this.level === OG.C.emperorLevel) OG.sys('Du hast die Stufe von Kaiser ' + OG.C.emperorName + ' erreicht. Die Säule der Kraft steht.');
    }
    if (this.level >= OG.C.maxLevel) this.exp = 0;
  }
  allocate(i) { if (this.free <= 0) return; this.free--; this.alloc[i]++; this.refresh(); }
  chooseSecond(id) {
    if (this.level < OG.C.secondClassLevel || this.second || id === this.cls.id) return;
    const h = OG.hybrid(this.cls.id, id); if (!h) return;
    this.second = OG.cls(id); this.hybrid = h; this.refresh();
    OG.sys('Neue Klasse: ' + h.name + '! Fähigkeit [R]: ' + h.r.name + '.'); OG.UI.banner(h.name.toUpperCase(), '#f2c14d');
  }
  spend(n) { if (this.taler < n) return false; this.taler -= n; return true; }
  addBuff(b) { this.buffs = this.buffs.filter(x => x.id !== b.id); this.buffs.push(b); this.refresh(); }
  equip(id) {
    const d = OG.Items[id]; if (!d || this.inv.count(id) <= 0) return;
    if (d.cat === 'Waffe') { this.inv.remove(id, 1); if (OG.Items[this.weapon]) this.inv.add(this.weapon, 1); this.weapon = id; }
    else if (d.cat === 'Ruestung') { this.inv.remove(id, 1); if (OG.Items[this.armorItem]) this.inv.add(this.armorItem, 1); this.armorItem = id; }
    this.refresh();
  }
  unequip(weapon) {
    if (weapon && OG.Items[this.weapon]) { this.inv.add(this.weapon, 1); this.weapon = ''; }
    else if (!weapon && OG.Items[this.armorItem]) { this.inv.add(this.armorItem, 1); this.armorItem = ''; }
    this.refresh();
  }
  use(id) {
    const d = OG.Items[id]; if (!d || d.cat !== 'Verbrauch' || this.inv.count(id) <= 0 || this.dead) return false;
    this.inv.remove(id, 1);
    if (d.heal) this.heal(d.heal + this.maxHp * 0.1);
    if (d.aether) this.aether = Math.min(this.maxAether, this.aether + d.aether + this.maxAether * 0.1);
    OG.log(d.name + ' benutzt.'); return true;
  }

  modify(info) {
    const parry = this.buffs.find(b => b.parry);
    if (parry && info.source && info.amount > 0) {
      parry.until = 0; this.buffs = this.buffs.filter(b => b !== parry);
      OG.log('Parade! Angriff abgewehrt — Konter mit dreifacher Kraft.');
      this.counter(info.source); info.amount = 0; return info;
    }
    info.amount *= this.buffMult('dmgTaken', 1);
    if (info.source instanceof OG.Rival && OG.time < info.source.duelGrace) { info.amount = 0; return info; }
    // Ehrenduell (Art. 124): wer unter 15 % fällt, gibt auf — niemand stirbt.
    if (OG.duel && info.source === OG.duel.rival) {
      const eff = Math.max(0, info.amount) * 60 / (60 + Math.max(0, this.armor));
      if (this.hp - eff <= this.maxHp * 0.15) { info.amount = 0; this.hp = Math.max(this.hp, this.maxHp * 0.15); OG.endDuel(false); }
    }
    return info;
  }
  onDamaged(info, d) { if (d > 0) { OG.UI.hit(d / this.maxHp); OG.fx.float(this.x, this.y - 1.5, '-' + Math.round(d), '#ff5a4d'); } }
  onDeath() { this.downedAt = OG.time; this.downed = true; }

  update(dt) {
    const I = OG.Input, blocked = OG.blocked();
    if (this.downed) {
      if (OG.time - this.downedAt > 1.2) { this.downed = false; this.dead = false; OG.onPlayerDowned(); }
      return;
    }
    const regen = (1 + this.int * 0.08) * (1 + this.race.aetherRegen);
    this.aether = Math.min(this.maxAether, this.aether + regen * dt);
    for (let i = this.buffs.length - 1; i >= 0; i--) {
      const b = this.buffs[i];
      if (b.hps) this.heal(b.hps * dt);
      if (OG.time >= b.until) { this.buffs.splice(i, 1); this.refresh(); }
    }
    this.heal(this.vit * 0.03 * dt);
    this.flash = Math.max(0, this.flash - dt * 4);

    // Bewegung
    let mx = 0, my = 0;
    if (!blocked) {
      if (I.down('KeyW') || I.down('ArrowUp')) my -= 1;
      if (I.down('KeyS') || I.down('ArrowDown')) my += 1;
      if (I.down('KeyA') || I.down('ArrowLeft')) mx -= 1;
      if (I.down('KeyD') || I.down('ArrowRight')) mx += 1;
      if (I.touch) { mx += I.stickX; my += I.stickY; }
    }
    const ml = Math.hypot(mx, my);
    if (ml > 1) { mx /= ml; my /= ml; }
    const sprint = ml > 0.1 && !blocked && (I.down('ShiftLeft') || I.down('ShiftRight') || ml > 0.95 && I.touch) && this.stamina > 1;
    if (sprint) this.stamina = Math.max(0, this.stamina - 20 * dt); else this.stamina = Math.min(this.maxStamina, this.stamina + 18 * dt);
    const slow = this.slowUntil > OG.time ? this.slowMult : 1;
    const speed = 4.6 * (1 + this.agi * 0.004) * this.encumbrance * slow * (sprint ? 1.6 : 1);
    this.vx *= Math.max(0, 1 - 6 * dt); this.vy *= Math.max(0, 1 - 6 * dt);
    this.moveX = mx * speed + this.vx; this.moveY = my * speed + this.vy;
    this.move(this.moveX * dt, this.moveY * dt);
    this.walk += Math.hypot(this.moveX, this.moveY) * dt * 2.2;

    // Zielen
    const aim = this.aimPoint();
    if (OG.time < this.faceAimUntil || !I.touch) this.face = Math.atan2(aim.y - this.y, aim.x - this.x);
    else if (ml > 0.1) this.face = Math.atan2(my, mx);

    this.updateCombat(dt, blocked);
  }

  aimPoint() {
    const I = OG.Input;
    if (I.touch) {
      const t = OG.nearest(this.x, this.y, 12, e => this.canHit(e) && OG.isHostileToPlayer(e));
      if (t) return { x: t.x, y: t.y };
      return { x: this.x + Math.cos(this.face) * 6, y: this.y + Math.sin(this.face) * 6 };
    }
    return OG.screenToWorld(I.mouseX, I.mouseY);
  }

  // Bürger, Wachen und den Kaiser trifft man nur, wenn man genau auf sie zielt (kein Verbrechen aus Versehen).
  canHit(e) {
    if (!e || e === this || e.dead || e.faction === 'Ally' || e.faction === 'Player') return false;
    if (e instanceof OG.Npc && e.target !== this && e !== this.aimed) return false;
    // Andere Reisende nur, wenn man sie genau anvisiert, sie angreifen, sie gesetzlos sind oder ein Duell läuft.
    if (e instanceof OG.Rival && !e.outlaw && !e.duel && e.target !== this && e !== this.aimed && e !== this.feud) return false;
    if (OG.duel && e instanceof OG.Rival && e !== OG.duel.rival && !e.outlaw && e.target !== this) return false;
    return true;
  }

  // ---------- Kampf ----------
  updateCombat(dt, blocked) {
    const I = OG.Input;
    if (this.swing) {
      const s = this.swing; s.t += dt;
      if (s.t >= s.dur * 0.55 && !s.done) { s.done = true; this.meleeHit(s); }
      if (s.t >= s.dur) this.swing = null;
    }
    if (blocked) return;
    if ((I.mouseDown || I.touchAttack) && OG.time >= this.attackReady) this.basicAttack(1);
    if (I.hit('KeyQ')) this.useSkill(0);
    if (I.hit('KeyE')) this.useSkill(1);
    if (I.hit('KeyR')) this.useSkill(2);
    if (I.hit('Digit1')) this.use('heiltrank');
    if (I.hit('Digit2')) this.use('aetherelixier');
    if (I.hit('Digit3')) this.use('gebratenes_fleisch');
  }
  slot(i) { return i === 0 ? this.cls.q : i === 1 ? this.cls.e : (this.hybrid ? this.hybrid.r : null); }
  cdLeft(i) { return Math.max(0, this.cooldowns[i] - OG.time); }

  faceAim(sec) {
    this.faceAimUntil = OG.time + sec; const a = this.aimPoint(); this.face = Math.atan2(a.y - this.y, a.x - this.x);
    this.aimed = OG.Input.touch ? null : OG.nearest(a.x, a.y, 1.1, e => e instanceof OG.Npc || e instanceof OG.Rival);
  }

  basicAttack(mult) {
    const w = this.wpn; this.faceAim(0.5);
    if (w.kind === 'Nahkampf') this.startSwing(mult, false, 0);
    else if (w.kind === 'Stab') { this.castBolt(0.25, mult, 0, 0, false); this.attackReady = OG.time + 0.55 / (1 + this.agi * 0.006) / this.buffMult('atkSpeed', 1); this.castAnim = OG.time; }
    else { this.shootArrow(1, mult, 0, 0); this.attackReady = OG.time + 0.65 / (1 + this.agi * 0.01) / this.buffMult('atkSpeed', 1); this.castAnim = OG.time; }
  }
  startSwing(mult, full, extra) {
    const dur = this.swingTime(this.wpn);
    const approach = Math.cos(this.face) * this.moveX + Math.sin(this.face) * this.moveY;
    this.swing = { t: 0, dur, mult, full, extra, approach, done: false };
    this.attackReady = OG.time + dur + 0.12;
  }
  meleeHit(s) {
    const w = this.wpn, reach = w.length + 0.9 + s.extra;
    const cx = s.full ? this.x : this.x + Math.cos(this.face) * w.length * 0.5, cy = s.full ? this.y : this.y + Math.sin(this.face) * w.length * 0.5;
    OG.fx.slash(this.x, this.y, this.face, s.full ? Math.PI * 2 : 2.4, reach + 0.3, s.full ? '#ffe8a0' : '#ffffff');
    for (const e of this.gather(cx, cy, reach)) {
      const ang = Math.atan2(e.y - this.y, e.x - this.x);
      if (!s.full && Math.abs(OG.angleDiff(this.face, ang)) > 1.3) continue;
      const st = OG.Phys.melee(w.mass, w.length, s.dur, s.approach, s.mult * this.buffMult('dmg', 1));
      this.hitTarget(e, st, Math.cos(ang), Math.sin(ang), false);
    }
    OG.shake(0.15 + w.mass * 0.05);
  }
  gather(x, y, r) { return OG.ents.filter(e => this.canHit(e) && Math.hypot(e.x - x, e.y - y) <= r + e.r); }

  hitTarget(t, st, dx, dy, magic) {
    let crit = Math.random() < this.crit;
    if (this.nextCrit > OG.time) { crit = true; this.nextCrit = 0; }
    let dmg = st.dmg * (crit ? 1.7 : 1);
    if (this.bounty === t && OG.time < this.bountyUntil) dmg *= 1.4;
    const dealt = t.damage({ amount: dmg, source: this, magic, crit });
    OG.log(st.f + ' → ' + Math.round(dealt) + ' Schaden' + (crit ? ' (kritisch)' : ''));
    OG.fx.float(t.x, t.y - 1.2, String(Math.round(dealt)), crit ? '#ffd24d' : '#ffffff', crit);
    if (t.knock) t.knock(dx * st.J * OG.C.knockbackScale, dy * st.J * OG.C.knockbackScale);
    if (t instanceof OG.Npc) t.attackedByPlayer();
    if (t instanceof OG.Creature && this.hasBuff('poison')) t.poison(4 + this.int * 0.4 + this.agi * 0.3, 4);
    const steal = this.buffs.reduce((s, b) => s + (b.lifesteal || 0), 0);
    if (steal > 0) this.heal(dealt * steal);
    if (!magic && !this.inRune && this.hasBuff('rune')) {
      this.inRune = true; this.explosion(t.x, t.y, 2.5, OG.Phys.spell(0.6, this.aetherAccel(), 0.8), '#80bfff'); this.inRune = false;
    }
  }
  aetherAccel() { const w = this.wpn; return 120 * (1 + this.int * 0.03) * (w.kind === 'Stab' ? w.focus : 0.7); }
  explosion(x, y, r, st, color) {
    OG.fx.ring(x, y, r, color, 0.4); OG.fx.burst(x, y, color, 10);
    for (const e of this.gather(x, y, r)) { const a = Math.atan2(e.y - y, e.x - x); this.hitTarget(e, st, Math.cos(a), Math.sin(a), true); }
  }
  aimDir() { const a = this.aimPoint(); const d = Math.atan2(a.y - this.y, a.x - this.x); return d; }

  castBolt(m, mult, explodeR, pierce, homing, spreadA) {
    const st = OG.Phys.spell(m, this.aetherAccel(), mult * this.buffMult('dmg', 1));
    const a = this.aimDir() + (spreadA || 0), sx = this.x + Math.cos(a) * 0.6, sy = this.y + Math.sin(a) * 0.6;
    const target = homing ? OG.nearest(this.aimPoint().x, this.aimPoint().y, 10, e => this.canHit(e)) : null;
    const p = new OG.Projectile(this, sx, sy, 1.4, Math.cos(a) * 24, Math.sin(a) * 24, { r: 0.18 + m * 0.2, color: '#78bfff', pierce, explodeR, homing: target, glow: true });
    p.canHit = e => this.canHit(e);
    p.onHit = (e, pr) => { const n = Math.hypot(pr.vx, pr.vy); this.hitTarget(e, st, pr.vx / n, pr.vy / n, true); };
    p.onExplode = (x, y) => this.explosion(x, y, explodeR, st, '#78bfff');
  }
  // Pfeil mit echter Flugbahn: Start in 1,4 m Höhe, fällt mit g = 9,81 m/s².
  shootArrow(forceMult, mult, spreadA, pierce) {
    const w = this.wpn;
    const F = (w.draw || 60) * (1 + this.str * 0.005) * forceMult, m = 0.05;
    const v = Math.min(70, OG.Phys.arrowSpeed(F, 0.6, m));
    const a = this.aimDir() + (spreadA || 0), aim = this.aimPoint();
    const dist = Math.hypot(aim.x - this.x, aim.y - this.y);
    // Abschusswinkel so, dass der Pfeil das Ziel erreicht (flache Bahn).
    const tFlight = Math.max(0.05, dist / v), vz = Math.min(8, Math.max(0, (9.81 * tFlight * tFlight / 2 - 1.2) / tFlight));
    const p = new OG.Projectile(this, this.x + Math.cos(a) * 0.5, this.y + Math.sin(a) * 0.5, 1.4, Math.cos(a) * v, Math.sin(a) * v, { r: 0.12, color: '#e6d9b3', gravity: true, pierce, arrow: true });
    p.vz = vz;
    p.canHit = e => this.canHit(e) && p.z < (e.height || 1.8) + 0.3;
    const wm = OG.Weather.bowMult();
    p.onHit = (e, pr) => { const sp = Math.hypot(pr.vx, pr.vy, pr.vz); const st = OG.Phys.arrowImpact(m, sp, mult * this.buffMult('dmg', 1) * wm); this.hitTarget(e, st, pr.vx / sp, pr.vy / sp, false); };
    OG.log('Bogen: a = F/m = ' + OG.fmt(F) + ' N / 0,05 kg = ' + OG.fmt(F / m) + ' m/s², v = ' + OG.fmt(v) + ' m/s');
  }

  useSkill(i) {
    const a = this.slot(i); if (!a) return;
    if (OG.time < this.cooldowns[i]) { OG.log(a.name + ' lädt noch (' + OG.fmt(this.cdLeft(i), 1) + ' s).'); return; }
    if (a.kind === 'BloodLance') {
      const cost = this.maxHp * 0.15; if (this.hp <= cost + 1) { OG.log('Zu wenig Leben für die Blutlanze.'); return; }
      this.hp -= cost;
    } else {
      if (this.aether < a.cost) { OG.log('Nicht genug Äther für ' + a.name + '.'); return; }
      this.aether -= a.cost;
    }
    this.cooldowns[i] = OG.time + a.cd; this.faceAim(0.5);
    this.execute(a);
  }

  execute(a) {
    const w = this.wpn, c = this.cls.color, T = OG.time;
    switch (a.kind) {
      case 'PowerStrike':
        if (w.kind === 'Nahkampf') this.startSwing(a.power, false, 0.3);
        else if (w.kind === 'Bogen') this.shootArrow(1.5, a.power, 0, 1);
        else this.castBolt(0.5, a.power, 1.5, 0, false);
        break;
      case 'Heal': this.addBuff({ id: a.id, name: a.name, until: T + a.dur, hps: this.maxHp * a.power / a.dur }); OG.fx.ring(this.x, this.y, 1.5, '#66ff80', 0.6); break;
      case 'Spin': this.startSwing(a.power, true, a.radius - w.length); OG.fx.ring(this.x, this.y, a.radius, c, 0.35); break;
      case 'Charge': {
        // Δv = 20 m/s in 0,35 s ⇒ a ≈ 57 m/s² zusätzlich zum Schwung.
        this.vx += Math.cos(this.face) * 20; this.vy += Math.sin(this.face) * 20;
        const cx = this.x + Math.cos(this.face) * a.radius * 0.5, cy = this.y + Math.sin(this.face) * a.radius * 0.5;
        for (const e of this.gather(cx, cy, a.radius * 0.6)) {
          const st = OG.Phys.melee(w.mass + 2, Math.max(0.6, w.length), a.dur, 20, a.power * this.buffMult('dmg', 1));
          this.hitTarget(e, st, Math.cos(this.face), Math.sin(this.face), false);
        }
        OG.fx.burst(this.x, this.y, c, 12); break;
      }
      case 'Bolt': this.castBolt(0.8, a.power, a.radius, 0, false); break;
      case 'Nova': {
        const st = OG.Phys.spell(1, this.aetherAccel(), a.power * this.buffMult('dmg', 1));
        OG.fx.ring(this.x, this.y, a.radius, '#99e6ff', 0.5);
        for (const e of this.gather(this.x, this.y, a.radius)) { const an = Math.atan2(e.y - this.y, e.x - this.x); this.hitTarget(e, st, Math.cos(an), Math.sin(an), true); if (e.slow) e.slow(0.5, a.dur); }
        break;
      }
      case 'Volley': for (let k = -2; k <= 2; k++) this.shootArrow(0.8, a.power, k * 0.1, 0); break;
      case 'Pierce': this.shootArrow(a.power, 1, 0, 5); break;
      case 'Blink': {
        OG.fx.burst(this.x, this.y, '#4d3366', 10);
        for (let d = a.radius; d > 0; d -= 0.5) {
          const nx = this.x + Math.cos(this.face) * d, ny = this.y + Math.sin(this.face) * d;
          if (!OG.World.blocked(nx, ny, this.r)) { this.x = nx; this.y = ny; break; }
        }
        this.nextCrit = T + a.dur; OG.fx.burst(this.x, this.y, '#4d3366', 10); break;
      }
      case 'PoisonBuff': this.addBuff({ id: a.id, name: a.name, until: T + a.dur, poison: true }); break;
      case 'Slam': {
        const st = OG.Phys.body(this.mass, 3 + this.str * 0.05, a.power * this.buffMult('dmg', 1));
        OG.fx.ring(this.x, this.y, a.radius, '#cc9a4d', 0.5); OG.fx.burst(this.x, this.y, '#806640', 16); OG.shake(0.7);
        for (const e of this.gather(this.x, this.y, a.radius)) { const an = Math.atan2(e.y - this.y, e.x - this.x); this.hitTarget(e, st, Math.cos(an), Math.sin(an), false); }
        break;
      }
      case 'Rage': this.addBuff({ id: a.id, name: a.name, until: T + a.dur, atkSpeed: 1.35, lifesteal: 0.15, dmgTaken: 1.2 }); OG.fx.ring(this.x, this.y, 2, '#e63326', 0.5); break;
      case 'RuneBuff': this.addBuff({ id: a.id, name: a.name, until: T + a.dur, rune: true }); break;
      case 'ShieldWall': this.addBuff({ id: a.id, name: a.name, until: T + a.dur, dmgTaken: a.power, noKnock: true }); OG.fx.ring(this.x, this.y, 1.6, '#ccd9ff', a.dur); break;
      case 'Counter': this.addBuff({ id: a.id, name: a.name, until: T + a.dur, parry: true }); break;
      case 'WarCry':
        OG.fx.ring(this.x, this.y, a.radius, '#ffb34d', 0.6);
        // F = 1200 N über Δt = 0,1 s ⇒ J = 120 N·s, Δv = J / m
        for (const e of this.gather(this.x, this.y, a.radius)) { const an = Math.atan2(e.y - this.y, e.x - this.x); if (e.knock) e.knock(Math.cos(an) * 240, Math.sin(an) * 240); }
        this.addBuff({ id: a.id, name: a.name, until: T + a.dur, dmg: a.power }); break;
      case 'HomingArrow': for (let k = -1; k <= 1; k++) this.castBolt(0.4, a.power, 0, 0, true, k * 0.25); break;
      case 'RaiseDead': {
        let n = 0;
        for (const e of OG.ents) {
          if (n >= 2) break;
          if (!(e instanceof OG.Creature) || !e.corpse || e.def.diet === 'Konstrukt' || Math.hypot(e.x - this.x, e.y - this.y) > a.radius) continue;
          e.raise(a.dur); n++;
        }
        OG.log(n > 0 ? n + ' Gefallene erheben sich für ' + a.dur + ' s.' : 'Keine Leichen in der Nähe.'); break;
      }
      case 'BloodLance': this.castBolt(1.2, a.power, 0, 6, false); break;
      case 'Bounty': {
        const ap = this.aimPoint(), t = OG.nearest(ap.x, ap.y, 4, e => this.canHit(e));
        if (t) { this.bounty = t; this.bountyUntil = T + a.dur; OG.log('Kopfgeld auf ' + t.name + ' ausgesetzt.'); } else OG.log('Kein Ziel anvisiert.');
        break;
      }
      case 'Tame': {
        const ap = this.aimPoint(), t = OG.nearest(ap.x, ap.y, 4, e => e instanceof OG.Creature && !e.corpse && e.def.ecosystem && Math.hypot(e.x - this.x, e.y - this.y) < a.radius);
        if (t && t.frac < 0.4) t.tame(); else OG.log('Zähmen gelingt nur bei geschwächten Wildtieren (< 40 % LP) in 6 m Nähe.');
        break;
      }
      case 'BladeDance':
        for (let k = 0; k < 6; k++) setTimeout(() => {
          if (this.dead) return;
          OG.fx.ring(this.x, this.y, a.radius, '#ccccdd', 0.2);
          for (const e of this.gather(this.x, this.y, a.radius)) {
            const an = Math.atan2(e.y - this.y, e.x - this.x);
            this.hitTarget(e, OG.Phys.melee(w.mass, Math.max(0.5, w.length), a.dur / 6, 0, a.power * this.buffMult('dmg', 1)), Math.cos(an), Math.sin(an), false);
          }
        }, k * a.dur / 6 * 1000);
        break;
      case 'Pioneer': this.addBuff({ id: a.id, name: a.name, until: T + a.dur, statMult: 1 + a.power, gather: 2 }); break;
    }
  }
  counter(src) {
    if (!src || src.dead) return;
    const w = this.wpn, an = Math.atan2(src.y - this.y, src.x - this.x);
    this.hitTarget(src, OG.Phys.melee(w.mass, Math.max(0.6, w.length), this.swingTime(w), 0, 3 * this.buffMult('dmg', 1)), Math.cos(an), Math.sin(an), false);
  }
  knock(jx, jy) { if (!this.hasBuff('noKnock')) super.knock(jx, jy); }
  slow(m, s) { this.slowMult = m; this.slowUntil = OG.time + s; }

  draw(ctx) {
    const s = this.race.scale, swing = this.swing ? this.swing.t / this.swing.dur : -1;
    OG.drawHumanoid(ctx, this.x, this.y, this.face, s, this.cls.color, '#2e2a26', this.race.skin, this.walk, this.flash, this.wpn, swing, this.castAnim && OG.time - this.castAnim < 0.2);
    if (this.buffs.some(b => b.noKnock || b.parry)) { ctx.strokeStyle = 'rgba(200,215,255,0.8)'; ctx.lineWidth = 0.06; ctx.beginPath(); ctx.arc(this.x, this.y, 0.8, 0, Math.PI * 2); ctx.stroke(); }
  }
}
OG.Player = Player;

// Figur von oben: Schultern, Kopf, Arme und Waffe in Blickrichtung.
OG.drawHumanoid = (ctx, x, y, face, s, cloth, accent, skin, walk, flash, wpn, swing, casting, extra) => {
  ctx.save();
  ctx.translate(x, y);
  ctx.fillStyle = 'rgba(0,0,0,0.28)';
  ctx.beginPath(); ctx.ellipse(0.12, 0.18, 0.55 * s, 0.38 * s, 0, 0, Math.PI * 2); ctx.fill();
  ctx.rotate(face);
  const step = Math.sin(walk * 3) * 0.16 * s;
  ctx.fillStyle = accent;
  ctx.beginPath(); ctx.ellipse(step, -0.15 * s, 0.17 * s, 0.11 * s, 0, 0, Math.PI * 2); ctx.fill();
  ctx.beginPath(); ctx.ellipse(-step, 0.15 * s, 0.17 * s, 0.11 * s, 0, 0, Math.PI * 2); ctx.fill();
  if (extra && extra.robe) { ctx.fillStyle = extra.robe; ctx.beginPath(); ctx.ellipse(-0.05, 0, 0.42 * s, 0.5 * s, 0, 0, Math.PI * 2); ctx.fill(); }
  ctx.fillStyle = flash > 0.05 ? '#ffffff' : cloth;
  ctx.beginPath(); ctx.ellipse(0, 0, 0.26 * s, 0.42 * s, 0, 0, Math.PI * 2); ctx.fill();
  ctx.strokeStyle = 'rgba(0,0,0,0.35)'; ctx.lineWidth = 0.04; ctx.stroke();
  // Waffe
  let wa = 0.35;
  if (swing >= 0) wa = swing < 0.4 ? OG.lerp(0.35, -1.2, swing / 0.4) : OG.lerp(-1.2, 1.3, (swing - 0.4) / 0.6);
  if (wpn && wpn.id !== 'faeuste') {
    ctx.save();
    ctx.translate(0.05, 0.32 * s); ctx.rotate(wa);
    if (wpn.kind === 'Nahkampf') {
      const L = Math.max(0.35, wpn.length), heavy = wpn.mass > 2.5;
      ctx.fillStyle = '#6b4a2e'; ctx.fillRect(0, -0.04, 0.22, 0.08);
      ctx.fillStyle = '#d0d4da'; ctx.fillRect(0.2, heavy ? -0.08 : -0.04, L, heavy ? 0.16 : 0.08);
      if (wpn.id === 'kriegsaxt') { ctx.fillStyle = '#b8bcc2'; ctx.beginPath(); ctx.moveTo(L - 0.05, -0.05); ctx.lineTo(L + 0.18, -0.3); ctx.lineTo(L + 0.22, 0.05); ctx.fill(); }
    } else if (wpn.kind === 'Stab') {
      ctx.fillStyle = '#6b4a2e'; ctx.fillRect(-0.2, -0.04, 1.5, 0.08);
      ctx.fillStyle = casting ? '#ffffff' : '#7ac2ff'; ctx.beginPath(); ctx.arc(1.32, 0, 0.13, 0, Math.PI * 2); ctx.fill();
    } else {
      ctx.strokeStyle = '#7a5530'; ctx.lineWidth = 0.07;
      ctx.beginPath(); ctx.arc(-0.25, 0, 0.55, -1.1, 1.1); ctx.stroke();
      ctx.strokeStyle = '#eee'; ctx.lineWidth = 0.02; ctx.beginPath(); ctx.moveTo(0.0, -0.49); ctx.lineTo(casting ? -0.2 : 0.0, 0); ctx.lineTo(0.0, 0.49); ctx.stroke();
    }
    ctx.restore();
  }
  ctx.fillStyle = skin;
  ctx.beginPath(); ctx.arc(0.04, 0, 0.2 * s, 0, Math.PI * 2); ctx.fill();
  if (extra && extra.crown) {
    ctx.fillStyle = '#f2c14d';
    for (let i = 0; i < 8; i++) { const a = i * Math.PI / 4; ctx.beginPath(); ctx.arc(0.04 + Math.cos(a) * 0.2 * s, Math.sin(a) * 0.2 * s, 0.05, 0, Math.PI * 2); ctx.fill(); }
  } else if (extra && extra.helmet) {
    ctx.fillStyle = '#b3b8bf'; ctx.beginPath(); ctx.arc(0.04, 0, 0.21 * s, -1.9, 1.9); ctx.fill();
  } else if (extra && extra.hood) {
    ctx.fillStyle = extra.hood; ctx.beginPath(); ctx.arc(-0.02, 0, 0.23 * s, 1.2, -1.2); ctx.fill();
  } else {
    ctx.fillStyle = 'rgba(60,40,25,0.85)'; ctx.beginPath(); ctx.arc(-0.03, 0, 0.19 * s, 1.5, -1.5); ctx.fill();
  }
  ctx.restore();
};
