// Omni Genesis 2D — Oberfläche: HUD, Systemfenster, Dialoge, Hauptmenü, Touch-Steuerung.
'use strict';

const $ = s => document.querySelector(s);
const E = OG.esc;

OG.UI = {
  panel: null, arg: null, state: {}, textT: 0, hitA: 0, bannerT: 99, renderT: 0,
  init() {
    this.hud = $('#hud'); this.win = $('#win'); this.menu = $('#menu'); this.load = $('#loading');
    this.win.addEventListener('click', e => this.onClick(e));
    this.menu.addEventListener('click', e => this.onClick(e));
    this.win.addEventListener('input', e => { if (e.target.id === 'treatyName') this.state.treatyName = e.target.value; });
    this.menu.addEventListener('input', e => { if (e.target.id === 'charName') this.state.charName = e.target.value; });
    this.initTouch();
  },
  isOpen() { return !!this.panel; },

  // ---------- Kleinigkeiten ----------
  banner(text, color) { const b = $('#banner'); b.textContent = text; b.style.color = color; this.bannerT = 0; },
  hit(f) { this.hitA = Math.min(0.6, 0.25 + f); },
  bar(sel, frac) { const el = $(sel); if (el) el.style.width = (OG.clamp01(frac) * 100).toFixed(1) + '%'; },
  set(sel, html) { const el = $(sel); if (el && el._h !== html) { el.innerHTML = html; el._h = html; } },

  showLoading(p, status) {
    this.menu.hidden = true; this.load.hidden = false; this.hud.hidden = true;
    $('#loadBar').style.width = Math.round(p * 100) + '%';
    $('#loadText').textContent = 'Lade Welt… ' + Math.round(p * 100) + ' %';
    $('#loadStatus').textContent = status || '';
  },
  hideLoading() { this.load.hidden = true; this.hud.hidden = false; $('#touch').hidden = !OG.Input.touch; document.body.classList.toggle('touch', OG.Input.touch); },

  // ---------- HUD ----------
  update(dt, paused) {
    const I = OG.Input, P = OG.player;
    // Tastenkürzel für Fenster
    if (!this.panel || ['char', 'inv', 'map', 'quests', 'edict', 'pillars'].includes(this.panel)) {
      const map = { KeyC: 'char', KeyI: 'inv', KeyM: 'map', KeyL: 'quests', KeyJ: 'edict', KeyP: 'pillars' };
      for (const k in map) if (I.hit(k)) { this.panel === map[k] ? this.close() : this.openPanel(map[k]); }
    }
    if (I.hit('Escape')) { this.panel ? this.close() : this.openPanel('pause'); }
    if (I.hit('F5')) OG.save(false);

    this.hitA = Math.max(0, this.hitA - dt * 1.5);
    $('#hitflash').style.opacity = this.hitA;
    this.bannerT += dt;
    const b = $('#banner'); b.style.opacity = this.bannerT < 1.6 ? 1 : Math.max(0, 1 - (this.bannerT - 1.6) / 0.6);

    this.bar('#hpBar', P.frac); this.bar('#aeBar', P.aether / P.maxAether); this.bar('#stBar', P.stamina / P.maxStamina);
    this.bar('#xpBar', P.level >= OG.C.maxLevel ? 1 : P.exp / OG.Player.expToNext(P.level));

    this.textT -= dt;
    if (this.textT <= 0) { this.textT = 0.12; this.hudText(); }
    if (this.panel) {
      this.renderT -= dt;
      const typing = document.activeElement && document.activeElement.tagName === 'INPUT';
      if (this.renderT <= 0 && !typing) { this.renderT = 0.5; this.render(); }
    }
  },

  hudText() {
    const P = OG.player, rc = OG.Ranks.color(P.rank);
    const badge = $('#rank'); badge.textContent = OG.Ranks.name(P.rank); badge.style.color = rc; badge.style.borderColor = rc;
    this.set('#who', '<b>' + E(P.charName) + '</b> · Stufe ' + P.level);
    this.set('#cls', E(P.race.name + ' · ' + P.classTitle));
    $('#hpTxt').textContent = 'LP ' + Math.ceil(P.hp) + ' / ' + Math.ceil(P.maxHp);
    $('#aeTxt').textContent = 'Äther ' + Math.floor(P.aether) + ' / ' + Math.floor(P.maxAether);
    let line = OG.fmt(P.taler) + ' Taler';
    if (P.free > 0) line += ' · ' + P.free + ' freie Punkte (C)';
    this.set('#money', line);
    const load = P.carried, over = load > P.carry;
    this.set('#load', '<span class="' + (over ? 'bad' : '') + '">Last ' + OG.fmt(load, 1) + ' / ' + OG.fmt(P.carry) + ' kg' + (over ? ' · überladen' : '') + '</span>');
    let extra = P.buffs.map(b => '<span class="good">' + E(b.name) + ' ' + Math.max(0, Math.ceil(b.until - OG.time)) + ' s</span>').join(' ');
    if (OG.Law.wanted > 0) extra += '<div class="bad strong">GESUCHT (Stufe ' + OG.Law.wanted + ') — [G] dem Gericht stellen</div>';
    if (P.demo) extra += '<div class="demo">DEMO: F1 +5 Stufen · F2 +5000 Taler · F3 +6 Std. · F4 Respekt +25 · F6 Siegel −30 %</div>';
    this.set('#buffs', extra);

    const region = OG.region(P.x, P.y);
    const where = region.indexOf('Eichenhain') >= 0 && OG.Autonomy.isAutonomous() ? 'Autonomie „' + OG.Autonomy.name + '“' : region;
    this.set('#where', E(where));
    this.set('#when', OG.Clock.stamp() + (OG.Clock.isNight() ? ' · Nacht' : '') + ' · ' + OG.Weather.name());
    this.set('#seal', '<span class="' + (OG.Rift.strength < 30 ? 'bad' : '') + '">Siegel des Risses ' + Math.round(OG.Rift.strength) + ' %' + (OG.Rift.summons ? ' · GROSSER AUFRUF' : '') + '</span>');
    this.set('#respect', 'Respekt in Eichenhain: ' + OG.Residents.respecting() + ' / ' + OG.Residents.list.length);
    this.set('#warn', region === 'Verbotene Lande' && !P.permit ? 'Ohne Kaiserliche Erlaubnis! Die Ordensmagier greifen an (Art. 112).' : '');

    // Aufgabenverfolger
    const Q = OG.QuestSys, act = Q.active.slice().sort((a, b) => (b.def.main ? 1 : 0) - (a.def.main ? 1 : 0)).slice(0, 4);
    let tr = act.map(s => { const st = Q.step(s); return '<div class="trk ' + (s.def.main ? 'main' : '') + '"><b>' + E(s.def.title) + '</b><span>• ' + E(st.text) + Q.progressText(s) + (st.night && !OG.Clock.isNight() ? ' — warte auf die Nacht' : '') + '</span></div>'; }).join('');
    const hint = Q.nextHint();
    if (hint && !Q.active.some(s => s.def.main)) tr += '<div class="dim small">Nächstes Kapitel: ' + E(hint) + '</div>';
    this.set('#tracker', tr);

    // Meldungen
    const now = performance.now();
    OG.messages = OG.messages.filter(m => now - m.t < 6500);
    this.set('#msgs', OG.messages.slice(-3).map(m => '<div class="msg ' + m.kind + '" style="opacity:' + Math.min(1, (6500 - (now - m.t)) / 900).toFixed(2) + '">' + E(m.text) + '</div>').join(''));
    this.set('#log', '<div class="logh">Physik-Protokoll (F = m · a)</div>' + OG.combatLog.map(l => '<div>' + E(l) + '</div>').join(''));

    // Fähigkeiten
    const keys = ['Q', 'E', 'R'];
    let sk = '';
    for (let i = 0; i < 3; i++) {
      const a = P.slot(i);
      if (!a) { sk += '<div class="slot off"><span class="k">R</span><small>ab Stufe 50</small></div>'; continue; }
      const cd = P.cdLeft(i), f = cd / a.cd;
      sk += '<div class="slot' + (cd > 0 ? ' cd' : '') + '" data-touch="Key' + keys[i] + '"><div class="cdfill" style="height:' + (f * 100).toFixed(0) + '%"></div><span class="k ' + (a.cost > P.aether ? 'bad' : '') + '">' + (cd > 0 ? OG.fmt(cd, 1) : keys[i]) + '</span><small>' + E(a.name) + '</small>' + (a.cost ? '<i>' + a.cost + '</i>' : '') + '</div>';
    }
    const pots = [['heiltrank', 'Heiltrank'], ['aetherelixier', 'Elixier'], ['gebratenes_fleisch', 'Fleisch']];
    pots.forEach(([id, n], i) => { sk += '<div class="slot pot" data-touch="Digit' + (i + 1) + '"><span class="k">' + (i + 1) + '</span><small>' + n + '</small><i>×' + P.inv.count(id) + '</i></div>'; });
    this.set('#skills', sk);
    if (OG.Input.touch) ['KeyQ', 'KeyE', 'KeyR'].forEach((k, i) => {
      const b = document.querySelector('#touch [data-key="' + k + '"]'); if (!b) return;
      const a = P.slot(i), cd = P.cdLeft(i), label = !a ? '—' : cd > 0 ? OG.fmt(cd, 0) : keys[i];
      if (b.textContent !== label) b.textContent = label;
    });

    // Interaktion und Ziel
    const it = OG.current;
    this.set('#prompt', it ? '<span class="key">F</span> ' + E(typeof it.prompt === 'function' ? it.prompt() : it.prompt) : '');
    $('#prompt').hidden = !it;
    const mw = OG.Input.touch ? P.aimPoint() : OG.screenToWorld(OG.Input.mouseX, OG.Input.mouseY);
    const t = OG.nearest(mw.x, mw.y, 1.6, e => e !== P && !e.invulnerable);
    if (t) {
      let info = '';
      if (t instanceof OG.Creature && t.def.ecosystem && !t.managed) info = 'Masse ' + t.def.mass + ' kg · Bestand ' + OG.Eco.count(t.def.id) + ' / ' + t.def.capacity + (t.juvenile ? ' · Jungtier' : '') + (t.def.diet === 'Fleischfresser' ? ' · Hunger ' + Math.round(t.hunger * 100) + ' %' : '');
      else if (t instanceof OG.Creature) info = 'Masse ' + t.def.mass + ' kg';
      this.set('#target', '<b>' + E(t.name) + '</b><div class="tbar"><i style="width:' + (t.frac * 100).toFixed(0) + '%"></i></div><small>' + E(info) + '</small>');
      $('#target').hidden = false;
    } else $('#target').hidden = true;
  },

  // ---------- Fenster ----------
  openPanel(kind, arg) { this.panel = kind; this.arg = arg; this.state = { tab: 0, treatyName: 'Freie Mark Eichenhain', color: 0 }; this.render(); this.win.hidden = false; },
  openDialogue(npc) { this.openPanel('npc', npc); this.state.greeting = OG.greeting(npc); },
  close() { this.panel = null; this.win.hidden = true; this.win.innerHTML = ''; OG.Input.mouseDown = false; },

  render() {
    if (!this.panel) return;
    const scrolls = [...this.win.querySelectorAll('.scroll')].map(s => s.scrollTop);
    const f = OG.Panels[this.panel];
    const r = f ? f(this.arg, this.state) : { title: '', body: '' };
    this.win.innerHTML = '<div class="window ' + (r.wide ? 'wide' : '') + '" role="dialog" aria-label="' + E(r.title) + '"><header><h2>' + E(r.title) + '</h2><button class="x" data-act="close" aria-label="Schließen">×</button></header><div class="wbody">' + r.body + '</div></div>';
    [...this.win.querySelectorAll('.scroll')].forEach((s, i) => { if (scrolls[i]) s.scrollTop = scrolls[i]; });
    if (r.after) r.after();
  },

  onClick(e) {
    const el = e.target.closest('[data-act]');
    if (!el) { if (e.target === this.win) this.close(); return; }
    const act = el.dataset.act, arg = el.dataset.arg;
    const fn = OG.Actions[act];
    if (fn) { fn(arg, el); if (this.panel) this.render(); }
  },

  // ---------- Hauptmenü ----------
  showMenu(screen) {
    this.menu.hidden = false; this.hud.hidden = true; this.load.hidden = true; $('#touch').hidden = true;
    this.mstate = Object.assign({ screen: screen || 'start', race: 0, cls: 0, demo: false }, this.mstate || {}, { screen: screen || 'start' });
    this.renderMenu();
  },
  renderMenu() {
    const s = this.mstate, has = OG.Store.exists();
    let h = '<div class="mhead"><h1>Omni Genesis</h1><p class="motto">' + OG.C.motto + '</p><p class="dim">Eine lebendige Welt nach dem Kaiserlichen Edikt · 2D-Version</p></div>';
    if (s.screen === 'start') {
      h += '<div class="mbtns">' + (has ? '<button class="big" data-act="menuContinue">Fortsetzen</button>' : '') +
        '<button class="big" data-act="menuNew">Neues Spiel</button><button class="big" data-act="menuEdict">Das Edikt lesen</button></div>' +
        '<p class="dim small">Spielstände werden nur in diesem Browser gespeichert.</p>';
    } else if (s.screen === 'create') {
      const r = OG.Races[s.race], c = OG.Classes[s.cls];
      h += '<div class="create"><section><h3>Name</h3><input id="charName" maxlength="24" value="' + E(this.state.charName || '') + '" placeholder="Name deines Charakters" autocomplete="off">' +
        '<h3>Rasse <small>Art. 11</small></h3><div class="choice">' + OG.Races.map((x, i) => '<button class="' + (i === s.race ? 'on' : '') + '" data-act="pickRace" data-arg="' + i + '">' + x.name + '</button>').join('') + '</div>' +
        '<p>' + E(r.desc) + '</p><p class="dim small">Bonus: STR +' + r.str + ', BEW +' + r.agi + ', VIT +' + r.vit + ', INT +' + r.int + '</p>' +
        '<label class="toggle"><input type="checkbox" id="demoBox" data-act="toggleDemo" ' + (s.demo ? 'checked' : '') + '> Demo-Modus (F1–F6 beschleunigen alles für eine Vorführung)</label></section>' +
        '<section><h3>Klasse <small>Art. 48</small></h3><div class="choice col">' + OG.Classes.map((x, i) => '<button class="' + (i === s.cls ? 'on' : '') + '" data-act="pickClass" data-arg="' + i + '"><span class="dot" style="background:' + x.color + '"></span>' + x.name + '</button>').join('') + '</div></section>' +
        '<section><h3>' + c.name + '</h3><p>' + E(c.desc) + '</p><p class="small">Start: Stärke ' + (c.str + r.str) + ' · Beweglichkeit ' + (c.agi + r.agi) + ' · Vitalität ' + (c.vit + r.vit) + ' · Intelligenz ' + (c.int + r.int) + '</p>' +
        '<p class="small dim">Waffe: ' + OG.itemName(c.weapon) + ' (' + OG.Items[c.weapon].mass + ' kg) · Rüstung: ' + OG.itemName(c.armor) + '</p>' +
        '<p class="small"><b>[Q] ' + c.q.name + ':</b> ' + E(c.q.desc) + '</p><p class="small"><b>[E] ' + c.e.name + ':</b> ' + E(c.e.desc) + '</p>' +
        '<p class="small dim">Ab Stufe 50 ergibt eine zweite Klasse eine Hybridklasse: ' + OG.Classes.filter(o => o.id !== c.id).map(o => o.name + ' = ' + OG.hybrid(c.id, o.id).name).join(', ') + '.</p></section></div>' +
        '<div class="mbtns row"><button data-act="menuBack">Zurück</button><button class="big gold" data-act="menuStart">Welt betreten</button></div>';
      if (has) h += '<p class="small bad center">Ein neuer Charakter ersetzt den vorhandenen Spielstand.</p>';
    } else {
      h += '<div class="edictbox scroll"><p class="gold">' + E(OG.EdictPreamble) + '</p>' + OG.Edict.map(a => '<h4>' + a[0] + ' — ' + E(a[1]) + '</h4><p>' + E(a[2]) + '</p>').join('') + '</div><div class="mbtns row"><button data-act="menuBack">Zurück</button></div>';
    }
    this.menu.innerHTML = '<div class="menuin">' + h + '</div>';
  },

  // ---------- Touch ----------
  initTouch() {
    const I = OG.Input, T = $('#touch');
    const enable = () => { if (!I.touch) { I.touch = true; document.body.classList.add('touch'); if (OG.running) { T.hidden = false; OG.cam.zoom = Math.min(OG.cam.zoom, 24); } } };
    if (window.matchMedia && window.matchMedia('(pointer: coarse)').matches) I.touch = true;
    window.addEventListener('touchstart', enable, { passive: true });
    const stick = $('#stick'), knob = $('#knob');
    let sid = null, cx = 0, cy = 0;
    stick.addEventListener('pointerdown', e => { sid = e.pointerId; const r = stick.getBoundingClientRect(); cx = r.left + r.width / 2; cy = r.top + r.height / 2; stick.setPointerCapture(sid); move(e); });
    const move = e => {
      if (e.pointerId !== sid) return;
      let dx = e.clientX - cx, dy = e.clientY - cy; const l = Math.hypot(dx, dy), m = 46;
      if (l > m) { dx *= m / l; dy *= m / l; }
      I.stickX = dx / m; I.stickY = dy / m; knob.style.transform = 'translate(' + dx + 'px,' + dy + 'px)';
    };
    stick.addEventListener('pointermove', move);
    const end = e => { if (e.pointerId !== sid) return; sid = null; I.stickX = I.stickY = 0; knob.style.transform = ''; };
    stick.addEventListener('pointerup', end); stick.addEventListener('pointercancel', end);
    T.addEventListener('pointerdown', e => {
      const b = e.target.closest('[data-key]'); if (!b) return;
      e.preventDefault();
      const k = b.dataset.key;
      if (k === 'attack') I.touchAttack = true; else { I.pressed.add(k); I.held.add(k); }
      b.classList.add('down');
      const up = () => { if (k === 'attack') I.touchAttack = false; else I.held.delete(k); b.classList.remove('down'); window.removeEventListener('pointerup', up); };
      window.addEventListener('pointerup', up);
    });
    $('#hud').addEventListener('pointerdown', e => {
      const b = e.target.closest('[data-touch]'); if (!b) return;
      I.pressed.add(b.dataset.touch);
    });
  },
};

