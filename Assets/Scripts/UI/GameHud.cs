using System.Collections.Generic;
using UnityEngine;

namespace Eferin
{
    public class GameHud : MonoBehaviour
    {
        public static GameHud Instance { get; private set; }

        public float enemyTagRange = 30f;

        struct Floater
        {
            public Vector3 world;
            public string text;
            public Color color;
            public float age;
            public bool big;
        }

        const float FloaterLife = 1.1f;
        const float BannerLife = 1.8f;

        readonly List<Floater> floaters = new List<Floater>();
        string bannerText;
        Color bannerColor;
        float bannerAge = BannerLife;
        Enemy target;
        float targetShownFor;

        PartyManager party;
        PlayerController player;
        Camera cam;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            party = PartyManager.Instance;
            if (party != null) player = party.GetComponent<PlayerController>();
            cam = Camera.main;
        }

        void Update()
        {
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                f.age += Time.deltaTime;
                if (f.age >= FloaterLife) floaters.RemoveAt(i);
                else floaters[i] = f;
            }
            bannerAge += Time.deltaTime;
            targetShownFor -= Time.deltaTime;
        }

        public void ShowWorldText(Vector3 world, string text, Color color, bool big = false)
        {
            world += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            floaters.Add(new Floater { world = world, text = text, color = color, big = big });
        }

        public void ShowBanner(string text, Color color)
        {
            bannerText = text;
            bannerColor = color;
            bannerAge = 0f;
        }

        public void SetTarget(Enemy enemy)
        {
            target = enemy;
            targetShownFor = 5f;
        }

        void OnGUI()
        {
            if (party == null || cam == null) return;
            GuiKit.BeginScaled();
            float w = GuiKit.Width, h = GuiKit.RefHeight;

            DrawEnemyTags();
            DrawFloaters();
            if (GameState.DialogueOpen) return;

            DrawPartyList(w);
            DrawActiveStatus(w, h);
            DrawAbilities(w, h);
            DrawTargetBar(w);
            DrawBanner(w);

            GuiKit.Text(new Rect(20f, h - 34f, w - 40f, 24f),
                "WASD — ходьба · Shift — бег · Пробел — прыжок · ЛКМ — атака · E — навык · Q — взрыв стихии · 1–5 — отряд · C — архив · Alt — курсор",
                15, new Color(1f, 1f, 1f, 0.6f));
        }

        void DrawPartyList(float w)
        {
            float x = w - 260f, y = 300f;
            for (int i = 0; i < party.members.Count; i++)
            {
                var m = party.members[i];
                bool active = i == party.ActiveIndex;
                Color ec = Elements.ColorOf(m.element);
                var row = new Rect(x, y + i * 64f, 240f, 56f);

                GuiKit.Rect(row, active ? new Color(0.91f, 0.78f, 0.49f, 0.28f) : GuiKit.Panel);
                GuiKit.Rect(new Rect(row.x, row.y, 6f, row.height), ec);
                GuiKit.Text(new Rect(row.x + 16f, row.y + 4f, 30f, 28f), (i + 1).ToString(), 18, GuiKit.Gold, TextAnchor.MiddleLeft, true);
                GuiKit.Text(new Rect(row.x + 42f, row.y + 4f, 190f, 28f), m.displayName, 20, m.IsDown ? Color.gray : Color.white, TextAnchor.MiddleLeft, active);
                GuiKit.Bar(new Rect(row.x + 42f, row.y + 36f, 180f, 6f), m.hp / m.maxHp, new Color(0.45f, 0.85f, 0.4f));
                if (m.BurstReady)
                    GuiKit.Text(new Rect(row.x + 190f, row.y + 4f, 40f, 28f), "Q", 18, ec, TextAnchor.MiddleRight, true);
            }
        }

        void DrawActiveStatus(float w, float h)
        {
            var c = party.Active;
            float barW = 420f;
            float x = (w - barW) / 2f, y = h - 110f;

            GuiKit.Text(new Rect(x, y - 34f, barW, 28f), c.displayName + " · " + c.title, 20, GuiKit.Gold, TextAnchor.MiddleCenter, true);
            GuiKit.Bar(new Rect(x, y, barW, 12f), c.hp / c.maxHp, new Color(0.45f, 0.85f, 0.4f));
            GuiKit.Text(new Rect(x, y + 12f, barW, 22f), Mathf.CeilToInt(c.hp) + " / " + Mathf.CeilToInt(c.maxHp), 15, Color.white, TextAnchor.MiddleCenter);

            if (player != null && player.Stamina < player.staminaMax - 0.5f)
                GuiKit.Bar(new Rect(x + barW / 4f, y - 50f, barW / 2f, 6f), player.Stamina / player.staminaMax, new Color(1f, 0.86f, 0.44f));
        }

        void DrawAbilities(float w, float h)
        {
            var c = party.Active;
            Color ec = Elements.ColorOf(c.element);
            float size = 86f;

            var skillRect = new Rect(w - 250f, h - 150f, size, size);
            GuiKit.Rect(skillRect, GuiKit.Panel);
            if (c.skillCooldownLeft > 0f)
            {
                float f = c.skillCooldownLeft / c.skillCooldown;
                GuiKit.Rect(new Rect(skillRect.x, skillRect.y + size * (1f - f), size, size * f), new Color(0f, 0f, 0f, 0.5f));
                GuiKit.Text(skillRect, c.skillCooldownLeft.ToString("0.0"), 26, Color.white, TextAnchor.MiddleCenter, true);
            }
            else
            {
                GuiKit.Text(skillRect, "E", 34, ec, TextAnchor.MiddleCenter, true);
            }
            GuiKit.Text(new Rect(skillRect.x - 30f, skillRect.yMax + 2f, size + 60f, 22f), c.skillName, 14, Color.white, TextAnchor.MiddleCenter);

            var burstRect = new Rect(w - 140f, h - 170f, size + 20f, size + 20f);
            GuiKit.Rect(burstRect, GuiKit.Panel);
            float energy = c.energy / c.energyMax;
            Color fill = ec;
            fill.a = c.BurstReady ? 0.85f : 0.45f;
            GuiKit.Rect(new Rect(burstRect.x, burstRect.y + burstRect.height * (1f - energy), burstRect.width, burstRect.height * energy), fill);
            GuiKit.Text(burstRect, "Q", 38, c.BurstReady ? Color.white : new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, true);
            GuiKit.Text(new Rect(burstRect.x - 30f, burstRect.yMax + 2f, burstRect.width + 60f, 22f), c.burstName, 14, Color.white, TextAnchor.MiddleCenter);
        }

        void DrawTargetBar(float w)
        {
            if (target == null || !target.IsAlive || targetShownFor <= 0f) return;
            float barW = 460f, x = (w - barW) / 2f;
            GuiKit.Text(new Rect(x, 24f, barW, 26f), target.displayName, 18, new Color(0.95f, 0.8f, 0.8f), TextAnchor.MiddleCenter, true);
            GuiKit.Bar(new Rect(x, 54f, barW, 10f), target.Hp / target.maxHp, new Color(0.89f, 0.36f, 0.36f));
        }

        void DrawBanner(float w)
        {
            if (bannerAge >= BannerLife || string.IsNullOrEmpty(bannerText)) return;
            float t = bannerAge / BannerLife;
            Color c = bannerColor;
            c.a = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
            GuiKit.Text(new Rect(0f, 150f, w, 60f), bannerText, 40, c, TextAnchor.MiddleCenter, true);
        }

        void DrawEnemyTags()
        {
            Vector3 camPos = cam.transform.position;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.IsAlive) continue;
                Vector3 top = e.transform.position + Vector3.up * (e.radius + 0.7f);
                if ((top - camPos).sqrMagnitude > enemyTagRange * enemyTagRange) continue;
                if (!GuiKit.WorldToGui(cam, top, out Vector2 p)) continue;

                GuiKit.Text(new Rect(p.x - 90f, p.y - 34f, 180f, 20f), e.displayName, 14, new Color(1f, 0.9f, 0.9f), TextAnchor.MiddleCenter);
                GuiKit.Bar(new Rect(p.x - 45f, p.y - 12f, 90f, 7f), e.Hp / e.maxHp, new Color(0.89f, 0.36f, 0.36f));
                if (e.Aura != ElementType.None)
                    GuiKit.Rect(new Rect(p.x - 7f, p.y - 56f, 14f, 14f), Elements.ColorOf(e.Aura));
            }
        }

        void DrawFloaters()
        {
            foreach (var f in floaters)
            {
                float t = f.age / FloaterLife;
                if (!GuiKit.WorldToGui(cam, f.world + Vector3.up * t * 1.2f, out Vector2 p)) continue;
                Color c = f.color;
                c.a = 1f - t * t;
                int size = f.big ? 30 : 20;
                GuiKit.Text(new Rect(p.x - 150f, p.y - 20f, 300f, 40f), f.text, size, c, TextAnchor.MiddleCenter, true);
            }
        }
    }
}
