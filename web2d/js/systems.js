// Omni Genesis 2D — die Systeme der lebendigen Welt.
'use strict';

// ---------- Meldungen ----------
OG.messages = [];
OG.announcements = [];
OG.post = (text, kind) => {
  OG.messages.push({ text, kind: kind || 'sys', t: performance.now() });
  if (OG.messages.length > 5) OG.messages.shift();
};
OG.sys = text => OG.post('[System] ' + text, 'sys');
OG.announce = text => {
  OG.post('Verkündung durch die Großen Tore: ' + text, 'ann');
  OG.announcements.unshift(OG.Clock.stamp() + ' — ' + text);
  if (OG.announcements.length > 60) OG.announcements.pop();
};

// ---------- Uhr: 1 Spielstunde = 60 s ----------
OG.Clock = {
  minutes: 480, lastHour: 0, onHour: [], onDay: [],
  init(m) { this.minutes = m; this.lastHour = Math.floor(m / 60); this.onHour = []; this.onDay = []; },
  day() { return Math.floor(this.minutes / 1440) + 1; },
  hourF() { return (this.minutes % 1440) / 60; },
  hour() { return Math.floor(this.hourF()); },
  isNight() { const h = this.hourF(); return h < 6 || h >= 21; },
  daylight() { const a = (this.hourF() / 24) * Math.PI * 2 - Math.PI / 2; return OG.clamp01(Math.sin(a) * 1.6 + 0.15); },
  stamp() { const m = Math.floor(this.minutes % 60); return 'Tag ' + this.day() + ', ' + String(this.hour()).padStart(2, '0') + ':' + String(m).padStart(2, '0'); },
  advance(min) { this.minutes += min; this.process(); },
  skipTo(h) { let d = h - this.hourF(); if (d <= 0) d += 24; this.advance(d * 60); },
  update(dt) { this.minutes += dt * 60 / OG.C.secondsPerGameHour; this.process(); },
  process() {
    const idx = Math.floor(this.minutes / 60);
    while (this.lastHour < idx) {
      this.lastHour++;
      const day = Math.floor(this.lastHour / 24) + 1, hour = this.lastHour % 24;
      for (const f of this.onHour) f(day, hour);
      if (hour === 6) for (const f of this.onDay) f(day);
    }
  },
};

// ---------- Wetter (Markow-Kette) ----------
OG.Weather = {
  kinds: ['Klar', 'Bewölkt', 'Regen', 'Gewitter', 'Nebel'],
  cur: 0, light: 1, fog: 0, flash: 0, next: 0, nextThunder: 0, drops: [],
  init(saved) {
    this.cur = OG.clamp(saved || 0, 0, 4); this.light = this.targetLight(); this.fog = this.targetFog();
    this.next = OG.Clock.minutes + OG.rand(180, 360);
    OG.Clock.onHour.push((d, h) => this.onHour(h));
  },
  name() { return this.kinds[this.cur]; },
  bowMult() { return this.cur === 2 || this.cur === 3 ? 0.9 : 1; },
  aggroMult() { return this.cur === 4 ? 0.6 : 1; },
  prodMult() { return this.cur === 3 ? 0.3 : 1; },
  growthMult() { return this.cur === 2 || this.cur === 3 ? 2 : 1; },
  isStorm() { return this.cur === 3; },
  targetLight() { return [1, 0.78, 0.6, 0.45, 0.72][this.cur]; },
  targetFog() { return [0, 0.1, 0.18, 0.25, 0.5][this.cur]; },
  onHour(h) {
    if (OG.Clock.minutes < this.next) return;
    this.next = OG.Clock.minutes + OG.rand(180, 360);
    const r = Math.random(), c = this.cur;
    let n;
    if (c === 0) n = r < 0.45 ? 0 : r < 0.8 ? 1 : 4;
    else if (c === 1) n = r < 0.3 ? 0 : r < 0.55 ? 1 : r < 0.85 ? 2 : r < 0.95 ? 3 : 4;
    else if (c === 2) n = r < 0.1 ? 0 : r < 0.5 ? 1 : r < 0.8 ? 2 : 3;
    else if (c === 3) n = r < 0.6 ? 2 : 1;
    else n = r < 0.5 ? 0 : 1;
    if (n === 4 && h > 10 && h < 19) n = 1;
    if (n === c) return;
    this.cur = n;
    const txt = ['Der Himmel klart auf.', 'Wolken ziehen auf.', 'Es regnet. Kräuter und Bäume wachsen doppelt so schnell, Bogensehnen werden nass (−10 %).',
      'Gewitter! Karawanen stocken (Handel −70 %), Tiere suchen Schutz.', 'Nebel zieht auf. Tiere bemerken dich später (−40 % Wahrnehmung).'][n];
    OG.post('[Wetter] ' + txt, 'sys');
  },
  update(dt) {
    this.light += (this.targetLight() - this.light) * Math.min(1, dt * 0.2);
    this.fog += (this.targetFog() - this.fog) * Math.min(1, dt * 0.2);
    if (this.cur === 3 && OG.time > this.nextThunder) { this.nextThunder = OG.time + OG.rand(6, 18); this.flash = 1; }
    this.flash = Math.max(0, this.flash - dt * 3);
  },
};

