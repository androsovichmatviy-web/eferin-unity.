using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum CreatureState { Idle, Wander, Flee, Chase, Windup, Recover, Sleep, Home, Dead }

    // Lebewesen mit Rigidbody. Bewegung über Kräfte: F = m · (v_soll − v) / τ, also F = m · a.
    public class Creature : Interactable
    {
        public static readonly List<Creature> Instances = new List<Creature>();

        public SpeciesDef def;
        public Health Health { get; private set; }
        public float age;               // Spielstunden
        public float hunger;            // 0 = satt, 1 = hungrig
        public float starvingHours;
        public bool managed;            // Prüfung, Kult, Riss — nicht Teil des Ökosystems
        public Action<Creature> onDeath;
        public bool IsCorpse { get; private set; }
        public bool Tamed { get; private set; }
        public bool Raider;
        public float deathMinute;

        public float damageMult = 1f;
        public float expMult = 1f;
        public string questTag;         // von der Geschichte erzeugt
        public float summonInterval;    // Anführer rufen Leerenbrut herbei
        float nextSummon;

        Rigidbody rb;
        Collider col;
        Vector3 home;
        CreatureState state = CreatureState.Wander;
        Health target;
        float thinkTimer;
        float stateUntil;
        Vector3 wanderTarget;
        Vector3 fleeFrom;
        float slowMult = 1f, slowUntil;
        float poisonDps, poisonUntil, poisonTick;
        float undeadUntil = -1f;
        float flash;
        Transform body;
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly List<Transform> legs = new List<Transform>();
        Color tint;
        float legPhase;
        Faction baseFaction;

        const float Tau = 0.2f;
        const float MaxAccel = 30f;

        public bool IsJuvenile { get { return def.ecosystem && !managed && age < 24f; } }
        public bool IsAlly { get { return Health != null && Health.faction == Faction.Ally; } }
        public bool Hostile
        {
            get
            {
                if (IsCorpse || IsAlly) return false;
                return def.temper != Temper.Scheu || def.diet == Diet.Leere || def.diet == Diet.Konstrukt || def.diet == Diet.Kult;
            }
        }

        protected override void OnEnable() { base.OnEnable(); Instances.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Instances.Remove(this); }

        // ---------- Erzeugen ----------
        public static Creature Spawn(SpeciesDef def, Vector3 pos, float age)
        {
            var c = Create(def, pos, def.diet == Diet.Leere || def.diet == Diet.Kult ? Faction.Void : def.diet == Diet.Konstrukt ? Faction.Construct : Faction.Wild);
            c.age = age;
            c.managed = !def.ecosystem;
            if (def.ecosystem && World.Eco != null) World.Eco.Register(c);
            c.ApplyGrowth();
            return c;
        }

        public static Creature SpawnManaged(SpeciesDef def, Vector3 pos, float hpMult, float dmgMult, float expMult, Faction faction)
        {
            var c = Create(def, pos, faction);
            c.managed = true;
            c.age = 100f;
            c.Health.maxHp = def.hp * hpMult;
            c.Health.hp = c.Health.maxHp;
            c.damageMult = dmgMult;
            c.expMult = expMult;
            c.ApplyGrowth();
            return c;
        }

        static Creature Create(SpeciesDef def, Vector3 pos, Faction faction)
        {
            var go = new GameObject(def.name);
            pos.y = WorldBootstrap.GroundHeight(pos) + 0.1f;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            var c = go.AddComponent<Creature>();
            c.def = def;
            c.home = pos;
            c.tint = def.color;
            c.baseFaction = faction;
            c.radius = 2.8f;
            c.offset = Vector3.up * 0.6f;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = def.mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Phys.SetDamping(rb, 0f, 0.05f);
            c.rb = rb;

            float s = def.size;
            var capsule = go.AddComponent<CapsuleCollider>();
            if (def.shape == BodyShape.Vierbeiner || def.shape == BodyShape.Kriecher)
            {
                capsule.direction = 2;
                capsule.radius = 0.38f * s;
                capsule.height = 1.5f * s;
                capsule.center = new Vector3(0f, 0.42f * s, 0f);
            }
            else
            {
                capsule.direction = 1;
                capsule.radius = 0.45f * s;
                capsule.height = 1.9f * s;
                capsule.center = new Vector3(0f, 0.95f * s, 0f);
            }
            capsule.sharedMaterial = Slippery;
            c.col = capsule;

            var h = go.AddComponent<Health>();
            h.displayName = def.name;
            h.faction = faction;
            h.maxHp = def.hp;
            h.hp = def.hp;
            h.armor = def.armor;
            h.mass = def.mass;
            h.height = def.shape == BodyShape.Vierbeiner ? 1.1f * s : 1.9f * s;
            h.Damaged += c.OnDamaged;
            h.Died += c.OnDied;
            c.Health = h;

            c.BuildVisual();
            return c;
        }

#if UNITY_2023_1_OR_NEWER
        static PhysicsMaterial slippery;
        static PhysicsMaterial Slippery
        {
            get
            {
                if (slippery == null)
                    slippery = new PhysicsMaterial("Glatt") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
                return slippery;
            }
        }
#else
        static PhysicMaterial slippery;
        static PhysicMaterial Slippery
        {
            get
            {
                if (slippery == null)
                    slippery = new PhysicMaterial("Glatt") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicMaterialCombine.Minimum };
                return slippery;
            }
        }
