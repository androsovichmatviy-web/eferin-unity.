using System.Collections.Generic;

namespace OmniGenesis
{
    public enum StepType { Talk, Kill, Deliver, Reach, Investigate, Seal, Rank, Population, Level }

    public class StepDef
    {
        public StepType type;
        public string text;         // Anzeige im Aufgabenverfolger
        public string target;       // NPC-Name, Tierart, Gegenstand, Spur, Ort
        public int count = 1;
        public string npc;          // Empfänger bei Lieferungen
        public string place;        // Ortsschlüssel für Karte und "Erreiche"
        public float radius = 14f;
        public bool night;          // nur nachts erfüllbar
        public string line;         // was der NPC beim Abschluss des Schritts sagt
        public string spawn;        // Ereignis der Geschichte beim Beginn des Schritts
        public ItemStack[] give;    // Gegenstände beim Abschluss des Schritts
    }

    public class QuestDef
    {
        public string id;
        public string title;
        public string giver;
        public string summary;
        public bool main;
        public bool contract;
        public string requires;
        public int minLevel = 1;
        public bool grantPermit;
        public StepDef[] steps;

        // Belohnung
        public float expLevels;     // Anteil einer Stufe (1 = eine volle Stufe)
        public int taler;
        public ItemStack[] items;
        public float respect;       // Respekt aller Einwohner von Eichenhain
        public string titleReward;
        public string hook;         // besondere Wirkung beim Abschluss
        public string completeText;
    }

    // Die Geschichte von Omni Genesis: eine Meuterei gegen das Edikt.
    // Haus Vorn verlor seine Lande, weil es den Vasalleneid verweigerte (Übergangsbestimmungen Nr. 2),
    // und will nun das Siegel des Risses brechen, um Grünmark im Chaos an sich zu reißen — ohne Manifest,
    // also als Meuterei (Art. 141). Der Spieler zeigt, dass es einen rechtmäßigen Weg gibt.
    public static class QuestData
    {
        static StepDef Talk(string npc, string place, string text, string line, ItemStack[] give = null)
        {
            return new StepDef { type = StepType.Talk, target = npc, place = place, text = text, line = line, give = give };
        }

        static StepDef Kill(string species, int count, string place, string text, string spawn = null)
        {
            return new StepDef { type = StepType.Kill, target = species, count = count, place = place, text = text, spawn = spawn };
        }

        static StepDef Clue(string clue, int count, string place, string text, string spawn)
        {
            return new StepDef { type = StepType.Investigate, target = clue, count = count, place = place, text = text, spawn = spawn };
        }

        static StepDef Deliver(string item, int count, string npc, string place, string text)
        {
            return new StepDef { type = StepType.Deliver, target = item, count = count, npc = npc, place = place, text = text };
        }

        static ItemStack[] I(string id, int n) { return new[] { new ItemStack(id, n) }; }

