// Omni Genesis 2D — andere Reisende (Spieler der Welt), Gesetzlose und Ehrenduelle (Art. 124).
'use strict';

OG.pvp = { kills: 0, innocents: 0, outlaws: 0, duelsWon: 0, duelsLost: 0, deaths: 0 };
OG.drawHooks = OG.drawHooks || [];
OG.duel = null;

OG.inSafeZone = e => OG.dist(e.x, e.y, OG.L.city.x, OG.L.city.y) < OG.L.cityR || OG.dist(e.x, e.y, OG.L.village.x, OG.L.village.y) < 26;
OG.bountyOf = r => 50 + 15 * r.level;

class Rival extends OG.Entity {
  constructor(d) {
    super(d.x, d.y, 0.4);
    this.name = this.npcName = d.name;
    this.cls = OG.cls(d.classId);
    this.level = d.level || 1; this.exp = d.exp || 0;
    this.outlaw = !!d.outlaw;
    this.faction = this.outlaw ? 'Outlaw' : 'Rival';
    this.taler = d.taler != null ? d.taler : 100 + this.level * 30;
    this.kills = d.kills || 0; this.deaths = d.deaths || 0; this.grudge = d.grudge || 0;
    this.state = 'roam'; this.wp = null; this.target = null; this.attackReady = 0; this.respawnAt = 0;
    this.walk = 0; this.face = 0; this.duel = false; this.duelGrace = 0; this.lastPlayerHit = -99; this.hitPlayerAt = -99; this.thinkT = Math.random() * 0.3; this.swingAt = -9;
    this.recalc(); this.hp = this.maxHp;
    OG.ents.push(this);
  }
  recalc() { this.maxHp = 120 + this.level * 24; this.armor = 2 + this.level * 0.35; this.mass = 75; this.height = 1.8; }
  get weapon() {
    const up = this.level >= 15;
    return OG.Items[{ novize: up ? 'kurzschwert' : 'holzknueppel', schwertkaempfer: up ? 'langschwert' : 'kurzschwert', kampfmagier: up ? 'aetherstab' : 'lehrlingsstab',
      bogenschuetze: up ? 'langbogen' : 'jagdbogen', assassine: 'dolche', berserker: up ? 'grossschwert' : 'kriegsaxt' }[this.cls.id]];
  }
  get power() { return 0.8 + this.level * 0.045; }
  prompt() { return 'Sprechen: ' + this.name + ' (Reisender, ' + this.cls.name + ', Stufe ' + this.level + ')'; }
  canInteract() { return !this.dead && !this.outlaw && this.target !== OG.player && !this.duel; }
  interact() { OG.UI.openPanel('rival', this); }

  gainLevels(h) {
    for (let i = 0; i < h; i++) {
      if (this.dead || this.level >= 95) continue;
      if (Math.random() < 1 / (8 + this.level * 0.6)) { this.level++; this.recalc(); this.hp = this.maxHp; }
      this.taler += this.level * 2;
    }
  }