// ---------- Begrüßungen der NPCs ----------
OG.greeting = n => {
  const P = OG.player;
  switch (n.role) {
    case 'Kaiser': return '„Der Thron steht, solange die Sterne brennen. Was suchst du vor dem Thron der Genesis, ' + P.charName + '?“';
    case 'Richter': return '„Ich bewahre alle hundertsechsundsechzig Artikel des Edikts im Gedächtnis. Keines meiner Urteile weicht vom Buchstaben des Gesetzes ab.“';
    case 'Haendler': case 'Kraemer': return '„Ehrlicher Handel steht unter dem Schutz des Throns (Art. 42). Die Preise richten sich nach Angebot und Nachfrage.“';
    case 'Schmied': return '„Masse mal Beschleunigung, Reisender. Eine schwere Klinge schlägt härter — wenn du stark genug bist, sie schnell zu führen.“';
    case 'Vermieter': return '„Ein Zimmer im Haus des Throns. Die Miete fließt in die Kasse (Art. 116–117).“';
    case 'Heiler': return '„Jeder hat das Recht auf Heilung in den kaiserlichen Lazaretten (Art. 45). Komm, ich sehe nach dir.“';
    case 'Pruefungsmeister': return '„Die Kaiserliche Prüfung der Meisterschaft (Art. 48). Drei Wellen Golems. Ich messe Zeit und Treffer.“';
    case 'Gildenmeister': return '„Die Gilde der Abenteurer arbeitet nach der Charta des Throns (Art. 121). Arbeit gibt es jeden Tag: Jagd, Lieferungen, Risswache.“';
    case 'Wache': return OG.Law.wanted > 0 ? '„Im Namen des Throns: Du bist festgenommen!“' : '„Die Legionen schützen Trakte und Siedlungen vor den Geschöpfen der Risse (Art. 46).“';
    case 'Ordensmagier': return P.permit ? '„Deine Erlaubnis ist in Ordnung. Hüte dich vor der Leerenbrut.“' : '„Ohne Kaiserliche Erlaubnis kein Schritt weiter (Art. 112). Die Kanzlei in Grad Genesis stellt sie aus.“';
    case 'Soeldnerfuehrer': return '„Eine Autonomie braucht eigene Wachen (Art. 133). Meine Leute kämpfen für jeden, der den Sold zahlt.“';
    case 'Dorfwache': return '„Eichenhain ist sicher, solange ich atme.“';
    case 'Einwohner': {
      const r = n.resident;
      if (r.respect >= 80) return '„Du bist ein Segen für Eichenhain. Ich würde dir überallhin folgen.“';
      if (OG.Residents.respects(r)) return '„Ich respektiere dich. Du hast für uns getan, was kein Fremder getan hätte.“';
      if (r.respect < -20) return '„Geh mir aus den Augen.“';
      return '„Fremde kommen und gehen. Zeig uns erst, wer du bist.“';
    }
    default: {
      if (OG.Rift.summons) return '„Der Große Aufruf! Alle Wehrfähigen sollen zum Riss. Die Heiltränke werden knapp.“';
      if (OG.Rift.strength < 35) return '„Man sagt, das Siegel im Norden wird schwach. Nachts hört man die Leerenbrut.“';
      if (OG.Eco.stock('graslaeufer') < 0.35) return '„Die Jäger finden kaum noch Grasläufer. Kein Wunder, dass das Fleisch so teuer ist.“';
      if (OG.Market.trend('fleisch') > 1.1) return '„Fleisch wird teurer. Irgendwer jagt zu viel oder die Karawanen stocken.“';
      if (OG.Weather.isStorm()) return '„Bei diesem Gewitter kommt keine Karawane durch.“';
      if (OG.Autonomy.isAutonomous()) return '„Hast du gehört? ' + OG.Autonomy.name + ' ist jetzt eine anerkannte Autonomie!“';
      return '„Ein Kontinent, ein Thron, ein Gesetz. So ist es seit der Ära der Einheit.“';
    }
  }
};

