using UnityEngine;

namespace OmniGenesis
{
    // Hauptmenü und Charaktererstellung (Name, Rasse, Klasse).
    public class MainMenu : MonoBehaviour
    {
        enum Screen { Start, Create, Overwrite, Edict }

        Screen screen = Screen.Start;
        string charName = "";
        int race;
        int cls;
        bool demo;
        bool hasSave;
        Vector2 scroll;
        Camera cam;

        void Start()
        {
            Time.timeScale = 1f;
            World.Reset();
            hasSave = SaveSystem.Exists();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.03f, 0.07f);
            BuildBackdrop();
        }

        // Ein kleiner Turm mit leuchtenden Ringen als Hintergrund.
        void BuildBackdrop()
        {
            var root = new GameObject("Kulisse").transform;
            Build.Prim(PrimitiveType.Cylinder, root, new Vector3(6f, 0f, 30f), new Vector3(3f, 30f, 3f), new Color(0.6f, 0.62f, 0.7f));
            for (int i = 0; i < 6; i++)
                Build.Prim(PrimitiveType.Cylinder, root, new Vector3(6f, -12f + i * 5f, 30f), new Vector3(3.3f, 0.15f, 3.3f), new Color(0.4f, 0.8f, 1f), false, 3f);
            Build.Prim(PrimitiveType.Sphere, root, new Vector3(6f, 31f, 30f), Vector3.one * 2.5f, new Color(0.5f, 0.85f, 1f), false, 4f);
            var light = new GameObject("Licht").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(30f, -40f, 0f);
            light.intensity = 0.6f;
            cam.transform.position = new Vector3(0f, 6f, 0f);
            cam.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
        }

        void Update()
        {
            if (cam != null) cam.transform.rotation = Quaternion.Euler(-12f + Mathf.Sin(Time.time * 0.2f) * 2f, Mathf.Sin(Time.time * 0.1f) * 4f, 0f);
        }

        void OnGUI()
        {
            if (LoadingScreen.Instance != null) return;
            Ui.BeginScaled();
            float w = Ui.Width, h = Ui.Height;
            Ui.Text(new Rect(80f, 70f, w - 160f, 100f), GameConstants.Title.ToUpper(), 84, Ui.Gold, TextAnchor.MiddleLeft, true);
            Ui.Text(new Rect(84f, 170f, w - 160f, 40f), GameConstants.Motto, 24, Ui.TextColor, TextAnchor.MiddleLeft);
            Ui.Text(new Rect(84f, 210f, w - 160f, 30f), "Eine lebendige Simulationswelt nach dem Kaiserlichen Edikt", 18, Ui.Dim, TextAnchor.MiddleLeft);

            switch (screen)
            {
                case Screen.Start: DrawStart(); break;
                case Screen.Create: DrawCreate(w, h); break;
                case Screen.Overwrite: DrawOverwrite(); break;
                case Screen.Edict: DrawEdict(w, h); break;
            }
        }

        void DrawStart()
        {
            var col = new Col(new Rect(84f, 300f, 420f, 500f));
            if (hasSave && Ui.Button(col.Next(60f, 12f), "Fortsetzen", true, 24))
            {
                Session.Current = SaveSystem.Load();
                if (Session.Current != null) LoadingScreen.LoadWorld();
                else hasSave = false;
            }
            if (Ui.Button(col.Next(60f, 12f), "Neues Spiel", true, 24)) screen = Screen.Create;
            if (Ui.Button(col.Next(60f, 12f), "Das Edikt lesen", true, 24)) screen = Screen.Edict;
            if (Ui.Button(col.Next(60f, 12f), "Beenden", true, 24)) Application.Quit();
        }