// ---------- Ressourcen ----------
OG.Nodes = {
  list: [], trees: 0,
  hits: { Baum: 3, Erz: 4, Fels: 3, Kraut: 1, Aether: 5 },
  regrowTime: { Baum: 48, Erz: 72, Fels: 72, Kraut: 18, Aether: 96 },
  label: { Baum: 'Baum fällen', Erz: 'Erz abbauen', Fels: 'Stein brechen', Kraut: 'Heilkraut pflücken', Aether: 'Ätherkristall lösen' },
  reset() { this.list = []; this.trees = 0; },
  add(kind, x, y, o) {
    o = o || {};
    const n = { id: this.list.length, kind, x, y, hits: this.hits[kind], regrow: 0, scale: o.scale || 1, variant: o.variant || 0, dead: !!o.dead };
    const r = { Baum: 0.45 * n.scale, Erz: 0.95, Fels: 0.8, Aether: 0.45, Kraut: 0 }[kind];
    if (r > 0) n.col = OG.World.circle(x, y, r, 'ressource');
    const self = this;
    n.spot = new OG.Spot(x, y, kind === 'Baum' ? 2.4 : 1.9, () => {
      const t = self.hits[kind];
      return t > 1 ? self.label[kind] + ' (' + (t - n.hits + 1) + '/' + t + ')' : self.label[kind];
    }, p => self.gather(n, p), () => n.regrow <= 0);
    if (kind === 'Baum') this.trees++;
    this.list.push(n);
    return n;
  },
  gather(n, p) {
    p.swing = { t: 0, dur: 0.3, mult: 0, full: false, extra: 0, approach: 0, done: true };
    n.hits--; OG.fx.burst(n.x, n.y, n.kind === 'Baum' ? '#735033' : n.kind === 'Kraut' ? '#66cc4d' : '#999999', 5);
    p.gainExp(2);
    if (n.hits > 0) return;
    const m = Math.max(1, Math.round(p.buffMult('gather', 1)));
    const give = (id, k) => { p.inv.add(id, k * m); OG.log('+' + k * m + ' ' + OG.itemName(id)); };
    if (n.kind === 'Baum') give('holz', 3);
    else if (n.kind === 'Erz') { give('eisenerz', 2); give('stein', 1); }
    else if (n.kind === 'Fels') give('stein', 3);
    else if (n.kind === 'Kraut') give('heilkraut', 2);
    else give('aetherkristall', 1);
    this.deplete(n, this.regrowTime[n.kind]);
  },
  deplete(n, h) {
    n.regrow = h; n.hits = this.hits[n.kind];
    if (n.col && n.kind !== 'Baum') n.col.on = h <= 0;
  },
  onHour() {
    const g = OG.Weather.growthMult();
    for (const n of this.list) {
      if (n.regrow <= 0) continue;
      n.regrow -= n.kind === 'Baum' || n.kind === 'Kraut' ? g : 1;
      if (n.regrow <= 0) this.deplete(n, 0);
    }
  },
  treeFraction() {
    if (!this.trees) return 1;
    let a = 0; for (const n of this.list) if (n.kind === 'Baum' && n.regrow <= 0) a++;
    return a / this.trees;
  },
  save() { return this.list.filter(n => n.regrow > 0).map(n => [n.id, n.regrow]); },
  load(arr) { for (const [id, h] of arr || []) if (this.list[id]) this.deplete(this.list[id], h); },
};

