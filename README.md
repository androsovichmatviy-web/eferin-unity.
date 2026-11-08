# Omni Genesis

**Eine lebendige Simulationswelt nach dem Kaiserlichen Edikt — Unity 6 (URP), C#.**

*Ein Kontinent. Ein Thron. Ein Gesetz.*

Omni Genesis ist eine offene Sandbox-Welt mit einer eigenen Geschichte. Die Welt läuft nach eigenen Regeln:
Tiere vermehren sich und sterben, Preise steigen und fallen mit Angebot und Nachfrage, das Siegel des Risses
wird schwächer, Wachen verfolgen Verbrecher. Grundlage ist das **Kaiserliche Edikt von Omni Genesis**
(166 Artikel). Viele Artikel sind direkt als Spielmechanik umgesetzt. Im Spiel öffnet die Taste **J** die Auszüge des Edikts,
jeweils mit dem Hinweis, welche Mechanik sie umsetzt.

Die Hauptgeschichte erzählt von einer **Meuterei gegen das Edikt** (sechs Kapitel). Danach wartet das große Ziel (Art. 127): ein eigener Teil des Reiches, die **Autonomie**. Dafür muss der Prätendent
**so stark sein wie Kaiser Mortem Underwood (Stufe 100)**, und **alle Einwohner** des Landes müssen ihn respektieren.

---

## 2D-Version im Browser (`web2d/`)

Dasselbe Spiel mit Blick von oben, direkt im Browser spielbar, auch auf dem Handy (virtueller Stick und Touch-Knöpfe).
Gleiche Welt, gleiche Regeln: Physik F = m · a, Klassen und Hybride, Ökosystem, Markt, Recht, Siegel des Risses,
Prüfung der Meisterschaft, Hauptgeschichte, Neben- und Gildenaufträge, Weg der Diplomatie.

* Starten: `web2d/index.html` über einen beliebigen lokalen Webserver öffnen, z. B. `python3 -m http.server --directory web2d`
  und dann `http://localhost:8000` aufrufen. (Die Datei beginnt ohne `<!doctype>`, weil sie auch als Seite auf claude.ai veröffentlicht wird.)
* Spielstand: im `localStorage` des Browsers.
* Aufbau: `js/util.js` (Rauschen, Eingabe), `data.js` (Regelwerk, Geschichte, Edikt), `world.js` (Gelände, Gebäude),
  `entities.js` (Spieler, Physik), `actors.js` (Tiere, NPCs), `systems.js` (Uhr, Wetter, Ökosystem, Markt, Recht, Siegel, Einwohner),
  `society.js` (Autonomie, Rangprüfung, Aufträge), `rivals.js` (andere Reisende, Gesetzlose, Ehrenduell),
  `secrets.js` (Geheimnisse), `render.js`, `ui.js`, `main.js`.
* Raubtiere außer Sicht des Spielers (über 45 m) jagen abstrakt weiter, damit das Ökosystem stabil bleibt
  (Test über 12 Spieltage: 12–18 Grasläufer, 5–8 Wölfe). Wachen greifen Wildtiere nur an, wenn diese gerade jemanden angreifen.

### Freies Spiel: Leveln, andere Reisende, PvP

* **Die Geschichte ist freiwillig.** Kapitel bieten ihre Auftraggeber an (goldenes `!`), nichts startet von selbst.
  Man kann genauso gut nur leveln, jagen, handeln und kämpfen.
* **Zehn andere Reisende** ziehen durch die Welt, steigen selbst auf, jagen, kämpfen und stehen in der **Rangliste** (Edikt `J` → Rangliste).
  Der Kaiser steht über allen (Stufe 100) — Kaiser ist nur er.
* **Gesetzlose** (rote Kapuze, ☠) überfallen Reisende außerhalb der Städte (Neulingsschutz bis Stufe 5). Ihr Versteck liegt westlich der Arena.
  Wer sie besiegt, bekommt Beute, Erfahrung und **Kopfgeld** aus der Kaiserlichen Kasse.
