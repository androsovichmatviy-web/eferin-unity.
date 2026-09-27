using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Eferin
{
    public enum GameKey
    {
        Skill,
        Burst,
        Jump,
        Sprint,
        Codex,
        Advance,
        ShowCursor,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
    }

    // Works with both the legacy Input Manager and the Input System package
    // (Unity 6 templates enable only the new one, 2022 LTS templates only the old one).
    public static class GameInput
    {
        public static Vector2 Move()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        public static Vector2 LookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue() * 0.1f;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        public static float Scroll()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m == null ? 0f : Mathf.Clamp(m.scroll.ReadValue().y, -1f, 1f);
#else
            return Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f);
#endif
        }

        public static bool AttackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool Pressed(GameKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case GameKey.Skill: return k.eKey.wasPressedThisFrame;
                case GameKey.Burst: return k.qKey.wasPressedThisFrame;
                case GameKey.Jump: return k.spaceKey.wasPressedThisFrame;
                case GameKey.Codex: return k.cKey.wasPressedThisFrame;
                case GameKey.Advance: return k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || AttackPressed();
                case GameKey.Slot1: return k.digit1Key.wasPressedThisFrame;
                case GameKey.Slot2: return k.digit2Key.wasPressedThisFrame;
                case GameKey.Slot3: return k.digit3Key.wasPressedThisFrame;
                case GameKey.Slot4: return k.digit4Key.wasPressedThisFrame;
                case GameKey.Slot5: return k.digit5Key.wasPressedThisFrame;
                default: return false;
            }
#else
            switch (key)
            {
                case GameKey.Skill: return Input.GetKeyDown(KeyCode.E);
                case GameKey.Burst: return Input.GetKeyDown(KeyCode.Q);
                case GameKey.Jump: return Input.GetKeyDown(KeyCode.Space);
                case GameKey.Codex: return Input.GetKeyDown(KeyCode.C);
                case GameKey.Advance: return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || AttackPressed();
                case GameKey.Slot1: return Input.GetKeyDown(KeyCode.Alpha1);
                case GameKey.Slot2: return Input.GetKeyDown(KeyCode.Alpha2);
                case GameKey.Slot3: return Input.GetKeyDown(KeyCode.Alpha3);
                case GameKey.Slot4: return Input.GetKeyDown(KeyCode.Alpha4);
                case GameKey.Slot5: return Input.GetKeyDown(KeyCode.Alpha5);
                default: return false;
            }
#endif
        }

        public static bool Held(GameKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case GameKey.Sprint: return k.leftShiftKey.isPressed;
                case GameKey.ShowCursor: return k.leftAltKey.isPressed;
                default: return false;
            }
#else
            switch (key)
            {
                case GameKey.Sprint: return Input.GetKey(KeyCode.LeftShift);
                case GameKey.ShowCursor: return Input.GetKey(KeyCode.LeftAlt);
                default: return false;
            }
#endif
        }
    }
}
