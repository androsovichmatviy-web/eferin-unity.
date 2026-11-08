using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum NpcRole
    {
        Kaiser, Richter, Haendler, Schmied, Vermieter, Heiler, Pruefungsmeister, Gildenmeister,
        Wache, Ordensmagier, Buerger, Einwohner, Soeldnerfuehrer, Dorfwache, Kraemer,
    }

    public class Npc : Interactable
    {
        public static readonly List<Npc> AllNpcs = new List<Npc>();

        public NpcRole role;
        public string npcName;
        public string title;
        public Health health;
        public HumanoidVisual visual;
        public List<Vector3> route;
        public Vector3 homePos;
        public float homeYaw;
        public Resident resident;
        public float walkSpeed = 1.6f;

        int routeIndex;
        float waitUntil;
        Health target;
        float attackReady;
        float knockedOutUntil = -1f;
        float lastPlayerHit = -100f;
        float groundTimer;
        float groundY;
        float thinkTimer;

        public bool IsGuard { get { return role == NpcRole.Wache || role == NpcRole.Dorfwache || role == NpcRole.Ordensmagier; } }
        public bool KnockedOut { get { return knockedOutUntil > 0f; } }

        protected override void OnEnable() { base.OnEnable(); AllNpcs.Add(this); }
        protected override void OnDisable() { base.OnDisable(); AllNpcs.Remove(this); }

        public static Npc Spawn(NpcRole role, string name, string title, Vector3 pos, float yaw, Color cloth, Color accent)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.35f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var npc = go.AddComponent<Npc>();
            npc.role = role;
            npc.npcName = name;
            npc.title = title;
            npc.homePos = pos;
            npc.homeYaw = yaw;
            npc.groundY = pos.y;
            npc.radius = 3f;
            npc.offset = Vector3.up * 1.2f;

            var h = go.AddComponent<Health>();
            h.displayName = name;
            h.height = 1.8f;
            h.mass = 75f;
            switch (role)
            {
                case NpcRole.Kaiser:
                case NpcRole.Richter:
                    h.faction = Faction.Imperial; h.invulnerable = true; h.maxHp = 99999f; break;
                case NpcRole.Wache:
                    h.faction = Faction.Guard; h.maxHp = 900f; h.armor = 20f; npc.walkSpeed = 2f; break;
                case NpcRole.Ordensmagier:
                    h.faction = Faction.Guard; h.maxHp = 700f; h.armor = 10f; break;
                case NpcRole.Dorfwache:
                    h.faction = Faction.Ally; h.maxHp = 500f; h.armor = 12f; npc.walkSpeed = 1.8f; break;
                default:
                    h.faction = Faction.Citizen; h.maxHp = 120f; break;
            }
            h.hp = h.maxHp;
            h.Died += npc.OnKnockedOut;
            npc.health = h;

            Color skin = new Color(0.9f, 0.76f, 0.62f);
            npc.visual = HumanoidVisual.Build(go.transform, cloth, accent, skin, 1f);
            switch (role)
            {
                case NpcRole.Kaiser:
                    npc.visual.AddRobe(cloth);
                    npc.visual.AddCrown(new Color(0.95f, 0.78f, 0.3f));
                    break;
                case NpcRole.Richter:
                    npc.visual.AddRobe(cloth);
                    break;
                case NpcRole.Ordensmagier:
                    npc.visual.AddRobe(cloth);
                    npc.visual.SetWeapon(Items.Get("aetherstab"));
                    break;
                case NpcRole.Wache:
                case NpcRole.Dorfwache:
                    npc.visual.AddHelmet(new Color(0.7f, 0.72f, 0.75f));
                    npc.visual.SetWeapon(Items.Get("langschwert"));
                    break;
            }
            return npc;
        }

        public override string Prompt { get { return "Sprechen: " + npcName + (string.IsNullOrEmpty(title) ? "" : " (" + title + ")"); } }
        public override bool CanInteract { get { return !KnockedOut && target == null; } }

        public override void Interact(PlayerCharacter player)
        {
            World.Ui.OpenDialogue(new NpcDialogue(this));
        }

        public void OnAttackedByPlayer()
        {
            if (Time.time - lastPlayerHit > 8f && World.Law != null) World.Law.OnPlayerAttackedNpc(this);
            lastPlayerHit = Time.time;
            if (resident != null && World.Residents != null) World.Residents.OnAttacked(resident);
        }

        void OnKnockedOut(DamageInfo info)
        {
            knockedOutUntil = Time.time + 45f;
            target = null;
            visual.transform.localRotation = Quaternion.Euler(-80f, 0f, 0f);
            visual.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        }

        void Update()
        {
            if (!World.Ready) return;
            if (KnockedOut)
            {
                if (Time.time > knockedOutUntil)
                {
                    knockedOutUntil = -1f;
                    health.Revive(0.5f);
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localPosition = Vector3.zero;
                }
                return;
            }

            if (IsGuard)
            {
                thinkTimer -= Time.deltaTime;
                if (thinkTimer <= 0f)
                {
                    thinkTimer = 0.3f;
                    GuardThink();
                }
                if (target != null) { Fight(); return; }
            }

            Vector3 goal = homePos;
            bool walking = false;
            bool nightHome = (role == NpcRole.Einwohner || role == NpcRole.Buerger) && World.Clock != null && World.Clock.IsNight;
            if (route != null && route.Count > 0 && !nightHome)
            {
                goal = route[routeIndex];
                walking = true;
                if (Flat(goal - transform.position).magnitude < 0.6f)
                {
                    if (Time.time > waitUntil + 0.01f && waitUntil > 0f)
                    {
                        routeIndex = (routeIndex + 1 + Random.Range(0, 2)) % route.Count;
                        waitUntil = 0f;
                    }
                    else if (waitUntil <= 0f) waitUntil = Time.time + Random.Range(2f, 7f);
                    walking = false;
                }
            }
            MoveTowards(goal, walkSpeed, walking || Flat(goal - transform.position).magnitude > 0.6f);
        }

        void MoveTowards(Vector3 goal, float speed, bool move)
        {
            Vector3 d = Flat(goal - transform.position);
            if (move && d.magnitude > 0.3f)
            {
                transform.position += d.normalized * Mathf.Min(speed * Time.deltaTime, d.magnitude);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 6f * Time.deltaTime);
                visual.moveSpeed = speed;
            }
            else
            {
                visual.moveSpeed = 0f;
                if (route == null || route.Count == 0)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, homeYaw, 0f), 3f * Time.deltaTime);
            }
            UpdateGround();
        }

        void UpdateGround()
        {
            groundTimer -= Time.deltaTime;
            if (groundTimer <= 0f)
            {
                groundTimer = 0.3f;
                groundY = WorldBootstrap.GroundHeight(transform.position + Vector3.up * 0.5f, transform);
            }
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(p.y, groundY, 10f * Time.deltaTime);
            transform.position = p;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ---------- Wachen ----------
        void GuardThink()
        {
            if (target != null && (target.IsDead || !target.isActiveAndEnabled)) target = null;
            var pc = World.Player;
            if (pc == null) return;
            float dPlayer = (pc.transform.position - transform.position).magnitude;

            if (role != NpcRole.Dorfwache && !pc.health.IsDead)
            {
                bool wanted = World.Law.Wanted > 0 && dPlayer < 35f;
                bool trespass = role == NpcRole.Ordensmagier && World.Law.Trespassing && dPlayer < 30f;
                if (wanted || trespass) { target = pc.health; return; }
                if (target == pc.health) target = null;
            }
            if (target != null) return;

            Vector3 center = role == NpcRole.Dorfwache ? Places.VillageCenter : transform.position;
            float range = role == NpcRole.Dorfwache ? 30f : 22f;
            target = Health.Nearest(center, range, h =>
            {
                var c = h.GetComponent<Creature>();
                if (c == null || c.IsCorpse || c.IsAlly) return false;
                if (c.def.diet == Diet.Konstrukt) return false;
                // Wildtiere nur, wenn sie gerade jemanden angreifen — sonst rottet der Orden die Wölfe an der Grenze aus.
                var t = c.Target;
                if (c.def.diet == Diet.Leere || c.def.diet == Diet.Kult) return true;
                if (!c.Hostile || t == null) return false;
                return t == pc.health || t.GetComponent<Npc>() != null || t.faction == Faction.Ally;
            });
        }

        void Fight()
        {
            if (target == null || target.IsDead) { target = null; return; }
            Vector3 d = Flat(target.transform.position - transform.position);
            float dist = d.magnitude;
            if (dist > 45f) { target = null; return; }

            if (role == NpcRole.Ordensmagier)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 8f * Time.deltaTime);
                if (dist > 20f) MoveTowards(target.transform.position, 3f, true);
                else if (Time.time >= attackReady)
                {
                    attackReady = Time.time + 2f;
                    Strike s = PhysicsCombat.Spell(0.5f, 220f, 1f);
                    Vector3 from = transform.position + Vector3.up * 1.5f + transform.forward * 0.6f;
                    var p = Projectile.Spawn(health, from, (target.Center - from).normalized * 22f, new Color(0.7f, 0.4f, 1f), 0.25f);
                    var t = target;
                    p.canHit = h => h == t || (h.GetComponent<Creature>() != null && h.faction != Faction.Ally);
                    p.onHit = (h, pr) => DealDamage(h, s, pr.Velocity.normalized);
                }
                return;
            }

            if (dist > 2f) MoveTowards(target.transform.position, 4.2f, true);
            else
            {
                visual.moveSpeed = 0f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
                if (Time.time >= attackReady)
                {
                    attackReady = Time.time + 1.1f;
                    visual.swing = 0f;
                    Invoke("EndSwing", 0.3f);
                    Strike s = PhysicsCombat.Melee(1.6f, 1f, 0.26f, 0f, role == NpcRole.Dorfwache ? 0.9f : 1f);
                    DealDamage(target, s, d.normalized);
                }
            }
        }

        void EndSwing() { if (visual != null) visual.swing = -1f; }

        void DealDamage(Health h, Strike s, Vector3 dir)
        {
            float dealt = h.Apply(new DamageInfo { amount = s.damage, point = h.Center, direction = dir, impulse = s.impulse, source = health, formula = s.formula });
            var c = h.GetComponent<Creature>();
            if (c != null) c.Knockback(dir * s.impulse * GameConstants.KnockbackScale);
            if (h.faction == Faction.Player)
            {
                World.Player.motor.AddImpulse(dir * s.impulse * GameConstants.KnockbackScale);
                Notify.Combat(npcName + ": " + s.formula + " → " + Mathf.RoundToInt(dealt) + " Schaden an dir");
            }
            else if (World.Hud != null && dealt > 0f) World.Hud.Floating(h.Center, Mathf.RoundToInt(dealt).ToString(), new Color(0.8f, 0.85f, 1f), false);
        }
    }
}
