using UnityEngine;

namespace Eferin
{
    [RequireComponent(typeof(PartyManager))]
    public class CombatController : MonoBehaviour
    {
        public float attackCooldown = 0.45f;
        public float attackRadius = 2.2f;
        public float attackReach = 1.2f;
        public float energyPerHit = 6f;
        public float energyPerSkillHit = 12f;

        PartyManager party;
        float attackCooldownLeft;

        void Awake()
        {
            party = GetComponent<PartyManager>();
        }

        void Update()
        {
            attackCooldownLeft = Mathf.Max(0f, attackCooldownLeft - Time.deltaTime);
            if (GameState.GameplayBlocked) return;

            if (GameInput.AttackPressed()) NormalAttack();
            if (GameInput.Pressed(GameKey.Skill)) ElementalSkill();
            if (GameInput.Pressed(GameKey.Burst)) ElementalBurst();
        }

        void NormalAttack()
        {
            if (attackCooldownLeft > 0f) return;
            attackCooldownLeft = attackCooldown;

            var c = party.Active;
            Vector3 center = transform.position + transform.forward * attackReach;
            RingEffect.Spawn(center + Vector3.up * 0.1f, attackRadius, Color.white, 0.25f);
            HitArea(center, attackRadius, c.attack, ElementType.None, c, energyPerHit);
        }

        void ElementalSkill()
        {
            var c = party.Active;
            if (c.skillCooldownLeft > 0f) return;
            c.skillCooldownLeft = c.skillCooldown;

            Color color = Elements.ColorOf(c.element);
            RingEffect.Spawn(transform.position + Vector3.up * 0.1f, c.skillRadius, color, 0.5f);
            GameHud.Instance?.ShowWorldText(transform.position + Vector3.up * 2.4f, c.skillName, color);
            HitArea(transform.position, c.skillRadius, c.skillDamage, c.element, c, energyPerSkillHit);
        }

        void ElementalBurst()
        {
            var c = party.Active;
            if (!c.BurstReady) return;
            c.energy = 0f;

            Color color = Elements.ColorOf(c.element);
            RingEffect.Spawn(transform.position + Vector3.up * 0.1f, c.burstRadius, color, 0.8f);
            RingEffect.Spawn(transform.position + Vector3.up * 0.1f, c.burstRadius * 0.6f, Color.white, 0.6f);
            GameHud.Instance?.ShowBanner(c.burstName, color);
            HitArea(transform.position, c.burstRadius, c.burstDamage, c.element, c, 0f);
        }

        void HitArea(Vector3 center, float radius, float damage, ElementType element, CharacterData source, float energyPerTarget)
        {
            // Iterate over a snapshot because a hit can kill an enemy and remove it from the registry.
            var targets = Enemy.All.ToArray();
            foreach (var enemy in targets)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                Vector3 d = enemy.transform.position - center;
                d.y = 0f;
                if (d.magnitude > radius + enemy.radius) continue;
                enemy.TakeHit(damage, element, transform.position);
                if (energyPerTarget > 0f) party.ShareEnergy(source, energyPerTarget);
            }
        }
    }
}
