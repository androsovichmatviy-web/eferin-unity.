// Omni Genesis 2D — Lebewesen (Kräfte: F = m · a) und Bürger des Reiches.
'use strict';

OG.isHostileToPlayer = e => {
  if (e instanceof OG.Creature) return e.hostile;
  if (e instanceof OG.Npc) return e.target === OG.player;
  return false;
};

// Ort, mit dem man per [F] interagiert (Bett, Truhe, Amboss, Tore, Siegel, Spuren …).
class Spot {
  constructor(x, y, radius, prompt, interact, can) {
    Object.assign(this, { x, y, radius, prompt, interact, can: can || (() => true), on: true });
    OG.interactables.push(this);
  }
  remove() { this.on = false; OG.interactables = OG.interactables.filter(s => s !== this); }
}
OG.Spot = Spot;

// ---------- Lebewesen ----------
class Creature extends OG.Entity {
  constructor(def, x, y, o) {
    o = o || {};
    super(x, y, 0.45 * def.size);
    this.def = def; this.name = def.name; this.mass = def.mass; this.armor = def.armor;
    this.maxHp = this.hp = def.hp * (o.hpMult || 1);
    this.faction = o.faction || (def.diet === 'Leere' || def.diet === 'Kult' ? 'Void' : def.diet === 'Konstrukt' ? 'Construct' : 'Wild');
    this.age = o.age != null ? o.age : 100; this.hunger = o.hunger || 0; this.starving = 0;
    this.managed = !def.ecosystem || !!o.managed;
    this.dmgMult = o.dmgMult || 1; this.expMult = o.expMult != null ? o.expMult : 1;
    this.questTag = null; this.summonInterval = 0; this.nextSummon = 0;
    this.home = { x, y }; this.state = 'Wander'; this.target = null; this.thinkT = Math.random() * 0.3; this.until = 0; this.wp = { x, y };
    this.corpse = false; this.tamed = false; this.raider = false; this.undeadUntil = 0;
    this.slowMult = 1; this.slowUntil = 0; this.poisonDps = 0; this.poisonUntil = 0; this.poisonTick = 0;
    this.height = def.shape === 'tier' || def.shape === 'kriecher' ? 1.1 * def.size : 1.9 * def.size;
    this.walk = Math.random() * 10; this.face = Math.random() * 6.28;
    this.size = 1;
    this.applyGrowth();
    OG.ents.push(this);
    if (def.ecosystem && !this.managed) OG.Eco.register(this);
  }
  get juvenile() { return this.def.ecosystem && !this.managed && this.age < 24; }
  get isAlly() { return this.faction === 'Ally'; }
  get hostile() {
    if (this.corpse || this.isAlly) return false;
    const d = this.def.diet;
    return this.def.temper !== 'Scheu' || d === 'Leere' || d === 'Konstrukt' || d === 'Kult';
  }
  applyGrowth() { this.size = this.juvenile ? OG.lerp(0.6, 1, this.age / 24) : 1; this.r = 0.45 * this.def.size * this.size; }
  ageHours(h) { const j = this.juvenile; this.age += h; if (j) this.applyGrowth(); }

  // Leiche ausweiden
  prompt() { return 'Ausweiden: ' + this.def.name; }
  canInteract() { return this.corpse && !this.managed && this.def.drops.length > 0; }
  interact(p) {
    for (const [id, n0] of this.def.drops) {
      const n = this.juvenile ? n0 - 1 : n0;
      if (n > 0) { p.inv.add(id, n); OG.log('+' + n + ' ' + OG.itemName(id)); }
    }
    this.remove();
  }
  remove() { this.removed = true; if (this.def.ecosystem) OG.Eco.unregister(this); }

  onDamaged(info) {
    const src = info.source;
    if (!src || src === this) return;
    if (this.def.temper === 'Scheu' && !this.isAlly) { this.fleeFrom = { x: src.x, y: src.y }; this.set('Flee', 4); }
    else if (!this.isAlly || src.faction !== 'Player') { this.target = src; if (this.state !== 'Windup') this.set('Chase', 12); }
  }

