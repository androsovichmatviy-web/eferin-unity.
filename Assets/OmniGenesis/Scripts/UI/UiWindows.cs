using UnityEngine;

namespace OmniGenesis
{
    public interface IDialogue
    {
        string Title { get; }
        bool Closed { get; }
        void Draw(Rect area);
    }

    public enum WindowId { None, Character, Inventory, Map, Edict, Pillars, Quests, Dialogue, Pause }

    // Systemfenster: Charakter (C), Inventar (I), Karte (M), Edikt (J), Drei Säulen (P), Menü (Esc), Dialoge.
    public class UiWindows : MonoBehaviour
    {
        public WindowId Open { get; private set; }
        public bool BlocksGameplay { get { return Open != WindowId.None; } }

        IDialogue dialogue;
        Vector2 scroll, scroll2;
        int edictTab;
        Texture2D mapTex;

        static readonly string[] StatNames = { "Stärke", "Beweglichkeit", "Vitalität", "Intelligenz" };
        static readonly string[] StatHints =
        {
            "kürzere Schwungzeit ⇒ größeres a ⇒ größeres F; Traglast +1,2 kg",
            "Lauftempo, Schlag- und Schusstempo, kritische Treffer",
            "+10 LP, +0,2 Rüstung, Regeneration",
            "+6 Äther, Ätherbeschleunigung +3 %, Ätherregeneration",
        };
        static readonly string[] EdictTabs = { "Edikt", "Ränge", "Kasse", "Verkündungen", "Register" };

        public void SetMapTexture(Texture2D t) { mapTex = t; }

        public void OpenDialogue(IDialogue d)
        {
            dialogue = d;
            Open = WindowId.Dialogue;
            scroll = Vector2.zero;
        }

        public void Close()
        {
            Open = WindowId.None;
            dialogue = null;
            Time.timeScale = 1f;
            GUIUtility.keyboardControl = 0;
        }

        void Toggle(WindowId id)
        {
            if (Open == id) { Close(); return; }
            Open = id;
            scroll = scroll2 = Vector2.zero;
            Time.timeScale = id == WindowId.Pause ? 0f : 1f;
        }

        void Update()
        {
            if (!World.Ready) return;
            if (GameInput.Pressed(Act.Pause))
            {
                if (Open != WindowId.None) Close();
                else Toggle(WindowId.Pause);
                return;
            }
            if (Open == WindowId.Dialogue)
            {
                if (dialogue == null || dialogue.Closed) Close();
                return;
            }
            if (Open == WindowId.Pause) return;
            if (GameInput.Pressed(Act.Character)) Toggle(WindowId.Character);
            if (GameInput.Pressed(Act.Inventory)) Toggle(WindowId.Inventory);
            if (GameInput.Pressed(Act.Map)) Toggle(WindowId.Map);
            if (GameInput.Pressed(Act.Edict)) Toggle(WindowId.Edict);
            if (GameInput.Pressed(Act.Pillars)) Toggle(WindowId.Pillars);
            if (GameInput.Pressed(Act.Quests)) Toggle(WindowId.Quests);
            if (GameInput.Pressed(Act.QuickSave))
            {
                WorldBootstrap.SaveGame();
                Notify.System("Spiel gespeichert.");
            }
        }

        void OnGUI()
        {
            if (!World.Ready || Open == WindowId.None) return;
            GUI.depth = 0;
            Ui.BeginScaled();
            switch (Open)
            {
                case WindowId.Character: DrawCharacter(); break;
                case WindowId.Inventory: DrawInventory(); break;
                case WindowId.Map: DrawMap(); break;
                case WindowId.Edict: DrawEdict(); break;
                case WindowId.Pillars: DrawPillars(); break;
                case WindowId.Quests: DrawQuests(); break;
                case WindowId.Pause: DrawPause(); break;
                case WindowId.Dialogue: DrawDialogue(); break;
            }
        }

        Rect Window(float w, float h, string title)
        {
            var r = Ui.Centered(w, h);
            var inner = Ui.Panel(r, title);
            if (Ui.Button(new Rect(r.xMax - 46f, r.y + 8f, 34f, 32f), "X")) Close();
            return inner;
        }

