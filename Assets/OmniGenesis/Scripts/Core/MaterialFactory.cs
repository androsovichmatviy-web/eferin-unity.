using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    // Materialien für URP (mit Rückfall auf Standard), gecacht nach Farbe.
    public static class MaterialFactory
    {
        static Shader lit;
        static Shader sprite;
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

        static Shader Lit
        {
            get
            {
                if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = Shader.Find("Standard");
                return lit;
            }
        }

        static Shader Sprite
        {
            get
            {
                if (sprite == null) sprite = Shader.Find("Sprites/Default");
                if (sprite == null) sprite = Shader.Find("Universal Render Pipeline/Unlit");
                return sprite;
            }
        }

        public static void ClearCache() { cache.Clear(); }

        static int Key(Color c, float smooth, float emission)
        {
            Color32 k = c;
            return (k.r << 24) ^ (k.g << 16) ^ (k.b << 8) ^ k.a ^ Mathf.RoundToInt(smooth * 97f) * 7919 ^ Mathf.RoundToInt(emission * 31f) * 104729;
        }

        public static Material Shared(Color color, float smoothness = 0.15f, float emission = 0f)
        {
            int key = Key(color, smoothness, emission);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = emission > 0f ? Glowing(color, emission) : Create(color, smoothness);
            cache[key] = m;
            return m;
        }

        public static Material Create(Color color, float smoothness = 0.15f)
        {
            var mat = new Material(Lit);
            mat.enableInstancing = true;
            SetColor(mat, color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }

        public static Material Glowing(Color color, float intensity = 2f)
        {
            var mat = Create(color, 0.5f);
            SetEmission(mat, color * intensity);
            return mat;
        }

        public static Material Textured(Texture2D tex, float smoothness = 0.05f)
        {
            var mat = Create(Color.white, smoothness);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            return mat;
        }

        public static void SetColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }

        public static void SetEmission(Material mat, Color emission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
        }

        // Unbeleuchtet, transparent, mit Vertexfarben: für Effekte, Linien, Partikel.
        public static Material Effect(Texture2D tex = null)
        {
            var mat = new Material(Sprite);
            if (tex != null) mat.mainTexture = tex;
            return mat;
        }
    }

    public static class ProcTex
    {
        static Texture2D circle, ring, banner;

        public static Texture2D Circle
        {
            get
            {
                if (circle == null) circle = MakeRadial(64, false);
                return circle;
            }
        }

        public static Texture2D Ring
        {
            get
            {
                if (ring == null) ring = MakeRadial(128, true);
                return ring;
            }
        }

        static Texture2D MakeRadial(int size, bool hollow)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a;
                    if (hollow) a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.08f);
                    else a = Mathf.Clamp01((1f - d) * 3f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // Art. 20: Reichsbanner — mitternachtsazurblau mit goldenem achtzackigem Stern der Genesis.
        public static Texture2D Banner
        {
            get
            {
                if (banner != null) return banner;
                const int w = 64, h = 128;
                banner = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var azure = new Color(0.06f, 0.12f, 0.32f);
                var gold = new Color(0.95f, 0.78f, 0.3f);
                var px = new Color[w * h];
                Vector2 center = new Vector2(w / 2f, h * 0.58f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                        float ang = Mathf.Atan2(p.y, p.x);
                        float r = p.magnitude;
                        float star = Mathf.Lerp(7f, 22f, Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 4f)), 6f));
                        Color col = azure;
                        if (r < star) col = gold;
                        if (y < 6 || y > h - 4 || x < 3 || x > w - 4) col = gold * 0.85f;
                        px[y * w + x] = col;
                    }
                }
                banner.SetPixels(px);
                banner.Apply();
                return banner;
            }
        }
    }
}
