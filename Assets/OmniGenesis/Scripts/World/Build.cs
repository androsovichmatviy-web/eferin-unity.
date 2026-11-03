using UnityEngine;

namespace OmniGenesis
{
    // Bausteine aus Grundkörpern. Spätere Modelle aus Art/ und Prefabs/ können diese Platzhalter ersetzen.
    public static class Build
    {
        public static Transform Group(string name, Transform parent, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = pos;
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, bool collider = true, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            bool nonUniform = Mathf.Abs(scale.x - scale.y) > 0.01f || Mathf.Abs(scale.x - scale.z) > 0.01f;
            if (!collider)
            {
                Object.Destroy(go.GetComponent<Collider>());
            }
            else if (type == PrimitiveType.Cylinder || (type == PrimitiveType.Sphere && nonUniform))
            {
                // Zylinder haben standardmäßig eine Kapsel als Collider — eine flache Scheibe würde zur Riesenkugel.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Shared(color, 0.15f, emission);
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 localPos, Vector3 size, Color c, bool collider = true, float emission = 0f)
        {
            return Prim(PrimitiveType.Cube, parent, localPos, size, c, collider, emission);
        }

        // Satteldach entlang der lokalen x-Achse.
        public static void Roof(Transform parent, Vector3 localCenter, float width, float depth, float rise, Color c)
        {
            float half = width * 0.5f;
            float len = Mathf.Sqrt(half * half + rise * rise) + 0.3f;
            float ang = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
            var l = Box(parent, localCenter + new Vector3(-half * 0.5f, rise * 0.5f, 0f), new Vector3(len, 0.25f, depth + 0.6f), c);
            l.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            var r = Box(parent, localCenter + new Vector3(half * 0.5f, rise * 0.5f, 0f), new Vector3(len, 0.25f, depth + 0.6f), c);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, -ang);
            Box(parent, localCenter + new Vector3(0f, rise * 0.25f, depth * 0.5f), new Vector3(width * 0.9f, rise * 0.5f, 0.2f), c * 0.9f);
            Box(parent, localCenter + new Vector3(0f, rise * 0.25f, -depth * 0.5f), new Vector3(width * 0.9f, rise * 0.5f, 0.2f), c * 0.9f);
        }

        // Geschlossenes Wohnhaus (nicht begehbar). Vorderseite = lokale +z.
        public static Transform House(Transform parent, Vector3 pos, float yaw, float w, float d, float h, Color wall, Color roof)
        {
            var t = Group("Haus", parent, pos, yaw);
            Box(t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall);
            Roof(t, new Vector3(0f, h, 0f), w + 0.6f, d, h * 0.45f, roof);
            Box(t, new Vector3(0f, 1.1f, d * 0.5f + 0.03f), new Vector3(1.2f, 2.2f, 0.08f), new Color(0.35f, 0.22f, 0.12f), false);
            Box(t, new Vector3(-w * 0.28f, h * 0.6f, d * 0.5f + 0.03f), new Vector3(0.8f, 0.8f, 0.06f), new Color(0.95f, 0.85f, 0.55f), false, 0.4f);
            Box(t, new Vector3(w * 0.28f, h * 0.6f, d * 0.5f + 0.03f), new Vector3(0.8f, 0.8f, 0.06f), new Color(0.95f, 0.85f, 0.55f), false, 0.4f);
            return t;
        }

        // Begehbare Halle mit Türöffnung in der Vorderwand (lokale +z).
        public static Transform Hall(Transform parent, Vector3 pos, float yaw, float w, float d, float h, Color wall, Color roof, Color floor, float door)
        {
            var t = Group("Halle", parent, pos, yaw);
            Box(t, new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.1f, d), floor);
            Box(t, new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.4f), wall);
            Box(t, new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wall);
            Box(t, new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), wall);
            float seg = (w - door) * 0.5f;
            Box(t, new Vector3(-(door * 0.5f + seg * 0.5f), h * 0.5f, d * 0.5f), new Vector3(seg, h, 0.4f), wall);
            Box(t, new Vector3(door * 0.5f + seg * 0.5f, h * 0.5f, d * 0.5f), new Vector3(seg, h, 0.4f), wall);
            float doorH = Mathf.Min(3.2f, h - 0.5f);
            Box(t, new Vector3(0f, doorH + (h - doorH) * 0.5f, d * 0.5f), new Vector3(door, h - doorH, 0.4f), wall);
            Roof(t, new Vector3(0f, h, 0f), w + 0.8f, d + 0.4f, Mathf.Min(4f, w * 0.25f), roof);

            var lamp = new GameObject("Licht");
            lamp.transform.SetParent(t, false);
            lamp.transform.localPosition = new Vector3(0f, h - 0.8f, 0f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Mathf.Max(w, d) * 0.9f;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.85f, 0.6f);
            return t;
        }

        // Art. 20: Reichsbanner (mitternachtsazur, goldener Stern der Genesis) bzw. eigene Farbe einer Autonomie.
        public static void Banner(Transform parent, Vector3 worldPos, float yaw, float height, Color? tint = null)
        {
            var t = Group("Banner", parent, worldPos, yaw);
            Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.12f, height * 0.5f, 0.12f), new Color(0.3f, 0.25f, 0.2f));
            Material mat;
            if (tint.HasValue)
            {
                mat = MaterialFactory.Shared(tint.Value, 0.2f);
            }
            else
            {
                mat = MaterialFactory.Textured(ProcTex.Banner);
            }
            for (int side = 0; side < 2; side++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(t, false);
                q.transform.localPosition = new Vector3(0.75f, height - 1.6f, side == 0 ? 0.03f : -0.03f);
                q.transform.localRotation = Quaternion.Euler(0f, side == 0 ? 180f : 0f, 0f);
                q.transform.localScale = new Vector3(1.4f, 2.8f, 1f);
                q.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        public static GameObject Tree(Transform parent, Vector3 pos, float scale, int variant, bool dead)
        {
            var root = new GameObject(dead ? "Toter Baum" : "Baum");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, variant * 47f, 0f);
            Color bark = dead ? new Color(0.22f, 0.18f, 0.2f) : new Color(0.38f, 0.26f, 0.16f);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.6f * scale, 0f), new Vector3(0.38f * scale, 1.6f * scale, 0.38f * scale), bark);
            if (dead)
            {
                var b = Box(root.transform, new Vector3(0.4f * scale, 2.6f * scale, 0f), new Vector3(1.2f * scale, 0.15f * scale, 0.15f * scale), bark, false);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
                return root;
            }
            Color leaf = variant % 3 == 0 ? new Color(0.2f, 0.42f, 0.18f) : variant % 3 == 1 ? new Color(0.26f, 0.48f, 0.2f) : new Color(0.17f, 0.36f, 0.2f);
            if (variant % 2 == 0)
            {
                Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 3.9f * scale, 0f), new Vector3(2.8f, 2.5f, 2.8f) * scale, leaf, false);
                Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.6f * scale, 4.8f * scale, 0.3f * scale), new Vector3(1.8f, 1.6f, 1.8f) * scale, leaf * 1.1f, false);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    float s = (2.6f - i * 0.7f) * scale;
                    Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, (2.8f + i * 1.3f) * scale, 0f), new Vector3(s, 0.6f * scale, s), leaf * (1f + i * 0.08f), false);
                }
            }
            return root;
        }
    }
}
