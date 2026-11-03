using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Grundangriffe und Fähigkeiten. Jeder Treffer läuft über PhysicsCombat (F = m · a).
    public class PlayerCombat : MonoBehaviour
    {
        PlayerCharacter pc;
        PlayerMotor motor;
        float attackReady;
        float swingStart = -1f;
        float swingDuration;
        bool swingHitDone;
        float swingMult = 1f;
        bool swingFull;
        float swingExtraRadius;
        float swingApproach;
        readonly float[] cooldownUntil = new float[3];
        bool nextCrit;
        float nextCritUntil;
        Health bountyTarget;
        float bountyUntil;
        bool inRune;

        const float ArrowMass = 0.05f;
        const float DrawLength = 0.6f;

        void Awake()
        {
            pc = GetComponent<PlayerCharacter>();
            motor = GetComponent<PlayerMotor>();
        }

        public AbilityDef Slot(int i)
        {
            if (i == 0) return pc.cls.skillQ;
            if (i == 1) return pc.cls.skillE;
            return pc.hybrid != null ? pc.hybrid.skillR : null;
        }

        public float CooldownLeft(int i) { return Mathf.Max(0f, cooldownUntil[i] - Time.time); }

        public Health BountyTarget { get { return Time.time < bountyUntil ? bountyTarget : null; } }

        void Update()
        {
            if (!World.Ready || pc.health.IsDead) return;

            if (swingStart >= 0f)
            {
                float t = (Time.time - swingStart) / swingDuration;
                if (pc.visual != null) pc.visual.swing = t;
                if (t >= 0.55f && !swingHitDone)
                {
                    swingHitDone = true;
                    MeleeHit();
                }
                if (t >= 1f)
                {
                    swingStart = -1f;
                    if (pc.visual != null) pc.visual.swing = -1f;
                }
            }

            if (World.GameplayBlocked) return;

            if (GameInput.AttackHeld() && Time.time >= attackReady) BasicAttack(1f);
            if (GameInput.Pressed(Act.Skill1)) UseSkill(0);
            if (GameInput.Pressed(Act.Skill2)) UseSkill(1);
            if (GameInput.Pressed(Act.Skill3)) UseSkill(2);
            if (GameInput.Pressed(Act.Potion1)) pc.Use("heiltrank");
            if (GameInput.Pressed(Act.Potion2)) pc.Use("aetherelixier");
            if (GameInput.Pressed(Act.Potion3)) pc.Use("gebratenes_fleisch");
        }

        // ---------- Grundangriff ----------
        void BasicAttack(float mult)
        {
            var w = pc.Weapon;
            motor.FaceAim(0.6f);
            switch (w.weaponKind)
            {
                case WeaponKind.Nahkampf:
                    StartSwing(mult, false, 0f);
                    break;
                case WeaponKind.Stab:
                    CastBolt(0.25f, mult, 0f, 0, false);
                    attackReady = Time.time + 0.55f / (1f + pc.Agi * 0.006f) / pc.AttackSpeedMult;
                    if (pc.visual != null) { pc.visual.swing = 0.3f; Invoke("EndCastPose", 0.25f); }
                    break;
                case WeaponKind.Bogen:
                    ShootArrow(1f, mult, 0f, 0);
                    attackReady = Time.time + 0.65f / (1f + pc.Agi * 0.01f) / pc.AttackSpeedMult;
                    break;
            }
        }

        void EndCastPose()
        {
            if (pc.visual != null && swingStart < 0f) pc.visual.swing = -1f;
        }

        void StartSwing(float mult, bool full, float extraRadius)
        {
            swingDuration = pc.SwingTime(pc.Weapon);
            swingStart = Time.time;
            swingHitDone = false;
            swingMult = mult;
            swingFull = full;
            swingExtraRadius = extraRadius;
            swingApproach = Vector3.Dot(motor.Velocity, transform.forward);
            attackReady = Time.time + swingDuration + 0.12f;
        }

        void MeleeHit()
        {
            var w = pc.Weapon;
            float reach = w.length + 0.7f + swingExtraRadius;
            Vector3 center = transform.position + Vector3.up * 1.1f + (swingFull ? Vector3.zero : transform.forward * w.length * 0.5f);
            foreach (var target in Gather(center, reach))
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (!swingFull && to.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, to) > 75f) continue;
                Strike s = PhysicsCombat.Melee(w.mass, w.length, swingDuration, swingApproach, swingMult * pc.DamageMult);
                Hit(target, s, to.sqrMagnitude > 0.01f ? to.normalized : transform.forward, false);
            }
            if (World.Rig != null) World.Rig.Shake(0.2f + w.mass * 0.05f);
        }

        List<Health> Gather(Vector3 center, float radius)
        {
            var list = new List<Health>();
            foreach (var c in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var h = c.GetComponentInParent<Health>();
                if (h == null || h == pc.health || h.IsDead || list.Contains(h)) continue;
                if (!CanHit(h)) continue;
                list.Add(h);
            }
            return list;
        }

        public bool CanHit(Health h)
        {
            if (h == null || h == pc.health || h.IsDead) return false;
            if (h.faction == Faction.Ally || h.faction == Faction.Player) return false;
            return true;
        }

        // ---------- Treffer ----------
        public void Hit(Health target, Strike s, Vector3 dir, bool magic)
        {
            bool crit = Random.value < pc.Crit;
            if (nextCrit && Time.time < nextCritUntil) { crit = true; nextCrit = false; }
            float dmg = s.damage * (crit ? 1.7f : 1f);
            if (BountyTarget == target) dmg *= 1.4f;

            var info = new DamageInfo
            {
                amount = dmg, point = target.Center, direction = dir, impulse = s.impulse,
                source = pc.health, crit = crit, magic = magic, formula = s.formula,
            };
            float dealt = target.Apply(info);
            Notify.Combat(s.formula + " → " + Mathf.RoundToInt(dealt) + " Schaden" + (crit ? " (kritisch)" : ""));
            if (World.Hud != null) World.Hud.Floating(target.Center + Vector3.up * 0.4f, Mathf.RoundToInt(dealt).ToString(), crit ? new Color(1f, 0.85f, 0.3f) : Color.white, crit);

            var creature = target.GetComponent<Creature>();
            if (creature != null)
            {
                creature.Knockback(dir * s.impulse * GameConstants.KnockbackScale);
                creature.OnHitBy(pc.health);
                if (pc.HasBuffFlag(b => b.poison)) creature.Poison(4f + pc.Int * 0.4f + pc.Agi * 0.3f, 4f);
            }
            var npc = target.GetComponent<Npc>();
            if (npc != null) npc.OnAttackedByPlayer();

            float steal = 0f;
            foreach (var b in pc.buffs) steal += b.lifesteal;
            if (steal > 0f) pc.health.Heal(dealt * steal);

            if (!magic && !inRune && pc.HasBuffFlag(b => b.rune))
            {
                inRune = true;
                Explosion(target.Center, 2.5f, PhysicsCombat.Spell(0.6f, AetherAccel(), 0.8f), new Color(0.5f, 0.75f, 1f));
                inRune = false;
            }
        }

        float AetherAccel()
        {
            var w = pc.Weapon;
            float focus = w.weaponKind == WeaponKind.Stab ? w.focus : 0.7f;
            return 120f * (1f + pc.Int * 0.03f) * focus;
        }

        void Explosion(Vector3 center, float radius, Strike s, Color color)
        {
            Fx.Ring(center, radius, color, 0.4f);
            Fx.Burst(center, color, 10);
            foreach (var h in Gather(center, radius))
            {
                Vector3 d = h.transform.position - center;
                d.y = 0f;
                Hit(h, s, d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward, true);
            }
        }

        // ---------- Projektile ----------
        Vector3 Muzzle { get { return transform.position + Vector3.up * 1.45f + transform.forward * 0.5f; } }

        Vector3 AimDir(Vector3 from)
        {
            Vector3 p = World.Rig != null ? World.Rig.AimPoint(120f) : from + transform.forward * 50f;
            Vector3 d = p - from;
            return d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
        }

        void CastBolt(float aetherMass, float mult, float explodeRadius, int pierce, bool homing)
        {
            Strike s = PhysicsCombat.Spell(aetherMass, AetherAccel(), mult * pc.DamageMult);
            Vector3 from = Muzzle;
            var proj = Projectile.Spawn(pc.health, from, AimDir(from) * 26f, new Color(0.45f, 0.75f, 1f), 0.18f + aetherMass * 0.2f);
            proj.canHit = CanHit;
            proj.pierce = pierce;
            proj.explodeRadius = explodeRadius;
            if (homing) proj.homingTarget = World.Rig != null ? World.Rig.AimTarget(60f) : null;
            proj.onHit = (h, p) => Hit(h, s, p.Velocity.normalized, true);
            proj.onExplode = (pos) => Explosion(pos, explodeRadius, s, new Color(0.45f, 0.75f, 1f));
        }

        // Pfeil: a = F / m, v = √(2 · F · d / m). Schaden beim Aufprall aus E = ½ · m · v².
        void ShootArrow(float forceMult, float mult, float spreadDeg, int pierce)
        {
            var w = pc.Weapon;
            float force = (w.drawForce > 0f ? w.drawForce : 60f) * (1f + pc.Str * 0.005f) * forceMult;
            float v = Mathf.Min(70f, PhysicsCombat.ArrowSpeed(force, DrawLength, ArrowMass));
            Vector3 from = Muzzle;
            Vector3 dir = Quaternion.AngleAxis(spreadDeg, Vector3.up) * AimDir(from);
            var proj = Projectile.Spawn(pc.health, from, dir * v, new Color(0.9f, 0.85f, 0.7f), 0.08f);
            proj.gravity = true;
            proj.pierce = pierce;
            proj.canHit = CanHit;
            float weatherMult = World.Weather != null ? World.Weather.BowMult : 1f;
            proj.onHit = (h, p) =>
            {
                Strike s = PhysicsCombat.ArrowImpact(ArrowMass, p.Velocity.magnitude, mult * pc.DamageMult * weatherMult);
                Hit(h, s, p.Velocity.normalized, false);
            };
            Notify.Combat("Bogen: a = F/m = " + force.ToString("0") + " N / " + ArrowMass + " kg = " + (force / ArrowMass).ToString("0") + " m/s², v = " + v.ToString("0") + " m/s");
        }

        // ---------- Fähigkeiten ----------
        void UseSkill(int slot)
        {
            var a = Slot(slot);
            if (a == null) return;
            if (Time.time < cooldownUntil[slot])
            {
                Notify.Combat(a.name + " lädt noch (" + CooldownLeft(slot).ToString("0.0") + " s).");
                return;
            }
            if (a.kind == AbilityKind.BloodLance)
            {
                float cost = pc.health.maxHp * 0.15f;
                if (pc.health.hp <= cost + 1f) { Notify.Combat("Zu wenig Leben für die Blutlanze."); return; }
                pc.health.hp -= cost;
            }
            else
            {
                if (pc.aether < a.cost) { Notify.Combat("Nicht genug Äther für " + a.name + "."); return; }
                pc.aether -= a.cost;
            }
            cooldownUntil[slot] = Time.time + a.cooldown;
            motor.FaceAim(0.5f);
            Execute(a);
        }

        void Execute(AbilityDef a)
        {
            var w = pc.Weapon;
            Color c = pc.cls.color;
            switch (a.kind)
            {
                case AbilityKind.PowerStrike:
                    if (w.weaponKind == WeaponKind.Nahkampf) StartSwing(a.power, false, 0.3f);
                    else if (w.weaponKind == WeaponKind.Bogen) ShootArrow(1.5f, a.power, 0f, 1);
                    else CastBolt(0.5f, a.power, 1.5f, 0, false);
                    break;

                case AbilityKind.Heal:
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, healPerSecond = pc.MaxHp * a.power / a.duration });
                    Fx.Ring(transform.position + Vector3.up * 0.1f, 1.5f, new Color(0.4f, 1f, 0.5f), 0.6f);
                    break;

                case AbilityKind.Spin:
                    StartSwing(a.power, true, a.radius - w.length);
                    Fx.Ring(transform.position + Vector3.up * 0.2f, a.radius, c, 0.35f);
                    break;

                case AbilityKind.Charge:
                {
                    // Ansturm: Δv = 20 m/s in 0,35 s ⇒ a ≈ 57 m/s² zusätzlich zum Schwung.
                    motor.Dash(transform.forward * 20f);
                    float approach = 20f;
                    Vector3 start = transform.position;
                    var hits = Gather(start + transform.forward * a.radius * 0.5f + Vector3.up, a.radius * 0.6f);
                    foreach (var h in hits)
                    {
                        Strike s = PhysicsCombat.Melee(w.mass + 2f, Mathf.Max(0.6f, w.length), a.duration, approach, a.power * pc.DamageMult);
                        Hit(h, s, transform.forward, false);
                    }
                    Fx.Burst(transform.position + Vector3.up, c, 12);
                    break;
                }

                case AbilityKind.Bolt:
                    CastBolt(0.8f, a.power, a.radius, 0, false);
                    break;

                case AbilityKind.Nova:
                {
                    Strike s = PhysicsCombat.Spell(1f, AetherAccel(), a.power * pc.DamageMult);
                    Fx.Ring(transform.position + Vector3.up * 0.2f, a.radius, new Color(0.6f, 0.9f, 1f), 0.5f);
                    foreach (var h in Gather(transform.position + Vector3.up, a.radius))
                    {
                        Vector3 d = h.transform.position - transform.position;
                        d.y = 0f;
                        Hit(h, s, d.normalized, true);
                        var cr = h.GetComponent<Creature>();
                        if (cr != null) cr.Slow(0.5f, a.duration);
                    }
                    break;
                }

                case AbilityKind.Volley:
                    for (int i = -2; i <= 2; i++) ShootArrow(0.8f, a.power, i * 6f, 0);
                    break;

                case AbilityKind.Pierce:
                    ShootArrow(a.power, 1f, 0f, 5);
                    break;

                case AbilityKind.Blink:
                {
                    Vector3 target = transform.position + transform.forward * a.radius;
                    target.y = WorldBootstrap.GroundHeight(target) + 0.1f;
                    Fx.Burst(transform.position + Vector3.up, new Color(0.3f, 0.2f, 0.4f), 10);
                    motor.Teleport(target, transform.eulerAngles.y);
                    nextCrit = true;
                    nextCritUntil = Time.time + a.duration;
                    Fx.Burst(transform.position + Vector3.up, new Color(0.3f, 0.2f, 0.4f), 10);
                    break;
                }

                case AbilityKind.PoisonBuff:
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, poison = true });
                    break;

                case AbilityKind.Slam:
                {
                    // Erdstoß: Körper- plus Waffenmasse × Aufprallbeschleunigung.
                    Strike s = PhysicsCombat.Body(pc.health.mass, 3f + pc.Str * 0.05f, a.power * pc.DamageMult);
                    Fx.Ring(transform.position + Vector3.up * 0.1f, a.radius, new Color(0.8f, 0.6f, 0.3f), 0.5f);
                    Fx.Burst(transform.position, new Color(0.5f, 0.4f, 0.3f), 16);
                    if (World.Rig != null) World.Rig.Shake(0.8f);
                    foreach (var h in Gather(transform.position + Vector3.up * 0.5f, a.radius))
                    {
                        Vector3 d = h.transform.position - transform.position;
                        d.y = 0f;
                        Hit(h, s, (d.normalized + Vector3.up * 0.6f).normalized, false);
                    }
                    break;
                }

                case AbilityKind.Rage:
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, attackSpeedMult = 1.35f, lifesteal = 0.15f, damageTakenMult = 1.2f });
                    Fx.Ring(transform.position + Vector3.up * 0.1f, 2f, new Color(0.9f, 0.2f, 0.15f), 0.5f);
                    break;

                case AbilityKind.RuneBuff:
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, rune = true });
                    break;

                case AbilityKind.ShieldWall:
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, damageTakenMult = a.power, knockbackImmune = true });
                    Fx.Ring(transform.position + Vector3.up, 1.6f, new Color(0.8f, 0.85f, 1f), a.duration);
                    break;

                case AbilityKind.Counter:
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, parry = true });
                    break;

                case AbilityKind.WarCry:
                {
                    Fx.Ring(transform.position + Vector3.up * 0.2f, a.radius, new Color(1f, 0.7f, 0.3f), 0.6f);
                    foreach (var h in Gather(transform.position, a.radius))
                    {
                        Vector3 d = h.transform.position - transform.position;
                        d.y = 0f;
                        var cr = h.GetComponent<Creature>();
                        // F = 1200 N über Δt = 0,1 s ⇒ J = 120 N·s, Δv = J / m.
                        if (cr != null) cr.Knockback(d.normalized * 1200f * GameConstants.ImpactTime * 2f);
                    }
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, damageMult = a.power });
                    break;
                }

                case AbilityKind.HomingArrow:
                    for (int i = -1; i <= 1; i++)
                    {
                        CastBolt(0.4f, a.power, 0f, 0, true);
                    }
                    break;

                case AbilityKind.RaiseDead:
                {
                    int raised = 0;
                    foreach (var cr in Creature.Instances.ToArray())
                    {
                        if (raised >= 2) break;
                        if (cr == null || !cr.IsCorpse || cr.def.diet == Diet.Konstrukt) continue;
                        if ((cr.transform.position - transform.position).magnitude > a.radius) continue;
                        cr.RaiseAsUndead(a.duration);
                        raised++;
                    }
                    Notify.Combat(raised > 0 ? raised + " Gefallene erheben sich für " + a.duration + " s." : "Keine Leichen in der Nähe.");
                    break;
                }

                case AbilityKind.BloodLance:
                    CastBolt(1.2f, a.power, 0f, 6, false);
                    break;

                case AbilityKind.Bounty:
                {
                    var t = World.Rig != null ? World.Rig.AimTarget(a.radius) : null;
                    if (t != null && CanHit(t))
                    {
                        bountyTarget = t;
                        bountyUntil = Time.time + a.duration;
                        Notify.Combat("Kopfgeld auf " + t.displayName + " ausgesetzt.");
                    }
                    else Notify.Combat("Kein Ziel anvisiert.");
                    break;
                }

                case AbilityKind.Tame:
                {
                    var t = World.Rig != null ? World.Rig.AimTarget(a.radius) : null;
                    var cr = t != null ? t.GetComponent<Creature>() : null;
                    if (cr != null && cr.def.ecosystem && t.Fraction < 0.4f) cr.Tame();
                    else Notify.Combat("Zähmen gelingt nur bei geschwächten Wildtieren (< 40 % LP) in 6 m Nähe.");
                    break;
                }

                case AbilityKind.BladeDance:
                    StartCoroutine(BladeDance(a));
                    break;

                case AbilityKind.Pioneer:
                    pc.AddBuff(new Buff { id = a.id, name = a.name, until = Time.time + a.duration, statMult = 1f + a.power, gatherMult = 2f });
                    break;
            }
        }

        System.Collections.IEnumerator BladeDance(AbilityDef a)
        {
            var w = pc.Weapon;
            for (int i = 0; i < 6; i++)
            {
                Fx.Ring(transform.position + Vector3.up * 0.6f, a.radius, new Color(0.8f, 0.8f, 0.9f), 0.2f);
                foreach (var h in Gather(transform.position + Vector3.up, a.radius))
                {
                    Vector3 d = h.transform.position - transform.position;
                    d.y = 0f;
                    Strike s = PhysicsCombat.Melee(w.mass, Mathf.Max(0.5f, w.length), a.duration / 6f, 0f, a.power * pc.DamageMult);
                    Hit(h, s, d.normalized, false);
                }
                yield return new WaitForSeconds(a.duration / 6f);
            }
        }

        // Konter nach einer gelungenen Parade (Duellant).
        public void Counter(Health attacker)
        {
            if (attacker == null || attacker.IsDead) return;
            var w = pc.Weapon;
            Strike s = PhysicsCombat.Melee(w.mass, Mathf.Max(0.6f, w.length), pc.SwingTime(w), 0f, 3f * pc.DamageMult);
            Vector3 d = attacker.transform.position - transform.position;
            d.y = 0f;
            Hit(attacker, s, d.normalized, false);
        }
    }
}