#endif

        void BuildVisual()
        {
            body = new GameObject("Körper").transform;
            body.SetParent(transform, false);
            float s = def.size;
            Color c = def.color;
            switch (def.shape)
            {
                case BodyShape.Vierbeiner:
                    Part(PrimitiveType.Capsule, new Vector3(0f, 0.75f, 0f), new Vector3(0.6f, 0.65f, 0.6f), c, new Vector3(90f, 0f, 0f));
                    Part(PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0.75f), new Vector3(0.42f, 0.4f, 0.5f), c * 1.05f, Vector3.zero);
                    if (def.id == "wildeber")
                    {
                        Part(PrimitiveType.Cube, new Vector3(0.12f, 0.82f, 1f), new Vector3(0.05f, 0.05f, 0.25f), Color.white, new Vector3(-30f, 0f, 0f));
                        Part(PrimitiveType.Cube, new Vector3(-0.12f, 0.82f, 1f), new Vector3(0.05f, 0.05f, 0.25f), Color.white, new Vector3(-30f, 0f, 0f));
                    }
                    if (def.id == "graslaeufer")
                    {
                        Part(PrimitiveType.Cube, new Vector3(0.12f, 1.3f, 0.7f), new Vector3(0.04f, 0.4f, 0.04f), c * 0.7f, new Vector3(-20f, 0f, 20f));
                        Part(PrimitiveType.Cube, new Vector3(-0.12f, 1.3f, 0.7f), new Vector3(0.04f, 0.4f, 0.04f), c * 0.7f, new Vector3(-20f, 0f, -20f));
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        var leg = new GameObject("Bein").transform;
                        leg.SetParent(body, false);
                        leg.localPosition = new Vector3(i % 2 == 0 ? -0.2f : 0.2f, 0.55f, i < 2 ? 0.45f : -0.45f);
                        PartOn(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.28f, 0f), new Vector3(0.12f, 0.28f, 0.12f), c * 0.8f);
                        legs.Add(leg);
                    }
                    break;
                case BodyShape.Kriecher:
                    Part(PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 0.8f, 1.6f), c, Vector3.zero);
                    for (int i = 0; i < 5; i++)
                        Part(PrimitiveType.Cube, new Vector3((i - 2) * 0.22f, 0.95f, (i % 2) * 0.3f - 0.15f), new Vector3(0.35f, 0.3f, 0.5f), c * 0.75f, new Vector3(15f * i, 20f * i, 0f));
                    Part(PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0.85f), new Vector3(0.5f, 0.4f, 0.4f), c * 0.9f, Vector3.zero);
                    break;
                case BodyShape.Leerenwesen:
                    Part(PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), Vector3.one * 0.9f, c, Vector3.zero, 2.5f);
                    for (int i = 0; i < 6; i++)
                        Part(PrimitiveType.Cube, new Vector3(Mathf.Cos(i) * 0.5f, 1.2f + Mathf.Sin(i * 2f) * 0.4f, Mathf.Sin(i) * 0.5f), new Vector3(0.15f, 0.9f, 0.15f), c * 0.6f, new Vector3(i * 40f, i * 60f, i * 25f), 1.5f);
                    Part(PrimitiveType.Capsule, new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.45f, 0.35f), c * 0.5f, Vector3.zero);
                    break;
                case BodyShape.Mensch:
                    Part(PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.62f, 0.55f, 0.5f), c, Vector3.zero);
                    Part(PrimitiveType.Capsule, new Vector3(0f, 1.2f, 0f), new Vector3(0.52f, 0.42f, 0.36f), c * 1.1f, Vector3.zero);
                    Part(PrimitiveType.Sphere, new Vector3(0f, 1.75f, 0f), Vector3.one * 0.32f, new Color(0.8f, 0.68f, 0.58f), Vector3.zero);
                    Part(PrimitiveType.Sphere, new Vector3(0f, 1.8f, -0.04f), new Vector3(0.4f, 0.4f, 0.42f), c * 0.7f, Vector3.zero);
                    Part(PrimitiveType.Cube, new Vector3(0f, 1.76f, 0.15f), new Vector3(0.2f, 0.05f, 0.05f), new Color(0.75f, 0.3f, 1f), Vector3.zero, 3f);
                    Part(PrimitiveType.Capsule, new Vector3(-0.34f, 1.15f, 0.05f), new Vector3(0.14f, 0.32f, 0.14f), c, new Vector3(-20f, 0f, 0f));
                    Part(PrimitiveType.Capsule, new Vector3(0.34f, 1.15f, 0.12f), new Vector3(0.14f, 0.32f, 0.14f), c, new Vector3(-50f, 0f, 0f));
                    Part(PrimitiveType.Sphere, new Vector3(0.36f, 1.0f, 0.45f), Vector3.one * 0.16f, new Color(0.7f, 0.25f, 1f), Vector3.zero, 3f);
                    break;
                default:
                    Part(PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), new Vector3(0.9f, 0.9f, 0.6f), c, Vector3.zero);
                    Part(PrimitiveType.Cube, new Vector3(0f, 1.75f, 0.05f), new Vector3(0.5f, 0.45f, 0.45f), c * 1.1f, Vector3.zero);
                    Part(PrimitiveType.Cube, new Vector3(0f, 1.78f, 0.28f), new Vector3(0.35f, 0.08f, 0.05f), new Color(0.5f, 0.9f, 1f), Vector3.zero, 3f);
                    Part(PrimitiveType.Cube, new Vector3(-0.62f, 1.0f, 0f), new Vector3(0.3f, 0.9f, 0.3f), c * 0.9f, Vector3.zero);
                    Part(PrimitiveType.Cube, new Vector3(0.62f, 1.0f, 0f), new Vector3(0.3f, 0.9f, 0.3f), c * 0.9f, Vector3.zero);
                    Part(PrimitiveType.Cube, new Vector3(-0.22f, 0.3f, 0f), new Vector3(0.3f, 0.6f, 0.3f), c * 0.8f, Vector3.zero);
                    Part(PrimitiveType.Cube, new Vector3(0.22f, 0.3f, 0f), new Vector3(0.3f, 0.6f, 0.3f), c * 0.8f, Vector3.zero);
                    break;
            }
            body.localScale = Vector3.one * s;
        }

        void Part(PrimitiveType type, Vector3 pos, Vector3 scale, Color c, Vector3 euler, float emission = 0f)
        {
            var p = PartOn(body, type, pos, scale, c, emission);
            p.transform.localRotation = Quaternion.Euler(euler);
        }

        GameObject PartOn(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color c, float emission = 0f)
        {
            var p = GameObject.CreatePrimitive(type);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            var r = p.GetComponent<Renderer>();
            r.material = emission > 0f ? MaterialFactory.Glowing(c, emission) : MaterialFactory.Create(c, 0.2f);
            renderers.Add(r);
            return p;
        }

        public void SetTint(Color c)
        {
            tint = c;
            foreach (var r in renderers) if (r != null) MaterialFactory.SetColor(r.material, c);
        }

        void ApplyGrowth()
        {
            float g = IsJuvenile ? Mathf.Lerp(0.6f, 1f, age / 24f) : 1f;
            transform.localScale = Vector3.one * g;
        }

        void OnDestroy()
        {
            foreach (var r in renderers) if (r != null) Destroy(r.material);
            if (def != null && def.ecosystem && !managed && World.Eco != null) World.Eco.Unregister(this);
        }

        // ---------- Interaktion (Leiche ausweiden) ----------
        public override string Prompt { get { return "Ausweiden: " + def.name; } }
        public override bool CanInteract { get { return IsCorpse && def.drops.Length > 0; } }

        public override void Interact(PlayerCharacter player)
        {
            if (!IsCorpse) return;
            for (int i = 0; i < def.drops.Length; i++)
            {
                int n = i < def.dropCounts.Length ? def.dropCounts[i] : 1;
                if (IsJuvenile) n = Mathf.Max(0, n - 1);
                if (n <= 0) continue;
                player.inv.Add(def.drops[i], n);
                Notify.Combat("+" + n + " " + Items.Name(def.drops[i]));
            }
            Destroy(gameObject);
        }

        // ---------- Ereignisse ----------
        void OnDamaged(DamageInfo info)
        {
            flash = 1f;
            if (info.source == null || info.source == Health) return;
            if (def.temper == Temper.Scheu && !IsAlly)
            {
                fleeFrom = info.source.transform.position;
                Set(CreatureState.Flee, 4f);
            }
            else if (!IsAlly || info.source.faction != Faction.Player)
            {
                target = info.source;
                if (state != CreatureState.Windup) Set(CreatureState.Chase, 12f);
            }
        }

        public void OnHitBy(Health attacker)
        {
            // Treffer durch den Spieler werden bereits über Damaged behandelt.
        }

        void OnDied(DamageInfo info)
        {
            IsCorpse = true;
            state = CreatureState.Dead;
            deathMinute = World.Clock != null ? World.Clock.minutes : 0f;
            if (def.ecosystem && !managed && World.Eco != null) World.Eco.Unregister(this);
            if (Tamed && World.Player != null && World.Player.companion == this) World.Player.companion = null;
            rb.constraints = RigidbodyConstraints.None;
            Phys.SetVelocity(rb, Vector3.zero);
            rb.isKinematic = true;
            col.enabled = false;
            body.localRotation = Quaternion.Euler(0f, 0f, 90f);
            body.localPosition = new Vector3(0f, 0.25f * def.size, 0f);
            foreach (var r in renderers) if (r != null) MaterialFactory.SetColor(r.material, Color.Lerp(tint, new Color(0.3f, 0.3f, 0.3f), 0.5f));

            var killer = info.source;
            bool byPlayer = killer != null && (killer.faction == Faction.Player || killer.faction == Faction.Ally);
            if (byPlayer && World.Player != null)
            {
                World.Player.GainExp(def.exp * expMult);
                var combat = World.Player.combat;
                if (combat != null && combat.BountyTarget == Health)
                {
                    int bounty = Mathf.RoundToInt(def.exp * expMult * 2f);
                    if (World.Treasury.Pay(bounty, "Kopfgelder")) World.Player.AddTaler(bounty);
                    Notify.System("Kopfgeld kassiert: " + bounty + " Taler aus der Kaiserlichen Kasse.");
                }
            }
            if (killer != null && World.Residents != null && Hostile) World.Residents.OnHostileKilled(transform.position, def, byPlayer);
            if (def.diet == Diet.Leere && World.Rift != null) World.Rift.OnVoidKilled(this, byPlayer);
            var killerCreature = killer != null ? killer.GetComponent<Creature>() : null;
            if (killerCreature != null && killerCreature.def.diet == Diet.Fleischfresser) killerCreature.Feed();

            // Risswesen zerfallen sofort — ihre Beute fällt direkt in das Inventar des Bezwingers.
            if (byPlayer && managed && (def.diet == Diet.Leere || def.diet == Diet.Kult) && World.Player != null)
            {
                for (int i = 0; i < def.drops.Length; i++)
                {
                    int n = i < def.dropCounts.Length ? def.dropCounts[i] : 1;
                    if (n <= 0) continue;
                    World.Player.inv.Add(def.drops[i], n);
                    Notify.Combat("+" + n + " " + Items.Name(def.drops[i]));
                }
            }

            // Aufträge: Gegner der Geschichte zählen immer, Tiere nur, wenn der Spieler (oder sein Begleiter) sie erlegt.
            if (World.Quests != null && (questTag != null || byPlayer)) World.Quests.OnKill(def.id);

            if (onDeath != null) onDeath(this);
            if (managed || undeadUntil > 0f)
            {
                Fx.Burst(transform.position + Vector3.up, tint, 8);
                Destroy(gameObject, 1.5f);
            }
        }

        public void Feed()
        {
            hunger = 0f;
            starvingHours = 0f;
            target = null;
            Set(CreatureState.Idle, 6f);
        }

        // Nekromant: die Leiche wird für kurze Zeit zum Diener. Danach zerfällt sie endgültig.
        public void RaiseAsUndead(float seconds)
        {
            if (!IsCorpse) return;
            IsCorpse = false;
            managed = true;
            undeadUntil = Time.time + seconds;
            Health.faction = Faction.Ally;
            Health.Revive(0.6f);
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            col.enabled = true;
            body.localRotation = Quaternion.identity;
            body.localPosition = Vector3.zero;
            SetTint(Color.Lerp(def.color, new Color(0.35f, 0.15f, 0.45f), 0.6f));
            Fx.Burst(transform.position + Vector3.up, new Color(0.5f, 0.2f, 0.7f), 12);
            state = CreatureState.Idle;
        }

        // Wildjäger: das Tier verlässt die wilde Population und folgt dem Spieler.
        public void Tame(bool silent = false)
        {
            if (Tamed || IsCorpse) return;
            var pc = World.Player;
            if (pc.companion != null && pc.companion != this) pc.companion.Release();
            if (def.ecosystem && !managed && World.Eco != null) World.Eco.Unregister(this);
            managed = true;
            Tamed = true;
            Health.faction = Faction.Ally;
            Health.Heal(Health.maxHp);
            pc.companion = this;
            if (!silent) Notify.System(def.name + " wurde gezähmt und folgt dir nun.");
            target = null;
        }

        public void Release()
        {
            Tamed = false;
            Health.faction = baseFaction;
            managed = !def.ecosystem;
            if (def.ecosystem && World.Eco != null) World.Eco.Register(this);
            home = transform.position;
        }

        public void Knockback(Vector3 impulse)
        {
            if (rb == null || rb.isKinematic) return;
            rb.AddForce(impulse + Vector3.up * impulse.magnitude * 0.15f, ForceMode.Impulse);
        }

        public void Slow(float mult, float seconds)
        {
            slowMult = mult;
            slowUntil = Time.time + seconds;
        }

        public void Poison(float dps, float seconds)
        {
            poisonDps = Mathf.Max(poisonDps, dps);
            poisonUntil = Time.time + seconds;
        }

        public void SetHome(Vector3 p) { home = p; }

        public void AgeHours(float hours)
        {
            bool wasJuvenile = IsJuvenile;
            age += hours;
            if (wasJuvenile) ApplyGrowth();
        }

        // ---------- KI ----------
        void Set(CreatureState s, float duration)
        {
            state = s;
            stateUntil = Time.time + duration;
        }

        void Update()
        {
            if (IsCorpse || !World.Ready) return;
            float dt = Time.deltaTime;

            if (flash > 0f)
            {
                flash = Mathf.MoveTowards(flash, 0f, dt * 5f);
                Color cc = Color.Lerp(tint, Color.white, flash);
                foreach (var r in renderers) if (r != null) MaterialFactory.SetColor(r.material, cc);
            }

            if (Time.time < poisonUntil)
            {
                poisonTick -= dt;
                if (poisonTick <= 0f)
                {
                    poisonTick = 0.5f;
                    Health.Apply(new DamageInfo { amount = poisonDps * 0.5f, point = Health.Center, source = World.Player != null ? World.Player.health : null });
                    if (World.Hud != null) World.Hud.Floating(Health.Center, Mathf.RoundToInt(poisonDps * 0.5f).ToString(), new Color(0.5f, 0.9f, 0.3f), false);
                    if (IsCorpse) return;
                }
            }

            if (undeadUntil > 0f && Time.time > undeadUntil)
            {
                Fx.Burst(transform.position + Vector3.up, new Color(0.4f, 0.2f, 0.5f), 10);
                Destroy(gameObject);
                return;
            }

            float distToPlayer = World.Player != null ? (World.Player.transform.position - transform.position).magnitude : 999f;
            if (distToPlayer > 140f && !Tamed) { Steer(Vector3.zero, 0f); return; }

            thinkTimer -= dt;
            if (thinkTimer <= 0f)
            {
                thinkTimer = 0.25f + UnityEngine.Random.value * 0.1f;
                Think();
            }
            Act();
            Animate();
        }

        float AggroRange
        {
            get
            {
                float r = def.aggroRange;
                if (World.Weather != null) r *= World.Weather.AggroMult;
                if (def.nocturnal && World.Clock != null && World.Clock.IsNight) r *= 1.5f;
                return r;
            }
        }

        bool IsEnemy(Health h)
        {
            if (h == null || h.IsDead || h == Health) return false;
            Faction me = Health.faction;
            Faction o = h.faction;
            if (me == Faction.Ally) return (o == Faction.Void || o == Faction.Construct) || (o == Faction.Wild && h.GetComponent<Creature>() != null && h.GetComponent<Creature>().target != null && h.GetComponent<Creature>().target.faction == Faction.Player);
            if (me == Faction.Void && def.diet == Diet.Kult) return o == Faction.Player || o == Faction.Ally || o == Faction.Citizen || o == Faction.Guard;
            if (me == Faction.Void) return o != Faction.Void && o != Faction.Imperial;
            if (me == Faction.Construct) return o == Faction.Player || o == Faction.Ally;
            return o == Faction.Player || o == Faction.Ally;
        }

        void Think()
        {
            var pc = World.Player;
            if (target != null && (target.IsDead || !target.isActiveAndEnabled)) target = null;

            if (IsAlly)
            {
                if (target == null) target = Health.Nearest(pc.transform.position, 16f, IsEnemy);
                if (target == null)
                {
                    float d = (pc.transform.position - transform.position).magnitude;
                    state = d > 4f ? CreatureState.Home : CreatureState.Idle;
                    home = pc.transform.position;
                }
                else if (state != CreatureState.Windup && state != CreatureState.Recover) state = CreatureState.Chase;
                return;
            }

            bool night = World.Clock != null && World.Clock.IsNight;

            if (def.diet == Diet.Leere || def.diet == Diet.Konstrukt || def.diet == Diet.Kult)
            {
                if (target == null) target = Health.Nearest(transform.position, AggroRange, IsEnemy);
                if (target != null && summonInterval > 0f && Time.time >= nextSummon)
                {
                    nextSummon = Time.time + summonInterval;
                    Summon();
                }
                if (target != null) { if (state != CreatureState.Windup && state != CreatureState.Recover) state = CreatureState.Chase; }
                else if (Raider && World.Residents != null) { home = Places.VillageCenter; state = CreatureState.Home; }
                else if (state != CreatureState.Wander && state != CreatureState.Idle) state = CreatureState.Wander;
                return;
            }

            if (def.temper == Temper.Scheu)
            {
                if (state == CreatureState.Flee && Time.time < stateUntil) return;
                Health threat = Health.Nearest(transform.position, 12f * (World.Weather != null ? World.Weather.AggroMult : 1f), h =>
                {
                    if (h.faction == Faction.Player || h.faction == Faction.Ally || h.faction == Faction.Void) return true;
                    var c = h.GetComponent<Creature>();
                    return c != null && c.def.diet == Diet.Fleischfresser;
                });
                if (threat != null)
                {
                    fleeFrom = threat.transform.position;
                    Set(CreatureState.Flee, 3f);
                    return;
                }
                if (night) { state = CreatureState.Sleep; return; }
                if (World.Weather != null && World.Weather.IsStorm) { state = CreatureState.Idle; return; }
                if (state == CreatureState.Sleep || state == CreatureState.Flee) state = CreatureState.Wander;
                return;
            }

            if ((transform.position - home).magnitude > def.habitatRadius * 0.9f && target == null)
            {
                state = CreatureState.Home;
                return;
            }

            if (def.diet == Diet.Fleischfresser && hunger > 0.6f && target == null)
            {
                target = Health.Nearest(transform.position, 40f, h =>
                {
                    var c = h.GetComponent<Creature>();
                    return c != null && c.def.diet == Diet.Pflanzenfresser && c.def.temper == Temper.Scheu && !c.IsCorpse;
                });
            }

            bool aggressive = def.temper == Temper.Feindselig && (hunger > 0.5f || night);
            if (target == null && (aggressive || def.temper == Temper.Territorial))
            {
                float range = def.temper == Temper.Territorial ? def.aggroRange : AggroRange;
                target = Health.Nearest(transform.position, range, IsEnemy);
            }

            if (target != null)
            {
                if ((target.transform.position - home).magnitude > def.habitatRadius * 1.2f && def.temper == Temper.Territorial)
                {
                    target = null;
                    state = CreatureState.Home;
                    return;
                }
                if (state != CreatureState.Windup && state != CreatureState.Recover) state = CreatureState.Chase;
            }
            else if (state == CreatureState.Chase || state == CreatureState.Sleep)
            {
                state = CreatureState.Wander;
            }
            if (night && def.temper == Temper.Territorial && target == null) state = CreatureState.Sleep;
        }

        void Act()
        {
            float speed = def.speed * (Time.time < slowUntil ? slowMult : 1f) * (IsJuvenile ? 0.8f : 1f);
            switch (state)
            {
                case CreatureState.Idle:
                case CreatureState.Sleep:
                    Steer(Vector3.zero, 0f);
                    if (state == CreatureState.Idle && Time.time > stateUntil && !IsAlly) state = CreatureState.Wander;
                    break;

                case CreatureState.Wander:
                    if (Time.time > stateUntil || (Flat(wanderTarget - transform.position)).magnitude < 1.5f)
                    {
                        Vector2 r = UnityEngine.Random.insideUnitCircle * Mathf.Min(def.habitatRadius, 25f);
                        wanderTarget = home + new Vector3(r.x, 0f, r.y);
                        Set(CreatureState.Wander, UnityEngine.Random.Range(5f, 11f));
                    }
                    Steer(Flat(wanderTarget - transform.position).normalized, speed * 0.35f);
                    break;

                case CreatureState.Home:
                {
                    Vector3 d = Flat(home - transform.position);
                    if (d.magnitude < 3f && !Raider) { state = IsAlly ? CreatureState.Idle : CreatureState.Wander; Steer(Vector3.zero, 0f); }
                    else Steer(d.normalized, speed * (IsAlly ? 1f : 0.6f));
                    break;
                }

                case CreatureState.Flee:
                    Steer(Flat(transform.position - fleeFrom).normalized, speed);
                    if (Time.time > stateUntil) state = CreatureState.Wander;
                    break;

                case CreatureState.Chase:
                {
                    if (target == null || target.IsDead) { state = IsAlly ? CreatureState.Idle : CreatureState.Wander; break; }
                    Vector3 d = Flat(target.transform.position - transform.position);
                    float reach = def.ranged ? def.attackRange : def.attackRange * def.size + 0.4f;
                    if (d.magnitude <= reach)
                    {
                        Steer(Vector3.zero, 0f);
                        Face(d);
                        Set(CreatureState.Windup, def.ranged ? 0.7f : 0.45f);
                        flash = 0.6f;
                        if (!def.ranged && (def.size > 1.8f || managed)) Fx.Disc(transform.position + d.normalized * reach * 0.6f, reach * 0.7f, new Color(1f, 0.3f, 0.2f), 0.45f);
                    }
                    else Steer(d.normalized, speed);
                    break;
                }

                case CreatureState.Windup:
                    Steer(Vector3.zero, 0f);
                    if (Time.time >= stateUntil)
                    {
                        Strike();
                        Set(CreatureState.Recover, UnityEngine.Random.Range(0.6f, 1f));
                    }
                    break;

                case CreatureState.Recover:
                    Steer(Vector3.zero, 0f);
                    if (Time.time >= stateUntil) state = target != null ? CreatureState.Chase : CreatureState.Wander;
                    break;
            }
        }

        // Sprungangriff: Körpermasse × Beschleunigung. Der Sprung selbst ist ein echter Impuls J = m · Δv.
        void Strike()
        {
            if (target == null || target.IsDead) return;
            Vector3 d = Flat(target.transform.position - transform.position);
            if (def.ranged) { Cast(); return; }
            rb.AddForce(d.normalized * def.mass * 4f, ForceMode.Impulse);
            float reach = (def.attackRange * def.size + 0.4f) * 1.4f;
            if (d.magnitude > reach) return;

            Strike s = PhysicsCombat.Body(def.mass * transform.localScale.x, def.attackAccel, damageMult);
            var info = new DamageInfo { amount = s.damage, point = target.Center, direction = d.normalized, impulse = s.impulse, source = Health, formula = s.formula };
            float dealt = target.Apply(info);
            if (target.faction == Faction.Player)
            {
                Notify.Combat(def.name + ": " + s.formula + " → " + Mathf.RoundToInt(dealt) + " Schaden an dir");
                if (World.Player != null) World.Player.motor.AddImpulse(d.normalized * s.impulse * GameConstants.KnockbackScale);
            }
            var otherCreature = target.GetComponent<Creature>();
            if (otherCreature != null) otherCreature.Knockback(d.normalized * s.impulse * GameConstants.KnockbackScale);
            if (World.Hud != null && target.faction != Faction.Player && dealt > 0f)
                World.Hud.Floating(target.Center, Mathf.RoundToInt(dealt).ToString(), new Color(1f, 0.6f, 0.5f), false);
        }

        // Äthergeschoss der Kultisten: F = m_Äther · a, wie beim Kampfmagier.
        void Cast()
        {
            Strike s = PhysicsCombat.Spell(def.projectileMass, def.projectileAccel, damageMult);
            Vector3 from = transform.position + Vector3.up * 1.5f * def.size + transform.forward * 0.6f;
            Vector3 dir = (target.Center - from).normalized;
            var p = Projectile.Spawn(Health, from, dir * 20f, new Color(0.7f, 0.25f, 1f), 0.25f + def.projectileMass * 0.3f);
            p.canHit = IsEnemy;
            string name = def.name;
            p.onHit = (h, pr) =>
            {
                float dealt = h.Apply(new DamageInfo { amount = s.damage, point = h.Center, direction = pr.Velocity.normalized, impulse = s.impulse, source = Health, magic = true, formula = s.formula });
                if (h.faction == Faction.Player)
                {
                    Notify.Combat(name + ": " + s.formula + " → " + Mathf.RoundToInt(dealt) + " Schaden an dir");
                    if (World.Player != null) World.Player.motor.AddImpulse(pr.Velocity.normalized * s.impulse * GameConstants.KnockbackScale);
                }
                else if (World.Hud != null && dealt > 0f) World.Hud.Floating(h.Center, Mathf.RoundToInt(dealt).ToString(), new Color(0.8f, 0.5f, 1f), false);
            };
        }

        void Summon()
        {
            var def2 = Species.Get("leerenbrut");
            for (int i = 0; i < 2; i++)
            {
                Vector3 p = transform.position + new Vector3(UnityEngine.Random.Range(-4f, 4f), 0.5f, UnityEngine.Random.Range(-4f, 4f));
                var v = Spawn(def2, p, 100f);
                v.SetHome(transform.position);
                if (questTag != null) v.questTag = null;
            }
            Fx.Ring(transform.position + Vector3.up * 0.2f, 5f, new Color(0.6f, 0.2f, 0.9f), 0.8f);
            Notify.Combat(def.name + " ruft Leerenbrut aus dem Riss!");
        }

        void Steer(Vector3 dir, float speed)
        {
            if (rb == null || rb.isKinematic) return;
            Vector3 v = Phys.GetVelocity(rb);
            Vector3 vh = new Vector3(v.x, 0f, v.z);
            Vector3 want = dir * speed;
            Vector3 a = (want - vh) / Tau;
            if (a.magnitude > MaxAccel) a = a.normalized * MaxAccel;
            rb.AddForce(a * rb.mass, ForceMode.Force); // F = m · a
            if (want.sqrMagnitude > 0.05f) Face(want);
        }

        void Face(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion q = Quaternion.LookRotation(Flat(dir).normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, q, 8f * Time.deltaTime));
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void Animate()
        {
            float sp = rb != null ? Flat(Phys.GetVelocity(rb)).magnitude : 0f;
            legPhase += Time.deltaTime * (3f + sp * 3f);
            for (int i = 0; i < legs.Count; i++)
            {
                float a = Mathf.Sin(legPhase + (i % 2 == 0 ? 0f : Mathf.PI) + (i < 2 ? 0f : Mathf.PI)) * Mathf.Clamp01(sp / 4f) * 35f;
                legs[i].localRotation = Quaternion.Euler(a, 0f, 0f);
            }
            if (def.shape == BodyShape.Leerenwesen) body.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2f) * 0.15f, 0f);
            if (state == CreatureState.Sleep) body.localPosition = new Vector3(0f, -0.15f * def.size, 0f);
            else if (def.shape != BodyShape.Leerenwesen) body.localPosition = Vector3.zero;
        }

        public CreatureSave ToSave()
        {
            return new CreatureSave { species = def.id, position = transform.position, age = age, hunger = hunger };
        }
    }
}
