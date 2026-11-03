using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public class Resident
    {
        public string name;
        public float respect;
        public string needItem = "";
        public int needCount;
        public Npc npc;

        public bool Respects { get { return respect >= GameConstants.RespectThreshold; } }
    }

    // Einwohner von Eichenhain. Ihr Respekt ist die Säule des Volkes (Art. 132).
    // Respekt entsteht durch Schutz, durch erfüllte Bedürfnisse und geht durch Verbrechen verloren.
    public class Residents : MonoBehaviour
    {
        public readonly List<Resident> list = new List<Resident>();

        static readonly string[] Names =
        {
            "Anna Weber", "Bruno Kessler", "Clara Brandt", "Dieter Holm", "Elsa Fink", "Friedrich Lang",
            "Greta Moor", "Hans Riedel", "Ida Sommer", "Jonas Falk", "Katrin Bauer", "Lukas Stein",
        };

        static readonly string[] NeedItems = { "holz", "fleisch", "heilkraut", "heiltrank", "fell", "eisenbarren", "stein", "gebratenes_fleisch" };
        static readonly int[] NeedCounts = { 5, 3, 4, 1, 2, 1, 4, 2 };

        float lastThanks = -100f;

        public int RespectingCount
        {
            get
            {
                int n = 0;
                foreach (var r in list) if (r.Respects) n++;
                return n;
            }
        }

        public bool AllRespect { get { return list.Count > 0 && RespectingCount == list.Count; } }

        public float AverageRespect
        {
            get
            {
                if (list.Count == 0) return 0f;
                float s = 0f;
                foreach (var r in list) s += r.respect;
                return s / list.Count;
            }
        }

        public void Init(SaveData d, WorldClock clock)
        {
            list.Clear();
            for (int i = 0; i < Names.Length; i++)
            {
                var r = new Resident { name = Names[i] };
                foreach (var s in d.residents)
                {
                    if (s.name != r.name) continue;
                    r.respect = s.respect;
                    r.needItem = s.needItem;
                    r.needCount = s.needCount;
                }
                if (d.residents.Count == 0) AssignNeed(r);
                list.Add(r);

                Vector3 home = Places.VillageHomes.Count > 0 ? Places.VillageHomes[i % Places.VillageHomes.Count] : Places.VillageCenter;
                Color cloth = Color.HSVToRGB((i * 0.13f) % 1f, 0.35f, 0.7f);
                var npc = Npc.Spawn(NpcRole.Einwohner, r.name, "Einwohnerin von Eichenhain", home, Random.Range(0f, 360f), cloth, new Color(0.35f, 0.28f, 0.2f));
                if (i % 2 == 1) npc.title = "Einwohner von Eichenhain";
                npc.homePos = home;
                npc.route = new List<Vector3>();
                for (int k = 0; k < 4; k++) npc.route.Add(Places.VillageWalk[(i + k * 3) % Places.VillageWalk.Count]);
                npc.resident = r;
                r.npc = npc;
            }
            clock.DayStarted += OnDay;
        }

        void AssignNeed(Resident r)
        {
            int k = Random.Range(0, NeedItems.Length);
            r.needItem = NeedItems[k];
            r.needCount = NeedCounts[k];
        }

        void OnDay(int day)
        {
            foreach (var r in list) if (string.IsNullOrEmpty(r.needItem)) AssignNeed(r);
        }

        public void Change(Resident r, float delta)
        {
            bool before = r.Respects;
            r.respect = Mathf.Clamp(r.respect + delta, -100f, 100f);
            if (!before && r.Respects) Notify.System(r.name + " respektiert dich nun. (" + RespectingCount + "/" + list.Count + " Einwohner)");
            if (before && !r.Respects) Notify.System(r.name + " hat den Respekt vor dir verloren.");
        }

        public void ChangeAll(float delta)
        {
            foreach (var r in list) Change(r, delta);
        }

        public bool Fulfil(Resident r, PlayerCharacter pc)
        {
            if (string.IsNullOrEmpty(r.needItem) || pc.inv.Count(r.needItem) < r.needCount) return false;
            pc.inv.Remove(r.needItem, r.needCount);
            r.needItem = "";
            r.needCount = 0;
            Change(r, 12f);
            foreach (var o in list) if (o != r) Change(o, 1f);
            pc.GainExp(25f);
            return true;
        }

        public void OnHostileKilled(Vector3 pos, SpeciesDef def, bool byPlayer)
        {
            if (!byPlayer || !WorldLayout.InVillageRegion(pos)) return;
            if (def.temper == Temper.Scheu && def.diet != Diet.Leere) return;
            float gain = def.diet == Diet.Leere ? 4f : 2f;
            ChangeAll(gain);
            if (Time.time - lastThanks > 20f)
            {
                lastThanks = Time.time;
                Notify.System("Die Einwohner von Eichenhain haben gesehen, wie du sie beschützt hast (+" + gain + " Respekt).");
            }
        }

        public void OnCrimeInRegion()
        {
            ChangeAll(-15f);
        }

        public void OnAttacked(Resident r)
        {
            Change(r, -40f);
            foreach (var o in list) if (o != r) Change(o, -10f);
        }

        public void Save(SaveData d)
        {
            d.residents.Clear();
            foreach (var r in list)
                d.residents.Add(new ResidentSave { name = r.name, respect = r.respect, needItem = r.needItem, needCount = r.needCount });
        }
    }
}
