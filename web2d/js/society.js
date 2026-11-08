// Omni Genesis 2D — Weg der Diplomatie, Rangprüfung und Aufträge.
'use strict';

// ---------- Weg der Diplomatie (Abschnitt X des Edikts) ----------
OG.Autonomy = {
  lawIds: ['freier_weg', 'jagdschutz', 'marktfrieden', 'wachpflicht', 'siegelschutz'],
  lawNames: ['Recht des Freien Weges (Pflicht laut Art. 33 und 136)', 'Jagdschutz: Jungtiere sind geschützt (Nachwuchs +20 %)', 'Marktfrieden: Handelsabgabe 5 % in der Autonomie', 'Wachpflicht: die Dorfwache patrouilliert auch nachts', 'Schutz der Siegel: Eichenhain unterstützt den Orden'],
  colors: ['#bf2626', '#268c40', '#d9a61a', '#7333a6', '#1a8099', '#333338'],
  guards: 0, townHall: false, laws: [], state: 'Keine', revisionEnd: 0, audienceDay: 0, audienceHour: 12, rejectedDay: -1000, rejectReason: '', name: '', color: 0, warningUntil: -1, guardNpcs: [],
  init(s) {
    Object.assign(this, { guards: 0, townHall: false, laws: [], state: 'Keine', revisionEnd: 0, audienceDay: 0, audienceHour: 12, rejectedDay: -1000, rejectReason: '', name: '', color: 0, warningUntil: -1 }, s || {});
    this.guardNpcs = [];
    for (let i = 0; i < this.guards; i++) this.spawnGuard(i);
    const P = OG.Places;
    this.siteSpot = new OG.Spot(P.townHallSite.x, P.townHallSite.y, 3, () => this.townHall ? 'Rathaus von Eichenhain' : 'Bauplatz: Rathaus errichten', () => OG.UI.openPanel('townhall'));
    if (this.townHall) this.buildHall();
    OG.Clock.onDay.push(d => this.onDay(d));
  },
  save() {
    const o = {};
    for (const k of ['guards', 'townHall', 'laws', 'state', 'revisionEnd', 'audienceDay', 'audienceHour', 'rejectedDay', 'rejectReason', 'name', 'color', 'warningUntil']) o[k] = this[k];
    return o;
  },
  isAutonomous() { return this.state === 'Vertrag'; },
  hasLaw(id) { return this.laws.includes(id); },
  owned(id) { return OG.player.inv.count(id) + OG.player.storage.count(id); },
  pillars() {
    const P = OG.player, L = OG.Law, C = OG.C, R = OG.Residents;
    const food = this.owned('fleisch') + this.owned('gebratenes_fleisch'), wood = this.owned('holz'), iron = this.owned('eisenbarren'), deer = OG.Eco.stock('graslaeufer');
    const lawsOk = this.laws.length >= C.pillarLaws && this.hasLaw('freier_weg');
    return [
      { title: 'Würdig an Kraft (Art. 129 Nr. 3)', ok: P.level >= C.emperorLevel, detail: 'Stufe ' + P.level + ' / ' + C.emperorLevel + ' — die Stufe von Kaiser ' + C.emperorName },
      { title: 'Würdig an Seele (Art. 129 Nr. 2)', ok: !L.greatTreason && L.wanted === 0,
        detail: L.greatTreason ? 'Großer Verrat — das Recht ist für immer verwirkt.' : L.wanted > 0 ? 'Du wirst gesucht. Stelle dich zuerst dem Gericht.' : 'Kein Großer Verrat, keine offene Schuld. Einträge im Register: ' + L.record.length + '.' },
      { title: 'Säule der Ressourcen (Art. 131)', ok: P.taler >= C.pillarTaler && food >= C.pillarFood && wood >= C.pillarWood && iron >= C.pillarIron && deer >= C.pillarGameStock,
        detail: 'Kasse ' + P.taler + '/' + C.pillarTaler + ' Taler · Nahrung ' + food + '/' + C.pillarFood + ' · Holz ' + wood + '/' + C.pillarWood + ' · Eisenbarren ' + iron + '/' + C.pillarIron + ' · Wildbestand ' + Math.round(deer * 100) + ' % (mind. 50 %)' },
      { title: 'Säule des Volkes (Art. 132)', ok: R.allRespect(), detail: R.respecting() + ' / ' + R.list.length + ' Einwohner respektieren dich (alle müssen es, Respekt ≥ ' + C.respectThreshold + ')' },
      { title: 'Säule der Souveränität (Art. 133)', ok: this.guards >= C.pillarGuards && lawsOk && this.townHall,
        detail: 'Wachen ' + this.guards + '/' + C.pillarGuards + ' · Gesetzbuch ' + this.laws.length + '/' + C.pillarLaws + (this.hasLaw('freier_weg') ? '' : ' (Freier Weg fehlt)') + ' · Rathaus ' + (this.townHall ? 'errichtet' : 'fehlt') },
    ];
  },
  firstFail(withStrength) {
    for (const p of this.pillars()) { if (!withStrength && p.title.startsWith('Würdig an Kraft')) continue; if (!p.ok) return p.title + ': ' + p.detail; }
    return null;
  },
  canPetition() {
    if (this.state === 'Revision') return 'Die Revision läuft bereits.';
    if (this.state === 'Audienz') return 'Deine Audienz ist bereits bestimmt.';
    if (this.state === 'Vertrag') return 'Du bist bereits Herr der Autonomie ' + this.name + '.';
    const f = this.firstFail(true); if (f) return f;
    if (OG.player.taler < OG.C.petitionFee + OG.C.pillarTaler) return 'Die Petitionsgebühr (' + OG.C.petitionFee + ' Taler) darf die Säule der Ressourcen nicht unterschreiten.';
    return null;
  },
  submit() {
    const r = this.canPetition(); if (r) { OG.sys(r); return; }
    OG.player.spend(OG.C.petitionFee); OG.Treasury.add('Petitionsgebühren', OG.C.petitionFee);
    this.state = 'Revision'; this.revisionEnd = OG.Clock.day() + OG.C.revisionDays;
    OG.announce(OG.player.charName + ' reicht eine Petition des Aufstiegs für Eichenhain ein. Die Arbiter beginnen die Revision (Art. 134).');
  },
  audienceOpen() { const h = OG.Clock.hour(); return this.state === 'Audienz' && OG.Clock.day() === this.audienceDay && h >= this.audienceHour && h < this.audienceHour + 2; },
  reject(reason) { this.state = 'Keine'; this.rejectedDay = OG.Clock.day(); this.rejectReason = reason; OG.sys('Petition abgelehnt (Art. 139): ' + reason + ' Eine neue Petition ist möglich, sobald der Grund behoben ist.'); },
  gaze() {
    const f = this.firstFail(true); if (f) return f;
    if (OG.Clock.day() - OG.Law.lastCrimeDay < 7) return 'Der Kaiser sieht frisches Unrecht an deinen Händen (ein Vergehen in den letzten 7 Tagen).';
    if (OG.Residents.avg() < 60) return 'Der Kaiser sieht Zweifel in deinem Volk (durchschnittlicher Respekt unter 60).';
    return null;
  },
  sign(name, color) {
    this.name = (name || '').trim() || 'Autonomie Eichenhain'; this.color = color; this.state = 'Vertrag'; this.warningUntil = -1;
    for (const g of this.guardNpcs) g.removed = true;
    this.guardNpcs = []; for (let i = 0; i < this.guards; i++) this.spawnGuard(i);
    OG.announce('Der Vertrag ist mit dem Großen Siegel der Genesis besiegelt (Art. 136). ' + OG.player.charName + ' ist Herr der Autonomie „' + this.name + '“. Der Kontinent bleibt eins.');
    OG.UI.banner(this.name.toUpperCase(), this.colors[this.color]);
    OG.save();
  },
  onDay(day) {
    if (this.guards > 0 && !OG.player.spend(this.guards * OG.C.guardUpkeep)) {
      this.guards--; const g = this.guardNpcs.pop(); if (g) g.removed = true;
      OG.sys('Eine Dorfwache hat Eichenhain verlassen — ihr Sold (' + OG.C.guardUpkeep + ' Taler/Tag) wurde nicht bezahlt.');
    }
    if (this.state === 'Revision') {
      const f = this.firstFail(true);
      if (f) { this.reject('Bei der Revision fiel eine Säule: ' + f); return; }
      if (day >= this.revisionEnd) {
        this.state = 'Audienz'; this.audienceDay = day + 1; this.audienceHour = 12;
        OG.sys('Richter Ataklas bestätigt die Drei Säulen. Audienz bei Kaiser ' + OG.C.emperorName + ': Tag ' + this.audienceDay + ', 12–14 Uhr, im Thronsaal. Wer nicht erscheint, verliert die Petition (Art. 135).');
      }
    } else if (this.state === 'Audienz' && day > this.audienceDay) this.reject('Du bist nicht zur Audienz erschienen (Art. 135).');
    else if (this.state === 'Vertrag') {
      const f = this.firstFail(false);
      if (!f) {
        if (this.warningUntil >= 0) OG.sys('Die Säulen stehen wieder. Die Vorwarnung des Throns ist aufgehoben.');
        this.warningUntil = -1;
        const income = OG.Residents.list.length * 12, share = Math.round(income * 0.1);
        OG.player.taler += income - share; OG.Treasury.add('Anteil der Autonomien', share);
      } else if (this.warningUntil < 0) {
        this.warningUntil = day + OG.C.treatyWarningDays;
        OG.announce('Vorwarnung des Throns an ' + this.name + ' (Art. 140): ' + f + ' Frist bis Tag ' + this.warningUntil + '.');
      } else if (day >= this.warningUntil) {
        OG.announce('Der Vertrag mit ' + this.name + ' ist aufgelöst (Art. 140 Nr. 4). Die Lande kehren in die Kronendomäne zurück.');
        this.state = 'Keine'; this.warningUntil = -1; this.name = '';
      }
    }
  },
  hireGuard() {
    if (this.guards >= OG.C.maxGuards) { OG.sys('Mehr Wachen braucht Eichenhain nicht.'); return; }
    if (!OG.player.spend(OG.C.guardHireCost)) { OG.sys('Nicht genug Taler (' + OG.C.guardHireCost + ').'); return; }
    this.spawnGuard(this.guards); this.guards++;
    OG.sys('Dorfwache angeworben (' + this.guards + '/' + OG.C.pillarGuards + '). Sold: ' + OG.C.guardUpkeep + ' Taler pro Tag.');
  },
  spawnGuard(i) {
    const V = OG.Places.village, a = i * Math.PI / 3;
    const col = this.isAutonomous() ? this.colors[this.color] : '#596648';
    const g = new OG.Npc('Dorfwache', 'Dorfwache ' + (i + 1), 'im Dienst von ' + OG.player.charName, V.x + Math.cos(a) * 11, V.y + Math.sin(a) * 11, a, col, '#4d4d52');
    g.route = [0, 1, 2, 3].map(k => ({ x: V.x + Math.cos(a + k * Math.PI / 2) * 18, y: V.y + Math.sin(a + k * Math.PI / 2) * 18 }));
    this.guardNpcs.push(g);
  },
  toggleLaw(id) { if (this.hasLaw(id)) this.laws = this.laws.filter(l => l !== id); else this.laws.push(id); },
  buildTownHall() {
    const p = OG.player;
    if (this.townHall) return;
    if (p.inv.count('holz') < 30 || p.inv.count('stein') < 20 || p.inv.count('eisenbarren') < 10) { OG.sys('Für das Rathaus brauchst du 30 Holz, 20 Stein und 10 Eisenbarren im Inventar.'); return; }
    p.inv.remove('holz', 30); p.inv.remove('stein', 20); p.inv.remove('eisenbarren', 10);
    this.townHall = true; this.buildHall(); OG.Residents.changeAll(5);
    OG.sys('Das Rathaus von Eichenhain steht. Die Einwohner sind beeindruckt (+5 Respekt).');
  },
  buildHall() {
    if (this.hallBuilt) return;
    this.hallBuilt = true;
    const V = OG.Places.village;
    const b = OG.World.hall('Rathaus von Eichenhain', V.x, V.y + 1.5, 10, 7, 'N', 2.2, '#4d3826', '#735438');
    b.furniture.push({ type: 'desk', x0: V.x - 1.5, y0: V.y + 3.2, x1: V.x + 1.5, y1: V.y + 3.8 });
    this.siteSpot.y = V.y - 3.2;
  },
  stateText() {
    switch (this.state) {
      case 'Revision': return 'Revision durch die Arbiter läuft bis Tag ' + this.revisionEnd + '.';
      case 'Audienz': return 'Audienz beim Kaiser: Tag ' + this.audienceDay + ', 12–14 Uhr im Thronsaal.';
      case 'Vertrag': return 'Autonomie „' + this.name + '“ besiegelt.' + (this.warningUntil >= 0 ? ' VORWARNUNG bis Tag ' + this.warningUntil + '!' : '');
      default: return OG.Clock.day() - this.rejectedDay < 30 && this.rejectReason ? 'Letzte Petition abgelehnt: ' + this.rejectReason : 'Keine Petition eingereicht.';
    }
  },
};

