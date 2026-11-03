using UnityEngine;

namespace OmniGenesis
{
    // IMGUI-Bausatz im Stil halbtransparenter VR-Systemfenster. Virtuelle Leinwand: 1080 px Höhe.
    public static class Ui
    {
        public const float RefHeight = 1080f;
        public static float Scale { get { return Screen.height / RefHeight; } }
        public static float Width { get { return Screen.width / Scale; } }
        public static float Height { get { return RefHeight; } }

        public static readonly Color PanelBg = new Color(0.02f, 0.06f, 0.12f, 0.84f);
        public static readonly Color PanelLight = new Color(0.06f, 0.14f, 0.24f, 0.9f);
        public static readonly Color Border = new Color(0.35f, 0.85f, 1f, 0.9f);
        public static readonly Color Gold = new Color(0.95f, 0.8f, 0.42f);
        public static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        public static readonly Color Dim = new Color(0.65f, 0.75f, 0.85f, 0.85f);
        public static readonly Color Good = new Color(0.45f, 0.92f, 0.55f);
        public static readonly Color Bad = new Color(1f, 0.45f, 0.4f);

        static GUIStyle label, button, field;
        static Texture2D texNormal, texHover, texActive;

        public static void BeginScaled()
        {
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        static void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false, clipping = TextClipping.Clip };
            texNormal = Solid(new Color(0.08f, 0.22f, 0.36f, 0.95f));
            texHover = Solid(new Color(0.14f, 0.36f, 0.55f, 0.98f));
            texActive = Solid(new Color(0.25f, 0.55f, 0.75f, 1f));
            button = new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleCenter };
            button.normal.background = texNormal;
            button.hover.background = texHover;
            button.active.background = texActive;
            button.focused.background = texNormal;
            button.normal.textColor = button.hover.textColor = button.active.textColor = button.focused.textColor = TextColor;
            field = new GUIStyle(GUI.skin.textField) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
            field.normal.background = Solid(new Color(0.02f, 0.05f, 0.09f, 0.95f));
            field.focused.background = Solid(new Color(0.04f, 0.1f, 0.16f, 1f));
            field.normal.textColor = field.focused.textColor = field.hover.textColor = TextColor;
            field.padding = new RectOffset(10, 10, 4, 4);
        }

        public static void Rect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Frame(Rect r, Color c, float t = 2f)
        {
            Rect(new Rect(r.x, r.y, r.width, t), c);
            Rect(new Rect(r.x, r.yMax - t, r.width, t), c);
            Rect(new Rect(r.x, r.y, t, r.height), c);
            Rect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        // Systemfenster mit Titelleiste. Gibt den Innenbereich zurück.
        public static Rect Panel(Rect r, string title = null)
        {
            Rect(r, PanelBg);
            Frame(r, Border, 2f);
            Rect(new Rect(r.x + 2f, r.y + 2f, 40f, 3f), Gold);
            Rect(new Rect(r.xMax - 42f, r.yMax - 5f, 40f, 3f), Gold);
            if (string.IsNullOrEmpty(title)) return new Rect(r.x + 18f, r.y + 14f, r.width - 36f, r.height - 28f);
            Rect(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, 44f), new Color(0.05f, 0.18f, 0.3f, 0.9f));
            Text(new Rect(r.x + 18f, r.y + 4f, r.width - 36f, 40f), title, 22, Gold, TextAnchor.MiddleLeft, true);
            return new Rect(r.x + 18f, r.y + 58f, r.width - 36f, r.height - 72f);
        }

        public static void Text(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false, bool shadow = true)
        {
            EnsureStyles();
            label.fontSize = size;
            label.alignment = anchor;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            if (shadow)
            {
                label.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.8f);
                GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, label);
            }
            label.normal.textColor = color;
            GUI.Label(r, text, label);
        }

        public static float TextHeight(string text, int size, float width)
        {
            EnsureStyles();
            label.fontSize = size;
            label.fontStyle = FontStyle.Normal;
            return label.CalcHeight(new GUIContent(text), width);
        }

        public static bool Button(Rect r, string text, bool enabled = true, int size = 18)
        {
            EnsureStyles();
            button.fontSize = size;
            if (!enabled)
            {
                Rect(r, new Color(0.12f, 0.14f, 0.17f, 0.85f));
                Text(r, text, size, new Color(0.6f, 0.65f, 0.7f, 0.7f), TextAnchor.MiddleCenter, false, false);
                return false;
            }
            bool clicked = GUI.Button(r, text, button);
            Frame(r, new Color(Border.r, Border.g, Border.b, 0.45f), 1f);
            return clicked;
        }

        public static string Field(Rect r, string value, int max)
        {
            EnsureStyles();
            return GUI.TextField(r, value ?? "", max, field);
        }

        public static void Bar(Rect r, float fill, Color c)
        {
            Rect(r, new Color(0f, 0f, 0f, 0.55f));
            Rect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), c);
            Frame(r, new Color(1f, 1f, 1f, 0.15f), 1f);
        }

        public static bool WorldToGui(Camera cam, Vector3 world, out Vector2 gui)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            gui = new Vector2(sp.x / Scale, (Screen.height - sp.y) / Scale);
            return sp.z > 0f;
        }

        public static Rect Centered(float w, float h)
        {
            w = Mathf.Min(w, Width - 40f);
            h = Mathf.Min(h, Height - 40f);
            return new Rect((Width - w) * 0.5f, (Height - h) * 0.5f, w, h);
        }
    }

    // Einfache vertikale Anordnung.
    public class Col
    {
        public float x, y, w;

        public Col(Rect r) { x = r.x; y = r.y; w = r.width; }

        public Rect Next(float h, float gap = 6f)
        {
            var r = new Rect(x, y, w, h);
            y += h + gap;
            return r;
        }

        public Rect Text(string text, int size, Color c, bool bold = false)
        {
            float h = Ui.TextHeight(text, size, w) + 2f;
            var r = Next(h, 4f);
            Ui.Text(r, text, size, c, TextAnchor.UpperLeft, bold);
            return r;
        }

        public void Space(float h) { y += h; }
    }
}