// ---------- Ökosystem: getötete Tiere kehren nie zurück ----------
OG.Eco = {
  pop: {},
  reset() { this.pop = {}; },
  register(c) { const l = this.pop[c.def.id] || (this.pop[c.def.id] = []); if (!l.includes(c)) l.push(c); },
  unregister(c) { const l = this.pop[c.def.id]; if (l) { const i = l.indexOf(c); if (i >= 0) l.splice(i, 1); } },
  count(id) { const l = this.pop[id]; if (!l) return 0; this.pop[id] = l.filter(c => !c.corpse && !c.removed); return this.pop[id].length; },
  stock(id) { const d = OG.Species[id]; return d && d.capacity ? this.count(id) / d.capacity : 1; },
  habitatPoint(def) {
    for (let i = 0; i < 25; i++) {
      const a = Math.random() * Math.PI * 2, r = Math.sqrt(Math.random()) * def.habR;
      const x = def.hab[0] + Math.cos(a) * r, y = def.hab[1] + Math.sin(a) * r;
      if (OG.freeLand(x, y) && !OG.World.blocked(x, y, 0.6)) return { x, y };
    }
    return { x: def.hab[0], y: def.hab[1] };
  },
  spawn(def, age) {
    const p = this.habitatPoint(def);
    const c = new OG.Creature(def, p.x, p.y, { age });
    c.home = { x: def.hab[0], y: def.hab[1] };
    return c;
  },
  init(saved) {
    OG.Clock.onHour.push(() => this.onHour());
    OG.Clock.onDay.push(() => this.onDay());
    if (!saved) {
      for (const id in OG.Species) { const d = OG.Species[id]; if (d.ecosystem) for (let i = 0; i < d.initial; i++) this.spawn(d, OG.rand(24, 200)); }
    } else {
      for (const s of saved) {
        const d = OG.Species[s.id]; if (!d || !d.ecosystem) continue;
        const c = new OG.Creature(d, s.x, s.y, { age: s.age, hunger: s.hunger });
        c.home = { x: d.hab[0], y: d.hab[1] };
      }
    }
  },
  // Fern vom Spieler ruht die KI. Dort jagen Raubtiere abstrakt, damit sie nicht verhungern.
  offscreenHunt() {
    const P = OG.player, far = e => Math.hypot(e.x - P.x, e.y - P.y) > 140, unseen = e => Math.hypot(e.x - P.x, e.y - P.y) > 45;
    const prey = [];
    for (const id in this.pop) { const d = OG.Species[id]; if (d.diet === 'Pflanzenfresser' && d.temper === 'Scheu') for (const c of this.pop[id]) if (!c.corpse && !c.removed && unseen(c)) prey.push(c); }
    for (const id in this.pop) {
      if (OG.Species[id].diet !== 'Fleischfresser') continue;
      for (const w of this.pop[id].slice()) {
        if (w.corpse || w.removed || w.hunger < 0.6 || !far(w) || !prey.length) continue;
        const st = Math.min(1, this.stock('graslaeufer'));
        if (Math.random() < 0.3 * st * st) {   // Jagderfolg sinkt stark, wenn Beute knapp ist
          const v = prey.splice(Math.floor(Math.random() * prey.length), 1)[0];
          v.damage({ amount: v.maxHp * 10, source: w });
        }
      }
    }
  },
  onHour() {
    this.offscreenHunt();
    const protect = OG.Autonomy.hasLaw('jagdschutz');
    for (const id in this.pop) {
      const d = OG.Species[id], l = this.pop[id] = this.pop[id].filter(c => !c.corpse && !c.removed);
      let adults = 0, hunger = 0;
      for (const c of l.slice()) {
        c.ageHours(1);
        if (!c.juvenile) adults++;
        if (d.diet === 'Fleischfresser') {
          c.hunger = Math.min(1, c.hunger + 1 / 40); hunger += c.hunger;
          if (c.hunger >= 1 && ++c.starving >= 24) c.damage({ amount: c.maxHp * 10 });
        }
      }
      const n = l.length;
      if (adults < 2 || n >= d.capacity) continue;
      if (d.diet === 'Fleischfresser' && hunger / Math.max(1, n) > 0.5) continue;
      let p = d.breed * (1 - n / d.capacity) * (adults / 2);
      if (protect) p *= 1.2;
      if (Math.random() < Math.min(0.9, p)) {
        const par = OG.pick(l);
        const y = new OG.Creature(d, par.x + OG.rand(-2, 2), par.y + OG.rand(-2, 2), { age: 0 });
        y.home = { x: d.hab[0], y: d.hab[1] };
      }
    }
    for (const e of OG.ents) if (e instanceof OG.Creature && e.corpse && !e.managed && OG.Clock.minutes - e.deathMinute > 180) e.removed = true;
  },
  onDay() {
    if (!OG.C.migration) return;
    for (const id in OG.Species) {
      const d = OG.Species[id];
      if (!d.ecosystem || this.count(id) > 0 || Math.random() > OG.C.migrationChance) continue;
      this.spawn(d, 48); this.spawn(d, 48);
      OG.sys('Zuwanderung: zwei ' + d.name + ' sind aus den Nachbarprovinzen eingewandert. Getötete Tiere kehren nie zurück — nur Fremde kommen nach.');
    }
  },
  save() {
    const out = [];
    for (const id in this.pop) for (const c of this.pop[id]) if (!c.corpse && !c.removed && !c.managed) out.push({ id, x: c.x, y: c.y, age: c.age, hunger: c.hunger });
    return out;
  },
};

