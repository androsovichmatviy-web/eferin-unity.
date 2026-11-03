using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum PetitionState { Keine = 0, Revision = 1, Audienz = 2, Vertrag = 3 }

    public struct PillarCheck
    {
        public bool ok;
        public string title;
        public string detail;
    }

    // Abschnitt X des Edikts — der Weg der Diplomatie.
    // Prätendent: Stufe ≥ Stufe des Kaisers, ehrenhaft, und die Drei Säulen der Souveränität stehen.
    public class Autonomy : MonoBehaviour
    {
        public int guards;
        public bool townHall;
        public readonly List<string> laws = new List<string>();
        public PetitionState state;
        public int revisionEndDay;
        public int audienceDay;
        public int audienceHour;
        public int rejectedDay = -1000;
        public string rejectReason = "";
        public string autonomyName = "";
        public int color;
        public int warningUntil = -1;

        readonly List<Npc> guardNpcs = new List<Npc>();

        public static readonly string[] LawIds = { "freier_weg", "jagdschutz", "marktfrieden", "wachpflicht", "siegelschutz" };
        public static readonly string[] LawNames =
        {
            "Recht des Freien Weges (Pflicht laut Art. 33 und 136)",
            "Jagdschutz: Jungtiere sind geschützt (Nachwuchs +20 %)",
            "Marktfrieden: Handelsabgabe 5 % in der Autonomie",
            "Wachpflicht: die Dorfwache patrouilliert auch nachts",
            "Schutz der Siegel: Eichenhain unterstützt den Orden",
        };
        public static readonly Color[] Colors =
        {
            new Color(0.75f, 0.15f, 0.15f), new Color(0.15f, 0.55f, 0.25f), new Color(0.85f, 0.65f, 0.1f),
            new Color(0.45f, 0.2f, 0.65f), new Color(0.1f, 0.5f, 0.6f), new Color(0.2f, 0.2f, 0.22f),
        };

        public bool IsAutonomous { get { return state == PetitionState.Vertrag; } }
        public bool HasLaw(string id) { return laws.Contains(id); }

        public void Init(SaveData d, WorldClock clock)
        {
            guards = d.guardsHired;
            townHall = d.townHallBuilt;
            laws.Clear();
            laws.AddRange(d.laws);
            state = (PetitionState)d.petitionState;
            revisionEndDay = d.revisionEndDay;
            audienceDay = d.audienceDay;
            audienceHour = d.audienceHour;
            rejectedDay = d.petitionRejectedDay;
            rejectReason = d.petitionRejectReason;
            autonomyName = d.autonomyName;
            color = d.autonomyColor;
            warningUntil = d.treatyWarningUntil;
            if (townHall) WorldBuilder.BuildTownHall();
            for (int i = 0; i < guards; i++) SpawnGuard(i);
            if (IsAutonomous) RaiseBanners();
            clock.DayStarted += OnDay;
        }

        public void Save(SaveData d)
        {
            d.guardsHired = guards;
            d.townHallBuilt = townHall;
            d.laws = new List<string>(laws);
            d.petitionState = (int)state;
            d.revisionEndDay = revisionEndDay;
            d.audienceDay = audienceDay;
            d.audienceHour = audienceHour;
            d.petitionRejectedDay = rejectedDay;
            d.petitionRejectReason = rejectReason;
            d.autonomyName = autonomyName;
            d.autonomyColor = color;
            d.treatyWarningUntil = warningUntil;
        }

        // ---------- Die Säulen ----------
        public PillarCheck Strength()
        {
            var pc = World.Player;
            return new PillarCheck
            {
                ok = pc.level >= GameConstants.EmperorLevel,
                title = "Würdig an Kraft (Art. 129 Nr. 3)",
                detail = "Stufe " + pc.level + " / " + GameConstants.EmperorLevel + " — die Stufe von Kaiser " + GameConstants.EmperorName,
            };
        }

        public PillarCheck Honor()
        {
            // Gesühnte Taten zählen nicht noch einmal (Art. 58). Großer Verrat verwirkt das Recht für immer.
            var law = World.Law;
            bool ok = !law.GreatTreason && law.Wanted == 0;
            string d = law.GreatTreason ? "Großer Verrat — das Recht ist für immer verwirkt."
                : law.Wanted > 0 ? "Du wirst gesucht. Stelle dich zuerst dem Gericht."
                : "Kein Großer Verrat, keine offene Schuld. Einträge im Register: " + law.record.Count + ".";
            return new PillarCheck { ok = ok, title = "Würdig an Seele (Art. 129 Nr. 2)", detail = d };
        }

        int Owned(string id) { return World.Player.inv.Count(id) + World.Player.storage.Count(id); }

        public PillarCheck Resources()
        {
            var pc = World.Player;
            int food = Owned("fleisch") + Owned("gebratenes_fleisch");
            int wood = Owned("holz");
            int iron = Owned("eisenbarren");
            float deer = World.Eco.StockFraction("graslaeufer");
            bool ok = pc.taler >= GameConstants.PillarTaler && food >= GameConstants.PillarFood && wood >= GameConstants.PillarWood
                      && iron >= GameConstants.PillarIron && deer >= GameConstants.PillarGameStock;
            string d = "Kasse " + pc.taler + "/" + GameConstants.PillarTaler + " Taler · Nahrung " + food + "/" + GameConstants.PillarFood
                       + " · Holz " + wood + "/" + GameConstants.PillarWood + " · Eisenbarren " + iron + "/" + GameConstants.PillarIron
                       + " · Wildbestand " + Mathf.RoundToInt(deer * 100f) + " % (mind. " + Mathf.RoundToInt(GameConstants.PillarGameStock * 100f) + " %)";
            return new PillarCheck { ok = ok, title = "Säule der Ressourcen (Art. 131)", detail = d };
        }

        public PillarCheck People()
        {
            var r = World.Residents;
            return new PillarCheck
            {
                ok = r.AllRespect,
                title = "Säule des Volkes (Art. 132)",
                detail = r.RespectingCount + " / " + r.list.Count + " Einwohner respektieren dich (alle müssen es, Respekt ≥ " + GameConstants.RespectThreshold + ")",
            };
        }

        public PillarCheck Sovereignty()
        {
            bool lawsOk = laws.Count >= GameConstants.PillarLaws && HasLaw("freier_weg");
            bool ok = guards >= GameConstants.PillarGuards && lawsOk && townHall;
            string d = "Wachen " + guards + "/" + GameConstants.PillarGuards + " · Gesetzbuch " + laws.Count + "/" + GameConstants.PillarLaws
                       + (HasLaw("freier_weg") ? "" : " (Freier Weg fehlt)") + " · Rathaus " + (townHall ? "errichtet" : "fehlt");
            return new PillarCheck { ok = ok, title = "Säule der Souveränität (Art. 133)", detail = d };
        }

        public List<PillarCheck> All()
        {
            return new List<PillarCheck> { Strength(), Honor(), Resources(), People(), Sovereignty() };
        }

        string FirstFailure(bool includeStrength)
        {
            foreach (var p in All())
            {
                if (!includeStrength && p.title.StartsWith("Würdig an Kraft")) continue;
                if (!p.ok) return p.title + ": " + p.detail;
            }
            return null;
        }

        public bool CanPetition(out string reason)
        {
            reason = null;
            if (state == PetitionState.Revision) { reason = "Die Revision läuft bereits."; return false; }
            if (state == PetitionState.Audienz) { reason = "Deine Audienz ist bereits bestimmt."; return false; }
            if (state == PetitionState.Vertrag) { reason = "Du bist bereits Herr der Autonomie " + autonomyName + "."; return false; }
            reason = FirstFailure(true);
            if (reason != null) return false;
            if (World.Player.taler < GameConstants.PetitionFee + GameConstants.PillarTaler) { reason = "Die Petitionsgebühr (" + GameConstants.PetitionFee + " Taler) darf die Säule der Ressourcen nicht unterschreiten."; return false; }
            return true;
        }

        public void Submit()
        {
            string reason;
            if (!CanPetition(out reason)) { Notify.System(reason); return; }
            World.Player.TrySpend(GameConstants.PetitionFee);
            World.Treasury.Add("Petitionsgebühren", GameConstants.PetitionFee);
            state = PetitionState.Revision;
            revisionEndDay = World.Day + GameConstants.RevisionDays;
            Notify.Announce(World.Player.charName + " reicht eine Petition des Aufstiegs für Eichenhain ein. Die Arbiter des Throns beginnen die Revision (Art. 134).");
        }

        public bool AudienceOpen
        {
            get
            {
                return state == PetitionState.Audienz && World.Day == audienceDay && World.Clock.Hour >= audienceHour && World.Clock.Hour < audienceHour + 2;
            }
        }

        // Art. 135: Eine Ablehnung wird immer mit Gründen verkündet.
        public void RejectByEmperor(string reason)
        {
            Reject("Der Kaiser verweigert den Vertrag: " + reason);
        }

        void Reject(string reason)
        {
            state = PetitionState.Keine;
            rejectedDay = World.Day;
            rejectReason = reason;
            Notify.System("Petition abgelehnt (Art. 139): " + reason + " Eine neue Petition ist möglich, sobald der Grund behoben ist.");
        }

        void OnDay(int day)
        {
            PayGuards();

            if (state == PetitionState.Revision)
            {
                string fail = FirstFailure(true);
                if (fail != null) { Reject("Bei der Revision fiel eine Säule: " + fail); return; }
                if (day >= revisionEndDay)
                {
                    state = PetitionState.Audienz;
                    audienceDay = day + 1;
                    audienceHour = 12;
                    Notify.System(GameConstants.JudgeName + " bestätigt die Drei Säulen. Deine Audienz bei Kaiser " + GameConstants.EmperorName + ": Tag " + audienceDay + ", 12:00–14:00 Uhr, im Thronsaal. Wer nicht erscheint, verliert die Petition (Art. 135).");
                }
            }
            else if (state == PetitionState.Audienz && day > audienceDay)
            {
                Reject("Du bist nicht zur Audienz erschienen (Art. 135).");
            }
            else if (state == PetitionState.Vertrag)
            {
                string fail = FirstFailure(false);
                if (fail == null)
                {
                    if (warningUntil >= 0) Notify.System("Die Säulen stehen wieder. Die Vorwarnung des Throns ist aufgehoben.");
                    warningUntil = -1;
                    int income = World.Residents.list.Count * 12;
                    int share = Mathf.RoundToInt(income * 0.1f);
                    World.Player.AddTaler(income - share);
                    World.Treasury.Add("Anteil der Autonomien", share);
                }
                else if (warningUntil < 0)
                {
                    warningUntil = day + GameConstants.TreatyWarningDays;
                    Notify.Announce("Vorwarnung des Throns an " + autonomyName + " (Art. 140): " + fail + " Frist bis Tag " + warningUntil + ".");
                }
                else if (day >= warningUntil)
                {
                    state = PetitionState.Keine;
                    warningUntil = -1;
                    Notify.Announce("Der Vertrag mit " + autonomyName + " ist aufgelöst (Art. 140 Nr. 4: eine Säule ist gefallen). Die Lande kehren in die Kronendomäne zurück.");
                    autonomyName = "";
                }
            }
        }

        void PayGuards()
        {
            if (guards == 0) return;
            int cost = guards * GameConstants.GuardUpkeepPerDay;
            if (World.Player.TrySpend(cost)) return;
            guards--;
            if (guardNpcs.Count > 0)
            {
                var g = guardNpcs[guardNpcs.Count - 1];
                guardNpcs.RemoveAt(guardNpcs.Count - 1);
                if (g != null) Destroy(g.gameObject);
            }
            Notify.System("Eine Dorfwache hat Eichenhain verlassen — ihr Sold (" + GameConstants.GuardUpkeepPerDay + " Taler/Tag) wurde nicht bezahlt.");
        }

        public void HireGuard()
        {
            if (guards >= GameConstants.MaxGuards) { Notify.System("Mehr Wachen braucht Eichenhain nicht."); return; }
            if (!World.Player.TrySpend(GameConstants.GuardHireCost)) { Notify.System("Nicht genug Taler (" + GameConstants.GuardHireCost + ")."); return; }
            SpawnGuard(guards);
            guards++;
            Notify.System("Dorfwache angeworben (" + guards + "/" + GameConstants.PillarGuards + "). Sold: " + GameConstants.GuardUpkeepPerDay + " Taler pro Tag.");
        }

        void SpawnGuard(int i)
        {
            Color c = IsAutonomous ? Colors[Mathf.Clamp(color, 0, Colors.Length - 1)] : new Color(0.35f, 0.4f, 0.3f);
            float a = i * 60f * Mathf.Deg2Rad;
            Vector3 p = Places.VillageCenter + new Vector3(Mathf.Cos(a) * 11f, 0f, Mathf.Sin(a) * 11f);
            var g = Npc.Spawn(NpcRole.Dorfwache, "Dorfwache " + (i + 1), "im Dienst von " + World.Player.charName, p, a * Mathf.Rad2Deg, c, new Color(0.3f, 0.3f, 0.32f));
            g.route = new List<Vector3>();
            for (int k = 0; k < 4; k++)
            {
                float b = a + k * Mathf.PI * 0.5f;
                g.route.Add(Places.VillageCenter + new Vector3(Mathf.Cos(b) * 18f, 0f, Mathf.Sin(b) * 18f));
            }
            guardNpcs.Add(g);
        }

        public void ToggleLaw(string id)
        {
            if (laws.Contains(id)) laws.Remove(id);
            else laws.Add(id);
        }

        public void BuildTownHall()
        {
            var pc = World.Player;
            if (townHall) return;
            if (pc.inv.Count("holz") < 30 || pc.inv.Count("stein") < 20 || pc.inv.Count("eisenbarren") < 10)
            {
                Notify.System("Für das Rathaus brauchst du 30 Holz, 20 Stein und 10 Eisenbarren im Inventar.");
                return;
            }
            pc.inv.Remove("holz", 30);
            pc.inv.Remove("stein", 20);
            pc.inv.Remove("eisenbarren", 10);
            townHall = true;
            WorldBuilder.BuildTownHall();
            World.Residents.ChangeAll(5f);
            Notify.System("Das Rathaus von Eichenhain steht. Die Einwohner sind beeindruckt (+5 Respekt).");
        }

        // Art. 135: Prüfung des Blicks — der Kaiser wägt Willen und Ehre des Prätendenten.
        public string GazeTrial()
        {
            string fail = FirstFailure(true);
            if (fail != null) return fail;
            if (World.Day - World.Law.LastCrimeDay < 7) return "Der Kaiser sieht frisches Unrecht an deinen Händen (ein Vergehen in den letzten 7 Tagen).";
            if (World.Residents.AverageRespect < 60f) return "Der Kaiser sieht Zweifel in deinem Volk (durchschnittlicher Respekt unter 60).";
            return null;
        }

        public void Sign(string name, int colorIndex)
        {
            autonomyName = string.IsNullOrEmpty(name) ? "Autonomie Eichenhain" : name.Trim();
            color = Mathf.Clamp(colorIndex, 0, Colors.Length - 1);
            state = PetitionState.Vertrag;
            warningUntil = -1;
            RaiseBanners();
            foreach (var g in guardNpcs) if (g != null) Destroy(g.gameObject);
            guardNpcs.Clear();
            for (int i = 0; i < guards; i++) SpawnGuard(i);
            Notify.Announce("Der Vertrag ist mit dem Großen Siegel der Genesis besiegelt (Art. 136). " + World.Player.charName + " ist Herr der Autonomie „" + autonomyName + "“. Der Kontinent bleibt eins.");
            if (World.Hud != null) World.Hud.Banner(autonomyName.ToUpper(), Colors[color]);
            WorldBootstrap.SaveGame();
        }

        void RaiseBanners()
        {
            var v = Places.VillageRoot;
            if (v == null) return;
            var old = v.Find("Banner der Autonomie");
            if (old != null) Destroy(old.gameObject);
            var root = new GameObject("Banner der Autonomie").transform;
            root.SetParent(v, false);
            Color c = Colors[Mathf.Clamp(color, 0, Colors.Length - 1)];
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector3 p = Places.VillageCenter + new Vector3(Mathf.Cos(a) * 21f, 0f, Mathf.Sin(a) * 21f);
                Build.Banner(root, p, -i * 90f, 6f, c);
            }
        }

        public string StateText
        {
            get
            {
                switch (state)
                {
                    case PetitionState.Revision: return "Revision durch die Arbiter läuft bis Tag " + revisionEndDay + ".";
                    case PetitionState.Audienz: return "Audienz beim Kaiser: Tag " + audienceDay + ", " + audienceHour + ":00–" + (audienceHour + 2) + ":00 Uhr im Thronsaal.";
                    case PetitionState.Vertrag: return "Autonomie „" + autonomyName + "“ besiegelt." + (warningUntil >= 0 ? " VORWARNUNG bis Tag " + warningUntil + "!" : "");
                    default: return World.Day - rejectedDay < 30 && !string.IsNullOrEmpty(rejectReason) ? "Letzte Petition abgelehnt: " + rejectReason : "Keine Petition eingereicht.";
                }
            }
        }
    }
}
