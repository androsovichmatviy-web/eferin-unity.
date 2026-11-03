using System.Collections.Generic;

namespace OmniGenesis
{
    public enum ItemCategory { Material, Waffe, Ruestung, Verbrauch, Dokument }
    public enum WeaponKind { Nahkampf, Stab, Bogen }

    public class ItemDef
    {
        public string id;
        public string name;
        public ItemCategory category;
        public float mass;          // kg — fließt in F = m · a und in die Traglast ein
        public int price;
        public string description = "";
        public bool tradable = true;

        // Waffen
        public WeaponKind weaponKind;
        public float length;        // m (Nahkampf)
        public float swingTime;     // s bei Stärke 0
        public float drawForce;     // N (Bogen)
        public float focus = 1f;    // Stab: Verstärkung der Ätherbeschleunigung

        // Rüstung
        public float armor;

        // Verbrauch
        public float heal;
        public float aether;
    }

    public static class Items
    {
        static readonly Dictionary<string, ItemDef> byId = new Dictionary<string, ItemDef>();
        public static readonly List<ItemDef> All = new List<ItemDef>();

        static Items()
        {
            // Materialien
            Mat("holz", "Holz", 2f, 4);
            Mat("stein", "Stein", 3f, 3);
            Mat("eisenerz", "Eisenerz", 3f, 9);
            Mat("eisenbarren", "Eisenbarren", 1.5f, 25);
            Mat("heilkraut", "Heilkraut", 0.1f, 6);
            Mat("fleisch", "Fleisch", 1f, 8);
            Mat("fell", "Fell", 1.5f, 12);
            Mat("aetherkristall", "Ätherkristall", 0.2f, 60);
            Mat("leerenessenz", "Leerenessenz", 0.3f, 45);

            // Nahkampfwaffen: Masse, Länge, Schwungzeit
            Melee("holzknueppel", "Holzknüppel", 1.2f, 0.8f, 0.36f, 5);
            Melee("kurzschwert", "Kurzschwert", 1.1f, 0.8f, 0.28f, 40);
            Melee("langschwert", "Langschwert", 1.5f, 1.05f, 0.32f, 120);
            Melee("grossschwert", "Großschwert", 3.5f, 1.4f, 0.5f, 260);
            Melee("kriegsaxt", "Kriegsaxt", 2.8f, 1.1f, 0.46f, 150);
            Melee("dolche", "Dolche", 0.5f, 0.4f, 0.16f, 60);

            // Stäbe: Fokus verstärkt die Ätherbeschleunigung
            Add(new ItemDef { id = "lehrlingsstab", name = "Lehrlingsstab", category = ItemCategory.Waffe, weaponKind = WeaponKind.Stab, mass = 1.6f, focus = 1f, price = 40 });
            Add(new ItemDef { id = "aetherstab", name = "Ätherstab", category = ItemCategory.Waffe, weaponKind = WeaponKind.Stab, mass = 1.8f, focus = 1.4f, price = 220 });

            // Bögen: Zugkraft in Newton
            Add(new ItemDef { id = "jagdbogen", name = "Jagdbogen", category = ItemCategory.Waffe, weaponKind = WeaponKind.Bogen, mass = 0.9f, drawForce = 110f, price = 45 });
            Add(new ItemDef { id = "langbogen", name = "Langbogen", category = ItemCategory.Waffe, weaponKind = WeaponKind.Bogen, mass = 1.3f, drawForce = 180f, price = 140 });

            // Rüstungen
            Add(new ItemDef { id = "stoffkleidung", name = "Stoffkleidung", category = ItemCategory.Ruestung, mass = 2f, armor = 2f, price = 10 });
            Add(new ItemDef { id = "lederruestung", name = "Lederrüstung", category = ItemCategory.Ruestung, mass = 6f, armor = 6f, price = 80 });
            Add(new ItemDef { id = "eisenruestung", name = "Eisenrüstung", category = ItemCategory.Ruestung, mass = 15f, armor = 16f, price = 300 });

            // Verbrauch
            Add(new ItemDef { id = "heiltrank", name = "Heiltrank", category = ItemCategory.Verbrauch, mass = 0.3f, heal = 60f, price = 30 });
            Add(new ItemDef { id = "aetherelixier", name = "Ätherelixier", category = ItemCategory.Verbrauch, mass = 0.3f, aether = 50f, price = 50 });
            Add(new ItemDef { id = "gebratenes_fleisch", name = "Gebratenes Fleisch", category = ItemCategory.Verbrauch, mass = 0.5f, heal = 25f, price = 12 });

            // Dokumente
            Add(new ItemDef { id = "erlaubnis", name = "Kaiserliche Erlaubnis", category = ItemCategory.Dokument, mass = 0f, price = 0, tradable = false,
                description = "Art. 112: Erlaubt das Betreten der Verbotenen Lande." });
        }

        static void Mat(string id, string name, float mass, int price)
        {
            Add(new ItemDef { id = id, name = name, category = ItemCategory.Material, mass = mass, price = price });
        }

        static void Melee(string id, string name, float mass, float length, float swing, int price)
        {
            Add(new ItemDef { id = id, name = name, category = ItemCategory.Waffe, weaponKind = WeaponKind.Nahkampf, mass = mass, length = length, swingTime = swing, price = price });
        }

        static void Add(ItemDef d)
        {
            byId[d.id] = d;
            All.Add(d);
        }

        public static ItemDef Get(string id)
        {
            ItemDef d;
            if (!string.IsNullOrEmpty(id) && byId.TryGetValue(id, out d)) return d;
            return null;
        }

        public static string Name(string id)
        {
            var d = Get(id);
            return d != null ? d.name : id;
        }
    }

    public class Recipe
    {
        public string result;
        public int count = 1;
        public ItemStack[] inputs;
        public bool forge;
    }

    public static class Recipes
    {
        public static readonly List<Recipe> All = new List<Recipe>
        {
            R("eisenbarren", 1, true, "eisenerz", 2, "holz", 1),
            R("langschwert", 1, true, "eisenbarren", 3, "holz", 1),
            R("grossschwert", 1, true, "eisenbarren", 6, "holz", 2),
            R("kriegsaxt", 1, true, "eisenbarren", 4, "holz", 2),
            R("dolche", 1, true, "eisenbarren", 2, "fell", 1),
            R("eisenruestung", 1, true, "eisenbarren", 8, "fell", 2),
            R("langbogen", 1, false, "holz", 4, "fell", 1),
            R("aetherstab", 1, false, "holz", 3, "aetherkristall", 1),
            R("lederruestung", 1, false, "fell", 4),
            R("heiltrank", 1, false, "heilkraut", 3),
            R("aetherelixier", 1, false, "aetherkristall", 1, "heilkraut", 2),
            R("gebratenes_fleisch", 1, false, "fleisch", 1, "holz", 1),
        };

        static Recipe R(string result, int count, bool forge, params object[] pairs)
        {
            var inputs = new ItemStack[pairs.Length / 2];
            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = new ItemStack((string)pairs[i * 2], (int)pairs[i * 2 + 1]);
            return new Recipe { result = result, count = count, forge = forge, inputs = inputs };
        }
    }
}