        void DrawCreate(float w, float h)
        {
            var r = new Rect(84f, 270f, Mathf.Min(1400f, w - 168f), h - 320f);
            var area = Ui.Panel(r, "CHARAKTERERSTELLUNG");
            var left = new Col(new Rect(area.x, area.y, 420f, area.height));
            left.Text("Name", 18, Ui.Gold, true);
            charName = Ui.Field(left.Next(46f, 14f), charName, 24);

            left.Text("Rasse (Art. 11)", 18, Ui.Gold, true);
            for (int i = 0; i < Races.All.Count; i++)
                if (Ui.Button(left.Next(44f, 6f), (i == race ? "► " : "") + Races.All[i].name, true, 19)) race = i;
            var rd = Races.All[race];
            left.Text(rd.description, 15, Ui.Dim);
            left.Text("Bonus: Stärke +" + rd.str + ", Beweglichkeit +" + rd.agi + ", Vitalität +" + rd.vit + ", Intelligenz +" + rd.intl, 14, Ui.TextColor);
            left.Space(10f);
            if (Ui.Button(left.Next(40f, 6f), (demo ? "[x] " : "[  ] ") + "Demo-Modus für die Präsentation", true, 16)) demo = !demo;
            left.Text("Im Demo-Modus helfen F1–F6 (Stufen, Taler, Zeit, Respekt, Siegel), um alle Systeme schnell vorzuführen.", 13, Ui.Dim);

            var mid = new Col(new Rect(area.x + 460f, area.y, 380f, area.height));
            mid.Text("Klasse (Art. 48)", 18, Ui.Gold, true);
            for (int i = 0; i < Classes.All.Count; i++)
                if (Ui.Button(mid.Next(44f, 6f), (i == cls ? "► " : "") + Classes.All[i].name, true, 19)) cls = i;
            mid.Text("Alle Klassen haben dieselbe Struktur und dasselbe Budget: 24 Startpunkte, 4 Punkte Wachstum je Stufe.", 13, Ui.Dim);

            var right = new Col(new Rect(area.x + 880f, area.y, area.width - 880f, area.height));
            var c = Classes.All[cls];
            right.Text(c.name, 24, Ui.Gold, true);
            right.Text(c.description, 16, Ui.TextColor);
            right.Text("Start: Stärke " + (c.str + rd.str) + " · Beweglichkeit " + (c.agi + rd.agi) + " · Vitalität " + (c.vit + rd.vit) + " · Intelligenz " + (c.intl + rd.intl), 15, Ui.TextColor);
            right.Text("Wachstum je Stufe: STR +" + c.gStr + ", BEW +" + c.gAgi + ", VIT +" + c.gVit + ", INT +" + c.gInt, 14, Ui.Dim);
            right.Text("Waffe: " + Items.Name(c.startWeapon) + " (" + Items.Get(c.startWeapon).mass + " kg) · Rüstung: " + Items.Name(c.startArmor), 14, Ui.Dim);
            right.Space(6f);
            right.Text("[Q] " + c.skillQ.name + ": " + c.skillQ.description, 14, Ui.TextColor);
            right.Text("[E] " + c.skillE.name + ": " + c.skillE.description, 14, Ui.TextColor);
            right.Space(6f);
            right.Text("Ab Stufe 50 ergibt eine zweite Klasse eine Hybridklasse:", 14, Ui.Dim);
            foreach (var other in Classes.All)
            {
                if (other.id == c.id) continue;
                var hy = Classes.Hybrid(c.id, other.id);
                if (hy != null) right.Text("+ " + other.name + " = " + hy.name, 13, Ui.Dim);
            }

            bool ok = charName.Trim().Length >= 2;
            if (Ui.Button(new Rect(r.xMax - 340f, r.yMax - 70f, 320f, 54f), "Welt betreten", ok, 22))
            {
                if (hasSave) screen = Screen.Overwrite;
                else StartNew();
            }
            if (Ui.Button(new Rect(r.xMax - 520f, r.yMax - 70f, 160f, 54f), "Zurück", true, 20)) screen = Screen.Start;
        }

        void DrawOverwrite()
        {
            var r = Ui.Centered(620f, 260f);
            var area = Ui.Panel(r, "SPIELSTAND ÜBERSCHREIBEN?");
            Ui.Text(new Rect(area.x, area.y, area.width, 80f), "Es gibt bereits einen Spielstand. Ein neuer Charakter ersetzt ihn vollständig.", 18, Ui.TextColor);
            if (Ui.Button(new Rect(area.x, area.yMax - 54f, 260f, 50f), "Ja, neu beginnen", true, 19)) StartNew();
            if (Ui.Button(new Rect(area.xMax - 260f, area.yMax - 54f, 260f, 50f), "Abbrechen", true, 19)) screen = Screen.Create;
        }

        void DrawEdict(float w, float h)
        {
            var r = new Rect(84f, 270f, Mathf.Min(1300f, w - 168f), h - 320f);
            var area = Ui.Panel(r, "KAISERLICHES EDIKT — AUSZÜGE");
            float tw = area.width - 24f;
            float th = Ui.TextHeight(EdictTexts.Preamble, 18, tw) + 20f;
            foreach (var a in EdictTexts.Articles) th += Ui.TextHeight(a.text, 15, tw) + 34f;
            var view = new Rect(area.x, area.y, area.width, area.height - 60f);
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0f, 0f, tw, th));
            var col = new Col(new Rect(0f, 0f, tw, th));
            col.Text(EdictTexts.Preamble, 18, Ui.Gold);
            col.Space(10f);
            foreach (var a in EdictTexts.Articles)
            {
                col.Text(a.number + " — " + a.title, 16, Ui.Gold, true);
                col.Text(a.text, 15, Ui.TextColor);
                col.Space(6f);
            }
            GUI.EndScrollView();
            if (Ui.Button(new Rect(area.xMax - 200f, area.yMax - 50f, 200f, 46f), "Zurück", true, 19)) screen = Screen.Start;
        }

        void StartNew()
        {
            var c = Classes.All[cls];
            var d = new SaveData
            {
                name = charName.Trim(),
                race = Races.All[race].id,
                classId = c.id,
                weapon = c.startWeapon,
                armor = c.startArmor,
                demoMode = demo,
            };
            d.inventory.Add(new ItemStack("heiltrank", 3));
            d.inventory.Add(new ItemStack("gebratenes_fleisch", 4));
            if (c.id == "kampfmagier") d.inventory.Add(new ItemStack("aetherelixier", 2));
            SaveSystem.Delete();
            Session.Current = d;
            LoadingScreen.LoadWorld();
        }
    }
}
