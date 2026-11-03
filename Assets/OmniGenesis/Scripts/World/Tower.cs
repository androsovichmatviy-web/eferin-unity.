using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Art. 113–115: Der fünfzigstöckige Turm. Etagen werden prozedural erzeugt.
    // Turmwesen sind Teil der Prüfung und kehren zurück — anders als die Tiere des Ökosystems.
    public class Tower : MonoBehaviour
    {
        public int Floor { get; private set; }
        public bool InTower { get { return Floor > 0; } }
        public readonly List<string> chronicles = new List<string>();

        Transform floorRoot;
        readonly List<Creature> monsters = new List<Creature>();
        bool cleared;
        int earned;

        // Frühere Bezwinger laut den Chroniken der Genesis (erfundene Helden dieser Welt).
        static readonly string[] Legends =
        {
            "Haus Valkenrath", "Liora Sternwind", "Gilde der Grauen Klingen", "Bram Eisenfaust", "Seraphine Adler",
            "Orden der Morgenröte", "Tamsin Kreuzberg", "Clan Nebelwolf", "Jorek der Stille", "Haus Ardent",
            "Mirella von Asch", "Gilde der Sternwanderer", "Kael Rotdorn", "Ysolde Weißbach", "Clan Eisenkrone",
            "Dorian Falkenhain", "Haus Silberquell",
        };

        public static int RequiredRank(int floor)
        {
            if (floor <= 10) return 0;
            if (floor <= 20) return 2;
            if (floor <= 30) return 3;
            if (floor <= 40) return 4;
            return 5;
        }

        public void Load(SaveData d)
        {
            chronicles.Clear();
            if (d.chronicles != null) chronicles.AddRange(d.chronicles);
            while (chronicles.Count < GameConstants.TowerFloors)
            {
                int i = chronicles.Count;
                chronicles.Add(i < Legends.Length ? Legends[i] + " (Ära der Einheit)" : "");
            }
        }

        public void Save(SaveData d)
        {
            d.chronicles = new List<string>(chronicles);
        }

        public bool CanEnter(int floor, out string reason)
        {
            var pc = World.Player;
            reason = "";
            if (floor < 1 || floor > GameConstants.TowerFloors) { reason = "Diese Etage gibt es nicht."; return false; }
            if (floor > pc.towerBest + 1) { reason = "Erst Etage " + (pc.towerBest + 1) + " bezwingen."; return false; }
            int req = RequiredRank(floor);
            if (pc.rankIndex < req)
            {
                reason = "Ukas des Throns: ab Etage " + floor + " ist Rang " + Ranks.Name(req) + " nötig (Prüfung in der Arena).";
                return false;
            }
            return true;
        }

        public void Enter(int floor)
        {
            Floor = floor;
            cleared = false;
            earned = 0;
            BuildFloor(floor);
            World.Player.motor.Teleport(WorldLayout.TowerSpace + new Vector3(0f, 0.3f, -16f), 0f);
            Notify.System("Turm — Etage " + floor + ". Besiege alle Turmwesen.");
            if (World.Hud != null) World.Hud.Banner("ETAGE " + floor, new Color(0.5f, 0.85f, 1f));
        }

        public void Next()
        {
            if (Floor >= GameConstants.TowerFloors) return;
            string reason;
            if (!CanEnter(Floor + 1, out reason)) { Notify.System(reason); return; }
            Enter(Floor + 1);
        }

        public void Leave(bool teleport = true)
        {
            Clear();
            Floor = 0;
            if (teleport) World.Player.motor.Teleport(Places.TowerDoor, -90f);
        }

        void Clear()
        {
            foreach (var m in monsters) if (m != null) Destroy(m.gameObject);
            monsters.Clear();
            if (floorRoot != null) Destroy(floorRoot.gameObject);
            floorRoot = null;
        }

        void BuildFloor(int f)
        {
            Clear();
            floorRoot = new GameObject("Turm-Etage " + f).transform;
            Vector3 o = WorldLayout.TowerSpace;
            int tier = (f - 1) / 10;
            Color[] floors = { new Color(0.55f, 0.52f, 0.45f), new Color(0.35f, 0.45f, 0.4f), new Color(0.4f, 0.4f, 0.55f), new Color(0.5f, 0.3f, 0.25f), new Color(0.25f, 0.2f, 0.3f) };
            Color floorColor = floors[Mathf.Clamp(tier, 0, floors.Length - 1)];
            Build.Prim(PrimitiveType.Cylinder, floorRoot, o + Vector3.down * 0.5f, new Vector3(46f, 0.5f, 46f), floorColor);
            for (int i = 0; i < 36; i++)
            {
                float a = i * 10f * Mathf.Deg2Rad;
                var wall = Build.Box(floorRoot, o + new Vector3(Mathf.Cos(a) * 23.5f, 4f, Mathf.Sin(a) * 23.5f), new Vector3(4.4f, 8f, 1f), floorColor * 0.8f);
                wall.transform.rotation = Quaternion.Euler(0f, -i * 10f + 90f, 0f);
            }
            var rng = new System.Random(f * 977);
            int pillars = 4 + rng.Next(0, 5);
            for (int i = 0; i < pillars; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 6f + (float)rng.NextDouble() * 12f;
                Build.Prim(PrimitiveType.Cylinder, floorRoot, o + new Vector3(Mathf.Cos(a) * r, 3f, Mathf.Sin(a) * r), new Vector3(1.6f, 3f, 1.6f), floorColor * 1.15f);
            }
            var lightGo = new GameObject("Etagenlicht");
            lightGo.transform.SetParent(floorRoot, false);
            lightGo.transform.position = o + Vector3.up * 14f;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 50f;
            l.intensity = 2f;
            l.color = new Color(0.8f, 0.9f, 1f);

            var exit = Build.Box(floorRoot, o + new Vector3(0f, 1.6f, -21f), new Vector3(2.6f, 3.2f, 0.3f), new Color(0.9f, 0.6f, 0.3f), false, 2f);
            exit.AddComponent<TowerPortal>().up = false;

            var def = Species.Get("turmwesen");
            bool boss = f % 10 == 0;
            int count = boss ? 2 + f / 10 : Mathf.Min(3 + f / 2, 14);
            float hpMult = 1f + 0.35f * (f - 1);
            float dmgMult = 1f + 0.18f * (f - 1);
            for (int i = 0; i < count; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 6f + (float)rng.NextDouble() * 12f;
                var pos = o + new Vector3(Mathf.Cos(a) * r, 0.5f, Mathf.Sin(a) * r + 3f);
                var m = Creature.SpawnManaged(def, pos, hpMult, dmgMult, 1f + 0.4f * (f - 1), Faction.Tower);
                m.Health.displayName = def.name + " (Etage " + f + ")";
                m.onDeath = OnMonsterDied;
                m.SetTint(Color.Lerp(def.color, floorColor * 1.4f, 0.4f));
                monsters.Add(m);
            }
            if (boss)
            {
                var b = Creature.SpawnManaged(def, o + new Vector3(0f, 0.5f, 8f), hpMult * 8f, dmgMult * 1.6f, (1f + 0.4f * (f - 1)) * 10f, Faction.Tower);
                b.Health.displayName = "Hüter der Etage " + f;
                b.transform.localScale = Vector3.one * 2.2f;
                b.onDeath = OnMonsterDied;
                b.SetTint(new Color(0.9f, 0.75f, 0.3f));
                monsters.Add(b);
            }
        }

        void OnMonsterDied(Creature c)
        {
            monsters.Remove(c);
            int reward = 15 * Floor;
            int tax = Mathf.RoundToInt(reward * GameConstants.TowerTreasuryShare);
            World.Player.AddTaler(reward - tax);
            World.Treasury.Add("Turmanteil", tax);
            earned += reward - tax;
            if (Random.value < 0.15f + Floor * 0.005f) { World.Player.inv.Add("aetherkristall", 1); Notify.Combat("+1 Ätherkristall"); }

            if (monsters.Count == 0 && !cleared)
            {
                cleared = true;
                var pc = World.Player;
                bool first = Floor > pc.towerBest;
                pc.towerBest = Mathf.Max(pc.towerBest, Floor);
                int idx = Floor - 1;
                if (idx < chronicles.Count && string.IsNullOrEmpty(chronicles[idx]))
                {
                    chronicles[idx] = pc.charName + " (Tag " + World.Day + ")";
                    Notify.Announce("Die Chroniken der Genesis verzeichnen: " + pc.charName + " bezwang als Erster Etage " + Floor + " des Turms (Art. 114).");
                }
                Notify.System("Etage " + Floor + " bezwungen! Beute: " + earned + " Taler (10 % gingen an die Kasse, Art. 115)." + (first ? " Neuer Rekord." : ""));
                if (Floor < GameConstants.TowerFloors)
                {
                    var up = Build.Box(floorRoot, WorldLayout.TowerSpace + new Vector3(0f, 1.8f, 0f), new Vector3(3f, 3.6f, 0.3f), new Color(0.4f, 0.85f, 1f), false, 2.5f);
                    up.AddComponent<TowerPortal>().up = true;
                }
                else
                {
                    Notify.Announce(pc.charName + " hat den Gipfel des Turms erreicht. Alle fünfzig Etagen sind bezwungen.");
                }
            }
        }
    }

    public class TowerPortal : Interactable
    {
        public bool up;

        protected override void OnEnable()
        {
            base.OnEnable();
            radius = 3.5f;
            offset = Vector3.zero;
        }

        public override string Prompt { get { return up ? "Aufsteigen zur nächsten Etage" : "Turm verlassen"; } }

        public override void Interact(PlayerCharacter player)
        {
            if (up) World.Tower.Next();
            else World.Tower.Leave(true);
        }
    }
}
