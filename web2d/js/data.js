// Omni Genesis 2D — Regelwerk und Daten (gleiche Werte wie die Unity-Version).
'use strict';

OG.C = {
  title: 'Omni Genesis',
  motto: 'Ein Kontinent. Ein Thron. Ein Gesetz.',
  maxLevel: 100, secondClassLevel: 50, statPointsPerLevel: 3,
  emperorName: 'Mortem Underwood', emperorTitle: 'Oberster Kaiser des Großen Reiches Omni Genesis', emperorLevel: 100,
  judgeName: 'Ataklas',
  secondsPerGameHour: 60,
  revisionDays: 3,
  tradeTax: 0.10, gateFee: 25, roomRentPerDay: 30, petitionFee: 1000, permitFee: 200, trialFee: 100,
  respectThreshold: 50,
  pillarTaler: 20000, pillarFood: 30, pillarWood: 30, pillarIron: 15, pillarGameStock: 0.5,
  pillarGuards: 4, pillarLaws: 3, guardHireCost: 400, guardUpkeep: 20, maxGuards: 6, treatyWarningDays: 3,
  // Physik: F = m · a
  damagePerNewton: 0.5, damagePerJoule: 0.3, impactTime: 0.1, knockbackScale: 8,
  migration: true, migrationChance: 0.25,
  contractExpLevels: 0.25,
};

// ---------- Rassen (Art. 11, 24: gleiche Summe an Boni) ----------
OG.Races = [
  { id: 'mensch', name: 'Mensch', str: 1, agi: 1, vit: 2, int: 1, expBonus: 0.05, carry: 0, aetherRegen: 0, scale: 1, skin: '#e9c7a3',
    desc: 'Vielfalt der Wege. Ausgeglichen, lernt schneller (+5 % Erfahrung).' },
  { id: 'zwerg', name: 'Zwerg', str: 2, agi: 0, vit: 3, int: 0, expBonus: 0, carry: 15, aetherRegen: 0, scale: 0.85, skin: '#d6a784',
    desc: 'Meister der Schmieden. Robust, trägt 15 kg mehr, erhält beim Schmelzen einen Barren extra.' },
  { id: 'elf', name: 'Elf', str: 0, agi: 3, vit: 0, int: 2, expBonus: 0, carry: 0, aetherRegen: 0.25, scale: 1.05, skin: '#f3e0cc',
    desc: 'Erben der alten Künste. Gewandt und klug, Äther regeneriert 25 % schneller.' },
];
OG.race = id => OG.Races.find(r => r.id === id) || OG.Races[0];

// ---------- Klassen (gleiche Struktur, gleiches Budget) ----------
const A = (id, name, kind, cd, cost, power, radius, dur, desc) => ({ id, name, kind, cd, cost, power, radius, dur, desc });
OG.Classes = [
  { id: 'novize', name: 'Novize', desc: 'Ein Anfänger ohne festen Weg. Wächst in allen Werten gleichmäßig.',
    str: 6, agi: 6, vit: 6, int: 6, g: [1, 1, 1, 1], weapon: 'holzknueppel', armor: 'stoffkleidung', color: '#9e9a8c',
    q: A('kraftschlag', 'Kraftschlag', 'PowerStrike', 5, 8, 2.2, 0, 0, 'Ein wuchtiger Schlag mit doppelter Kraft.'),
    e: A('erste_hilfe', 'Erste Hilfe', 'Heal', 20, 15, 0.3, 0, 5, 'Heilt 30 % der Lebenspunkte über 5 Sekunden.') },
  { id: 'schwertkaempfer', name: 'Schwertkämpfer', desc: 'Meister der Klinge. Stark und ausdauernd.',
    str: 8, agi: 6, vit: 7, int: 3, g: [2, 1, 1, 0], weapon: 'kurzschwert', armor: 'lederruestung', color: '#c0392e',
    q: A('wirbelschlag', 'Wirbelschlag', 'Spin', 6, 12, 1.4, 3, 0, 'Ein Rundumschlag um 360°.'),
    e: A('ansturm', 'Ansturm', 'Charge', 8, 15, 1, 7, 0.35, 'Sturmangriff über 7 m — der Anlauf erhöht a und damit F.') },
  { id: 'kampfmagier', name: 'Kampfmagier', desc: 'Beherrscht den Äther. Zerbrechlich, aber mächtig.',
    str: 3, agi: 5, vit: 5, int: 11, g: [0, 1, 0, 3], weapon: 'lehrlingsstab', armor: 'stoffkleidung', color: '#4d59d9',
    q: A('aetherbolzen', 'Ätherbolzen', 'Bolt', 4, 18, 1.6, 3, 0, 'Schwere Äthermasse, explodiert beim Aufprall.'),
    e: A('frostring', 'Frostring', 'Nova', 9, 25, 1.2, 6, 4, 'Eisring um dich, verlangsamt Gegner um 50 %.') },
  { id: 'bogenschuetze', name: 'Bogenschütze', desc: 'Präzise auf Distanz. Gewandt und schnell.',
    str: 5, agi: 10, vit: 5, int: 4, g: [1, 2, 1, 0], weapon: 'jagdbogen', armor: 'lederruestung', color: '#3f8c3f',
    q: A('pfeilsalve', 'Pfeilsalve', 'Volley', 6, 15, 0.8, 0, 0, 'Fünf Pfeile im Fächer.'),
    e: A('durchschuss', 'Durchschuss', 'Pierce', 7, 15, 2, 0, 0, 'Doppelte Zugkraft, durchschlägt bis zu 5 Ziele.') },
  { id: 'assassine', name: 'Assassine', desc: 'Schnell, lautlos, tödlich aus dem Schatten.',
    str: 5, agi: 11, vit: 4, int: 4, g: [1, 2, 0, 1], weapon: 'dolche', armor: 'lederruestung', color: '#4a3f5c',
    q: A('schattenschritt', 'Schattenschritt', 'Blink', 7, 15, 1, 8, 3, 'Sprung 8 m nach vorn, der nächste Treffer ist kritisch.'),
    e: A('giftklinge', 'Giftklinge', 'PoisonBuff', 14, 20, 1, 0, 8, '8 s lang vergiften deine Treffer das Ziel.') },
  { id: 'berserker', name: 'Berserker', desc: 'Rohe Kraft. Schwere Waffen, maximale Masse — maximales F.',
    str: 11, agi: 5, vit: 7, int: 1, g: [2, 0, 2, 0], weapon: 'kriegsaxt', armor: 'lederruestung', color: '#8c4d1f',
    q: A('erdstoss', 'Erdstoß', 'Slam', 7, 15, 1.5, 4.5, 0, 'Körpermasse × Aufprallbeschleunigung: Flächenschaden.'),
    e: A('blutrausch', 'Blutrausch', 'Rage', 18, 20, 1, 0, 10, '10 s: +35 % Tempo, 15 % Lebensraub, +20 % erlittener Schaden.') },
];
OG.cls = id => OG.Classes.find(c => c.id === id) || OG.Classes[0];

