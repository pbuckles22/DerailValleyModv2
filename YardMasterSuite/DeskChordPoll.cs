using System.Runtime.InteropServices;
using UnityEngine;

namespace YardMasterSuite
{
    /// <summary>
    /// Unity 2019 omits Ctrl+arrow and Ctrl+Insert from <c>Input.GetKeyDown</c>
    /// and from IMGUI <c>KeyDown</c> (cab 2.16.27: no <c>T2 desk-key</c>).
    /// Read those keys from the OS while the game is focused. Does not call
    /// Rewired and does not write <c>ControlBindings.json</c>.
    /// </summary>
    internal static class DeskChordPoll
    {
        private const int VkControl = 0x11;
        private const int VkLeft = 0x25;
        private const int VkRight = 0x27;
        private const int VkInsert = 0x2D;
        private const int VkLControl = 0xA2;
        private const int VkRControl = 0xA3;

        internal static void Read(out bool control, out bool insert, out bool right, out bool left)
        {
            var unityControl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var unityInsert = Input.GetKey(KeyCode.Insert);
            var unityRight = Input.GetKey(KeyCode.RightArrow);
            var unityLeft = Input.GetKey(KeyCode.LeftArrow);
            if (!Application.isFocused)
            {
                control = unityControl;
                insert = unityInsert;
                right = unityRight;
                left = unityLeft;
                return;
            }

            control = unityControl || Down(VkControl) || Down(VkLControl) || Down(VkRControl);
            insert = unityInsert || Down(VkInsert);
            right = unityRight || Down(VkRight);
            left = unityLeft || Down(VkLeft);
        }

        private static bool Down(int vKey) => (GetAsyncKeyState(vKey) & 0x8000) != 0;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