// ---------- Kaiserliche Kasse und Markt ----------
OG.Treasury = {
  balance: 250000, income: {}, expenses: {},
  add(cat, n) { if (n <= 0) return; this.balance += n; this.income[cat] = (this.income[cat] || 0) + n; },
  pay(n, cat) { if (n <= 0) return true; if (this.balance < n) return false; this.balance -= n; this.expenses[cat] = (this.expenses[cat] || 0) + n; return true; },
  save() { return { balance: this.balance, income: this.income, expenses: this.expenses }; },
  load(s) { if (s) { this.balance = s.balance; this.income = s.income || {}; this.expenses = s.expenses || {}; } else { this.balance = 250000; this.income = {}; this.expenses = {}; } },
};

// Preis = Grundpreis · (Sollbestand / Bestand)^0,7
OG.Market = {
  goods: {},
  init(saved) {
    this.goods = {};
    for (const id in OG.Items) {
      const it = OG.Items[id]; if (!it.tradable) continue;
      const target = it.cat === 'Material' ? 40 : it.cat === 'Verbrauch' ? 20 : 4;
      this.goods[id] = { id, stock: saved && saved[id] != null ? saved[id] : target, target, last: 0 };
    }
    for (const id in this.goods) this.goods[id].last = this.price(id);
    OG.Clock.onHour.push((d, h) => this.onHour(h));
  },
  isOpen() { const h = OG.Clock.hour(); return h >= 6 && h < 22; },
  price(id) { const g = this.goods[id], it = OG.Items[id]; if (!g) return 0; return it.price * OG.clamp(Math.pow(g.target / Math.max(g.stock, 0.5), 0.7), 0.3, 5); },
  buyPrice(id) { return Math.max(1, Math.ceil(this.price(id) * 1.1)); },
  sellPrice(id) { return Math.max(0, Math.floor(this.price(id) * 0.9)); },
  trend(id) { const g = this.goods[id]; return g && g.last > 0 ? this.price(id) / g.last : 1; },
  taxRate() { const P = OG.player; return OG.Autonomy.isAutonomous() && OG.Autonomy.hasLaw('marktfrieden') && OG.inVillage(P.x, P.y) ? 0.05 : OG.C.tradeTax; },
  buy(p, id) {
    if (!this.isOpen()) return 'Der Markt ist geschlossen (6–22 Uhr).';
    const g = this.goods[id]; if (!g || g.stock < 1) return 'Ausverkauft.';
    const pr = this.buyPrice(id); if (!p.spend(pr)) return 'Nicht genug Taler.';
    g.stock -= 1; p.inv.add(id, 1); OG.Treasury.add('Handelsabgabe', Math.round(pr * this.taxRate())); return null;
  },
  sell(p, id) {
    if (!this.isOpen()) return 'Der Markt ist geschlossen (6–22 Uhr).';
    const g = this.goods[id]; if (!g) return 'Das kauft hier niemand.';
    if (!p.inv.remove(id, 1)) return 'Nicht im Inventar.';
    const gross = this.sellPrice(id), tax = Math.round(gross * this.taxRate());
    p.taler += gross - tax; g.stock += 1; OG.Treasury.add('Handelsabgabe', tax); return null;
  },
  ch(id, d) { const g = this.goods[id]; if (g) g.stock = OG.clamp(g.stock + d, 0, g.target * 4); },
  st(id) { return this.goods[id] ? this.goods[id].stock : 0; },
  onHour(h) {
    if (h === 0) for (const id in this.goods) this.goods[id].last = this.price(id);
    if (h < 6 || h >= 22) return;
    const prod = OG.Weather.prodMult(), deer = OG.clamp01(OG.Eco.stock('graslaeufer')), trees = OG.Nodes.treeFraction(), grow = OG.Weather.growthMult();
    const war = OG.Rift.summons ? 2 : 1;
    // Jäger hängen am echten Wildbestand, Holzfäller an den stehenden Bäumen.
    this.ch('fleisch', 1.6 * deer * prod - 1.4 * war);
    this.ch('fell', 0.5 * deer * prod - 0.3);
    this.ch('holz', 1.2 * trees * prod - 1);
    this.ch('stein', 0.7 * prod - 0.5);
    this.ch('eisenerz', 0.6 * prod);
    if (this.st('eisenerz') >= 2 && this.st('holz') >= 1) { this.ch('eisenerz', -2); this.ch('holz', -0.5); this.ch('eisenbarren', prod); }
    this.ch('eisenbarren', -0.4);
    this.ch('heilkraut', 0.6 * grow * prod - 0.3);
    if (h % 2 === 0 && this.st('heilkraut') >= 3) { this.ch('heilkraut', -3); this.ch('heiltrank', prod); }
    this.ch('heiltrank', -0.35 * war);
    this.ch('aetherelixier', 0.1 * prod - 0.1);
    if (this.st('fleisch') > 3) { this.ch('fleisch', -0.4); this.ch('gebratenes_fleisch', 0.4); }
    this.ch('gebratenes_fleisch', -0.5);
    this.ch('aetherkristall', 0.05 * prod - 0.03);
    this.ch('leerenessenz', -0.02);
    for (const id in this.goods) { const c = OG.Items[id].cat; if (c === 'Waffe' || c === 'Ruestung') this.ch(id, 0.05 * prod - 0.04); }
    // Händler gleichen Überschuss und Mangel langsam über das Handelsnetz aus.
    for (const id in this.goods) { const g = this.goods[id]; this.ch(id, (g.target - g.stock) * 0.03); }
  },
  save() { const o = {}; for (const id in this.goods) o[id] = this.goods[id].stock; return o; },
};