OG.Hybrids = [
  { id: 'runenritter', name: 'Runenritter', a: 'schwertkaempfer', b: 'kampfmagier', desc: 'Runen verstärken die Klinge.', r: A('runenklinge', 'Runenklinge', 'RuneBuff', 25, 35, 0.8, 2.5, 12, '12 s: jeder Nahkampftreffer löst eine Ätherexplosion aus.') },
  { id: 'waechter', name: 'Wächter', a: 'schwertkaempfer', b: 'bogenschuetze', desc: 'Schild und Armbrust.', r: A('schildwall', 'Schildwall', 'ShieldWall', 22, 30, 0.4, 0, 8, '8 s: −60 % Schaden, kein Rückstoß.') },
  { id: 'duellant', name: 'Duellant', a: 'schwertkaempfer', b: 'assassine', desc: 'Parade und Konter.', r: A('konter', 'Konter', 'Counter', 8, 20, 3, 2.5, 1, '1 s Parade: der nächste Treffer wird abgewehrt und dreifach erwidert.') },
  { id: 'kriegsherr', name: 'Kriegsherr', a: 'schwertkaempfer', b: 'berserker', desc: 'Schwere Waffen, größte Wucht.', r: A('kriegsschrei', 'Kriegsschrei', 'WarCry', 25, 35, 1.3, 8, 12, 'Stoßwelle mit 1200 N, danach 12 s +30 % Schaden.') },
  { id: 'arkanschuetze', name: 'Arkanschütze', a: 'kampfmagier', b: 'bogenschuetze', desc: 'Magische Pfeile.', r: A('aetherpfeil', 'Ätherpfeil', 'HomingArrow', 8, 30, 1.2, 0, 0, 'Drei zielsuchende Ätherpfeile.') },
  { id: 'nekromant', name: 'Nekromant', a: 'kampfmagier', b: 'assassine', desc: 'Ruft die Gefallenen als Diener.', r: A('totenruf', 'Totenruf', 'RaiseDead', 30, 40, 1, 15, 60, 'Erhebt bis zu 2 Leichen für 60 s. Tote bleiben tot — es ist nur ein Echo.') },
  { id: 'blutmagier', name: 'Blutmagier', a: 'kampfmagier', b: 'berserker', desc: 'Zauber kosten Leben statt Äther.', r: A('blutlanze', 'Blutlanze', 'BloodLance', 6, 0, 3, 0, 0, 'Kostet 15 % LP: durchschlagende Lanze mit dreifacher Kraft.') },
  { id: 'kopfgeldjaeger', name: 'Kopfgeldjäger', a: 'bogenschuetze', b: 'assassine', desc: 'Markiert Ziele und kassiert Kopfgeld.', r: A('kopfgeld', 'Kopfgeld', 'Bounty', 12, 20, 1.4, 30, 20, 'Markiert das anvisierte Ziel: +40 % Schaden, die Kasse zahlt beim Tod.') },
  { id: 'wildjaeger', name: 'Wildjäger', a: 'bogenschuetze', b: 'berserker', desc: 'Zähmt Tiere des Ökosystems.', r: A('zaehmen', 'Zähmen', 'Tame', 20, 30, 1, 6, 0, 'Zähmt ein geschwächtes Wildtier (< 40 % LP) als Begleiter.') },
  { id: 'klingentaenzer', name: 'Klingentänzer', a: 'assassine', b: 'berserker', desc: 'Schnelle Klingenserien.', r: A('klingentanz', 'Klingentanz', 'BladeDance', 10, 30, 0.7, 3.5, 1.2, 'Sechs Schläge in 1,2 s rund um dich.') },
];
OG.PioneerSkill = A('weg_des_pioniers', 'Weg des Pioniers', 'Pioneer', 30, 30, 0.25, 0, 15, '15 s: alle Werte +25 %, doppelte Sammelausbeute.');
OG.hybrid = (a, b) => {
  if (!b || a === b) return null;
  if (a === 'novize' || b === 'novize') {
    const other = a === 'novize' ? b : a;
    return { id: 'pionier', name: 'Pionier', a: 'novize', b: other, desc: 'Universalist mit dem Weg des ' + OG.cls(other).name + 's.', r: OG.PioneerSkill };
  }
  return OG.Hybrids.find(h => (h.a === a && h.b === b) || (h.a === b && h.b === a)) || null;
};