// ---------- Bausteine für Fenster ----------
const btn = (label, act, arg, on, cls) => '<button class="' + (cls || '') + '" data-act="' + act + '"' + (arg != null ? ' data-arg="' + E(arg) + '"' : '') + (on === false ? ' disabled' : '') + '>' + label + '</button>';
const bar = (f, col) => '<div class="mbar"><i style="width:' + (OG.clamp01(f) * 100).toFixed(0) + '%;background:' + col + '"></i></div>';

OG.Panels = {
  char() {
    const P = OG.player, w = P.wpn, names = ['Stärke', 'Beweglichkeit', 'Vitalität', 'Intelligenz'];
    const hints = ['kürzere Schwungzeit ⇒ größeres a ⇒ größeres F; Traglast +1,2 kg', 'Lauftempo, Schlag- und Schusstempo, kritische Treffer', '+10 LP, +0,2 Rüstung, Regeneration', '+6 Äther, Ätherbeschleunigung +3 %'];
    let phys = '';
    if (w.kind === 'Nahkampf') {
      const t = P.swingTime(w), v = 2.4 * w.length / t, a = v / t, F = w.mass * a;
      phys = '<p>' + w.name + ': m = ' + OG.fmt(w.mass, 1) + ' kg, L = ' + OG.fmt(w.length, 2) + ' m, Schwungzeit t = ' + OG.fmt(t, 2) + ' s</p><p class="mono">v = θ·L/t = ' + OG.fmt(v, 1) + ' m/s · a = v/t = ' + OG.fmt(a) + ' m/s² · F = m·a = ' + OG.fmt(F) + ' N</p><p class="dim">Schaden ≈ F · 0,5 = ' + OG.fmt(F * 0.5) + ' (Anlauf erhöht a um v_Anlauf / t)</p>';
    } else if (w.kind === 'Bogen') {
      const F = w.draw * (1 + P.str * 0.005), v = Math.min(70, OG.Phys.arrowSpeed(F, 0.6, 0.05));
      phys = '<p>' + w.name + ': Zugkraft F = ' + OG.fmt(F) + ' N, Pfeil m = 0,05 kg, Auszug d = 0,6 m</p><p class="mono">a = F/m = ' + OG.fmt(F / 0.05) + ' m/s² · v = √(2·F·d/m) = ' + OG.fmt(v) + ' m/s · E = ½·m·v² = ' + OG.fmt(0.025 * v * v) + ' J</p><p class="dim">Der Pfeil fällt mit g = 9,81 m/s² — weite Ziele brauchen einen höheren Bogen.</p>';
    } else {
      const a = P.aetherAccel();
      phys = '<p>' + w.name + ': Äthermasse 0,25 kg, a = 120 · (1 + 0,03 · INT) · Fokus = ' + OG.fmt(a) + ' m/s²</p><p class="mono">F = m·a = ' + OG.fmt(0.25 * a) + ' N</p>';
    }
    let second = '';
    if (P.hybrid) second = '<p class="good">' + P.hybrid.name + ': ' + E(P.hybrid.desc) + '</p>';
    else if (P.level < OG.C.secondClassLevel) second = '<p class="dim">Ab Stufe 50 wählst du eine zweite Klasse. Beide zusammen ergeben eine Hybridklasse (z. B. Kampfmagier + Assassine = Nekromant).</p>';
    else second = OG.Classes.filter(c => c.id !== P.cls.id).map(c => { const h = OG.hybrid(P.cls.id, c.id); return btn(c.name + ' ⇒ ' + h.name + ' (' + h.r.name + ')', 'second', c.id, true, 'wide'); }).join('');
    const sk = [0, 1, 2].map(i => { const a = P.slot(i); return a ? '<p><b>[' + 'QER'[i] + '] ' + a.name + '</b> — ' + E(a.desc) + ' <span class="dim">(Äther ' + a.cost + ', ' + a.cd + ' s)</span></p>' : ''; }).join('');
    return {
      title: 'Status — ' + P.charName, wide: true,
      body: '<div class="cols"><div><p class="gold strong">' + P.race.name + ' · ' + E(P.classTitle) + ' · Stufe ' + P.level + ' / ' + OG.C.maxLevel + '</p>' +
        '<p class="dim">' + (P.level >= OG.C.maxLevel ? 'Höchststufe erreicht.' : 'Erfahrung: ' + Math.floor(P.exp) + ' / ' + OG.Player.expToNext(P.level)) + '</p>' +
        '<p class="' + (P.free ? 'good' : 'dim') + ' strong">Freie Attributpunkte: ' + P.free + '</p>' +
        [0, 1, 2, 3].map(i => '<div class="statrow"><div><b>' + names[i] + '</b><small>' + hints[i] + '</small></div><span class="num">' + P.stat(i) + '</span>' + btn('+', 'alloc', i, P.free > 0, 'sq') + '</div>').join('') +
        '<p>LP ' + Math.round(P.maxHp) + ' · Äther ' + Math.round(P.maxAether) + ' · Ausdauer ' + Math.round(P.maxStamina) + '</p>' +
        '<p>Rüstung ' + OG.fmt(P.armorValue, 1) + ' (Schaden × 60/(60+Rüstung)) · Kritisch ' + Math.round(P.crit * 100) + ' %</p>' +
        '<p>Körpermasse ' + OG.fmt(P.bodyMass) + ' kg + Ausrüstung ' + OG.fmt(P.equipMass, 1) + ' kg · Traglast ' + OG.fmt(P.carried, 1) + ' / ' + OG.fmt(P.carry) + ' kg</p>' +
        '<h3>Physik deines Angriffs</h3>' + phys + '</div><div>' +
        '<h3>Ausrüstung</h3><div class="row2"><span>Waffe: ' + w.name + ' (' + OG.fmt(w.mass, 1) + ' kg)</span>' + btn('Ablegen', 'unequip', 'w', !!OG.Items[P.weapon]) + '</div>' +
        '<div class="row2"><span>Rüstung: ' + (P.arm ? P.arm.name + ' (' + P.arm.mass + ' kg, ' + P.arm.armor + ')' : 'keine') + '</span>' + btn('Ablegen', 'unequip', 'a', !!P.arm) + '</div>' +
        '<h3>Rang und Titel</h3><p>Rang des Aufstiegs: <b style="color:' + OG.Ranks.color(P.rank) + '">' + OG.Ranks.name(P.rank) + '</b>' + (OG.QuestSys.title ? ' · Titel: „' + E(OG.QuestSys.title) + '“' : '') + '</p>' +
        '<p class="' + (P.permit ? 'good' : 'dim') + '">Kaiserliche Erlaubnis für die Verbotenen Lande: ' + (P.permit ? 'ja' : 'nein') + '</p>' +
        '<h3>Fähigkeiten</h3>' + sk + '<h3>Zweite Klasse</h3>' + second + '</div></div>',
    };
  },

  inv() {
    const P = OG.player;
    const rows = P.inv.list().map(({ id, n }) => {
      const d = OG.Items[id];
      let a = '';
      if (d.cat === 'Waffe' || d.cat === 'Ruestung') a = btn('Ausrüsten', 'equip', id);
      else if (d.cat === 'Verbrauch') a = btn('Benutzen', 'use', id);
      else if (d.cat === 'Material') a = btn('1 ablegen', 'drop', id);
      return '<div class="irow"><span>' + d.name + ' <b>×' + n + '</b></span><small>' + OG.fmt(d.mass * n, 1) + ' kg</small>' + a + '</div>';
    }).join('') || '<p class="dim">Das Inventar ist leer.</p>';
    return {
      title: 'Inventar und Handwerk', wide: true,
      body: '<div class="cols"><div><p class="gold">Last ' + OG.fmt(P.carried, 1) + ' / ' + OG.fmt(P.carry) + ' kg · ' + OG.fmt(P.taler) + ' Taler</p><div class="scroll">' + rows + '</div></div>' +
        '<div><p class="gold">Handwerk unterwegs (Metall nur am Amboss der Schmiede)</p><div class="scroll">' + OG.recipesHtml(false) + '</div></div></div>',
    };
  },

  craft() { return { title: 'Amboss — Schmieden', body: '<div class="scroll">' + OG.recipesHtml(true) + '</div>' }; },

  map() {
    const Eco = OG.Eco, census = Object.values(OG.Species).filter(d => d.ecosystem).map(d => '<p>' + d.name + ': <b>' + Eco.count(d.id) + '</b> / ' + d.capacity + '</p>').join('');
    const prices = ['fleisch', 'holz', 'eisenbarren', 'heiltrank', 'aetherkristall'].map(id => { const t = OG.Market.trend(id); return '<p>' + OG.itemName(id) + ': ' + OG.Market.sellPrice(id) + ' T' + (t > 1.05 ? ' <span class="bad">▲</span>' : t < 0.95 ? ' <span class="good">▼</span>' : '') + '</p>'; }).join('');
    return {
      title: 'Karte des Kontinents (Ausschnitt)', wide: true,
      body: '<div class="cols map"><canvas id="mapc" width="760" height="760" aria-label="Karte"></canvas><div><h3>Ökosystem der Provinz</h3><p class="dim small">Getötete Tiere kehren nie zurück. Bestände wachsen nur durch Nachwuchs (logistisch bis zur Tragfähigkeit).</p>' + census +
        '<h3>Welt</h3><p>' + OG.Clock.stamp() + ' · ' + OG.Weather.name() + '</p><p>Siegel des Risses: ' + Math.round(OG.Rift.strength) + ' %' + (OG.Rift.summons ? ' — Großer Aufruf!' : '') + '</p><p>Bäume stehend: ' + Math.round(OG.Nodes.treeFraction() * 100) + ' %</p><h3>Marktpreise (Ankauf)</h3>' + prices + '</div></div>',
      after: () => OG.drawMap($('#mapc')),
    };
  },

  quests() {
    const Q = OG.QuestSys;
    const act = Q.active.slice().sort((a, b) => (b.def.main ? 1 : 0) - (a.def.main ? 1 : 0)).map(s => '<div class="qblock ' + (s.def.main ? 'main' : '') + '"><h4>' + E(s.def.title) + (s.def.main ? '' : ' <small>— ' + E(s.def.giver) + '</small>') + '</h4>' + (s.def.summary ? '<p class="dim small">' + E(s.def.summary) + '</p>' : '') +
      s.def.steps.map((st, i) => '<p class="' + (i < s.step ? 'good' : i === s.step ? '' : 'dim') + '">' + (i < s.step ? '✓ ' : i === s.step ? '► ' : '· ') + (i <= s.step ? E(st.text) + (i === s.step ? Q.progressText(s) : '') + (st.night && i === s.step ? ' (nur nachts)' : '') : '???') + '</p>').join('') + '</div>').join('') ||
      '<p class="dim">Keine aktiven Aufträge. Achte auf ! über den Köpfen der Leute und auf das Auftragsbrett am Gildenhaus.</p>';
    const story = OG.Quests.filter(q => q.main && Q.done.includes(q.id)).map(q => '<h4>' + E(q.title) + '</h4><p class="dim">' + E(q.done) + '</p>').join('') || '<p class="dim">Noch kein Kapitel abgeschlossen.</p>';
    const hint = Q.nextHint(), side = OG.Quests.filter(q => !q.main && Q.done.includes(q.id)).length;
    return {
      title: 'Aufträge und Geschichte', wide: true,
      body: '<div class="cols"><div class="scroll"><h3>Aktive Aufträge</h3>' + act + '</div><div class="scroll"><h3>Die Geschichte bisher</h3>' + story + (hint ? '<p class="gold">Nächstes Kapitel: ' + E(hint) + '</p>' : '') +
        '<p class="dim">Erfüllte Nebenaufträge: ' + side + ' · Titel: ' + (Q.title ? E(Q.title) : '—') + '</p><p class="dim small">Gildenaufträge gibt es täglich bei Runa am Gildenhaus östlich der Stadt (Art. 121). Jagdaufträge nur für Arten mit gesundem Bestand.</p></div></div>',
    };
  },

  edict(a, s) {
    const tabs = ['Edikt', 'Ränge', 'Kasse', 'Verkündungen', 'Register'];
    let body = '<div class="tabs">' + tabs.map((t, i) => btn(t, 'tab', i, true, i === s.tab ? 'on' : '')).join('') + '</div><div class="scroll tall">';
    if (s.tab === 0) body += '<p class="gold">' + E(OG.EdictPreamble) + '</p>' + OG.Edict.map(x => '<h4>' + x[0] + ' — ' + E(x[1]) + '</h4><p>' + E(x[2]) + '</p><p class="cyan small">Im Spiel: ' + E(x[3]) + '</p>').join('');
    else if (s.tab === 1) body += '<p>Ränge des Aufstiegs (Art. 48, 69). Vergabe durch die Prüfung der Meisterschaft in der Arena westlich von Grad Genesis. Der Rang ist begrenzt durch Leistung und Stufe.</p>' +
      OG.Ranks.names.map((n, i) => '<p style="color:' + OG.Ranks.color(i) + '" class="' + (i === OG.player.rank ? 'strong' : '') + '">Rang ' + n + ': ab Stufe ' + OG.Ranks.levelReq[i] + (i >= 6 ? ' — verliehen durch den Kaiser' : '') + (i === OG.player.rank ? ' ◄ dein Rang' : '') + '</p>').join('') +
      '<p class="dim">Kaiserliche Erlaubnis für die Verbotenen Lande: ab Rang D in der Kanzlei (Art. 112).</p>';
    else if (s.tab === 2) {
      const T = OG.Treasury;
      body += '<p class="gold strong">Kaiserliche Kasse: ' + OG.fmt(T.balance) + ' Taler</p><p class="dim">Einnahmen:</p>' + Object.keys(T.income).map(k => '<p class="good">+ ' + E(k) + ': ' + OG.fmt(T.income[k]) + '</p>').join('') +
        '<p class="dim">Ausgaben:</p>' + Object.keys(T.expenses).map(k => '<p class="bad">− ' + E(k) + ': ' + OG.fmt(T.expenses[k]) + '</p>').join('') +
        '<p class="dim small">Handelsabgabe 10 % (Art. 93) · Miete (Art. 117) · Gebühren und Bußgelder · Torgebühr ' + OG.C.gateFee + ' Taler (Art. 34)</p>';
    } else if (s.tab === 3) body += OG.announcements.map(x => '<p>' + E(x) + '</p>').join('') || '<p class="dim">Noch keine Verkündungen.</p>';
    else body += OG.Law.record.map(x => '<p>' + E(x) + '</p>').join('') || '<p class="dim">Dein Register ist rein.</p>';
    return { title: 'Kaiserliches Edikt von Omni Genesis', wide: true, body: body + '</div>' };
  },

  pillars() {
    const A = OG.Autonomy, R = OG.Residents;
    const pil = A.pillars().map(p => '<div class="pillar ' + (p.ok ? 'ok' : 'no') + '"><div><b>' + E(p.title) + '</b><small>' + E(p.detail) + '</small></div><span>' + (p.ok ? 'ERFÜLLT' : 'OFFEN') + '</span></div>').join('');
    const reason = A.canPetition();
    const laws = A.lawIds.map((id, i) => btn((A.hasLaw(id) ? '☑ ' : '☐ ') + A.lawNames[i], 'law', id, true, 'wide left')).join('');
    const res = R.list.map(r => '<div class="res"><b class="' + (R.respects(r) ? 'good' : '') + '">' + E(r.name) + '</b>' + bar((r.respect + 100) / 200, R.respects(r) ? '#5fd17a' : '#d9a640') + '<span class="num">' + Math.round(r.respect) + '</span><small>' + (r.need ? 'braucht ' + r.need.n + '× ' + OG.itemName(r.need.id) : 'zufrieden') + '</small></div>').join('');
    return {
      title: 'Weg der Diplomatie — Eichenhain', wide: true,
      body: '<div class="cols"><div><p class="dim">Um einen Teil des Reiches zu erhalten, musst du so stark sein wie der Kaiser, und alle Einwohner des Landes müssen dich respektieren. Dann prüfen die Arbiter die Drei Säulen (Art. 130).</p>' + pil +
        '<p class="gold strong">Stand: ' + E(A.stateText()) + '</p><p class="' + (reason ? 'dim' : 'good') + '">' + (reason ? 'Noch nicht bereit: ' + E(reason) : 'Bereit: Reiche die Petition bei Richter Ataklas in der Kanzlei ein.') + '</p></div>' +
        '<div><h3>Gesetzbuch <small>(mind. 3, Freier Weg ist Pflicht)</small></h3>' + laws + '<h3>Einwohner von Eichenhain</h3><div class="scroll">' + res + '</div></div></div>',
    };
  },

  pause() {
    return {
      title: 'Menü',
      body: '<p class="dim">' + OG.C.title + ' — ' + OG.C.motto + '</p><div class="mbtns">' + btn('Fortsetzen', 'close', null, true, 'big') + btn('Speichern', 'saveGame', null, true, 'big') +
        btn('Speichern und zum Hauptmenü', 'toMenu', null, true, 'big') + '</div><p class="dim small">Steuerung: WASD laufen · Shift sprinten · Maus zielen · linke Maustaste angreifen · Q/E/R Fähigkeiten · 1/2/3 Tränke · F interagieren · Mausrad zoomen · C I M L J P Fenster · G dem Gericht stellen · F5 speichern</p>',
    };
  },

  gate(name) {
    const P = OG.player;
    const dest = OG.gates.filter(g => g.name !== name).map(g => btn('Reisen nach ' + g.name + ' (' + OG.C.gateFee + ' Taler)', 'travel', g.name, P.taler >= OG.C.gateFee && OG.Law.wanted === 0, 'wide')).join('');
    return {
      title: 'Großes Tor ' + name,
      body: '<p>Die Gebühr setzt der Thron fest; sie ist auf dem ganzen Kontinent gleich: ' + OG.C.gateFee + ' Taler (Art. 34).</p>' + dest + (OG.Law.wanted ? '<p class="bad">Die Torwächter lassen Gesuchte nicht passieren.</p>' : '') +
        '<h3>Verkündungen (Art. 54)</h3>' + (OG.announcements.slice(0, 6).map(a => '<p class="dim small">' + E(a) + '</p>').join('') || '<p class="dim">Keine neuen Verkündungen.</p>'),
    };
  },

  storage() {
    const P = OG.player;
    const col = (inv, act, label) => inv.list().map(({ id, n }) => '<div class="irow"><span>' + OG.itemName(id) + ' <b>×' + n + '</b></span>' + btn(label, act, id) + btn('Alle', act + 'All', id) + '</div>').join('') || '<p class="dim">leer</p>';
    return { title: 'Truhe im Mietzimmer', wide: true, body: '<div class="cols"><div><h3>Inventar</h3><div class="scroll">' + col(P.inv, 'store', 'Einlagern') + '</div></div><div><h3>Truhe</h3><div class="scroll">' + col(P.storage, 'take', 'Nehmen') + '</div></div></div>' };
  },

  townhall() {
    const A = OG.Autonomy, P = OG.player;
    if (!A.townHall) {
      const ok = P.inv.count('holz') >= 30 && P.inv.count('stein') >= 20 && P.inv.count('eisenbarren') >= 10;
      return { title: 'Bauplatz des Rathauses', body: '<p>Eine Autonomie braucht eigene Organe der Verwaltung (Art. 133). Errichte hier das Rathaus.</p><p class="dim">Benötigt: 30 Holz (' + P.inv.count('holz') + '), 20 Stein (' + P.inv.count('stein') + '), 10 Eisenbarren (' + P.inv.count('eisenbarren') + ')</p>' + btn('Rathaus errichten', 'buildHall', null, ok, 'big') };
    }
    return { title: 'Rathaus von Eichenhain', body: '<p>' + (A.isAutonomous() ? 'Sitz der Autonomie „' + E(A.name) + '“.' : 'Das Rathaus steht. Dein Gesetzbuch verwaltest du im Fenster der Drei Säulen (P).') + '</p><p class="gold">Stand: ' + E(A.stateText()) + '</p><p class="dim">Einwohner, die dich respektieren: ' + OG.Residents.respecting() + ' / ' + OG.Residents.list.length + '</p>' };
  },

  npc(n, s) { return OG.npcPanel(n, s); },
};

