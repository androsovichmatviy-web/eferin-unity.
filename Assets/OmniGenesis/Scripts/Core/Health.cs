using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum Faction { Player, Ally, Citizen, Guard, Imperial, Wild, Void, Construct }

    public struct DamageInfo
    {
        public float amount;
        public Vector3 point;
        public Vector3 direction;
        public float impulse;
        public Health source;
        public bool crit;
        public bool magic;
        public string formula;
    }

    public class Health : MonoBehaviour
    {
        public static readonly List<Health> All = new List<Health>();

        public string displayName = "Wesen";
        public Faction faction = Faction.Wild;
        public float maxHp = 100f;
        public float hp = 100f;
        public float armor;
        public float mass = 70f;
        public bool invulnerable;
        public float height = 1.8f;

        // Der Spieler hängt hier Buffs ein (z. B. Schildwall, Konter).
        public Func<DamageInfo, DamageInfo> Modify;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;

        public bool IsDead { get { return hp <= 0f; } }
        public float Fraction { get { return maxHp <= 0f ? 0f : hp / maxHp; } }
        public float LastDamagedTime { get; private set; }
        public Health LastAttacker { get; private set; }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public float Apply(DamageInfo info)
        {
            if (IsDead) return 0f;
            if (Modify != null) info = Modify(info);
            float dmg = Mathf.Max(0f, info.amount) * (60f / (60f + Mathf.Max(0f, armor)));
            if (invulnerable) dmg = 0f;
            if (info.amount > 0f && dmg < 1f && !invulnerable) dmg = 1f;
            info.amount = dmg;

            LastDamagedTime = Time.time;
            LastAttacker = info.source;
            hp -= dmg;
            if (Damaged != null) Damaged(info);

            if (hp <= 0f)
            {
                hp = 0f;
                if (Died != null) Died(info);
            }
            return dmg;
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            hp = Mathf.Min(maxHp, hp + amount);
        }

        public void Revive(float fraction)
        {
            hp = Mathf.Max(1f, maxHp * fraction);
        }

        public Vector3 Center { get { return transform.position + Vector3.up * height * 0.55f; } }

        public static Health Nearest(Vector3 pos, float radius, Predicate<Health> filter)
        {
            Health best = null;
            float bestD = radius * radius;
            for (int i = 0; i < All.Count; i++)
            {
                var h = All[i];
                if (h == null || h.IsDead || !h.isActiveAndEnabled) continue;
                float d = (h.transform.position - pos).sqrMagnitude;
                if (d < bestD && filter(h))
                {
                    bestD = d;
                    best = h;
                }
            }
            return best;
        }
    }
}