// ---------- Gegenstände ----------
OG.Items = {};
const item = (id, name, cat, mass, price, extra) => { OG.Items[id] = Object.assign({ id, name, cat, mass, price, tradable: true }, extra || {}); };
item('holz', 'Holz', 'Material', 2, 4);
item('stein', 'Stein', 'Material', 3, 3);
item('eisenerz', 'Eisenerz', 'Material', 3, 9);
item('eisenbarren', 'Eisenbarren', 'Material', 1.5, 25);
item('heilkraut', 'Heilkraut', 'Material', 0.1, 6);
item('fleisch', 'Fleisch', 'Material', 1, 8);
item('fell', 'Fell', 'Material', 1.5, 12);
item('aetherkristall', 'Ätherkristall', 'Material', 0.2, 60);
item('leerenessenz', 'Leerenessenz', 'Material', 0.3, 45);
item('holzknueppel', 'Holzknüppel', 'Waffe', 1.2, 5, { kind: 'Nahkampf', length: 0.8, swing: 0.36 });
item('kurzschwert', 'Kurzschwert', 'Waffe', 1.1, 40, { kind: 'Nahkampf', length: 0.8, swing: 0.28 });
item('langschwert', 'Langschwert', 'Waffe', 1.5, 120, { kind: 'Nahkampf', length: 1.05, swing: 0.32 });
item('grossschwert', 'Großschwert', 'Waffe', 3.5, 260, { kind: 'Nahkampf', length: 1.4, swing: 0.5 });
item('kriegsaxt', 'Kriegsaxt', 'Waffe', 2.8, 150, { kind: 'Nahkampf', length: 1.1, swing: 0.46 });
item('dolche', 'Dolche', 'Waffe', 0.5, 60, { kind: 'Nahkampf', length: 0.4, swing: 0.16 });
item('lehrlingsstab', 'Lehrlingsstab', 'Waffe', 1.6, 40, { kind: 'Stab', focus: 1 });
item('aetherstab', 'Ätherstab', 'Waffe', 1.8, 220, { kind: 'Stab', focus: 1.4 });
item('jagdbogen', 'Jagdbogen', 'Waffe', 0.9, 45, { kind: 'Bogen', draw: 110 });
item('langbogen', 'Langbogen', 'Waffe', 1.3, 140, { kind: 'Bogen', draw: 180 });
item('stoffkleidung', 'Stoffkleidung', 'Ruestung', 2, 10, { armor: 2 });
item('lederruestung', 'Lederrüstung', 'Ruestung', 6, 80, { armor: 6 });
item('eisenruestung', 'Eisenrüstung', 'Ruestung', 15, 300, { armor: 16 });
item('heiltrank', 'Heiltrank', 'Verbrauch', 0.3, 30, { heal: 60 });
item('aetherelixier', 'Ätherelixier', 'Verbrauch', 0.3, 50, { aether: 50 });
item('gebratenes_fleisch', 'Gebratenes Fleisch', 'Verbrauch', 0.5, 12, { heal: 25 });
item('erlaubnis', 'Kaiserliche Erlaubnis', 'Dokument', 0, 0, { tradable: false });
OG.FISTS = { id: 'faeuste', name: 'Fäuste', cat: 'Waffe', kind: 'Nahkampf', mass: 0.6, length: 0.6, swing: 0.25 };
OG.itemName = id => (OG.Items[id] ? OG.Items[id].name : id);

