using UnityEngine;

namespace OmniGenesis
{
    // Zugriff auf alle Systeme der Welt-Szene.
    public static class World
    {
        public static bool Ready;
        public static SaveData Data;
        public static PlayerCharacter Player;
        public static CameraRig Rig;
        public static WorldClock Clock;
        public static Weather Weather;
        public static Market Market;
        public static Treasury Treasury;
        public static Law Law;
        public static Ecosystem Eco;
        public static RiftSeal Rift;
        public static Residents Residents;
        public static Autonomy Autonomy;
        public static Tower Tower;
        public static RankTrial Trial;
        public static UiWindows Ui;
        public static Hud Hud;
        public static ResourceNodes Nodes;

        public static void Reset()
        {
            Ready = false;
            Data = null;
            Player = null;
            Rig = null;
            Clock = null;
            Weather = null;
            Market = null;
            Treasury = null;
            Law = null;
            Eco = null;
            Rift = null;
            Residents = null;
            Autonomy = null;
            Tower = null;
            Trial = null;
            Ui = null;
            Hud = null;
            Nodes = null;
        }

        public static bool GameplayBlocked
        {
            get { return !Ready || (Ui != null && Ui.BlocksGameplay); }
        }

        public static int Day { get { return Clock != null ? Clock.Day : 1; } }
    }

    public enum Region { Kronendomaene, Gruenmark, Eichenhain, Erzhoehen, VerboteneLande, Turm }

    // Karte des Kontinents (Ausschnitt): Grad Genesis, Provinz Grünmark mit dem Dorf Eichenhain,
    // die Erzhöhen und die Verbotenen Lande mit dem Riss.
    public static class WorldLayout
    {
        public const float Half = 200f;

        public static readonly Vector2 City = new Vector2(0f, -120f);
        public const float CityRadius = 44f;
        public static readonly Vector2 Village = new Vector2(-100f, 40f);
        public const float VillageCore = 24f;
        public const float VillageRegion = 70f;
        public static readonly Vector2 Hills = new Vector2(120f, -20f);
        public static readonly Vector2 Rift = new Vector2(0f, 160f);
        public const float ForbiddenZ = 110f;
        public static readonly Vector2 TowerPos = new Vector2(68f, -120f);
        public static readonly Vector2 ArenaPos = new Vector2(-72f, -120f);
        public static readonly Vector3 TowerSpace = new Vector3(3000f, 0f, 0f);

        public static readonly Vector2[][] Roads =
        {
            new[] { new Vector2(0f, -76f), new Vector2(0f, 0f), new Vector2(0f, 104f) },
            new[] { new Vector2(0f, 0f), new Vector2(-50f, 22f), new Vector2(-100f, 40f) },
            new[] { new Vector2(0f, 0f), new Vector2(55f, -8f), new Vector2(112f, -18f) },
            new[] { new Vector2(44f, -120f), new Vector2(60f, -120f) },
            new[] { new Vector2(-44f, -120f), new Vector2(-58f, -120f) },
        };

        public static Vector2 XZ(Vector3 p) { return new Vector2(p.x, p.z); }

        public static Region RegionAt(Vector3 p)
        {
            if (p.x > 1000f) return Region.Turm;
            Vector2 q = XZ(p);
            if (q.y > ForbiddenZ) return Region.VerboteneLande;
            if ((q - City).magnitude < CityRadius + 6f) return Region.Kronendomaene;
            if ((q - TowerPos).magnitude < 22f || (q - ArenaPos).magnitude < 22f) return Region.Kronendomaene;
            if ((q - Village).magnitude < VillageRegion) return Region.Eichenhain;
            if ((q - Hills).magnitude < 70f) return Region.Erzhoehen;
            return Region.Gruenmark;
        }

        public static string RegionName(Region r)
        {
            switch (r)
            {
                case Region.Kronendomaene: return "Kronendomäne · Grad Genesis";
                case Region.Eichenhain: return "Provinz Grünmark · Eichenhain";
                case Region.Erzhoehen: return "Provinz Grünmark · Erzhöhen";
                case Region.VerboteneLande: return "Verbotene Lande";
                case Region.Turm: return "Der Turm";
                default: return "Provinz Grünmark";
            }
        }

        public static float RoadDistance(float x, float z)
        {
            Vector2 p = new Vector2(x, z);
            float best = float.MaxValue;
            for (int r = 0; r < Roads.Length; r++)
            {
                var road = Roads[r];
                for (int i = 0; i < road.Length - 1; i++)
                {
                    Vector2 a = road[i], b = road[i + 1];
                    Vector2 ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    float d = (a + ab * t - p).magnitude;
                    if (d < best) best = d;
                }
            }
            return best;
        }

        public static bool InVillageRegion(Vector3 p)
        {
            return p.x < 1000f && (XZ(p) - Village).magnitude < VillageRegion;
        }
    }
}