OG.recipesHtml = forge => {
  const P = OG.player;
  return OG.Recipes.filter(r => forge || !r.forge).map((r, i) => {
    const it = OG.Items[r.result], ok = P.inv.has(r.inputs);
    const parts = r.inputs.map(x => x.n + '× ' + OG.itemName(x.id) + ' (' + P.inv.count(x.id) + ')').join(', ') + (r.forge ? ' · Amboss' : '');
    return '<div class="irow tall"><div><b>' + it.name + '</b> <small>(' + it.mass + ' kg' + (it.length ? ', L ' + it.length + ' m' : '') + (it.armor ? ', Rüstung ' + it.armor : '') + ')</small><small class="' + (ok ? 'dim' : 'bad') + '">' + parts + '</small></div>' + btn('Herstellen', 'craft', OG.Recipes.indexOf(r), ok) + '</div>';
  }).join('');
};

OG.marketHtml = () => {
  const M = OG.Market, P = OG.player;
  return '<p class="' + (M.isOpen() ? 'gold' : 'bad') + '">' + (M.isOpen() ? 'Geöffnet' : 'Geschlossen (6–22 Uhr)') + ' · ' + OG.fmt(P.taler) + ' Taler · Handelsabgabe 10 % an die Kasse (Art. 93)</p><div class="scroll">' +
    Object.keys(M.goods).map(id => {
      const g = M.goods[id], t = M.trend(id), b = M.buyPrice(id), sl = M.sellPrice(id);
      return '<div class="irow"><span>' + OG.itemName(id) + (t > 1.05 ? ' <span class="bad">▲</span>' : t < 0.95 ? ' <span class="good">▼</span>' : '') + '</span><small>Bestand ' + Math.floor(g.stock) + ' · hast ' + P.inv.count(id) + '</small>' +
        btn('Kaufen ' + b + ' T', 'buy', id, M.isOpen() && g.stock >= 1 && P.taler >= b) + btn('Verkaufen ' + sl + ' T', 'sell', id, M.isOpen() && P.inv.count(id) > 0) + '</div>';
    }).join('') + '</div>';
};

