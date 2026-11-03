using UnityEngine;

namespace OmniGenesis
{
    // Höhenfunktion und Bodenfarbe des Kontinents. Alles wird aus Rauschen und der Karte (WorldLayout) berechnet.
    public static class Land
    {
        public const float CityHeight = 2f;
        public const float VillageHeight = 1.5f;

        public const int Chunks = 4;
        public const int Cells = 50;
        public const float CellSize = 2f;

        static float Fbm(float x, float z)
        {
            float s = 0f, a = 0.5f, f = 1f;
            for (int i = 0; i < 3; i++)
            {
                s += a * Mathf.PerlinNoise(x * f + 31.7f, z * f + 17.3f);
                a *= 0.5f;
                f *= 2.03f;
            }
            return s / 0.875f;
        }

        static float Sq(float v) { return v * v; }

        static float Dist(float x, float z, Vector2 c)
        {
            float dx = x - c.x, dz = z - c.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static float Flatten(float h, float x, float z, Vector2 c, float inner, float outer, float target)
        {
            float d = Dist(x, z, c);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - inner) / (outer - inner)));
            return Mathf.Lerp(target, h, t);
        }

        public static float HillMask(float x, float z)
        {
            return Sq(Mathf.Clamp01(1f - Dist(x, z, WorldLayout.Hills) / 75f));
        }

        public static float Height(float x, float z)
        {
            if (x > 1000f) return 0f;
            float h = (Fbm(x * 0.011f, z * 0.011f) - 0.5f) * 12f;
            h += HillMask(x, z) * 24f * (0.7f + 0.6f * Fbm(x * 0.03f + 50f, z * 0.03f));

            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            h += Sq(Mathf.Clamp01((edge - 160f) / 40f)) * 50f;

            h -= Sq(Mathf.Clamp01(1f - Dist(x, z, WorldLayout.Rift) / 32f)) * 10f;

            float road = WorldLayout.RoadDistance(x, z);
            if (road < 7f) h = Mathf.Lerp(h * 0.5f, h, Mathf.SmoothStep(0f, 1f, road / 7f));

            h = Flatten(h, x, z, WorldLayout.City, WorldLayout.CityRadius + 6f, WorldLayout.CityRadius + 26f, CityHeight);
            h = Flatten(h, x, z, WorldLayout.TowerPos, 18f, 32f, CityHeight);
            h = Flatten(h, x, z, WorldLayout.ArenaPos, 19f, 32f, CityHeight);
            h = Flatten(h, x, z, WorldLayout.Village, 26f, 42f, VillageHeight);
            return h;
        }

        public static float ForestMask(float x, float z)
        {
            float m = Fbm(x * 0.022f + 300f, z * 0.022f + 120f);
            if (Dist(x, z, WorldLayout.City) < WorldLayout.CityRadius + 14f) return 0f;
            if (Dist(x, z, WorldLayout.TowerPos) < 24f || Dist(x, z, WorldLayout.ArenaPos) < 24f) return 0f;
            if (Dist(x, z, WorldLayout.Village) < 30f) return 0f;
            if (WorldLayout.RoadDistance(x, z) < 5f) return 0f;
            return m;
        }

        public static Color32 GroundColor(float x, float z)
        {
            float h = Height(x, z);
            float n = Fbm(x * 0.09f + 7f, z * 0.09f + 3f);
            Color c = new Color(0.34f, 0.52f, 0.22f) * (0.85f + 0.3f * n);

            float forest = ForestMask(x, z);
            if (forest > 0.5f) c = Color.Lerp(c, new Color(0.2f, 0.34f, 0.15f), Mathf.Clamp01((forest - 0.5f) * 6f));

            float hill = HillMask(x, z) * Mathf.Clamp01((h - 5f) / 10f);
            c = Color.Lerp(c, new Color(0.46f, 0.43f, 0.38f) * (0.9f + 0.2f * n), hill);

            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (edge > 162f) c = Color.Lerp(c, new Color(0.5f, 0.5f, 0.5f), Mathf.Clamp01((h - 10f) / 10f));
            if (h > 30f) c = Color.Lerp(c, new Color(0.92f, 0.94f, 0.97f), Mathf.Clamp01((h - 30f) / 6f));

            if (z > 98f)
            {
                float t = Mathf.Clamp01((z - 98f) / 14f);
                c = Color.Lerp(c, new Color(0.3f, 0.27f, 0.33f) * (0.85f + 0.3f * n), t);
                float dr = Dist(x, z, WorldLayout.Rift);
                if (dr < 34f)
                {
                    float vein = Mathf.Abs(Mathf.PerlinNoise(x * 0.25f, z * 0.25f) - 0.5f);
                    if (vein < 0.05f) c = Color.Lerp(c, new Color(0.55f, 0.2f, 0.75f), 1f - dr / 34f);
                }
            }

            float road = WorldLayout.RoadDistance(x, z);
            if (road < 2.8f) c = Color.Lerp(new Color(0.52f, 0.43f, 0.3f) * (0.9f + 0.2f * n), c, Mathf.Clamp01((road - 2f) / 0.8f));

            float dv = Dist(x, z, WorldLayout.Village);
            if (dv < 25f) c = Color.Lerp(new Color(0.48f, 0.4f, 0.28f) * (0.9f + 0.2f * n), c, Mathf.Clamp01((dv - 22f) / 3f));

            float dc = Dist(x, z, WorldLayout.City);
            bool plaza = dc < WorldLayout.CityRadius + 1f || Dist(x, z, WorldLayout.TowerPos) < 17f || Dist(x, z, WorldLayout.ArenaPos) < 18f;
            if (plaza)
            {
                bool tile = ((Mathf.FloorToInt(x * 1.5f) + Mathf.FloorToInt(z * 1.5f)) & 1) == 0;
                c = new Color(0.58f, 0.56f, 0.52f) * (tile ? 1f : 0.93f) * (0.95f + 0.1f * n);
            }
            c.a = 1f;
            return c;
        }

        public static void FillTextureRows(Color32[] px, int size, int y0, int y1)
        {
            float step = WorldLayout.Half * 2f / size;
            for (int y = y0; y < y1; y++)
            {
                float z = -WorldLayout.Half + (y + 0.5f) * step;
                for (int x = 0; x < size; x++)
                {
                    float wx = -WorldLayout.Half + (x + 0.5f) * step;
                    px[y * size + x] = GroundColor(wx, z);
                }
            }
        }

        public static GameObject BuildChunk(int cx, int cz, Material mat, Transform parent)
        {
            int n = Cells + 1;
            var verts = new Vector3[n * n];
            var normals = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            float x0 = -WorldLayout.Half + cx * Cells * CellSize;
            float z0 = -WorldLayout.Half + cz * Cells * CellSize;
            float span = WorldLayout.Half * 2f;

            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    float x = x0 + i * CellSize;
                    float z = z0 + j * CellSize;
                    int k = j * n + i;
                    verts[k] = new Vector3(x, Height(x, z), z);
                    float hl = Height(x - 1f, z), hr = Height(x + 1f, z), hd = Height(x, z - 1f), hu = Height(x, z + 1f);
                    normals[k] = new Vector3(hl - hr, 2f, hd - hu).normalized;
                    uvs[k] = new Vector2((x + WorldLayout.Half) / span, (z + WorldLayout.Half) / span);
                }
            }

            var tris = new int[Cells * Cells * 6];
            int t = 0;
            for (int j = 0; j < Cells; j++)
            {
                for (int i = 0; i < Cells; i++)
                {
                    int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            }

            var mesh = new Mesh { name = "Gelaende_" + cx + "_" + cz };
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }
    }
}
