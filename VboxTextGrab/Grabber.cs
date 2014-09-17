using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace VboxTextGrab
{
    static class Grabber
    {
        public static Bitmap GrabScreen()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                int pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (Process.GetProcessById(pid).ProcessName.ToLowerInvariant() == "virtualbox")
                {
                    IntPtr hwndChild = FindChild(FindChild(FindChild(hwnd, 2, 4), 0, 1), 2, 3);
                    if (hwndChild != IntPtr.Zero)
                    {
                        RECT rect;
                        GetWindowRect(hwndChild, out rect);

                        Bitmap bitmap = new Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top);

                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(new Point(rect.Left, rect.Top), Point.Empty, bitmap.Size);
                        }
                        return bitmap;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static IntPtr FindChild(IntPtr hwnd, int childToFind, int childCount)
        {
            if (hwnd == IntPtr.Zero)
                return hwnd;
            IntPtr hwndChild = GetWindow(hwnd, GetWindow_Cmd.GW_CHILD);
            IntPtr result = childToFind == 0 ? hwndChild : IntPtr.Zero;
            for (int i = 1; i < childCount && hwndChild != IntPtr.Zero; i++)
            {
                hwndChild = GetWindow(hwndChild, GetWindow_Cmd.GW_HWNDNEXT);
                if (i == childToFind)
                    result = hwndChild;
            }
            if (hwndChild == IntPtr.Zero || GetWindow(hwndChild, GetWindow_Cmd.GW_HWNDNEXT) != IntPtr.Zero)
                result = IntPtr.Zero;
            return result;
        }

        #region PInvoke declarations

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
        #endregion
    }
}
