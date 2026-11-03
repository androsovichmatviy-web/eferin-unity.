using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public class Hud : MonoBehaviour
    {
        struct Floater
        {
            public Vector3 world;
            public string text;
            public Color color;
            public float age;
            public bool big;
        }

        const float FloaterLife = 1.1f;
        const float BannerLife = 2.2f;

        readonly List<Floater> floaters = new List<Floater>();
        string bannerText;
        Color bannerColor;
        float bannerAge = BannerLife;
        float hitFlash;

        public void Floating(Vector3 world, string text, Color color, bool big)
        {
            world += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            floaters.Add(new Floater { world = world, text = text, color = color, big = big });
        }

        public void Banner(string text, Color color)
        {
            bannerText = text;
            bannerColor = color;
            bannerAge = 0f;
        }

        public void OnPlayerHit(DamageInfo info)
        {
            hitFlash = Mathf.Clamp01(0.25f + info.amount / Mathf.Max(1f, World.Player.health.maxHp));
            Floating(World.Player.health.Center + Vector3.up * 0.5f, "-" + Mathf.RoundToInt(info.amount), new Color(1f, 0.35f, 0.3f), false);
        }

        void Update()
        {
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                f.age += Time.unscaledDeltaTime;
                if (f.age >= FloaterLife) floaters.RemoveAt(i);
                else floaters[i] = f;
            }
            bannerAge += Time.unscaledDeltaTime;
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, Time.unscaledDeltaTime * 1.5f);
            Notify.Prune();
        }

        void OnGUI()
        {
            if (!World.Ready || World.Player == null || World.Rig == null) return;
            GUI.depth = 10;
            Ui.BeginScaled();
            float w = Ui.Width, h = Ui.Height;
            var pc = World.Player;
            var cam = World.Rig.Cam;

            if (hitFlash > 0f) Ui.Rect(new Rect(0f, 0f, w, h), new Color(0.6f, 0f, 0f, hitFlash * 0.35f));

            DrawTags(cam);
            DrawFloaters(cam);

            if (World.Ui != null && World.Ui.Open != WindowId.None && World.Ui.Open != WindowId.Dialogue) return;

            DrawStatus(pc);
            DrawInfo(w, pc);
            DrawQuests(w);
            DrawSkills(w, h, pc);
            DrawMessages(w);
            DrawCombatLog(h);
            DrawPrompt(w, h);
            DrawTarget(w);
            DrawBanner(w);

            if (World.Rig.FirstPerson || pc.Weapon.weaponKind != WeaponKind.Nahkampf)
            {
                Ui.Rect(new Rect(w * 0.5f - 1f, h * 0.5f - 9f, 2f, 18f), new Color(1f, 1f, 1f, 0.7f));
                Ui.Rect(new Rect(w * 0.5f - 9f, h * 0.5f - 1f, 18f, 2f), new Color(1f, 1f, 1f, 0.7f));
            }

            Ui.Text(new Rect(20f, h - 30f, w - 40f, 24f),
                "WASD · Shift Sprint · Leertaste Sprung · LMB Angriff · Q/E/R Fähigkeiten · 1/2/3 Tränke · F Interagieren · V Kamera · C Charakter · I Inventar · M Karte · L Aufträge · J Edikt · P Drei Säulen · Esc Menü",
                14, new Color(1f, 1f, 1f, 0.55f));
        }

        void DrawStatus(PlayerCharacter pc)
        {
            var r = new Rect(16f, 16f, 400f, 172f);
            Ui.Rect(r, Ui.PanelBg);
            Ui.Frame(r, new Color(Ui.Border.r, Ui.Border.g, Ui.Border.b, 0.5f), 1f);

            var badge = new Rect(r.x + 10f, r.y + 10f, 64f, 64f);
            Color rc = Ranks.Color(pc.rankIndex);
            Ui.Rect(badge, new Color(rc.r * 0.25f, rc.g * 0.25f, rc.b * 0.25f, 0.9f));
            Ui.Frame(badge, rc, 2f);
            Ui.Text(badge, Ranks.Name(pc.rankIndex), pc.rankIndex >= 7 ? 22 : 30, rc, TextAnchor.MiddleCenter, true);
            Ui.Text(new Rect(badge.x, badge.yMax, 64f, 18f), "Rang", 12, Ui.Dim, TextAnchor.UpperCenter);

            float x = r.x + 86f, bw = r.width - 98f;
            Ui.Text(new Rect(x, r.y + 6f, bw, 26f), pc.charName + "  ·  Stufe " + pc.level, 20, Ui.Gold, TextAnchor.MiddleLeft, true);
            Ui.Text(new Rect(x, r.y + 30f, bw, 20f), pc.race.name + " · " + pc.ClassTitle, 14, Ui.Dim, TextAnchor.MiddleLeft);

            Ui.Bar(new Rect(x, r.y + 56f, bw, 14f), pc.health.Fraction, new Color(0.85f, 0.25f, 0.25f));
            Ui.Text(new Rect(x, r.y + 54f, bw, 18f), "LP " + Mathf.CeilToInt(pc.health.hp) + " / " + Mathf.CeilToInt(pc.health.maxHp), 13, Color.white, TextAnchor.MiddleCenter);
            Ui.Bar(new Rect(x, r.y + 76f, bw, 12f), pc.aether / pc.MaxAether, new Color(0.3f, 0.6f, 1f));
            Ui.Text(new Rect(x, r.y + 73f, bw, 18f), "Äther " + Mathf.FloorToInt(pc.aether) + " / " + Mathf.FloorToInt(pc.MaxAether), 12, Color.white, TextAnchor.MiddleCenter);
            Ui.Bar(new Rect(x, r.y + 94f, bw, 6f), pc.stamina / pc.MaxStamina, new Color(1f, 0.85f, 0.4f));
            float need = PlayerCharacter.ExpToNext(pc.level);
            Ui.Bar(new Rect(x, r.y + 106f, bw, 6f), pc.level >= GameConstants.MaxLevel ? 1f : pc.exp / need, new Color(0.6f, 0.95f, 0.6f));

            string line = pc.taler + " Taler";
            if (pc.freePoints > 0) line += "  ·  " + pc.freePoints + " freie Punkte (C)";
            Ui.Text(new Rect(r.x + 12f, r.y + 118f, r.width - 24f, 22f), line, 15, Ui.Gold, TextAnchor.MiddleLeft);
            float load = pc.CarriedMass;
            Color lc = load > pc.CarryCapacity ? Ui.Bad : Ui.Dim;
            Ui.Text(new Rect(r.x + 12f, r.y + 140f, r.width - 24f, 22f), "Last " + load.ToString("0.0") + " / " + pc.CarryCapacity.ToString("0") + " kg" + (load > pc.CarryCapacity ? "  (überladen: langsamer)" : ""), 13, lc, TextAnchor.MiddleLeft);

            float by = r.yMax + 6f;
            foreach (var b in pc.buffs)
            {
                Ui.Text(new Rect(r.x + 4f, by, 400f, 20f), b.name + " " + Mathf.Max(0f, b.until - Time.time).ToString("0") + " s", 14, Ui.Good);
                by += 18f;
            }
            if (World.Law.Wanted > 0)
            {
                Ui.Text(new Rect(r.x + 4f, by + 4f, 420f, 24f), "GESUCHT (Stufe " + World.Law.Wanted + ") — [G] dem Gericht stellen", 17, Ui.Bad, TextAnchor.MiddleLeft, true);
                by += 26f;
            }
            if (pc.demo) Ui.Text(new Rect(r.x + 4f, by + 4f, 520f, 22f), "DEMO-MODUS: F1 +5 Stufen · F2 +5000 Taler · F3 +6 Std. · F4 Respekt +25 · F6 Siegel −30 %", 13, new Color(1f, 0.7f, 0.3f));
        }

        void DrawInfo(float w, PlayerCharacter pc)
        {
            var r = new Rect(w - 356f, 16f, 340f, 112f);
            Ui.Rect(r, Ui.PanelBg);
            Ui.Frame(r, new Color(Ui.Border.r, Ui.Border.g, Ui.Border.b, 0.5f), 1f);
            var region = WorldLayout.RegionAt(pc.transform.position);
            string where = WorldLayout.RegionName(region);
            if (region == Region.Eichenhain && World.Autonomy.IsAutonomous) where = "Autonomie „" + World.Autonomy.autonomyName + "“";
            Ui.Text(new Rect(r.x + 12f, r.y + 6f, r.width - 24f, 24f), where, 16, Ui.Gold, TextAnchor.MiddleLeft, true);
            Ui.Text(new Rect(r.x + 12f, r.y + 30f, r.width - 24f, 22f), World.Clock.Stamp() + (World.Clock.IsNight ? " · Nacht" : "") + " · " + Weather.Name(World.Weather.Current), 15, Ui.TextColor);
            Ui.Text(new Rect(r.x + 12f, r.y + 52f, r.width - 24f, 22f), "Siegel des Risses: " + Mathf.RoundToInt(World.Rift.Strength) + " %" + (World.Rift.Summons ? " · GROSSER AUFRUF" : ""), 14, World.Rift.Strength < 30f ? Ui.Bad : Ui.Dim);
            Ui.Text(new Rect(r.x + 12f, r.y + 74f, r.width - 24f, 22f), "Respekt in Eichenhain: " + World.Residents.RespectingCount + " / " + World.Residents.list.Count, 14, Ui.Dim);
            if (region == Region.VerboteneLande && !pc.permit)
                Ui.Text(new Rect(r.x - 300f, r.yMax + 4f, r.width + 300f, 40f), "Ohne Kaiserliche Erlaubnis! Die Ordensmagier greifen an (Art. 112).", 15, Ui.Bad, TextAnchor.UpperLeft, true);
        }

        // Aufgabenverfolger rechts oben: aktuelle Schritte der Geschichte und der Aufträge.
        void DrawQuests(float w)
        {
            var q = World.Quests;
            if (q == null) return;
            float x = w - 356f, y = 168f, width = 340f;
            int shown = 0;
            var list = new List<QuestState>(q.active);
            list.Sort((a, b) => (b.def.main ? 1 : 0) - (a.def.main ? 1 : 0));
            foreach (var s in list)
            {
                if (shown >= 4 || s.Step == null) break;
                string head = s.def.title;
                string step = "• " + s.Step.text + s.ProgressText + (s.Step.night && !World.Clock.IsNight ? " — warte auf die Nacht" : "");
                float h = Ui.TextHeight(head, 15, width - 20f) + Ui.TextHeight(step, 14, width - 20f) + 14f;
                var r = new Rect(x, y, width, h);
                Ui.Rect(r, new Color(0.02f, 0.06f, 0.12f, 0.7f));
                Ui.Rect(new Rect(r.x, r.y, 4f, r.height), s.def.main ? Ui.Gold : Ui.Border);
                float th = Ui.TextHeight(head, 15, width - 20f);
                Ui.Text(new Rect(r.x + 12f, r.y + 4f, width - 20f, th), head, 15, s.def.main ? Ui.Gold : Ui.TextColor, TextAnchor.UpperLeft, true);
                Ui.Text(new Rect(r.x + 12f, r.y + 6f + th, width - 20f, h - th), step, 14, Ui.Dim);
                y += h + 6f;
                shown++;
            }
            string hint = q.NextChapterHint;
            if (hint != null && !q.active.Exists(s => s.def.main))
                Ui.Text(new Rect(x, y, width, 40f), "Nächstes Kapitel: " + hint, 13, Ui.Dim);
        }

        void DrawSkills(float w, float h, PlayerCharacter pc)
        {
            var combat = pc.combat;
            string[] keys = { "Q", "E", "R" };
            float size = 72f, gap = 10f;
            float total = 3 * size + 2 * gap + 3 * 56f + 3 * gap + 20f;
            float x = (w - total) * 0.5f, y = h - 120f;
            for (int i = 0; i < 3; i++)
            {
                var a = combat.Slot(i);
                var r = new Rect(x + i * (size + gap), y, size, size);
                Ui.Rect(r, Ui.PanelBg);
                Ui.Frame(r, a != null ? Ui.Border : new Color(0.3f, 0.3f, 0.3f, 0.6f), 1.5f);
                if (a == null)
                {
                    Ui.Text(r, "R\nab Stufe 50", 12, Ui.Dim, TextAnchor.MiddleCenter);
                    continue;
                }
                float cd = combat.CooldownLeft(i);
                if (cd > 0f)
                {
                    float f = cd / a.cooldown;
                    Ui.Rect(new Rect(r.x, r.y + size * (1f - f), size, size * f), new Color(0f, 0f, 0f, 0.6f));
                    Ui.Text(r, cd.ToString("0.0"), 22, Color.white, TextAnchor.MiddleCenter, true);
                }
                else Ui.Text(new Rect(r.x, r.y + 4f, size, 30f), keys[i], 24, a.cost <= pc.aether ? Ui.Gold : Ui.Bad, TextAnchor.MiddleCenter, true);
                Ui.Text(new Rect(r.x - 16f, r.yMax + 2f, size + 32f, 20f), a.name, 12, Ui.TextColor, TextAnchor.UpperCenter);
                if (a.cost > 0f) Ui.Text(new Rect(r.x, r.y + size - 22f, size - 4f, 20f), Mathf.RoundToInt(a.cost).ToString(), 12, new Color(0.5f, 0.75f, 1f), TextAnchor.LowerRight);
            }
            string[] potions = { "heiltrank", "aetherelixier", "gebratenes_fleisch" };
            float px = x + 3 * (size + gap) + 20f;
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(px + i * (56f + gap), y + 16f, 56f, 56f);
                Ui.Rect(r, Ui.PanelBg);
                Ui.Frame(r, new Color(0.6f, 0.6f, 0.6f, 0.5f), 1f);
                Ui.Text(new Rect(r.x + 4f, r.y + 2f, 20f, 20f), (i + 1).ToString(), 14, Ui.Gold, TextAnchor.UpperLeft, true);
                Ui.Text(r, Items.Name(potions[i]).Split(' ')[0], 11, Ui.TextColor, TextAnchor.MiddleCenter);
                Ui.Text(new Rect(r.x, r.y, r.width - 4f, r.height - 2f), "×" + pc.inv.Count(potions[i]), 14, Color.white, TextAnchor.LowerRight, true);
            }
        }

        void DrawMessages(float w)
        {
            float y = 140f;
            float mw = 640f;
            foreach (var m in Notify.Active)
            {
                float age = Time.unscaledTime - m.time;
                float a = age > Notify.MessageLife - 1f ? Notify.MessageLife - age : 1f;
                Color border = m.kind == MessageKind.Announcement ? Ui.Gold : m.kind == MessageKind.Law ? Ui.Bad : Ui.Border;
                float th = Ui.TextHeight(m.text, 16, mw - 24f) + 14f;
                var r = new Rect((w - mw) * 0.5f, y, mw, th);
                Ui.Rect(r, new Color(0.02f, 0.06f, 0.12f, 0.82f * a));
                Ui.Frame(r, new Color(border.r, border.g, border.b, a), 1.5f);
                Ui.Text(new Rect(r.x + 12f, r.y + 6f, r.width - 24f, r.height), m.text, 16, new Color(1f, 1f, 1f, a));
                y += th + 6f;
            }
        }

        void DrawCombatLog(float h)
        {
            float y = h - 70f - Notify.CombatLog.Count * 20f;
            Ui.Text(new Rect(16f, y - 22f, 600f, 20f), "Physik-Protokoll (F = m · a)", 13, Ui.Gold);
            foreach (var line in Notify.CombatLog)
            {
                Ui.Text(new Rect(16f, y, 760f, 20f), line, 13, new Color(0.85f, 0.92f, 1f, 0.85f));
                y += 20f;
            }
        }

        void DrawPrompt(float w, float h)
        {
            var it = World.Player.GetComponent<PlayerInteractor>().Current;
            if (it == null) return;
            string text = "[F]  " + it.Prompt;
            var r = new Rect((w - 520f) * 0.5f, h * 0.5f + 70f, 520f, 36f);
            Ui.Rect(r, new Color(0.02f, 0.06f, 0.12f, 0.8f));
            Ui.Frame(r, Ui.Border, 1f);
            Ui.Text(r, text, 18, it.CanInteract ? Ui.TextColor : Ui.Dim, TextAnchor.MiddleCenter, true);
        }

        void DrawTarget(float w)
        {
            var t = World.Rig.AimTarget(45f);
            if (t == null || t.faction == Faction.Player) return;
            float bw = 420f, x = (w - bw) * 0.5f;
            Ui.Text(new Rect(x, 20f, bw, 24f), t.displayName, 18, t.faction == Faction.Ally ? Ui.Good : new Color(1f, 0.85f, 0.8f), TextAnchor.MiddleCenter, true);
            if (!t.invulnerable) Ui.Bar(new Rect(x, 48f, bw, 10f), t.Fraction, new Color(0.89f, 0.36f, 0.36f));
            var c = t.GetComponent<Creature>();
            if (c != null && c.def.ecosystem && !c.managed)
            {
                string info = "Masse " + c.def.mass + " kg · Bestand " + World.Eco.Count(c.def.id) + " / " + c.def.capacity + (c.IsJuvenile ? " · Jungtier" : "");
                if (c.def.diet == Diet.Fleischfresser) info += " · Hunger " + Mathf.RoundToInt(c.hunger * 100f) + " %";
                Ui.Text(new Rect(x - 40f, 60f, bw + 80f, 20f), info, 13, Ui.Dim, TextAnchor.MiddleCenter);
            }
        }

        void DrawBanner(float w)
        {
            if (bannerAge >= BannerLife || string.IsNullOrEmpty(bannerText)) return;
            float t = bannerAge / BannerLife;
            Color c = bannerColor;
            c.a = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
            Ui.Text(new Rect(0f, 300f, w, 70f), bannerText, 48, c, TextAnchor.MiddleCenter, true);
        }

        void DrawTags(Camera cam)
        {
            Vector3 camPos = cam.transform.position;
            foreach (var hp in Health.All)
            {
                if (hp == null || hp.IsDead || hp.faction == Faction.Player) continue;
                float d = (hp.transform.position - camPos).magnitude;
                bool npc = hp.GetComponent<Npc>() != null;
                if (d > (npc ? 14f : 28f)) continue;
                if (!npc && hp.Fraction >= 1f && hp.faction != Faction.Void && hp.faction != Faction.Construct) continue;
                Vector3 top = hp.transform.position + Vector3.up * (hp.height + 0.5f);
                Vector2 p;
                if (!Ui.WorldToGui(cam, top, out p)) continue;
                Color nc = hp.faction == Faction.Ally ? Ui.Good : hp.faction == Faction.Imperial ? Ui.Gold : npc ? Ui.TextColor : new Color(1f, 0.85f, 0.85f);
                Ui.Text(new Rect(p.x - 110f, p.y - 30f, 220f, 20f), hp.displayName, 13, nc, TextAnchor.MiddleCenter);
                var n = hp.GetComponent<Npc>();
                if (n != null && World.Quests != null)
                {
                    // ! = neuer Auftrag, ? = hier geht ein Auftrag weiter
                    string mark = World.Quests.HasTurnIn(n.npcName) ? "?" : World.Quests.HasOffer(n.npcName) ? "!" : null;
                    if (mark != null) Ui.Text(new Rect(p.x - 30f, p.y - 72f, 60f, 44f), mark, 36, Ui.Gold, TextAnchor.MiddleCenter, true);
                }
                if (!npc || hp.Fraction < 1f)
                    if (!hp.invulnerable) Ui.Bar(new Rect(p.x - 40f, p.y - 10f, 80f, 6f), hp.Fraction, new Color(0.89f, 0.36f, 0.36f));
            }
        }

        void DrawFloaters(Camera cam)
        {
            foreach (var f in floaters)
            {
                float t = f.age / FloaterLife;
                Vector2 p;
                if (!Ui.WorldToGui(cam, f.world + Vector3.up * t * 1.2f, out p)) continue;
                Color c = f.color;
                c.a = 1f - t * t;
                int size = f.big ? 30 : 20;
                Ui.Text(new Rect(p.x - 80f, p.y - 20f, 160f, 40f), f.text, size, c, TextAnchor.MiddleCenter, true);
            }
        }
    }
}
