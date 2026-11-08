using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Populationen der Provinz. Getötete Tiere kehren nie zurück — Bestände wachsen nur durch Nachwuchs
    // der Überlebenden (logistisches Wachstum bis zur Tragfähigkeit K). Wölfe jagen Grasläufer und verhungern ohne Beute.
    public class Ecosystem : MonoBehaviour
    {
        readonly Dictionary<string, List<Creature>> pop = new Dictionary<string, List<Creature>>();

        public void Register(Creature c)
        {
            List<Creature> list;
            if (!pop.TryGetValue(c.def.id, out list)) { list = new List<Creature>(); pop[c.def.id] = list; }
            if (!list.Contains(c)) list.Add(c);
        }

        public void Unregister(Creature c)
        {
            List<Creature> list;
            if (c != null && c.def != null && pop.TryGetValue(c.def.id, out list)) list.Remove(c);
        }

        public int Count(string id)
        {
            List<Creature> list;
            if (!pop.TryGetValue(id, out list)) return 0;
            list.RemoveAll(c => c == null || c.IsCorpse);
            return list.Count;
        }

        public float StockFraction(string id)
        {
            var def = Species.Get(id);
            if (def == null || def.capacity <= 0) return 1f;
            return Count(id) / (float)def.capacity;
        }

        public void Init(SaveData d, WorldClock clock)
        {
            clock.HourPassed += OnHour;
            clock.DayStarted += OnDay;
            if (!d.worldInitialized)
            {
                foreach (var def in Species.All)
                    if (def.ecosystem) for (int i = 0; i < def.initial; i++) SpawnInHabitat(def, Random.Range(24f, 200f));
            }
            else
            {
                foreach (var cs in d.creatures)
                {
                    var def = Species.Get(cs.species);
                    if (def == null || !def.ecosystem) continue;
                    var c = Creature.Spawn(def, cs.position, cs.age);
                    c.hunger = cs.hunger;
                    c.SetHome(HabitatCenter(def));
                }
            }
        }

        static Vector3 HabitatCenter(SpeciesDef def)
        {
            return new Vector3(def.habitat.x, Land.Height(def.habitat.x, def.habitat.y), def.habitat.y);
        }

        public static bool HabitatPoint(SpeciesDef def, out Vector3 point)
        {
            for (int i = 0; i < 25; i++)
            {
                Vector2 p = def.habitat + Random.insideUnitCircle * def.habitatRadius;
                bool ok = def.diet == Diet.Leere ? p.y > WorldLayout.ForbiddenZ + 5f : WorldBuilder.FreeLand(p);
                if (!ok) continue;
                point = new Vector3(p.x, Land.Height(p.x, p.y) + 0.5f, p.y);
                return true;
            }
            point = HabitatCenter(def);
            return false;
        }

        Creature SpawnInHabitat(SpeciesDef def, float age)
        {
            Vector3 p;
            HabitatPoint(def, out p);
            var c = Creature.Spawn(def, p, age);
            c.SetHome(HabitatCenter(def));
            return c;
        }

        // Fern vom Spieler ruht die KI (Creature.Update). Dort jagen Raubtiere abstrakt, damit sie nicht verhungern.
        void OffscreenHunt()
        {
            var player = World.Player;
            System.Func<Creature, bool> unseen = c => player == null || (c.transform.position - player.transform.position).magnitude > 60f;
            var prey = new List<Creature>();
            foreach (var kv in pop)
            {
                var d = Species.Get(kv.Key);
                if (d.diet != Diet.Pflanzenfresser || d.temper != Temper.Scheu) continue;
                foreach (var c in kv.Value) if (c != null && !c.IsCorpse && unseen(c)) prey.Add(c);
            }
            var hunters = new List<Creature>();
            foreach (var kv in pop)
                if (Species.Get(kv.Key).diet == Diet.Fleischfresser)
                    foreach (var w in kv.Value) if (w != null && !w.IsCorpse && w.hunger >= 0.6f && unseen(w)) hunters.Add(w);
            foreach (var w in hunters)
            {
                if (prey.Count == 0) break;
                float st = Mathf.Min(1f, StockFraction("graslaeufer"));
                if (Random.value >= 0.3f * st * st) continue;   // Jagderfolg sinkt stark, wenn Beute knapp ist
                int k = Random.Range(0, prey.Count);
                var victim = prey[k];
                prey.RemoveAt(k);
                victim.Health.Apply(new DamageInfo { amount = victim.Health.maxHp * 10f, point = victim.Health.Center, source = w.Health });
            }
        }

        void OnHour(int day, int hour)
        {
            OffscreenHunt();
            bool huntProtection = World.Autonomy != null && World.Autonomy.HasLaw("jagdschutz");
            foreach (var kv in pop)
            {
                var def = Species.Get(kv.Key);
                var list = kv.Value;
                list.RemoveAll(c => c == null || c.IsCorpse);

                int adults = 0;
                float hungerSum = 0f;
                foreach (var c in list.ToArray())
                {
                    c.AgeHours(1f);
                    if (!c.IsJuvenile) adults++;
                    if (def.diet == Diet.Fleischfresser)
                    {
                        c.hunger = Mathf.Min(1f, c.hunger + 1f / 40f);
                        hungerSum += c.hunger;
                        if (c.hunger >= 1f)
                        {
                            c.starvingHours++;
                            if (c.starvingHours >= 24f)
                                c.Health.Apply(new DamageInfo { amount = c.Health.maxHp * 10f, point = c.Health.Center });
                        }
                    }
                }

                int n = list.Count;
                if (adults < 2 || n >= def.capacity) continue;
                if (def.diet == Diet.Fleischfresser && hungerSum / Mathf.Max(1, n) > 0.5f) continue;
                float p = def.breedChancePerHour * (1f - n / (float)def.capacity) * (adults / 2f);
                if (huntProtection) p *= 1.2f;
                if (Random.value < Mathf.Min(0.9f, p))
                {
                    var parent = list[Random.Range(0, list.Count)];
                    Vector3 pos = parent.transform.position + new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-2f, 2f));
                    var young = Creature.Spawn(def, pos, 0f);
                    young.SetHome(HabitatCenter(def));
                }
            }

            if (World.Clock != null)
            {
                foreach (var c in Creature.Instances.ToArray())
                    if (c != null && c.IsCorpse && World.Clock.minutes - c.deathMinute > 180f) Destroy(c.gameObject);
            }
        }

        void OnDay(int day)
        {
            if (!GameConstants.MigrationEnabled) return;
            foreach (var def in Species.All)
            {
                if (!def.ecosystem || Count(def.id) > 0) continue;
                if (Random.value > GameConstants.MigrationChancePerDay) continue;
                for (int i = 0; i < 2; i++) SpawnInHabitat(def, 48f);
                Notify.System("Zuwanderung: zwei " + def.name + " sind aus den Nachbarprovinzen eingewandert. Getötete Tiere kehren nie zurück — nur Fremde kommen nach.");
            }
        }

        public void Save(SaveData d)
        {
            d.creatures.Clear();
            foreach (var kv in pop)
                foreach (var c in kv.Value)
                    if (c != null && !c.IsCorpse && !c.managed) d.creatures.Add(c.ToSave());
        }

        public string Census()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var def in Species.All)
            {
                if (!def.ecosystem) continue;
                sb.Append(def.name).Append(": ").Append(Count(def.id)).Append(" / ").Append(def.capacity).Append('\n');
            }
            return sb.ToString();
        }
    }
}