  // Hat dieser Reisende den Spieler in den letzten 30 s angegriffen? Dann ist Gegenwehr Notwehr.
  get aggressor() { return OG.time - this.hitPlayerAt < 30; }
  // Ehrenduell: niemand fällt, wer unter 15 % sinkt, gibt auf. Einmischen gilt nicht.
  modify(info) {
    if (OG.time < this.duelGrace || (this.duel && info.source !== OG.player)) { info.amount = 0; info.duelEnd = true; return info; }
    if (this.duel) {
      const eff = info.amount * 60 / (60 + this.armor);
      if (this.hp - eff <= this.maxHp * 0.15) { info.amount = 0; info.duelEnd = true; this.hp = this.maxHp * 0.15; OG.endDuel(true); }
    }
    return info;
  }
  onDamaged(info) {
    const src = info.source;
    if (!src || src === this || info.duelEnd) return;
    if (src === OG.player && !this.duel) {
      if (!this.outlaw && !this.aggressor && OG.time - this.lastPlayerHit > 8) OG.Law.report(1, 80 + OG.player.level * 10, 'Angriff auf den Reisenden ' + this.name + ' (Art. 27).');
      this.lastPlayerHit = OG.time;
    }
    if (!this.duel) this.target = src;
  }
  onDeath(info) {
    const P = OG.player, k = info.source;
    const byPlayer = k === P || (k instanceof OG.Creature && k.isAlly);
    if (P.feud === this) P.feud = null;
    this.deaths++; this.respawnAt = OG.time + 60; this.target = null;
    OG.fx.burst(this.x, this.y, this.outlaw ? '#b33333' : '#8cb3d9', 10);
    const loot = Math.round(this.taler * 0.3);
    this.taler -= loot;
    if (byPlayer) {
      OG.pvp.kills++;
      P.taler += loot;
      P.gainExp(30 * this.level);
      const mats = ['eisenbarren', 'fell', 'heiltrank', 'aetherkristall', 'holz'];
      const m = OG.pick(mats), n = OG.randInt(1, 2);
      P.inv.add(m, n);
      OG.log('Beute: ' + loot + ' Taler, ' + n + '× ' + OG.itemName(m));
      if (this.outlaw) {
        OG.pvp.outlaws++;
        const b = OG.bountyOf(this);
        if (OG.Treasury.pay(b, 'Kopfgelder')) P.taler += b;
        OG.sys('Der Gesetzlose ' + this.name + ' ist besiegt. Die Kaiserliche Kasse zahlt ' + b + ' Taler Kopfgeld.');
        if (OG.inVillage(this.x, this.y)) OG.Residents.changeAll(3);
        if (this.level >= 20) OG.announce(P.charName + ' hat den Gesetzlosen ' + this.name + ' (Stufe ' + this.level + ') zur Strecke gebracht.');
      } else if (this.aggressor) {
        OG.post('[Kampf] ' + this.name + ' hat dich zuerst angegriffen. Gegenwehr ist Notwehr (Art. 27) — kein Verbrechen.', 'pvp');
        this.grudge = Math.max(0, this.grudge - 1);
      } else {
        OG.pvp.innocents++;
        this.grudge = 3;
        OG.Law.report(2, 400 + P.level * 30, 'Totschlag an dem Reisenden ' + this.name + ' (Art. 27). Blutschuld!');
        if (Math.random() < 0.5) OG.announce('Der Reisende ' + this.name + ' wurde von ' + P.charName + ' erschlagen. Die Legion sucht den Täter.');
      }
    } else if (k instanceof Rival) {
      k.kills++; k.taler += loot;
      if (k.outlaw && Math.random() < 0.5) OG.announce('Der Gesetzlose ' + k.name + ' hat den Reisenden ' + this.name + ' überfallen.');
    }
  }

  update(dt) {
    if (this.respawnAt) {
      if (OG.time < this.respawnAt) return;
      // Reisende kehren zurück — anders als Tiere (Art. 45: Heilung im Lazarett).
      const p = this.outlaw ? OG.Places.hideout : OG.Places.lazarett;
      this.x = p.x + OG.rand(-2, 2); this.y = p.y + OG.rand(-2, 2);
      this.dead = false; this.hp = this.maxHp * 0.6; this.respawnAt = 0; this.state = 'roam'; this.wp = null;
    }
    this.flash = Math.max(0, this.flash - dt * 4);
    if (!this.target && this.hp < this.maxHp) this.heal(this.maxHp * 0.02 * dt);
    this.thinkT -= dt;
    if (this.thinkT <= 0) { this.thinkT = 0.3; this.think(); }
    this.act(dt);
    this.vx *= Math.max(0, 1 - 6 * dt); this.vy *= Math.max(0, 1 - 6 * dt);
  }