// ---------- Prüfung der Meisterschaft (Art. 48) ----------
OG.Trial = {
  active: false, wave: 0, start: 0, taken: 0, golems: [],
  begin() {
    const P = OG.player, A = OG.Places.arena;
    this.active = true; this.wave = 0; this.taken = 0; this.start = OG.time;
    for (const c of OG.Places.arenaGate) c.on = true;
    P.x = A.x + 8; P.y = A.y; P.vx = P.vy = 0;
    this.hpStart = P.hp;
    OG.sys('Die Prüfung der Meisterschaft beginnt. Drei Wellen Prüfungsgolems. Zeit und Treffer werden gewertet.');
    this.spawnWave();
  },
  spawnWave() {
    const d = OG.Species.pruefungsgolem, lvl = OG.player.level, n = 2 + this.wave, A = OG.Places.arena;
    for (let i = 0; i < n; i++) {
      const a = i / n * Math.PI * 2;
      const g = new OG.Creature(d, A.x - 3 + Math.cos(a) * 8, A.y + Math.sin(a) * 8, { faction: 'Construct', hpMult: (60 + lvl * 18) / d.hp, dmgMult: 1 + lvl * 0.12, expMult: 0 });
      this.golems.push(g);
    }
    OG.UI.banner('WELLE ' + (this.wave + 1) + ' / 3', '#99ccff');
  },
  onHit(d) { if (this.active) this.taken += d; },
  update() {
    if (!this.active) return;
    this.golems = this.golems.filter(g => !g.dead && !g.removed);
    if (this.golems.length) return;
    this.wave++;
    if (this.wave >= 3) this.finish(true); else this.spawnWave();
  },
  playerDowned() { if (this.active) this.finish(false); },
  finish(ok) {
    const P = OG.player;
    this.active = false;
    for (const g of this.golems) g.removed = true;
    this.golems = [];
    for (const c of OG.Places.arenaGate) c.on = false;
    const time = OG.time - this.start;
    let perf;
    if (!ok) perf = this.wave - 1;
    else {
      perf = 3 + (time < 40 ? 3 : time < 70 ? 2 : time < 110 ? 1 : 0);
      const frac = this.taken / P.maxHp; perf += frac < 0.2 ? 2 : frac < 0.6 ? 1 : 0;
    }
    const cap = OG.Ranks.cap(P.level), rank = Math.min(perf, cap);
    const detail = 'Zeit ' + Math.round(time) + ' s, erlittener Schaden ' + Math.round(this.taken) + '. Leistung: ' + OG.Ranks.name(perf) + ', Stufengrenze: ' + OG.Ranks.name(cap) + '.';
    if (rank < 0) OG.sys('Prüfung nicht bestanden — nicht einmal die erste Welle wurde besiegt. ' + detail);
    else if (rank > P.rank) {
      P.rank = rank;
      OG.sys('Rang ' + OG.Ranks.name(rank) + ' erhalten' + (rank >= 6 ? ' — verliehen durch Kaiser ' + OG.C.emperorName + ' (Art. 69)' : '') + '. ' + detail);
      OG.UI.banner('RANG ' + OG.Ranks.name(rank), OG.Ranks.color(rank));
      if (rank >= 6) OG.announce(P.charName + ' wurde in den Rang ' + OG.Ranks.name(rank) + ' des Aufstiegs erhoben.');
    } else OG.sys('Prüfung beendet. Dein Rang bleibt ' + OG.Ranks.name(P.rank) + '. ' + detail);
    P.x = OG.Places.trialMaster.x + 2; P.y = OG.Places.trialMaster.y; P.vx = P.vy = 0;
  },
};

