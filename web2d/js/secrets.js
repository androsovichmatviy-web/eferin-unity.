// Omni Genesis 2D — Geheimnisse der Welt (Easter Eggs). Wer sucht, wird belohnt.
'use strict';

OG.drawHooks = OG.drawHooks || [];

OG.Secrets = {
  defs: [
    { id: 'splitter', name: 'Die acht Sternensplitter', hint: 'Acht Splitter des Sterns der Genesis liegen über den Kontinent verstreut — einer sogar hinter dem Riss. Sie glitzern nur, wenn man ganz nah ist.' },
    { id: 'thron', name: 'Der leere Thron', hint: 'Jeder will wissen, wie es sich anfühlt, auf dem Thron der Genesis zu sitzen.' },
    { id: 'brunnen', name: 'Der Mitternachtsbrunnen', hint: 'Der Brunnen am Platz von Grad Genesis hört nur in der Stunde nach Mitternacht zu. Er nimmt nur Münzen.' },
    { id: 'mitternacht', name: 'Audienz um Mitternacht', hint: 'Schläft der Kaiser eigentlich jemals? Frag ihn, wenn alle anderen schlafen.' },
    { id: 'goldlaeufer', name: 'Der goldene Grasläufer', hint: 'Jäger erzählen von einem goldenen Grasläufer auf den Wiesen westlich von Eichenhain. Wer ihn tötet, findet nur Staub. Nachts soll er schlafen — man darf nur nicht rennen.' },
    { id: 'weltenrand', name: 'Am Rand der Welt', hint: 'Wo endet die Karte des Throns? Geh so weit, wie die Füße tragen.' },
    { id: 'archiv', name: 'Der erste Entwurf', hint: 'Im Regal der Kanzlei liegt etwas, das älter ist als jedes Urteil.' },
    { id: 'stern', name: 'Das Wort des Sterns', hint: 'Im Hauptmenü dreht sich der Stern der Genesis. Er hat einen Namen — und acht Zacken.' },
  ],
  shardPos: [[14, 158], [-150, 150], [165, 120], [-170, -60], [95, -95], [-60, -150], [150, -170], [0, -185]],
  found: [], shards: [], golden: null, goldenDay: -1, edgeT: 0,

  init(s) {
    s = s || {};
    this.found = (s.found || []).filter(id => this.def(id));
    this.shards = (s.shards || []).slice();
    this.goldenDay = -1; this.golden = null;
    let star = false;
    try { star = localStorage.getItem('og2d_stern') === '1'; } catch (e) { /* ohne Speicher */ }
    if (star && !this.has('stern')) this.found.push('stern');

    // Splitter: nur sichtbar und aufhebbar, wenn man nah ist.
    this.spots = [];
    this.shardPos.forEach((p, i) => {
      let [x, y] = p;
      for (let k = 0; k < 12 && OG.World.blocked(x, y, 0.5); k++) { x += OG.rand(-2, 2); y += OG.rand(-2, 2); }
      const sp = new OG.Spot(x, y, 1.8, () => 'Sternensplitter aufheben', () => this.takeShard(i, sp), () => !this.shards.includes(i));
      sp.shard = i;
      this.spots.push(sp);
    });
    // Der Thron (nur der Kaiser sitzt darauf, Art. 61)
    new OG.Spot(0, 153.4, 1.9, () => 'Auf dem Thron Platz nehmen', () => this.throne());
    // Brunnen am Platz
    new OG.Spot(0, 113, 2.8, () => OG.Clock.hour() === 0 ? 'Eine Münze in den Brunnen werfen (1 Taler)' : 'Brunnen (das Wasser ist still)', () => this.well());
    // Regal in der Kanzlei
    new OG.Spot(-30.2, 121.2, 1.6, () => 'Regal der Kanzlei durchsehen', () => this.archive());

    OG.Clock.onHour.push((d, h) => this.onHour(d, h));
    this.onHour(OG.Clock.day(), OG.Clock.hour());
    OG.drawHooks.push((ctx, v) => this.draw(ctx, v));
  },
  def(id) { return this.defs.find(d => d.id === id); },
  has(id) { return this.found.includes(id); },
  // Jeder gefundene Splitter: +0,5 % Erfahrung, alle acht zusammen +5 %, alle Geheimnisse +5 % dazu.
  expBonus() { return (this.shards.length >= 8 ? 0.05 : this.shards.length * 0.005) + (this.found.length >= this.defs.length ? 0.05 : 0); },

  discover(id, text) {
    if (this.has(id)) return false;
    this.found.push(id);
    const P = OG.player, d = this.def(id);
    OG.post('[Geheimnis] ' + d.name + ' — ' + text, 'secret');
    OG.UI.banner('GEHEIMNIS ENTDECKT', '#e6b3ff');
    P.gainExp(OG.Player.expToNext(P.level) * 0.3 / (1 + P.race.expBonus));
    OG.fx.ring(P.x, P.y, 3, '#e6b3ff', 0.8);
    if (this.found.length >= this.defs.length) {
      OG.announce(P.charName + ' hat alle Geheimnisse von Omni Genesis gefunden und trägt nun den Titel „Hüter der Geheimnisse“.');
      OG.sys('Alle ' + this.defs.length + ' Geheimnisse gefunden! Titel: Hüter der Geheimnisse. Dauerhaft +5 % Erfahrung.');
    } else OG.sys('Geheimnisse gefunden: ' + this.found.length + ' / ' + this.defs.length + ' (Edikt J → Geheimnisse).');
    OG.save(true);
    return true;
  },

  takeShard(i, sp) {
    if (this.shards.includes(i)) return;
    this.shards.push(i);
    OG.fx.burst(sp.x, sp.y, '#f2c14d', 14);
    const n = this.shards.length;
    OG.sys('Sternensplitter ' + n + ' / 8. ' + (n < 8 ? 'Er wärmt die Hand (+0,5 % Erfahrung).' : 'Die acht Splitter fügen sich zu einem Stern zusammen.'));
    if (n >= 8) this.discover('splitter', 'Der Stern der Genesis ist wieder ganz. Dauerhaft +5 % Erfahrung.');
  },

  throne() {
    const P = OG.player;
    OG.UI.banner('NUR EINER', '#f2c14d');
    OG.post('[' + OG.C.emperorName + '] „Auf diesem Thron sitzt nur einer: ich. So steht es im Edikt (Art. 61), und so bleibt es. … Aber ich mag Leute, die neugierig sind.“', 'sys');
    P.knock(0, -P.mass * 5);
    this.discover('thron', 'Der Kaiser lässt dich nicht Platz nehmen — aber er lächelt.');
  },

  well() {
    const P = OG.player;
    if (OG.Clock.hour() !== 0) { OG.sys('Das Wasser ist still. Man erzählt sich, der Brunnen höre nur in der Stunde nach Mitternacht zu.'); return; }
    if (!P.spend(1)) { OG.sys('Du hast keine Münze.'); return; }
    OG.fx.ring(0, 113, 1.4, '#9fd0ff', 0.8);
    const r = Math.random();
    if (r < 0.3) { P.addBuff({ id: 'brunnen', name: 'Segen des Brunnens', until: OG.time + 300, dmg: 1.15 }); OG.sys('Das Wasser leuchtet auf. Segen des Brunnens: +15 % Schaden für 5 Minuten.'); }
    else if (r < 0.6) { P.hp = P.maxHp; P.aether = P.maxAether; OG.sys('Kühles Licht steigt auf. Leben und Äther sind voll.'); }
    else if (r < 0.85) { const n = OG.randInt(5, 30); P.taler += n; OG.sys('Der Brunnen gibt etwas zurück: ' + n + ' Taler glitzern am Rand.'); }
    else { P.inv.add('aetherkristall', 1); OG.sys('Am Grund liegt ein Ätherkristall. Du fischst ihn heraus.'); }
    this.discover('brunnen', 'Der Brunnen hat deine Münze angenommen.');
  },

  archive() {
    OG.UI.openPanel('secretText', {
      title: 'Der erste Entwurf des Edikts',
      text: ['Ein vergilbter Bogen, älter als jedes Urteil in diesem Raum. Die ersten Zeilen kennst du: „Ein Kontinent. Ein Thron. Ein Gesetz.“',
        'Viele Artikel sind durchgestrichen, neu geschrieben, wieder durchgestrichen. Am Rand stehen Rechnungen: F = m · a, und daneben ein kleiner gezeichneter Wolf.',
        'Ganz unten steht in großer, schwungvoller Schrift: „Diese Welt habe ich erdacht.“',
        'Darunter, in der strengen Hand des Obersten Arbiters: „Bezeugt. — Ataklas.“'],
    });
    this.discover('archiv', 'Du hast den ersten Entwurf des Edikts gefunden.');
  },

  // Wird aus der Begrüßung des Kaisers aufgerufen.
  emperorLine() {
    if (OG.Clock.hour() !== 0) return null;
    this.discover('mitternacht', 'Der Kaiser ist wach. Er ist immer wach.');
    return '„Du fragst, ob ich schlafe? Hundertsechsundsechzig Artikel, ein Kontinent, ein Riss im Norden. Einer muss wach bleiben. … Geh schlafen, ' + OG.player.charName + '. Ich halte Wache.“';
  },

  // Hauptmenü: STERN tippen.
  menuKey(code) {
    const want = ['KeyS', 'KeyT', 'KeyE', 'KeyR', 'KeyN'];
    this.typed = (this.typed || []).concat(code).slice(-5);
    if (want.every((k, i) => this.typed[i] === k)) {
      this.typed = [];
      try { localStorage.setItem('og2d_stern', '1'); } catch (e) { /* ohne Speicher */ }
      OG.menuStarBoost = performance.now();
      return true;
    }
    return false;
  },

  onHour(day, hour) {
    // Der goldene Grasläufer zeigt sich an manchen Tagen (bis man ihn gefunden hat).
    if (!this.has('goldlaeufer') && this.goldenDay !== day && hour >= 18) {
      this.goldenDay = day;
      if (Math.random() < 0.5 && (!this.golden || this.golden.corpse || this.golden.removed)) this.spawnGolden();
    }
    if (this.golden && !this.golden.corpse && hour === 7 && Math.random() < 0.5) { this.golden.removed = true; this.golden = null; }
  },
  spawnGolden() {
    let x = -150, y = 40;
    for (let k = 0; k < 20; k++) { x = OG.rand(-175, -125); y = OG.rand(10, 70); if (!OG.World.blocked(x, y, 0.8)) break; }
    const g = new OG.Creature(OG.Species.graslaeufer, x, y, { managed: true, age: 100 });
    g.golden = true; g.tint = '#e6b84d'; g.expMult = 0; g.home = { x, y };
    const base = g.think.bind(g);
    // Schlafend bemerkt er nur, wer rennt.
    g.think = () => {
      const P = OG.player;
      if (OG.Clock.isNight() && g.state !== 'Flee') {
        const running = Math.hypot(P.moveX || 0, P.moveY || 0) > 6.5, d = Math.hypot(P.x - g.x, P.y - g.y);
        if (!running || d > 8) { g.state = 'Sleep'; return; }
      }
      base();
    };
    g.canInteract = () => !g.corpse && g.state === 'Sleep';
    g.prompt = () => 'Den goldenen Grasläufer ganz vorsichtig streicheln';
    g.interact = () => {
      OG.fx.burst(g.x, g.y, '#f2c14d', 24);
      g.removed = true; this.golden = null;
      OG.player.addBuff({ id: 'gold', name: 'Goldglanz', until: OG.time + 600, statMult: 1.1 });
      this.discover('goldlaeufer', 'Er öffnet kurz die Augen, schnaubt — und löst sich in goldenes Licht auf. Goldglanz: +10 % auf alle Werte für 10 Minuten.');
    };
    g.onDeathCb = () => { this.golden = null; if (g.lastAttacker === OG.player || (g.lastAttacker && g.lastAttacker.isAlly)) OG.sys('Das Gold zerfällt zu Staub. Der goldene Grasläufer war nicht zum Jagen da. Vielleicht zeigt sich ein anderer …'); };
    this.golden = g;
  },

  update(dt) {
    this.edgeT -= dt;
    if (this.edgeT > 0) return;
    this.edgeT = 1;
    const P = OG.player;
    if (!this.has('weltenrand') && Math.max(Math.abs(P.x), Math.abs(P.y)) > 189)
      this.discover('weltenrand', 'Hier endet die Karte des Throns. Dahinter liegt nur Nebel — vielleicht wartet dort eines Tages eine Welt in drei Dimensionen.');
  },

  draw(ctx, v) {
    const P = OG.player, t = OG.time;
    for (const sp of this.spots) {
      if (this.shards.includes(sp.shard)) continue;
      const d = Math.hypot(sp.x - P.x, sp.y - P.y);
      if (d > 12) continue;
      const a = OG.clamp01(1 - d / 12) * (0.55 + 0.45 * Math.sin(t * 4 + sp.shard));
      ctx.save(); ctx.globalAlpha = a; ctx.translate(sp.x, sp.y); ctx.rotate(t * 0.8);
      OG.drawStar(ctx, 0, 0, 0.35, '#f2d77a');
      ctx.restore();
    }
    if (this.golden && !this.golden.corpse && !this.golden.removed && Math.hypot(this.golden.x - P.x, this.golden.y - P.y) < 30) {
      ctx.save(); ctx.globalAlpha = 0.25 + 0.15 * Math.sin(t * 3);
      ctx.fillStyle = '#f2c14d'; ctx.beginPath(); ctx.arc(this.golden.x, this.golden.y, 1.3, 0, Math.PI * 2); ctx.fill();
      ctx.restore();
    }
    // Regal in der Kanzlei
    if (v && P.x > -31 && P.x < -21 && P.y > 120 && P.y < 132) {
      ctx.fillStyle = '#5c4026'; ctx.fillRect(-30.6, 120.5, 1.6, 0.5);
      ctx.fillStyle = '#e6dcc0'; ctx.fillRect(-30.4, 120.6, 0.3, 0.3); ctx.fillStyle = '#8c3a3a'; ctx.fillRect(-29.9, 120.6, 0.25, 0.3); ctx.fillStyle = '#3a5a8c'; ctx.fillRect(-29.5, 120.6, 0.3, 0.3);
    }
  },

  save() { return { found: this.found, shards: this.shards }; },

  panelHtml() {
    const E = OG.esc;
    return '<p class="dim">Omni Genesis versteckt Geheimnisse. Jedes gefundene bringt Erfahrung; wer alle findet, wird „Hüter der Geheimnisse“ (+5 % Erfahrung). Sternensplitter: ' + this.shards.length + ' / 8.</p>' +
      '<p class="gold strong">Gefunden: ' + this.found.length + ' / ' + this.defs.length + (this.found.length >= this.defs.length ? ' · Hüter der Geheimnisse' : '') + '</p>' +
      this.defs.map(d => this.has(d.id) ? '<h4 class="good">✓ ' + E(d.name) + '</h4>' : '<h4 class="dim">??? </h4><p class="small dim">Hinweis: ' + E(d.hint) + '</p>').join('');
  },
};
