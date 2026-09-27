using UnityEngine;

namespace Eferin
{
    public class Codex : MonoBehaviour
    {
        static readonly string[,] Entries =
        {
            { "Эферин", "Континент семи ветров, разделённый на земли: Пламя, Поток, Гром, Иней, Ветер, Камень и Свет. Каждой землёй правит свой Хранитель — древнее существо, чья сила пропитывает климат и характер жителей." },
            { "Искра Отклика", "Кристалл, откликающийся на душу своего носителя. Даёт власть над одной из семи стихий. Редко передаётся по рождению — чаще находит того, кто пережил нечто, изменившее его." },
            { "Орден Семи Дорог", "Вольное содружество странников, не подчиняющееся Хранителям земель. Девиз — «Дорога длиннее любой стены». Новичков зовут Ищущими, опытных — Странниками, легенд — просто Именами." },
            { "Забытые", "Древний культ, поклоняющийся Пустоте, из которой родился Создатель. Верят, что свобода воли — болезнь мира, и вырезают Искры Отклика из носителей. Во главе — Безымянный Судья." },
            { "Эйла из Кайрена", "Странница Ордена, резкая и ироничная. Владеет силой Грома. Первой находит героя на берегу Мерейны." },
            { "Мал из Ирвары", "Кузнец-полудракон, добродушный великан. Ищет сбежавшего младшего брата, которого видели среди Забытых." },
            { "Сая из Аврелии", "Изгнанная учёная, исследовавшая природу Искр Отклика. Подозревает, что в герое есть нечто необъяснимое." },
            { "Тиль из Мерейны", "Беспризорник, промышлявший мелким воровством в портовых городах. Прибился к отряду почти случайно." },
            { "Стихийные реакции", "Пламя + Поток — Испарение. Пламя + Иней — Плавление. Пламя + Гром — Перегрузка. Поток + Гром — Наэлектризация. Поток + Иней — Заморозка. Гром + Иней — Сверхпроводимость. Ветер разносит чужую стихию — Развеивание." },
        };

        Vector2 scroll;

        void Awake()
        {
            GameState.CodexOpen = false;
        }

        void Update()
        {
            if (GameState.DialogueOpen) return;
            if (GameInput.Pressed(GameKey.Codex)) SetOpen(!GameState.CodexOpen);
        }

        void SetOpen(bool open)
        {
            GameState.CodexOpen = open;
            if (!open) GameState.MarkOverlayClosed();
        }

        void OnGUI()
        {
            if (!GameState.CodexOpen) return;
            GuiKit.BeginScaled();
            float w = GuiKit.Width, h = GuiKit.RefHeight;

            GuiKit.Rect(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, 0.6f));
            float boxW = Mathf.Min(900f, w - 80f);
            var box = new Rect((w - boxW) / 2f, 90f, boxW, h - 180f);
            GuiKit.Rect(box, new Color(0.06f, 0.07f, 0.09f, 0.97f));
            GuiKit.Rect(new Rect(box.x, box.y, box.width, 2f), GuiKit.Gold);
            GuiKit.Text(new Rect(box.x + 32f, box.y + 16f, boxW - 64f, 44f), "Архив странника", 32, GuiKit.Gold, TextAnchor.MiddleLeft, true);

            const float entryH = 130f;
            var view = new Rect(box.x + 24f, box.y + 80f, boxW - 48f, box.height - 150f);
            var content = new Rect(0f, 0f, view.width - 24f, Entries.GetLength(0) * entryH);
            scroll = GUI.BeginScrollView(view, scroll, content);
            for (int i = 0; i < Entries.GetLength(0); i++)
            {
                float y = i * entryH;
                GuiKit.Text(new Rect(8f, y, content.width - 16f, 30f), Entries[i, 0], 22, GuiKit.Gold, TextAnchor.MiddleLeft, true);
                GuiKit.Text(new Rect(8f, y + 32f, content.width - 16f, entryH - 40f), Entries[i, 1], 18, new Color(0.86f, 0.83f, 0.76f), TextAnchor.UpperLeft);
            }
            GUI.EndScrollView();

            if (GUI.Button(new Rect(box.xMax - 200f, box.yMax - 56f, 170f, 38f), "Закрыть (C)")) SetOpen(false);
        }
    }
}
