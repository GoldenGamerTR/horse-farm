using UnityEngine;
using UnityEngine.InputSystem;

namespace HorseFarm.Core
{
    public static class GameInput
    {
        public static Vector2 GetMoveVector()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = 0f;
            float y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

            Vector2 v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static bool IsSprintHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        }

        public static bool IsAltHeld()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
        }

        public static bool IsJumpPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
        }

        public static bool IsInteractPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.eKey.wasPressedThisFrame;
        }

        public static bool IsScreamPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.fKey.wasPressedThisFrame;
        }

        public static bool IsInventoryPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.iKey.wasPressedThisFrame;
        }

        public static bool IsQuestsPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.jKey.wasPressedThisFrame;
        }

        public static bool IsBarnPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.bKey.wasPressedThisFrame;
        }

        public static bool IsRegionsPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.mKey.wasPressedThisFrame;
        }

        public static bool IsWolfTestPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.tKey.wasPressedThisFrame;
        }

        public static bool IsEscapePressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        public static Vector2 GetMouseLookDelta()
        {
            var mouse = Mouse.current;
            if (mouse == null) return Vector2.zero;
            return mouse.delta.ReadValue();
        }

        public static bool IsRightMouseHeld()
        {
            var mouse = Mouse.current;
            return mouse != null && mouse.rightButton.isPressed;
        }
    }
}