  think() {
    const P = OG.player;
    const t = this.target;
    if (t && (t.dead || t.removed || t.corpse || t.downed || (t instanceof Rival && t.respawnAt))) this.target = null;
    if (this.duel) { this.target = P; this.state = 'fight'; return; }
    if (this.target && Math.hypot(this.target.x - this.x, this.target.y - this.y) > 40) this.target = null;
    if (!this.outlaw && this.hp < this.maxHp * 0.25 && this.target) { this.state = 'flee'; this.target = null; return; }
    if (this.state === 'flee' && !OG.inSafeZone(this)) return;
    const dP = Math.hypot(P.x - this.x, P.y - this.y);
    // Neulingsschutz: unter Stufe 5 lassen Gesetzlose neue Bürger in Ruhe.
    const playerOk = !P.dead && !P.downed && !OG.inSafeZone(P) && P.level >= 5 && !OG.Trial.active;
    if (!this.target && this.outlaw && playerOk && dP < 18 && (this.level >= P.level - 3 || P.frac < 0.5)) this.target = P;
    if (!this.target && this.outlaw && Math.random() < 0.3) this.target = OG.nearest(this.x, this.y, 12, e => e instanceof Rival && !e.outlaw && !e.dead && !e.duel && !OG.inSafeZone(e));
    if (!this.target && this.grudge > 0 && playerOk && dP < 25) this.target = P;
    // Kopfgeldjäger: wer Blutschuld trägt, wird auch von anständigen Reisenden gestellt.
    if (!this.target && !this.outlaw && playerOk && OG.Law.wanted >= 2 && OG.pvp.innocents > 0 && dP < 15 && this.level >= P.level - 5) {
      this.target = P;
      OG.post('[Kampf] ' + this.name + ': „Blutschuld! Im Namen des Edikts — ergib dich!“', 'pvp');
    }
    // Wildtiere nur in der Nähe des Spielers (weit weg ruht die Wildnis). Reisende jagen maßvoll:
    // Leerenbrut und Kult immer, Wildtiere nur bei Überbestand — oder wenn sie selbst angegriffen werden.
    if (!this.target && dP < 120 && Math.random() < 0.35) {
      this.target = OG.nearest(this.x, this.y, 14, e => {
        if (!(e instanceof OG.Creature) || e.corpse || e.isAlly || e.def.diet === 'Konstrukt' || e.questTag || e.golden) return false;
        if (e.target === this) return true;
        if (e.managed) return e.def.diet === 'Leere' || e.def.diet === 'Kult';
        return e.def.ecosystem && OG.Eco.stock(e.def.id) > 0.85;
      });
    }
    this.state = this.target ? 'fight' : (this.state === 'flee' && !OG.inSafeZone(this) ? 'flee' : 'roam');
  }

  pickWaypoint() {
    const L = OG.L, r = Math.random(), lvl = this.level;
    let p;
    if (!this.outlaw && r < 0.15) p = { x: OG.Places.plaza.x + OG.rand(-8, 8), y: OG.Places.plaza.y + OG.rand(-6, 6) };
    else if (!this.outlaw && r < 0.25) p = { x: L.village.x + OG.rand(-10, 10), y: L.village.y + OG.rand(-10, 10) };
    else if (lvl >= 8 && r < 0.45) p = { x: L.hills.x + OG.rand(-40, 40), y: L.hills.y + OG.rand(-40, 40) };
    else if (lvl >= 20 && r < 0.6) p = { x: OG.rand(-80, 80), y: OG.rand(-150, -118) };
    else if (this.outlaw && r < 0.7) p = { x: OG.Places.hideout.x + OG.rand(-15, 15), y: OG.Places.hideout.y + OG.rand(-15, 15) };
    else p = { x: OG.rand(-150, 150), y: OG.rand(-90, 70) };
    p.x = OG.clamp(p.x, -175, 175); p.y = OG.clamp(p.y, -175, 175);
    this.wp = p;
  }

  act(dt) {
    let gx, gy, sp = 0;
    if (this.state === 'flee') { gx = OG.Places.plaza.x; gy = OG.Places.plaza.y; sp = 5.2; }
    else if (this.state === 'fight' && this.target) {
      const t = this.target, w = this.weapon, ranged = w.kind !== 'Nahkampf';
      const d = Math.hypot(t.x - this.x, t.y - this.y), reach = ranged ? 10 : w.length + 0.8 + t.r;
      this.face = Math.atan2(t.y - this.y, t.x - this.x);
      if (d > reach) { gx = t.x; gy = t.y; sp = 4.6; }
      else if (OG.time >= this.attackReady) this.attack(t, w, ranged);
    } else {
      if (!this.wp || Math.hypot(this.wp.x - this.x, this.wp.y - this.y) < 2) this.pickWaypoint();
      gx = this.wp.x; gy = this.wp.y; sp = 3.4;
    }
    if (sp > 0) {
      const dx = gx - this.x, dy = gy - this.y, l = Math.hypot(dx, dy);
      if (l > 0.3) {
        const st = Math.min(sp * dt, l);
        this.move(dx / l * st + this.vx * dt, dy / l * st + this.vy * dt);
        if (this.state !== 'fight') this.face = Math.atan2(dy, dx);
        this.walk += st * 2.2;
      }
    } else if (Math.abs(this.vx) + Math.abs(this.vy) > 0.01) this.move(this.vx * dt, this.vy * dt);
  }