        // ---------- Charakter ----------
        void DrawCharacter()
        {
            var pc = World.Player;
            var area = Window(1180f, 800f, "STATUS — " + pc.charName);
            var left = new Col(new Rect(area.x, area.y, 560f, area.height));
            left.Text(pc.race.name + " · " + pc.ClassTitle + " · Stufe " + pc.level + " / " + GameConstants.MaxLevel, 20, Ui.Gold, true);
            float need = PlayerCharacter.ExpToNext(pc.level);
            left.Text(pc.level >= GameConstants.MaxLevel ? "Höchststufe erreicht." : "Erfahrung: " + Mathf.FloorToInt(pc.exp) + " / " + Mathf.RoundToInt(need), 16, Ui.Dim);
            left.Text("Freie Attributpunkte: " + pc.freePoints, 17, pc.freePoints > 0 ? Ui.Good : Ui.Dim, true);
            left.Space(4f);
            int[] values = { pc.Str, pc.Agi, pc.Vit, pc.Int };
            for (int i = 0; i < 4; i++)
            {
                var row = left.Next(46f, 4f);
                Ui.Rect(row, Ui.PanelLight);
                Ui.Text(new Rect(row.x + 10f, row.y + 2f, 200f, 24f), StatNames[i], 18, Ui.TextColor, TextAnchor.MiddleLeft, true);
                Ui.Text(new Rect(row.x + 200f, row.y + 2f, 60f, 24f), values[i].ToString(), 20, Ui.Gold, TextAnchor.MiddleRight, true);
                Ui.Text(new Rect(row.x + 10f, row.y + 24f, 480f, 20f), StatHints[i], 12, Ui.Dim);
                if (Ui.Button(new Rect(row.xMax - 52f, row.y + 6f, 42f, 34f), "+", pc.freePoints > 0, 22)) pc.Allocate(i);
            }
            left.Space(6f);
            left.Text("Lebenspunkte " + Mathf.RoundToInt(pc.MaxHp) + " · Äther " + Mathf.RoundToInt(pc.MaxAether) + " · Ausdauer " + Mathf.RoundToInt(pc.MaxStamina), 16, Ui.TextColor);
            left.Text("Rüstung " + pc.ArmorValue.ToString("0.0") + " (Schaden × 60/(60+Rüstung)) · Kritisch " + Mathf.RoundToInt(pc.Crit * 100f) + " %", 16, Ui.TextColor);
            left.Text("Körpermasse " + pc.BodyMass.ToString("0") + " kg + Ausrüstung " + pc.EquipmentMass.ToString("0.0") + " kg · Traglast " + pc.CarriedMass.ToString("0.0") + " / " + pc.CarryCapacity.ToString("0") + " kg", 16, Ui.TextColor);

            left.Space(8f);
            left.Text("PHYSIK DEINES ANGRIFFS", 17, Ui.Gold, true);
            var w = pc.Weapon;
            if (w.weaponKind == WeaponKind.Nahkampf)
            {
                float t = pc.SwingTime(w);
                float v = 2.4f * w.length / t;
                float a = v / t;
                float f = w.mass * a;
                left.Text(w.name + ": m = " + w.mass + " kg, L = " + w.length + " m, Schwungzeit t = " + t.ToString("0.00") + " s", 15, Ui.TextColor);
                left.Text("v = θ·L/t = " + v.ToString("0.0") + " m/s   a = v/t = " + a.ToString("0") + " m/s²   F = m·a = " + f.ToString("0") + " N", 15, Ui.TextColor);
                left.Text("Schaden ≈ F · " + GameConstants.DamagePerNewton + " = " + (f * GameConstants.DamagePerNewton).ToString("0") + " (Anlauf erhöht a zusätzlich um v_Anlauf / t)", 15, Ui.Dim);
            }
            else if (w.weaponKind == WeaponKind.Bogen)
            {
                float force = w.drawForce * (1f + pc.Str * 0.005f);
                float v = Mathf.Min(70f, PhysicsCombat.ArrowSpeed(force, 0.6f, 0.05f));
                left.Text(w.name + ": Zugkraft F = " + force.ToString("0") + " N, Pfeil m = 0,05 kg, Auszug d = 0,6 m", 15, Ui.TextColor);
                left.Text("a = F/m = " + (force / 0.05f).ToString("0") + " m/s²   v = √(2·F·d/m) = " + v.ToString("0") + " m/s   E = ½·m·v² = " + (0.025f * v * v).ToString("0") + " J", 15, Ui.TextColor);
            }
            else
            {
                float acc = 120f * (1f + pc.Int * 0.03f) * w.focus;
                left.Text(w.name + ": Äthermasse 0,25 kg, a = 120 · (1 + 0,03 · INT) · Fokus = " + acc.ToString("0") + " m/s²", 15, Ui.TextColor);
                left.Text("F = m·a = " + (0.25f * acc).ToString("0") + " N", 15, Ui.TextColor);
            }

            var right = new Col(new Rect(area.x + 600f, area.y, area.width - 600f, area.height));
            right.Text("AUSRÜSTUNG", 17, Ui.Gold, true);
            var wr = right.Next(36f);
            Ui.Text(new Rect(wr.x, wr.y, wr.width - 130f, 36f), "Waffe: " + w.name + " (" + w.mass + " kg)", 16, Ui.TextColor, TextAnchor.MiddleLeft);
            if (Ui.Button(new Rect(wr.xMax - 120f, wr.y + 2f, 120f, 32f), "Ablegen", Items.Get(pc.weaponId) != null)) pc.Unequip(true);
            var ar = right.Next(36f);
            Ui.Text(new Rect(ar.x, ar.y, ar.width - 130f, 36f), "Rüstung: " + (pc.Armor != null ? pc.Armor.name + " (" + pc.Armor.mass + " kg, " + pc.Armor.armor + ")" : "keine"), 16, Ui.TextColor, TextAnchor.MiddleLeft);
            if (Ui.Button(new Rect(ar.xMax - 120f, ar.y + 2f, 120f, 32f), "Ablegen", pc.Armor != null)) pc.Unequip(false);

            right.Space(8f);
            right.Text("RANG UND TURM", 17, Ui.Gold, true);
            right.Text("Rang des Aufstiegs: " + Ranks.Name(pc.rankIndex) + (string.IsNullOrEmpty(World.Quests.title) ? "" : " · Titel: „" + World.Quests.title + "“"), 16, Ui.TextColor);
            right.Text("Kaiserliche Erlaubnis für die Verbotenen Lande: " + (pc.permit ? "ja" : "nein"), 16, pc.permit ? Ui.Good : Ui.Dim);

            right.Space(8f);
            right.Text("FÄHIGKEITEN", 17, Ui.Gold, true);
            string[] keys = { "Q", "E", "R" };
            for (int i = 0; i < 3; i++)
            {
                var a = pc.combat.Slot(i);
                if (a == null) continue;
                right.Text("[" + keys[i] + "] " + a.name + " — " + a.description + " (Äther " + a.cost + ", " + a.cooldown + " s)", 14, Ui.TextColor);
            }

            right.Space(8f);
            right.Text("ZWEITE KLASSE", 17, Ui.Gold, true);
            if (pc.hybrid != null)
            {
                right.Text(pc.hybrid.name + ": " + pc.hybrid.description, 15, Ui.Good);
            }
            else if (pc.level < GameConstants.SecondClassLevel)
            {
                right.Text("Ab Stufe " + GameConstants.SecondClassLevel + " wählst du eine zweite Klasse. Beide zusammen ergeben eine Hybridklasse (z. B. Kampfmagier + Assassine = Nekromant).", 14, Ui.Dim);
            }
            else
            {
                foreach (var c in Classes.All)
                {
                    if (c.id == pc.cls.id) continue;
                    var h = Classes.Hybrid(pc.cls.id, c.id);
                    if (h == null) continue;
                    if (Ui.Button(right.Next(34f, 4f), c.name + "  ⇒  " + h.name + " (" + h.skillR.name + ")", true, 15)) pc.ChooseSecondClass(c.id);
                }
            }
        }

