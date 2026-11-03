using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Art. 16 und 60 Nr. 3: Das Siegel der Eindämmung hält den Riss. Wird es schwach, quillt Leerenbrut hervor.
    // Risswesen sind die einzige Ausnahme vom Gesetz "Getötete kehren nicht zurück": die Geschichte bringt sie zurück.
    public class RiftSeal : Interactable
    {
        public float Strength { get; private set; }
        public bool Summons { get; private set; }

        readonly List<Creature> voids = new List<Creature>();
        Creature guardian;
        bool guardianDead;
        int guardianDeathDay;
        Renderer rune;
        Light glow;

        public override string Prompt { get { return "Siegel verstärken (1 Ätherkristall) — Stärke " + Mathf.RoundToInt(Strength) + " %"; } }

        public void Init(SaveData d, WorldClock clock)
        {
            Strength = d.sealStrength;
            guardianDead = d.riftGuardianDead;
            guardianDeathDay = d.riftGuardianDeathDay;
            radius = 4.5f;
            offset = Vector3.up * 1.5f;

            Build.Box(transform, new Vector3(0f, 3f, 0f), new Vector3(1.4f, 6f, 1.4f), new Color(0.25f, 0.25f, 0.3f));
            Build.Box(transform, new Vector3(0f, 0.3f, 0f), new Vector3(3f, 0.6f, 3f), new Color(0.35f, 0.35f, 0.38f));
            var r = Build.Prim(PrimitiveType.Cylinder, transform, new Vector3(0f, 4.5f, 0f), new Vector3(2f, 0.15f, 2f), Color.cyan, false);
            rune = r.GetComponent<Renderer>();
            rune.material = MaterialFactory.Glowing(Color.cyan, 2f);
            var lg = new GameObject("Siegellicht");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = Vector3.up * 5f;
            glow = lg.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 18f;

            clock.HourPassed += OnHour;
            UpdateVisual();
        }

        void UpdateVisual()
        {
            float t = Strength / 100f;
            Color c = Color.Lerp(new Color(0.7f, 0.15f, 0.9f), new Color(0.3f, 0.85f, 1f), t);
            MaterialFactory.SetColor(rune.material, c);
            MaterialFactory.SetEmission(rune.material, c * Mathf.Lerp(0.6f, 3f, t));
            glow.color = c;
            glow.intensity = Mathf.Lerp(0.5f, 2.5f, t);
        }

        public override void Interact(PlayerCharacter player)
        {
            if (player.inv.Count("aetherkristall") < 1)
            {
                Notify.System("Du brauchst einen Ätherkristall (Ätheradern in den Verbotenen Landen, Markt, Aufträge).");
                return;
            }
            if (Strength >= 99.5f)
            {
                Notify.System("Das Siegel ist vollständig.");
                return;
            }
            player.inv.Remove("aetherkristall", 1);
            Strength = Mathf.Min(100f, Strength + 20f);
            player.GainExp(120f);
            if (World.Treasury.Pay(60, "Belohnungen")) player.AddTaler(60);
            Notify.System("Siegel verstärkt auf " + Mathf.RoundToInt(Strength) + " %. Die Kasse dankt dir mit 60 Talern (Art. 60 Nr. 3: Hüte die Siegel).");
            UpdateVisual();
        }

        void OnHour(int day, int hour)
        {
            float order = Strength > 20f ? 1.5f : 0.3f;   // der Orden der Kampfmagier hält dagegen
            Strength = Mathf.Clamp(Strength - 2f + order, 0f, 100f);
            if (Random.value < 0.012f)
            {
                Strength = Mathf.Max(0f, Strength - 25f);
                Notify.Announce("Rissbeben! Das Siegel der Eindämmung ist geschwächt (" + Mathf.RoundToInt(Strength) + " %).");
            }

            voids.RemoveAll(v => v == null || v.IsCorpse);
            bool night = World.Clock.IsNight;
            int want = Mathf.FloorToInt((1f - Strength / 100f) * 3f) + (night ? 1 : 0);
            want = Mathf.Min(want, 2);
            var def = Species.Get("leerenbrut");
            for (int i = 0; i < want && voids.Count < 12; i++)
            {
                Vector3 p = Places.RiftPos + new Vector3(Random.Range(-10f, 10f), 1f, Random.Range(-8f, 8f));
                var v = Creature.Spawn(def, p, 100f);
                v.SetHome(Places.RiftPos + new Vector3(0f, 0f, -15f));
                if (night && Strength < 40f && Random.value < 0.35f) v.Raider = true;
                voids.Add(v);
            }

            if ((guardian == null || guardian.IsCorpse) && Strength < 30f && (!guardianDead || day > guardianDeathDay))
            {
                var g = Species.Get("risswaechter");
                guardian = Creature.Spawn(g, Places.RiftPos + Vector3.up, 100f);
                guardian.SetHome(Places.RiftPos);
                guardianDead = false;
                Notify.Announce("Der Wächter des Risses ist zurückgekehrt. Nur Risswesen kehren wieder — die Geschichte bringt sie zurück.");
            }

            if (!Summons && Strength < 15f)
            {
                Summons = true;
                Notify.Announce("GROSSER AUFRUF (Art. 66): Kaiser " + GameConstants.EmperorName + " ruft den Kontinent zur Verteidigung. Für jede getötete Leerenbrut zahlt die Kasse 40 Taler.");
            }
            else if (Summons && Strength >= 50f)
            {
                Summons = false;
                Notify.Announce("Der Große Aufruf ist beendet. Das Siegel hält wieder.");
            }
            UpdateVisual();
        }

        public void OnVoidKilled(Creature c, bool byPlayer)
        {
            voids.Remove(c);
            if (c == guardian)
            {
                guardianDead = true;
                guardianDeathDay = World.Day;
                Strength = Mathf.Min(100f, Strength + 15f);
                Notify.Announce("Der Wächter des Risses ist gefallen. Das Siegel erholt sich (+15 %).");
                UpdateVisual();
            }
            if (byPlayer && Summons && World.Treasury.Pay(40, "Großer Aufruf")) World.Player.AddTaler(40);
        }

        // Für den Demo-Modus.
        public void Weaken(float amount)
        {
            Strength = Mathf.Max(0f, Strength - amount);
            UpdateVisual();
        }

        public void Save(SaveData d)
        {
            d.sealStrength = Strength;
            d.riftGuardianDead = guardianDead;
            d.riftGuardianDeathDay = guardianDeathDay;
        }
    }
}