* **Friedliche Reisende angreifen** geht (genau anvisieren oder im Gesprächsfenster „Trotzdem angreifen“), ist aber ein Verbrechen (Art. 27).
  Wer einen tötet, trägt **Blutschuld**: Die Legion jagt ihn, und starke Reisende werden zu Kopfgeldjägern. **Notwehr** ist kein Verbrechen.
* **Ehrenduell** (Art. 124): Mit `F` einen Reisenden ansprechen und fordern. Einsatz 10 Taler pro Stufe des Gegners; wer unter 15 % Leben fällt, gibt auf.

### Geheimnisse

Acht Geheimnisse sind versteckt (Edikt `J` → Geheimnisse zeigt Hinweise). Jedes bringt Erfahrung; wer alle findet, wird
„Hüter der Geheimnisse“ (+5 % Erfahrung). Andere Reisende und Bürger erzählen manchmal Gerüchte, die weiterhelfen.

---

## Installation

Voraussetzung: **Unity 6 LTS** mit der Vorlage **Universal 3D (URP)**.

1. Unity Hub → *New project* → **Universal 3D** → Projekt anlegen.
2. Den Ordner `Assets/OmniGenesis` aus diesem Repository in den Ordner `Assets` des Projekts kopieren
   (oder das Projekt mit Git verbinden, siehe unten).
3. In Unity im Menü **Omni Genesis → Projekt einrichten** klicken.
   Das legt die Szenen `Hauptmenue` und `Welt` an und trägt sie in die Build Settings ein
   (nötig für das asynchrone Laden).
4. **Play** drücken.

Mehr ist nicht nötig. Welt, Gebäude, Figuren, Tiere und Oberfläche werden beim Start aus Code erzeugt.
Die Eingabe funktioniert mit dem alten Input Manager und mit dem neuen Input System.
Im Menü **Omni Genesis → Spielstand löschen** wird der Spielstand zurückgesetzt.

### Projekt mit Git verbinden

```bash
git init -b main
git remote add origin https://github.com/androsovichmatviy-web/eferin-unity.
git pull origin main
```

`.meta`-Dateien werden mit eingecheckt, `Library/`, `Temp/` usw. sind in `.gitignore` ausgeschlossen.

---

## Steuerung

| Taste | Aktion |
|---|---|
| WASD · Shift · Leertaste | Laufen · Sprinten · Springen |
| Maus · LMB | Umsehen · Angriff (gedrückt halten) |
| Q / E / R | Klassenfähigkeiten (R ab Stufe 50 mit Hybridklasse) |
| 1 / 2 / 3 | Heiltrank · Ätherelixier · Gebratenes Fleisch |
| F | Interagieren (Sprechen, Sammeln, Ausweiden, Tore, Bett …) |
| **V** | **Ich-Perspektive ↔ dritte Person** |
| C · I · M · **L** · J · P | Charakter · Inventar/Handwerk · Karte · **Aufträge** · Edikt · Drei Säulen |
| G | Sich dem Gericht stellen (wenn gesucht) |
| F5 · Esc | Schnellspeichern · Menü |
| Alt (halten) | Mauszeiger freigeben |
| F1–F4, F6 | nur im **Demo-Modus**: +5 Stufen · +5000 Taler · +6 Stunden · Respekt +25 · Siegel −30 % |

Der **Demo-Modus** wird bei der Charaktererstellung gewählt. Damit lassen sich bei einer Präsentation alle Systeme
in wenigen Minuten zeigen, bis hin zum Vertrag mit dem Kaiser.

---

## Systeme

### Physikalisches Kampfsystem: F = m · a

Jeder Treffer wird physikalisch berechnet (`Core/Phys.cs`). Das Kampfprotokoll unten links zeigt die Rechnung live.

