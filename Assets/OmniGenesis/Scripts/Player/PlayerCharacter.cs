using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public class Inventory
    {
        public readonly List<ItemStack> items = new List<ItemStack>();

        public int Count(string id)
        {
            for (int i = 0; i < items.Count; i++) if (items[i].id == id) return items[i].count;
            return 0;
        }

        public void Add(string id, int n)
        {
            if (n <= 0 || Items.Get(id) == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == id) { items[i].count += n; return; }
            }
            items.Add(new ItemStack(id, n));
        }

        public bool Remove(string id, int n)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id != id) continue;
                if (items[i].count < n) return false;
                items[i].count -= n;
                if (items[i].count == 0) items.RemoveAt(i);
                return true;
            }
            return n <= 0;
        }

        public bool Has(ItemStack[] req)
        {
            foreach (var r in req) if (Count(r.id) < r.count) return false;
            return true;
        }

        public float Mass
        {
            get
            {
                float m = 0f;
                foreach (var s in items)
                {
                    var d = Items.Get(s.id);
                    if (d != null) m += d.mass * s.count;
                }
                return m;
            }
        }

        public void Load(List<ItemStack> src)
        {
            items.Clear();
            if (src == null) return;
            foreach (var s in src) if (s != null && s.count > 0 && Items.Get(s.id) != null) items.Add(new ItemStack(s.id, s.count));
        }

        public List<ItemStack> ToList()
        {
            var list = new List<ItemStack>();
            foreach (var s in items) list.Add(new ItemStack(s.id, s.count));
            return list;
        }
    }

    public class Buff
    {
        public string id;
        public string name;
        public float until;
        public float damageMult = 1f;
        public float attackSpeedMult = 1f;
        public float damageTakenMult = 1f;
        public float lifesteal;
        public float statMult = 1f;
        public float gatherMult = 1f;
        public float healPerSecond;
        public bool poison;
        public bool rune;
        public bool knockbackImmune;
        public bool parry;
    }

    // Charakterwerte, Fortschritt, Inventar und Ausrüstung des Spielers.
    public class PlayerCharacter : MonoBehaviour
    {
        public string charName;
        public RaceDef race;
        public ClassDef cls;
        public ClassDef second;
        public HybridDef hybrid;
        public int level = 1;
        public float exp;
        public int allocStr, allocAgi, allocVit, allocInt;
        public int freePoints;
        public int taler;
        public readonly Inventory inv = new Inventory();
        public readonly Inventory storage = new Inventory();
        public string weaponId = "";
        public string armorId = "";
        public int rankIndex = -1;
        public bool permit;
        public bool demo;

        public Health health;
        public PlayerMotor motor;
        public PlayerCombat combat;
        public HumanoidVisual visual;
        public float aether;
        public float stamina;
        public readonly List<Buff> buffs = new List<Buff>();
        public Creature companion;

        static readonly ItemDef Fists = new ItemDef { id = "faeuste", name = "Fäuste", category = ItemCategory.Waffe, weaponKind = WeaponKind.Nahkampf, mass = 0.6f, length = 0.6f, swingTime = 0.25f };

        // ---------- Werte ----------
        int Stat(int classBase, float g1, float g2, int raceBonus, int alloc)
        {
            int firstLevels = Mathf.Min(level, GameConstants.SecondClassLevel) - 1;
            int laterLevels = Mathf.Max(0, level - GameConstants.SecondClassLevel);
            float gLater = second != null ? (g1 + g2) * 0.5f : g1;
            int v = classBase + raceBonus + alloc + Mathf.FloorToInt(g1 * firstLevels + gLater * laterLevels);
            return Mathf.RoundToInt(v * StatMult);
        }

        float StatMult
        {
            get
            {
                float m = 1f;
                foreach (var b in buffs) m *= b.statMult;
                return m;
            }
        }

        public int Str { get { return Stat(cls.str, cls.gStr, second != null ? second.gStr : 0f, race.str, allocStr); } }
        public int Agi { get { return Stat(cls.agi, cls.gAgi, second != null ? second.gAgi : 0f, race.agi, allocAgi); } }
        public int Vit { get { return Stat(cls.vit, cls.gVit, second != null ? second.gVit : 0f, race.vit, allocVit); } }
        public int Int { get { return Stat(cls.intl, cls.gInt, second != null ? second.gInt : 0f, race.intl, allocInt); } }

        public float MaxHp { get { return 100f + Vit * 10f + level * 5f; } }
        public float MaxAether { get { return 40f + Int * 6f + level * 2f; } }
        public float MaxStamina { get { return 100f + Agi * 2f; } }
        public float Crit { get { return Mathf.Min(0.5f, 0.05f + Agi * 0.003f); } }
        public float CarryCapacity { get { return 30f + Str * 1.2f + race.carryBonus; } }
        public float BodyMass { get { return 72f * race.heightScale; } }

        public ItemDef Weapon { get { var w = Items.Get(weaponId); return w != null ? w : Fists; } }
        public ItemDef Armor { get { return Items.Get(armorId); } }

        public float EquipmentMass
        {
            get
            {
                float m = Weapon.mass;
                if (Armor != null) m += Armor.mass;
                return m;
            }
        }

        public float CarriedMass { get { return inv.Mass + EquipmentMass; } }

        public float EncumbranceFactor
        {
            get
            {
                float over = CarriedMass - CarryCapacity;
                if (over <= 0f) return 1f;
                return Mathf.Clamp(1f - over / CarryCapacity, 0.35f, 1f);
            }
        }

        public float ArmorValue { get { return (Armor != null ? Armor.armor : 0f) + Vit * 0.2f; } }

        public float DamageMult
        {
            get
            {
                float m = 1f;
                foreach (var b in buffs) m *= b.damageMult;
                return m;
            }
        }

        public float AttackSpeedMult
        {
            get
            {
                float m = 1f;
                foreach (var b in buffs) m *= b.attackSpeedMult;
                return m;
            }
        }

        public float GatherMult
        {
            get
            {
                float m = 1f;
                foreach (var b in buffs) m *= b.gatherMult;
                return m;
            }
        }

        public Buff FindBuff(string id)
        {
            foreach (var b in buffs) if (b.id == id) return b;
            return null;
        }

        public bool HasBuffFlag(System.Predicate<Buff> p)
        {
            foreach (var b in buffs) if (p(b)) return true;
            return false;
        }

        // Stärke verkürzt die Schwungzeit: t = t₀ / (1 + 0,01 · Stärke). Kürzeres t => größeres a => größeres F.
        public float SwingTime(ItemDef w)
        {
            float t = w.swingTime / (1f + Str * 0.01f) / AttackSpeedMult;
            return Mathf.Max(0.12f, t);
        }

        public string ClassTitle
        {
            get
            {
                if (hybrid != null) return hybrid.name + " (" + cls.name + " + " + second.name + ")";
                return cls.name;
            }
        }

        public static int ExpToNext(int lvl) { return Mathf.RoundToInt(40f * Mathf.Pow(lvl, 1.6f)); }

        // ---------- Lebenszyklus ----------
        public void Init(SaveData d)
        {
            health = gameObject.AddComponent<Health>();
            health.faction = Faction.Player;
            health.height = 1.8f;
            health.Died += OnDied;
            health.Damaged += OnDamaged;
            health.Modify = ModifyIncoming;
            Load(d);
        }

        void Update()
        {
            if (!World.Ready) return;
            float dt = Time.deltaTime;

            float regen = (1f + Int * 0.08f) * (1f + race.aetherRegenBonus);
            aether = Mathf.Min(MaxAether, aether + regen * dt);
            if (motor == null || !motor.IsSprinting) stamina = Mathf.Min(MaxStamina, stamina + 18f * dt);

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var b = buffs[i];
                if (b.healPerSecond > 0f) health.Heal(b.healPerSecond * dt);
                if (Time.time >= b.until)
                {
                    buffs.RemoveAt(i);
                    RefreshDerived();
                }
            }

            if (!health.IsDead) health.Heal(Vit * 0.03f * dt);
        }

        public void RefreshDerived()
        {
            float frac = health.maxHp > 0f ? health.hp / health.maxHp : 1f;
            health.maxHp = MaxHp;
            health.hp = Mathf.Clamp(frac * health.maxHp, health.IsDead ? 0f : 1f, health.maxHp);
            health.armor = ArmorValue;
            health.mass = BodyMass + EquipmentMass;
            health.displayName = charName;
            aether = Mathf.Min(aether, MaxAether);
        }

        DamageInfo ModifyIncoming(DamageInfo info)
        {
            foreach (var b in buffs)
            {
                if (b.parry && info.source != null && info.amount > 0f)
                {
                    b.parry = false;
                    b.until = Time.time;
                    Notify.Combat("Parade! Angriff abgewehrt — Konter mit dreifacher Kraft.");
                    if (combat != null) combat.Counter(info.source);
                    info.amount = 0f;
                    return info;
                }
            }
            float m = 1f;
            foreach (var b in buffs) m *= b.damageTakenMult;
            info.amount *= m;
            return info;
        }

        void OnDamaged(DamageInfo info)
        {
            if (World.Hud != null && info.amount > 0f) World.Hud.OnPlayerHit(info);
        }

        void OnDied(DamageInfo info)
        {
            StartCoroutine(Downed());
        }

        System.Collections.IEnumerator Downed()
        {
            yield return new WaitForSeconds(1.2f);
            // Prüfung in der Arena: Niederlage ist nicht tödlich.
            if (World.Trial != null && World.Trial.Active)
            {
                health.Revive(0.3f);
                World.Trial.OnPlayerDowned();
                yield break;
            }
            // Gesuchte werden festgenommen und vor Gericht gestellt (Art. 29).
            if (World.Law != null && World.Law.Wanted > 0)
            {
                health.Revive(0.5f);
                World.Law.Arrest();
                yield break;
            }
            int lost = Mathf.RoundToInt(taler * 0.1f);
            taler -= lost;
            health.Revive(1f);
            aether = MaxAether;
            if (World.Clock != null) World.Clock.Advance(120f);
            motor.Teleport(WorldBootstrap.LazarettSpawn, 0f);
            Notify.System("Du bist gefallen. Die Heiler des Kaiserlichen Lazaretts haben dich gerettet (Art. 45). Verlust: " + lost + " Taler.");
        }

        // ---------- Fortschritt ----------
        public void GainExp(float amount)
        {
            if (level >= GameConstants.MaxLevel || amount <= 0f) return;
            exp += amount * (1f + race.expBonus);
            while (level < GameConstants.MaxLevel && exp >= ExpToNext(level))
            {
                exp -= ExpToNext(level);
                level++;
                freePoints += GameConstants.StatPointsPerLevel;
                RefreshDerived();
                health.hp = health.maxHp;
                aether = MaxAether;
                Notify.System("Stufe " + level + " erreicht! +" + GameConstants.StatPointsPerLevel + " Attributpunkte (Taste C).");
                if (World.Hud != null) World.Hud.Banner("STUFE " + level, new Color(0.45f, 0.9f, 1f));
                if (level == GameConstants.SecondClassLevel && second == null)
                    Notify.System("Stufe 50: Du kannst eine zweite Klasse wählen. Öffne das Charakterfenster (C).");
                if (level == GameConstants.EmperorLevel)
                    Notify.System("Du hast die Stufe des Kaisers " + GameConstants.EmperorName + " erreicht. Die Säule der Kraft steht.");
            }
            if (level >= GameConstants.MaxLevel) exp = 0f;
        }

        public void Allocate(int stat)
        {
            if (freePoints <= 0) return;
            freePoints--;
            if (stat == 0) allocStr++;
            else if (stat == 1) allocAgi++;
            else if (stat == 2) allocVit++;
            else allocInt++;
            RefreshDerived();
        }

        public bool ChooseSecondClass(string id)
        {
            if (level < GameConstants.SecondClassLevel || second != null || id == cls.id) return false;
            var h = Classes.Hybrid(cls.id, id);
            if (h == null) return false;
            second = Classes.Get(id);
            hybrid = h;
            RefreshDerived();
            Notify.System("Neue Klasse: " + hybrid.name + "! Fähigkeit [R]: " + hybrid.skillR.name + ".");
            if (World.Hud != null) World.Hud.Banner(hybrid.name.ToUpper(), new Color(1f, 0.8f, 0.35f));
            return true;
        }

        public void AddTaler(int amount)
        {
            taler += amount;
        }

        public bool TrySpend(int amount)
        {
            if (taler < amount) return false;
            taler -= amount;
            return true;
        }

        public void AddBuff(Buff b)
        {
            for (int i = buffs.Count - 1; i >= 0; i--) if (buffs[i].id == b.id) buffs.RemoveAt(i);
            buffs.Add(b);
            RefreshDerived();
        }

        public void Equip(string id)
        {
            var d = Items.Get(id);
            if (d == null || inv.Count(id) <= 0) return;
            if (d.category == ItemCategory.Waffe)
            {
                inv.Remove(id, 1);
                if (Items.Get(weaponId) != null) inv.Add(weaponId, 1);
                weaponId = id;
                if (visual != null) visual.SetWeapon(Weapon);
            }
            else if (d.category == ItemCategory.Ruestung)
            {
                inv.Remove(id, 1);
                if (Items.Get(armorId) != null) inv.Add(armorId, 1);
                armorId = id;
            }
            RefreshDerived();
        }

        public void Unequip(bool weapon)
        {
            if (weapon && Items.Get(weaponId) != null)
            {
                inv.Add(weaponId, 1);
                weaponId = "";
                if (visual != null) visual.SetWeapon(Weapon);
            }
            else if (!weapon && Items.Get(armorId) != null)
            {
                inv.Add(armorId, 1);
                armorId = "";
            }
            RefreshDerived();
        }

        public bool Use(string id)
        {
            var d = Items.Get(id);
            if (d == null || d.category != ItemCategory.Verbrauch || inv.Count(id) <= 0) return false;
            if (health.IsDead) return false;
            inv.Remove(id, 1);
            if (d.heal > 0f) health.Heal(d.heal + MaxHp * 0.1f);
            if (d.aether > 0f) aether = Mathf.Min(MaxAether, aether + d.aether + MaxAether * 0.1f);
            Notify.Combat(d.name + " benutzt.");
            return true;
        }

        // ---------- Speichern ----------
        public void Load(SaveData d)
        {
            charName = string.IsNullOrEmpty(d.name) ? "Reisender" : d.name;
            race = Races.Get(d.race);
            cls = Classes.Get(d.classId);
            second = string.IsNullOrEmpty(d.secondClassId) ? null : Classes.Get(d.secondClassId);
            hybrid = second != null ? Classes.Hybrid(cls.id, second.id) : null;
            if (hybrid == null) second = null;
            level = Mathf.Clamp(d.level, 1, GameConstants.MaxLevel);
            exp = d.exp;
            allocStr = d.allocStr; allocAgi = d.allocAgi; allocVit = d.allocVit; allocInt = d.allocInt;
            freePoints = d.freePoints;
            taler = d.taler;
            inv.Load(d.inventory);
            storage.Load(d.storage);
            weaponId = d.weapon;
            armorId = d.armor;
            rankIndex = d.rankIndex;
            permit = d.permit;
            demo = d.demoMode;

            health.maxHp = MaxHp;
            health.hp = d.hp > 0f ? Mathf.Min(d.hp, MaxHp) : MaxHp;
            aether = d.aether >= 0f ? Mathf.Min(d.aether, MaxAether) : MaxAether;
            stamina = MaxStamina;
            RefreshDerived();
        }

        public void Save(SaveData d)
        {
            d.name = charName;
            d.race = race.id;
            d.classId = cls.id;
            d.secondClassId = second != null ? second.id : "";
            d.level = level;
            d.exp = exp;
            d.allocStr = allocStr; d.allocAgi = allocAgi; d.allocVit = allocVit; d.allocInt = allocInt;
            d.freePoints = freePoints;
            d.taler = taler;
            d.inventory = inv.ToList();
            d.storage = storage.ToList();
            d.weapon = weaponId;
            d.armor = armorId;
            d.rankIndex = rankIndex;
            d.permit = permit;
            d.demoMode = demo;
            d.hp = health.hp;
            d.aether = aether;
            d.companionSpecies = companion != null && !companion.Health.IsDead ? companion.def.id : "";
        }
    }
}
