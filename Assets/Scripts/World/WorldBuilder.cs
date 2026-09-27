using UnityEngine;
using UnityEngine.Rendering;

namespace Eferin
{
    // Builds the placeholder open world at runtime so the scene only needs a single GameBootstrap object.
    public static class WorldBuilder
    {
        public const float HalfSize = 150f;
        const int Segments = 150;
        const float ShoreStartZ = -95f;
        const float ShoreEndZ = -112f;

        public static readonly Vector3 SpawnPoint = new Vector3(0f, 0f, -90f);

        public static float HeightAt(float x, float z)
        {
            float broad = (Mathf.PerlinNoise(x * 0.015f + 100f, z * 0.015f + 100f) - 0.5f) * 5f;
            float detail = (Mathf.PerlinNoise(x * 0.07f + 7f, z * 0.07f + 7f) - 0.5f) * 1.1f;
            float inland = 2.4f + broad + detail;

            // Rise into hills toward the far edges so the playable area feels like a valley.
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) / HalfSize;
            inland += Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, edge)) * 18f;

            float shore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ShoreStartZ, ShoreEndZ, z));
            return Mathf.Lerp(inland, -2.5f, shore);
        }

        public static Vector3 Build()
        {
            BuildGround();
            BuildSea();
            BuildBounds();
            BuildDecor();
            SetupLighting();
            Physics.SyncTransforms();
            return new Vector3(SpawnPoint.x, HeightAt(SpawnPoint.x, SpawnPoint.z) + 0.1f, SpawnPoint.z);
        }

        static void BuildGround()
        {
            int vertsPerSide = Segments + 1;
            var vertices = new Vector3[vertsPerSide * vertsPerSide];
            var uvs = new Vector2[vertices.Length];
            float step = HalfSize * 2f / Segments;

            for (int iz = 0; iz < vertsPerSide; iz++)
            {
                for (int ix = 0; ix < vertsPerSide; ix++)
                {
                    float x = -HalfSize + ix * step;
                    float z = -HalfSize + iz * step;
                    int i = iz * vertsPerSide + ix;
                    vertices[i] = new Vector3(x, HeightAt(x, z), z);
                    uvs[i] = new Vector2(ix / (float)Segments, iz / (float)Segments);
                }
            }

            var triangles = new int[Segments * Segments * 6];
            int t = 0;
            for (int iz = 0; iz < Segments; iz++)
            {
                for (int ix = 0; ix < Segments; ix++)
                {
                    int a = iz * vertsPerSide + ix;
                    int b = a + vertsPerSide;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                    triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
                }
            }

            var mesh = new Mesh { name = "EferinGround", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var ground = new GameObject("Ground");
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Lit(new Color(0.34f, 0.58f, 0.3f), 0.05f);
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void BuildSea()
        {
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea";
            Object.Destroy(sea.GetComponent<Collider>());
            sea.transform.position = new Vector3(0f, 0f, -200f);
            sea.transform.localScale = new Vector3(60f, 1f, 21f);
            sea.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Lit(new Color(0.18f, 0.49f, 0.69f), 0.92f);
        }

        static void BuildBounds()
        {
            var root = new GameObject("WorldBounds");
            float h = 60f;
            AddWall(root.transform, new Vector3(0f, h / 2f, HalfSize), new Vector3(HalfSize * 2f, h, 1f));
            // The sea is an opaque plane, so stop the player at the waterline instead of letting them walk under it.
            AddWall(root.transform, new Vector3(0f, h / 2f, -103f), new Vector3(HalfSize * 2f, h, 1f));
            AddWall(root.transform, new Vector3(HalfSize, h / 2f, 0f), new Vector3(1f, h, HalfSize * 2f));
            AddWall(root.transform, new Vector3(-HalfSize, h / 2f, 0f), new Vector3(1f, h, HalfSize * 2f));
        }

        static void AddWall(Transform parent, Vector3 center, Vector3 size)
        {
            var wall = new GameObject("Wall");
            wall.transform.SetParent(parent, false);
            wall.transform.position = center;
            wall.AddComponent<BoxCollider>().size = size;
        }

        static void BuildDecor()
        {
            var root = new GameObject("Decor").transform;
            var rng = new System.Random(1337);
            float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            var trunkMat = MaterialFactory.Lit(new Color(0.42f, 0.29f, 0.19f));
            var leafMats = new[]
            {
                MaterialFactory.Lit(new Color(0.2f, 0.45f, 0.24f)),
                MaterialFactory.Lit(new Color(0.27f, 0.55f, 0.27f)),
                MaterialFactory.Lit(new Color(0.55f, 0.7f, 0.3f)),
            };
            var rockMat = MaterialFactory.Lit(new Color(0.45f, 0.44f, 0.41f));

            int placed = 0, attempts = 0;
            while (placed < 170 && attempts++ < 2000)
            {
                float x = Range(-HalfSize * 0.92f, HalfSize * 0.92f);
                float z = Range(ShoreStartZ + 4f, HalfSize * 0.92f);
                if (Vector2.Distance(new Vector2(x, z), new Vector2(SpawnPoint.x, SpawnPoint.z)) < 8f) continue;
                float y = HeightAt(x, z);

                if (rng.NextDouble() < 0.68)
                {
                    float s = Range(0.8f, 1.6f);
                    var tree = new GameObject("Tree").transform;
                    tree.SetParent(root, false);
                    tree.position = new Vector3(x, y, z);
                    tree.localScale = Vector3.one * s;

                    var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    trunk.transform.SetParent(tree, false);
                    trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    trunk.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
                    trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

                    var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(crown.GetComponent<Collider>());
                    crown.transform.SetParent(tree, false);
                    crown.transform.localPosition = new Vector3(0f, 3.1f, 0f);
                    crown.transform.localScale = new Vector3(2.4f, 2.1f, 2.4f);
                    crown.GetComponent<Renderer>().sharedMaterial = leafMats[rng.Next(leafMats.Length)];
                }
                else
                {
                    var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rock.name = "Rock";
                    rock.transform.SetParent(root, false);
                    float s = Range(0.6f, 2.4f);
                    rock.transform.position = new Vector3(x, y + s * 0.25f, z);
                    rock.transform.localScale = new Vector3(s * Range(0.8f, 1.4f), s * Range(0.5f, 0.9f), s * Range(0.8f, 1.4f));
                    rock.transform.rotation = Quaternion.Euler(Range(-15f, 15f), Range(0f, 360f), Range(-15f, 15f));
                    rock.GetComponent<Renderer>().sharedMaterial = rockMat;
                }
                placed++;
            }
        }

        static void SetupLighting()
        {
            Light sun = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) { sun = light; break; }
            }
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            sun.color = new Color(1f, 0.95f, 0.84f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.78f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.65f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.3f, 0.22f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.87f, 0.97f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 260f;
        }
    }
}