  onDeath(info) {
    this.corpse = true; this.state = 'Dead'; this.vx = this.vy = 0;
    this.deathMinute = OG.Clock.minutes;
    if (this.def.ecosystem && !this.managed) OG.Eco.unregister(this);
    if (this.tamed && OG.player.companion === this) OG.player.companion = null;
    const k = info.source, P = OG.player;
    const byPlayer = !!k && (k.faction === 'Player' || k.faction === 'Ally');
    if (byPlayer) {
      P.gainExp(this.def.exp * this.expMult);
      if (P.bounty === this && OG.time < P.bountyUntil) {
        const b = Math.round(this.def.exp * this.expMult * 2);
        if (OG.Treasury.pay(b, 'Kopfgelder')) P.taler += b;
        OG.sys('Kopfgeld kassiert: ' + b + ' Taler aus der Kaiserlichen Kasse.');
      }
      if (this.managed && (this.def.diet === 'Leere' || this.def.diet === 'Kult')) {
        for (const [id, n] of this.def.drops) if (n > 0) { P.inv.add(id, n); OG.log('+' + n + ' ' + OG.itemName(id)); }
      }
    }
    if (k && this.hostile !== undefined && this.def.temper !== 'Scheu') OG.Residents.onHostileKilled(this.x, this.y, this.def, byPlayer);
    if (this.def.diet === 'Leere') OG.Rift.onVoidKilled(this, byPlayer);
    if (k instanceof Creature && k.def.diet === 'Fleischfresser') k.feed();
    if (this.questTag || byPlayer) OG.QuestSys.onKill(this.def.id);
    if (this.onDeathCb) this.onDeathCb(this);
    if (this.managed || this.undeadUntil > 0) { OG.fx.burst(this.x, this.y, this.def.color, 8); this.removeAt = OG.time + 1.5; }
  }
  feed() { this.hunger = 0; this.starving = 0; this.target = null; this.set('Idle', 6); }

  raise(sec) {
    if (!this.corpse) return;
    this.corpse = false; this.dead = false; this.managed = true; this.undeadUntil = OG.time + sec;
    this.faction = 'Ally'; this.hp = this.maxHp * 0.6; this.tint = '#593373'; this.state = 'Idle';
    OG.fx.burst(this.x, this.y, '#8033b3', 12);
  }
  tame(silent) {
    if (this.tamed || this.corpse) return;
    const P = OG.player;
    if (P.companion && P.companion !== this) P.companion.release();
    if (this.def.ecosystem && !this.managed) OG.Eco.unregister(this);
    this.managed = true; this.tamed = true; this.faction = 'Ally'; this.hp = this.maxHp; this.target = null;
    P.companion = this;
    if (!silent) OG.sys(this.def.name + ' wurde gezähmt und folgt dir nun.');
  }
  release() {
    this.tamed = false; this.faction = 'Wild'; this.managed = !this.def.ecosystem;
    if (this.def.ecosystem) OG.Eco.register(this);
    this.home = { x: this.x, y: this.y };
  }
  slow(m, s) { this.slowMult = m; this.slowUntil = OG.time + s; }
  poison(dps, s) { this.poisonDps = Math.max(this.poisonDps, dps); this.poisonUntil = OG.time + s; }

  set(s, d) { this.state = s; this.until = OG.time + d; }

  aggroRange() {
    let r = this.def.aggro * OG.Weather.aggroMult();
    if (this.def.nocturnal && OG.Clock.isNight()) r *= 1.5;
    return r;
  }
  isEnemy(h) {
    if (!h || h.dead || h === this) return false;
    const me = this.faction, o = h.faction;
    if (me === 'Ally') return o === 'Void' || o === 'Construct' || (h instanceof Creature && h.target === OG.player);
    if (me === 'Void' && this.def.diet === 'Kult') return o === 'Player' || o === 'Ally' || o === 'Citizen' || o === 'Guard';
    if (me === 'Void') return o !== 'Void' && o !== 'Imperial';
    if (me === 'Construct') return o === 'Player' || o === 'Ally';
    return o === 'Player' || o === 'Ally';
  }

  update(dt) {
    if (this.corpse) return;
    this.flash = Math.max(0, this.flash - dt * 5);
    if (OG.time < this.poisonUntil) {
      this.poisonTick -= dt;
      if (this.poisonTick <= 0) {
        this.poisonTick = 0.5;
        const d = this.damage({ amount: this.poisonDps * 0.5, source: OG.player });
        OG.fx.float(this.x, this.y - 1, String(Math.round(d)), '#80e64d');
        if (this.corpse) return;
      }
    }
    if (this.undeadUntil > 0 && OG.time > this.undeadUntil) { OG.fx.burst(this.x, this.y, '#664080', 10); this.removed = true; return; }
    const P = OG.player;
    if (Math.hypot(P.x - this.x, P.y - this.y) > 140 && !this.tamed) { this.vx = this.vy = 0; return; }
    this.thinkT -= dt;
    if (this.thinkT <= 0) { this.thinkT = 0.25 + Math.random() * 0.1; this.think(); }
    this.act(dt);
  }

