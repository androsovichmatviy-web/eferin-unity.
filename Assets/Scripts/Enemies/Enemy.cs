using System.Collections.Generic;
using UnityEngine;

namespace Eferin
{
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public string displayName = "Забытый Клинок";
        public float maxHp = 100f;
        public float attack = 10f;
        public float moveSpeed = 3f;
        public float radius = 0.7f;
        public float aggroRange = 14f;
        public float leashRange = 26f;
        public float attackInterval = 1.2f;
        public float auraDuration = 8f;
        public float innateAuraRegen = 4f;
        public ElementType innateAura = ElementType.None;

        public float Hp { get; private set; }
        public ElementType Aura { get; private set; }
        public bool IsAlive => Hp > 0f;
        public bool IsFrozen => frozenLeft > 0f;
        public bool IsAggro { get; private set; }

        Transform player;
        Vector3 home;
        Renderer body;
        Color baseColor;
        LineRenderer auraRing;
        float auraLeft;
        float innateRegenLeft;
        float frozenLeft;
        float shredLeft;
        float dotLeft;
        float dotTick;
        float attackLeft;
        float bobPhase;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void OnDestroy()
        {
            if (body != null) Destroy(body.sharedMaterial);
            if (auraRing != null) Destroy(auraRing.sharedMaterial);
        }

