// Omni Genesis 2D — Spielschleife, Weltaufbau beim Laden, Spielstand.
'use strict';

OG.running = false;
OG.rentUntil = 0;
OG.gates = [];
OG.current = null;
OG.blocked = () => !OG.running || OG.UI.isOpen() || (OG.player && OG.player.downed);

// ---------- Neues Spiel oder Spielstand laden ----------
OG.start = data => {
  OG.running = false;
  OG.UI.close();
  OG.UI.showLoading(0, 'Lade Welt…');
  const gen = OG.generate(data || {});
  const step = () => {
    let r;
    try { r = gen.next(); } catch (e) { console.error(e); OG.UI.showLoading(1, 'Fehler beim Aufbau der Welt: ' + e.message); return; }
    if (r.done) { OG.UI.hideLoading(); OG.running = true; OG.lastFrame = performance.now(); return; }
    OG.UI.showLoading(r.value[0], r.value[1]);
    requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
};

OG.generate = function* (d) {
  OG.ents = []; OG.projectiles = []; OG.interactables = []; OG.gates = []; OG.current = null;
  OG.FX = { rings: [], parts: [], discs: [], slashes: [], floaters: [] };
  OG.messages = []; OG.combatLog = []; OG.announcements = (d.announcements || []).slice();
  OG.time = 0;
  OG.World.reset(); OG.Nodes.reset(); OG.Eco.reset();
  yield [0.02, 'Erwecke den Thron der Genesis…'];
  OG.buildFields();
  yield [0.1, 'Vermesse das Land…'];
  for (const p of OG.buildGround()) yield [0.1 + p * 0.5, 'Zeichne das Land…'];
  OG.buildWorld();
  yield [0.64, 'Errichte Grad Genesis…'];
  OG.plantWorld();
  yield [0.7, 'Pflanze die Wälder…'];

  OG.Clock.init(d.minutes != null ? d.minutes : 480);
  OG.Weather.init(d.weather || 0);
  OG.Treasury.load(d.treasury);
  OG.Market.init(d.market);
  OG.Law.load(d.law);
  OG.Rift.init(d.rift);
  OG.Nodes.load(d.nodes);
  OG.Clock.onHour.push(() => OG.Nodes.onHour());
  OG.rentUntil = d.rentUntil || 0;
  OG.statics();
  yield [0.78, 'Rufe die Bürger des Reiches…'];
  OG.spawnNpcs();
  const pd = Object.assign({}, d.player || {});
  if (pd.x == null || pd.y == null) { pd.x = OG.Places.cityGate.x; pd.y = OG.Places.cityGate.y + 7; }
  OG.player = new OG.Player(pd);
  OG.ents.push(OG.player);
  OG.cam.x = OG.player.x; OG.cam.y = OG.player.y;
  OG.cam.zoom = OG.Input.touch ? 24 : 32;
  yield [0.84, 'Eichenhain erwacht…'];
  OG.Residents.init(d.residents);
  OG.Autonomy.init(d.autonomy);
  yield [0.9, 'Siedle die Wildtiere an…'];
  OG.Eco.init(d.worldInit ? d.eco : null);
  if (pd.companion && OG.Species[pd.companion]) {
    const c = new OG.Creature(OG.Species[pd.companion], OG.player.x + 2, OG.player.y, { age: 48 });
    c.tame(true);
  }
  yield [0.96, 'Die Geschichte beginnt…'];
  OG.QuestSys.init(d.quests);
  OG.pvp = Object.assign({ kills: 0, innocents: 0, outlaws: 0, duelsWon: 0, duelsLost: 0, deaths: 0 }, d.pvp || {});
  OG.duel = null;
  OG.drawHooks = OG.drawHooks.filter(f => f.keep);
  OG.Rivals.init(d.rivals);
  OG.Secrets.init(d.secrets);
  OG.Clock.onDay.push(() => OG.save(true));
  if (!d.worldInit) {
    OG.sys('Willkommen in Grad Genesis, ' + OG.player.charName + '. Die Geschichte ist freiwillig: Richter Ataklas in der Kanzlei (!) hat das erste Kapitel. Du kannst aber auch einfach leveln, jagen und dich mit anderen Reisenden messen.');
    OG.sys('Ziel nach Art. 127: eine eigene Autonomie. Werde so stark wie der Kaiser (Stufe 100) und gewinne den Respekt aller Einwohner von Eichenhain (P).');
    OG.announce('Der Thron der Genesis grüßt alle Reisenden. Ein Kontinent. Ein Thron. Ein Gesetz.');
  } else OG.sys('Willkommen zurück, ' + OG.player.charName + '. ' + OG.Clock.stamp() + '.');
  yield [1, 'Fertig'];
};

// Orte mit [F]: Bett, Truhe, Amboss, Tore, Auftragsbrett.
OG.statics = () => {
  const P = OG.Places;
  new OG.Spot(P.bed.x, P.bed.y, 1.8, () => OG.rentUntil >= OG.Clock.day() ? 'Schlafen bis 6:00 Uhr' : 'Bett (nur mit gemietetem Zimmer)', () => {
    if (OG.rentUntil < OG.Clock.day()) { OG.sys('Miete zuerst ein Zimmer bei Henrike vor dem Haus (Art. 116).'); return; }
    if (OG.Law.wanted > 0) { OG.sys('Gesuchte finden keinen Schlaf. Stelle dich dem Gericht (G).'); return; }
    OG.Clock.skipTo(6); const p = OG.player; p.hp = p.maxHp; p.aether = p.maxAether; OG.save(true);
    OG.sys('Du hast geschlafen. ' + OG.Clock.stamp() + '. Spiel gespeichert.');
  });
  new OG.Spot(P.chest.x, P.chest.y, 1.6, () => 'Truhe öffnen', () => {
    if (OG.rentUntil < OG.Clock.day()) { OG.sys('Die Truhe gehört zum Mietzimmer. Miete zuerst ein Zimmer.'); return; }
    OG.UI.openPanel('storage');
  });
  new OG.Spot(P.anvil.x, P.anvil.y, 1.8, () => 'Schmieden (Amboss)', () => OG.UI.openPanel('craft'));
  for (const [name, p] of [['Grad Genesis', P.cityGate], ['Eichenhain', P.villageGate], ['Grenzposten', P.borderGate]]) {
    OG.gates.push({ name, x: p.x, y: p.y });
    new OG.Spot(p.x, p.y, 4, () => 'Großes Tor ' + name, () => OG.UI.openPanel('gate', name));
  }
  new OG.Spot(60.4, 116.4, 2, () => 'Auftragsbrett der Gilde', () => { const r = OG.ents.find(e => e.npcName === 'Runa'); if (r) OG.UI.openDialogue(r); });
};

OG.spawnNpcs = () => {
  const P = OG.Places, gold = '#f2c14d', azure = '#14244f', N = (...a) => new OG.Npc(...a);
  N('Kaiser', OG.C.emperorName, 'Oberster Kaiser', P.emperor.x, P.emperor.y, P.emperorFace, '#14142a', '#0a0a14');
  N('Richter', 'Ataklas', 'Oberster Arbiter des Throns', P.judge.x, P.judge.y, P.judgeFace, '#59595f', '#2e2e33');
  N('Haendler', 'Mirela', 'Händlerin', P.merchantA.x, P.merchantA.y, 0, '#993333', '#4d3326');
  N('Haendler', 'Oskar', 'Händler', P.merchantB.x, P.merchantB.y, Math.PI, '#336699', '#4d3326');
  N('Schmied', 'Gundram', 'Schmied', P.smith.x, P.smith.y, 2.8, '#59402e', '#333333');
  N('Vermieter', 'Henrike', 'Vermieterin', P.landlord.x, P.landlord.y, 0, '#80734d', '#4d4033');
  N('Heiler', 'Schwester Alwine', 'Heilerin des Lazaretts', P.healer.x, P.healer.y, Math.PI, '#f2f2f2', '#b33333');
  N('Pruefungsmeister', 'Brakka', 'Prüfungsmeisterin', P.trialMaster.x, P.trialMaster.y, 0, '#4d5980', gold);
  N('Gildenmeister', 'Runa', 'Gildenmeisterin der Abenteurer', P.guildMaster.x, P.guildMaster.y, Math.PI, '#4d734d', gold);
  ['Varus', 'Tilda', 'Marek', 'Odila', 'Konrad', 'Sabin'].forEach((n, i) => {
    const route = P.cityPatrol.map((_, k) => P.cityPatrol[(k + i * 2) % P.cityPatrol.length]);
    N('Wache', 'Legionär ' + n, 'Kaiserliche Legion', route[0].x, route[0].y, 0, azure, '#b3b8bf').route = route;
  });
  ['Edda', 'Florian', 'Gisela', 'Hagen', 'Irmgard', 'Jost', 'Liesel', 'Ruprecht'].forEach((n, i) => {
    const route = [0, 1, 2, 3].map(k => P.cityWalk[(i + k * 3) % P.cityWalk.length]);
    N('Buerger', n, 'Bürger von Grad Genesis', route[0].x, route[0].y, 0, OG.hsl2hex((i * 61) % 360, 0.4, 0.5), '#4d4033').route = route;
  });
  N('Ordensmagier', 'Magistra Velde', 'Orden der Kampfmagier', P.borderL.x, P.borderL.y, Math.PI / 2, '#592673', gold);
  N('Ordensmagier', 'Magister Aurel', 'Orden der Kampfmagier', P.borderR.x, P.borderR.y, Math.PI / 2, '#592673', gold);
  N('Kraemer', 'Wendelin', 'Krämer von Eichenhain', P.villageTrader.x, P.villageTrader.y, Math.PI, '#736640', '#4d4033');
  N('Soeldnerfuehrer', 'Hedda', 'Söldnerführerin', P.mercenary.x, P.mercenary.y, Math.PI, '#4d5247', '#80808a');
};

// ---------- Niederlage (Art. 45) ----------
OG.onPlayerDowned = () => {
  const P = OG.player;
  if (OG.Trial.active) { P.hp = P.maxHp * 0.3; OG.Trial.playerDowned(); return; }
  if (OG.Law.wanted > 0) { P.hp = P.maxHp * 0.5; OG.Law.arrest(); return; }
  let lost = Math.round(P.taler * 0.1);
  P.taler -= lost;
  lost = OG.Rivals.onPlayerDowned(P, lost); P.hp = P.maxHp; P.aether = P.maxAether; P.vx = P.vy = 0;
  OG.Clock.advance(120);
  P.x = OG.Places.lazarett.x; P.y = OG.Places.lazarett.y;
  OG.sys('Du bist gefallen. Die Heiler des Kaiserlichen Lazaretts haben dich gerettet (Art. 45). Verlust: ' + lost + ' Taler.');
};

// ---------- Spielstand ----------
OG.buildSave = () => {
  const P = OG.player;
  return {
    v: 1, worldInit: true, minutes: OG.Clock.minutes, weather: OG.Weather.cur, rentUntil: OG.rentUntil, announcements: OG.announcements,
    player: {
      name: P.charName, race: P.race.id, classId: P.cls.id, secondClassId: P.second ? P.second.id : '', level: P.level, exp: P.exp, alloc: P.alloc, free: P.free,
      taler: P.taler, inventory: P.inv.items, storage: P.storage.items, weapon: P.weapon, armor: P.armorItem, rank: P.rank, permit: P.permit, demo: P.demo,
      hp: P.hp, aether: P.aether, x: OG.Trial.active ? null : P.x, y: OG.Trial.active ? null : P.y, companion: P.companion && !P.companion.dead ? P.companion.def.id : '',
    },
    eco: OG.Eco.save(), nodes: OG.Nodes.save(), market: OG.Market.save(), treasury: OG.Treasury.save(), law: OG.Law.save(), rift: OG.Rift.save(),
    residents: OG.Residents.save(), autonomy: OG.Autonomy.save(), quests: OG.QuestSys.save(),
    rivals: OG.Rivals.save(), pvp: OG.pvp, secrets: OG.Secrets.save(),
  };
};
OG.save = silent => {
  if (!OG.player || !OG.running && silent) return;
  const ok = OG.Store.save(OG.buildSave());
  if (!silent) OG.sys(ok ? 'Spiel gespeichert (in diesem Browser).' : 'Speichern ist in diesem Browser nicht möglich.');
};

// ---------- Spielschleife ----------
OG.update = dt => {
  const P = OG.player, I = OG.Input;
  OG.Clock.update(dt);
  OG.Weather.update(dt);
  OG.Law.update(dt);
  OG.Trial.update();
  OG.QuestSys.update(dt);
  OG.Rivals.update();
  OG.Secrets.update(dt);

  for (const e of OG.ents) if (!e.removed) e.update(dt);
  // Figuren schieben sich sanft auseinander
  const mov = OG.ents.filter(e => !e.removed && !e.dead && !e.corpse);
  for (let i = 0; i < mov.length; i++) for (let j = i + 1; j < mov.length; j++) {
    const a = mov[i], b = mov[j], dx = b.x - a.x, dy = b.y - a.y, min = a.r + b.r;
    if (Math.abs(dx) > min || Math.abs(dy) > min) continue;
    const d = Math.hypot(dx, dy);
    if (d < min && d > 1e-4) {
      const p = (min - d) / d * 0.5, wa = a.invulnerable || a instanceof OG.Npc && !a.route ? 0 : 1, wb = b.invulnerable || b instanceof OG.Npc && !b.route ? 0 : 1;
      if (wa + wb === 0) continue;
      a.x -= dx * p * wa * 2 / (wa + wb); a.y -= dy * p * wa * 2 / (wa + wb);
      b.x += dx * p * wb * 2 / (wa + wb); b.y += dy * p * wb * 2 / (wa + wb);
    }
  }
  for (const p of OG.projectiles) p.update(dt);
  OG.projectiles = OG.projectiles.filter(p => !p.done);
  OG.fx.update(dt);
  OG.ents = OG.ents.filter(e => !e.removed && !(e.removeAt && OG.time > e.removeAt));

  // Interaktion
  let best = null, bd = 1e9;
  if (!P.dead && !P.downed) {
    for (const s of OG.interactables) {
      if (!s.on || !s.can()) continue;
      const d = Math.hypot(s.x - P.x, s.y - P.y);
      if (d <= s.radius && d < bd) { bd = d; best = s; }
    }
    for (const e of OG.ents) {
      if (e === P || !e.canInteract || !e.canInteract()) continue;
      const d = Math.hypot(e.x - P.x, e.y - P.y), r = e instanceof OG.Npc ? 2.6 : 2.2;
      if (d <= r && d < bd) { bd = d; best = e; }
    }
  }
  OG.current = best;
  if (best && I.hit('KeyF')) best.interact(P);

  // Demo-Modus
  if (P.demo) {
    if (I.hit('F1')) for (let i = 0; i < 5 && P.level < OG.C.maxLevel; i++) P.gainExp((OG.Player.expToNext(P.level) - P.exp) / (1 + P.race.expBonus) + 1);
    if (I.hit('F2')) { P.taler += 5000; OG.sys('Demo: +5000 Taler.'); }
    if (I.hit('F3')) { OG.Clock.advance(360); OG.sys('Demo: sechs Stunden vergehen. ' + OG.Clock.stamp()); }
    if (I.hit('F4')) { OG.Residents.changeAll(25); OG.sys('Demo: Respekt aller Einwohner +25.'); }
    if (I.hit('F6')) { OG.Rift.weaken(30); OG.sys('Demo: Siegel −30 %.'); }
  }
};

OG.frame = now => {
  const dt = Math.min(0.05, Math.max(0, (now - (OG.lastFrame || now)) / 1000));
  OG.lastFrame = now; OG.lastDt = dt;
  if (OG.running) {
    const paused = OG.UI.isOpen();
    const I = OG.Input;
    if (I.wheel) OG.cam.zoom = OG.clamp(OG.cam.zoom * (I.wheel > 0 ? 0.9 : 1.11), 12, 44);
    if (!paused) { OG.time += dt; OG.update(dt); }
    const P = OG.player;
    OG.cam.x += (P.x - OG.cam.x) * Math.min(1, dt * 8);
    OG.cam.y += (P.y - OG.cam.y) * Math.min(1, dt * 8);
    OG.cam.shake = Math.max(0, OG.cam.shake - dt * 3);
    OG.R.frame();
    OG.UI.update(dt, paused);
  } else OG.menuFrame(dt);
  OG.Input.endFrame();
  requestAnimationFrame(OG.frame);
};

// Hintergrund des Hauptmenüs: Stern der Genesis über einem Nachthimmel.
OG.menuStars = [];
OG.menuFrame = () => {
  const R = OG.R, c = R.ctx, W = R.canvas.width, H = R.canvas.height;
  c.setTransform(1, 0, 0, 1, 0, 0);
  const g = c.createLinearGradient(0, 0, 0, H);
  g.addColorStop(0, '#060a1c'); g.addColorStop(1, '#14204a');
  c.fillStyle = g; c.fillRect(0, 0, W, H);
  if (!OG.menuStars.length) for (let i = 0; i < 160; i++) OG.menuStars.push([Math.random(), Math.random(), Math.random()]);
  const t = performance.now() / 1000;
  for (const [x, y, s] of OG.menuStars) { c.fillStyle = 'rgba(255,245,220,' + (0.3 + 0.5 * Math.abs(Math.sin(t * s + x * 10))) + ')'; c.fillRect(x * W, y * H, 1.5 * R.dpr, 1.5 * R.dpr); }
  const cx = W * 0.72, cy = H * 0.5, r = Math.min(W, H) * 0.32;
  const boost = OG.menuStarBoost ? OG.clamp01(1 - (performance.now() - OG.menuStarBoost) / 5000) : 0;
  OG.menuStarAngle = (OG.menuStarAngle || 0) + 0.0008 + boost * 0.05;
  c.save(); c.translate(cx, cy); c.rotate(OG.menuStarAngle);
  c.globalAlpha = 0.18 + boost * 0.6; OG.drawStar(c, 0, 0, r, '#f2c14d');
  c.globalAlpha = 0.4; c.strokeStyle = '#f2c14d'; c.lineWidth = 1.5 * R.dpr; c.beginPath(); c.arc(0, 0, r * 1.1, 0, Math.PI * 2); c.stroke();
  c.restore(); c.globalAlpha = 1;
};

// ---------- Start ----------
OG.boot = hot => {
  const canvas = document.getElementById('game');
  OG.R.init(canvas);
  OG.Input.init(canvas);
  OG.UI.init();
  OG.UI.showMenu('start');
  requestAnimationFrame(OG.frame);
  try {
    if (window.claude && window.claude.hot && typeof window.claude.hot.snapshot === 'function')
      window.claude.hot.snapshot(() => (OG.running ? { save: OG.buildSave() } : {}));
  } catch (e) { /* ohne Vorschau-Aktualisierung */ }
  if (hot && hot.save) OG.start(hot.save);
};

(() => {
  const h = window.claude && window.claude.hot;
  if (h && typeof h.ready === 'function') h.ready(OG.boot);
  else OG.boot(h && h.data ? h.data : {});
})();