  think() {
    const P = OG.player, night = OG.Clock.isNight(), d = this.def;
    if (this.target && (this.target.dead || this.target.removed)) this.target = null;
    if (this.isAlly) {
      if (!this.target) this.target = OG.nearest(P.x, P.y, 16, e => this.isEnemy(e));
      if (!this.target) { const dd = Math.hypot(P.x - this.x, P.y - this.y); this.state = dd > 4 ? 'Home' : 'Idle'; this.home = { x: P.x, y: P.y }; }
      else if (this.state !== 'Windup' && this.state !== 'Recover') this.state = 'Chase';
      return;
    }
    if (d.diet === 'Leere' || d.diet === 'Konstrukt' || d.diet === 'Kult') {
      if (!this.target) this.target = OG.nearest(this.x, this.y, this.aggroRange(), e => this.isEnemy(e));
      if (this.target && this.summonInterval > 0 && OG.time >= this.nextSummon) { this.nextSummon = OG.time + this.summonInterval; this.summon(); }
      if (this.target) { if (this.state !== 'Windup' && this.state !== 'Recover') this.state = 'Chase'; }
      else if (this.raider) { this.home = { x: OG.Places.village.x, y: OG.Places.village.y }; this.state = 'Home'; }
      else if (this.state !== 'Wander' && this.state !== 'Idle') this.state = 'Wander';
      return;
    }
    if (d.temper === 'Scheu') {
      if (this.state === 'Flee' && OG.time < this.until) return;
      const threat = OG.nearest(this.x, this.y, 12 * OG.Weather.aggroMult(), e => e.faction === 'Player' || e.faction === 'Ally' || e.faction === 'Void' || (e instanceof Creature && e.def.diet === 'Fleischfresser'));
      if (threat) { this.fleeFrom = { x: threat.x, y: threat.y }; this.set('Flee', 3); return; }
      if (night) { this.state = 'Sleep'; return; }
      if (OG.Weather.isStorm()) { this.state = 'Idle'; return; }
      if (this.state === 'Sleep' || this.state === 'Flee') this.state = 'Wander';
      return;
    }
    if (Math.hypot(this.x - this.home.x, this.y - this.home.y) > d.habR * 0.9 && !this.target) { this.state = 'Home'; return; }
    if (d.diet === 'Fleischfresser' && this.hunger > 0.6 && !this.target)
      this.target = OG.nearest(this.x, this.y, 40, e => e instanceof Creature && e.def.diet === 'Pflanzenfresser' && e.def.temper === 'Scheu' && !e.corpse);
    const aggressive = d.temper === 'Feindselig' && (this.hunger > 0.5 || night);
    if (!this.target && (aggressive || d.temper === 'Territorial'))
      this.target = OG.nearest(this.x, this.y, d.temper === 'Territorial' ? d.aggro : this.aggroRange(), e => this.isEnemy(e));
    if (this.target) {
      if (d.temper === 'Territorial' && Math.hypot(this.target.x - this.home.x, this.target.y - this.home.y) > d.habR * 1.2) { this.target = null; this.state = 'Home'; return; }
      if (this.state !== 'Windup' && this.state !== 'Recover') this.state = 'Chase';
    } else if (this.state === 'Chase' || this.state === 'Sleep') this.state = 'Wander';
    if (night && d.temper === 'Territorial' && !this.target) this.state = 'Sleep';
  }

  // Lenkkraft: F = m · (v_soll − v) / τ  ⇒  a = F / m (gedeckelt auf 30 m/s²)
  steer(dx, dy, speed, dt) {
    const wx = dx * speed, wy = dy * speed;
    let ax = (wx - this.vx) / 0.2, ay = (wy - this.vy) / 0.2;
    const am = Math.hypot(ax, ay);
    if (am > 30) { ax *= 30 / am; ay *= 30 / am; }
    this.vx += ax * dt; this.vy += ay * dt;
    if (wx * wx + wy * wy > 0.05) this.face = Math.atan2(wy, wx);
  }

