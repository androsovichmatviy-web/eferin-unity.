using UnityEngine;

namespace Eferin
{
    // Prologue from «Дар Вечности: Пролог Мира», shown with cinematic letterboxing and a typewriter reveal.
    public class DialogueSystem : MonoBehaviour
    {
        public float charsPerSecond = 45f;

        static readonly string[,] Prologue =
        {
            { "Рассказчик", "Где-то в пустоте между мирами гаснет свеча одной короткой жизни..." },
            { "Создатель", "«Твой счёт с болью закрыт. Я не властен вернуть тебе прежнюю жизнь, но могу дать то, чего не получал ни один смертный — выбор, каким будет твоё продолжение»." },
            { "Создатель", "«Условие одно: ты не будешь помнить это имя. Но твоя природа — твоё любопытство, твоя доброта — останется с тобой»." },
            { "???", "«Я хочу мир, где ничего не решено заранее. Хочу выбирать сам — каждый день, который у меня теперь есть»." },
            { "Создатель", "«Тогда иди. Эферин — континент семи ветров — ждёт тебя. Одна частица силы останется с тобой: та, что откликается на само право двигаться дальше»." },
            { "Рассказчик", "Герой открывает глаза на каменистом берегу Мерейны. Ни имени, ни памяти о доме — только тёплая Искра Отклика на груди." },
            { "Эйла", "«Очнулся? Хорошо. Не важно, откуда ты родом. Важно, куда ты решишь идти»." },
            { "Эйла", "«Я Эйла, Странница Ордена Семи Дорог. Раз уж Искра выбрала тебя — идём, покажу, на что ты способен»." },
        };

        int index;
        float revealed;

        void Start()
        {
            GameState.DialogueOpen = true;
        }

        void Update()
        {
            if (!GameState.DialogueOpen) return;

            string text = Prologue[index, 1];
            revealed = Mathf.Min(text.Length, revealed + charsPerSecond * Time.deltaTime);

            if (!GameInput.Pressed(GameKey.Advance)) return;
            if (revealed < text.Length)
            {
                revealed = text.Length;
                return;
            }

            index++;
            revealed = 0f;
            if (index >= Prologue.GetLength(0)) Close();
        }

        void Close()
        {
            GameState.DialogueOpen = false;
            GameState.MarkOverlayClosed();
            GameHud.Instance?.ShowBanner("Эйла присоединилась к отряду", GuiKit.Gold);
            enabled = false;
        }

        void OnGUI()
        {
            if (!GameState.DialogueOpen) return;
            GuiKit.BeginScaled();
            float w = GuiKit.Width, h = GuiKit.RefHeight;

            GuiKit.Rect(new Rect(0f, 0f, w, 110f), Color.black);
            GuiKit.Rect(new Rect(0f, h - 110f, w, 110f), Color.black);

            float boxW = Mathf.Min(1100f, w - 80f);
            var box = new Rect((w - boxW) / 2f, h - 330f, boxW, 190f);
            GuiKit.Rect(box, new Color(0.04f, 0.05f, 0.07f, 0.88f));
            GuiKit.Rect(new Rect(box.x, box.y, box.width, 2f), GuiKit.Gold);

            string text = Prologue[index, 1];
            string shown = text.Substring(0, Mathf.FloorToInt(revealed));
            GuiKit.Text(new Rect(box.x + 36f, box.y + 16f, boxW - 72f, 34f), Prologue[index, 0], 24, GuiKit.Gold, TextAnchor.MiddleLeft, true);
            GuiKit.Text(new Rect(box.x + 36f, box.y + 58f, boxW - 72f, 100f), shown, 22, Color.white, TextAnchor.UpperLeft);

            if (revealed >= text.Length)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
                GuiKit.Text(new Rect(box.x, box.yMax - 32f, boxW - 30f, 24f), "▼  ЛКМ / Пробел", 15, new Color(0.91f, 0.78f, 0.49f, blink), TextAnchor.MiddleRight);
            }
        }
    }
}