// ---------- Recht und Gericht ----------
OG.Law = {
  wanted: 0, greatTreason: false, lastCrimeDay: -1000, pendingFine: 0, trespassing: false, record: [], checkT: 0,
  load(s) { Object.assign(this, { wanted: 0, greatTreason: false, lastCrimeDay: -1000, pendingFine: 0, trespassing: false, record: [] }, s || {}); },
  save() { return { wanted: this.wanted, greatTreason: this.greatTreason, lastCrimeDay: this.lastCrimeDay, pendingFine: this.pendingFine, record: this.record }; },
  report(sev, fine, text) {
    this.wanted = OG.clamp(Math.max(this.wanted, sev), 0, 3);
    this.pendingFine += fine; this.lastCrimeDay = OG.Clock.day();
    this.record.push(OG.Clock.stamp() + ': ' + text);
    OG.post('[Gesetz] ' + text + ' Fahndungsstufe ' + this.wanted + '. Stelle dich mit [G] oder fliehe vor den Wachen.', 'law');
    if (OG.inVillage(OG.player.x, OG.player.y)) OG.Residents.changeAll(-15);
  },
  onPlayerAttackedNpc(n) {
    const lvl = OG.player.level;
    if (n.role === 'Kaiser') { this.greatTreason = true; this.report(3, 5000, 'GROSSER VERRAT: Angriff auf den Kaiser (Art. 62). Das Recht, Prätendent zu werden, ist für immer verwirkt.'); }
    else if (n.role === 'Richter') { this.greatTreason = true; this.report(3, 3000, 'Angriff auf den Obersten Arbiter (Art. 97). Dies gilt als Großer Verrat.'); }
    else if (n.role === 'Wache' || n.role === 'Ordensmagier') this.report(2, 300 + lvl * 20, 'Angriff auf ' + n.npcName + '.');
    else if (n.role !== 'Dorfwache') this.report(1, 100 + lvl * 10, 'Angriff auf ' + n.npcName + ' (Art. 27: Recht auf Leben).');
  },
  update(dt) {
    this.checkT -= dt;
    if (this.checkT <= 0) {
      this.checkT = 1;
      const now = OG.player.y < OG.L.forbiddenY && !OG.player.permit;
      if (now && !this.trespassing) this.report(1, 150, 'Betreten der Verbotenen Lande ohne Kaiserliche Erlaubnis (Art. 112).');
      this.trespassing = now;
    }
    if (this.wanted > 0 && !OG.blocked() && OG.Input.hit('KeyG')) this.arrest();
  },
  arrest() {
    const P = OG.player;
    OG.Clock.advance(24 * 60);
    const fine = this.pendingFine, paid = Math.min(fine, P.taler);
    P.taler -= paid; OG.Treasury.add('Bußgelder', paid);
    let jail = 0;
    if (paid < fine) { jail = OG.clamp(Math.ceil((fine - paid) / 200), 1, 5); OG.Clock.advance(jail * 1440); }
    let v = 'Urteil des Gerichts (binnen drei Tagen, Art. 29; durch Zeugen bewiesen, Art. 57): Bußgeld ' + fine + ' Taler, bezahlt ' + paid + '.';
    if (jail) v += ' Restschuld als Haft: ' + jail + ' Tag(e).';
    v += ' Die Tat ist gesühnt (Art. 58).';
    this.record.push(OG.Clock.stamp() + ': ' + v);
    OG.post('[Gericht] ' + v, 'law');
    this.wanted = 0; this.pendingFine = 0;
    P.hp = Math.max(P.hp, P.maxHp * 0.5);
    P.x = OG.Places.court.x; P.y = OG.Places.court.y; P.vx = P.vy = 0;
  },
};

