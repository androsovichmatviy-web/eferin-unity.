using UnityEngine;

namespace Eferin
{
    public static class GameState
    {
        public static bool DialogueOpen;
        public static bool CodexOpen;

        // The click/key that closes an overlay must not also fire an attack or jump on the same frame.
        static int resumeFrame = -1;

        public static bool GameplayBlocked => DialogueOpen || CodexOpen || Time.frameCount <= resumeFrame;

        public static void MarkOverlayClosed()
        {
            resumeFrame = Time.frameCount;
        }
    }
}