        // ---------- Inventar ----------
        void DrawInventory()
        {
            var pc = World.Player;
            var area = Window(1180f, 780f, "INVENTAR UND HANDWERK");
            var listRect = new Rect(area.x, area.y, 560f, area.height);
            Ui.Text(new Rect(listRect.x, listRect.y, listRect.width, 24f), "Last " + pc.CarriedMass.ToString("0.0") + " / " + pc.CarryCapacity.ToString("0") + " kg · " + pc.taler + " Taler", 16, Ui.Gold);
            var view = new Rect(listRect.x, listRect.y + 30f, listRect.width, listRect.height - 30f);
            float rowH = 40f;
            var content = new Rect(0f, 0f, view.width - 20f, Mathf.Max(view.height, pc.inv.items.Count * (rowH + 4f)));
            scroll = GUI.BeginScrollView(view, scroll, content);
            float y = 0f;
            foreach (var s in pc.inv.items.ToArray())
            {
                var d = Items.Get(s.id);
                var row = new Rect(0f, y, content.width, rowH);
                Ui.Rect(row, Ui.PanelLight);
                Ui.Text(new Rect(row.x + 10f, row.y, 300f, rowH), d.name + "  ×" + s.count, 16, Ui.TextColor, TextAnchor.MiddleLeft);
                Ui.Text(new Rect(row.x + 260f, row.y, 100f, rowH), (d.mass * s.count).ToString("0.0") + " kg", 13, Ui.Dim, TextAnchor.MiddleLeft);
                var btn = new Rect(row.xMax - 130f, row.y + 4f, 120f, rowH - 8f);
                if (d.category == ItemCategory.Waffe || d.category == ItemCategory.Ruestung)
                {
                    if (Ui.Button(btn, "Ausrüsten", true, 15)) pc.Equip(s.id);
                }
                else if (d.category == ItemCategory.Verbrauch)
                {
                    if (Ui.Button(btn, "Benutzen", true, 15)) pc.Use(s.id);
                }
                else if (d.category == ItemCategory.Material)
                {
                    if (Ui.Button(new Rect(row.xMax - 130f, row.y + 4f, 120f, rowH - 8f), "1 ablegen", true, 14)) pc.inv.Remove(s.id, 1);
                }
                y += rowH + 4f;
            }
            if (pc.inv.items.Count == 0) Ui.Text(new Rect(10f, 10f, 400f, 30f), "Das Inventar ist leer.", 16, Ui.Dim);
            GUI.EndScrollView();

            var craft = new Rect(area.x + 590f, area.y, area.width - 590f, area.height);
            Ui.Text(new Rect(craft.x, craft.y, craft.width, 24f), "Handwerk unterwegs (Metall nur am Amboss der Schmiede)", 16, Ui.Gold);
            CraftingDialogue.DrawRecipes(new Rect(craft.x, craft.y + 30f, craft.width, craft.height - 30f), false, ref scroll2);
        }

