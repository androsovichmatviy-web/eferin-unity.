using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Art. 48: Ränge des Aufstiegs, vergeben durch die Kaiserliche Prüfung der Meisterschaft.
    // Art. 69: Die höchsten Ränge (S, SS, SSS) verleiht der Kaiser selbst.
    public static class Ranks
    {
        public static readonly string[] Names = { "F", "E", "D", "C", "B", "A", "S", "SS", "SSS" };
        public static readonly int[] LevelReq = { 1, 5, 10, 20, 30, 45, 60, 80, 100 };

        public static string Name(int i) { return i < 0 ? "—" : Names[Mathf.Clamp(i, 0, Names.Length - 1)]; }

        public static Color Color(int i)
        {
            switch (i)
            {
                case 0: return new Color(0.7f, 0.7f, 0.7f);
                case 1: return new Color(0.75f, 0.9f, 0.75f);
                case 2: return new Color(0.4f, 0.85f, 0.45f);
                case 3: return new Color(0.35f, 0.85f, 0.9f);
                case 4: return new Color(0.4f, 0.55f, 1f);
                case 5: return new Color(0.75f, 0.45f, 1f);
                case 6: return new Color(1f, 0.82f, 0.3f);
                case 7: return new Color(1f, 0.55f, 0.2f);
                case 8: return new Color(1f, 0.25f, 0.35f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        public static int LevelCap(int level)
        {
            int cap = 0;
            for (int i = 0; i < LevelReq.Length; i++) if (level >= LevelReq[i]) cap = i;
            return cap;
        }
    }

    public class RankTrial : MonoBehaviour
    {
        public bool Active { get; private set; }

        int wave;
        float startTime;
        float damageTaken;
        readonly List<Creature> golems = new List<Creature>();

        public void Begin()
        {
            var pc = World.Player;
            Active = true;
            wave = 0;
            damageTaken = 0f;
            startTime = Time.time;
            if (Places.ArenaGate != null) Places.ArenaGate.SetActive(true);
            pc.motor.Teleport(Places.ArenaCenter + new Vector3(8f, 0.3f, 0f), -90f);
            pc.health.Damaged += OnDamaged;
            Notify.System("Die Prüfung der Meisterschaft beginnt. Drei Wellen Prüfungsgolems. Zeit und Treffer werden gewertet.");
            SpawnWave();
        }

        void OnDamaged(DamageInfo info) { damageTaken += info.amount; }

        void SpawnWave()
        {
            var def = Species.Get("pruefungsgolem");
            int lvl = World.Player.level;
            int count = 2 + wave;
            float hpMult = (60f + lvl * 18f) / def.hp;
            float dmgMult = 1f + lvl * 0.12f;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f;
                var pos = Places.ArenaCenter + new Vector3(Mathf.Cos(a) * 8f - 3f, 0.6f, Mathf.Sin(a) * 8f);
                var g = Creature.SpawnManaged(def, pos, hpMult, dmgMult, 0f, Faction.Tower);
                g.onDeath = c => golems.Remove(c);
                golems.Add(g);
            }
            if (World.Hud != null) World.Hud.Banner("WELLE " + (wave + 1) + " / 3", new Color(0.6f, 0.8f, 1f));
        }

        void Update()
        {
            if (!Active) return;
            golems.RemoveAll(g => g == null);
            if (golems.Count > 0) return;
            wave++;
            if (wave >= 3) Finish(true);
            else SpawnWave();
        }

        public void OnPlayerDowned()
        {
            if (Active) Finish(false);
        }

        void Finish(bool success)
        {
            var pc = World.Player;
            Active = false;
            pc.health.Damaged -= OnDamaged;
            foreach (var g in golems) if (g != null) Destroy(g.gameObject);
            golems.Clear();
            if (Places.ArenaGate != null) Places.ArenaGate.SetActive(false);

            int perf;
            float time = Time.time - startTime;
            if (!success) perf = wave - 1;   // F nach der ersten Welle, E nach der zweiten
            else
            {
                perf = 3;
                perf += time < 40f ? 3 : time < 70f ? 2 : time < 110f ? 1 : 0;
                float frac = damageTaken / pc.MaxHp;
                perf += frac < 0.2f ? 2 : frac < 0.6f ? 1 : 0;
            }
            int cap = Ranks.LevelCap(pc.level);
            int rank = Mathf.Min(perf, cap);
            string detail = "Zeit " + time.ToString("0") + " s, erlittener Schaden " + Mathf.RoundToInt(damageTaken) + ". Leistung: " + Ranks.Name(perf) + ", Stufengrenze: " + Ranks.Name(cap) + ".";
            if (rank < 0)
            {
                Notify.System("Prüfung nicht bestanden — nicht einmal die erste Welle wurde besiegt. " + detail);
            }
            else if (rank > pc.rankIndex)
            {
                pc.rankIndex = rank;
                string by = rank >= 6 ? " — verliehen durch Kaiser " + GameConstants.EmperorName + " (Art. 69)" : "";
                Notify.System("Rang " + Ranks.Name(rank) + " erhalten" + by + ". " + detail);
                if (World.Hud != null) World.Hud.Banner("RANG " + Ranks.Name(rank), Ranks.Color(rank));
                if (rank >= 6) Notify.Announce(pc.charName + " wurde in den Rang " + Ranks.Name(rank) + " des Aufstiegs erhoben.");
            }
            else
            {
                Notify.System("Prüfung beendet. Dein Rang bleibt " + Ranks.Name(pc.rankIndex) + ". " + detail);
            }
            pc.motor.Teleport(Places.TrialMasterPos + new Vector3(2f, 0.3f, 0f), 90f);
        }
    }
}
