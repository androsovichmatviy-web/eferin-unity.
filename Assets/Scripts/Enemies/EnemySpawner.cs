using UnityEngine;

namespace Eferin
{
    public static class EnemySpawner
    {
        struct Kind
        {
            public string name;
            public float hp;
            public float atk;
            public float radius;
            public Color color;
            public ElementType aura;
        }

        static readonly Kind Void = new Kind { name = "Забытый Клинок", hp = 90, atk = 10, radius = 0.6f, color = new Color(0.54f, 0.54f, 0.6f), aura = ElementType.None };
        static readonly Kind FireBearer = new Kind { name = "Носитель Пламени", hp = 110, atk = 12, radius = 0.7f, color = Elements.ColorOf(ElementType.Fire), aura = ElementType.Fire };
        static readonly Kind CryoBearer = new Kind { name = "Носитель Инея", hp = 100, atk = 10, radius = 0.7f, color = Elements.ColorOf(ElementType.Cryo), aura = ElementType.Cryo };
        static readonly Kind ElectroBearer = new Kind { name = "Носитель Грома", hp = 95, atk = 13, radius = 0.7f, color = Elements.ColorOf(ElementType.Electro), aura = ElementType.Electro };
        static readonly Kind HydroBearer = new Kind { name = "Носитель Потока", hp = 105, atk = 9, radius = 0.7f, color = Elements.ColorOf(ElementType.Hydro), aura = ElementType.Hydro };

        // Camps of the Забытые cult laid out from the shore inland.
        public static void SpawnAll(Transform player)
        {
            Spawn(Void, 12, -70, player);
            Spawn(FireBearer, 16, -66, player);

            Spawn(Void, -30, -40, player);
            Spawn(CryoBearer, -34, -36, player);
            Spawn(Void, -26, -34, player);

            Spawn(ElectroBearer, 40, -20, player);
            Spawn(Void, 44, -15, player);
            Spawn(HydroBearer, 36, -14, player);

            Spawn(FireBearer, -10, 10, player);
            Spawn(CryoBearer, -6, 14, player);
            Spawn(HydroBearer, -14, 14, player);

            Spawn(Void, 60, 40, player);
            Spawn(ElectroBearer, 64, 44, player);
            Spawn(HydroBearer, -60, 50, player);
        }

        static void Spawn(Kind kind, float x, float z, Transform player)
        {
            var go = new GameObject(kind.name);
            go.transform.position = new Vector3(x, WorldBuilder.HeightAt(x, z) + kind.radius, z);
            var enemy = go.AddComponent<Enemy>();
            enemy.displayName = kind.name;
            enemy.maxHp = kind.hp;
            enemy.attack = kind.atk;
            enemy.radius = kind.radius;
            enemy.innateAura = kind.aura;
            enemy.Setup(player, kind.color);
        }
    }
}