const R = (result, forge, ...pairs) => {
  const inputs = [];
  for (let i = 0; i < pairs.length; i += 2) inputs.push({ id: pairs[i], n: pairs[i + 1] });
  return { result, n: 1, forge, inputs };
};
OG.Recipes = [
  R('eisenbarren', true, 'eisenerz', 2, 'holz', 1),
  R('langschwert', true, 'eisenbarren', 3, 'holz', 1),
  R('grossschwert', true, 'eisenbarren', 6, 'holz', 2),
  R('kriegsaxt', true, 'eisenbarren', 4, 'holz', 2),
  R('dolche', true, 'eisenbarren', 2, 'fell', 1),
  R('eisenruestung', true, 'eisenbarren', 8, 'fell', 2),
  R('langbogen', false, 'holz', 4, 'fell', 1),
  R('aetherstab', false, 'holz', 3, 'aetherkristall', 1),
  R('lederruestung', false, 'fell', 4),
  R('heiltrank', false, 'heilkraut', 3),
  R('aetherelixier', false, 'aetherkristall', 1, 'heilkraut', 2),
  R('gebratenes_fleisch', false, 'fleisch', 1, 'holz', 1),
];

// ---------- Arten (2D-Koordinaten: y zeigt nach Süden) ----------
OG.Species = {};
const sp = def => { OG.Species[def.id] = Object.assign({ armor: 0, attackRange: 1.6, aggro: 10, size: 1, capacity: 0, initial: 0, breed: 0, drops: [], ecosystem: true, nocturnal: false, ranged: false }, def); };
sp({ id: 'graslaeufer', name: 'Grasläufer', diet: 'Pflanzenfresser', temper: 'Scheu', shape: 'tier', mass: 45, hp: 50, speed: 6.5, accel: 0, aggro: 12,
  capacity: 22, initial: 16, breed: 0.12, exp: 15, drops: [['fleisch', 2], ['fell', 1]], hab: [-40, 10], habR: 75, color: '#b88c59' });
sp({ id: 'wildeber', name: 'Wildeber', diet: 'Pflanzenfresser', temper: 'Territorial', shape: 'tier', mass: 85, hp: 120, armor: 4, speed: 5, accel: 0.6, aggro: 7, size: 1.1,
  capacity: 10, initial: 8, breed: 0.05, exp: 35, drops: [['fleisch', 3], ['fell', 1]], hab: [60, -50], habR: 60, color: '#5a4033' });
sp({ id: 'grauwolf', name: 'Grauwolf', diet: 'Fleischfresser', temper: 'Feindselig', shape: 'tier', mass: 40, hp: 70, armor: 2, speed: 7.5, accel: 0.8, aggro: 14, size: 0.9,
  capacity: 8, initial: 6, breed: 0.04, exp: 40, nocturnal: true, drops: [['fell', 2], ['fleisch', 1]], hab: [10, -70], habR: 80, color: '#808088' });
sp({ id: 'steinkriecher', name: 'Steinkriecher', diet: 'Pflanzenfresser', temper: 'Territorial', shape: 'kriecher', mass: 140, hp: 220, armor: 14, speed: 3.2, accel: 0.4, aggro: 7, size: 1.2,
  capacity: 6, initial: 5, breed: 0.02, exp: 60, drops: [['stein', 3], ['eisenerz', 1]], hab: [125, 20], habR: 45, color: '#736e66' });
sp({ id: 'leerenbrut', name: 'Leerenbrut', diet: 'Leere', temper: 'Feindselig', shape: 'leere', mass: 60, hp: 160, armor: 6, speed: 5.5, accel: 1, aggro: 25,
  exp: 80, ecosystem: false, nocturnal: true, drops: [['leerenessenz', 1]], hab: [0, -150], habR: 50, color: '#5a1a80' });
sp({ id: 'risswaechter', name: 'Wächter des Risses', diet: 'Leere', temper: 'Feindselig', shape: 'golem', mass: 400, hp: 2500, armor: 25, speed: 4, accel: 0.5, attackRange: 3, aggro: 30, size: 2.4,
  exp: 1500, ecosystem: false, drops: [['aetherkristall', 5], ['leerenessenz', 5]], hab: [0, -160], habR: 25, color: '#330d4d' });
sp({ id: 'pruefungsgolem', name: 'Prüfungsgolem', diet: 'Konstrukt', temper: 'Feindselig', shape: 'golem', mass: 90, hp: 80, armor: 2, speed: 4, accel: 0.45, aggro: 40,
  exp: 0, ecosystem: false, hab: [0, 0], habR: 12, color: '#8c99b3' });
sp({ id: 'kultist', name: 'Vermummter Kultist', diet: 'Kult', temper: 'Feindselig', shape: 'mensch', mass: 75, hp: 140, armor: 4, speed: 4.6, accel: 0.5, attackRange: 12, aggro: 22,
  exp: 70, ecosystem: false, ranged: true, projMass: 0.3, projAccel: 150, drops: [['leerenessenz', 1]], hab: [0, 0], habR: 20, color: '#40204d' });
sp({ id: 'kultmeisterin', name: 'Kultmeisterin Sela', diet: 'Kult', temper: 'Feindselig', shape: 'mensch', mass: 70, hp: 700, armor: 10, speed: 4.8, accel: 0.6, attackRange: 14, aggro: 26, size: 1.15,
  exp: 400, ecosystem: false, ranged: true, projMass: 0.45, projAccel: 180, drops: [['aetherkristall', 2], ['leerenessenz', 2]], hab: [0, 0], habR: 20, color: '#731a59' });
