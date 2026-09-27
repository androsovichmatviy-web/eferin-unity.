using UnityEngine;

namespace Eferin
{
    public static class MaterialFactory
    {
        static Shader litShader;

        static Shader LitShader
        {
            get
            {
                if (litShader == null)
                    litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                return litShader;
            }
        }

        public static Material Lit(Color color, float smoothness = 0.15f)
        {
            var mat = new Material(LitShader);
            SetColor(mat, color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }

        public static Material Glowing(Color color, float intensity = 2f)
        {
            var mat = Lit(color, 0.5f);
            SetEmission(mat, color * intensity);
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

        public static Material Line()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(shader);
        }
    }
}