  act(dt) {
    const d = this.def;
    const speed = d.speed * (OG.time < this.slowUntil ? this.slowMult : 1) * (this.juvenile ? 0.8 : 1);
    let dx = 0, dy = 0, sp = 0;
    switch (this.state) {
      case 'Idle': case 'Sleep':
        if (this.state === 'Idle' && OG.time > this.until && !this.isAlly) this.state = 'Wander';
        break;
      case 'Wander': {
        if (OG.time > this.until || Math.hypot(this.wp.x - this.x, this.wp.y - this.y) < 1.5) {
          const a = Math.random() * Math.PI * 2, r = Math.random() * Math.min(d.habR, 25);
          this.wp = { x: this.home.x + Math.cos(a) * r, y: this.home.y + Math.sin(a) * r };
          this.set('Wander', OG.rand(5, 11));
        }
        const l = Math.hypot(this.wp.x - this.x, this.wp.y - this.y) || 1;
        dx = (this.wp.x - this.x) / l; dy = (this.wp.y - this.y) / l; sp = speed * 0.35;
        break;
      }
      case 'Home': {
        const l = Math.hypot(this.home.x - this.x, this.home.y - this.y);
        if (l < 3 && !this.raider) this.state = this.isAlly ? 'Idle' : 'Wander';
        else { dx = (this.home.x - this.x) / l; dy = (this.home.y - this.y) / l; sp = speed * (this.isAlly ? 1 : 0.6); }
        break;
      }
      case 'Flee': {
        const l = Math.hypot(this.x - this.fleeFrom.x, this.y - this.fleeFrom.y) || 1;
        dx = (this.x - this.fleeFrom.x) / l; dy = (this.y - this.fleeFrom.y) / l; sp = speed;
        if (OG.time > this.until) this.state = 'Wander';
        break;
      }
      case 'Chase': {
        const t = this.target;
        if (!t || t.dead) { this.state = this.isAlly ? 'Idle' : 'Wander'; break; }
        const l = Math.hypot(t.x - this.x, t.y - this.y);
        const reach = d.ranged ? d.attackRange : d.attackRange * d.size + 0.4 + t.r;
        if (l <= reach) {
          this.face = Math.atan2(t.y - this.y, t.x - this.x);
          this.set('Windup', d.ranged ? 0.7 : 0.45); this.flash = 0.6;
          if (!d.ranged && (d.size > 1.8 || this.managed)) OG.fx.disc(this.x + Math.cos(this.face) * reach * 0.6, this.y + Math.sin(this.face) * reach * 0.6, reach * 0.7, '#ff4d33', 0.45);
        } else { dx = (t.x - this.x) / l; dy = (t.y - this.y) / l; sp = speed; }
        break;
      }
      case 'Windup':
        if (OG.time >= this.until) { this.strike(); this.set('Recover', OG.rand(0.6, 1)); }
        break;
      case 'Recover':
        if (OG.time >= this.until) this.state = this.target ? 'Chase' : 'Wander';
        break;
    }
    this.steer(dx, dy, sp, dt);
    this.move(this.vx * dt, this.vy * dt);
    this.walk += Math.hypot(this.vx, this.vy) * dt * 2;
  }