| Angriff | Rechnung |
|---|---|
| Nahkampf | Spitzengeschwindigkeit `v = θ · L / t`, Beschleunigung `a = v / t` (+ `v_Anlauf / t`), **`F = m_Waffe · a`**, Schaden = `F · 0,5` |
| Stärke | verkürzt die Schwungzeit `t = t₀ / (1 + 0,01 · Stärke)`, kleineres t ergibt größeres a |
| Bogen | Sehnenkraft auf den Pfeil: `a = F / m`, aus `v² = 2 · a · d` folgt `v = √(2 · F · d / m)`, Schaden aus `E = ½ · m · v²` beim Aufprall (mit Schwerkraft) |
| Äther | Äthermasse × Beschleunigung durch Intelligenz: `F = m_Äther · a` |
| Tiere | Körpermasse × Sprungbeschleunigung, der Sprung ist ein echter Impuls `J = m · Δv` |
| Rückstoß | `J = F · Δt`, Geschwindigkeitsänderung des Ziels `Δv = J / m_Ziel` |
| Bewegung der Tiere | `F = m · (v_soll − v) / τ`, das ist F = m · a mit a = Δv / τ (Rigidbody.AddForce) |

Die Masse der Ausrüstung zählt mehrfach: Schwere Waffen treffen härter, Rüstung und Inventar wiegen,
und wer über der Traglast liegt, wird langsamer.

### Rassen und Klassen (Art. 11, 24, 48)

* Rassen: **Mensch, Zwerg, Elf**. Jede Rasse hat dieselbe Summe an Boni (Art. 24: Gleichheit).
* Klassen: **Novize, Schwertkämpfer, Kampfmagier, Bogenschütze, Assassine, Berserker**. Alle nutzen dieselbe
  Datenstruktur und dasselbe Budget (24 Startpunkte, 4 Wachstumspunkte je Stufe).
* Ab **Stufe 50** kommt eine zweite Klasse hinzu. Beide zusammen ergeben eine **Hybridklasse** mit eigener Fähigkeit [R]:

| Kombination | Hybrid | Fähigkeit |
|---|---|---|
| Schwertkämpfer + Kampfmagier | Runenritter | Runenklinge |
| Schwertkämpfer + Bogenschütze | Wächter | Schildwall |
| Schwertkämpfer + Assassine | Duellant | Konter |
| Schwertkämpfer + Berserker | Kriegsherr | Kriegsschrei |
| Kampfmagier + Bogenschütze | Arkanschütze | Ätherpfeil |
| Kampfmagier + Assassine | **Nekromant** | Totenruf (erhebt Leichen kurz als Diener) |
| Kampfmagier + Berserker | Blutmagier | Blutlanze (kostet Leben statt Äther) |
| Bogenschütze + Assassine | **Kopfgeldjäger** | Kopfgeld (die Kasse zahlt beim Tod des Ziels) |
| Bogenschütze + Berserker | Wildjäger | Zähmen (dauerhafter Tierbegleiter) |
| Assassine + Berserker | Klingentänzer | Klingentanz |
| Novize + beliebig | Pionier | Weg des Pioniers |

### Lebendige Welt

* **Tag/Nacht und Wetter**: 1 Spieltag = 24 Minuten. Das Wetter (klar, bewölkt, Regen, Gewitter, Nebel) ist eine Markow-Kette
  und wirkt auf Licht, Sichtweite, Wachstum, Bogenschüsse und Handel.
* **Ökosystem**: Grasläufer, Wildeber, Grauwölfe, Steinkriecher. **Getötete Tiere kehren nie zurück.** Bestände wachsen
  nur durch Nachwuchs der Überlebenden (logistisches Wachstum bis zur Tragfähigkeit K). Wölfe jagen Grasläufer,
  ohne Beute verhungern sie. Stirbt eine Art aus, können Tiere aus Nachbarprovinzen zuwandern (abschaltbar).
* **Ausnahme laut Geschichte**: Risswesen und der *Wächter des Risses* kehren zurück, solange das Siegel schwach ist.
* **Ressourcen und Handwerk**: Holz, Stein, Erz, Heilkraut, Ätherkristalle. Abgebaute Knoten wachsen nach (bei Regen schneller).
  Metall wird am Amboss der Schmiede verarbeitet (Zwerge erhalten einen Barren extra).
* **Wirtschaft**: Preise folgen Angebot und Nachfrage. Jäger, Holzfäller, Schmiede und Alchemisten produzieren stündlich.
  Das Fleischangebot hängt am **echten Wildbestand**: Wer überjagt, macht Fleisch teuer.
  Einheitliche Abgabe von 10 % an die **Kaiserliche Kasse** (Art. 93).