  // Dieselbe Physik wie beim Spieler: F = m · a.
  attack(t, w, ranged) {
    const swing = Math.max(0.15, (w.swing || 0.3) / (1 + this.level * 0.01));
    this.attackReady = OG.time + (ranged ? 1 : swing + 0.45);
    this.swingAt = OG.time;
    const deal = (st, nx, ny) => {
      const dealt = t.damage({ amount: st.dmg, source: this });
      if (t === OG.player) OG.log(this.name + ': ' + st.f + ' → ' + Math.round(dealt) + ' Schaden an dir');
      else if (dealt > 0) OG.fx.float(t.x, t.y - 1, String(Math.round(dealt)), '#9fc8ff');
      if (t.knock) t.knock(nx * st.J * OG.C.knockbackScale, ny * st.J * OG.C.knockbackScale);
    };
    const a = this.face, nx = Math.cos(a), ny = Math.sin(a);
    if (t === OG.player && !this.duel) this.hitPlayerAt = OG.time;
    if (!ranged) { deal(OG.Phys.melee(w.mass, w.length, swing, 0, this.power), nx, ny); return; }
    const isBow = w.kind === 'Bogen';
    const st = isBow ? OG.Phys.arrowImpact(0.05, Math.min(70, OG.Phys.arrowSpeed(w.draw, 0.6, 0.05)), this.power) : OG.Phys.spell(0.25, 120 * (1 + this.level * 0.03) * (w.focus || 1), this.power);
    const p = new OG.Projectile(this, this.x + nx * 0.6, this.y + ny * 0.6, 1.4, nx * 24, ny * 24, { r: isBow ? 0.12 : 0.2, color: isBow ? '#e6d9b3' : '#8fb8ff', arrow: isBow, glow: !isBow });
    p.canHit = e => e === t;
    p.onHit = () => deal(st, nx, ny);
  }

  draw(ctx) {
    if (this.respawnAt) return;
    const sw = OG.time - this.swingAt < 0.3 ? (OG.time - this.swingAt) / 0.3 : -1;
    const ex = this.outlaw ? { hood: '#4d1414', robe: '#3a0f0f' } : {};
    OG.drawHumanoid(ctx, this.x, this.y, this.face, 1, this.cls.color, this.outlaw ? '#260a0a' : '#2e2a26', '#e3c2a3', this.walk, this.flash, this.weapon, sw, false, ex);
    if (this.duel) { ctx.strokeStyle = 'rgba(242,193,77,0.9)'; ctx.lineWidth = 0.07; ctx.beginPath(); ctx.arc(this.x, this.y, 0.8, 0, Math.PI * 2); ctx.stroke(); }
  }
}
OG.Rival = Rival;