sp({ id: 'kasimir', name: 'Kasimir Vorn', diet: 'Kult', temper: 'Feindselig', shape: 'mensch', mass: 90, hp: 2200, armor: 15, speed: 5, accel: 0.8, attackRange: 16, aggro: 32, size: 1.35,
  exp: 1500, ecosystem: false, ranged: true, projMass: 0.6, projAccel: 220, drops: [['aetherkristall', 4], ['leerenessenz', 4]], hab: [0, 0], habR: 20, color: '#260d33' });

// ---------- Ränge (Art. 48, 69) ----------
OG.Ranks = {
  names: ['F', 'E', 'D', 'C', 'B', 'A', 'S', 'SS', 'SSS'],
  levelReq: [1, 5, 10, 20, 30, 45, 60, 80, 100],
  colors: ['#b3b3b3', '#bfe6bf', '#66d973', '#59d9e6', '#668cff', '#bf73ff', '#ffd14d', '#ff8c33', '#ff4059'],
  name(i) { return i < 0 ? '—' : this.names[OG.clamp(i, 0, 8)]; },
  color(i) { return i < 0 ? '#808080' : this.colors[OG.clamp(i, 0, 8)]; },
  cap(level) { let c = 0; this.levelReq.forEach((r, i) => { if (level >= r) c = i; }); return c; },
};

