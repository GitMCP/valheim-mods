using UnityEngine;

namespace NjordWarehouseKeeper.Client
{
    /// <summary>
    /// Valheim's gamepad drives a software cursor on <see cref="ZInput.pointerPosition"/>.
    /// Custom hit tests that read <see cref="Input.mousePosition"/> miss that cursor.
    /// </summary>
    internal static class HubInput
    {
        private const float StickGate = 0.55f;
        private const float RepeatDelay = 0.32f;
        private const float RepeatRate = 0.11f;

        private static float _vHold;
        private static int _vDir;
        private static float _hHold;
        private static int _hDir;

        internal static Vector2 Pointer()
        {
            return ZInput.instance != null
                ? (Vector2)ZInput.pointerPosition
                : (Vector2)Input.mousePosition;
        }

        internal static bool Gamepad()
        {
            return ZInput.IsGamepadActive();
        }

        internal static bool ClickDown()
        {
            return ZInput.GetMouseButtonDown(0) || Down("JoyButtonA");
        }

        internal static bool PoseDown()
        {
            return ZInput.GetKeyDown(KeyCode.R, false)
                || Input.GetKeyDown(KeyCode.R)
                || Down("JoyButtonX");
        }

        internal static string PoseHint()
        {
            return Gamepad() ? "X" : "R";
        }

        internal static bool Down(string button)
        {
            return ZInput.GetButtonDown(button);
        }

        internal static bool Held(string button)
        {
            return ZInput.GetButton(button);
        }

        internal static void Eat(string button)
        {
            ZInput.ResetButtonStatus(button);
        }

        internal static int Vertical()
        {
            var dir = 0;
            if (Down("JoyDPadUp") || Down("JoyLStickUp"))
            {
                dir = 1;
            }
            else if (Down("JoyDPadDown") || Down("JoyLStickDown"))
            {
                dir = -1;
            }
            else
            {
                var stick = ZInput.GetJoyLeftStickY();
                if (stick > StickGate)
                {
                    dir = 1;
                }
                else if (stick < -StickGate)
                {
                    dir = -1;
                }
            }

            return Repeat(ref _vHold, ref _vDir, dir);
        }

        internal static int Horizontal()
        {
            var dir = 0;
            if (Down("JoyDPadRight") || Down("JoyLStickRight"))
            {
                dir = 1;
            }
            else if (Down("JoyDPadLeft") || Down("JoyLStickLeft"))
            {
                dir = -1;
            }
            else
            {
                var stick = ZInput.GetJoyLeftStickX();
                if (stick > StickGate)
                {
                    dir = 1;
                }
                else if (stick < -StickGate)
                {
                    dir = -1;
                }
            }

            return Repeat(ref _hHold, ref _hDir, dir);
        }

        internal static float Scroll()
        {
            return ZInput.GetJoyRightStickY();
        }

        internal static bool Over(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            return Over(go.GetComponent<RectTransform>(), go.GetComponentInParent<Canvas>());
        }

        internal static bool Over(RectTransform rt, Canvas canvas)
        {
            if (rt == null)
            {
                return false;
            }

            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rt, Pointer(), cam);
        }

        private static int Repeat(ref float hold, ref int last, int dir)
        {
            if (dir == 0)
            {
                hold = 0f;
                last = 0;
                return 0;
            }

            if (dir != last)
            {
                last = dir;
                hold = 0f;
                return dir;
            }

            hold += Time.deltaTime;
            if (hold < RepeatDelay)
            {
                return 0;
            }

            hold -= RepeatRate;
            return dir;
        }
    }
}