### Das Edikt als Regelwerk

* **Grad Genesis** (Kronendomäne): Thronsaal mit Kaiser **Mortem Underwood**, Kanzlei und Gericht mit
  **Richter Ataklas**, Markt, Schmiede, Mietshaus (Art. 116), Kaiserliches Lazarett (Art. 45).
* **Große Tore** (Art. 34, 54): Schnellreise für eine einheitliche Gebühr, außerdem Verkündungen.
* **Recht** (Art. 27–29, 57, 58, 62): Angriffe werden gemeldet, Wachen verfolgen den Täter, das Gericht urteilt binnen drei Tagen.
  Ein Angriff auf den Kaiser ist **Großer Verrat** und verwirkt den Weg zur Autonomie für immer.
* **Verbotene Lande** (Art. 112): nur mit Kaiserlicher Erlaubnis (ab Rang D), bewacht vom Orden der Kampfmagier.
  Dort liegt das **Siegel der Eindämmung** (Art. 16). Wer es mit Ätherkristallen verstärkt, erfüllt seine Bürgerpflicht (Art. 60).
  Fällt es unter 15 %, folgt der **Große Aufruf** (Art. 66).
* **Ränge des Aufstiegs** F bis SSS (Art. 48, 69): Prüfung in der Arena, die Ränge S und höher verleiht der Kaiser.
* **Gildenhaus der Abenteurer** (Art. 121) östlich der Stadt: Gildenmeisterin Runa vergibt jeden Morgen neue Aufträge.

### Geschichte und Aufträge (Taste L)

Die Hauptgeschichte spielt in der Welt des Edikts und startet von selbst, sobald die nötige Stufe erreicht ist.

| Kapitel | Inhalt | ab Stufe |
|---|---|---|
| I. Bürger des Reiches | Anmeldung bei Richter Ataklas (Art. 87), Prüfung in der Arena | 1 |
| II. Das müde Siegel | Sondererlaubnis, Runensplitter am Kraterrand, Siegel verstärken: jemand schwächt es mit Absicht (Verbotene Künste) | 3 |
| III. Die Verschwundenen von Eichenhain | Brandzeichen am Waldrand, Kultisten, ein Brief mit dem Wappen von Haus Vorn | 5 |
| IV. Das Zeugnis der Arbiter | Ataklas bezeugt den Brief (Art. 97), der Kaiser befiehlt, die Meuterei aufzuhalten (Art. 141, 153) | 6 |
| V. Das Lager in den Erzhöhen | Lager der Meuterer, Kultmeisterin Sela, der Ritualplan | 8 |
| VI. Die Nacht des Risses | nachts am Riss: Kasimir Vorn, Siegel wiederherstellen, Titel „Hüter von Grünmark“ | 12 |

Haus Vorn verlor seine Lande, weil es den Vasalleneid verweigerte (Übergangsbestimmungen Nr. 2). Statt den rechtmäßigen Weg
zu gehen, will es das Siegel brechen und Grünmark im Chaos an sich reißen — eine Meuterei. Am Ende zeigt der Kaiser dem
Spieler den rechtmäßigen Weg: die Drei Säulen.

* **Nebenaufträge** von Bürgern und Einwohnern (! über dem Kopf, ? wenn es weitergeht). Einige sind an die Systeme
  gekoppelt: Wölfe jagen, den Bestand der Grasläufer wieder wachsen lassen, Fleisch gegen hohe Preise liefern, Leerenbrut an der Grenze.
* **Gildenaufträge** jeden Tag: Jagd (nur auf Arten mit gesundem Bestand über 70 %), Lieferungen, Risswache.
* Aufträge erscheinen im Verfolger rechts oben und als goldene Markierung auf der Karte (M).

### Weg der Diplomatie (Abschnitt X)

Ziel ist das Dorf **Eichenhain** als eigene Autonomie. Fenster **P**:

1. **Würdig an Kraft**: Stufe ≥ Stufe des Kaisers (100).
2. **Würdig an Seele**: kein Großer Verrat, keine offene Schuld.
3. **Säule der Ressourcen**: 20 000 Taler, Vorräte (Nahrung, Holz, Eisen) und ein gesunder Wildbestand.
4. **Säule des Volkes**: **alle 12 Einwohner** respektieren dich. Respekt gibt es für Schutz vor Raubtieren und
   Leerenbrut und für erfüllte Bedürfnisse, Verbrechen kosten Respekt.
5. **Säule der Souveränität**: 4 Dorfwachen (mit Sold), ein Gesetzbuch mit mindestens 3 Gesetzen
   (der Freie Weg ist Pflicht) und ein gebautes Rathaus.

Dann folgen: Petition bei Richter Ataklas → Revision durch die Arbiter (3 Tage) → **Audienz zur festgesetzten Stunde**
(wer sie verpasst, verliert die Petition) → **Prüfung des Blicks** → Vertrag mit Name und Wappenfarbe.
Fällt danach eine Säule, schickt der Thron eine Vorwarnung mit Frist und löst den Vertrag sonst auf (Art. 140).

---

## Projektstruktur

```
Assets/OmniGenesis/
  Scenes/        Hauptmenue.unity, Welt.unity (werden per Menü erzeugt)
  Scripts/
    Core/        Regeln (GameConstants), Eingabe, Spielstand, Physik F = m·a, Lebenspunkte
    Data/        Rassen, Klassen und Hybride, Gegenstände und Rezepte, Tierarten, Geschichte und Aufträge
    Player/      Charakter, Bewegung, Kamera (1./3. Person), Kampf, Interaktion, Figur
    Combat/      Geschosse, Effekte
    World/       Gelände, Weltbau, Uhr, Wetter, Ressourcen, Rangprüfung
    Ecosystem/   Lebewesen (Rigidbody-KI), Populationen, Riss und Siegel
    Society/     NPCs, Markt und Kasse, Recht, Einwohner, Autonomie, Aufträge, Edikt-Texte
    UI/          HUD, Systemfenster, Dialoge, Hauptmenü, Ladebildschirm „Lade Welt… %“
    Bootstrap/   WorldBootstrap: baut die Welt Schritt für Schritt
    Editor/      Menü „Omni Genesis → Projekt einrichten“
  Prefabs/  Art/  Audio/  UI_Assets/   für spätere Modelle, Klänge und Grafiken
```

Alle Zahlen des Regelwerks (Kaiserstufe, Respektschwelle, Abgaben, Gebühren, Physikfaktoren) stehen gesammelt in
`Scripts/Core/GameConstants.cs`.

## Stand

* Alle Skripte kompilieren fehlerfrei gegen die Unity-Referenzassemblies, mit altem Input Manager und mit Input System.
* **Im Unity-Editor wurde das Spiel noch nicht gestartet.** Falls Fehler in der Konsole erscheinen, bitte den Text schicken.
* Grafik und Figuren sind Platzhalter aus Grundkörpern. Eigene Modelle und Klänge kommen nach `Art/` und `Audio/`.
* Nächster Schritt: **Weg des Krieges** (Abschnitt XI: Manifest, Vorbereitungszeit, Feld der Prüfung, Duell des Throns).
* Andere Reisende, PvP und Geheimnisse gibt es bisher nur in der 2D-Version (`web2d/`).

---

### Кратко по-русски

1. Unity Hub → New project → **Universal 3D (URP)**, Unity 6.
2. Скопируйте `Assets/OmniGenesis` в папку `Assets` проекта.
3. В Unity выберите меню **Omni Genesis → Projekt einrichten**. Создадутся сцены и Build Settings.
4. Нажмите **Play**. Камера переключается клавишей **V** (от первого или третьего лица), задания и сюжет открываются на **L**,
   справочник по Эдикту на **J**, Три Столпа на **P**. Для показа на защите при создании персонажа включите **Demo-Modus**: тогда работают клавиши F1–F6.

**2D-версия (`web2d/`):** сюжет необязательный (главы предлагают NPC со знаком `!`), по миру ходят 10 других путников со своими уровнями
и рейтингом, есть изгои с наградой за голову, нападение на мирных путников (это преступление — «кровавый долг»), Поединок Чести
и 8 пасхалок (подсказки: J → Geheimnisse).