// ---------- Geschichte und Aufträge ----------
const Talk = (npc, place, text, line, give) => ({ type: 'Talk', target: npc, place, text, line, give });
const Kill = (species, count, place, text, spawn) => ({ type: 'Kill', target: species, count, place, text, spawn });
const Clue = (clue, count, place, text, spawn) => ({ type: 'Investigate', target: clue, count, place, text, spawn });
const Deliver = (itemId, count, npc, place, text) => ({ type: 'Deliver', target: itemId, count, npc, place, text });
OG.Quests = [
  { id: 'haupt1', main: true, title: 'Kapitel I: Bürger des Reiches', giver: 'Ataklas', minLevel: 1,
    summary: 'Jeder Reisende braucht das Siegel des Bürgers (Art. 87). Melde dich in der Kanzlei von Grad Genesis.',
    steps: [
      Talk('Ataklas', 'kanzlei', 'Melde dich bei Richter Ataklas in der Kanzlei',
        '„Ein neuer Name für das Register der Bürgerschaft. Das Siegel des Bürgers gibt dir Rechte — und Pflichten (Art. 60). Zeig zuerst, was du kannst: Die Prüfung der Meisterschaft wartet in der Arena westlich der Stadt.“'),
      { type: 'Rank', count: 0, place: 'arena', text: 'Lege die Prüfung der Meisterschaft ab (mindestens Rang F)' },
      Talk('Ataklas', 'kanzlei', 'Berichte Richter Ataklas',
        '„Die Arbiter haben deine Prüfung bezeugt. Willkommen in der Ordnung des Throns, Bürger. Werde stärker — der Orden der Kampfmagier wird bald Hilfe brauchen.“'),
    ],
    expLevels: 1, taler: 150, items: [['heiltrank', 2]], done: 'Du trägst nun das Siegel des Bürgers.' },
  { id: 'haupt2', main: true, requires: 'haupt1', minLevel: 3, grantPermit: true, title: 'Kapitel II: Das müde Siegel', giver: 'Ataklas',
    summary: 'Der Orden meldet: Das Siegel der Eindämmung schwindet schneller, als der Riss es erklären kann. Richter Ataklas stellt dir eine Sondererlaubnis aus (Art. 112).',
    steps: [
      Talk('Magistra Velde', 'grenzposten', 'Sprich mit Magistra Velde am Grenzposten',
        '„Das Siegel verliert jede Nacht mehr Kraft, als der Riss allein nehmen kann. Am Kraterrand liegen Splitter, die nicht dorthin gehören. Bring sie mir. Und nimm diese Kristalle für das Siegel.“', [['aetherkristall', 2]]),
      Clue('runensplitter', 3, 'kraterrand', 'Untersuche die Runensplitter am Kraterrand', 'splitter'),
      { type: 'Seal', count: 60, place: 'siegel', text: 'Verstärke das Siegel auf mindestens 60 %' },
      Talk('Magistra Velde', 'grenzposten', 'Bring die Splitter zu Magistra Velde',
        '„Diese Runen sind Verbotene Kunst (Art. 60 Nr. 5). Jemand schwächt das Siegel mit Absicht. Und in Eichenhain verschwinden seit Wochen Menschen in der Nacht. Hedda, die Söldnerführerin dort, weiß mehr.“'),
    ],
    expLevels: 1.2, taler: 250, items: [['aetherelixier', 2]], done: 'Das Siegel wird mit Absicht geschwächt. Die Spur führt nach Eichenhain.' },
  { id: 'haupt3', main: true, requires: 'haupt2', minLevel: 5, title: 'Kapitel III: Die Verschwundenen von Eichenhain', giver: 'Hedda',
    summary: 'In Eichenhain verschwinden nachts Menschen. Am Waldrand im Nordwesten des Dorfes gibt es verbrannte Zeichen.',
    steps: [
      Talk('Hedda', 'eichenhain', 'Sprich mit Hedda in Eichenhain',
        '„Drei Leute fort, keine Leiche, keine Spur — nur verbrannte Zeichen am Waldrand im Nordwesten. Meine Leute trauen sich nicht hin. Du schon?“'),
      Clue('brandzeichen', 3, 'waldrand', 'Untersuche die Brandzeichen am Waldrand', 'brandzeichen'),
      Kill('kultist', 3, 'waldrand', 'Stelle die Vermummten am Waldrand', 'kult_wald'),
      Clue('brief', 1, 'waldrand', 'Durchsuche das Feuer der Vermummten', 'brief'),
      Talk('Hedda', 'eichenhain', 'Zeige Hedda den Brief',
        '„Das Wappen von Haus Vorn … Die Vorns hielten diese Lande vor dem Edikt. Sie verweigerten den Vasalleneid und verloren alles an die Kronendomäne. Bring den Brief zu Richter Ataklas. Und danke — Eichenhain vergisst das nicht.“'),
    ],
    expLevels: 1.2, taler: 300, respect: 10, done: 'Die Vermissten waren Opfer eines Rituals. Dahinter steht Haus Vorn.' },
  { id: 'haupt4', main: true, requires: 'haupt3', minLevel: 6, title: 'Kapitel IV: Das Zeugnis der Arbiter', giver: 'Ataklas',
    summary: 'Ein Brief mit dem Wappen von Haus Vorn. Nur ein Arbiter kann bezeugen, ob er echt ist (Art. 97).',
    steps: [
      Talk('Ataklas', 'kanzlei', 'Bring den Brief zu Richter Ataklas',
        '„Mein Siegel der Unparteilichkeit lügt nicht (Art. 97): Der Brief ist echt. Haus Vorn plant weder Vertrag noch offenen Vorstoß — es plant eine Meuterei. Eine Meuterei ist ein Verbrechen, kein Weg (Art. 141). Der Kaiser muss es erfahren.“'),
      Talk('Mortem Underwood', 'thronsaal', 'Berichte Kaiser Mortem Underwood im Thronsaal',
        '„Wer das Siegel bricht, um Land zu gewinnen, gewinnt nichts als Schande. Eine Meuterei wird mit aller Macht niedergeschlagen — ohne Frist und ohne Feld (Art. 153). Finde ihr Lager, Bürger. Der Thron sieht dich.“'),
    ],
    expLevels: 0.8, taler: 400, done: 'Der Kaiser hat dich beauftragt, die Meuterei aufzuhalten.' },
  { id: 'haupt5', main: true, requires: 'haupt4', minLevel: 8, title: 'Kapitel V: Das Lager in den Erzhöhen', giver: 'Mortem Underwood',
    summary: 'Die Meuterer von Haus Vorn verstecken sich in den Erzhöhen östlich der Provinz.',
    steps: [
      { type: 'Reach', target: 'lager', place: 'lager', radius: 22, text: 'Finde das Lager von Haus Vorn in den Erzhöhen' },
      Kill('kultist', 5, 'lager', 'Besiege die Kultisten im Lager', 'kult_lager'),
      Kill('kultmeisterin', 1, 'lager', 'Besiege Kultmeisterin Sela', 'sela'),
      Clue('ritualplan', 1, 'lager', 'Durchsuche Selas Zelt', 'ritualplan'),
      Talk('Magistra Velde', 'grenzposten', 'Bring den Ritualplan zu Magistra Velde',
        '„Eine Nacht am Riss … Sie wollen das Siegel auf einen Schlag brechen und die Leerenbrut über Grünmark lassen. Im Chaos will Kasimir Vorn die Lande an sich reißen. Wir müssen dort sein — in der Nacht.“'),
    ],
    expLevels: 1.5, taler: 500, items: [['aetherkristall', 2]], done: 'Das Ritual soll in der Nacht am Riss stattfinden.' },
  { id: 'haupt6', main: true, requires: 'haupt5', minLevel: 12, title: 'Kapitel VI: Die Nacht des Risses', giver: 'Magistra Velde',
    summary: 'Kasimir Vorn will das Siegel in einer Nacht brechen. Halte ihn auf.',
    steps: [
      { type: 'Reach', target: 'riss', place: 'riss', radius: 30, night: true, text: 'Geh in der Nacht (21–6 Uhr) zum Riss' },
      Kill('kasimir', 1, 'riss', 'Besiege Kasimir Vorn', 'ritual'),
      { type: 'Seal', count: 80, place: 'siegel', text: 'Stelle das Siegel wieder her (mindestens 80 %)' },
      Talk('Mortem Underwood', 'thronsaal', 'Erstatte dem Kaiser Bericht',
        '„Kasimir Vorn wählte die Meuterei — und fiel. Du hast das Siegel gehalten, wie es jeder Bürger soll (Art. 60). Das Reich erkennt dich als Hüter von Grünmark an. Und höre: Es gibt einen rechtmäßigen Weg zu eigener Macht. Werde so stark wie ich, gewinne dein Volk — und reiche deine Petition ein.“'),
    ],
    expLevels: 2.5, taler: 2000, respect: 20, titleReward: 'Hüter von Grünmark', hook: 'finale', done: 'Die Meuterei ist gescheitert. Der Weg der Diplomatie steht dir offen.' },

  { id: 'neben_woelfe', title: 'Wölfe vor Eichenhain', giver: 'Jonas Falk', minLevel: 2,
    summary: 'Die Grauwölfe reißen nachts die Grasläufer am Dorfrand. Erlege drei — aber bedenke: Getötete Tiere kehren nicht zurück.',
    steps: [Kill('grauwolf', 3, null, 'Erlege drei Grauwölfe')], expLevels: 0.5, taler: 150, respect: 4 },
  { id: 'neben_bestand', title: 'Der Bestand erholt sich', giver: 'Greta Moor', minLevel: 2,
    summary: 'Ohne Grasläufer kein Fleisch. Lass den Bestand auf 18 Tiere wachsen — jage keine und halte die Wölfe fern.',
    steps: [{ type: 'Population', target: 'graslaeufer', count: 18, text: 'Bestand der Grasläufer auf 18 bringen' }], expLevels: 0.5, taler: 120, respect: 6 },
  { id: 'neben_fleisch', title: 'Der Preis des Fleisches', giver: 'Mirela', minLevel: 1,
    summary: 'Die Fleischpreise steigen. Bring Mirela zehn Stück Fleisch für den Markt.',
    steps: [Deliver('fleisch', 10, 'Mirela', 'marktplatz', 'Bring Mirela 10 Fleisch')], expLevels: 0.4, taler: 220, hook: 'markt_fleisch' },
  { id: 'neben_erz', title: 'Eisen für die Legion', giver: 'Gundram', minLevel: 3,
    summary: 'Die Legion braucht Klingen. Bring dem Schmied fünf Eisenbarren — er bezahlt mit einem Langschwert.',
    steps: [Deliver('eisenbarren', 5, 'Gundram', 'schmiede', 'Bring Gundram 5 Eisenbarren')], expLevels: 0.5, taler: 100, items: [['langschwert', 1]] },
  { id: 'neben_kraeuter', title: 'Kräuter für das Lazarett', giver: 'Schwester Alwine', minLevel: 1,
    summary: 'Das Lazarett heilt jeden (Art. 45) — dafür braucht es Kräuter. Bring zehn Heilkräuter.',
    steps: [Deliver('heilkraut', 10, 'Schwester Alwine', 'lazarett', 'Bring Schwester Alwine 10 Heilkräuter')], expLevels: 0.3, taler: 60, items: [['heiltrank', 4]] },
  { id: 'neben_kriecher', title: 'Steine im Weg', giver: 'Oskar', minLevel: 4,
    summary: 'Steinkriecher blockieren die Erzgruben in den Erzhöhen. Vertreibe zwei — dauerhaft.',
    steps: [Kill('steinkriecher', 2, 'erzhoehen', 'Besiege zwei Steinkriecher')], expLevels: 0.6, taler: 180, items: [['eisenerz', 4]] },
  { id: 'neben_grenze', title: 'Grenzpatrouille', giver: 'Magister Aurel', minLevel: 6, requires: 'haupt2',
    summary: 'Leerenbrut streift an der Grenze. Der Orden zahlt für jede, die nicht mehr streift.',
    steps: [Kill('leerenbrut', 4, 'riss', 'Besiege vier Leerenbrut')], expLevels: 0.8, taler: 250, items: [['aetherkristall', 2]] },
  { id: 'neben_bote', title: 'Ein Bericht für die Kanzlei', giver: 'Hedda', minLevel: 1,
    summary: 'Hedda schickt einen Bericht über die Dorfwache an die Kanzlei. Die Großen Tore bringen dich schnell hin.',
    steps: [Talk('Ataklas', 'kanzlei', 'Übergib Heddas Bericht an Richter Ataklas', '„Ein Bericht über die Wache von Eichenhain. Ordentlich geführt. Die Kanzlei dankt.“')],
    expLevels: 0.2, taler: 80 },
];
OG.quest = id => OG.Quests.find(q => q.id === id) || null;