// ---------- Siegel des Risses (Art. 16, 60, 66) ----------
OG.Rift = {
  strength: 70, summons: false, voids: [], guardian: null, guardianDead: false, guardianDay: -100, spot: null,
  init(s) {
    s = s || {};
    this.strength = s.strength != null ? s.strength : 70; this.guardianDead = !!s.guardianDead; this.guardianDay = s.guardianDay != null ? s.guardianDay : -100;
    this.voids = []; this.guardian = null; this.summons = false;
    const P = OG.Places.seal;
    this.spot = new OG.Spot(P.x, P.y, 3, () => 'Siegel verstärken (1 Ätherkristall) — Stärke ' + Math.round(this.strength) + ' %', p => this.reinforce(p));
    OG.Clock.onHour.push((d, h) => this.onHour(d));
  },
  reinforce(p) {
    if (p.inv.count('aetherkristall') < 1) { OG.sys('Du brauchst einen Ätherkristall (Ätheradern in den Verbotenen Landen, Markt, Aufträge).'); return; }
    if (this.strength >= 99.5) { OG.sys('Das Siegel ist vollständig.'); return; }
    p.inv.remove('aetherkristall', 1);
    this.strength = Math.min(100, this.strength + 20); p.gainExp(120);
    if (OG.Treasury.pay(60, 'Belohnungen')) p.taler += 60;
    OG.sys('Siegel verstärkt auf ' + Math.round(this.strength) + ' %. Die Kasse dankt dir mit 60 Talern (Art. 60 Nr. 3).');
    OG.fx.ring(OG.Places.seal.x, OG.Places.seal.y, 4, '#66d9ff', 0.8);
  },
  weaken(n) { this.strength = Math.max(0, this.strength - n); },
  onHour(day) {
    // Der Orden hält das Siegel bei etwa 55 %. Unter 20 % ist er überfordert.
    // Während des Großen Aufrufs hilft das ganze Reich: das Siegel erholt sich langsam (+0,5 %/Std.).
    this.strength = OG.clamp(this.strength - 2 + (this.summons ? 2.5 : this.strength > 20 ? (this.strength < 55 ? 2.4 : 1.6) : 0.3), 0, 100);
    if (Math.random() < 0.012) { this.strength = Math.max(0, this.strength - 25); OG.announce('Rissbeben! Das Siegel der Eindämmung ist geschwächt (' + Math.round(this.strength) + ' %).'); }
    this.voids = this.voids.filter(v => !v.corpse && !v.removed);
    const night = OG.Clock.isNight();
    const want = Math.min(2, Math.floor((1 - this.strength / 100) * 3) + (night ? 1 : 0));
    const R = OG.Places.rift;
    for (let i = 0; i < want && this.voids.length < 12; i++) {
      const v = new OG.Creature(OG.Species.leerenbrut, R.x + OG.rand(-10, 10), R.y + OG.rand(-8, 8), {});
      v.home = { x: R.x, y: R.y + 15 };
      if (night && this.strength < 40 && Math.random() < 0.35) v.raider = true;
      this.voids.push(v);
    }
    if ((!this.guardian || this.guardian.corpse) && this.strength < 30 && (!this.guardianDead || day > this.guardianDay)) {
      this.guardian = new OG.Creature(OG.Species.risswaechter, R.x, R.y, {});
      this.guardianDead = false;
      OG.announce('Der Wächter des Risses ist zurückgekehrt. Nur Risswesen kehren wieder — die Geschichte bringt sie zurück.');
    }
    if (!this.summons && this.strength < 15) { this.summons = true; OG.announce('GROSSER AUFRUF (Art. 66): Kaiser ' + OG.C.emperorName + ' ruft den Kontinent zur Verteidigung. Für jede besiegte Leerenbrut zahlt die Kasse 40 Taler.'); }
    else if (this.summons && this.strength >= 50) { this.summons = false; OG.announce('Der Große Aufruf ist beendet. Das Siegel hält wieder.'); }
  },
  onVoidKilled(c, byPlayer) {
    if (c === this.guardian) {
      this.guardianDead = true; this.guardianDay = OG.Clock.day(); this.strength = Math.min(100, this.strength + 15);
      OG.announce('Der Wächter des Risses ist gefallen. Das Siegel erholt sich (+15 %).');
    }
    if (byPlayer && this.summons && OG.Treasury.pay(40, 'Großer Aufruf')) OG.player.taler += 40;
  },
  save() { return { strength: this.strength, guardianDead: this.guardianDead, guardianDay: this.guardianDay }; },
};