OG.Rivals = {
  names: ['Schattenfuchs', 'Kaira77', 'Eisenherz', 'NovaKlinge', 'Brummbär', 'Lichtpfeil', 'Graufalke', 'Morgentau', 'Rabenherz', 'Funkenflug', 'Silberdorn', 'Wolkenreiter'],
  list: [],
  init(saved) {
    this.list = [];
    OG.Places.hideout = { x: -150, y: 110 };
    OG.World.lights.push({ x: -147, y: 111.5, r: 9, warm: true, fire: true });
    let data = saved;
    if (!data || !data.length) {
      const lv = [3, 5, 7, 9, 12, 15, 18, 24, 30, 38];
      data = lv.map((l, i) => {
        const p = { x: OG.rand(-140, 140), y: OG.rand(-80, 60) };
        return { name: this.names[i], classId: OG.pick(OG.Classes).id, level: l, outlaw: i === 3 || i === 7, x: p.x, y: p.y };
      });
    }
    for (const d of data) this.list.push(new Rival(d));
    OG.Clock.onHour.push(() => this.onHour());
  },
  // Die Welt dreht sich weiter: Reisende steigen auf, Gesetzlose überfallen andere.
  onHour() {
    for (const r of this.list) { r.gainLevels(1); if (r.grudge > 0 && Math.random() < 0.08) r.grudge--; }
    const outlaws = this.list.filter(r => r.outlaw && !r.respawnAt), others = this.list.filter(r => !r.outlaw && !r.respawnAt);
    const P = OG.player;
    if (outlaws.length && others.length && Math.random() < 0.08) {
      const a = OG.pick(outlaws), b = OG.pick(others);
      if (Math.hypot(a.x - P.x, a.y - P.y) > 60 && Math.hypot(b.x - P.x, b.y - P.y) > 60) {
        const win = a.level + OG.rand(-10, 10) > b.level ? a : b, lose = win === a ? b : a;
        win.kills++; lose.deaths++;
        if (Math.random() < 0.4) OG.announce(win.outlaw ? 'Der Gesetzlose ' + win.name + ' hat den Reisenden ' + lose.name + ' überfallen.' : 'Der Reisende ' + win.name + ' hat den Gesetzlosen ' + lose.name + ' vertrieben.');
      }
    }
    if (Math.random() < 0.01) {
      const r = OG.pick(this.list);
      r.outlaw = !r.outlaw; r.faction = r.outlaw ? 'Outlaw' : 'Rival';
      OG.announce(r.outlaw ? 'Der Reisende ' + r.name + ' gilt nun als gesetzlos. Kopfgeld: ' + OG.bountyOf(r) + ' Taler.' : 'Der Gesetzlose ' + r.name + ' hat seine Schuld gesühnt und kehrt ins Recht zurück.');
    }
  },
  save() { return this.list.map(r => ({ name: r.name, classId: r.cls.id, level: r.level, exp: r.exp, outlaw: r.outlaw, taler: r.taler, kills: r.kills, deaths: r.deaths, grudge: r.grudge, x: r.x, y: r.y })); },
  ranking() {
    const P = OG.player;
    const rows = this.list.map(r => ({ name: r.name, cls: r.cls.name, level: r.level, kills: r.kills, deaths: r.deaths, outlaw: r.outlaw, me: false }));
    rows.push({ name: P.charName, cls: P.cls.name, level: P.level, kills: OG.pvp.kills, deaths: OG.pvp.deaths || 0, outlaw: OG.pvp.innocents > 0 && OG.Law.wanted > 0, me: true });
    rows.sort((a, b) => b.level - a.level || b.kills - a.kills);
    return rows;
  },
};

// Ehrenduell (Art. 124): mit Erlaubnis des Throns, ohne Tod, mit Einsatz.
OG.startDuel = r => {
  const P = OG.player, wager = 10 * r.level;
  if (P.taler < wager) return 'Für den Einsatz von ' + wager + ' Talern reicht dein Geld nicht.';
  const accept = OG.clamp(0.8 - (P.level - r.level) * 0.08, 0.1, 0.95);
  if (Math.random() > accept) return r.name + ' lehnt ab: „' + (P.level > r.level ? 'Gegen dich? Lieber nicht.' : 'Heute nicht, Reisender.') + '“';
  OG.duel = { rival: r, wager, start: OG.time };
  r.duel = true; r.target = P;
  OG.UI.close();
  OG.UI.banner('EHRENDUELL (ART. 124)', '#f2c14d');
  OG.sys('Ehrenduell gegen ' + r.name + ' (Stufe ' + r.level + '). Einsatz: ' + wager + ' Taler. Wer unter 15 % Leben fällt, gibt auf.');
  return null;
};

OG.endDuel = playerWon => {
  const d = OG.duel; if (!d) return;
  const P = OG.player, r = d.rival;
  OG.duel = null; r.duel = false; r.target = null; r.duelGrace = OG.time + 3;
  if (playerWon === true) {
    P.taler += d.wager; r.taler = Math.max(0, r.taler - d.wager); OG.pvp.duelsWon++;
    P.gainExp(15 * r.level);
    OG.sys('Du hast das Ehrenduell gegen ' + r.name + ' gewonnen! +' + d.wager + ' Taler.');
    OG.UI.banner('DUELL GEWONNEN', '#73eb8c');
  } else if (playerWon === false) {
    P.taler = Math.max(0, P.taler - d.wager); r.taler += d.wager; OG.pvp.duelsLost++;
    OG.sys(r.name + ' hat das Ehrenduell gewonnen. Du verlierst ' + d.wager + ' Taler.');
    OG.UI.banner('DUELL VERLOREN', '#ff6b5e');
  } else OG.sys('Das Duell gegen ' + r.name + ' endet ohne Sieger.');
};

OG.updateDuel = () => {
  const d = OG.duel; if (!d) return;
  if (OG.time - d.start > 120 || Math.hypot(d.rival.x - OG.player.x, d.rival.y - OG.player.y) > 40 || d.rival.dead || OG.player.downed) OG.endDuel(null);
};

