using System.Collections.Generic;
using UnityEngine;

namespace OmniGenesis
{
    public enum MessageKind { System, Announcement, Warning, Reward, Law }

    public struct GameMessage
    {
        public string text;
        public MessageKind kind;
        public float time;
    }

    // Systemfenster, Verkündungen der Großen Tore und das Physik-Kampfprotokoll.
    public static class Notify
    {
        public static readonly List<GameMessage> Active = new List<GameMessage>();
        public static readonly List<string> History = new List<string>();
        public static readonly List<string> Announcements = new List<string>();
        public static readonly List<string> CombatLog = new List<string>();

        public const float MessageLife = 6f;

        public static void Clear()
        {
            Active.Clear();
            History.Clear();
            Announcements.Clear();
            CombatLog.Clear();
        }

        public static void Post(string text, MessageKind kind = MessageKind.System)
        {
            Active.Add(new GameMessage { text = text, kind = kind, time = Time.unscaledTime });
            if (Active.Count > 5) Active.RemoveAt(0);
            History.Add(text);
            if (History.Count > 200) History.RemoveAt(0);
        }

        public static void System(string text) { Post("[System] " + text, MessageKind.System); }

        // Art. 54: Edikt und Ukase werden durch die Großen Tore verkündet.
        public static void Announce(string text)
        {
            string line = "Verkündung durch die Großen Tore: " + text;
            Post(line, MessageKind.Announcement);
            string stamped = (World.Clock != null ? World.Clock.Stamp() + " — " : "") + text;
            Announcements.Insert(0, stamped);
            if (Announcements.Count > 60) Announcements.RemoveAt(Announcements.Count - 1);
        }

        public static void Combat(string line)
        {
            CombatLog.Add(line);
            if (CombatLog.Count > 6) CombatLog.RemoveAt(0);
        }

        public static void Prune()
        {
            float now = Time.unscaledTime;
            for (int i = Active.Count - 1; i >= 0; i--)
                if (now - Active[i].time > MessageLife) Active.RemoveAt(i);
        }
    }
}
