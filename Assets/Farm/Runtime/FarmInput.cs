using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NextGenFarm
{
    // Works with either Unity input backend; no named Input Manager axes required.
    public static class FarmInput
    {
        public static bool Suppressed { get; set; }
        private static bool Active => Application.isFocused && !Suppressed;
        public static Vector2 Look
        {
            get
            {
                if(!Active)return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                if(Mouse.current!=null&&Mouse.current.rightButton.isPressed)return Mouse.current.delta.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if(Input.GetMouseButton(1))return new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*12;
#endif
                return Vector2.zero;
            }
        }
        private static readonly Dictionary<KeyCode, int> guiPresses = new Dictionary<KeyCode, int>();
        private static readonly Dictionary<KeyCode, int> handledFrames = new Dictionary<KeyCode, int>();
        private static readonly HashSet<KeyCode> guiHeld = new HashSet<KeyCode>();
        public static void ClearGUIState() { guiPresses.Clear(); guiHeld.Clear(); }
        public static void CaptureGUIEvent(Event evt)
        {
            if(!Active)return;
            if (evt == null || (evt.type != EventType.KeyDown && evt.type != EventType.KeyUp)) return;
            KeyCode code = evt.keyCode;
            bool characterOnly = code == KeyCode.None;
            if (characterOnly && evt.character != '\0') code = (KeyCode)char.ToLowerInvariant(evt.character);
            if (!IsGameKey(code)) return;
            if (evt.type == EventType.KeyUp) { guiHeld.Remove(code); return; }
            if (!characterOnly && !guiHeld.Add(code)) return;
            if (handledFrames.TryGetValue(code, out int frame) && frame >= Time.frameCount - 1) return;
            guiPresses[code] = Time.frameCount + 2;
        }
        private static bool IsGameKey(KeyCode code) => code == KeyCode.W || code == KeyCode.A || code == KeyCode.S || code == KeyCode.D ||
            code == KeyCode.UpArrow || code == KeyCode.DownArrow || code == KeyCode.LeftArrow || code == KeyCode.RightArrow ||
            code == KeyCode.T || code == KeyCode.R || code == KeyCode.F || code == KeyCode.G || code == KeyCode.B ||
            code == KeyCode.V || code == KeyCode.P || code == KeyCode.Space || code == KeyCode.F12 ||
            code == KeyCode.Equals || code == KeyCode.Minus || (code >= KeyCode.Alpha1 && code <= KeyCode.Alpha4);
        private static bool Consume(KeyCode code, bool hardware)
        {
            bool buffered = guiPresses.TryGetValue(code, out int expires) && expires >= Time.frameCount;
            guiPresses.Remove(code);
            if (!hardware && !buffered) return false;
            if (handledFrames.TryGetValue(code, out int frame) && frame >= Time.frameCount - 1) return false;
            handledFrames[code] = Time.frameCount;
            return true;
        }
        public static Vector2 Move
        {
            get
            {
                if(!Active)return Vector2.zero;
                Vector2 native = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k != null) native = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (native.sqrMagnitude == 0) native = new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
#endif
                if (native.sqrMagnitude > 0) return native;
                return new Vector2((guiHeld.Contains(KeyCode.D) || guiHeld.Contains(KeyCode.RightArrow) ? 1 : 0) -
                    (guiHeld.Contains(KeyCode.A) || guiHeld.Contains(KeyCode.LeftArrow) ? 1 : 0),
                    (guiHeld.Contains(KeyCode.W) || guiHeld.Contains(KeyCode.UpArrow) ? 1 : 0) -
                    (guiHeld.Contains(KeyCode.S) || guiHeld.Contains(KeyCode.DownArrow) ? 1 : 0));
            }
        }
        public static bool Press(char key)
        {
            if(!Active)return false;
            KeyCode code = key == '+' ? KeyCode.Equals : key == '-' ? KeyCode.Minus : (KeyCode)key;
            bool native = NativePress(key);
#if ENABLE_LEGACY_INPUT_MANAGER
            native |= Input.GetKeyDown(code);
#endif
            return Consume(code, native);
        }
        private static bool NativePress(char key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case '1': return k.digit1Key.wasPressedThisFrame;
                case '2': return k.digit2Key.wasPressedThisFrame;
                case '3': return k.digit3Key.wasPressedThisFrame;
                case '4': return k.digit4Key.wasPressedThisFrame;
                case 't': return k.tKey.wasPressedThisFrame;
                case 'r': return k.rKey.wasPressedThisFrame;
                case 'f': return k.fKey.wasPressedThisFrame;
                case 'g': return k.gKey.wasPressedThisFrame;
                case 'b': return k.bKey.wasPressedThisFrame;
                case 'v': return k.vKey.wasPressedThisFrame;
                case 'p': return k.pKey.wasPressedThisFrame;
                case '+': return k.equalsKey.wasPressedThisFrame;
                case '-': return k.minusKey.wasPressedThisFrame;
                case ' ': return k.spaceKey.wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key == '+' ? KeyCode.Equals : key == '-' ? KeyCode.Minus : (KeyCode)key);
#else
            return false;
#endif
        }
        public static bool ActionHeld => Active && (NativeActionHeld || guiHeld.Contains(KeyCode.Space) || Press(' '));
        public static bool PhotoPressed
        {
            get
            {
                if(!Active)return false;
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed = Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= Input.GetKeyDown(KeyCode.F12);
#endif
                return Consume(KeyCode.F12, pressed);
            }
        }
        private static bool NativeActionHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKey(KeyCode.Space);
#else
                return false;
#endif
            }
        }
        public static float Scroll
        {
            get
            {
                if(!Active)return 0;
#if ENABLE_INPUT_SYSTEM
                return Mouse.current == null ? 0 : Mouse.current.scroll.ReadValue().y / 120f;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mouseScrollDelta.y;
#else
                return 0;
#endif
            }
        }
    }
}
