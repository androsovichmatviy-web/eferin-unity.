using UnityEngine;

namespace Eferin
{
    public enum ElementType
    {
        None,
        Wind,
        Electro,
        Fire,
        Cryo,
        Hydro,
    }

    public static class Elements
    {
        public static Color ColorOf(ElementType element)
        {
            switch (element)
            {
                case ElementType.Wind: return new Color(0.56f, 0.94f, 0.75f);
                case ElementType.Electro: return new Color(0.79f, 0.65f, 1f);
                case ElementType.Fire: return new Color(1f, 0.54f, 0.36f);
                case ElementType.Cryo: return new Color(0.62f, 0.88f, 1f);
                case ElementType.Hydro: return new Color(0.29f, 0.66f, 0.88f);
                default: return Color.white;
            }
        }

        public static string NameOf(ElementType element)
        {
            switch (element)
            {
                case ElementType.Wind: return "Ветер";
                case ElementType.Electro: return "Гром";
                case ElementType.Fire: return "Пламя";
                case ElementType.Cryo: return "Иней";
                case ElementType.Hydro: return "Поток";
                default: return "—";
            }
        }
    }
}