// ---------- Einwohner von Eichenhain (Säule des Volkes) ----------
OG.Residents = {
  list: [], lastThanks: -99,
  names: ['Anna Weber', 'Bruno Kessler', 'Clara Brandt', 'Dieter Holm', 'Elsa Fink', 'Friedrich Lang', 'Greta Moor', 'Hans Riedel', 'Ida Sommer', 'Jonas Falk', 'Katrin Bauer', 'Lukas Stein'],
  needs: [['holz', 5], ['fleisch', 3], ['heilkraut', 4], ['heiltrank', 1], ['fell', 2], ['eisenbarren', 1], ['stein', 4], ['gebratenes_fleisch', 2]],
  init(saved) {
    this.list = [];
    const P = OG.Places;
    this.names.forEach((name, i) => {
      const s = (saved || []).find(r => r.name === name);
      const r = { name, respect: s ? s.respect : 0, need: s ? s.need : null };
      if (!s) this.assign(r);
      const home = P.villageHomes[i % P.villageHomes.length];
      const hue = (i * 47) % 360;
      const n = new OG.Npc('Einwohner', name, i % 2 ? 'Einwohner von Eichenhain' : 'Einwohnerin von Eichenhain', home.x, home.y, 0, 'hsl(' + hue + ',30%,55%)', '#5a4733');
      n.cloth = OG.hsl2hex(hue, 0.3, 0.55);
      n.route = [0, 1, 2, 3].map(k => P.villageWalk[(i + k * 3) % P.villageWalk.length]);
      n.resident = r; r.npc = n;
      this.list.push(r);
    });
    OG.Clock.onDay.push(() => { for (const r of this.list) if (!r.need) this.assign(r); });
  },
  assign(r) { const k = OG.pick(this.needs); r.need = { id: k[0], n: k[1] }; },
  respects(r) { return r.respect >= OG.C.respectThreshold; },
  respecting() { return this.list.filter(r => this.respects(r)).length; },
  allRespect() { return this.list.length > 0 && this.respecting() === this.list.length; },
  avg() { return this.list.reduce((s, r) => s + r.respect, 0) / Math.max(1, this.list.length); },
  change(r, d) {
    const before = this.respects(r);
    r.respect = OG.clamp(r.respect + d, -100, 100);
    if (!before && this.respects(r)) OG.sys(r.name + ' respektiert dich nun. (' + this.respecting() + '/' + this.list.length + ' Einwohner)');
    if (before && !this.respects(r)) OG.sys(r.name + ' hat den Respekt vor dir verloren.');
  },
  changeAll(d) { for (const r of this.list) this.change(r, d); },
  fulfil(r, p) {
    if (!r.need || p.inv.count(r.need.id) < r.need.n) return false;
    p.inv.remove(r.need.id, r.need.n); r.need = null;
    this.change(r, 12); for (const o of this.list) if (o !== r) this.change(o, 1);
    p.gainExp(25); return true;
  },
  onHostileKilled(x, y, def, byPlayer) {
    if (!byPlayer || !OG.inVillage(x, y)) return;
    const g = def.diet === 'Leere' || def.diet === 'Kult' ? 4 : 2;
    this.changeAll(g);
    if (OG.time - this.lastThanks > 20) { this.lastThanks = OG.time; OG.sys('Die Einwohner von Eichenhain haben gesehen, wie du sie beschützt hast (+' + g + ' Respekt).'); }
  },
  onAttacked(r) { this.change(r, -40); for (const o of this.list) if (o !== r) this.change(o, -10); },
  save() { return this.list.map(r => ({ name: r.name, respect: r.respect, need: r.need })); },
};
OG.hsl2hex = (h, s, l) => {
  const f = n => { const k = (n + h / 30) % 12, a = s * Math.min(l, 1 - l); const c = l - a * Math.max(-1, Math.min(k - 3, 9 - k, 1)); return Math.round(255 * c).toString(16).padStart(2, '0'); };
  return '#' + f(0) + f(8) + f(4);
};
