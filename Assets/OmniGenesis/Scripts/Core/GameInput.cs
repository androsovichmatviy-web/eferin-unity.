using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OmniGenesis
{
    public enum Act
    {
        Jump, Sprint, Interact,
        Skill1, Skill2, Skill3,
        Potion1, Potion2, Potion3,
        Character, Inventory, Map, Edict, Pillars, Quests,
        CameraToggle, Surrender, Pause, ShowCursor, QuickSave,
        Demo1, Demo2, Demo3, Demo4, Demo5,
    }

    // Funktioniert mit dem alten Input Manager und mit dem Input System Package
    // (Unity 6 aktiviert standardmäßig nur das neue).
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static Key KeyOf(Act a)
        {
            switch (a)
            {
                case Act.Jump: return Key.Space;
                case Act.Sprint: return Key.LeftShift;
                case Act.Interact: return Key.F;
                case Act.Skill1: return Key.Q;
                case Act.Skill2: return Key.E;
                case Act.Skill3: return Key.R;
                case Act.Potion1: return Key.Digit1;
                case Act.Potion2: return Key.Digit2;
                case Act.Potion3: return Key.Digit3;
                case Act.Character: return Key.C;
                case Act.Inventory: return Key.I;
                case Act.Map: return Key.M;
                case Act.Edict: return Key.J;
                case Act.Pillars: return Key.P;
                case Act.Quests: return Key.L;
                case Act.CameraToggle: return Key.V;
                case Act.Surrender: return Key.G;
                case Act.Pause: return Key.Escape;
                case Act.ShowCursor: return Key.LeftAlt;
                case Act.QuickSave: return Key.F5;
                case Act.Demo1: return Key.F1;
                case Act.Demo2: return Key.F2;
                case Act.Demo3: return Key.F3;
                case Act.Demo4: return Key.F4;
                case Act.Demo5: return Key.F6;
                default: return Key.None;
            }
        }

        public static bool Pressed(Act a)
        {
            var k = Keyboard.current;
            return k != null && k[KeyOf(a)].wasPressedThisFrame;
        }

        public static bool Held(Act a)
        {
            var k = Keyboard.current;
            return k != null && k[KeyOf(a)].isPressed;
        }

        public static Vector2 Move()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k[Key.D].isPressed ? 1f : 0f) - (k[Key.A].isPressed ? 1f : 0f);
            float y = (k[Key.W].isPressed ? 1f : 0f) - (k[Key.S].isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static Vector2 LookDelta()
        {
            var m = Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue() * 0.1f;
        }

        public static float Scroll()
        {
            var m = Mouse.current;
            return m == null ? 0f : Mathf.Clamp(m.scroll.ReadValue().y, -1f, 1f);
        }

        public static bool AttackHeld()
        {
            var m = Mouse.current;
            return m != null && m.leftButton.isPressed;
        }

        public static bool AttackPressed()
        {
            var m = Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
        }
#else
        static KeyCode CodeOf(Act a)
        {
            switch (a)
            {
                case Act.Jump: return KeyCode.Space;
                case Act.Sprint: return KeyCode.LeftShift;
                case Act.Interact: return KeyCode.F;
                case Act.Skill1: return KeyCode.Q;
                case Act.Skill2: return KeyCode.E;
                case Act.Skill3: return KeyCode.R;
                case Act.Potion1: return KeyCode.Alpha1;
                case Act.Potion2: return KeyCode.Alpha2;
                case Act.Potion3: return KeyCode.Alpha3;
                case Act.Character: return KeyCode.C;
                case Act.Inventory: return KeyCode.I;
                case Act.Map: return KeyCode.M;
                case Act.Edict: return KeyCode.J;
                case Act.Pillars: return KeyCode.P;
                case Act.Quests: return KeyCode.L;
                case Act.CameraToggle: return KeyCode.V;
                case Act.Surrender: return KeyCode.G;
                case Act.Pause: return KeyCode.Escape;
                case Act.ShowCursor: return KeyCode.LeftAlt;
                case Act.QuickSave: return KeyCode.F5;
                case Act.Demo1: return KeyCode.F1;
                case Act.Demo2: return KeyCode.F2;
                case Act.Demo3: return KeyCode.F3;
                case Act.Demo4: return KeyCode.F4;
                case Act.Demo5: return KeyCode.F6;
                default: return KeyCode.None;
            }
        }

        public static bool Pressed(Act a) { return Input.GetKeyDown(CodeOf(a)); }
        public static bool Held(Act a) { return Input.GetKey(CodeOf(a)); }
        public static Vector2 Move() { return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); }
        public static Vector2 LookDelta() { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
        public static float Scroll() { return Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f); }
        public static bool AttackHeld() { return Input.GetMouseButton(0); }
        public static bool AttackPressed() { return Input.GetMouseButtonDown(0); }
#endif
    }
}
