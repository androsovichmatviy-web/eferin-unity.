using UnityEngine;

namespace Eferin
{
    public enum ReactionEffect
    {
        None,
        Knockback,
        DamageOverTime,
        Freeze,
        DefenseShred,
        Swirl,
    }

    public readonly struct Reaction
    {
        public readonly string Name;
        public readonly float Multiplier;
        public readonly ReactionEffect Effect;
        public readonly Color Color;

        public Reaction(string name, float multiplier, ReactionEffect effect, Color color)
        {
            Name = name;
            Multiplier = multiplier;
            Effect = effect;
            Color = color;
        }

        // Freeze and Swirl leave the original aura on the target; every other reaction consumes it.
        public bool ConsumesAura => Effect != ReactionEffect.Freeze && Effect != ReactionEffect.Swirl;
    }

    public static class ReactionTable
    {
        public static bool TryResolve(ElementType aura, ElementType incoming, out Reaction reaction)
        {
            reaction = default;
            if (aura == ElementType.None || incoming == ElementType.None || aura == incoming)
                return false;

            if (incoming == ElementType.Wind)
            {
                reaction = new Reaction("Развеивание", 1.2f, ReactionEffect.Swirl, new Color(0.75f, 0.96f, 0.85f));
                return true;
            }

            if (Pair(aura, incoming, ElementType.Fire, ElementType.Hydro))
                reaction = new Reaction("Испарение", 1.5f, ReactionEffect.None, new Color(1f, 0.6f, 0.44f));
            else if (Pair(aura, incoming, ElementType.Fire, ElementType.Cryo))
                reaction = new Reaction("Плавление", 1.5f, ReactionEffect.None, new Color(1f, 0.69f, 0.48f));
            else if (Pair(aura, incoming, ElementType.Fire, ElementType.Electro))
                reaction = new Reaction("Перегрузка", 1.3f, ReactionEffect.Knockback, new Color(1f, 0.81f, 0.44f));
            else if (Pair(aura, incoming, ElementType.Hydro, ElementType.Electro))
                reaction = new Reaction("Наэлектризация", 1.2f, ReactionEffect.DamageOverTime, new Color(0.7f, 0.85f, 1f));
            else if (Pair(aura, incoming, ElementType.Hydro, ElementType.Cryo))
                reaction = new Reaction("Заморозка", 1f, ReactionEffect.Freeze, new Color(0.8f, 0.94f, 1f));
            else if (Pair(aura, incoming, ElementType.Electro, ElementType.Cryo))
                reaction = new Reaction("Сверхпроводимость", 1.2f, ReactionEffect.DefenseShred, new Color(0.84f, 0.76f, 1f));
            else
                return false;

            return true;
        }

        static bool Pair(ElementType a, ElementType b, ElementType x, ElementType y)
        {
            return (a == x && b == y) || (a == y && b == x);
        }
    }
}
