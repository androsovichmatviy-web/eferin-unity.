using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OmniGenesis
{
    // Asynchroner Ladebildschirm: lädt die Welt-Szene und wartet auf die Weltgenerierung.
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        float progress;
        float shown;
        string status = "Lade Welt…";
        float fade = 1f;
        bool done;

        static readonly string[] Tips =
        {
            "Jeder Treffer folgt F = m · a: schwere Waffen und Anlauf erhöhen die Kraft.",
            "Getötete Tiere kehren nie zurück. Wer zu viel jagt, treibt den Fleischpreis in die Höhe.",
            "Um einen Teil des Reiches zu erhalten, musst du die Stufe des Kaisers erreichen.",
            "Alle Einwohner von Eichenhain müssen dich respektieren — erfülle ihre Bedürfnisse und schütze sie.",
            "Die Großen Tore verkünden das Edikt (Art. 54) und bringen dich für 25 Taler ans Ziel.",
            "Fällt das Siegel unter 15 %, ruft der Kaiser den Großen Aufruf aus (Art. 66).",
            "Mit V wechselst du zwischen Ich-Perspektive und dritter Person.",
        };

        int tip;

        public static void LoadWorld()
        {
            if (Instance != null) return;
            var go = new GameObject("Ladebildschirm");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<LoadingScreen>();
            Instance.StartCoroutine(Instance.Run(true));
        }

        // Wird von der Welt aufgerufen, wenn man die Welt-Szene direkt im Editor startet.
        public static void Overlay()
        {
            if (Instance != null) return;
            var go = new GameObject("Ladebildschirm");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<LoadingScreen>();
            Instance.progress = 0.35f;
            Instance.StartCoroutine(Instance.Run(false));
        }

        IEnumerator Run(bool loadScene)
        {
            tip = Random.Range(0, Tips.Length);
            if (loadScene)
            {
                if (!Application.CanStreamedLevelBeLoaded(GameConstants.WorldScene))
                {
                    status = "Szene „" + GameConstants.WorldScene + "“ fehlt in den Build Settings. Im Editor: Menü Omni Genesis → Projekt einrichten.";
                    yield break;
                }
                var op = SceneManager.LoadSceneAsync(GameConstants.WorldScene);
                op.allowSceneActivation = false;
                while (op.progress < 0.9f)
                {
                    progress = op.progress / 0.9f * 0.35f;
                    status = "Lade Welt…";
                    yield return null;
                }
                progress = 0.35f;
                op.allowSceneActivation = true;
                while (!op.isDone) yield return null;
            }

            while (!World.Ready)
            {
                progress = 0.35f + WorldBootstrap.Progress * 0.65f;
                status = WorldBootstrap.Status;
                yield return null;
            }
            progress = 1f;
            status = "Willkommen in Omni Genesis";
            done = true;
            yield return new WaitForSecondsRealtime(0.4f);
            while (fade > 0f)
            {
                fade -= Time.unscaledDeltaTime * 2f;
                yield return null;
            }
            Instance = null;
            Destroy(gameObject);
        }

        void Update()
        {
            shown = Mathf.MoveTowards(shown, progress, Time.unscaledDeltaTime * 1.5f);
            if (!done && Time.frameCount % 400 == 0) tip = (tip + 1) % Tips.Length;
        }

        void OnGUI()
        {
            GUI.depth = -100;
            Ui.BeginScaled();
            float w = Ui.Width, h = Ui.Height;
            Ui.Rect(new Rect(0f, 0f, w, h), new Color(0.01f, 0.02f, 0.05f, fade));
            if (fade < 0.05f) return;
            Color gold = Ui.Gold;
            gold.a = fade;
            Color text = Ui.TextColor;
            text.a = fade;

            Ui.Text(new Rect(0f, h * 0.28f, w, 90f), GameConstants.Title.ToUpper(), 72, gold, TextAnchor.MiddleCenter, true);
            Ui.Text(new Rect(0f, h * 0.28f + 90f, w, 40f), GameConstants.Motto, 22, text, TextAnchor.MiddleCenter);

            float bw = Mathf.Min(900f, w - 80f);
            var bar = new Rect((w - bw) * 0.5f, h * 0.62f, bw, 18f);
            Ui.Bar(bar, shown, new Color(0.35f, 0.8f, 1f, fade));
            Ui.Frame(new Rect(bar.x - 3f, bar.y - 3f, bar.width + 6f, bar.height + 6f), new Color(0.35f, 0.85f, 1f, 0.6f * fade), 1f);
            Ui.Text(new Rect(bar.x, bar.yMax + 10f, bw, 30f), "Lade Welt… " + Mathf.RoundToInt(shown * 100f) + " %", 22, text, TextAnchor.MiddleLeft, true);
            Ui.Text(new Rect(bar.x, bar.yMax + 10f, bw, 30f), status, 16, new Color(0.65f, 0.75f, 0.85f, fade), TextAnchor.MiddleRight);
            Ui.Text(new Rect(bar.x, bar.yMax + 60f, bw, 60f), "Hinweis: " + Tips[tip], 17, new Color(0.65f, 0.75f, 0.85f, fade), TextAnchor.UpperCenter);
        }
    }
}
