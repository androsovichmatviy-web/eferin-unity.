using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public class QuestState
    {
        public QuestDef def;
        public int step;
        public int progress;

        public StepDef Step { get { return step < def.steps.Length ? def.steps[step] : null; } }

        public string ProgressText
        {
            get
            {
                var s = Step;
                if (s == null) return "";
                switch (s.type)
                {
                    case StepType.Kill:
                    case StepType.Investigate:
                        return " (" + progress + "/" + s.count + ")";
                    case StepType.Deliver:
                        return " (" + (World.Player != null ? World.Player.inv.Count(s.target) : 0) + "/" + s.count + ")";
                    case StepType.Population:
                        return " (" + (World.Eco != null ? World.Eco.Count(s.target) : 0) + "/" + s.count + ")";
                    case StepType.Seal:
                        return " (" + Mathf.RoundToInt(World.Rift != null ? World.Rift.Strength : 0f) + "/" + s.count + " %)";
                    default:
                        return "";
                }
            }
        }
    }

    // Spur, die man mit [F] untersucht.
    public class Clue : Interactable
    {
        public string clueId;
        public string label;
        public string finding;

        public override string Prompt { get { return "Untersuchen: " + label; } }

        public override void Interact(PlayerCharacter player)
        {
            if (World.Quests != null) World.Quests.OnInvestigate(this);
        }
    }

    // Aufträge: Hauptgeschichte (startet von selbst), Nebenaufträge von NPCs und tägliche Gildenaufträge (Art. 121).
    public class Quests : MonoBehaviour
    {
        public const int MaxContracts = 3;

        public readonly List<QuestState> active = new List<QuestState>();
        public readonly List<string> done = new List<string>();
        public readonly List<QuestDef> offeredContracts = new List<QuestDef>();
        public string title = "";

        readonly Dictionary<string, ContractSave> contractInfo = new Dictionary<string, ContractSave>();
        readonly Dictionary<string, QuestDef> contractDefs = new Dictionary<string, QuestDef>();
        readonly Dictionary<string, List<GameObject>> spawned = new Dictionary<string, List<GameObject>>();
        int contractDay;
        int contractCounter;
        float pollTimer;
        float autoTimer;

        // ---------- Laden ----------
        public void Init(SaveData d, WorldClock clock)
        {
            done.Clear();
            done.AddRange(d.questsDone);
            title = d.title;
            contractDay = d.contractDay;
            contractCounter = d.contractCounter;
            foreach (var c in d.contracts)
            {
                var def = BuildContract(c);
                contractInfo[c.id] = c;
                contractDefs[c.id] = def;
            }
            foreach (var qs in d.quests)
            {
                var def = Find(qs.id);
                if (def == null || qs.step >= def.steps.Length) continue;
                var s = new QuestState { def = def, step = qs.step, progress = qs.progress };
                active.Add(s);
            }
            foreach (var c in d.contracts)
                if (c.offered && !IsActive(c.id) && !done.Contains(c.id)) offeredContracts.Add(contractDefs[c.id]);

            foreach (var s in active.ToArray()) BeginStep(s, true);
            if (contractDay != World.Day || (offeredContracts.Count == 0 && ActiveContracts == 0)) RefreshContracts(World.Day);
            clock.DayStarted += RefreshContracts;
            CheckAutoStart();
        }

        public void Save(SaveData d)
        {
            d.quests.Clear();
            foreach (var s in active) d.quests.Add(new QuestSave { id = s.def.id, step = s.step, progress = s.progress });
            d.questsDone = new List<string>(done);
            d.title = title;
            d.contractDay = contractDay;
            d.contractCounter = contractCounter;
            d.contracts.Clear();
            foreach (var kv in contractInfo)
            {
                bool relevant = IsActive(kv.Key) || offeredContracts.Exists(q => q.id == kv.Key);
                if (!relevant) continue;
                var c = kv.Value;
                c.offered = !IsActive(kv.Key);
                d.contracts.Add(c);
            }
        }

        public QuestDef Find(string id)
        {
            var q = QuestData.Get(id);
            if (q != null) return q;
            QuestDef c;
            return contractDefs.TryGetValue(id, out c) ? c : null;
        }

        public bool IsActive(string id)
        {
            foreach (var s in active) if (s.def.id == id) return true;
            return false;
        }

        public bool IsDone(string id) { return done.Contains(id); }

        int ActiveContracts
        {
            get
            {
                int n = 0;
                foreach (var s in active) if (s.def.contract) n++;
                return n;
            }
        }

        bool Available(QuestDef q)
        {
            if (q.contract || IsActive(q.id) || done.Contains(q.id)) return false;
            if (World.Player.level < q.minLevel) return false;
            return string.IsNullOrEmpty(q.requires) || done.Contains(q.requires);
        }

        // Nächstes Kapitel, das noch auf eine Stufe wartet (für den Hinweis im HUD).
        public string NextChapterHint
        {
            get
            {
                foreach (var q in QuestData.All)
                {
                    if (!q.main || done.Contains(q.id) || IsActive(q.id)) continue;
                    if (!string.IsNullOrEmpty(q.requires) && !done.Contains(q.requires)) return null;
                    return q.title + " — ab Stufe " + q.minLevel;
                }
                return null;
            }
        }

        // ---------- Annehmen und Fortschritt ----------
        void CheckAutoStart()
        {
            foreach (var q in QuestData.All)
                if (q.main && Available(q)) Accept(q);
        }

        public List<QuestDef> OffersFrom(string npc)
        {
            var list = new List<QuestDef>();
            foreach (var q in QuestData.All)
                if (!q.main && q.giver == npc && Available(q)) list.Add(q);
            return list;
        }

        public bool HasOffer(string npc)
        {
            if (npc == "Runa" && offeredContracts.Count > 0 && ActiveContracts < MaxContracts) return true;
            foreach (var q in QuestData.All)
                if (!q.main && q.giver == npc && Available(q)) return true;
            return false;
        }

        public bool HasTurnIn(string npc)
        {
            foreach (var s in active)
            {
                var st = s.Step;
                if (st == null) continue;
                if (st.type == StepType.Talk && st.target == npc) return true;
                if (st.type == StepType.Deliver && st.npc == npc) return true;
            }
            return false;
        }

        public List<QuestState> TalkSteps(string npc)
        {
            var list = new List<QuestState>();
            foreach (var s in active)
                if (s.Step != null && s.Step.type == StepType.Talk && s.Step.target == npc) list.Add(s);
            return list;
        }

        public List<QuestState> DeliverSteps(string npc)
        {
            var list = new List<QuestState>();
            foreach (var s in active)
                if (s.Step != null && s.Step.type == StepType.Deliver && s.Step.npc == npc) list.Add(s);
            return list;
        }

        public void Accept(QuestDef q)
        {
            if (q.contract)
            {
                if (ActiveContracts >= MaxContracts) { Notify.System("Höchstens " + MaxContracts + " Gildenaufträge gleichzeitig."); return; }
                offeredContracts.Remove(q);
            }
            var s = new QuestState { def = q };
            active.Add(s);
            var pc = World.Player;
            if (q.grantPermit && !pc.permit)
            {
                pc.permit = true;
                pc.inv.Add("erlaubnis", 1);
                Notify.System(GameConstants.JudgeName + " stellt dir eine Sondererlaubnis für die Verbotenen Lande aus (Art. 112).");
            }
            Notify.System((q.main ? "Neues Kapitel: " : "Neuer Auftrag: ") + q.title);
            if (q.main && World.Hud != null) World.Hud.Banner(q.title.ToUpper(), Ui.Gold);
            BeginStep(s, false);
        }

        void BeginStep(QuestState s, bool resume)
        {
            if (!resume) s.progress = 0;
            var st = s.Step;
            if (st == null) { Complete(s); return; }
            ClearSpawns(s.def.id);
            if (!string.IsNullOrEmpty(st.spawn)) Spawn(s, st, resume);
            if (!resume) Notify.Post("[Auftrag] " + s.def.title + ": " + st.text, MessageKind.System);
        }

        void Advance(QuestState s)
        {
            var st = s.Step;
            var pc = World.Player;
            if (st != null && st.give != null)
            {
                foreach (var g in st.give)
                {
                    pc.inv.Add(g.id, g.count);
                    Notify.Combat("+" + g.count + " " + Items.Name(g.id));
                }
            }
            s.step++;
            s.progress = 0;
            if (s.step >= s.def.steps.Length) Complete(s);
            else BeginStep(s, false);
        }

        void Complete(QuestState s)
        {
            var q = s.def;
            var pc = World.Player;
            active.Remove(s);
            if (!done.Contains(q.id)) done.Add(q.id);
            ClearSpawns(q.id);

            int levelBefore = pc.level;
            if (q.expLevels > 0f) pc.GainExp(q.expLevels * PlayerCharacter.ExpToNext(pc.level) / (1f + pc.race.expBonus));
            int taler = q.hook == "auftrag" ? 40 + 10 * levelBefore : q.taler;
            if (taler > 0) pc.AddTaler(taler);
            if (q.items != null) foreach (var it in q.items) pc.inv.Add(it.id, it.count);
            if (q.respect != 0f && World.Residents != null) World.Residents.ChangeAll(q.respect);
            if (!string.IsNullOrEmpty(q.titleReward)) title = q.titleReward;

            if (q.hook == "markt_fleisch")
            {
                var g = World.Market.Get("fleisch");
                if (g != null) g.stock += 10f;
            }
            if (q.hook == "finale")
                Notify.Announce("Die Meuterei von Haus Vorn ist niedergeschlagen. " + pc.charName + " wird „" + q.titleReward + "“ genannt.");

            string reward = taler + " Taler";
            if (q.items != null) foreach (var it in q.items) reward += ", " + it.count + "× " + Items.Name(it.id);
            if (q.respect != 0f) reward += ", Respekt in Eichenhain +" + q.respect;
            if (!string.IsNullOrEmpty(q.titleReward)) reward += ", Titel „" + q.titleReward + "“";
            Notify.System((q.main ? "Kapitel abgeschlossen: " : "Auftrag erfüllt: ") + q.title + ". Belohnung: " + reward + ".");
            if (!string.IsNullOrEmpty(q.completeText)) Notify.System(q.completeText);
            if (World.Hud != null) World.Hud.Banner(q.main ? "KAPITEL ABGESCHLOSSEN" : "AUFTRAG ERFÜLLT", Ui.Good);
            if (q.main) WorldBootstrap.SaveGame();
            CheckAutoStart();
        }

        // ---------- Ereignisse ----------
        public void OnKill(string species)
        {
            foreach (var s in active.ToArray())
            {
                var st = s.Step;
                if (st == null || st.type != StepType.Kill || st.target != species) continue;
                s.progress++;
                Notify.Combat(s.def.title + ": " + s.progress + "/" + st.count);
                if (s.progress >= st.count) Advance(s);
            }
        }

        public void OnInvestigate(Clue clue)
        {
            foreach (var s in active.ToArray())
            {
                var st = s.Step;
                if (st == null || st.type != StepType.Investigate || st.target != clue.clueId) continue;
                s.progress++;
                Notify.System(clue.finding);
                Destroy(clue.gameObject);
                if (s.progress >= st.count) Advance(s);
                return;
            }
        }

        public string CompleteTalk(QuestState s)
        {
            string line = s.Step != null ? s.Step.line : "";
            Advance(s);
            return line;
        }

        public bool Deliver(QuestState s)
        {
            var st = s.Step;
            var pc = World.Player;
            if (st == null || pc.inv.Count(st.target) < st.count) return false;
            pc.inv.Remove(st.target, st.count);
            s.progress = st.count;
            Advance(s);
            return true;
        }

        void Update()
        {
            if (!World.Ready) return;
            pollTimer -= Time.deltaTime;
            autoTimer -= Time.deltaTime;
            if (autoTimer <= 0f)
            {
                autoTimer = 2f;
                CheckAutoStart();
            }
            if (pollTimer > 0f) return;
            pollTimer = 0.5f;
            var pc = World.Player;
            foreach (var s in active.ToArray())
            {
                var st = s.Step;
                if (st == null) continue;
                bool ok = false;
                switch (st.type)
                {
                    case StepType.Reach:
                        ok = (pc.transform.position - PlacePos(st.place)).magnitude <= st.radius && (!st.night || World.Clock.IsNight);
                        break;
                    case StepType.Seal: ok = World.Rift.Strength >= st.count; break;
                    case StepType.Rank: ok = pc.rankIndex >= st.count; break;
                    case StepType.Population: ok = World.Eco.Count(st.target) >= st.count; break;
                    case StepType.Level: ok = pc.level >= st.count; break;
                }
                if (ok) Advance(s);
            }
        }

        // ---------- Gildenaufträge ----------
        void RefreshContracts(int day)
        {
            foreach (var q in offeredContracts)
            {
                contractInfo.Remove(q.id);
                contractDefs.Remove(q.id);
            }
            offeredContracts.Clear();
            contractDay = day;

            var candidates = new List<ContractSave>();
            foreach (var def in Species.All)
            {
                if (!def.ecosystem || World.Eco.StockFraction(def.id) < 0.7f) continue;
                int n = Mathf.Clamp(Mathf.RoundToInt(World.Eco.Count(def.id) * 0.2f), 1, 3);
                candidates.Add(new ContractSave { kind = 0, target = def.id, count = n });
            }
            string[] goods = { "holz", "stein", "eisenerz", "fell", "heilkraut", "eisenbarren", "fleisch" };
            int[] counts = { 10, 8, 6, 4, 8, 3, 6 };
            for (int i = 0; i < 2; i++)
            {
                int k = Random.Range(0, goods.Length);
                candidates.Add(new ContractSave { kind = 1, target = goods[k], count = counts[k] });
            }
            if (World.Rift.Strength < 70f) candidates.Add(new ContractSave { kind = 2, target = "leerenbrut", count = 3 });

            for (int i = 0; i < 3 && candidates.Count > 0; i++)
            {
                int k = Random.Range(0, candidates.Count);
                var c = candidates[k];
                candidates.RemoveAt(k);
                contractCounter++;
                c.id = "auftrag_" + contractCounter;
                c.offered = true;
                var def = BuildContract(c);
                contractInfo[c.id] = c;
                contractDefs[c.id] = def;
                offeredContracts.Add(def);
            }
        }

        static QuestDef BuildContract(ContractSave c)
        {
            var q = new QuestDef { id = c.id, contract = true, giver = "Runa", expLevels = 0.25f, hook = "auftrag" };
            switch (c.kind)
            {
                case 0:
                {
                    var sp = Species.Get(c.target);
                    string name = sp != null ? sp.name : c.target;
                    q.title = "Jagdauftrag: " + c.count + "× " + name;
                    q.summary = "Die Wildhüter geben die Jagd frei, solange der Bestand gesund ist (über 70 % der Tragfähigkeit). Jedes erlegte Tier fehlt danach im Ökosystem.";
                    q.steps = new[] { new StepDef { type = StepType.Kill, target = c.target, count = c.count, text = "Erlege " + c.count + "× " + name } };
                    break;
                }
                case 1:
                    q.title = "Lieferauftrag: " + c.count + "× " + Items.Name(c.target);
                    q.summary = "Die Gilde braucht Vorräte. Liefere sie bei Gildenmeisterin Runa ab.";
                    q.steps = new[] { new StepDef { type = StepType.Deliver, target = c.target, count = c.count, npc = "Runa", place = "gildenhaus", text = "Liefere " + c.count + "× " + Items.Name(c.target) + " an Runa" } };
                    break;
                default:
                    q.title = "Risswache: " + c.count + "× Leerenbrut";
                    q.summary = "Das Siegel ist schwach. Der Orden zahlt über die Gilde für jede besiegte Leerenbrut.";
                    q.steps = new[] { new StepDef { type = StepType.Kill, target = "leerenbrut", count = c.count, place = "riss", text = "Besiege " + c.count + "× Leerenbrut" } };
                    break;
            }
            return q;
        }

        // ---------- Orte und Ereignisse der Geschichte ----------
        public static Vector3 PlacePos(string key)
        {
            switch (key)
            {
                case "kanzlei": return Places.JudgePos;
                case "thronsaal": return Places.EmperorPos;
                case "arena": return Places.ArenaCenter;
                case "grenzposten": return Places.BorderPostL;
                case "kraterrand": return Places.CraterRim;
                case "siegel": return Places.SealPos;
                case "riss": return Places.RiftPos;
                case "eichenhain": return Places.VillageCenter;
                case "waldrand": return Places.ForestEdge;
                case "lager": return Places.CampPos;
                case "marktplatz": return Places.CityPlaza;
                case "schmiede": return Places.SmithPos;
                case "lazarett": return Places.HealerPos;
                case "gildenhaus": return Places.GuildMasterPos;
                case "erzhoehen": return new Vector3(WorldLayout.Hills.x, Land.Height(WorldLayout.Hills.x, WorldLayout.Hills.y), WorldLayout.Hills.y);
                default: return Vector3.zero;
            }
        }

        void Track(string questId, GameObject go)
        {
            List<GameObject> list;
            if (!spawned.TryGetValue(questId, out list)) { list = new List<GameObject>(); spawned[questId] = list; }
            list.Add(go);
        }

        void ClearSpawns(string questId)
        {
            List<GameObject> list;
            if (!spawned.TryGetValue(questId, out list)) return;
            foreach (var go in list) if (go != null) Destroy(go);
            list.Clear();
        }

        static Vector3 Around(Vector3 c, int i, int n, float r)
        {
            float a = (i / (float)Mathf.Max(1, n)) * Mathf.PI * 2f + 0.4f;
            float x = c.x + Mathf.Cos(a) * r, z = c.z + Mathf.Sin(a) * r;
            return new Vector3(x, Land.Height(x, z), z);
        }

        void Spawn(QuestState s, StepDef st, bool resume)
        {
            int remaining = Mathf.Max(0, st.count - s.progress);
            string id = s.def.id;
            switch (st.spawn)
            {
                case "splitter":
                    for (int i = 0; i < remaining; i++)
                        SpawnClue(id, "runensplitter", "Runensplitter", Around(Places.CraterRim, i, 3, 9f),
                            "Ein Splitter mit eingeritzten Runen. Sie pulsieren im Takt des Risses — jemand hat sie hier vergraben.");
                    break;
                case "brandzeichen":
                    for (int i = 0; i < remaining; i++)
                        SpawnClue(id, "brandzeichen", "Brandzeichen", Around(Places.ForestEdge, i, 3, 8f),
                            "Ein eingebranntes Zeichen im Boden. Daneben Fußspuren, die tiefer in den Wald führen.");
                    break;
                case "brief":
                    SpawnClue(id, "brief", "Verkohltes Lagerfeuer", Places.ForestEdge + new Vector3(4f, 0f, 6f),
                        "Ein halb verbrannter Brief mit dem Wappen von Haus Vorn: „… nach dem Ritual gehört Grünmark wieder uns … Lager in den Erzhöhen …“");
                    break;
                case "ritualplan":
                    SpawnClue(id, "ritualplan", "Selas Zelt", Places.CampPos + new Vector3(0f, 0f, 4f),
                        "Ein Ritualplan: In einer Nacht soll das Siegel am Riss gebrochen werden. Unterzeichnet: Kasimir Vorn.");
                    break;
                case "kult_wald":
                    for (int i = 0; i < remaining; i++) SpawnFoe(id, "kultist", Around(Places.ForestEdge, i, Mathf.Max(1, remaining), 6f));
                    break;
                case "kult_lager":
                    for (int i = 0; i < remaining; i++) SpawnFoe(id, "kultist", Around(Places.CampPos, i, Mathf.Max(1, remaining), 9f));
                    break;
                case "sela":
                    if (remaining > 0) SpawnFoe(id, "kultmeisterin", Places.CampPos + new Vector3(0f, 0.5f, 2f));
                    break;
                case "ritual":
                    if (remaining <= 0) break;
                    var boss = SpawnFoe(id, "kasimir", Places.RiftPos + new Vector3(0f, 1f, -8f));
                    boss.summonInterval = 18f;
                    for (int i = 0; i < 3; i++) SpawnFoe(id, "kultist", Around(Places.RiftPos + new Vector3(0f, 0f, -8f), i, 3, 7f));
                    if (!resume)
                    {
                        World.Rift.Weaken(30f);
                        Notify.Announce("In der Nacht flammt der Riss auf. Kasimir Vorn von Haus Vorn beginnt das Ritual — eine Meuterei gegen den Thron (Art. 141)!");
                    }
                    break;
            }
        }

        Creature SpawnFoe(string questId, string speciesId, Vector3 pos)
        {
            var def = Species.Get(speciesId);
            int lvl = World.Player.level;
            float hp = 0.8f + lvl * 0.05f;
            float dmg = 0.8f + lvl * 0.04f;
            var c = Creature.SpawnManaged(def, pos + Vector3.up * 0.5f, hp, dmg, 1f, Faction.Void);
            c.questTag = questId;
            c.SetHome(pos);
            Track(questId, c.gameObject);
            return c;
        }

        void SpawnClue(string questId, string clueId, string label, Vector3 pos, string finding)
        {
            var go = new GameObject("Spur: " + label);
            go.transform.position = pos;
            var clue = go.AddComponent<Clue>();
            clue.clueId = clueId;
            clue.label = label;
            clue.finding = finding;
            clue.radius = 3f;
            clue.offset = Vector3.up * 0.5f;
            switch (clueId)
            {
                case "runensplitter":
                {
                    var shard = Build.Box(go.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.25f, 0.9f, 0.25f), new Color(0.7f, 0.25f, 1f), false, 3f);
                    shard.transform.localRotation = Quaternion.Euler(20f, 30f, 15f);
                    break;
                }
                case "brandzeichen":
                    Build.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1.6f, 0.02f, 1.6f), new Color(0.08f, 0.06f, 0.06f), false);
                    Build.Box(go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(1f, 0.02f, 0.12f), new Color(1f, 0.45f, 0.15f), false, 2.5f);
                    Build.Box(go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.12f, 0.02f, 1f), new Color(1f, 0.45f, 0.15f), false, 2.5f);
                    break;
                case "brief":
                    for (int i = 0; i < 4; i++)
                    {
                        var log = Build.Box(go.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.15f, 0.15f, 1f), new Color(0.2f, 0.12f, 0.08f), false);
                        log.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
                    }
                    Build.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.3f, 0.5f), new Color(1f, 0.4f, 0.1f), false, 2f);
                    Build.Box(go.transform, new Vector3(0.8f, 0.05f, 0.3f), new Vector3(0.3f, 0.02f, 0.4f), new Color(0.9f, 0.85f, 0.7f), false, 0.3f);
                    break;
                default:
                    Build.Box(go.transform, new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 0.7f), new Color(0.35f, 0.25f, 0.18f), false);
                    Build.Box(go.transform, new Vector3(0f, 0.82f, 0f), new Vector3(0.5f, 0.02f, 0.4f), new Color(0.95f, 0.9f, 0.75f), false, 0.4f);
                    break;
            }
            Track(questId, go);
        }
    }
}