        // ---------- Karte ----------
        void DrawMap()
        {
            var area = Window(1240f, 860f, "KARTE DES KONTINENTS (AUSSCHNITT)");
            float size = Mathf.Min(area.height, 760f);
            var map = new Rect(area.x, area.y, size, size);
            if (mapTex != null)
            {
                GUI.DrawTextureWithTexCoords(map, mapTex, new Rect(0f, 0f, 1f, 1f));
            }
            Ui.Frame(map, Ui.Border, 1.5f);

            Marker(map, Places.CityPlaza, "Grad Genesis", Ui.Gold);
            Marker(map, Places.GuildHall, "Gildenhaus", new Color(0.6f, 0.9f, 0.6f));
            Marker(map, Places.ArenaCenter, "Arena", Ui.TextColor);
            Marker(map, Places.VillageCenter, World.Autonomy.IsAutonomous ? World.Autonomy.autonomyName : "Eichenhain", Ui.Good);
            Marker(map, Places.VillageGate, "Tor", new Color(0.4f, 0.75f, 1f));
            Marker(map, Places.CityGate, "Tor", new Color(0.4f, 0.75f, 1f));
            Marker(map, Places.BorderGate, "Tor", new Color(0.4f, 0.75f, 1f));
            Marker(map, Places.SealPos, "Siegel " + Mathf.RoundToInt(World.Rift.Strength) + " %", new Color(0.8f, 0.5f, 1f));
            Marker(map, Places.RiftPos, "Der Riss", new Color(0.8f, 0.3f, 1f));
            Marker(map, new Vector3(WorldLayout.Hills.x, 0f, WorldLayout.Hills.y), "Erzhöhen", Ui.Dim);
            Vector2 lineA = MapPos(map, new Vector3(-200f, 0f, WorldLayout.ForbiddenZ));
            Ui.Rect(new Rect(map.x, lineA.y, map.width, 2f), new Color(0.7f, 0.3f, 1f, 0.8f));
            Ui.Text(new Rect(map.x + 6f, lineA.y - 22f, 300f, 20f), "Grenze der Verbotenen Lande (Art. 112)", 13, new Color(0.85f, 0.6f, 1f));

            foreach (var s in World.Quests.active)
            {
                if (s.Step == null || string.IsNullOrEmpty(s.Step.place)) continue;
                Vector2 qp = MapPos(map, Quests.PlacePos(s.Step.place));
                Ui.Rect(new Rect(qp.x - 7f, qp.y - 7f, 14f, 14f), Ui.Gold);
                Ui.Frame(new Rect(qp.x - 9f, qp.y - 9f, 18f, 18f), Color.black, 2f);
                Ui.Text(new Rect(qp.x + 10f, qp.y + 2f, 260f, 20f), s.def.title, 12, Ui.Gold);
            }

            var pc = World.Player;
            {
                Vector2 p = MapPos(map, pc.transform.position);
                Ui.Rect(new Rect(p.x - 6f, p.y - 6f, 12f, 12f), Color.yellow);
                Vector3 f = pc.transform.forward;
                Ui.Rect(new Rect(p.x + f.x * 12f - 3f, p.y - f.z * 12f - 3f, 6f, 6f), Color.yellow);
            }

            var col = new Col(new Rect(map.xMax + 24f, area.y, area.xMax - map.xMax - 24f, area.height));
            col.Text("ÖKOSYSTEM DER PROVINZ", 17, Ui.Gold, true);
            col.Text("Getötete Tiere kehren nie zurück. Bestände wachsen nur durch Nachwuchs (logistisch bis zur Tragfähigkeit).", 13, Ui.Dim);
            col.Text(World.Eco.Census(), 16, Ui.TextColor);
            col.Text("WELT", 17, Ui.Gold, true);
            col.Text(World.Clock.Stamp() + " · " + Weather.Name(World.Weather.Current), 15, Ui.TextColor);
            col.Text("Siegel des Risses: " + Mathf.RoundToInt(World.Rift.Strength) + " %" + (World.Rift.Summons ? " — Großer Aufruf!" : ""), 15, Ui.TextColor);
            col.Text("Bäume stehend: " + Mathf.RoundToInt(World.Nodes.TreeFraction * 100f) + " %", 15, Ui.TextColor);
            col.Text("MARKTPREISE (Ankauf)", 17, Ui.Gold, true);
            foreach (var id in new[] { "fleisch", "holz", "eisenbarren", "heiltrank", "aetherkristall" })
            {
                float tr = World.Market.Trend(id);
                string arrow = tr > 1.05f ? " ▲" : tr < 0.95f ? " ▼" : "";
                col.Text(Items.Name(id) + ": " + World.Market.SellPrice(id) + " T" + arrow, 15, Ui.TextColor);
            }
        }

