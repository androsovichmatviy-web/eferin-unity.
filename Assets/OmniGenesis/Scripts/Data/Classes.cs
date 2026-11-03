using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum AbilityKind
    {
        PowerStrike, Heal, Spin, Charge, Bolt, Nova, Volley, Pierce, Blink, PoisonBuff, Slam, Rage,
        RuneBuff, ShieldWall, Counter, WarCry, HomingArrow, RaiseDead, BloodLance, Bounty, Tame, BladeDance, Pioneer,
    }

    public class AbilityDef
    {
        public string id;
        public string name;
        public string description;
        public AbilityKind kind;
        public float cooldown;
        public float cost;      // Äther
        public float power;     // Multiplikator
        public float radius;
        public float duration;
    }

    // Alle Klassen haben dieselbe Datenstruktur und dasselbe Budget:
    // 24 Startpunkte und 4 Wachstumspunkte pro Stufe. Keine Klasse ist bevorzugt.
    public class ClassDef
    {
        public string id;
        public string name;
        public string description;
        public int str, agi, vit, intl;
        public float gStr, gAgi, gVit, gInt;
        public string startWeapon;
        public string startArmor;
        public AbilityDef skillQ;
        public AbilityDef skillE;
        public Color color;
    }

    public class HybridDef
    {
        public string id;
        public string name;
        public string description;
        public string a, b;
        public AbilityDef skillR;
    }

    public static class Classes
    {
        static AbilityDef A(string id, string name, AbilityKind kind, float cd, float cost, float power, float radius, float duration, string desc)
        {
            return new AbilityDef { id = id, name = name, kind = kind, cooldown = cd, cost = cost, power = power, radius = radius, duration = duration, description = desc };
        }

        public static readonly List<ClassDef> All = new List<ClassDef>
        {
            new ClassDef
            {
                id = "novize", name = "Novize",
                description = "Ein Anfänger ohne festen Weg. Wächst in allen Werten gleichmäßig.",
                str = 6, agi = 6, vit = 6, intl = 6, gStr = 1, gAgi = 1, gVit = 1, gInt = 1,
                startWeapon = "holzknueppel", startArmor = "stoffkleidung",
                skillQ = A("kraftschlag", "Kraftschlag", AbilityKind.PowerStrike, 5f, 8f, 2.2f, 0f, 0f, "Ein wuchtiger Schlag mit doppelter Kraft."),
                skillE = A("erste_hilfe", "Erste Hilfe", AbilityKind.Heal, 20f, 15f, 0.3f, 0f, 5f, "Heilt 30 % der Lebenspunkte über 5 Sekunden."),
                color = new Color(0.62f, 0.6f, 0.55f),
            },
            new ClassDef
            {
                id = "schwertkaempfer", name = "Schwertkämpfer",
                description = "Meister der Klinge. Stark und ausdauernd.",
                str = 8, agi = 6, vit = 7, intl = 3, gStr = 2, gAgi = 1, gVit = 1, gInt = 0,
                startWeapon = "kurzschwert", startArmor = "lederruestung",
                skillQ = A("wirbelschlag", "Wirbelschlag", AbilityKind.Spin, 6f, 12f, 1.4f, 3f, 0f, "Ein Rundumschlag um 360°."),
                skillE = A("ansturm", "Ansturm", AbilityKind.Charge, 8f, 15f, 1f, 7f, 0.35f, "Sturmangriff über 7 m — der Anlauf erhöht die Beschleunigung und damit F."),
                color = new Color(0.75f, 0.2f, 0.18f),
            },
            new ClassDef
            {
                id = "kampfmagier", name = "Kampfmagier",
                description = "Beherrscht den Äther. Zerbrechlich, aber mächtig.",
                str = 3, agi = 5, vit = 5, intl = 11, gStr = 0, gAgi = 1, gVit = 0, gInt = 3,
                startWeapon = "lehrlingsstab", startArmor = "stoffkleidung",
                skillQ = A("aetherbolzen", "Ätherbolzen", AbilityKind.Bolt, 4f, 18f, 1.6f, 3f, 0f, "Schwere Äthermasse, explodiert beim Aufprall."),
                skillE = A("frostring", "Frostring", AbilityKind.Nova, 9f, 25f, 1.2f, 6f, 4f, "Eisring um dich, verlangsamt Gegner um 50 %."),
                color = new Color(0.3f, 0.35f, 0.85f),
            },
            new ClassDef
            {
                id = "bogenschuetze", name = "Bogenschütze",
                description = "Präzise auf Distanz. Gewandt und schnell.",
                str = 5, agi = 10, vit = 5, intl = 4, gStr = 1, gAgi = 2, gVit = 1, gInt = 0,
                startWeapon = "jagdbogen", startArmor = "lederruestung",
                skillQ = A("pfeilsalve", "Pfeilsalve", AbilityKind.Volley, 6f, 15f, 0.8f, 0f, 0f, "Fünf Pfeile im Fächer."),
                skillE = A("durchschuss", "Durchschuss", AbilityKind.Pierce, 7f, 15f, 2f, 0f, 0f, "Doppelte Zugkraft, der Pfeil durchschlägt bis zu 5 Ziele."),
                color = new Color(0.25f, 0.55f, 0.25f),
            },
            new ClassDef
            {
                id = "assassine", name = "Assassine",
                description = "Schnell, lautlos, tödlich aus dem Schatten.",
                str = 5, agi = 11, vit = 4, intl = 4, gStr = 1, gAgi = 2, gVit = 0, gInt = 1,
                startWeapon = "dolche", startArmor = "lederruestung",
                skillQ = A("schattenschritt", "Schattenschritt", AbilityKind.Blink, 7f, 15f, 1f, 8f, 3f, "Sprung 8 m nach vorn, der nächste Treffer ist kritisch."),
                skillE = A("giftklinge", "Giftklinge", AbilityKind.PoisonBuff, 14f, 20f, 1f, 0f, 8f, "8 s lang vergiften deine Treffer das Ziel."),
                color = new Color(0.25f, 0.22f, 0.3f),
            },
            new ClassDef
            {
                id = "berserker", name = "Berserker",
                description = "Rohe Kraft. Schwere Waffen, maximale Masse — maximales F.",
                str = 11, agi = 5, vit = 7, intl = 1, gStr = 2, gAgi = 0, gVit = 2, gInt = 0,
                startWeapon = "kriegsaxt", startArmor = "lederruestung",
                skillQ = A("erdstoss", "Erdstoß", AbilityKind.Slam, 7f, 15f, 1.5f, 4.5f, 0f, "Körpermasse × Fallbeschleunigung trifft den Boden: Flächenschaden."),
                skillE = A("blutrausch", "Blutrausch", AbilityKind.Rage, 18f, 20f, 1f, 0f, 10f, "10 s: +35 % Schlagtempo, 15 % Lebensraub, +20 % erlittener Schaden."),
                color = new Color(0.55f, 0.3f, 0.12f),
            },
        };

        // Ab Stufe 50: zwei Klassen ergeben eine Hybridklasse.
        public static readonly List<HybridDef> Hybrids = new List<HybridDef>
        {
            H("runenritter", "Runenritter", "schwertkaempfer", "kampfmagier", "Runen verstärken die Klinge.",
                A("runenklinge", "Runenklinge", AbilityKind.RuneBuff, 25f, 35f, 0.8f, 2.5f, 12f, "12 s: jeder Nahkampftreffer löst eine Ätherexplosion aus.")),
            H("waechter", "Wächter", "schwertkaempfer", "bogenschuetze", "Schild und Armbrust.",
                A("schildwall", "Schildwall", AbilityKind.ShieldWall, 22f, 30f, 0.4f, 0f, 8f, "8 s: −60 % Schaden, kein Rückstoß.")),
            H("duellant", "Duellant", "schwertkaempfer", "assassine", "Parade und Konter.",
                A("konter", "Konter", AbilityKind.Counter, 8f, 20f, 3f, 2.5f, 1f, "1 s Parade: der nächste Treffer wird abgewehrt und dreifach erwidert.")),
            H("kriegsherr", "Kriegsherr", "schwertkaempfer", "berserker", "Schwere Waffen, größte Wucht.",
                A("kriegsschrei", "Kriegsschrei", AbilityKind.WarCry, 25f, 35f, 1.3f, 8f, 12f, "Stoßwelle mit 1200 N, danach 12 s +30 % Schaden.")),
            H("arkanschuetze", "Arkanschütze", "kampfmagier", "bogenschuetze", "Magische Pfeile.",
                A("aetherpfeil", "Ätherpfeil", AbilityKind.HomingArrow, 8f, 30f, 1.2f, 0f, 0f, "Drei zielsuchende Ätherpfeile.")),
            H("nekromant", "Nekromant", "kampfmagier", "assassine", "Ruft die Gefallenen als Diener.",
                A("totenruf", "Totenruf", AbilityKind.RaiseDead, 30f, 40f, 1f, 15f, 60f, "Erhebt bis zu 2 Leichen im Umkreis von 15 m für 60 s. Tote bleiben tot — es ist nur ein Echo.")),
            H("blutmagier", "Blutmagier", "kampfmagier", "berserker", "Zauber kosten Leben statt Äther.",
                A("blutlanze", "Blutlanze", AbilityKind.BloodLance, 6f, 0f, 3f, 0f, 0f, "Kostet 15 % LP: durchschlagende Lanze mit dreifacher Kraft.")),
            H("kopfgeldjaeger", "Kopfgeldjäger", "bogenschuetze", "assassine", "Markiert Ziele und kassiert Kopfgeld.",
                A("kopfgeld", "Kopfgeld", AbilityKind.Bounty, 12f, 20f, 1.4f, 30f, 20f, "Markiert das anvisierte Ziel: +40 % Schaden, beim Tod zahlt die Kasse ein Kopfgeld.")),
            H("wildjaeger", "Wildjäger", "bogenschuetze", "berserker", "Zähmt Tiere des Ökosystems.",
                A("zaehmen", "Zähmen", AbilityKind.Tame, 20f, 30f, 1f, 6f, 0f, "Zähmt ein geschwächtes Wildtier (< 40 % LP) als dauerhaften Begleiter.")),
            H("klingentaenzer", "Klingentänzer", "assassine", "berserker", "Schnelle Klingenserien.",
                A("klingentanz", "Klingentanz", AbilityKind.BladeDance, 10f, 30f, 0.7f, 3.5f, 1.2f, "Sechs Schläge in 1,2 s rund um dich.")),
        };

        static HybridDef H(string id, string name, string a, string b, string desc, AbilityDef r)
        {
            return new HybridDef { id = id, name = name, a = a, b = b, description = desc, skillR = r };
        }

        // Novize + beliebige Klasse = Pionier.
        public static readonly AbilityDef PioneerSkill = A("weg_des_pioniers", "Weg des Pioniers", AbilityKind.Pioneer, 30f, 30f, 0.25f, 0f, 15f, "15 s: alle Werte +25 %, doppelte Sammelausbeute.");

        public static ClassDef Get(string id)
        {
            foreach (var c in All) if (c.id == id) return c;
            return All[0];
        }

        public static HybridDef Hybrid(string first, string second)
        {
            if (string.IsNullOrEmpty(second) || first == second) return null;
            if (first == "novize" || second == "novize")
            {
                string other = first == "novize" ? second : first;
                return new HybridDef
                {
                    id = "pionier", name = "Pionier", a = "novize", b = other,
                    description = "Universalist: behält die Ausgewogenheit des Novizen und lernt den Weg des " + Get(other).name + "s.",
                    skillR = PioneerSkill,
                };
            }
            foreach (var h in Hybrids)
                if ((h.a == first && h.b == second) || (h.a == second && h.b == first)) return h;
            return null;
        }
    }
}
