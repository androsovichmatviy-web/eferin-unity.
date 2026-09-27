using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eferin
{
    [Serializable]
    public class CharacterData
    {
        public string displayName;
        public string title;
        public ElementType element;
        public float maxHp;
        public float attack;
        public float energyMax;

        [Header("Стихийный навык (E)")]
        public string skillName;
        public float skillCooldown;
        public float skillRadius;
        public float skillDamage;

        [Header("Взрыв стихии (Q)")]
        public string burstName;
        public float burstRadius;
        public float burstDamage;

        [NonSerialized] public float hp;
        [NonSerialized] public float energy;
        [NonSerialized] public float skillCooldownLeft;

        public bool IsDown => hp <= 0f;
        public bool BurstReady => energy >= energyMax;

        public void ResetRuntime()
        {
            hp = maxHp;
            energy = 0f;
            skillCooldownLeft = 0f;
        }

        public void GainEnergy(float amount)
        {
            energy = Mathf.Min(energyMax, energy + amount);
        }

        static CharacterData Make(string name, string title, ElementType element, float hp, float atk, float energyMax,
            string skill, float skillCd, float skillRadius, float skillDmg,
            string burst, float burstRadius, float burstDmg)
        {
            return new CharacterData
            {
                displayName = name, title = title, element = element,
                maxHp = hp, attack = atk, energyMax = energyMax,
                skillName = skill, skillCooldown = skillCd, skillRadius = skillRadius, skillDamage = skillDmg,
                burstName = burst, burstRadius = burstRadius, burstDamage = burstDmg,
            };
        }

        public static List<CharacterData> DefaultRoster()
        {
            return new List<CharacterData>
            {
                Make("Странник", "Безымянный", ElementType.Wind, 260, 22, 80, "Порыв", 3.2f, 5f, 32, "Вихрь Искры", 8f, 70),
                Make("Эйла", "Странница Ордена", ElementType.Electro, 220, 26, 70, "Клятва Грозы", 4.5f, 6f, 40, "Суд Молний", 9f, 95),
                Make("Мал", "Кузнец-полудракон", ElementType.Fire, 320, 30, 80, "Молот Кузнеца", 4f, 4.5f, 48, "Дыхание Аргеона", 7.5f, 110),
                Make("Сая", "Изгнанная учёная", ElementType.Cryo, 200, 24, 65, "Ледяная Печать", 4.2f, 6.5f, 36, "Академический Разлом", 8.5f, 85),
                Make("Тиль", "Беспризорник Мерейны", ElementType.Hydro, 210, 21, 60, "Воровской Всплеск", 3.6f, 5.5f, 30, "Морской Узел", 8f, 80),
            };
        }
    }
}
