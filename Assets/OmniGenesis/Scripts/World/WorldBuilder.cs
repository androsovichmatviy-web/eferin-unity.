using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Wichtige Orte für NPCs und Interaktionen (werden beim Bau gesetzt).
    public static class Places
    {
        public static Vector3 CityPlaza, CityGate, LazarettSpawn, EmperorPos, JudgePos, MerchantA, MerchantB,
            SmithPos, AnvilPos, LandlordPos, HealerPos, TrialMasterPos, GuildMasterPos, GuildHall, ArenaCenter,
            VillageCenter, VillageGate, VillageTraderPos, MercenaryPos, BorderGate, BorderPostL, BorderPostR,
            SealPos, RiftPos, CourtPos, CraterRim, ForestEdge, CampPos;
        public static float EmperorYaw, JudgeYaw;
        public static readonly List<Vector3> CityPatrol = new List<Vector3>();
        public static readonly List<Vector3> CityWalk = new List<Vector3>();
        public static readonly List<Vector3> VillageHomes = new List<Vector3>();
        public static readonly List<Vector3> VillageWalk = new List<Vector3>();
        public static GameObject ArenaGate;
        public static Transform VillageRoot;

        public static void Clear()
        {
            CityPatrol.Clear();
            CityWalk.Clear();
            VillageHomes.Clear();
            VillageWalk.Clear();
        }
    }

    public static class WorldBuilder
    {
        static Vector3 G(float x, float z) { return new Vector3(x, Land.Height(x, z), z); }
        static Vector3 G(Vector2 p) { return G(p.x, p.y); }

        static readonly Color Stone = new Color(0.72f, 0.7f, 0.66f);
        static readonly Color DarkStone = new Color(0.45f, 0.44f, 0.42f);
        static readonly Color Plaster = new Color(0.86f, 0.82f, 0.72f);
        static readonly Color RoofRed = new Color(0.55f, 0.22f, 0.16f);
        static readonly Color RoofBlue = new Color(0.12f, 0.2f, 0.42f);
        static readonly Color Wood = new Color(0.45f, 0.3f, 0.18f);
        static readonly Color Gold = new Color(0.95f, 0.78f, 0.3f);

        // ---------- Grad Genesis (Art. 20, 110: Kronendomäne) ----------
        public static void BuildCity(Transform root)
        {
            var city = new GameObject("Grad Genesis").transform;
            city.SetParent(root, false);
            Vector2 c = WorldLayout.City;
            float y = Land.CityHeight;

            // Stadtmauer mit drei Toren (Nord, Ost, West). Im Süden steht der Thronsaal.
            int segments = 40;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * 360f / segments, a1 = (i + 1) * 360f / segments;
                float mid = (a0 + a1) * 0.5f;
                if (Mathf.Abs(Mathf.DeltaAngle(mid, 90f)) < 7f || Mathf.Abs(Mathf.DeltaAngle(mid, 0f)) < 7f || Mathf.Abs(Mathf.DeltaAngle(mid, 180f)) < 7f) continue;
                float r = WorldLayout.CityRadius;
                Vector3 p0 = new Vector3(c.x + Mathf.Cos(a0 * Mathf.Deg2Rad) * r, y, c.y + Mathf.Sin(a0 * Mathf.Deg2Rad) * r);
                Vector3 p1 = new Vector3(c.x + Mathf.Cos(a1 * Mathf.Deg2Rad) * r, y, c.y + Mathf.Sin(a1 * Mathf.Deg2Rad) * r);
                Vector3 m = (p0 + p1) * 0.5f;
                var seg = Build.Box(city, m + Vector3.up * 3.5f, new Vector3((p1 - p0).magnitude + 0.3f, 7f, 1.6f), Stone);
                seg.transform.rotation = Quaternion.LookRotation(Vector3.Cross(p1 - p0, Vector3.up));
                if (i % 4 == 0) Build.Prim(PrimitiveType.Cylinder, city, m + Vector3.up * 4.5f, new Vector3(3f, 4.5f, 3f), DarkStone);
            }
            foreach (float a in new[] { 90f, 0f, 180f })
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float aa = (a + s * 7.5f) * Mathf.Deg2Rad;
                    Vector3 p = new Vector3(c.x + Mathf.Cos(aa) * WorldLayout.CityRadius, y, c.y + Mathf.Sin(aa) * WorldLayout.CityRadius);
                    Build.Prim(PrimitiveType.Cylinder, city, p + Vector3.up * 5f, new Vector3(3.4f, 5f, 3.4f), DarkStone);
                    Build.Banner(city, p + Vector3.up * 0.1f + (p - G(c)).normalized * 2.2f, a + 90f, 7f);
                }
            }

            // Marktplatz
            Places.CityPlaza = new Vector3(c.x, y, c.y + 8f);
            Color[] canopy = { new Color(0.75f, 0.2f, 0.2f), new Color(0.2f, 0.45f, 0.75f), new Color(0.85f, 0.7f, 0.2f), new Color(0.3f, 0.6f, 0.3f) };
            Vector2[] stalls = { new Vector2(-7f, 12f), new Vector2(7f, 12f), new Vector2(-7f, 2f), new Vector2(7f, 2f) };
            for (int i = 0; i < stalls.Length; i++)
            {
                var t = Build.Group("Marktstand", city, new Vector3(c.x + stalls[i].x, y, c.y + stalls[i].y), stalls[i].x < 0 ? 90f : -90f);
                Build.Box(t, new Vector3(0f, 0.5f, 0f), new Vector3(3f, 1f, 1.2f), Wood);
                for (int k = 0; k < 4; k++)
                    Build.Box(t, new Vector3(k < 2 ? -1.4f : 1.4f, 1.2f, k % 2 == 0 ? -0.9f : 0.9f), new Vector3(0.12f, 2.4f, 0.12f), Wood);
                Build.Box(t, new Vector3(0f, 2.45f, 0f), new Vector3(3.4f, 0.12f, 2.2f), canopy[i], false);
            }
            Places.MerchantA = new Vector3(c.x - 4.6f, y, c.y + 12f);
            Places.MerchantB = new Vector3(c.x + 4.6f, y, c.y + 12f);
            var well = Build.Prim(PrimitiveType.Cylinder, city, new Vector3(c.x, y + 0.5f, c.y + 7f), new Vector3(2.4f, 0.5f, 2.4f), DarkStone);
            well.name = "Brunnen";

            // Großes Tor in der Stadt (Art. 34)
            Places.CityGate = new Vector3(c.x, y, c.y + 30f);
            BuildGreatGate(city, Places.CityGate, 0f, "Grad Genesis");

            // Thronsaal des Kaisers
            Vector3 palacePos = new Vector3(c.x, y, c.y - 26f);
            var palace = Build.Hall(city, palacePos, 0f, 26f, 18f, 10f, new Color(0.88f, 0.86f, 0.8f), RoofBlue, new Color(0.3f, 0.32f, 0.4f), 4f);
            palace.name = "Thronsaal";
            Build.Box(palace, new Vector3(0f, 0.2f, -2f), new Vector3(3f, 0.05f, 14f), new Color(0.5f, 0.08f, 0.1f), false);
            Build.Box(palace, new Vector3(0f, 0.5f, -6.5f), new Vector3(6f, 1f, 3f), new Color(0.35f, 0.35f, 0.42f));
            var throne = Build.Box(palace, new Vector3(0f, 1.8f, -7.4f), new Vector3(2f, 1.6f, 1.2f), Gold, true, 0.3f);
            throne.name = "Thron der Genesis";
            Build.Box(palace, new Vector3(0f, 3.4f, -7.9f), new Vector3(2.2f, 3.2f, 0.3f), Gold, false, 0.3f);
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                Build.Prim(PrimitiveType.Cylinder, city, palacePos + new Vector3(i * 4.5f, 5f, 10f), new Vector3(1f, 5f, 1f), new Color(0.92f, 0.9f, 0.85f));
                Build.Prim(PrimitiveType.Cylinder, palace, new Vector3(i * 4.5f, 4.5f, -2f), new Vector3(0.9f, 4.5f, 0.9f), new Color(0.85f, 0.83f, 0.78f));
            }
            Build.Box(city, palacePos + new Vector3(0f, 10.2f, 10f), new Vector3(22f, 0.6f, 2.4f), new Color(0.88f, 0.86f, 0.8f));
            Build.Banner(city, palacePos + new Vector3(-6f, 0f, 9.5f), 0f, 8f);
            Build.Banner(city, palacePos + new Vector3(6f, 0f, 9.5f), 0f, 8f);
            Places.EmperorPos = palacePos + new Vector3(0f, 1f, -5.6f);
            Places.EmperorYaw = 0f;

            // Kaiserliche Kanzlei mit Gericht (Richter Ataklas)
            Vector3 kanzlei = new Vector3(c.x - 26f, y, c.y - 6f);
            var k1 = Build.Hall(city, kanzlei, 90f, 12f, 10f, 6f, Plaster, RoofBlue, new Color(0.4f, 0.32f, 0.25f), 2.4f);
            k1.name = "Kanzlei und Gericht";
            Build.Box(k1, new Vector3(0f, 0.6f, -1.8f), new Vector3(3.6f, 1.1f, 1f), Wood);
            Places.JudgePos = kanzlei + Quaternion.Euler(0f, 90f, 0f) * new Vector3(0f, 0f, -3f);
            Places.JudgeYaw = 90f;
            Places.CourtPos = kanzlei + new Vector3(7.5f, 0f, 0f);
            Build.Banner(city, kanzlei + new Vector3(6.6f, 0f, 3.2f), 90f, 5f);

            // Mietshaus (Art. 116)
            Vector3 miet = new Vector3(c.x - 26f, y, c.y + 16f);
            var m1 = Build.Hall(city, miet, 90f, 10f, 8f, 5f, Plaster, RoofRed, new Color(0.45f, 0.33f, 0.22f), 2f);
            m1.name = "Mietshaus";
            var bed = Build.Box(m1, new Vector3(-3f, 0.4f, -2.4f), new Vector3(1.4f, 0.5f, 2.2f), new Color(0.75f, 0.72f, 0.65f));
            bed.AddComponent<Bed>();
            var chest = Build.Box(m1, new Vector3(3f, 0.45f, -2.8f), new Vector3(1.2f, 0.8f, 0.7f), Wood);
            chest.AddComponent<StorageChest>();
            Places.LandlordPos = miet + new Vector3(6.2f, 0f, 2.2f);

            // Schmiede
            Vector3 smithy = new Vector3(c.x + 24f, y, c.y + 16f);
            var s1 = Build.Group("Schmiede", city, smithy, 0f);
            for (int k = 0; k < 4; k++) Build.Box(s1, new Vector3(k < 2 ? -3f : 3f, 1.8f, k % 2 == 0 ? -2.5f : 2.5f), new Vector3(0.3f, 3.6f, 0.3f), Wood);
            Build.Roof(s1, new Vector3(0f, 3.6f, 0f), 6.8f, 5.6f, 1.6f, DarkStone);
            Build.Box(s1, new Vector3(-1.8f, 0.8f, -1.8f), new Vector3(2f, 1.6f, 1.4f), DarkStone);
            Build.Box(s1, new Vector3(-1.8f, 1.65f, -1.8f), new Vector3(1.2f, 0.2f, 0.8f), new Color(1f, 0.45f, 0.1f), false, 3f);
            var anvil = Build.Box(s1, new Vector3(1f, 0.5f, -0.5f), new Vector3(1f, 1f, 0.5f), new Color(0.25f, 0.25f, 0.28f));
            anvil.name = "Amboss";
            anvil.AddComponent<Anvil>();
            Places.AnvilPos = anvil.transform.position;
            Places.SmithPos = smithy + new Vector3(1.8f, 0f, 1.2f);

            // Kaiserliches Lazarett (Art. 45)
            Vector3 laz = new Vector3(c.x + 26f, y, c.y - 8f);
            var l1 = Build.Hall(city, laz, -90f, 12f, 9f, 6f, new Color(0.95f, 0.95f, 0.92f), RoofRed, new Color(0.8f, 0.8f, 0.78f), 2.4f);
            l1.name = "Kaiserliches Lazarett";
            for (int b = -1; b <= 1; b++) Build.Box(l1, new Vector3(b * 3.5f, 0.35f, -2.5f), new Vector3(1.2f, 0.4f, 2f), Color.white);
            Places.HealerPos = laz + new Vector3(-2.5f, 0f, 1.5f);
            Places.LazarettSpawn = laz + new Vector3(-7.5f, 0.2f, 0f);

            // Wohnhäuser
            float[] houseAngles = { 15f, 345f, 50f, 75f, 105f, 130f, 165f, 205f, 225f, 315f };
            for (int i = 0; i < houseAngles.Length; i++)
            {
                float a = houseAngles[i] * Mathf.Deg2Rad;
                Vector3 p = new Vector3(c.x + Mathf.Cos(a) * 37f, y, c.y + Mathf.Sin(a) * 37f);
                float yaw = Mathf.Atan2(c.x - p.x, c.y - p.z) * Mathf.Rad2Deg;
                Build.House(city, p, yaw, 6f, 5.5f, 4.5f + (i % 3), Plaster * (0.95f + 0.05f * (i % 2)), i % 2 == 0 ? RoofRed : RoofBlue);
            }

            // Wege für Wachen und Bürger
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                Places.CityPatrol.Add(new Vector3(c.x + Mathf.Cos(a) * 19f, y, c.y + 4f + Mathf.Sin(a) * 15f));
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Places.CityWalk.Add(new Vector3(c.x + Mathf.Cos(a) * 11f, y, c.y + 8f + Mathf.Sin(a) * 9f));
            }
        }

        // ---------- Gildenhaus der Abenteurer (Art. 121) und Arena (Kronendomäne außerhalb der Mauern) ----------
        public static void BuildOutskirts(Transform root)
        {
            var t = new GameObject("Gildenhaus der Abenteurer").transform;
            t.SetParent(root, false);
            Vector3 gp = G(WorldLayout.GuildPos);
            Places.GuildHall = gp;
            var hall = Build.Hall(t, gp, -90f, 14f, 11f, 7f, new Color(0.7f, 0.58f, 0.42f), new Color(0.25f, 0.35f, 0.25f), new Color(0.42f, 0.3f, 0.2f), 2.6f);
            hall.name = "Gildenhalle";
            Build.Box(hall, new Vector3(0f, 0.5f, -2f), new Vector3(5f, 1f, 1.4f), Wood);
            for (int i = -1; i <= 1; i++) Build.Box(hall, new Vector3(i * 3.5f, 0.45f, 2.5f), new Vector3(1.6f, 0.9f, 1.6f), Wood * 0.9f);
            var board = Build.Group("Auftragsbrett", t, gp + new Vector3(-7.5f, 0f, -3.5f), -90f);
            Build.Box(board, new Vector3(-1.1f, 1.1f, 0f), new Vector3(0.15f, 2.2f, 0.15f), Wood);
            Build.Box(board, new Vector3(1.1f, 1.1f, 0f), new Vector3(0.15f, 2.2f, 0.15f), Wood);
            Build.Box(board, new Vector3(0f, 1.6f, 0f), new Vector3(2.4f, 1.3f, 0.1f), Wood * 0.8f);
            for (int i = 0; i < 4; i++)
                Build.Box(board, new Vector3(-0.8f + i * 0.55f, 1.6f + (i % 2) * 0.2f, 0.07f), new Vector3(0.4f, 0.5f, 0.02f), new Color(0.95f, 0.9f, 0.75f), false, 0.3f);
            Places.GuildMasterPos = gp + new Vector3(-8f, 0f, 1.5f);
            Build.Banner(t, gp + new Vector3(-7.5f, 0f, 4f), -90f, 6f);

            var a = new GameObject("Arena der Prüfung").transform;
            a.SetParent(root, false);
            Vector3 ap = G(WorldLayout.ArenaPos);
            Places.ArenaCenter = ap;
            int segs = 30;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * 360f / segs, a1 = (i + 1) * 360f / segs;
                float mid = (a0 + a1) * 0.5f;
                Vector3 p0 = ap + new Vector3(Mathf.Cos(a0 * Mathf.Deg2Rad), 0f, Mathf.Sin(a0 * Mathf.Deg2Rad)) * 15f;
                Vector3 p1 = ap + new Vector3(Mathf.Cos(a1 * Mathf.Deg2Rad), 0f, Mathf.Sin(a1 * Mathf.Deg2Rad)) * 15f;
                var seg = Build.Box(a, (p0 + p1) * 0.5f + Vector3.up * 1.6f, new Vector3((p1 - p0).magnitude + 0.2f, 3.2f, 1.2f), Stone);
                seg.transform.rotation = Quaternion.LookRotation(Vector3.Cross(p1 - p0, Vector3.up));
                if (Mathf.Abs(Mathf.DeltaAngle(mid, 0f)) < 7f)
                {
                    seg.name = "Arenator";
                    seg.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Shared(new Color(0.3f, 0.6f, 0.9f), 0.3f, 1.2f);
                    Places.ArenaGate = seg;
                    seg.SetActive(false);
                }
                else
                {
                    var stand = Build.Box(a, (p0 + p1) * 0.5f * 1f + (((p0 + p1) * 0.5f) - ap).normalized * 2f + Vector3.up * 2.4f, new Vector3((p1 - p0).magnitude + 0.2f, 0.4f, 3f), Wood);
                    stand.transform.rotation = seg.transform.rotation;
                }
            }
            Places.TrialMasterPos = ap + new Vector3(17.5f, 0f, 3f);
            Build.Banner(a, ap + new Vector3(16.5f, 0f, -3.5f), 90f, 5f);
        }

        // ---------- Dorf Eichenhain (Provinz Grünmark) ----------
        public static void BuildVillage(Transform root)
        {
            var v = new GameObject("Eichenhain").transform;
            v.SetParent(root, false);
            Places.VillageRoot = v;
            Vector2 c = WorldLayout.Village;
            float y = Land.VillageHeight;
            Places.VillageCenter = new Vector3(c.x, y, c.y);

            float[] angles = { 25f, 70f, 115f, 160f, 205f, 250f, 295f };
            for (int i = 0; i < angles.Length; i++)
            {
                float a = angles[i] * Mathf.Deg2Rad;
                Vector3 p = new Vector3(c.x + Mathf.Cos(a) * 15f, y, c.y + Mathf.Sin(a) * 15f);
                float yaw = Mathf.Atan2(c.x - p.x, c.y - p.z) * Mathf.Rad2Deg;
                Build.House(v, p, yaw, 5.5f, 5f, 3.6f, new Color(0.78f, 0.7f, 0.55f), new Color(0.4f, 0.3f, 0.2f));
                Vector3 door = p + (new Vector3(c.x, y, c.y) - p).normalized * 3.6f;
                Places.VillageHomes.Add(door);
                Places.VillageHomes.Add(door + Quaternion.Euler(0f, 90f, 0f) * (new Vector3(c.x, y, c.y) - p).normalized * 1.5f);
            }
            Build.Prim(PrimitiveType.Cylinder, v, new Vector3(c.x + 6f, y + 0.5f, c.y - 6f), new Vector3(2f, 0.5f, 2f), DarkStone);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                Places.VillageWalk.Add(new Vector3(c.x + Mathf.Cos(a) * 8f, y, c.y + Mathf.Sin(a) * 8f));
            }

            var site = new GameObject("Bauplatz Rathaus");
            site.transform.SetParent(v, false);
            site.transform.position = new Vector3(c.x, y, c.y);
            site.AddComponent<TownHallSite>();

            Places.VillageGate = G(c.x + 14f, c.y + 22f);
            BuildGreatGate(v, Places.VillageGate, 30f, "Eichenhain");
            Places.VillageTraderPos = new Vector3(c.x - 7f, y, c.y - 5f);
            Places.MercenaryPos = new Vector3(c.x + 7f, y, c.y + 4f);
            var stall = Build.Group("Krämerstand", v, Places.VillageTraderPos + new Vector3(-1.4f, 0f, 0f), 90f);
            Build.Box(stall, new Vector3(0f, 0.5f, 0f), new Vector3(2.6f, 1f, 1f), Wood);
            Build.Box(stall, new Vector3(0f, 2.3f, 0f), new Vector3(3f, 0.1f, 2f), new Color(0.6f, 0.45f, 0.2f), false);
        }

        public static void BuildTownHall()
        {
            var v = Places.VillageRoot;
            if (v == null || v.Find("Rathaus") != null) return;
            var hall = Build.Hall(v, Places.VillageCenter + new Vector3(0f, 0f, -2f), 0f, 10f, 7f, 6f, new Color(0.85f, 0.8f, 0.68f), new Color(0.3f, 0.22f, 0.15f), new Color(0.45f, 0.33f, 0.22f), 2.2f);
            hall.name = "Rathaus";
            var site = v.GetComponentInChildren<TownHallSite>();
            if (site != null) site.transform.position = Places.VillageCenter + new Vector3(0f, 0f, 3f);
        }

        // ---------- Grenze zu den Verbotenen Landen (Art. 112) und der Riss ----------
        public static RiftSeal BuildForbidden(Transform root)
        {
            var b = new GameObject("Grenze der Verbotenen Lande").transform;
            b.SetParent(root, false);
            for (float x = -170f; x <= 170f; x += 24f)
            {
                if (Mathf.Abs(x) < 8f) continue;
                Vector3 p = G(x, WorldLayout.ForbiddenZ);
                Build.Prim(PrimitiveType.Cylinder, b, p + Vector3.up * 1.6f, new Vector3(0.7f, 1.6f, 0.7f), DarkStone);
                Build.Prim(PrimitiveType.Sphere, b, p + Vector3.up * 3.5f, Vector3.one * 0.7f, new Color(0.6f, 0.3f, 0.9f), false, 3f);
            }
            Vector3 post = G(0f, WorldLayout.ForbiddenZ - 2f);
            Build.Prim(PrimitiveType.Cylinder, b, post + new Vector3(-4f, 3f, 0f), new Vector3(1.6f, 3f, 1.6f), DarkStone);
            Build.Prim(PrimitiveType.Cylinder, b, post + new Vector3(4f, 3f, 0f), new Vector3(1.6f, 3f, 1.6f), DarkStone);
            Build.Box(b, post + new Vector3(0f, 6.3f, 0f), new Vector3(10f, 0.8f, 1.6f), DarkStone);
            Places.BorderPostL = post + new Vector3(-2.2f, 0f, -1.5f);
            Places.BorderPostR = post + new Vector3(2.2f, 0f, -1.5f);
            Places.BorderGate = G(16f, 94f);
            BuildGreatGate(b, Places.BorderGate, 0f, "Grenzposten");

            var r = new GameObject("Der Riss").transform;
            r.SetParent(root, false);
            Vector3 rp = G(WorldLayout.Rift);
            Places.RiftPos = rp;
            var rng = new System.Random(77);
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = (float)rng.NextDouble() * 9f;
                float h = 3f + (float)rng.NextDouble() * 9f;
                var crystal = Build.Box(r, rp + new Vector3(Mathf.Cos(a) * d, h * 0.4f, Mathf.Sin(a) * d), new Vector3(1.2f, h, 1.2f), new Color(0.35f, 0.08f, 0.5f), true, 2f);
                crystal.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 30f - 15f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f - 15f);
            }
            var glow = new GameObject("Rissleuchten");
            glow.transform.SetParent(r, false);
            glow.transform.position = rp + Vector3.up * 4f;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.6f, 0.2f, 1f);
            light.range = 40f;
            light.intensity = 3f;

            Places.SealPos = G(0f, 136f);
            Places.CraterRim = G(0f, 128f);
            var seal = new GameObject("Siegel der Eindämmung");
            seal.transform.SetParent(r, false);
            seal.transform.position = Places.SealPos;
            return seal.AddComponent<RiftSeal>();
        }

        // ---------- Orte der Geschichte: Waldrand bei Eichenhain und das Lager von Haus Vorn ----------
        public static void BuildStoryPlaces(Transform root)
        {
            Places.ForestEdge = G(WorldLayout.ForestEdge);
            Places.CampPos = G(WorldLayout.Camp);

            var camp = Build.Group("Lager von Haus Vorn", root, Places.CampPos, 0f);
            Color cloth = new Color(0.25f, 0.15f, 0.3f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a) * 9f, 0f, Mathf.Sin(a) * 9f);
                var tent = Build.Group("Zelt", camp, Places.CampPos + p, -i * 72f + 90f);
                tent.position = new Vector3(tent.position.x, Land.Height(tent.position.x, tent.position.z), tent.position.z);
                Build.Roof(tent, new Vector3(0f, 0f, 0f), 3.2f, 3.6f, 2.2f, cloth);
            }
            Build.Prim(PrimitiveType.Cylinder, camp, Places.CampPos + Vector3.up * 0.1f, new Vector3(2f, 0.1f, 2f), new Color(0.1f, 0.08f, 0.08f), false);
            Build.Prim(PrimitiveType.Sphere, camp, Places.CampPos + Vector3.up * 0.4f, new Vector3(0.9f, 0.6f, 0.9f), new Color(0.7f, 0.25f, 1f), false, 2.5f);
            Build.Banner(camp, Places.CampPos + new Vector3(3f, 0f, -3f), 30f, 5f, new Color(0.3f, 0.08f, 0.12f));
            var glow = new GameObject("Lagerfeuer");
            glow.transform.SetParent(camp, false);
            glow.transform.position = Places.CampPos + Vector3.up * 1.5f;
            var l = glow.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.7f, 0.3f, 1f);
            l.range = 14f;
            l.intensity = 2f;
        }

        // Großes Tor: Bogen mit leuchtendem Portal (Art. 34).
        static void BuildGreatGate(Transform parent, Vector3 pos, float yaw, string name)
        {
            var t = Build.Group("Großes Tor " + name, parent, pos, yaw);
            Build.Box(t, new Vector3(-3f, 3.5f, 0f), new Vector3(1.2f, 7f, 1.2f), DarkStone);
            Build.Box(t, new Vector3(3f, 3.5f, 0f), new Vector3(1.2f, 7f, 1.2f), DarkStone);
            Build.Box(t, new Vector3(0f, 7.3f, 0f), new Vector3(7.6f, 1f, 1.4f), DarkStone);
            Build.Box(t, new Vector3(0f, 7.3f, 0.75f), new Vector3(1.2f, 1.2f, 0.1f), Gold, false, 1.5f);
            var portal = Build.Box(t, new Vector3(0f, 3.4f, 0f), new Vector3(4.8f, 6.6f, 0.15f), new Color(0.35f, 0.7f, 1f), false, 2.2f);
            portal.name = "Portal";
            var gate = t.gameObject.AddComponent<GreatGate>();
            gate.gateName = name;
            gate.offset = Vector3.up * 1.2f;
            gate.radius = 4.5f;
        }

        // ---------- Wälder und Ressourcen ----------
        public static void PlantVegetation(Transform root, ResourceNodes nodes)
        {
            var forest = new GameObject("Wälder").transform;
            forest.SetParent(root, false);
            var rng = new System.Random(4242);
            float step = 7f;
            for (float x = -185f; x <= 185f; x += step)
            {
                for (float z = -185f; z <= 185f; z += step)
                {
                    float px = x + ((float)rng.NextDouble() - 0.5f) * 5f;
                    float pz = z + ((float)rng.NextDouble() - 0.5f) * 5f;
                    float roll = (float)rng.NextDouble();
                    float scale = 0.8f + (float)rng.NextDouble() * 0.5f;
                    int variant = rng.Next(0, 6);
                    float mask = Land.ForestMask(px, pz);
                    float h = Land.Height(px, pz);
                    if (mask <= 0f || h > 18f) continue;
                    bool forbidden = pz > WorldLayout.ForbiddenZ - 4f;
                    float chance = mask > 0.56f ? 0.8f : 0.035f;
                    if (Land.HillMask(px, pz) > 0.3f) chance *= 0.3f;
                    if (forbidden) chance *= 0.35f;
                    if (roll > chance) continue;
                    var tree = Build.Tree(forest, new Vector3(px, h - 0.1f, pz), scale, variant, forbidden);
                    nodes.Register(tree, NodeKind.Baum);
                }
            }

            var res = new GameObject("Rohstoffe").transform;
            res.SetParent(root, false);
            for (int i = 0; i < 32; i++)
            {
                Vector2 p = WorldLayout.Hills + Random2(rng) * 58f;
                Vector3 g = G(p);
                var ore = Build.Prim(PrimitiveType.Sphere, res, g + Vector3.up * 0.5f, new Vector3(1.8f, 1.3f, 1.6f), new Color(0.4f, 0.36f, 0.34f));
                Build.Prim(PrimitiveType.Cube, ore.transform, new Vector3(0.25f, 0.3f, 0.2f), Vector3.one * 0.3f, new Color(0.7f, 0.45f, 0.3f), false);
                Build.Prim(PrimitiveType.Cube, ore.transform, new Vector3(-0.2f, 0.25f, -0.25f), Vector3.one * 0.25f, new Color(0.7f, 0.45f, 0.3f), false);
                nodes.Register(ore, NodeKind.Erz);
            }
            for (int i = 0; i < 40; i++)
            {
                Vector2 p = new Vector2(((float)rng.NextDouble() - 0.5f) * 330f, ((float)rng.NextDouble() - 0.5f) * 200f - 10f);
                if (!FreeLand(p)) continue;
                var rock = Build.Prim(PrimitiveType.Sphere, res, G(p) + Vector3.up * 0.3f, new Vector3(1.6f, 1f, 1.3f), new Color(0.52f, 0.5f, 0.47f));
                nodes.Register(rock, NodeKind.Fels);
            }
            for (int i = 0; i < 80; i++)
            {
                Vector2 p = new Vector2(((float)rng.NextDouble() - 0.5f) * 330f, ((float)rng.NextDouble() - 0.5f) * 210f - 10f);
                if (!FreeLand(p)) continue;
                var herb = new GameObject("Heilkraut");
                herb.transform.SetParent(res, false);
                herb.transform.position = G(p);
                for (int k = 0; k < 4; k++)
                    Build.Prim(PrimitiveType.Sphere, herb.transform, new Vector3(Mathf.Cos(k * 1.6f) * 0.2f, 0.2f, Mathf.Sin(k * 1.6f) * 0.2f), new Vector3(0.25f, 0.35f, 0.25f), new Color(0.35f, 0.75f, 0.3f), false);
                Build.Prim(PrimitiveType.Sphere, herb.transform, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.15f, new Color(0.95f, 0.85f, 0.3f), false, 0.8f);
                nodes.Register(herb, NodeKind.Kraut);
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 p = new Vector2(((float)rng.NextDouble() - 0.5f) * 220f, WorldLayout.ForbiddenZ + 10f + (float)rng.NextDouble() * 50f);
                var ae = Build.Prim(PrimitiveType.Cube, res, G(p) + Vector3.up * 0.9f, new Vector3(0.9f, 1.8f, 0.9f), new Color(0.45f, 0.75f, 1f), true, 2.2f);
                ae.transform.rotation = Quaternion.Euler(10f, (float)rng.NextDouble() * 360f, 15f);
                ae.name = "Ätherader";
                nodes.Register(ae, NodeKind.Aether);
            }
        }

        static Vector2 Random2(System.Random rng)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Sqrt((float)rng.NextDouble());
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        public static bool FreeLand(Vector2 p)
        {
            if ((p - WorldLayout.City).magnitude < WorldLayout.CityRadius + 10f) return false;
            if ((p - WorldLayout.GuildPos).magnitude < 22f || (p - WorldLayout.ArenaPos).magnitude < 22f) return false;
            if ((p - WorldLayout.Village).magnitude < 27f) return false;
            if (WorldLayout.RoadDistance(p.x, p.y) < 3.5f) return false;
            if (Mathf.Abs(p.x) > 172f || Mathf.Abs(p.y) > 172f) return false;
            if (p.y > WorldLayout.ForbiddenZ - 2f) return false;
            return true;
        }
    }
}