OG.Rivals.update = () => {
  OG.updateDuel();
  const P = OG.player;
  if (P.feud && (OG.time > P.feudUntil || P.feud.respawnAt)) P.feud = null;
};

// Der Spieler wurde von einem Reisenden besiegt: der Sieger nimmt die verlorenen Taler.
OG.Rivals.onPlayerDowned = (P, lost) => {
  const k = P.lastAttacker;
  if (!(k instanceof Rival)) return lost;
  OG.pvp.deaths++;
  k.kills++; k.grudge = 0; k.target = null;
  if (k.outlaw) { const extra = Math.round(P.taler * 0.1); P.taler -= extra; lost += extra; }
  k.taler += lost;
  OG.post('[Kampf] ' + k.name + (k.outlaw ? ' (Gesetzloser)' : '') + ' hat dich besiegt und nimmt ' + lost + ' Taler mit.', 'pvp');
  if (k.outlaw) OG.announce('Der Gesetzlose ' + k.name + ' hat ' + P.charName + ' überfallen. Kopfgeld: ' + OG.bountyOf(k) + ' Taler.');
  return lost;
};

// Versteck der Gesetzlosen (westlich der Arena).
OG.drawHideout = ctx => {
  const P = OG.player, h = OG.Places.hideout;
  if (!h || Math.abs(P.x - h.x) > 45 || Math.abs(P.y - h.y) > 35) return;
  ctx.fillStyle = '#3a1414'; ctx.beginPath(); ctx.moveTo(h.x - 2.2, h.y + 1.4); ctx.lineTo(h.x - 0.4, h.y - 1.8); ctx.lineTo(h.x + 1.4, h.y + 1.4); ctx.fill();
  ctx.fillStyle = '#261010'; ctx.beginPath(); ctx.moveTo(h.x - 0.4, h.y - 1.8); ctx.lineTo(h.x + 1.4, h.y + 1.4); ctx.lineTo(h.x - 0.4, h.y + 1.4); ctx.fill();
  ctx.fillStyle = '#2a1f1a'; ctx.beginPath(); ctx.arc(h.x + 3, h.y + 1.5, 0.8, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = 'rgba(255,140,40,' + (0.7 + Math.sin(OG.time * 8) * 0.2) + ')'; ctx.beginPath(); ctx.arc(h.x + 3, h.y + 1.5, 0.45, 0, Math.PI * 2); ctx.fill();
  ctx.strokeStyle = '#8c2a2a'; ctx.lineWidth = 0.12; ctx.beginPath(); ctx.moveTo(h.x - 3.4, h.y - 1); ctx.lineTo(h.x - 3.4, h.y - 3); ctx.stroke();
  ctx.fillStyle = '#b33333'; ctx.fillRect(h.x - 3.4, h.y - 3, 1.1, 0.7);
};
OG.drawHideout.keep = true;
OG.drawHooks.push(OG.drawHideout);

OG.Rivals.chatter = [
  'Die Wölfe im Westen sind hungrig. Lass ein paar Grasläufer übrig — sonst kommen die Wölfe ins Dorf.',
  'Gesetzlose erkennst du an der roten Kapuze. Ihr Versteck soll westlich der Arena liegen. Die Kasse zahlt Kopfgeld.',
  'Ich hab gehört, irgendwo am Boden glitzern Splitter wie kleine Sterne. Man sieht sie nur, wenn man direkt davor steht.',
  'Um Mitternacht soll der Brunnen am Platz Münzen annehmen. Oder er frisst sie einfach.',
  'Der Kaiser ist Stufe 100. Ich bin Stufe {lvl}. Noch ein Stück, haha.',
  'Ein Duell? Gern — aber nach Art. 124: wer unter 15 % fällt, gibt auf. Keiner stirbt.',
  'Die Geschichte mit Haus Vorn? Hab ich noch gar nicht angefangen. Ich level lieber in den Erzhöhen.',
  'Jemand meinte, die Karte hört nicht hinter der Stadt auf. Ganz im Süden soll nur noch Nebel sein.',
  'Wer einen friedlichen Reisenden erschlägt, trägt Blutschuld. Dann jagen dich die Legion und alle Kopfgeldjäger.',
  'Hast du die Kanzlei mal genauer angesehen? Da steht ein altes Regal, das keiner aufräumt.',
];
