using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public abstract class DialogueBase : IDialogue
    {
        public bool Closed { get; protected set; }
        public abstract string Title { get; }
        public abstract void Draw(Rect area);
        protected Vector2 scroll;
    }

    // ---------- Gespräche mit NPCs ----------
    public class NpcDialogue : DialogueBase
    {
        readonly Npc npc;
        readonly string greeting;
        string result = "";
        bool treatyForm;
        string treatyName = "";
        int treatyColor;

        public NpcDialogue(Npc npc)
        {
            this.npc = npc;
            greeting = Greeting();
            treatyName = "Freie Mark Eichenhain";
        }

        public override string Title
        {
            get
            {
                if (npc.role == NpcRole.Kaiser) return GameConstants.EmperorName + " — " + GameConstants.EmperorTitle + " · Stufe " + GameConstants.EmperorLevel;
                return npc.npcName + (string.IsNullOrEmpty(npc.title) ? "" : " — " + npc.title);
            }
        }

        string Greeting()
        {
            var pc = World.Player;
            switch (npc.role)
            {
                case NpcRole.Kaiser:
                    return "„Da steht der Thron, solange die Sterne brennen. Was suchst du vor dem Thron der Genesis, " + pc.charName + "?“";
                case NpcRole.Richter:
                    return "„Ich bewahre alle hundertsechsundsechzig Artikel des Edikts im Gedächtnis. Keines meiner Urteile weicht vom Buchstaben des Gesetzes ab.“";
                case NpcRole.Haendler:
                case NpcRole.Kraemer:
                    return "„Ehrlicher Handel steht unter dem Schutz des Throns (Art. 42). Die Preise richten sich nach Angebot und Nachfrage.“";
                case NpcRole.Schmied:
                    return "„Masse mal Beschleunigung, Reisender. Eine schwere Klinge schlägt härter — wenn du stark genug bist, sie schnell zu führen.“";
                case NpcRole.Vermieter:
                    return "„Ein Zimmer im Haus des Throns. Die Miete fließt in die Kasse (Art. 116–117).“";
                case NpcRole.Heiler:
                    return "„Jeder hat das Recht auf Heilung in den kaiserlichen Lazaretten (Art. 45). Komm, ich sehe nach dir.“";
                case NpcRole.Pruefungsmeister:
                    return "„Die Kaiserliche Prüfung der Meisterschaft (Art. 48). Drei Wellen Golems. Ich messe Zeit und Treffer.“";
                case NpcRole.Turmhueter:
                    return "„Fünfzig Etagen. Die Namen der Bezwinger stehen in den Chroniken der Genesis (Art. 114).“";
                case NpcRole.Wache:
                    return World.Law.Wanted > 0 ? "„Im Namen des Throns: Du bist festgenommen!“" : "„Die Legionen schützen Trakte und Siedlungen vor den Geschöpfen der Risse (Art. 46).“";
                case NpcRole.Ordensmagier:
                    return pc.permit ? "„Deine Erlaubnis ist in Ordnung. Hüte dich vor der Leerenbrut.“" : "„Ohne Kaiserliche Erlaubnis kein Schritt weiter (Art. 112). Die Kanzlei in Grad Genesis stellt sie aus.“";
                case NpcRole.Soeldnerfuehrer:
                    return "„Eine Autonomie braucht eigene Wachen (Art. 133). Meine Leute kämpfen für jeden, der den Sold zahlt.“";
                case NpcRole.Dorfwache:
                    return "„Eichenhain ist sicher, solange ich atme.“";
                case NpcRole.Einwohner:
                    return ResidentLine();
                default:
                    return Rumor();
            }
        }

        string ResidentLine()
        {
            var r = npc.resident;
            if (r == null) return "„Guten Tag.“";
            if (r.respect >= 80f) return "„Du bist ein Segen für Eichenhain. Ich würde dir überallhin folgen.“";
            if (r.Respects) return "„Ich respektiere dich. Du hast für uns getan, was kein Fremder getan hätte.“";
            if (r.respect < -20f) return "„Geh mir aus den Augen.“";
            return "„Fremde kommen und gehen. Zeig uns erst, wer du bist.“";
        }

        string Rumor()
        {
            var m = World.Market;
            float meat = m.Trend("fleisch");
            if (World.Rift.Summons) return "„Der Große Aufruf! Alle Wehrfähigen sollen zum Riss. Die Heiltränke werden knapp.“";
            if (World.Rift.Strength < 35f) return "„Man sagt, das Siegel im Norden wird schwach. Nachts hört man die Leerenbrut.“";
            if (World.Eco.StockFraction("graslaeufer") < 0.35f) return "„Die Jäger finden kaum noch Grasläufer. Kein Wunder, dass das Fleisch so teuer ist.“";
            if (meat > 1.1f) return "„Fleisch wird teurer. Irgendwer jagt zu viel oder die Karawanen stocken.“";
            if (World.Weather.IsStorm) return "„Bei diesem Gewitter kommt keine Karawane durch.“";
            if (World.Autonomy.IsAutonomous) return "„Hast du gehört? " + World.Autonomy.autonomyName + " ist jetzt eine anerkannte Autonomie!“";
            return "„Ein Kontinent, ein Thron, ein Gesetz. So ist es seit der Ära der Einheit.“";
        }

        public override void Draw(Rect area)
        {
            var pc = World.Player;
            var col = new Col(area);
            col.Text(greeting, 18, Ui.TextColor);
            col.Space(8f);
            var rest = new Rect(area.x, col.y, area.width, area.yMax - col.y);

            switch (npc.role)
            {
                case NpcRole.Kaiser: DrawEmperor(col); break;
                case NpcRole.Richter: DrawJudge(col); break;
                case NpcRole.Haendler:
                case NpcRole.Kraemer: MarketView.Draw(rest, ref scroll); break;
                case NpcRole.Schmied: CraftingDialogue.DrawRecipes(rest, true, ref scroll); break;
                case NpcRole.Vermieter: DrawLandlord(col); break;
                case NpcRole.Heiler:
                    if (Ui.Button(col.Next(48f), "Heilen lassen (kostenlos)", pc.health.hp < pc.health.maxHp || pc.aether < pc.MaxAether, 18))
                    {
                        pc.health.hp = pc.health.maxHp;
                        pc.aether = pc.MaxAether;
                        result = "Du bist vollständig geheilt.";
                    }
                    break;
                case NpcRole.Pruefungsmeister: DrawTrial(col); break;
                case NpcRole.Turmhueter: DrawTower(rest); break;
                case NpcRole.Wache:
                case NpcRole.Dorfwache:
                    if (World.Law.Wanted > 0 && npc.role == NpcRole.Wache && Ui.Button(col.Next(48f), "Mich ergeben (Gericht binnen drei Tagen, Art. 29)", true, 17))
                    {
                        World.Law.Arrest();
                        Closed = true;
                    }
                    break;
                case NpcRole.Ordensmagier:
                    col.Text("Siegel der Eindämmung: " + Mathf.RoundToInt(World.Rift.Strength) + " %. Verstärke es mit Ätherkristallen — das ist Pflicht jedes Bürgers (Art. 60 Nr. 3).", 16, Ui.Dim);
                    break;
                case NpcRole.Einwohner: DrawResident(col); break;
                case NpcRole.Soeldnerfuehrer: DrawMercenary(col); break;
            }
            if (!string.IsNullOrEmpty(result)) Ui.Text(new Rect(area.x, area.yMax - 30f, area.width - 220f, 30f), result, 16, Ui.Gold);
        }

        void DrawEmperor(Col col)
        {
            var au = World.Autonomy;
            var pc = World.Player;
            if (au.AudienceOpen)
            {
                if (!treatyForm)
                {
                    col.Text("Die Stunde der Audienz ist gekommen. Der Kaiser erhebt sich und blickt dir in die Seele — die Prüfung des Blicks (Art. 135).", 17, Ui.Gold);
                    if (Ui.Button(col.Next(52f), "Dem Blick des Kaisers standhalten", true, 19))
                    {
                        string fail = au.GazeTrial();
                        if (fail != null)
                        {
                            au.RejectByEmperor(fail);
                            result = "Der Kaiser verweigert den Vertrag. Begründung: " + fail;
                        }
                        else treatyForm = true;
                    }
                    return;
                }
                col.Text("„Du bist würdig an Seele und an Kraft. Die Drei Säulen stehen. So sei es: Nenne den Namen deiner Macht und die Farbe deines Wappens.“", 17, Ui.Gold);
                col.Text("Name der Autonomie:", 15, Ui.Dim);
                treatyName = Ui.Field(col.Next(44f), treatyName, 40);
                col.Text("Farbe des Wappens:", 15, Ui.Dim);
                var row = col.Next(44f);
                for (int i = 0; i < Autonomy.Colors.Length; i++)
                {
                    var r = new Rect(row.x + i * 70f, row.y, 60f, 40f);
                    Ui.Rect(r, Autonomy.Colors[i]);
                    if (i == treatyColor) Ui.Frame(r, Color.white, 3f);
                    if (GUI.Button(r, "", GUIStyle.none)) treatyColor = i;
                }
                col.Space(10f);
                col.Text("Der Vertrag bestimmt: Name und Wappen, Selbstverwaltung in Eichenhain, Pflichten (Freier Weg, Großer Aufruf, Verzicht auf Verbotene Künste, 10 % Anteil an die Kasse) und den Schutz durch den Thron (Art. 136).", 14, Ui.Dim);
                if (Ui.Button(col.Next(56f), "Vertrag mit dem Großen Siegel der Genesis besiegeln", treatyName.Trim().Length >= 3, 19))
                {
                    au.Sign(treatyName, treatyColor);
                    Closed = true;
                }
                return;
            }

            if (au.state == PetitionState.Audienz)
                col.Text("„Deine Audienz ist an Tag " + au.audienceDay + " um " + au.audienceHour + ":00 Uhr. Erscheine zur bestimmten Stunde — nicht früher, nicht später.“", 17, Ui.TextColor);
            else if (au.IsAutonomous)
                col.Text("„Herr von " + au.autonomyName + ". Halte die Drei Säulen aufrecht. Fällt eine, fällt dein Vertrag (Art. 140).“", 17, Ui.TextColor);
            else if (pc.level < GameConstants.EmperorLevel)
                col.Text("„Du willst einen Teil meines Reiches? Dann werde so stark wie ich — Stufe " + GameConstants.EmperorLevel + ". Und jeder einzelne Mensch auf diesem Land muss dich respektieren. Erst dann reiche deine Petition bei " + GameConstants.JudgeName + " ein.“", 17, Ui.TextColor);
            else
                col.Text("„Du hast meine Stufe erreicht. Respekt deines Volkes, Ressourcen und eigene Ordnung — beweise die Drei Säulen vor den Arbitern.“", 17, Ui.TextColor);

            col.Space(6f);
            col.Text("Es gibt zwei Wege zu eigener Macht: den Weg der Diplomatie und den Weg des Krieges. Einen dritten gibt es nicht. (Präambel)", 15, Ui.Dim);
            if (pc.rankIndex >= 6) col.Text("Der Kaiser nickt dir zu: Er selbst hat dir den Rang " + Ranks.Name(pc.rankIndex) + " verliehen.", 15, Ui.Gold);
        }

        void DrawJudge(Col col)
        {
            var pc = World.Player;
            var au = World.Autonomy;
            col.Text("Stand deiner Petition: " + au.StateText, 16, Ui.Gold);
            string reason;
            bool can = au.CanPetition(out reason);
            if (Ui.Button(col.Next(50f), "Petition des Aufstiegs für Eichenhain einreichen (" + GameConstants.PetitionFee + " Taler)", can, 17))
            {
                au.Submit();
                result = "Die Petition ist eingereicht. Die Revision dauert " + GameConstants.RevisionDays + " Tage.";
            }
            if (!can && reason != null) col.Text(reason, 14, Ui.Dim);
            col.Space(8f);

            if (!pc.permit)
            {
                bool ok = pc.rankIndex >= 2 && pc.taler >= GameConstants.PermitFee;
                if (Ui.Button(col.Next(48f), "Kaiserliche Erlaubnis für die Verbotenen Lande (" + GameConstants.PermitFee + " Taler, ab Rang D)", ok, 16))
                {
                    pc.TrySpend(GameConstants.PermitFee);
                    World.Treasury.Add("Gebühren", GameConstants.PermitFee);
                    pc.permit = true;
                    pc.inv.Add("erlaubnis", 1);
                    result = "Erlaubnis erteilt (Art. 112).";
                }
            }
            else col.Text("Du besitzt die Kaiserliche Erlaubnis für die Verbotenen Lande.", 15, Ui.Good);

            if (World.Law.Wanted > 0 && Ui.Button(col.Next(48f), "Mich dem Gericht stellen (offenes Bußgeld: " + World.Law.PendingFine + " Taler)", true, 16))
            {
                World.Law.Arrest();
                Closed = true;
            }
            col.Space(6f);
            col.Text("Einträge im Register: " + World.Law.record.Count + (World.Law.GreatTreason ? " · GROSSER VERRAT" : ""), 15, World.Law.GreatTreason ? Ui.Bad : Ui.Dim);
        }

        void DrawLandlord(Col col)
        {
            var d = World.Data;
            bool active = d.rentPaidUntilDay >= World.Day;
            col.Text(active ? "Dein Zimmer ist bezahlt bis einschließlich Tag " + d.rentPaidUntilDay + "." : "Du hast kein Zimmer gemietet.", 16, active ? Ui.Good : Ui.Dim);
            col.Text("Bett: schlafen bis 6:00, volle Heilung, Speichern. Truhe: Lager für Vorräte (zählt für die Säule der Ressourcen).", 15, Ui.Dim);
            foreach (int days in new[] { 1, 7, 30 })
            {
                int cost = days * GameConstants.RoomRentPerDay;
                if (Ui.Button(col.Next(46f), days + " Tag(e) mieten — " + cost + " Taler", World.Player.taler >= cost, 17))
                {
                    World.Player.TrySpend(cost);
                    int from = Mathf.Max(d.rentPaidUntilDay, World.Day - 1);
                    d.rentPaidUntilDay = from + days;
                    World.Treasury.Add("Miete", cost);
                    result = "Gemietet bis Tag " + d.rentPaidUntilDay + ".";
                }
            }
        }

        void DrawTrial(Col col)
        {
            var pc = World.Player;
            col.Text("Dein Rang: " + Ranks.Name(pc.rankIndex) + ". Mögliche Ränge bei Stufe " + pc.level + ": bis " + Ranks.Name(Ranks.LevelCap(pc.level)) + ".", 16, Ui.TextColor);
            col.Text("Wertung: Bestehen aller drei Wellen = C. Schneller als 40 s: +3, als 70 s: +2, als 110 s: +1. Kaum Schaden: +2. Höchstens so hoch, wie deine Stufe erlaubt.", 14, Ui.Dim);
            bool ok = !World.Trial.Active && World.Law.Wanted == 0 && pc.taler >= GameConstants.TrialFee;
            if (Ui.Button(col.Next(52f), "Prüfung beginnen (" + GameConstants.TrialFee + " Taler)", ok, 19))
            {
                pc.TrySpend(GameConstants.TrialFee);
                World.Treasury.Add("Gebühren", GameConstants.TrialFee);
                World.Trial.Begin();
                Closed = true;
            }
        }

        void DrawTower(Rect area)
        {
            var pc = World.Player;
            Ui.Text(new Rect(area.x, area.y, area.width, 24f), "Höchste bezwungene Etage: " + pc.towerBest + ". Du kannst jede Etage bis " + Mathf.Min(GameConstants.TowerFloors, pc.towerBest + 1) + " betreten.", 16, Ui.TextColor);
            float bw = 92f, bh = 44f;
            for (int i = 0; i < GameConstants.TowerFloors; i++)
            {
                int f = i + 1;
                var r = new Rect(area.x + (i % 10) * (bw + 8f), area.y + 34f + (i / 10) * (bh + 8f), bw, bh);
                string reason;
                bool ok = World.Tower.CanEnter(f, out reason);
                if (Ui.Button(r, "Etage " + f, ok, 15))
                {
                    World.Tower.Enter(f);
                    Closed = true;
                }
            }
            Ui.Text(new Rect(area.x, area.y + 34f + 5 * (bh + 8f) + 6f, area.width, 60f), "Ukas des Throns: Etage 11–20 ab Rang D, 21–30 ab C, 31–40 ab B, 41–50 ab A. 10 % der Beute gehen an die Kasse (Art. 115).", 14, Ui.Dim);
        }

        void DrawResident(Col col)
        {
            var r = npc.resident;
            if (r == null) return;
            col.Text("Respekt: " + Mathf.RoundToInt(r.respect) + " (respektiert dich ab " + GameConstants.RespectThreshold + ")", 16, r.Respects ? Ui.Good : Ui.TextColor);
            if (string.IsNullOrEmpty(r.needItem))
            {
                col.Text("„Heute fehlt mir nichts. Danke, dass du fragst.“", 16, Ui.Dim);
                return;
            }
            col.Text("„Ich bräuchte " + r.needCount + "× " + Items.Name(r.needItem) + ". Kannst du helfen?“", 17, Ui.TextColor);
            bool has = World.Player.inv.Count(r.needItem) >= r.needCount;
            if (Ui.Button(col.Next(48f), has ? "Geben: " + r.needCount + "× " + Items.Name(r.needItem) : "Du hast nicht genug " + Items.Name(r.needItem), has, 17))
            {
                World.Residents.Fulfil(r, World.Player);
                result = r.name + " ist dir dankbar (+12 Respekt, alle anderen +1).";
            }
        }

        void DrawMercenary(Col col)
        {
            var au = World.Autonomy;
            col.Text("Dorfwachen: " + au.guards + " / " + GameConstants.PillarGuards + " (höchstens " + GameConstants.MaxGuards + "). Anwerbung " + GameConstants.GuardHireCost + " Taler, Sold " + GameConstants.GuardUpkeepPerDay + " Taler pro Tag und Wache.", 16, Ui.TextColor);
            if (Ui.Button(col.Next(50f), "Wache anwerben (" + GameConstants.GuardHireCost + " Taler)", au.guards < GameConstants.MaxGuards && World.Player.taler >= GameConstants.GuardHireCost, 18))
                au.HireGuard();
        }
    }

    // ---------- Markt ----------
    public static class MarketView
    {
        public static void Draw(Rect area, ref Vector2 scroll)
        {
            var m = World.Market;
            var pc = World.Player;
            Ui.Text(new Rect(area.x, area.y, area.width, 24f), (m.IsOpen ? "Geöffnet" : "Geschlossen (6–22 Uhr)") + " · Deine Taler: " + pc.taler + " · Handelsabgabe 10 % an die Kasse (Art. 93)", 15, m.IsOpen ? Ui.Gold : Ui.Bad);
            var view = new Rect(area.x, area.y + 30f, area.width, area.height - 30f);
            float rowH = 38f;
            var goods = m.goods;
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, view.width - 20f, goods.Count * (rowH + 3f)));
            float y = 0f;
            foreach (var g in goods)
            {
                var item = Items.Get(g.id);
                var row = new Rect(0f, y, view.width - 24f, rowH);
                Ui.Rect(row, Ui.PanelLight);
                float tr = m.Trend(g.id);
                string arrow = tr > 1.05f ? " ▲" : tr < 0.95f ? " ▼" : "";
                Ui.Text(new Rect(row.x + 8f, row.y, 250f, rowH), item.name + arrow, 15, Ui.TextColor, TextAnchor.MiddleLeft);
                Ui.Text(new Rect(row.x + 250f, row.y, 120f, rowH), "Bestand " + Mathf.FloorToInt(g.stock), 13, Ui.Dim, TextAnchor.MiddleLeft);
                Ui.Text(new Rect(row.x + 370f, row.y, 140f, rowH), "Hast: " + pc.inv.Count(g.id), 13, Ui.Dim, TextAnchor.MiddleLeft);
                int buy = m.BuyPrice(g.id), sell = m.SellPrice(g.id);
                if (Ui.Button(new Rect(row.xMax - 330f, row.y + 3f, 160f, rowH - 6f), "Kaufen " + buy + " T", m.IsOpen && g.stock >= 1f && pc.taler >= buy, 14))
                {
                    string err = m.Buy(pc, g.id);
                    if (err != null) Notify.System(err);
                }
                if (Ui.Button(new Rect(row.xMax - 164f, row.y + 3f, 160f, rowH - 6f), "Verkaufen " + sell + " T", m.IsOpen && pc.inv.Count(g.id) > 0, 14))
                {
                    string err = m.Sell(pc, g.id);
                    if (err != null) Notify.System(err);
                }
                y += rowH + 3f;
            }
            GUI.EndScrollView();
        }
    }

    // ---------- Handwerk ----------
    public class CraftingDialogue : DialogueBase
    {
        readonly bool forge;

        public CraftingDialogue(bool forge) { this.forge = forge; }

        public override string Title { get { return forge ? "Amboss — Schmieden" : "Handwerk"; } }

        public override void Draw(Rect area)
        {
            DrawRecipes(area, forge, ref scroll);
        }

        public static void DrawRecipes(Rect area, bool forge, ref Vector2 scroll)
        {
            var pc = World.Player;
            var list = new List<Recipe>();
            foreach (var r in Recipes.All) if (forge || !r.forge) list.Add(r);
            float rowH = 54f;
            scroll = GUI.BeginScrollView(area, scroll, new Rect(0f, 0f, area.width - 20f, list.Count * (rowH + 4f)));
            float y = 0f;
            foreach (var r in list)
            {
                var item = Items.Get(r.result);
                var row = new Rect(0f, y, area.width - 24f, rowH);
                Ui.Rect(row, Ui.PanelLight);
                string title = item.name + (r.count > 1 ? " ×" + r.count : "") + "  (" + item.mass + " kg" + (item.category == ItemCategory.Waffe && item.weaponKind == WeaponKind.Nahkampf ? ", L " + item.length + " m" : "") + (item.armor > 0f ? ", Rüstung " + item.armor : "") + ")";
                Ui.Text(new Rect(row.x + 8f, row.y + 2f, row.width - 160f, 24f), title, 15, Ui.TextColor, TextAnchor.UpperLeft, true);
                var parts = new System.Text.StringBuilder();
                bool ok = true;
                foreach (var input in r.inputs)
                {
                    int have = pc.inv.Count(input.id);
                    if (have < input.count) ok = false;
                    if (parts.Length > 0) parts.Append(", ");
                    parts.Append(input.count).Append("× ").Append(Items.Name(input.id)).Append(" (").Append(have).Append(")");
                }
                if (r.forge) parts.Append(" · Amboss");
                Ui.Text(new Rect(row.x + 8f, row.y + 26f, row.width - 160f, 24f), parts.ToString(), 13, ok ? Ui.Dim : Ui.Bad);
                if (Ui.Button(new Rect(row.xMax - 140f, row.y + 8f, 130f, rowH - 16f), "Herstellen", ok, 15))
                {
                    foreach (var input in r.inputs) pc.inv.Remove(input.id, input.count);
                    int n = r.count;
                    if (r.result == "eisenbarren" && pc.race.id == "zwerg") n++;
                    pc.inv.Add(r.result, n);
                    pc.GainExp(10f);
                    Notify.Combat("Hergestellt: " + n + "× " + item.name);
                }
                y += rowH + 4f;
            }
            GUI.EndScrollView();
        }
    }

    // ---------- Truhe ----------
    public class StorageDialogue : DialogueBase
    {
        Vector2 scroll2;

        public override string Title { get { return "Truhe im Mietzimmer"; } }

        public override void Draw(Rect area)
        {
            var pc = World.Player;
            float half = area.width * 0.5f - 10f;
            Ui.Text(new Rect(area.x, area.y, half, 24f), "Inventar", 17, Ui.Gold, TextAnchor.UpperLeft, true);
            Ui.Text(new Rect(area.x + half + 20f, area.y, half, 24f), "Truhe", 17, Ui.Gold, TextAnchor.UpperLeft, true);
            Column(new Rect(area.x, area.y + 30f, half, area.height - 30f), pc.inv, pc.storage, ref scroll, "Einlagern →");
            Column(new Rect(area.x + half + 20f, area.y + 30f, half, area.height - 30f), pc.storage, pc.inv, ref scroll2, "← Nehmen");
        }

        static void Column(Rect area, Inventory from, Inventory to, ref Vector2 scroll, string label)
        {
            float rowH = 38f;
            scroll = GUI.BeginScrollView(area, scroll, new Rect(0f, 0f, area.width - 20f, from.items.Count * (rowH + 3f)));
            float y = 0f;
            foreach (var s in from.items.ToArray())
            {
                var row = new Rect(0f, y, area.width - 24f, rowH);
                Ui.Rect(row, Ui.PanelLight);
                Ui.Text(new Rect(row.x + 8f, row.y, row.width - 230f, rowH), Items.Name(s.id) + " ×" + s.count, 15, Ui.TextColor, TextAnchor.MiddleLeft);
                if (Ui.Button(new Rect(row.xMax - 220f, row.y + 3f, 110f, rowH - 6f), label, true, 13))
                {
                    if (from.Remove(s.id, 1)) to.Add(s.id, 1);
                }
                if (Ui.Button(new Rect(row.xMax - 104f, row.y + 3f, 100f, rowH - 6f), "Alle", true, 13))
                {
                    int n = s.count;
                    if (from.Remove(s.id, n)) to.Add(s.id, n);
                }
                y += rowH + 3f;
            }
            GUI.EndScrollView();
        }
    }

    // ---------- Große Tore ----------
    public class GateDialogue : DialogueBase
    {
        readonly GreatGate gate;

        public GateDialogue(GreatGate gate) { this.gate = gate; }

        public override string Title { get { return "Großes Tor " + gate.gateName; } }

        public override void Draw(Rect area)
        {
            var pc = World.Player;
            var col = new Col(area);
            col.Text("Die Gebühr für den Durchgang setzt der Thron fest und sie ist auf dem ganzen Kontinent gleich: " + GameConstants.GateFee + " Taler (Art. 34).", 16, Ui.TextColor);
            foreach (var g in GreatGate.Gates.ToArray())
            {
                if (g == gate) continue;
                bool ok = pc.taler >= GameConstants.GateFee && World.Law.Wanted == 0;
                if (Ui.Button(col.Next(48f), "Reisen nach " + g.gateName + " (" + GameConstants.GateFee + " Taler)", ok, 18))
                {
                    pc.TrySpend(GameConstants.GateFee);
                    World.Treasury.Add("Torgebühren", GameConstants.GateFee);
                    Vector3 dest = g.transform.position + g.transform.forward * 4f + Vector3.up * 0.3f;
                    pc.motor.Teleport(dest, g.transform.eulerAngles.y);
                    Fx.Ring(dest, 3f, new Color(0.4f, 0.75f, 1f), 0.8f);
                    Closed = true;
                }
            }
            if (World.Law.Wanted > 0) col.Text("Die Torwächter lassen Gesuchte nicht passieren.", 15, Ui.Bad);
            col.Space(10f);
            col.Text("VERKÜNDUNGEN (Art. 54)", 17, Ui.Gold, true);
            int n = 0;
            foreach (var a in Notify.Announcements)
            {
                col.Text(a, 14, Ui.Dim);
                if (++n >= 6) break;
            }
            if (n == 0) col.Text("Keine neuen Verkündungen.", 14, Ui.Dim);
        }
    }

    // ---------- Rathaus ----------
    public class TownHallDialogue : DialogueBase
    {
        public override string Title { get { return "Rathaus von Eichenhain"; } }

        public override void Draw(Rect area)
        {
            var au = World.Autonomy;
            var pc = World.Player;
            var col = new Col(area);
            if (!au.townHall)
            {
                col.Text("Eine Autonomie braucht eigene Organe der Verwaltung (Art. 133). Errichte hier das Rathaus.", 17, Ui.TextColor);
                col.Text("Benötigt: 30 Holz (" + pc.inv.Count("holz") + "), 20 Stein (" + pc.inv.Count("stein") + "), 10 Eisenbarren (" + pc.inv.Count("eisenbarren") + ")", 16, Ui.Dim);
                bool ok = pc.inv.Count("holz") >= 30 && pc.inv.Count("stein") >= 20 && pc.inv.Count("eisenbarren") >= 10;
                if (Ui.Button(col.Next(52f), "Rathaus errichten", ok, 19))
                {
                    au.BuildTownHall();
                    Closed = true;
                }
                return;
            }
            col.Text(au.IsAutonomous ? "Sitz der Autonomie „" + au.autonomyName + "“." : "Das Rathaus steht. Hier entsteht dein Gesetzbuch (Fenster der Drei Säulen, Taste P).", 17, Ui.TextColor);
            col.Text("Stand: " + au.StateText, 16, Ui.Gold);
            col.Text("Einwohner, die dich respektieren: " + World.Residents.RespectingCount + " / " + World.Residents.list.Count, 16, Ui.Dim);
        }
    }
}