// ---------- NPC-Dialoge ----------
OG.npcPanel = (n, s) => {
  const P = OG.player, Q = OG.QuestSys, A = OG.Autonomy;
  const title = n.role === 'Kaiser' ? OG.C.emperorName + ' — ' + OG.C.emperorTitle + ' · Stufe ' + OG.C.emperorLevel : n.npcName + (n.title ? ' — ' + n.title : '');
  let h = '<p class="' + (s.line ? 'gold' : '') + ' speech">' + E(s.line || s.greeting) + '</p>';
  for (const qs of Q.talkSteps(n.npcName)) h += btn('[Auftrag] ' + E(qs.def.title) + ': ' + E(Q.step(qs).text), 'talk', qs.def.id, true, 'wide quest');
  for (const qs of Q.deliverSteps(n.npcName)) { const st = Q.step(qs), have = P.inv.count(st.target); h += btn('[Auftrag] Übergeben: ' + st.count + '× ' + OG.itemName(st.target) + ' (du hast ' + have + ')', 'deliver', qs.def.id, have >= st.count, 'wide quest'); }
  if (n.role !== 'Gildenmeister') for (const q of Q.offersFrom(n.npcName)) h += '<div class="offer"><b>Neuer Auftrag: ' + E(q.title) + '</b><p class="dim small">' + E(q.summary) + '</p>' + btn('Annehmen', 'accept', q.id) + '</div>';

  switch (n.role) {
    case 'Kaiser': {
      if (A.audienceOpen()) {
        if (!s.treaty) h += '<p class="gold">Die Stunde der Audienz ist gekommen. Der Kaiser blickt dir in die Seele — die Prüfung des Blicks (Art. 135).</p>' + btn('Dem Blick des Kaisers standhalten', 'gaze', null, true, 'big');
        else h += '<p class="gold">„Du bist würdig an Seele und an Kraft. Die Drei Säulen stehen. Nenne den Namen deiner Macht und die Farbe deines Wappens.“</p><label for="treatyName">Name der Autonomie</label><input id="treatyName" maxlength="40" value="' + E(s.treatyName) + '">' +
          '<div class="colors">' + A.colors.map((c, i) => '<button class="sw ' + (i === s.color ? 'on' : '') + '" style="background:' + c + '" data-act="color" data-arg="' + i + '" aria-label="Farbe ' + (i + 1) + '"></button>').join('') + '</div>' +
          '<p class="dim small">Der Vertrag bestimmt Name und Wappen, Selbstverwaltung in Eichenhain, Pflichten (Freier Weg, Großer Aufruf, keine Verbotenen Künste, 10 % an die Kasse) und den Schutz durch den Thron (Art. 136).</p>' + btn('Vertrag mit dem Großen Siegel besiegeln', 'sign', null, true, 'big gold');
      } else if (A.state === 'Audienz') h += '<p>„Deine Audienz ist an Tag ' + A.audienceDay + ' um 12 Uhr. Erscheine zur bestimmten Stunde.“</p>';
      else if (A.isAutonomous()) h += '<p>„Herr von ' + E(A.name) + '. Halte die Drei Säulen aufrecht. Fällt eine, fällt dein Vertrag (Art. 140).“</p>';
      else if (P.level < OG.C.emperorLevel) h += '<p>„Du willst einen Teil meines Reiches? Dann werde so stark wie ich — Stufe ' + OG.C.emperorLevel + '. Und jeder einzelne Mensch auf diesem Land muss dich respektieren. Erst dann reiche deine Petition bei Richter Ataklas ein.“</p>';
      else h += '<p>„Du hast meine Stufe erreicht. Respekt deines Volkes, Ressourcen und eigene Ordnung — beweise die Drei Säulen vor den Arbitern.“</p>';
      h += '<p class="dim small">Es gibt zwei Wege zu eigener Macht: den Weg der Diplomatie und den Weg des Krieges. Einen dritten gibt es nicht. (Präambel)</p>';
      break;
    }
    case 'Richter': {
      const r = A.canPetition();
      h += '<p class="gold">Stand deiner Petition: ' + E(A.stateText()) + '</p>' + btn('Petition des Aufstiegs für Eichenhain einreichen (' + OG.C.petitionFee + ' Taler)', 'petition', null, !r, 'wide') + (r ? '<p class="dim small">' + E(r) + '</p>' : '');
      if (!P.permit) h += btn('Kaiserliche Erlaubnis für die Verbotenen Lande (' + OG.C.permitFee + ' Taler, ab Rang D)', 'permit', null, P.rank >= 2 && P.taler >= OG.C.permitFee, 'wide');
      else h += '<p class="good">Du besitzt die Kaiserliche Erlaubnis für die Verbotenen Lande.</p>';
      if (OG.Law.wanted > 0) h += btn('Mich dem Gericht stellen (offenes Bußgeld: ' + OG.Law.pendingFine + ' Taler)', 'surrender', null, true, 'wide');
      h += '<p class="' + (OG.Law.greatTreason ? 'bad' : 'dim') + '">Einträge im Register: ' + OG.Law.record.length + (OG.Law.greatTreason ? ' · GROSSER VERRAT' : '') + '</p>';
      break;
    }
    case 'Haendler': case 'Kraemer': h += OG.marketHtml(); break;
    case 'Schmied': h += '<div class="scroll">' + OG.recipesHtml(true) + '</div>'; break;
    case 'Vermieter': {
      const act = OG.rentUntil >= OG.Clock.day();
      h += '<p class="' + (act ? 'good' : 'dim') + '">' + (act ? 'Dein Zimmer ist bezahlt bis einschließlich Tag ' + OG.rentUntil + '.' : 'Du hast kein Zimmer gemietet.') + '</p><p class="dim small">Bett: schlafen bis 6:00, volle Heilung, Speichern. Truhe: Lager für Vorräte (zählt für die Säule der Ressourcen).</p>' +
        [1, 7, 30].map(d => btn(d + ' Tag(e) mieten — ' + d * OG.C.roomRentPerDay + ' Taler', 'rent', d, P.taler >= d * OG.C.roomRentPerDay, 'wide')).join('');
      break;
    }
    case 'Heiler': h += btn('Heilen lassen (kostenlos)', 'heal', null, P.hp < P.maxHp || P.aether < P.maxAether, 'big'); break;
    case 'Pruefungsmeister':
      h += '<p>Dein Rang: <b>' + OG.Ranks.name(P.rank) + '</b>. Möglich bei Stufe ' + P.level + ': bis ' + OG.Ranks.name(OG.Ranks.cap(P.level)) + '.</p><p class="dim small">Alle drei Wellen = C. Schneller als 40 s: +3, als 70 s: +2, als 110 s: +1. Kaum Schaden: +2. Höchstens so hoch, wie deine Stufe erlaubt.</p>' +
        btn('Prüfung beginnen (' + OG.C.trialFee + ' Taler)', 'trial', null, !OG.Trial.active && OG.Law.wanted === 0 && P.taler >= OG.C.trialFee, 'big');
      break;
    case 'Gildenmeister': {
      const ac = Q.activeContracts();
      h += '<p>Aktive Gildenaufträge: ' + ac + ' / 3. Neue Aufträge gibt es jeden Morgen um 6 Uhr.</p>' + (Q.offered.map(c => '<div class="offer"><b>' + E(c.title) + '</b><p class="dim small">' + E(c.summary) + '</p><p class="small">Belohnung: ' + (40 + 10 * P.level) + ' Taler und ein Viertel einer Stufe an Erfahrung.</p>' + btn('Annehmen', 'accept', c.id, ac < 3) + '</div>').join('') || '<p class="dim">Heute gibt es keine weiteren Aufträge.</p>');
      break;
    }
    case 'Wache': if (OG.Law.wanted > 0) h += btn('Mich ergeben (Gericht binnen drei Tagen, Art. 29)', 'surrender', null, true, 'big'); break;
    case 'Ordensmagier': h += '<p class="dim">Siegel der Eindämmung: ' + Math.round(OG.Rift.strength) + ' %. Verstärke es mit Ätherkristallen — Pflicht jedes Bürgers (Art. 60 Nr. 3).</p>'; break;
    case 'Einwohner': {
      const r = n.resident;
      h += '<p class="' + (OG.Residents.respects(r) ? 'good' : '') + '">Respekt: ' + Math.round(r.respect) + ' (respektiert dich ab ' + OG.C.respectThreshold + ')</p>';
      if (!r.need) h += '<p class="dim">„Heute fehlt mir nichts. Danke, dass du fragst.“</p>';
      else { const has = P.inv.count(r.need.id) >= r.need.n; h += '<p>„Ich bräuchte ' + r.need.n + '× ' + OG.itemName(r.need.id) + '. Kannst du helfen?“</p>' + btn(has ? 'Geben: ' + r.need.n + '× ' + OG.itemName(r.need.id) : 'Du hast nicht genug ' + OG.itemName(r.need.id), 'give', null, has, 'big'); }
      break;
    }
    case 'Soeldnerfuehrer':
      h += '<p>Dorfwachen: ' + A.guards + ' / ' + OG.C.pillarGuards + ' (höchstens ' + OG.C.maxGuards + '). Anwerbung ' + OG.C.guardHireCost + ' Taler, Sold ' + OG.C.guardUpkeep + ' Taler pro Tag und Wache.</p>' + btn('Wache anwerben (' + OG.C.guardHireCost + ' Taler)', 'hire', null, A.guards < OG.C.maxGuards && P.taler >= OG.C.guardHireCost, 'big');
      break;
  }
  if (s.result) h += '<p class="gold result">' + E(s.result) + '</p>';
  return { title, wide: ['Haendler', 'Kraemer', 'Schmied'].includes(n.role), body: h };
};