        public void Setup(Transform playerTransform, Color color)
        {
            player = playerTransform;
            baseColor = color;
            Hp = maxHp;
            home = transform.position;
            bobPhase = Random.value * 10f;
            if (innateAura != ElementType.None) ApplyAura(innateAura, float.PositiveInfinity);

            var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mesh.name = "Body";
            Destroy(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(transform, false);
            mesh.transform.localScale = Vector3.one * radius * 2f;
            body = mesh.GetComponent<Renderer>();
            body.sharedMaterial = MaterialFactory.Lit(color, 0.4f);

            var ringGo = new GameObject("AuraRing");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = new Vector3(0f, radius + 0.35f, 0f);
            auraRing = ringGo.AddComponent<LineRenderer>();
            auraRing.sharedMaterial = MaterialFactory.Line();
            auraRing.useWorldSpace = false;
            auraRing.loop = true;
            auraRing.positionCount = 24;
            auraRing.startWidth = auraRing.endWidth = 0.08f;
            auraRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                auraRing.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.35f, 0f, Mathf.Sin(a) * 0.35f));
            }
            RefreshVisuals();
        }

        void Update()
        {
            if (!IsAlive || player == null) return;
            float dt = Time.deltaTime;

            if (Aura != ElementType.None)
            {
                auraLeft -= dt;
                if (auraLeft <= 0f) ClearAura();
            }
            else if (innateAura != ElementType.None)
            {
                innateRegenLeft -= dt;
                if (innateRegenLeft <= 0f) ApplyAura(innateAura, float.PositiveInfinity);
            }
            shredLeft = Mathf.Max(0f, shredLeft - dt);
            TickDamageOverTime(dt);
            if (!IsAlive) return;

            if (frozenLeft > 0f)
            {
                frozenLeft -= dt;
            }
            else
            {
                Think(dt);
            }

            Vector3 p = transform.position;
            p.y = WorldBuilder.HeightAt(p.x, p.z) + radius + 0.15f + (IsFrozen ? 0f : Mathf.Sin((Time.time + bobPhase) * 2f) * 0.08f);
            transform.position = p;
            auraRing.transform.Rotate(0f, 90f * dt, 0f, Space.Self);
            RefreshVisuals();
        }

        void Think(float dt)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            float fromHome = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(home.x, 0f, home.z));

            if (dist < aggroRange) IsAggro = true;
            if (fromHome > leashRange || dist > leashRange) IsAggro = false;

            attackLeft = Mathf.Max(0f, attackLeft - dt);
            if (IsAggro)
            {
                if (dist > radius + 1f)
                {
                    transform.position += toPlayer.normalized * moveSpeed * dt;
                }
                else if (attackLeft <= 0f)
                {
                    attackLeft = attackInterval;
                    PartyManager.Instance?.DamageActive(attack);
                }
            }
            else
            {
                Vector3 toHome = home - transform.position;
                toHome.y = 0f;
                if (toHome.magnitude > 0.5f) transform.position += toHome.normalized * moveSpeed * 0.6f * dt;
            }
        }

        void TickDamageOverTime(float dt)
        {
            if (dotLeft <= 0f) return;
            dotLeft -= dt;
            dotTick -= dt;
            if (dotTick <= 0f)
            {
                dotTick = 0.5f;
                ApplyDamage(6f, new Color(0.7f, 0.85f, 1f));
            }
        }

        public void TakeHit(float amount, ElementType element, Vector3 attackerPosition)
        {
            if (!IsAlive) return;
            IsAggro = true;

            float multiplier = 1f;
            if (element != ElementType.None)
            {
                if (ReactionTable.TryResolve(Aura, element, out Reaction reaction))
                {
                    multiplier = reaction.Multiplier;
                    ResolveReaction(reaction, attackerPosition);
                }
                else if (element != ElementType.Wind)
                {
                    ApplyAura(element, auraDuration);
                }
            }

            float damage = amount * multiplier * (shredLeft > 0f ? 1.15f : 1f);
            ApplyDamage(damage, multiplier > 1f ? Color.yellow : Color.white);
        }

        void ResolveReaction(Reaction reaction, Vector3 attackerPosition)
        {
            GameHud.Instance?.ShowWorldText(transform.position + Vector3.up * (radius + 1.2f), reaction.Name, reaction.Color, true);

            switch (reaction.Effect)
            {
                case ReactionEffect.Knockback:
                    Vector3 away = transform.position - attackerPosition;
                    away.y = 0f;
                    transform.position += away.normalized * 2.5f;
                    break;
                case ReactionEffect.DamageOverTime:
                    dotLeft = 3f;
                    dotTick = 0f;
                    break;
                case ReactionEffect.Freeze:
                    frozenLeft = 2.5f;
                    break;
                case ReactionEffect.DefenseShred:
                    shredLeft = 5f;
                    break;
                case ReactionEffect.Swirl:
                    SpreadAura();
                    break;
            }

            if (reaction.ConsumesAura) ClearAura();
        }

        void SpreadAura()
        {
            foreach (var other in All)
            {
                if (other == this || !other.IsAlive || other.Aura != ElementType.None) continue;
                if (Vector3.Distance(other.transform.position, transform.position) < 5f)
                    other.ApplyAura(Aura, other.auraDuration);
            }
            RingEffect.Spawn(transform.position, 5f, Elements.ColorOf(Aura), 0.5f);
        }

        void ApplyAura(ElementType element, float duration)
        {
            auraLeft = element == Aura ? Mathf.Max(auraLeft, duration) : duration;
            Aura = element;
        }

        void ClearAura()
        {
            Aura = ElementType.None;
            auraLeft = 0f;
            innateRegenLeft = innateAuraRegen;
        }

        void ApplyDamage(float amount, Color color)
        {
            int dmg = Mathf.Max(1, Mathf.RoundToInt(amount));
            Hp -= dmg;
            GameHud.Instance?.ShowWorldText(transform.position + Vector3.up * (radius + 0.6f), dmg.ToString(), color);
            GameHud.Instance?.SetTarget(this);
            if (Hp <= 0f) Die();
        }

        void Die()
        {
            Hp = 0f;
            GameHud.Instance?.ShowWorldText(transform.position + Vector3.up, "Искра рассеяна", new Color(0.91f, 0.78f, 0.49f));
            RingEffect.Spawn(transform.position, 2f, baseColor, 0.6f);
            Destroy(gameObject);
        }

        void RefreshVisuals()
        {
            MaterialFactory.SetColor(body.sharedMaterial, IsFrozen ? new Color(0.75f, 0.92f, 1f) : baseColor);
            bool hasAura = Aura != ElementType.None;
            auraRing.enabled = hasAura;
            if (hasAura) auraRing.startColor = auraRing.endColor = Elements.ColorOf(Aura);
        }
    }
}
