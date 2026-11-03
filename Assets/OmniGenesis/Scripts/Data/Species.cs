using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum Diet { Pflanzenfresser, Fleischfresser, Leere, Konstrukt, Kult }
    public enum Temper { Scheu, Territorial, Feindselig }
    public enum BodyShape { Vierbeiner, Kriecher, Leerenwesen, Golem, Mensch }

    public class SpeciesDef
    {
        public string id;
        public string name;
        public Diet diet;
        public Temper temper;
        public BodyShape shape;
        public float mass;          // kg
        public float hp;
        public float armor;
        public float speed;
        public float attackAccel;   // m/s² beim Sprung — F = Masse · attackAccel
        public float attackRange = 1.6f;
        public float aggroRange = 10f;
        public float size = 1f;
        public int capacity;        // Tragfähigkeit K des Lebensraums
        public int initial;
        public float breedChancePerHour;
        public int exp;
        public string[] drops = new string[0];
        public int[] dropCounts = new int[0];
        public Vector2 habitat;
        public float habitatRadius;
        public bool nocturnal;
        public bool ecosystem = true;  // false: Prüfungsgolems, Kultisten und Risswesen
        public Color color;

        // Fernkampf: Äthergeschoss mit F = m · a
        public bool ranged;
        public float projectileMass;
        public float projectileAccel;
    }

    public static class Species
    {
        public static readonly List<SpeciesDef> All = new List<SpeciesDef>
        {
            new SpeciesDef
            {
                id = "graslaeufer", name = "Grasläufer", diet = Diet.Pflanzenfresser, temper = Temper.Scheu, shape = BodyShape.Vierbeiner,
                mass = 45f, hp = 50f, speed = 6.5f, attackAccel = 0f, aggroRange = 12f, size = 1f,
                capacity = 22, initial = 16, breedChancePerHour = 0.1f, exp = 15,
                drops = new[] { "fleisch", "fell" }, dropCounts = new[] { 2, 1 },
                habitat = new Vector2(-40f, -10f), habitatRadius = 75f, color = new Color(0.72f, 0.55f, 0.35f),
            },
            new SpeciesDef
            {
                id = "wildeber", name = "Wildeber", diet = Diet.Pflanzenfresser, temper = Temper.Territorial, shape = BodyShape.Vierbeiner,
                mass = 85f, hp = 120f, armor = 4f, speed = 5f, attackAccel = 0.6f, aggroRange = 7f, size = 1.1f,
                capacity = 10, initial = 8, breedChancePerHour = 0.05f, exp = 35,
                drops = new[] { "fleisch", "fell" }, dropCounts = new[] { 3, 1 },
                habitat = new Vector2(60f, 50f), habitatRadius = 60f, color = new Color(0.35f, 0.25f, 0.2f),
            },
            new SpeciesDef
            {
                id = "grauwolf", name = "Grauwolf", diet = Diet.Fleischfresser, temper = Temper.Feindselig, shape = BodyShape.Vierbeiner,
                mass = 40f, hp = 70f, armor = 2f, speed = 7.5f, attackAccel = 0.8f, aggroRange = 14f, size = 0.9f,
                capacity = 8, initial = 6, breedChancePerHour = 0.04f, exp = 40, nocturnal = true,
                drops = new[] { "fell", "fleisch" }, dropCounts = new[] { 2, 1 },
                habitat = new Vector2(10f, 70f), habitatRadius = 80f, color = new Color(0.5f, 0.5f, 0.53f),
            },
            new SpeciesDef
            {
                id = "steinkriecher", name = "Steinkriecher", diet = Diet.Pflanzenfresser, temper = Temper.Territorial, shape = BodyShape.Kriecher,
                mass = 140f, hp = 220f, armor = 14f, speed = 3.2f, attackAccel = 0.4f, aggroRange = 7f, size = 1.2f,
                capacity = 6, initial = 5, breedChancePerHour = 0.02f, exp = 60,
                drops = new[] { "stein", "eisenerz" }, dropCounts = new[] { 3, 1 },
                habitat = new Vector2(125f, -20f), habitatRadius = 45f, color = new Color(0.45f, 0.43f, 0.4f),
            },
            new SpeciesDef
            {
                id = "leerenbrut", name = "Leerenbrut", diet = Diet.Leere, temper = Temper.Feindselig, shape = BodyShape.Leerenwesen,
                mass = 60f, hp = 160f, armor = 6f, speed = 5.5f, attackAccel = 1f, aggroRange = 25f, size = 1f,
                exp = 80, ecosystem = false, nocturnal = true,
                drops = new[] { "leerenessenz" }, dropCounts = new[] { 1 },
                habitat = new Vector2(0f, 150f), habitatRadius = 50f, color = new Color(0.35f, 0.1f, 0.5f),
            },
            new SpeciesDef
            {
                id = "risswaechter", name = "Wächter des Risses", diet = Diet.Leere, temper = Temper.Feindselig, shape = BodyShape.Golem,
                mass = 400f, hp = 2500f, armor = 25f, speed = 4f, attackAccel = 0.5f, attackRange = 3f, aggroRange = 30f, size = 2.6f,
                exp = 1500, ecosystem = false,
                drops = new[] { "aetherkristall", "leerenessenz" }, dropCounts = new[] { 5, 5 },
                habitat = new Vector2(0f, 160f), habitatRadius = 25f, color = new Color(0.2f, 0.05f, 0.3f),
            },
            // Meuterer von Haus Vorn (Hauptgeschichte). Sie nutzen Verbotene Künste und kämpfen auf Distanz.
            new SpeciesDef
            {
                id = "kultist", name = "Vermummter Kultist", diet = Diet.Kult, temper = Temper.Feindselig, shape = BodyShape.Mensch,
                mass = 75f, hp = 140f, armor = 4f, speed = 4.6f, attackAccel = 0.5f, attackRange = 14f, aggroRange = 24f, size = 1f,
                exp = 70, ecosystem = false, ranged = true, projectileMass = 0.3f, projectileAccel = 150f,
                drops = new[] { "leerenessenz" }, dropCounts = new[] { 1 },
                habitat = Vector2.zero, habitatRadius = 20f, color = new Color(0.25f, 0.12f, 0.3f),
            },
            new SpeciesDef
            {
                id = "kultmeisterin", name = "Kultmeisterin Sela", diet = Diet.Kult, temper = Temper.Feindselig, shape = BodyShape.Mensch,
                mass = 70f, hp = 700f, armor = 10f, speed = 4.8f, attackAccel = 0.6f, attackRange = 16f, aggroRange = 28f, size = 1.15f,
                exp = 400, ecosystem = false, ranged = true, projectileMass = 0.45f, projectileAccel = 180f,
                drops = new[] { "aetherkristall", "leerenessenz" }, dropCounts = new[] { 2, 2 },
                habitat = Vector2.zero, habitatRadius = 20f, color = new Color(0.45f, 0.1f, 0.35f),
            },
            new SpeciesDef
            {
                id = "kasimir", name = "Kasimir Vorn", diet = Diet.Kult, temper = Temper.Feindselig, shape = BodyShape.Mensch,
                mass = 90f, hp = 2200f, armor = 15f, speed = 5f, attackAccel = 0.8f, attackRange = 18f, aggroRange = 35f, size = 1.35f,
                exp = 1500, ecosystem = false, ranged = true, projectileMass = 0.6f, projectileAccel = 220f,
                drops = new[] { "aetherkristall", "leerenessenz" }, dropCounts = new[] { 4, 4 },
                habitat = Vector2.zero, habitatRadius = 20f, color = new Color(0.15f, 0.05f, 0.2f),
            },
            new SpeciesDef
            {
                id = "pruefungsgolem", name = "Prüfungsgolem", diet = Diet.Konstrukt, temper = Temper.Feindselig, shape = BodyShape.Golem,
                mass = 90f, hp = 80f, armor = 2f, speed = 4f, attackAccel = 0.45f, aggroRange = 40f, size = 1f,
                exp = 0, ecosystem = false,
                habitat = Vector2.zero, habitatRadius = 12f, color = new Color(0.55f, 0.6f, 0.7f),
            },
        };

        public static SpeciesDef Get(string id)
        {
            foreach (var s in All) if (s.id == id) return s;
            return null;
        }
    }
}