// ---------- Aktionen der Fenster ----------
OG.Actions = {
  close() { OG.UI.close(); },
  alloc(i) { OG.player.allocate(+i); },
  second(id) { OG.player.chooseSecond(id); },
  unequip(w) { OG.player.unequip(w === 'w'); },
  equip(id) { OG.player.equip(id); },
  use(id) { OG.player.use(id); },
  drop(id) { OG.player.inv.remove(id, 1); },
  craft(i) {
    const r = OG.Recipes[+i], P = OG.player;
    if (!P.inv.has(r.inputs)) return;
    for (const x of r.inputs) P.inv.remove(x.id, x.n);
    const n = r.result === 'eisenbarren' && P.race.id === 'zwerg' ? 2 : 1;
    P.inv.add(r.result, n); P.gainExp(10); OG.log('Hergestellt: ' + n + '× ' + OG.itemName(r.result));
  },
  tab(i) { OG.UI.state.tab = +i; },
  law(id) { OG.Autonomy.toggleLaw(id); },
  saveGame() { OG.save(false); },
  toMenu() { OG.save(true); OG.running = false; OG.UI.close(); OG.UI.showMenu('start'); },
  travel(name) {
    const P = OG.player, g = OG.gates.find(x => x.name === name);
    if (!g || !P.spend(OG.C.gateFee)) return;
    OG.Treasury.add('Torgebühren', OG.C.gateFee);
    P.x = g.x; P.y = g.y + 3.5; P.vx = P.vy = 0;
    OG.fx.ring(P.x, P.y, 3, '#66b3ff', 0.8); OG.UI.close();
  },
  store(id) { const P = OG.player; if (P.inv.remove(id, 1)) P.storage.add(id, 1); },
  storeAll(id) { const P = OG.player, n = P.inv.count(id); if (P.inv.remove(id, n)) P.storage.add(id, n); },
  take(id) { const P = OG.player; if (P.storage.remove(id, 1)) P.inv.add(id, 1); },
  takeAll(id) { const P = OG.player, n = P.storage.count(id); if (P.storage.remove(id, n)) P.inv.add(id, n); },
  buildHall() { OG.Autonomy.buildTownHall(); OG.UI.close(); },
  talk(id) { const s = OG.QuestSys.active.find(q => q.def.id === id); if (s) OG.UI.state.line = OG.QuestSys.completeTalk(s); },
  deliver(id) { const s = OG.QuestSys.active.find(q => q.def.id === id); if (s && OG.QuestSys.deliver(s)) OG.UI.state.result = 'Übergeben.'; },
  accept(id) { const q = OG.QuestSys.find(id) || OG.QuestSys.offered.find(c => c.id === id); if (q) OG.QuestSys.accept(q); },
  gaze() {
    const f = OG.Autonomy.gaze();
    if (f) { OG.Autonomy.reject('Der Kaiser verweigert den Vertrag: ' + f); OG.UI.state.result = 'Der Kaiser verweigert den Vertrag. Begründung: ' + f; }
    else OG.UI.state.treaty = true;
  },
  color(i) { OG.UI.state.color = +i; },
  sign() { const s = OG.UI.state; if ((s.treatyName || '').trim().length < 3) { s.result = 'Der Name braucht mindestens 3 Zeichen.'; return; } OG.Autonomy.sign(s.treatyName, s.color); OG.UI.close(); },
  petition() { OG.Autonomy.submit(); OG.UI.state.result = 'Die Petition ist eingereicht. Die Revision dauert ' + OG.C.revisionDays + ' Tage.'; },
  permit() {
    const P = OG.player;
    if (P.rank < 2 || !P.spend(OG.C.permitFee)) return;
    OG.Treasury.add('Gebühren', OG.C.permitFee); P.permit = true; P.inv.add('erlaubnis', 1); OG.UI.state.result = 'Erlaubnis erteilt (Art. 112).';
  },
  surrender() { OG.Law.arrest(); OG.UI.close(); },
  rent(d) {
    const P = OG.player, cost = d * OG.C.roomRentPerDay;
    if (!P.spend(cost)) return;
    OG.rentUntil = Math.max(OG.rentUntil, OG.Clock.day() - 1) + +d;
    OG.Treasury.add('Miete', cost); OG.UI.state.result = 'Gemietet bis Tag ' + OG.rentUntil + '.';
  },
  heal() { const P = OG.player; P.hp = P.maxHp; P.aether = P.maxAether; OG.UI.state.result = 'Du bist vollständig geheilt.'; },
  trial() { const P = OG.player; if (!P.spend(OG.C.trialFee)) return; OG.Treasury.add('Gebühren', OG.C.trialFee); OG.UI.close(); OG.Trial.begin(); },
  give() { const n = OG.UI.arg; if (OG.Residents.fulfil(n.resident, OG.player)) OG.UI.state.result = n.resident.name + ' ist dir dankbar (+12 Respekt, alle anderen +1).'; },
  hire() { OG.Autonomy.hireGuard(); },
  buy(id) { const e = OG.Market.buy(OG.player, id); if (e) OG.UI.state.result = e; },
  sell(id) { const e = OG.Market.sell(OG.player, id); if (e) OG.UI.state.result = e; },

  // Hauptmenü
  menuContinue() { const d = OG.Store.load(); if (d) OG.start(d); },
  menuNew() { OG.UI.mstate.screen = 'create'; OG.UI.renderMenu(); setTimeout(() => { const i = $('#charName'); if (i) i.focus(); }, 30); },
  menuEdict() { OG.UI.mstate.screen = 'edict'; OG.UI.renderMenu(); },
  menuBack() { OG.UI.mstate.screen = 'start'; OG.UI.renderMenu(); },
  pickRace(i) { OG.UI.mstate.race = +i; OG.UI.renderMenu(); },
  pickClass(i) { OG.UI.mstate.cls = +i; OG.UI.renderMenu(); },
  toggleDemo(a, el) { OG.UI.mstate.demo = el.checked; },
  menuStart() {
    const s = OG.UI.mstate, name = (OG.UI.state.charName || ($('#charName') || {}).value || '').trim();
    if (name.length < 2) { const i = $('#charName'); if (i) { i.focus(); i.placeholder = 'Bitte mindestens 2 Zeichen'; } return; }
    const c = OG.Classes[s.cls];
    const inv = { heiltrank: 3, gebratenes_fleisch: 4 };
    if (c.id === 'kampfmagier') inv.aetherelixier = 2;
    OG.Store.clear();
    OG.start({ player: { name, race: OG.Races[s.race].id, classId: c.id, weapon: c.weapon, armor: c.armor, demo: s.demo, inventory: inv, taler: 150 } });
  },
};