// ---------- Aufträge: Hauptgeschichte, Nebenaufträge, Gildenaufträge (Art. 121) ----------
OG.QuestSys = {
  active: [], done: [], offered: [], contracts: {}, title: '', contractDay: 0, counter: 0, spawned: {}, pollT: 0, autoT: 0,
  init(s) {
    s = s || {};
    this.active = []; this.done = (s.done || []).slice(); this.title = s.title || ''; this.contractDay = s.contractDay || 0; this.counter = s.counter || 0;
    this.contracts = {}; this.offered = []; this.spawned = {};
    for (const c of s.contracts || []) this.contracts[c.id] = { info: c, def: this.buildContract(c) };
    for (const q of s.active || []) { const def = this.find(q.id); if (def && q.step < def.steps.length) this.active.push({ def, step: q.step, progress: q.progress }); }
    for (const c of s.contracts || []) if (c.offered && !this.isActive(c.id) && !this.done.includes(c.id)) this.offered.push(this.contracts[c.id].def);
    for (const st of this.active.slice()) this.beginStep(st, true);
    if (this.contractDay !== OG.Clock.day() || (!this.offered.length && !this.activeContracts())) this.refreshContracts(OG.Clock.day());
    OG.Clock.onDay.push(d => this.refreshContracts(d));
    this.autoStart();
  },
  save() {
    const contracts = [];
    for (const id in this.contracts) {
      const rel = this.isActive(id) || this.offered.some(q => q.id === id);
      if (rel) contracts.push(Object.assign({}, this.contracts[id].info, { offered: !this.isActive(id) }));
    }
    return { active: this.active.map(s => ({ id: s.def.id, step: s.step, progress: s.progress })), done: this.done, title: this.title, contractDay: this.contractDay, counter: this.counter, contracts };
  },
  find(id) { return OG.quest(id) || (this.contracts[id] ? this.contracts[id].def : null); },
  isActive(id) { return this.active.some(s => s.def.id === id); },
  activeContracts() { return this.active.filter(s => s.def.contract).length; },
  available(q) {
    if (q.contract || this.isActive(q.id) || this.done.includes(q.id)) return false;
    if (OG.player.level < (q.minLevel || 1)) return false;
    return !q.requires || this.done.includes(q.requires);
  },
  nextHint() {
    for (const q of OG.Quests) {
      if (!q.main || this.done.includes(q.id) || this.isActive(q.id)) continue;
      if (q.requires && !this.done.includes(q.requires)) return null;
      return q.title + ' — ab Stufe ' + q.minLevel + ', bei ' + q.giver;
    }
    return null;
  },
  step(s) { return s.def.steps[s.step] || null; },
  progressText(s) {
    const st = this.step(s); if (!st) return '';
    if (st.type === 'Kill' || st.type === 'Investigate') return ' (' + s.progress + '/' + st.count + ')';
    if (st.type === 'Deliver') return ' (' + OG.player.inv.count(st.target) + '/' + st.count + ')';
    if (st.type === 'Population') return ' (' + OG.Eco.count(st.target) + '/' + st.count + ')';
    if (st.type === 'Seal') return ' (' + Math.round(OG.Rift.strength) + '/' + st.count + ' %)';
    return '';
  },
  // Die Geschichte ist freiwillig: Kapitel bieten ihre Auftraggeber an, nichts startet von selbst.
  autoStart() {},
  offersFrom(npc) { return OG.Quests.filter(q => q.giver === npc && this.available(q) && (!q.main || !this.active.some(s => s.def.main))); },
  hasOffer(npc) {
    if (npc === 'Runa' && this.offered.length && this.activeContracts() < 3) return true;
    return this.offersFrom(npc).length > 0;
  },
  hasTurnIn(npc) { return this.active.some(s => { const st = this.step(s); return st && ((st.type === 'Talk' && st.target === npc) || (st.type === 'Deliver' && st.npc === npc)); }); },
  talkSteps(npc) { return this.active.filter(s => { const st = this.step(s); return st && st.type === 'Talk' && st.target === npc; }); },
  deliverSteps(npc) { return this.active.filter(s => { const st = this.step(s); return st && st.type === 'Deliver' && st.npc === npc; }); },
  accept(q) {
    if (q.contract) {
      if (this.activeContracts() >= 3) { OG.sys('Höchstens 3 Gildenaufträge gleichzeitig.'); return; }
      this.offered = this.offered.filter(x => x !== q);
    }
    const s = { def: q, step: 0, progress: 0 };
    this.active.push(s);
    const P = OG.player;
    if (q.grantPermit && !P.permit) { P.permit = true; P.inv.add('erlaubnis', 1); OG.sys('Richter Ataklas stellt dir eine Sondererlaubnis für die Verbotenen Lande aus (Art. 112).'); }
    OG.sys((q.main ? 'Neues Kapitel: ' : 'Neuer Auftrag: ') + q.title);
    if (q.main) OG.UI.banner(q.title.toUpperCase(), '#f2c14d');
    this.beginStep(s, false);
  },
  beginStep(s, resume) {
    if (!resume) s.progress = 0;
    const st = this.step(s);
    if (!st) return this.complete(s);
    this.clearSpawns(s.def.id);
    if (st.spawn) this.spawn(s, st, resume);
    if (!resume) OG.post('[Auftrag] ' + s.def.title + ': ' + st.text, 'sys');
  },
  advance(s) {
    const st = this.step(s), P = OG.player;
    if (st && st.give) for (const [id, n] of st.give) { P.inv.add(id, n); OG.log('+' + n + ' ' + OG.itemName(id)); }
    s.step++; s.progress = 0;
    if (s.step >= s.def.steps.length) this.complete(s); else this.beginStep(s, false);
  },
  complete(s) {
    const q = s.def, P = OG.player;
    this.active = this.active.filter(x => x !== s);
    if (!this.done.includes(q.id)) this.done.push(q.id);
    this.clearSpawns(q.id);
    const lvl = P.level;
    if (q.expLevels) P.gainExp(q.expLevels * OG.Player.expToNext(P.level) / (1 + P.race.expBonus));
    const taler = q.hook === 'auftrag' ? 40 + 10 * lvl : (q.taler || 0);
    P.taler += taler;
    for (const [id, n] of q.items || []) P.inv.add(id, n);
    if (q.respect) OG.Residents.changeAll(q.respect);
    if (q.titleReward) this.title = q.titleReward;
    if (q.hook === 'markt_fleisch' && OG.Market.goods.fleisch) OG.Market.goods.fleisch.stock += 10;
    if (q.hook === 'finale') OG.announce('Die Meuterei von Haus Vorn ist niedergeschlagen. ' + P.charName + ' wird „' + q.titleReward + '“ genannt.');
    let rew = taler + ' Taler';
    for (const [id, n] of q.items || []) rew += ', ' + n + '× ' + OG.itemName(id);
    if (q.respect) rew += ', Respekt in Eichenhain +' + q.respect;
    if (q.titleReward) rew += ', Titel „' + q.titleReward + '“';
    OG.sys((q.main ? 'Kapitel abgeschlossen: ' : 'Auftrag erfüllt: ') + q.title + '. Belohnung: ' + rew + '.');
    if (q.done) OG.sys(q.done);
    OG.UI.banner(q.main ? 'KAPITEL ABGESCHLOSSEN' : 'AUFTRAG ERFÜLLT', '#73eb8c');
    if (q.main) OG.save();
    this.autoStart();
  },
  onKill(species) {
    for (const s of this.active.slice()) {
      const st = this.step(s);
      if (!st || st.type !== 'Kill' || st.target !== species) continue;
      s.progress++; OG.log(s.def.title + ': ' + s.progress + '/' + st.count);
      if (s.progress >= st.count) this.advance(s);
    }
  },
  investigate(clue) {
    for (const s of this.active.slice()) {
      const st = this.step(s);
      if (!st || st.type !== 'Investigate' || st.target !== clue.clueId) continue;
      s.progress++; OG.sys(clue.finding); clue.remove();
      const list = this.spawned[s.def.id]; if (list) { const i = list.indexOf(clue); if (i >= 0) list.splice(i, 1); }
      if (s.progress >= st.count) this.advance(s);
      return;
    }
  },
  completeTalk(s) { const st = this.step(s), line = st ? st.line : ''; this.advance(s); return line; },
  deliver(s) {
    const st = this.step(s), P = OG.player;
    if (!st || P.inv.count(st.target) < st.count) return false;
    P.inv.remove(st.target, st.count); this.advance(s); return true;
  },
  update(dt) {
    this.autoT -= dt; this.pollT -= dt;
    if (this.autoT <= 0) { this.autoT = 2; this.autoStart(); }
    if (this.pollT > 0) return;
    this.pollT = 0.5;
    const P = OG.player;
    for (const s of this.active.slice()) {
      const st = this.step(s); if (!st) continue;
      let ok = false;
      if (st.type === 'Reach') { const p = this.place(st.place); ok = Math.hypot(P.x - p.x, P.y - p.y) <= st.radius && (!st.night || OG.Clock.isNight()); }
      else if (st.type === 'Seal') ok = OG.Rift.strength >= st.count;
      else if (st.type === 'Rank') ok = P.rank >= st.count;
      else if (st.type === 'Population') ok = OG.Eco.count(st.target) >= st.count;
      if (ok) this.advance(s);
    }
  },
  // ---------- Gildenaufträge ----------
  refreshContracts(day) {
    for (const q of this.offered) delete this.contracts[q.id];
    this.offered = []; this.contractDay = day;
    const cand = [];
    for (const id in OG.Species) {
      const d = OG.Species[id];
      if (!d.ecosystem || OG.Eco.stock(id) < 0.7) continue;
      cand.push({ kind: 0, target: id, count: OG.clamp(Math.round(OG.Eco.count(id) * 0.2), 1, 3) });
    }
    const goods = [['holz', 10], ['stein', 8], ['eisenerz', 6], ['fell', 4], ['heilkraut', 8], ['eisenbarren', 3], ['fleisch', 6]];
    for (let i = 0; i < 2; i++) { const g = OG.pick(goods); cand.push({ kind: 1, target: g[0], count: g[1] }); }
    if (OG.Rift.strength < 70) cand.push({ kind: 2, target: 'leerenbrut', count: 3 });
    for (let i = 0; i < 3 && cand.length; i++) {
      const c = cand.splice(Math.floor(Math.random() * cand.length), 1)[0];
      c.id = 'auftrag_' + (++this.counter);
      const def = this.buildContract(c);
      this.contracts[c.id] = { info: c, def };
      this.offered.push(def);
    }
  },
  buildContract(c) {
    const q = { id: c.id, contract: true, giver: 'Runa', expLevels: OG.C.contractExpLevels, hook: 'auftrag' };
    if (c.kind === 0) {
      const name = OG.Species[c.target] ? OG.Species[c.target].name : c.target;
      q.title = 'Jagdauftrag: ' + c.count + '× ' + name;
      q.summary = 'Die Wildhüter geben die Jagd frei, solange der Bestand gesund ist (über 70 %). Jedes erlegte Tier fehlt danach im Ökosystem.';
      q.steps = [{ type: 'Kill', target: c.target, count: c.count, text: 'Erlege ' + c.count + '× ' + name }];
    } else if (c.kind === 1) {
      q.title = 'Lieferauftrag: ' + c.count + '× ' + OG.itemName(c.target);
      q.summary = 'Die Gilde braucht Vorräte. Liefere sie bei Gildenmeisterin Runa ab.';
      q.steps = [{ type: 'Deliver', target: c.target, count: c.count, npc: 'Runa', place: 'gildenhaus', text: 'Liefere ' + c.count + '× ' + OG.itemName(c.target) + ' an Runa' }];
    } else {
      q.title = 'Risswache: ' + c.count + '× Leerenbrut';
      q.summary = 'Das Siegel ist schwach. Der Orden zahlt über die Gilde für jede besiegte Leerenbrut.';
      q.steps = [{ type: 'Kill', target: 'leerenbrut', count: c.count, place: 'riss', text: 'Besiege ' + c.count + '× Leerenbrut' }];
    }
    return q;
  },
  // ---------- Orte und Ereignisse der Geschichte ----------
  place(key) {
    const P = OG.Places;
    return {
      kanzlei: P.judge, thronsaal: P.emperor, arena: P.arena, grenzposten: P.borderL, kraterrand: P.craterRim, siegel: P.seal,
      riss: P.rift, eichenhain: P.village, waldrand: P.forestEdge, lager: P.camp, marktplatz: P.plaza, schmiede: P.smith,
      lazarett: P.healer, gildenhaus: P.guildMaster, erzhoehen: OG.L.hills,
    }[key] || null;
  },
  track(id, o) { (this.spawned[id] || (this.spawned[id] = [])).push(o); },
  clearSpawns(id) {
    for (const o of this.spawned[id] || []) { if (o instanceof OG.Spot) o.remove(); else o.removed = true; }
    this.spawned[id] = [];
  },
  around(c, i, n, r) { const a = i / Math.max(1, n) * Math.PI * 2 + 0.4; return { x: c.x + Math.cos(a) * r, y: c.y + Math.sin(a) * r }; },
  spawn(s, st, resume) {
    const left = Math.max(0, st.count - s.progress), id = s.def.id, P = OG.Places;
    const clue = (cid, label, p, finding) => {
      const sp = new OG.Spot(p.x, p.y, 2.2, () => 'Untersuchen: ' + label, () => this.investigate(sp));
      Object.assign(sp, { clueId: cid, finding, clue: cid });
      this.track(id, sp);
    };
    switch (st.spawn) {
      case 'splitter': for (let i = 0; i < left; i++) clue('runensplitter', 'Runensplitter', this.around(P.craterRim, i, 3, 9), 'Ein Splitter mit eingeritzten Runen. Sie pulsieren im Takt des Risses — jemand hat sie hier vergraben.'); break;
      case 'brandzeichen': for (let i = 0; i < left; i++) clue('brandzeichen', 'Brandzeichen', this.around(P.forestEdge, i, 3, 8), 'Ein eingebranntes Zeichen im Boden. Daneben Fußspuren, die tiefer in den Wald führen.'); break;
      case 'brief': clue('brief', 'Verkohltes Lagerfeuer', { x: P.forestEdge.x + 4, y: P.forestEdge.y - 6 }, 'Ein halb verbrannter Brief mit dem Wappen von Haus Vorn: „… nach dem Ritual gehört Grünmark wieder uns … Lager in den Erzhöhen …“'); break;
      case 'ritualplan': clue('ritualplan', 'Selas Zelt', { x: P.camp.x, y: P.camp.y - 4 }, 'Ein Ritualplan: In einer Nacht soll das Siegel am Riss gebrochen werden. Unterzeichnet: Kasimir Vorn.'); break;
      case 'kult_wald': for (let i = 0; i < left; i++) this.foe(id, 'kultist', this.around(P.forestEdge, i, left, 6)); break;
      case 'kult_lager': for (let i = 0; i < left; i++) this.foe(id, 'kultist', this.around(P.camp, i, left, 9)); break;
      case 'sela': if (left > 0) this.foe(id, 'kultmeisterin', { x: P.camp.x, y: P.camp.y - 2 }); break;
      case 'ritual':
        if (left <= 0) break;
        this.foe(id, 'kasimir', { x: P.rift.x, y: P.rift.y + 8 }).summonInterval = 18;
        for (let i = 0; i < 3; i++) this.foe(id, 'kultist', this.around({ x: P.rift.x, y: P.rift.y + 8 }, i, 3, 7));
        if (!resume) { OG.Rift.weaken(30); OG.announce('In der Nacht flammt der Riss auf. Kasimir Vorn beginnt das Ritual — eine Meuterei gegen den Thron (Art. 141)!'); }
        break;
    }
  },
  foe(id, species, p) {
    const lvl = OG.player.level;
    const c = new OG.Creature(OG.Species[species], p.x, p.y, { hpMult: 0.8 + lvl * 0.05, dmgMult: 0.8 + lvl * 0.04, faction: 'Void' });
    c.questTag = id; c.home = { x: p.x, y: p.y };
    this.track(id, c);
    return c;
  },
};