  // Sprungangriff: Körpermasse × Beschleunigung; der Sprung ist ein Impuls J = m · Δv.
  strike() {
    const t = this.target, d = this.def;
    if (!t || t.dead) return;
    if (d.ranged) return this.cast();
    const l = Math.hypot(t.x - this.x, t.y - this.y) || 1, nx = (t.x - this.x) / l, ny = (t.y - this.y) / l;
    this.vx += nx * 4; this.vy += ny * 4;
    if (l > (d.attackRange * d.size + 0.4 + t.r) * 1.4) return;
    const st = OG.Phys.body(d.mass * this.size, d.accel, this.dmgMult);
    const dealt = t.damage({ amount: st.dmg, source: this });
    if (t === OG.player) OG.log(d.name + ': ' + st.f + ' → ' + Math.round(dealt) + ' Schaden an dir');
    else if (dealt > 0) OG.fx.float(t.x, t.y - 1, String(Math.round(dealt)), '#ff9980');
    if (t.knock) t.knock(nx * st.J * OG.C.knockbackScale, ny * st.J * OG.C.knockbackScale);
  }
  // Äthergeschoss der Kultisten: F = m_Äther · a
  cast() {
    const t = this.target, d = this.def;
    const st = OG.Phys.spell(d.projMass, d.projAccel, this.dmgMult);
    const a = Math.atan2(t.y - this.y, t.x - this.x);
    const p = new OG.Projectile(this, this.x + Math.cos(a) * 0.6, this.y + Math.sin(a) * 0.6, 1.5, Math.cos(a) * 20, Math.sin(a) * 20, { r: 0.22 + d.projMass * 0.3, color: '#b340ff', glow: true });
    p.canHit = e => this.isEnemy(e);
    p.onHit = (e, pr) => {
      const n = Math.hypot(pr.vx, pr.vy), dealt = e.damage({ amount: st.dmg, source: this, magic: true });
      if (e === OG.player) OG.log(d.name + ': ' + st.f + ' → ' + Math.round(dealt) + ' Schaden an dir');
      else if (dealt > 0) OG.fx.float(e.x, e.y - 1, String(Math.round(dealt)), '#cc80ff');
      if (e.knock) e.knock(pr.vx / n * st.J * OG.C.knockbackScale, pr.vy / n * st.J * OG.C.knockbackScale);
    };
  }
  summon() {
    for (let i = 0; i < 2; i++) {
      const v = new Creature(OG.Species.leerenbrut, this.x + OG.rand(-4, 4), this.y + OG.rand(-4, 4), {});
      v.home = { x: this.x, y: this.y };
    }
    OG.fx.ring(this.x, this.y, 5, '#9933e6', 0.8);
    OG.log(this.def.name + ' ruft Leerenbrut aus dem Riss!');
  }
  knock(jx, jy) { this.vx += jx / this.mass; this.vy += jy / this.mass; }

