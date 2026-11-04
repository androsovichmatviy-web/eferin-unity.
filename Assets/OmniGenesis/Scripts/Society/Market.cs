using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Kaiserliche Kasse (Art. 82, 93, 115, 117).
    public class Treasury
    {
        public long Balance = 250000;
        public readonly Dictionary<string, long> income = new Dictionary<string, long>();
        public readonly Dictionary<string, long> expenses = new Dictionary<string, long>();

        public void Add(string category, long amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            long v;
            income.TryGetValue(category, out v);
            income[category] = v + amount;
        }

        public bool Pay(long amount, string category)
        {
            if (amount <= 0) return true;
            if (Balance < amount) return false;
            Balance -= amount;
            long v;
            expenses.TryGetValue(category, out v);
            expenses[category] = v + amount;
            return true;
        }

        public void Save(SaveData d)
        {
            d.treasury.Clear();
            d.treasury.Add(new FloatEntry { key = "balance", value = Balance });
            foreach (var kv in income) d.treasury.Add(new FloatEntry { key = "in:" + kv.Key, value = kv.Value });
            foreach (var kv in expenses) d.treasury.Add(new FloatEntry { key = "out:" + kv.Key, value = kv.Value });
        }

        public void Load(SaveData d)
        {
            foreach (var e in d.treasury)
            {
                if (e.key == "balance") Balance = (long)e.value;
                else if (e.key.StartsWith("in:")) income[e.key.Substring(3)] = (long)e.value;
                else if (e.key.StartsWith("out:")) expenses[e.key.Substring(4)] = (long)e.value;
            }
        }
    }

    // Markt mit Angebot und Nachfrage. Preis = Grundpreis · (Sollbestand / Bestand)^0,7.
    // Händler, Jäger, Holzfäller, Schmiede und Alchemisten der Stadt produzieren und verbrauchen stündlich.
    public class Market : MonoBehaviour
    {
        public class Good
        {
            public string id;
            public float stock;
            public float target;
            public float lastPrice;
        }

        public readonly List<Good> goods = new List<Good>();

        public bool IsOpen { get { return World.Clock == null || (World.Clock.Hour >= 6 && World.Clock.Hour < 22); } }

        public void Init(SaveData d, WorldClock clock)
        {
            foreach (var item in Items.All)
            {
                if (!item.tradable) continue;
                float target = item.category == ItemCategory.Material ? 40f : item.category == ItemCategory.Verbrauch ? 20f : 4f;
                var g = new Good { id = item.id, stock = target, target = target };
                goods.Add(g);
            }
            foreach (var e in d.market)
            {
                var g = Get(e.key);
                if (g != null) g.stock = e.value;
            }
            foreach (var g in goods) g.lastPrice = Price(g.id);
            clock.HourPassed += OnHour;
        }

        public Good Get(string id)
        {
            foreach (var g in goods) if (g.id == id) return g;
            return null;
        }

        public float Price(string id)
        {
            var g = Get(id);
            var item = Items.Get(id);
            if (g == null || item == null) return 0f;
            float ratio = g.target / Mathf.Max(g.stock, 0.5f);
            return item.price * Mathf.Clamp(Mathf.Pow(ratio, 0.7f), 0.3f, 5f);
        }

        public int BuyPrice(string id) { return Mathf.Max(1, Mathf.CeilToInt(Price(id) * 1.1f)); }
        public int SellPrice(string id) { return Mathf.Max(0, Mathf.FloorToInt(Price(id) * 0.9f)); }

        public float Trend(string id)
        {
            var g = Get(id);
            return g == null || g.lastPrice <= 0f ? 1f : Price(id) / g.lastPrice;
        }

        float TaxRate
        {
            get
            {
                // In einer eigenen Autonomie mit Marktfrieden gilt der Vertragssatz von 5 % (Art. 117).
                if (World.Autonomy != null && World.Autonomy.IsAutonomous && World.Autonomy.HasLaw("marktfrieden") && WorldLayout.InVillageRegion(World.Player.transform.position)) return 0.05f;
                return GameConstants.TradeTax;
            }
        }

        public string Buy(PlayerCharacter pc, string id)
        {
            if (!IsOpen) return "Der Markt ist geschlossen (6–22 Uhr).";
            var g = Get(id);
            if (g == null || g.stock < 1f) return "Ausverkauft.";
            int price = BuyPrice(id);
            if (!pc.TrySpend(price)) return "Nicht genug Taler.";
            g.stock -= 1f;
            pc.inv.Add(id, 1);
            World.Treasury.Add("Handelsabgabe", Mathf.RoundToInt(price * TaxRate));
            return null;
        }

        public string Sell(PlayerCharacter pc, string id)
        {
            if (!IsOpen) return "Der Markt ist geschlossen (6–22 Uhr).";
            var g = Get(id);
            if (g == null) return "Das kauft hier niemand.";
            if (!pc.inv.Remove(id, 1)) return "Nicht im Inventar.";
            int gross = SellPrice(id);
            int tax = Mathf.RoundToInt(gross * TaxRate);
            pc.AddTaler(gross - tax);
            g.stock += 1f;
            World.Treasury.Add("Handelsabgabe", tax);
            return null;
        }

        void Change(string id, float delta)
        {
            var g = Get(id);
            if (g != null) g.stock = Mathf.Clamp(g.stock + delta, 0f, g.target * 4f);
        }

        float Stock(string id)
        {
            var g = Get(id);
            return g != null ? g.stock : 0f;
        }

        void OnHour(int day, int hour)
        {
            if (hour == 0) foreach (var g in goods) g.lastPrice = Price(g.id);
            if (hour < 6 || hour >= 22) return;

            float prod = World.Weather != null ? World.Weather.ProductionMult : 1f;
            float deer = World.Eco != null ? Mathf.Clamp01(World.Eco.StockFraction("graslaeufer")) : 1f;
            float trees = World.Nodes != null ? World.Nodes.TreeFraction : 1f;
            float growth = World.Weather != null ? World.Weather.GrowthMult : 1f;
            float war = World.Rift != null && World.Rift.Summons ? 2f : 1f;

            // Jäger: abhängig vom echten Wildbestand im Ökosystem.
            Change("fleisch", 1.6f * deer * prod - 1.4f * war);
            Change("fell", 0.5f * deer * prod - 0.3f);
            // Holzfäller: abhängig von den stehenden Bäumen.
            Change("holz", 1.2f * trees * prod - 1f);
            Change("stein", 0.7f * prod - 0.5f);
            Change("eisenerz", 0.6f * prod);
            // Schmiede verarbeiten Erz zu Barren.
            if (Stock("eisenerz") >= 2f && Stock("holz") >= 1f)
            {
                Change("eisenerz", -2f);
                Change("holz", -0.5f);
                Change("eisenbarren", 1f * prod);
            }
            Change("eisenbarren", -0.4f);
            Change("heilkraut", 0.6f * growth * prod - 0.3f);
            if (hour % 2 == 0 && Stock("heilkraut") >= 3f)
            {
                Change("heilkraut", -3f);
                Change("heiltrank", 1f * prod);
            }
            Change("heiltrank", -0.35f * war);
            Change("aetherelixier", 0.1f * prod - 0.1f);
            if (Stock("fleisch") > 3f)
            {
                Change("fleisch", -0.4f);
                Change("gebratenes_fleisch", 0.4f);
            }
            Change("gebratenes_fleisch", -0.5f);
            Change("aetherkristall", 0.05f * prod - 0.03f);
            Change("leerenessenz", -0.02f);
            foreach (var g in goods)
            {
                var item = Items.Get(g.id);
                if (item.category == ItemCategory.Waffe || item.category == ItemCategory.Ruestung) Change(g.id, 0.05f * prod - 0.04f);
            }
            // Händler gleichen Überschuss und Mangel langsam über das Handelsnetz aus.
            foreach (var g in goods) Change(g.id, (g.target - g.stock) * 0.03f);
        }

        public void Save(SaveData d)
        {
            d.market.Clear();
            foreach (var g in goods) d.market.Add(new FloatEntry { key = g.id, value = g.stock });
        }
    }
}