        Vector2 MapPos(Rect map, Vector3 world)
        {
            float u = (world.x + WorldLayout.Half) / (WorldLayout.Half * 2f);
            float v = (world.z + WorldLayout.Half) / (WorldLayout.Half * 2f);
            return new Vector2(map.x + u * map.width, map.y + (1f - v) * map.height);
        }

        void Marker(Rect map, Vector3 world, string label, Color c)
        {
            Vector2 p = MapPos(map, world);
            Ui.Rect(new Rect(p.x - 4f, p.y - 4f, 8f, 8f), c);
            Ui.Text(new Rect(p.x + 7f, p.y - 10f, 220f, 20f), label, 13, c);
        }

        // ---------- Edikt ----------
        void DrawEdict()
        {
            var area = Window(1240f, 860f, "KAISERLICHES EDIKT VON OMNI GENESIS");
            float tw = area.width / EdictTabs.Length;
            for (int i = 0; i < EdictTabs.Length; i++)
            {
                if (Ui.Button(new Rect(area.x + i * tw, area.y, tw - 6f, 36f), (i == edictTab ? "• " : "") + EdictTabs[i], true, 16))
                {
                    edictTab = i;
                    scroll = Vector2.zero;
                }
            }
            var view = new Rect(area.x, area.y + 48f, area.width, area.height - 48f);
            switch (edictTab)
            {
                case 0: DrawEdictText(view); break;
                case 1: DrawRanks(view); break;
                case 2: DrawTreasury(view); break;
                case 3: DrawList(view, Notify.Announcements, "Noch keine Verkündungen."); break;
                default: DrawList(view, World.Law.record, "Dein Register ist rein."); break;
            }
        }

