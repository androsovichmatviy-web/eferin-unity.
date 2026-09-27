using UnityEngine;

namespace Eferin
{
    // Shared IMGUI helpers. All UI is laid out on a virtual 1080p-tall canvas and scaled to the screen.
    public static class GuiKit
    {
        public const float RefHeight = 1080f;
        public static readonly Color Gold = new Color(0.91f, 0.78f, 0.49f);
        public static readonly Color Panel = new Color(0.05f, 0.06f, 0.08f, 0.72f);

        static GUIStyle label;

        public static float Scale => Screen.height / RefHeight;
        public static float Width => Screen.width / Scale;

        public static void BeginScaled()
        {
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        }

        public static bool WorldToGui(Camera cam, Vector3 world, out Vector2 gui)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            gui = new Vector2(sp.x / Scale, (Screen.height - sp.y) / Scale);
            return sp.z > 0f;
        }

        public static void Rect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Bar(Rect r, float fill, Color c)
        {
            Rect(r, new Color(0f, 0f, 0f, 0.55f));
            Rect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), c);
        }

        public static void Text(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            if (label == null) label = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false };
            label.fontSize = size;
            label.alignment = anchor;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

            var shadow = new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height);
            label.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.85f);
            GUI.Label(shadow, text, label);
            label.normal.textColor = color;
            GUI.Label(r, text, label);
        }
    }
}