// ---------- Auszüge aus dem Edikt (Vorlage des Autors, sinngemäß übertragen) ----------
OG.EdictPreamble = 'Ein Kontinent. Ein Thron. Ein Gesetz. — Mortem Underwood, Oberster Kaiser des Großen Reiches Omni Genesis, verkündet dieses Edikt allen Völkern, Großen Häusern, Gilden und Landen. Aus der Asche der Ära der Spaltung erhob sich der Thron der Genesis und vereinte den Kontinent. Würdigen lässt das Gesetz zwei Wege zu eigener Macht: den Weg der Diplomatie und den Weg des Krieges. Einen dritten Weg gibt es nicht.';
OG.Edict = [
  ['Art. 11', 'Völker des Kontinents', 'Das Reich fördert die Einheit von Menschen, Zwergen, Elfen und anderen Völkern.', 'Wahl der Rasse.'],
  ['Art. 16', 'Siegel und Gleichgewicht', 'Das Reich hütet die Siegel, die Risse und alte Mächte eindämmen.', 'Siegel der Eindämmung am Riss.'],
  ['Art. 20', 'Symbole', 'Das Reichsbanner ist mitternachtsazurblau mit dem goldenen achtzackigen Stern der Genesis. Hauptstadt ist Grad Genesis.', 'Banner und Krone der Acht Sterne.'],
  ['Art. 24', 'Gleichheit', 'Bürger sind gleich, unabhängig von Rasse, Herkunft, Rang oder magischer Gabe.', 'Alle Rassen und Klassen haben dasselbe Budget.'],
  ['Art. 27–29', 'Leben und Festnahme', 'Niemand darf willkürlich getötet werden. Festgenommene kommen binnen drei Tagen vor Gericht.', 'Wachen verfolgen Täter, Urteil binnen drei Spieltagen. Wer einen friedlichen Reisenden tötet, trägt Blutschuld; Gesetzlose (rote Kapuze) dürfen gegen Kopfgeld gestellt werden. Notwehr ist kein Verbrechen.'],
  ['Art. 33–34', 'Freier Weg und Große Tore', 'Jeder darf frei reisen. Die Torgebühr setzt der Thron einheitlich fest.', 'Schnellreise für 25 Taler.'],
  ['Art. 45', 'Heilung', 'Jeder hat das Recht auf Heilung in den kaiserlichen Lazaretten.', 'Kostenlose Heilung, dort erwacht man nach einer Niederlage.'],
  ['Art. 48, 69', 'Ränge des Aufstiegs', 'Ränge vergibt die Kaiserliche Prüfung. Die höchsten Ränge verleiht der Kaiser.', 'Ränge F–SSS in der Arena.'],
  ['Art. 54', 'Verkündung', 'Edikt und Ukase werden durch die Großen Tore verkündet.', 'Verkündungen an den Toren.'],
  ['Art. 57–58', 'Unschuld und Sühne', 'Jeder gilt als unschuldig bis zum Beweis. Niemand wird für dieselbe Tat zweimal bestraft.', 'Nach dem Urteil ist die Tat gesühnt.'],
  ['Art. 60', 'Pflichten', 'Das Edikt achten, beim Großen Aufruf verteidigen, die Siegel hüten, Abgaben zahlen, keine Verbotenen Künste.', 'Siegel verstärken, Abgaben, Großer Aufruf.'],
  ['Art. 62', 'Unantastbarkeit des Kaisers', 'Ein Anschlag auf den Kaiser ist Großer Verrat.', 'Verwirkt den Weg zur Autonomie für immer.'],
  ['Art. 66', 'Großer Aufruf', 'Der Kaiser ruft den Kontinent angesichts einer Bedrohung zur Mobilmachung.', 'Siegel unter 15 %.'],
  ['Art. 87', 'Kanzlei', 'Die Kanzlei nimmt Petitionen an, führt die Register und stellt das Siegel des Bürgers aus.', 'Kapitel I.'],
  ['Art. 93', 'Abgaben', 'Abgaben setzt allein der Thron fest; sie sind überall gleich.', '10 % Handelsabgabe an die Kasse.'],
  ['Art. 112', 'Verbotene Lande', 'Betreten nur mit kaiserlicher Erlaubnis; der Orden der Kampfmagier wacht.', 'Erlaubnis ab Rang D in der Kanzlei.'],
  ['Art. 116–117', 'Miete', 'Häuser und Land werden gemietet; Einnahmen mit Abgabe an die Kasse.', 'Zimmer im Mietshaus.'],
  ['Art. 121', 'Gilden', 'Gilden handeln nach der Charta des Throns und sind vor dem Gesetz gleich.', 'Tägliche Gildenaufträge bei Runa.'],
  ['Art. 124', 'Fehden und Ehrenduell', 'Private Kriege sind verboten. Streit schlichtet der Oberste Arbiter — oder, mit Erlaubnis des Throns, ein Ehrenduell.', 'Ehrenduell mit anderen Reisenden (F): mit Einsatz, wer unter 15 % fällt, gibt auf.'],
  ['Art. 127–133', 'Weg der Diplomatie', 'Ein würdiger Anführer darf eine eigene Autonomie gründen, wenn die Drei Säulen stehen: Ressourcen, Volk, Souveränität.', 'Fenster „Drei Säulen“ (P).'],
  ['Art. 134–136', 'Petition, Revision, Audienz, Vertrag', 'Richter Ataklas prüft die Petition, bestimmt Tag und Stunde der Audienz; der Kaiser prüft mit seinem Blick und besiegelt den Vertrag.', 'Endziel: Eichenhain als Autonomie.'],
  ['Art. 140', 'Auflösung', 'Fällt eine Säule, folgt eine Vorwarnung mit Frist, danach die Auflösung.', 'Tägliche Prüfung nach dem Vertrag.'],
  ['Art. 141, 153', 'Meuterei', 'Meuterei ist ein heimtückischer Aufstand ohne Manifest — ein Verbrechen, kein Weg.', 'Hauptgeschichte: Haus Vorn.'],
];