        void DrawEdictText(Rect view)
        {
            float w = view.width - 30f;
            float h = Ui.TextHeight(EdictTexts.Preamble, 17, w) + 30f;
            foreach (var a in EdictTexts.Articles) h += Ui.TextHeight(a.text, 15, w) + Ui.TextHeight(a.mechanic, 13, w) + 40f;
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, w, h));
            var col = new Col(new Rect(0f, 0f, w, h));
            col.Text(EdictTexts.Preamble, 17, Ui.Gold);
            col.Space(12f);
            foreach (var a in EdictTexts.Articles)
            {
                col.Text(a.number + " — " + a.title, 16, Ui.Gold, true);
                col.Text(a.text, 15, Ui.TextColor);
                col.Text("Im Spiel: " + a.mechanic, 13, new Color(0.55f, 0.85f, 1f));
                col.Space(8f);
            }
            GUI.EndScrollView();
        }

        void DrawRanks(Rect view)
        {
            var col = new Col(view);
            col.Text("Ränge des Aufstiegs (Art. 48, 69). Vergabe durch die Prüfung der Meisterschaft in der Arena westlich von Grad Genesis.", 16, Ui.TextColor);
            col.Text("Der erreichte Rang ist begrenzt durch deine Leistung (Zeit, erlittener Schaden) und durch deine Stufe.", 15, Ui.Dim);
            col.Space(8f);
            for (int i = 0; i < Ranks.Names.Length; i++)
            {
                string by = i >= 6 ? " — verliehen durch den Kaiser" : "";
                col.Text("Rang " + Ranks.Names[i] + ": ab Stufe " + Ranks.LevelReq[i] + by + (i == World.Player.rankIndex ? "   ◄ dein Rang" : ""), 17, Ranks.Color(i), i == World.Player.rankIndex);
            }
            col.Space(8f);
            col.Text("Kaiserliche Erlaubnis für die Verbotenen Lande: ab Rang D in der Kanzlei (Art. 112). Ohne Rang beginnt die Geschichte nicht über Kapitel I hinaus.", 15, Ui.TextColor);
        }

        void DrawTreasury(Rect view)
        {
            var t = World.Treasury;
            var col = new Col(view);
            col.Text("Kaiserliche Kasse: " + t.Balance.ToString("N0") + " Taler", 20, Ui.Gold, true);
            col.Text("Große Rolle der Kasse (Art. 82) — Einnahmen dieser Welt:", 15, Ui.Dim);
            foreach (var kv in t.income) col.Text("+ " + kv.Key + ": " + kv.Value.ToString("N0"), 16, Ui.Good);
            col.Text("Ausgaben:", 15, Ui.Dim);
            foreach (var kv in t.expenses) col.Text("− " + kv.Key + ": " + kv.Value.ToString("N0"), 16, Ui.Bad);
            col.Space(8f);
            col.Text("Handelsabgabe 10 % (Art. 93) · Miete und Abgabe (Art. 117) · Gebühren und Bußgelder · Torgebühr " + GameConstants.GateFee + " Taler (Art. 34)", 14, Ui.Dim);
        }

        void DrawList(Rect view, System.Collections.Generic.List<string> list, string empty)
        {
            float w = view.width - 30f;
            float h = 10f;
            foreach (var s in list) h += Ui.TextHeight(s, 15, w) + 8f;
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, w, Mathf.Max(h, 40f)));
            var col = new Col(new Rect(0f, 0f, w, h));
            if (list.Count == 0) col.Text(empty, 16, Ui.Dim);
            foreach (var s in list) col.Text(s, 15, Ui.TextColor);
            GUI.EndScrollView();
        }

        // ---------- Drei Säulen ----------
        void DrawPillars()
        {
            var au = World.Autonomy;
            var area = Window(1240f, 880f, "WEG DER DIPLOMATIE — EICHENHAIN");
            var left = new Col(new Rect(area.x, area.y, 640f, area.height));
            left.Text("Um einen Teil des Reiches zu erhalten, musst du so stark sein wie der Kaiser, und alle Einwohner des Landes müssen dich respektieren. Dann prüfen die Arbiter die Drei Säulen (Art. 130).", 15, Ui.Dim);
            left.Space(4f);
            foreach (var p in au.All())
            {
                var row = left.Next(Ui.TextHeight(p.detail, 14, 560f) + 36f, 6f);
                Ui.Rect(row, Ui.PanelLight);
                Ui.Rect(new Rect(row.x, row.y, 6f, row.height), p.ok ? Ui.Good : Ui.Bad);
                Ui.Text(new Rect(row.x + 16f, row.y + 4f, 460f, 24f), p.title, 17, Ui.TextColor, TextAnchor.UpperLeft, true);
                Ui.Text(new Rect(row.xMax - 140f, row.y + 4f, 130f, 24f), p.ok ? "ERFÜLLT" : "OFFEN", 15, p.ok ? Ui.Good : Ui.Bad, TextAnchor.UpperRight, true);
                Ui.Text(new Rect(row.x + 16f, row.y + 28f, row.width - 26f, row.height - 28f), p.detail, 14, Ui.Dim);
            }
            left.Space(6f);
            left.Text("STAND: " + au.StateText, 16, Ui.Gold, true);
            string reason;
            bool can = au.CanPetition(out reason);
            left.Text(can ? "Bereit: Reiche die Petition bei " + GameConstants.JudgeName + " in der Kanzlei ein." : "Noch nicht bereit: " + reason, 14, can ? Ui.Good : Ui.Dim);

            var right = new Col(new Rect(area.x + 670f, area.y, area.width - 670f, area.height));
            right.Text("GESETZBUCH (mind. " + GameConstants.PillarLaws + ", Freier Weg ist Pflicht)", 16, Ui.Gold, true);
            for (int i = 0; i < Autonomy.LawIds.Length; i++)
            {
                bool on = au.HasLaw(Autonomy.LawIds[i]);
                if (Ui.Button(right.Next(36f, 4f), (on ? "[x] " : "[  ] ") + Autonomy.LawNames[i], true, 14)) au.ToggleLaw(Autonomy.LawIds[i]);
            }
            right.Space(6f);
            right.Text("EINWOHNER VON EICHENHAIN", 16, Ui.Gold, true);
            var view = new Rect(right.x, right.y, right.w, area.yMax - right.y);
            var rl = World.Residents.list;
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, view.width - 20f, rl.Count * 48f));
            for (int i = 0; i < rl.Count; i++)
            {
                var r = rl[i];
                var row = new Rect(0f, i * 48f, view.width - 24f, 44f);
                Ui.Rect(row, Ui.PanelLight);
                Ui.Text(new Rect(row.x + 8f, row.y + 2f, 220f, 22f), r.name, 15, r.Respects ? Ui.Good : Ui.TextColor, TextAnchor.UpperLeft, true);
                Ui.Bar(new Rect(row.x + 8f, row.y + 28f, 200f, 8f), (r.respect + 100f) / 200f, r.Respects ? Ui.Good : new Color(0.9f, 0.7f, 0.3f));
                Ui.Text(new Rect(row.x + 214f, row.y + 20f, 60f, 22f), Mathf.RoundToInt(r.respect).ToString(), 13, Ui.Dim);
                string need = string.IsNullOrEmpty(r.needItem) ? "zufrieden" : "braucht " + r.needCount + "× " + Items.Name(r.needItem);
                Ui.Text(new Rect(row.x + 270f, row.y + 2f, row.width - 280f, 40f), need, 13, Ui.Dim);
            }
            GUI.EndScrollView();
        }

        // ---------- Aufträge und Geschichte ----------
        void DrawQuests()
        {
            var q = World.Quests;
            var area = Window(1240f, 860f, "AUFTRÄGE UND GESCHICHTE");
            float half = area.width * 0.55f;
            var left = new Rect(area.x, area.y, half - 20f, area.height);
            float w = left.width - 24f;
            float h = 40f;
            foreach (var s in q.active) h += 60f + s.def.steps.Length * 26f + Ui.TextHeight(s.def.summary ?? "", 14, w);
            scroll = GUI.BeginScrollView(left, scroll, new Rect(0f, 0f, w, Mathf.Max(h, left.height)));
            var col = new Col(new Rect(0f, 0f, w, h));
            col.Text("AKTIVE AUFTRÄGE", 17, Ui.Gold, true);
            if (q.active.Count == 0) col.Text("Keine aktiven Aufträge. Achte auf ! über den Köpfen der Leute und auf das Auftragsbrett im Gildenhaus.", 15, Ui.Dim);
            var list = new System.Collections.Generic.List<QuestState>(q.active);
            list.Sort((a, b) => (b.def.main ? 1 : 0) - (a.def.main ? 1 : 0));
            foreach (var s in list)
            {
                col.Text(s.def.title + (s.def.main ? "" : " — " + s.def.giver), 16, s.def.main ? Ui.Gold : Ui.TextColor, true);
                if (!string.IsNullOrEmpty(s.def.summary)) col.Text(s.def.summary, 14, Ui.Dim);
                for (int i = 0; i < s.def.steps.Length; i++)
                {
                    var st = s.def.steps[i];
                    string mark = i < s.step ? "[x] " : i == s.step ? "► " : "[  ] ";
                    string txt = i <= s.step ? st.text + (i == s.step ? s.ProgressText : "") : "???";
                    if (st.night && i == s.step) txt += " (nur nachts)";
                    col.Text(mark + txt, 14, i < s.step ? Ui.Good : i == s.step ? Ui.TextColor : Ui.Dim);
                }
                col.Space(8f);
            }
            GUI.EndScrollView();

            var right = new Col(new Rect(area.x + half, area.y, area.width - half, area.height));
            right.Text("DIE GESCHICHTE BISHER", 17, Ui.Gold, true);
            bool any = false;
            foreach (var def in QuestData.All)
            {
                if (!def.main || !q.IsDone(def.id)) continue;
                any = true;
                right.Text(def.title, 15, Ui.TextColor, true);
                right.Text(def.completeText, 14, Ui.Dim);
            }
            if (!any) right.Text("Noch kein Kapitel abgeschlossen.", 14, Ui.Dim);
            string hint = q.NextChapterHint;
            if (hint != null) right.Text("Nächstes Kapitel: " + hint, 14, Ui.Gold);
            right.Space(10f);
            int side = 0;
            foreach (var def in QuestData.All) if (!def.main && q.IsDone(def.id)) side++;
            right.Text("Erfüllte Nebenaufträge: " + side + " · Titel: " + (string.IsNullOrEmpty(q.title) ? "—" : q.title), 14, Ui.Dim);
            right.Text("Gildenaufträge gibt es täglich bei Runa im Gildenhaus östlich der Stadt (Art. 121). Jagdaufträge gibt es nur für Arten mit gesundem Bestand.", 14, Ui.Dim);
        }

        // ---------- Pause ----------
        void DrawPause()
        {
            var r = Ui.Centered(460f, 420f);
            var area = Ui.Panel(r, "MENÜ");
            var col = new Col(area);
            col.Text(GameConstants.Title + " — " + GameConstants.Motto, 15, Ui.Dim);
            if (Ui.Button(col.Next(52f, 10f), "Fortsetzen", true, 20)) Close();
            if (Ui.Button(col.Next(52f, 10f), "Speichern", true, 20)) { WorldBootstrap.SaveGame(); Notify.System("Spiel gespeichert."); }
            if (Ui.Button(col.Next(52f, 10f), "Speichern und zum Hauptmenü", true, 20))
            {
                WorldBootstrap.SaveGame();
                Time.timeScale = 1f;
                World.Ready = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameConstants.MainMenuScene);
            }
            if (Ui.Button(col.Next(52f, 10f), "Speichern und beenden", true, 20))
            {
                WorldBootstrap.SaveGame();
                Application.Quit();
            }
        }

        // ---------- Dialog ----------
        void DrawDialogue()
        {
            if (dialogue == null) return;
            var r = Ui.Centered(1080f, 760f);
            var area = Ui.Panel(r, dialogue.Title);
            if (Ui.Button(new Rect(r.xMax - 46f, r.y + 8f, 34f, 32f), "X")) { Close(); return; }
            dialogue.Draw(new Rect(area.x, area.y, area.width, area.height - 50f));
            if (Ui.Button(new Rect(area.xMax - 200f, area.yMax - 42f, 200f, 40f), "Schließen (Esc)", true, 16)) Close();
        }
    }
}