// ---------- Karte ----------
OG.drawMap = cv => {
  if (!cv || !OG.World.mini) return;
  const c = cv.getContext('2d'), S = cv.width / 400;
  c.imageSmoothingEnabled = true;
  c.drawImage(OG.World.mini, 0, 0, cv.width, cv.height);
  const m = (x, y) => [(x + 200) * S, (y + 200) * S];
  const mark = (p, label, col) => { if (!p) return; const [x, y] = m(p.x, p.y); c.fillStyle = col; c.fillRect(x - 4, y - 4, 8, 8); c.font = '600 14px "Barlow Semi Condensed", sans-serif'; c.fillStyle = 'rgba(0,0,0,0.7)'; c.fillText(label, x + 8, y + 6); c.fillStyle = col; c.fillText(label, x + 7, y + 5); };
  const [, by] = m(0, OG.L.forbiddenY);
  c.fillStyle = 'rgba(180,90,255,0.85)'; c.fillRect(0, by - 1, cv.width, 2);
  c.font = '600 13px "Barlow Semi Condensed", sans-serif'; c.fillText('Grenze der Verbotenen Lande (Art. 112)', 8, by - 6);
  const P = OG.Places;
  mark(P.plaza, 'Grad Genesis', '#f2c14d'); mark(P.guildHall, 'Gildenhaus', '#8fe08f'); mark(P.arena, 'Arena', '#e6eef5');
  mark(P.village, OG.Autonomy.isAutonomous() ? OG.Autonomy.name : 'Eichenhain', '#7deb8f');
  for (const g of OG.gates) mark(g, 'Tor', '#66b3ff');
  mark(P.seal, 'Siegel ' + Math.round(OG.Rift.strength) + ' %', '#cc80ff'); mark(P.rift, 'Der Riss', '#b34dff');
  mark(OG.L.hills, 'Erzhöhen', '#c9c2b3');
  for (const s of OG.QuestSys.active) {
    const st = OG.QuestSys.step(s); if (!st || !st.place) continue;
    const p = OG.QuestSys.place(st.place); if (!p) continue;
    const [x, y] = m(p.x, p.y);
    c.fillStyle = '#f2c14d'; c.beginPath(); c.moveTo(x, y - 9); c.lineTo(x + 7, y); c.lineTo(x, y + 9); c.lineTo(x - 7, y); c.fill(); c.strokeStyle = '#000'; c.lineWidth = 2; c.stroke();
    c.fillStyle = '#f2c14d'; c.font = '600 12px "Barlow Semi Condensed", sans-serif'; c.fillText(s.def.title, x + 10, y + 16);
  }
  const Pl = OG.player, [px, py] = m(Pl.x, Pl.y);
  c.fillStyle = '#ffeb3b'; c.beginPath(); c.arc(px, py, 6, 0, Math.PI * 2); c.fill();
  c.strokeStyle = '#ffeb3b'; c.lineWidth = 3; c.beginPath(); c.moveTo(px, py); c.lineTo(px + Math.cos(Pl.face) * 14, py + Math.sin(Pl.face) * 14); c.stroke();
};
