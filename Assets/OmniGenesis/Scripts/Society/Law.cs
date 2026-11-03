using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Recht und Gericht nach dem Edikt: Festnahme nur bei Tat (Art. 29), Gericht binnen drei Tagen,
    // Unschuldsvermutung (Art. 57), keine Doppelbestrafung (Art. 58), Großer Verrat (Art. 62).
    public class Law : MonoBehaviour
    {
        public int Wanted { get; private set; }
        public bool GreatTreason { get; private set; }
        public int LastCrimeDay { get; private set; }
        public int PendingFine { get; private set; }
        public bool Trespassing { get; private set; }
        public readonly List<string> record = new List<string>();

        float checkTimer;

        public void Load(SaveData d)
        {
            Wanted = d.wanted;
            GreatTreason = d.greatTreason;
            LastCrimeDay = d.lastCrimeDay;
            PendingFine = d.pendingFine;
            record.Clear();
            record.AddRange(d.record);
        }

        public void Save(SaveData d)
        {
            d.wanted = Wanted;
            d.greatTreason = GreatTreason;
            d.lastCrimeDay = LastCrimeDay;
            d.pendingFine = PendingFine;
            d.record = new List<string>(record);
        }

        public bool CleanRecord(int days)
        {
            return !GreatTreason && Wanted == 0 && World.Day - LastCrimeDay > days;
        }

        void Report(int severity, int fine, string text)
        {
            Wanted = Mathf.Clamp(Mathf.Max(Wanted, severity), 0, 3);
            PendingFine += fine;
            LastCrimeDay = World.Day;
            string entry = World.Clock.Stamp() + ": " + text;
            record.Add(entry);
            Notify.Post("[Gesetz] " + text + " Fahndungsstufe " + Wanted + ". Stelle dich mit [G] oder fliehe vor den Wachen.", MessageKind.Law);
            if (World.Player != null && WorldLayout.InVillageRegion(World.Player.transform.position) && World.Residents != null)
                World.Residents.OnCrimeInRegion();
        }

        public void OnPlayerAttackedNpc(Npc npc)
        {
            int lvl = World.Player != null ? World.Player.level : 1;
            switch (npc.role)
            {
                case NpcRole.Kaiser:
                    GreatTreason = true;
                    Report(3, 5000, "GROSSER VERRAT: Angriff auf den Kaiser (Art. 62). Das Recht, Prätendent zu werden, ist für immer verwirkt (Art. 129).");
                    break;
                case NpcRole.Richter:
                    GreatTreason = true;
                    Report(3, 3000, "Angriff auf den Obersten Arbiter (Art. 97: unantastbar im Amt). Dies gilt als Großer Verrat.");
                    break;
                case NpcRole.Wache:
                case NpcRole.Ordensmagier:
                    Report(2, 300 + lvl * 20, "Angriff auf " + npc.title + " " + npc.npcName + ".");
                    break;
                case NpcRole.Dorfwache:
                    break;
                default:
                    Report(1, 100 + lvl * 10, "Angriff auf " + npc.npcName + " (Art. 27: Recht auf Leben).");
                    break;
            }
        }

        void Update()
        {
            if (!World.Ready || World.Player == null) return;
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = 1f;
                bool inForbidden = WorldLayout.RegionAt(World.Player.transform.position) == Region.VerboteneLande;
                bool now = inForbidden && !World.Player.permit;
                if (now && !Trespassing)
                    Report(1, 150, "Betreten der Verbotenen Lande ohne Kaiserliche Erlaubnis (Art. 112).");
                Trespassing = now;
            }
            if (Wanted > 0 && !World.GameplayBlocked && GameInput.Pressed(Act.Surrender)) Arrest();
        }

        // Festnahme und Gerichtsverhandlung bei Richter Ataklas.
        public void Arrest()
        {
            var pc = World.Player;
            if (World.Tower != null && World.Tower.InTower) World.Tower.Leave(false);
            World.Clock.Advance(24f * 60f);
            int fine = PendingFine;
            int paid = Mathf.Min(fine, pc.taler);
            pc.taler -= paid;
            World.Treasury.Add("Bußgelder", paid);
            int jailDays = 0;
            if (paid < fine) jailDays = Mathf.Clamp(Mathf.CeilToInt((fine - paid) / 200f), 1, 5);
            if (jailDays > 0) World.Clock.Advance(jailDays * 24f * 60f);

            string verdict = "Urteil des Gerichts (verhandelt binnen drei Tagen, Art. 29; die Schuld wurde durch Zeugen bewiesen, Art. 57): Bußgeld " + fine + " Taler, bezahlt " + paid + ".";
            if (jailDays > 0) verdict += " Restschuld als Haft: " + jailDays + " Tag(e).";
            verdict += " Die Tat ist gesühnt — niemand wird für dieselbe Tat zweimal bestraft (Art. 58).";
            record.Add(World.Clock.Stamp() + ": " + verdict);
            Notify.Post("[Gericht] " + verdict, MessageKind.Law);

            Wanted = 0;
            PendingFine = 0;
            pc.health.hp = Mathf.Max(pc.health.hp, pc.health.maxHp * 0.5f);
            pc.motor.Teleport(Places.CourtPos + Vector3.up * 0.2f, 90f);
        }
    }
}