  draw(ctx) {
    const d = this.def, s = d.size * this.size;
    let col = this.tint || d.color;
    if (this.corpse) col = '#6b6660';
    ctx.save();
    ctx.translate(this.x, this.y);
    ctx.fillStyle = 'rgba(0,0,0,0.25)';
    ctx.beginPath(); ctx.ellipse(0.1, 0.15, 0.7 * s, 0.45 * s, 0, 0, Math.PI * 2); ctx.fill();
    if (this.isAlly) { ctx.strokeStyle = this.undeadUntil ? '#b366ff' : '#66e680'; ctx.lineWidth = 0.06; ctx.beginPath(); ctx.arc(0, 0, 0.75 * s, 0, Math.PI * 2); ctx.stroke(); }
    ctx.rotate(this.face);
    const fl = this.flash > 0.05 && !this.corpse;
    const body = fl ? '#ffffff' : col;
    const sleep = this.state === 'Sleep';
    if (this.corpse) ctx.scale(1, 0.75);
    if (d.shape === 'tier') {
      const step = Math.sin(this.walk * 3) * 0.12 * s;
      ctx.fillStyle = OG.shadeCol(col, 0.75);
      for (const [lx, ly, sg] of [[0.35, -0.22, 1], [0.35, 0.22, -1], [-0.35, -0.22, -1], [-0.35, 0.22, 1]]) {
        ctx.beginPath(); ctx.ellipse((lx + step * sg) * s, ly * s, 0.12 * s, 0.07 * s, 0, 0, Math.PI * 2); ctx.fill();
      }
      ctx.fillStyle = body;
      ctx.beginPath(); ctx.ellipse(0, 0, 0.62 * s, 0.3 * s, 0, 0, Math.PI * 2); ctx.fill();
      ctx.beginPath(); ctx.arc(0.62 * s, 0, (sleep ? 0.17 : 0.2) * s, 0, Math.PI * 2); ctx.fill();
      if (d.id === 'graslaeufer') { ctx.strokeStyle = '#7a5c3d'; ctx.lineWidth = 0.05; ctx.beginPath(); ctx.moveTo(0.62 * s, -0.1 * s); ctx.lineTo(0.5 * s, -0.4 * s); ctx.moveTo(0.62 * s, 0.1 * s); ctx.lineTo(0.5 * s, 0.4 * s); ctx.stroke(); }
      if (d.id === 'wildeber') { ctx.fillStyle = '#eee'; ctx.fillRect(0.75 * s, -0.12 * s, 0.14 * s, 0.04 * s); ctx.fillRect(0.75 * s, 0.08 * s, 0.14 * s, 0.04 * s); }
      if (d.id === 'grauwolf') { ctx.fillStyle = OG.shadeCol(col, 0.6); ctx.beginPath(); ctx.moveTo(0.55 * s, -0.12 * s); ctx.lineTo(0.48 * s, -0.28 * s); ctx.lineTo(0.66 * s, -0.16 * s); ctx.fill(); ctx.beginPath(); ctx.moveTo(0.55 * s, 0.12 * s); ctx.lineTo(0.48 * s, 0.28 * s); ctx.lineTo(0.66 * s, 0.16 * s); ctx.fill(); ctx.fillRect(-0.85 * s, -0.04 * s, 0.25 * s, 0.08 * s); }
    } else if (d.shape === 'kriecher') {
      ctx.fillStyle = body; ctx.beginPath(); ctx.ellipse(0, 0, 0.75 * s, 0.55 * s, 0, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = OG.shadeCol(col, 0.7);
      for (let i = 0; i < 5; i++) { ctx.beginPath(); ctx.arc((i - 2) * 0.25 * s, ((i % 2) - 0.5) * 0.3 * s, 0.17 * s, 0, Math.PI * 2); ctx.fill(); }
      ctx.fillStyle = OG.shadeCol(col, 0.9); ctx.beginPath(); ctx.arc(0.72 * s, 0, 0.22 * s, 0, Math.PI * 2); ctx.fill();
    } else if (d.shape === 'leere') {
      const pulse = 1 + Math.sin(OG.time * 4 + this.x) * 0.08;
      ctx.fillStyle = 'rgba(150,60,230,0.25)'; ctx.beginPath(); ctx.arc(0, 0, 0.8 * s * pulse, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = OG.shadeCol(col, 0.6);
      for (let i = 0; i < 6; i++) { const a = i * Math.PI / 3 + OG.time * 0.8; ctx.beginPath(); ctx.moveTo(Math.cos(a) * 0.3 * s, Math.sin(a) * 0.3 * s); ctx.lineTo(Math.cos(a + 0.25) * 0.75 * s * pulse, Math.sin(a + 0.25) * 0.75 * s * pulse); ctx.lineTo(Math.cos(a + 0.5) * 0.3 * s, Math.sin(a + 0.5) * 0.3 * s); ctx.fill(); }
      ctx.fillStyle = body; ctx.beginPath(); ctx.arc(0, 0, 0.42 * s, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = '#e6b3ff'; ctx.beginPath(); ctx.arc(0.2 * s, -0.1 * s, 0.06 * s, 0, Math.PI * 2); ctx.arc(0.2 * s, 0.1 * s, 0.06 * s, 0, Math.PI * 2); ctx.fill();
    } else if (d.shape === 'golem') {
      ctx.fillStyle = OG.shadeCol(col, 0.85); ctx.fillRect(-0.15 * s, -0.75 * s, 0.4 * s, 0.3 * s); ctx.fillRect(-0.15 * s, 0.45 * s, 0.4 * s, 0.3 * s);
      ctx.fillStyle = body; ctx.fillRect(-0.4 * s, -0.48 * s, 0.75 * s, 0.96 * s);
      ctx.fillStyle = OG.shadeCol(col, 1.15); ctx.fillRect(-0.1 * s, -0.25 * s, 0.45 * s, 0.5 * s);
      ctx.fillStyle = d.diet === 'Leere' ? '#c266ff' : '#80e6ff'; ctx.fillRect(0.3 * s, -0.18 * s, 0.06 * s, 0.36 * s);
    } else {
      OG.drawHumanoid(ctx, 0, 0, 0, s, body, '#1f1426', '#cfb39c', this.walk, 0, { id: 'stab', kind: 'Stab', mass: 1 }, -1, this.state === 'Windup', { robe: OG.shadeCol(col, 0.8), hood: OG.shadeCol(col, 0.6) });
      ctx.fillStyle = '#d980ff'; ctx.beginPath(); ctx.arc(0.18 * s, -0.07 * s, 0.035, 0, Math.PI * 2); ctx.arc(0.18 * s, 0.07 * s, 0.035, 0, Math.PI * 2); ctx.fill();
    }
    ctx.restore();
    if (sleep && !this.corpse && Math.floor(OG.time * 1.5 + this.x) % 3 === 0) { ctx.fillStyle = 'rgba(255,255,255,0.7)'; ctx.font = '0.5px sans-serif'; ctx.fillText('z', this.x + 0.4, this.y - 0.6); }
  }
}
OG.Creature = Creature;

OG.shadeCol = (hex, k) => {
  const n = parseInt(hex.slice(1), 16);
  const r = Math.min(255, Math.round(((n >> 16) & 255) * k)), g = Math.min(255, Math.round(((n >> 8) & 255) * k)), b = Math.min(255, Math.round((n & 255) * k));
  return '#' + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
};

// ---------- Bürger, Wachen, Kaiser ----------
class Npc extends OG.Entity {
  constructor(role, name, title, x, y, face, cloth, accent) {
    super(x, y, 0.4);
    Object.assign(this, { role, npcName: name, name, title, home: { x, y }, homeFace: face, face, cloth, accent, route: null, ri: 0, wait: 0, target: null, attackReady: 0, koUntil: 0, lastPlayerHit: -99, walkSpeed: 1.6, resident: null, walk: 0 });
    this.mass = 75;
    switch (role) {
      case 'Kaiser': case 'Richter': this.faction = 'Imperial'; this.invulnerable = true; this.maxHp = this.hp = 99999; break;
      case 'Wache': this.faction = 'Guard'; this.maxHp = this.hp = 900; this.armor = 20; this.walkSpeed = 2; break;
      case 'Ordensmagier': this.faction = 'Guard'; this.maxHp = this.hp = 700; this.armor = 10; break;
      case 'Dorfwache': this.faction = 'Ally'; this.maxHp = this.hp = 500; this.armor = 12; this.walkSpeed = 1.8; break;
      default: this.faction = 'Citizen'; this.maxHp = this.hp = 120;
    }
    OG.ents.push(this);
  }
  get guard() { return this.role === 'Wache' || this.role === 'Dorfwache' || this.role === 'Ordensmagier'; }
  get ko() { return this.koUntil > 0; }
  prompt() { return 'Sprechen: ' + this.npcName + (this.title ? ' (' + this.title + ')' : ''); }
  canInteract() { return !this.ko && !this.target; }
  interact() { OG.UI.openDialogue(this); }
  attackedByPlayer() {
    if (OG.time - this.lastPlayerHit > 8) OG.Law.onPlayerAttackedNpc(this);
    this.lastPlayerHit = OG.time;
    if (this.resident) OG.Residents.onAttacked(this.resident);
  }
  onDeath() { this.koUntil = OG.time + 45; this.target = null; }

  update(dt) {
    this.flash = Math.max(0, this.flash - dt * 4);
    if (this.ko) {
      if (OG.time > this.koUntil) { this.koUntil = 0; this.dead = false; this.hp = this.maxHp * 0.5; }
      return;
    }
    if (this.guard) {
      this.thinkT = (this.thinkT || 0) - dt;
      if (this.thinkT <= 0) { this.thinkT = 0.3; this.guardThink(); }
      if (this.target) { this.fight(dt); return; }
    }
    let goal = this.home, walking = false;
    const nightHome = (this.role === 'Einwohner' || this.role === 'Buerger') && OG.Clock.isNight();
    if (this.route && this.route.length && !nightHome) {
      goal = this.route[this.ri]; walking = true;
      if (Math.hypot(goal.x - this.x, goal.y - this.y) < 0.6) {
        walking = false;
        if (this.wait <= 0) this.wait = OG.time + OG.rand(2, 7);
        else if (OG.time > this.wait) { this.ri = (this.ri + 1 + OG.randInt(0, 1)) % this.route.length; this.wait = 0; }
      }
    }
    this.moveTo(goal, this.walkSpeed, walking || Math.hypot(goal.x - this.x, goal.y - this.y) > 0.6, dt);
  }
  moveTo(goal, speed, go, dt) {
    const dx = goal.x - this.x, dy = goal.y - this.y, l = Math.hypot(dx, dy);
    if (go && l > 0.3) {
      const st = Math.min(speed * dt, l);
      this.move(dx / l * st, dy / l * st);
      this.face = Math.atan2(dy, dx); this.walk += st * 2.2;
    } else if (!this.route) this.face = this.homeFace;
    this.vx *= 0.85; this.vy *= 0.85;
    if (Math.abs(this.vx) + Math.abs(this.vy) > 0.01) this.move(this.vx * dt, this.vy * dt);
  }
  guardThink() {
    const P = OG.player;
    if (this.target && (this.target.dead || this.target.removed)) this.target = null;
    const dP = Math.hypot(P.x - this.x, P.y - this.y);
    if (this.role !== 'Dorfwache' && !P.dead && !P.downed) {
      const wanted = OG.Law.wanted > 0 && dP < 35;
      const trespass = this.role === 'Ordensmagier' && OG.Law.trespassing && dP < 30;
      if (wanted || trespass) { this.target = P; return; }
      if (this.target === P) this.target = null;
    }
    if (this.target) return;
    const cx = this.role === 'Dorfwache' ? OG.Places.village.x : this.x, cy = this.role === 'Dorfwache' ? OG.Places.village.y : this.y;
    this.target = OG.nearest(cx, cy, this.role === 'Dorfwache' ? 30 : 22, e => {
      if (!(e instanceof Creature) || e.corpse || e.isAlly || e.def.diet === 'Konstrukt') return false;
      return e.def.diet === 'Leere' || e.def.diet === 'Kult' || (e.hostile && (e.lastAttacker || e.def.temper === 'Feindselig'));
    });
  }
  fight(dt) {
    const t = this.target;
    if (!t || t.dead) { this.target = null; return; }
    const dx = t.x - this.x, dy = t.y - this.y, l = Math.hypot(dx, dy);
    if (l > 45) { this.target = null; return; }
    this.face = Math.atan2(dy, dx);
    if (this.role === 'Ordensmagier') {
      if (l > 20) this.moveTo(t, 3, true, dt);
      else if (OG.time >= this.attackReady) {
        this.attackReady = OG.time + 2;
        const st = OG.Phys.spell(0.5, 220, 1), a = this.face;
        const p = new OG.Projectile(this, this.x + Math.cos(a) * 0.6, this.y + Math.sin(a) * 0.6, 1.5, Math.cos(a) * 22, Math.sin(a) * 22, { r: 0.28, color: '#b366ff', glow: true });
        p.canHit = e => e === t || (e instanceof Creature && e.faction !== 'Ally');
        p.onHit = (e, pr) => this.deal(e, st, pr.vx / 22, pr.vy / 22);
      }
      return;
    }
    if (l > 1.6 + t.r) this.moveTo(t, 4.2, true, dt);
    else if (OG.time >= this.attackReady) {
      this.attackReady = OG.time + 1.1; this.swingAt = OG.time;
      this.deal(t, OG.Phys.melee(1.6, 1, 0.26, 0, this.role === 'Dorfwache' ? 0.9 : 1), dx / l, dy / l);
    }
  }
  deal(t, st, nx, ny) {
    const dealt = t.damage({ amount: st.dmg, source: this });
    if (t.knock) t.knock(nx * st.J * OG.C.knockbackScale, ny * st.J * OG.C.knockbackScale);
    if (t === OG.player) OG.log(this.npcName + ': ' + st.f + ' → ' + Math.round(dealt) + ' Schaden an dir');
    else if (dealt > 0) OG.fx.float(t.x, t.y - 1, String(Math.round(dealt)), '#ccd9ff');
  }
  knock(jx, jy) { if (!this.invulnerable) { this.vx += jx / this.mass; this.vy += jy / this.mass; } }

  draw(ctx) {
    if (this.ko) {
      ctx.save(); ctx.translate(this.x, this.y); ctx.rotate(this.homeFace + 1.2); ctx.globalAlpha = 0.8;
      OG.drawHumanoid(ctx, 0, 0, 0, 1, '#7a7570', '#3a3530', '#c9a98c', 0, 0, null, -1, false);
      ctx.restore(); return;
    }
    const ex = {};
    if (this.role === 'Kaiser') { ex.robe = '#14142a'; ex.crown = true; }
    else if (this.role === 'Richter') ex.robe = '#4d4d57';
    else if (this.role === 'Ordensmagier') { ex.robe = '#4d2266'; ex.hood = '#3a1a4d'; }
    else if (this.role === 'Wache' || this.role === 'Dorfwache') ex.helmet = true;
    const wpn = this.role === 'Ordensmagier' ? OG.Items.aetherstab : this.guard ? OG.Items.langschwert : null;
    const swing = this.swingAt && OG.time - this.swingAt < 0.3 ? (OG.time - this.swingAt) / 0.3 : -1;
    OG.drawHumanoid(ctx, this.x, this.y, this.face, 1, this.cloth, this.accent, '#e3c2a3', this.walk, this.flash, wpn, swing, false, ex);
  }
}
OG.Npc = Npc;
