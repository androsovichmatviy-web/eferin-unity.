using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Art. 11: Menschen, Zwerge und Elfen. Art. 24: Gleichheit unabhängig von der Rasse —
    // deshalb hat jede Rasse dieselbe Summe an Boni (5 Punkte), nur anders verteilt.
    public class RaceDef
    {
        public string id;
        public string name;
        public string description;
        public int str, agi, vit, intl;
        public float carryBonus;
        public float expBonus;
        public float aetherRegenBonus;
        public float heightScale = 1f;
        public Color skin;
    }

    public static class Races
    {
        public static readonly List<RaceDef> All = new List<RaceDef>
        {
            new RaceDef
            {
                id = "mensch", name = "Mensch",
                description = "Vielfalt der Wege. Ausgeglichen, lernt schneller (+5 % Erfahrung).",
                str = 1, agi = 1, vit = 2, intl = 1, expBonus = 0.05f,
                heightScale = 1f, skin = new Color(0.93f, 0.78f, 0.64f),
            },
            new RaceDef
            {
                id = "zwerg", name = "Zwerg",
                description = "Meister der Schmieden. Robust, trägt mehr (+15 kg), erhält beim Schmelzen einen Barren extra.",
                str = 2, agi = 0, vit = 3, intl = 0, carryBonus = 15f,
                heightScale = 0.8f, skin = new Color(0.85f, 0.66f, 0.52f),
            },
            new RaceDef
            {
                id = "elf", name = "Elf",
                description = "Erben der alten Künste. Gewandt und klug, Äther regeneriert schneller (+25 %).",
                str = 0, agi = 3, vit = 0, intl = 2, aetherRegenBonus = 0.25f,
                heightScale = 1.06f, skin = new Color(0.96f, 0.88f, 0.8f),
            },
        };

        public static RaceDef Get(string id)
        {
            foreach (var r in All) if (r.id == id) return r;
            return All[0];
        }
    }
}
