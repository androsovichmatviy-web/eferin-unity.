using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum NodeKind { Baum, Erz, Fels, Kraut, Aether }

    // Abbaubare Ressource. Erschöpfte Knoten wachsen nach Spielstunden nach (Regen beschleunigt Bäume und Kräuter).
    public class ResourceNode : Interactable
    {
        public int id;
        public NodeKind kind;
        public int hitsLeft;
        public float regrowHours;

        public bool Depleted { get { return regrowHours > 0f; } }

        public static int Hits(NodeKind k)
        {
            switch (k)
            {
                case NodeKind.Baum: return 3;
                case NodeKind.Erz: return 4;
                case NodeKind.Fels: return 3;
                case NodeKind.Kraut: return 1;
                default: return 5;
            }
        }

        public static float RegrowTime(NodeKind k)
        {
            switch (k)
            {
                case NodeKind.Baum: return 48f;
                case NodeKind.Erz: return 72f;
                case NodeKind.Fels: return 72f;
                case NodeKind.Kraut: return 18f;
                default: return 96f;
            }
        }

        public static string Label(NodeKind k)
        {
            switch (k)
            {
                case NodeKind.Baum: return "Baum fällen";
                case NodeKind.Erz: return "Erz abbauen";
                case NodeKind.Fels: return "Stein brechen";
                case NodeKind.Kraut: return "Heilkraut pflücken";
                default: return "Ätherkristall lösen";
            }
        }

        public override string Prompt
        {
            get
            {
                int total = Hits(kind);
                return total > 1 ? Label(kind) + " (" + (total - hitsLeft + 1) + "/" + total + ")" : Label(kind);
            }
        }

        public override void Interact(PlayerCharacter player)
        {
            if (player.visual != null) player.visual.swing = 0.6f;
            hitsLeft--;
            Fx.Burst(Point, kind == NodeKind.Baum ? new Color(0.45f, 0.3f, 0.2f) : kind == NodeKind.Kraut ? new Color(0.4f, 0.8f, 0.3f) : new Color(0.6f, 0.6f, 0.6f), 5);
            player.GainExp(2f);
            if (hitsLeft > 0) return;

            int m = Mathf.Max(1, Mathf.RoundToInt(player.GatherMult));
            switch (kind)
            {
                case NodeKind.Baum: Give(player, "holz", 3 * m); break;
                case NodeKind.Erz: Give(player, "eisenerz", 2 * m); Give(player, "stein", 1 * m); break;
                case NodeKind.Fels: Give(player, "stein", 3 * m); break;
                case NodeKind.Kraut: Give(player, "heilkraut", 2 * m); break;
                case NodeKind.Aether: Give(player, "aetherkristall", 1 * m); break;
            }
            SetDepleted(RegrowTime(kind));
        }

        static void Give(PlayerCharacter p, string id, int n)
        {
            p.inv.Add(id, n);
            Notify.Combat("+" + n + " " + Items.Name(id));
        }

        public void SetDepleted(float hours)
        {
            regrowHours = hours;
            hitsLeft = Hits(kind);
            gameObject.SetActive(hours <= 0f);
        }
    }

    public class ResourceNodes : MonoBehaviour
    {
        readonly List<ResourceNode> nodes = new List<ResourceNode>();
        int treeCount;

        public void Register(GameObject go, NodeKind kind)
        {
            var n = go.AddComponent<ResourceNode>();
            n.id = nodes.Count;
            n.kind = kind;
            n.hitsLeft = ResourceNode.Hits(kind);
            n.radius = kind == NodeKind.Baum ? 3.2f : 2.6f;
            n.offset = kind == NodeKind.Baum ? Vector3.up * 1.2f : Vector3.up * 0.5f;
            nodes.Add(n);
            if (kind == NodeKind.Baum) treeCount++;
        }

        public void Init(WorldClock clock)
        {
            clock.HourPassed += OnHour;
        }

        void OnHour(int day, int hour)
        {
            float growth = World.Weather != null ? World.Weather.GrowthMult : 1f;
            foreach (var n in nodes)
            {
                if (!n.Depleted) continue;
                bool plant = n.kind == NodeKind.Baum || n.kind == NodeKind.Kraut;
                n.regrowHours -= plant ? growth : 1f;
                if (n.regrowHours <= 0f) n.SetDepleted(0f);
            }
        }

        // Anteil stehender Bäume — die Holzfäller des Marktes richten sich danach.
        public float TreeFraction
        {
            get
            {
                if (treeCount == 0) return 1f;
                int alive = 0;
                foreach (var n in nodes) if (n.kind == NodeKind.Baum && !n.Depleted) alive++;
                return alive / (float)treeCount;
            }
        }

        public void Save(SaveData d)
        {
            d.depletedNodes.Clear();
            foreach (var n in nodes)
                if (n.Depleted) d.depletedNodes.Add(new FloatEntry { key = n.id.ToString(), value = n.regrowHours });
        }

        public void Load(SaveData d)
        {
            foreach (var e in d.depletedNodes)
            {
                int id;
                if (int.TryParse(e.key, out id) && id >= 0 && id < nodes.Count) nodes[id].SetDepleted(e.value);
            }
        }
    }
}