        public static readonly List<QuestDef> All = new List<QuestDef>
        {
            // ---------------- Hauptgeschichte ----------------
            new QuestDef
            {
                id = "haupt1", main = true, title = "Kapitel I: Bürger des Reiches", giver = "Ataklas",
                summary = "Jeder Reisende braucht das Siegel des Bürgers (Art. 87). Melde dich in der Kanzlei von Grad Genesis.",
                steps = new[]
                {
                    Talk("Ataklas", "kanzlei", "Melde dich bei Richter Ataklas in der Kanzlei",
                        "„Ein neuer Name für das Register der Bürgerschaft. Das Siegel des Bürgers gibt dir Rechte — und Pflichten (Art. 60). Zeig zuerst, was du kannst: Die Prüfung der Meisterschaft wartet in der Arena westlich der Stadt.“"),
                    new StepDef { type = StepType.Rank, count = 0, place = "arena", text = "Lege die Prüfung der Meisterschaft ab (mindestens Rang F)" },
                    Talk("Ataklas", "kanzlei", "Berichte Richter Ataklas",
                        "„Die Arbiter haben deine Prüfung bezeugt. Willkommen in der Ordnung des Throns, Bürger. Werde stärker — der Orden der Kampfmagier wird bald Hilfe brauchen.“"),
                },
                expLevels = 1f, taler = 150, items = I("heiltrank", 2),
                completeText = "Du trägst nun das Siegel des Bürgers.",
            },
            new QuestDef
            {
                id = "haupt2", main = true, requires = "haupt1", minLevel = 3, grantPermit = true,
                title = "Kapitel II: Das müde Siegel", giver = "Ataklas",
                summary = "Der Orden meldet: Das Siegel der Eindämmung schwindet schneller, als der Riss es erklären kann. Richter Ataklas stellt dir eine Sondererlaubnis für die Verbotenen Lande aus (Art. 112).",
                steps = new[]
                {
                    Talk("Magistra Velde", "grenzposten", "Sprich mit Magistra Velde am Grenzposten",
                        "„Das Siegel verliert jede Nacht mehr Kraft, als der Riss allein nehmen kann. Am Kraterrand liegen Splitter, die nicht dorthin gehören. Bring sie mir. Und nimm diese Kristalle für das Siegel.“",
                        I("aetherkristall", 2)),
                    Clue("runensplitter", 3, "kraterrand", "Untersuche die Runensplitter am Kraterrand", "splitter"),
                    new StepDef { type = StepType.Seal, count = 60, place = "siegel", text = "Verstärke das Siegel auf mindestens 60 %" },
                    Talk("Magistra Velde", "grenzposten", "Bring die Splitter zu Magistra Velde",
                        "„Diese Runen sind Verbotene Kunst (Art. 60 Nr. 5). Jemand schwächt das Siegel mit Absicht. Und in Eichenhain verschwinden seit Wochen Menschen in der Nacht. Hedda, die Söldnerführerin dort, weiß mehr.“"),
                },
                expLevels = 1.2f, taler = 250, items = I("aetherelixier", 2),
                completeText = "Das Siegel wird mit Absicht geschwächt. Die Spur führt nach Eichenhain.",
            },
            new QuestDef
            {
                id = "haupt3", main = true, requires = "haupt2", minLevel = 5,
                title = "Kapitel III: Die Verschwundenen von Eichenhain", giver = "Hedda",
                summary = "In Eichenhain verschwinden nachts Menschen. Am Waldrand im Nordwesten des Dorfes gibt es verbrannte Zeichen.",
                steps = new[]
                {
                    Talk("Hedda", "eichenhain", "Sprich mit Hedda in Eichenhain",
                        "„Drei Leute fort, keine Leiche, keine Spur — nur verbrannte Zeichen am Waldrand im Nordwesten. Meine Leute trauen sich nicht hin. Du schon?“"),
                    Clue("brandzeichen", 3, "waldrand", "Untersuche die Brandzeichen am Waldrand", "brandzeichen"),
                    Kill("kultist", 3, "waldrand", "Stelle die Vermummten am Waldrand", "kult_wald"),
                    Clue("brief", 1, "waldrand", "Durchsuche das Feuer der Vermummten", "brief"),
                    Talk("Hedda", "eichenhain", "Zeige Hedda den Brief",
                        "„Das Wappen von Haus Vorn … Die Vorns hielten diese Lande vor dem Edikt. Sie verweigerten den Vasalleneid und verloren alles an die Kronendomäne. Bring den Brief zu Richter Ataklas. Und danke — Eichenhain vergisst das nicht.“"),
                },
                expLevels = 1.2f, taler = 300, respect = 10f,
                completeText = "Die Vermissten waren Opfer eines Rituals. Dahinter steht Haus Vorn.",
            },
            new QuestDef
            {
                id = "haupt4", main = true, requires = "haupt3", minLevel = 6,
                title = "Kapitel IV: Das Zeugnis der Arbiter", giver = "Ataklas",
                summary = "Ein Brief mit dem Wappen von Haus Vorn. Nur ein Arbiter kann bezeugen, ob er echt ist (Art. 97).",
                steps = new[]
                {
                    Talk("Ataklas", "kanzlei", "Bring den Brief zu Richter Ataklas",
                        "„Mein Siegel der Unparteilichkeit lügt nicht (Art. 97): Der Brief ist echt. Haus Vorn plant weder Vertrag noch offenen Vorstoß — es plant eine Meuterei. Eine Meuterei ist ein Verbrechen, kein Weg (Art. 141). Der Kaiser muss es erfahren.“"),
                    Talk(GameConstants.EmperorName, "thronsaal", "Berichte Kaiser Mortem Underwood im Thronsaal",
                        "„Wer das Siegel bricht, um Land zu gewinnen, gewinnt nichts als Schande. Eine Meuterei wird mit aller Macht niedergeschlagen — ohne Frist und ohne Feld (Art. 153). Finde ihr Lager, Bürger. Der Thron sieht dich.“"),
                },
                expLevels = 0.8f, taler = 400,
                completeText = "Der Kaiser hat dich beauftragt, die Meuterei aufzuhalten.",
            },
            new QuestDef
            {
                id = "haupt5", main = true, requires = "haupt4", minLevel = 8,
                title = "Kapitel V: Das Lager in den Erzhöhen", giver = GameConstants.EmperorName,
                summary = "Die Meuterer von Haus Vorn verstecken sich in den Erzhöhen östlich der Provinz.",
                steps = new[]
                {
                    new StepDef { type = StepType.Reach, target = "lager", place = "lager", radius = 22f, text = "Finde das Lager von Haus Vorn in den Erzhöhen" },
                    Kill("kultist", 5, "lager", "Besiege die Kultisten im Lager", "kult_lager"),
                    Kill("kultmeisterin", 1, "lager", "Besiege Kultmeisterin Sela", "sela"),
                    Clue("ritualplan", 1, "lager", "Durchsuche Selas Zelt", "ritualplan"),
                    Talk("Magistra Velde", "grenzposten", "Bring den Ritualplan zu Magistra Velde",
                        "„Eine Nacht am Riss … Sie wollen das Siegel auf einen Schlag brechen und die Leerenbrut über Grünmark lassen. Im Chaos will Kasimir Vorn die Lande an sich reißen. Wir müssen dort sein — in der Nacht.“"),
                },
                expLevels = 1.5f, taler = 500, items = I("aetherkristall", 2),
                completeText = "Das Ritual soll in der Nacht am Riss stattfinden.",
            },
            new QuestDef
            {
                id = "haupt6", main = true, requires = "haupt5", minLevel = 12,
                title = "Kapitel VI: Die Nacht des Risses", giver = "Magistra Velde",
                summary = "Kasimir Vorn will das Siegel in einer Nacht brechen. Halte ihn auf.",
                steps = new[]
                {
                    new StepDef { type = StepType.Reach, target = "riss", place = "riss", radius = 30f, night = true, text = "Geh in der Nacht (21–6 Uhr) zum Riss" },
                    Kill("kasimir", 1, "riss", "Besiege Kasimir Vorn", "ritual"),
                    new StepDef { type = StepType.Seal, count = 80, place = "siegel", text = "Stelle das Siegel wieder her (mindestens 80 %)" },
                    Talk(GameConstants.EmperorName, "thronsaal", "Erstatte dem Kaiser Bericht",
                        "„Kasimir Vorn wählte die Meuterei — und fiel. Du hast das Siegel gehalten, wie es jeder Bürger soll (Art. 60). Das Reich erkennt dich als Hüter von Grünmark an. Und höre: Es gibt einen rechtmäßigen Weg zu eigener Macht. Werde so stark wie ich, gewinne dein Volk — und reiche deine Petition ein.“"),
                },
                expLevels = 2.5f, taler = 2000, respect = 20f, titleReward = "Hüter von Grünmark", hook = "finale",
                completeText = "Die Meuterei ist gescheitert. Der Weg der Diplomatie steht dir offen.",
            },

            // ---------------- Nebenaufträge ----------------
            new QuestDef
            {
                id = "neben_woelfe", title = "Wölfe vor Eichenhain", giver = "Jonas Falk", minLevel = 2,
                summary = "Die Grauwölfe reißen nachts die Grasläufer am Dorfrand. Erlege drei — aber bedenke: Getötete Tiere kehren nicht zurück.",
                steps = new[] { Kill("grauwolf", 3, "eichenhain", "Erlege drei Grauwölfe") },
                expLevels = 0.5f, taler = 150, respect = 4f,
            },
            new QuestDef
            {
                id = "neben_bestand", title = "Der Bestand erholt sich", giver = "Greta Moor", minLevel = 2,
                summary = "Ohne Grasläufer kein Fleisch und keine Wölfe, die satt sind. Lass den Bestand auf 18 Tiere wachsen — jage keine und halte die Wölfe fern.",
                steps = new[] { new StepDef { type = StepType.Population, target = "graslaeufer", count = 18, text = "Bestand der Grasläufer auf 18 bringen" } },
                expLevels = 0.5f, taler = 120, respect = 6f,
            },
            new QuestDef
            {
                id = "neben_fleisch", title = "Der Preis des Fleisches", giver = "Mirela",
                summary = "Die Fleischpreise steigen. Bring Mirela zehn Stück Fleisch für den Markt.",
                steps = new[] { Deliver("fleisch", 10, "Mirela", "marktplatz", "Bring Mirela 10 Fleisch") },
                expLevels = 0.4f, taler = 220, hook = "markt_fleisch",
            },
            new QuestDef
            {
                id = "neben_erz", title = "Eisen für die Legion", giver = "Gundram", minLevel = 3,
                summary = "Die Legion braucht Klingen. Bring dem Schmied fünf Eisenbarren — er bezahlt mit einem Langschwert.",
                steps = new[] { Deliver("eisenbarren", 5, "Gundram", "schmiede", "Bring Gundram 5 Eisenbarren") },
                expLevels = 0.5f, taler = 100, items = I("langschwert", 1),
            },
            new QuestDef
            {
                id = "neben_kraeuter", title = "Kräuter für das Lazarett", giver = "Schwester Alwine",
                summary = "Das Lazarett heilt jeden (Art. 45) — dafür braucht es Kräuter. Bring zehn Heilkräuter.",
                steps = new[] { Deliver("heilkraut", 10, "Schwester Alwine", "lazarett", "Bring Schwester Alwine 10 Heilkräuter") },
                expLevels = 0.3f, taler = 60, items = I("heiltrank", 4),
            },
            new QuestDef
            {
                id = "neben_kriecher", title = "Steine im Weg", giver = "Oskar", minLevel = 4,
                summary = "Steinkriecher blockieren die Erzgruben in den Erzhöhen. Vertreibe zwei — dauerhaft.",
                steps = new[] { Kill("steinkriecher", 2, "erzhoehen", "Besiege zwei Steinkriecher") },
                expLevels = 0.6f, taler = 180, items = I("eisenerz", 4),
            },
            new QuestDef
            {
                id = "neben_grenze", title = "Grenzpatrouille", giver = "Magister Aurel", minLevel = 6, requires = "haupt2",
                summary = "Leerenbrut streift an der Grenze. Der Orden zahlt für jede, die nicht mehr streift.",
                steps = new[] { Kill("leerenbrut", 4, "riss", "Besiege vier Leerenbrut") },
                expLevels = 0.8f, taler = 250, items = I("aetherkristall", 2),
            },
            new QuestDef
            {
                id = "neben_bote", title = "Ein Bericht für die Kanzlei", giver = "Hedda",
                summary = "Hedda schickt einen Bericht über die Dorfwache an die Kanzlei. Die Großen Tore bringen dich schnell hin.",
                steps = new[]
                {
                    Talk("Ataklas", "kanzlei", "Übergib Heddas Bericht an Richter Ataklas",
                        "„Ein Bericht über die Wache von Eichenhain. Ordentlich geführt. Die Kanzlei dankt.“"),
                },
                expLevels = 0.2f, taler = 80,
            },
        };

        public static QuestDef Get(string id)
        {
            foreach (var q in All) if (q.id == id) return q;
            return null;
        }
    }
}
