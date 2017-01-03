using System;
using System.Threading;

namespace Trapper
{
    public class WindowContainer
    {
        public System.Windows.Point offset;
        public IntPtr childWindowHandle;
        public Win32.Rect childWindowRect;

        public IntPtr mainWindowHandle;
        public Win32.Rect mainWindowRect;

        public WindowContainer(IntPtr mainWindow, IntPtr windowHandle)
        {
            this.childWindowHandle = windowHandle;
            this.mainWindowHandle = mainWindow;

            childWindowRect = new Win32.Rect();
            Win32.GetWindowRect(windowHandle, ref childWindowRect);
            Win32.GetWindowRect(mainWindow, ref mainWindowRect);

            offset.X = childWindowRect.Left - mainWindowRect.Left;
            offset.Y = childWindowRect.Top - mainWindowRect.Top;
        }


        public void setWindow(Win32.WinDocPos windowDockLocation = Win32.WinDocPos.main)
        {
            Win32.Rect main = new Win32.Rect();
            Win32.GetWindowRect(mainWindowHandle, ref main);
            childWindowRect.Left = main.Left;
            childWindowRect.Top = main.Top;

            if (windowDockLocation != Win32.WinDocPos.main)
            { // these windows require sizing
                int WTop = childWindowRect.Top;
                int WLeft = childWindowRect.Left;
                int WWidth = 1000;
                int WHeight = 1000;
                switch (windowDockLocation) // set size
                {
                    case Win32.WinDocPos.TopLeft:
                    case Win32.WinDocPos.TopRight:
                    case Win32.WinDocPos.BottomLeft:
                    case Win32.WinDocPos.BottomRight:
                        WWidth = main.Width / 2;
                        WHeight = main.Height / 2;
                        break;
                    case Win32.WinDocPos.Top:
                    case Win32.WinDocPos.Bottom:
                        WWidth = main.Width;
                        WHeight = main.Height / 2;
                        break;
                    case Win32.WinDocPos.Right:
                    case Win32.WinDocPos.Left:
                        WWidth = main.Width / 2;
                        WHeight = main.Height;
                        break;
                }
                switch (windowDockLocation)
                {
                    case Win32.WinDocPos.TopLeft:
                    case Win32.WinDocPos.Left:
                    case Win32.WinDocPos.Top:
                        // Already in pos
                        break;
                    case Win32.WinDocPos.Bottom:
                    case Win32.WinDocPos.BottomLeft:
                        WTop += main.Height / 2;
                        break;
                    case Win32.WinDocPos.TopRight:
                    case Win32.WinDocPos.Right:
                        WLeft += main.Width / 2;
                        break;
                    case Win32.WinDocPos.BottomRight:
                        WTop += main.Height / 2;
                        WLeft += main.Width / 2;
                        break;
                }

                // Update
                childWindowRect.Top = WTop;
                childWindowRect.Left = WLeft;
                offset.X = 0;
                offset.Y = 0;

                Thread.Sleep(100);
                Win32.SetWindowPos(
                        childWindowHandle,
                        Win32.HWND_TOPMOST,
                        WLeft,
                        WTop,
                        WWidth,
                        WHeight,
                        Win32.SWP_SHOWWINDOW | Win32.SWP_NOACTIVATE);
            }
            else
            {
                Win32.SetWindowPos(
                        childWindowHandle,
                        Win32.HWND_TOPMOST,
                        (int)(childWindowRect.Left + offset.X),
                        (int)(childWindowRect.Top + offset.Y),
                        0,
                        0,
                        Win32.SWP_SHOWWINDOW | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
            }
            resetWindow();
        }
        public void resetWindow()
        {
            Win32.SetWindowPos(
                childWindowHandle,
                Win32.HWND_NOTOPMOST,
                0, 0, 0, 0,
                Win32.SWP_SHOWWINDOW | Win32.SWP_NOSIZE | Win32.SWP_NOMOVE | Win32.SWP_NOACTIVATE);
        }
        public void setTop()
        {
            Win32.SetWindowPos(
                childWindowHandle,
                Win32.HWND_TOPMOST,
                0, 0, 0, 0,
                Win32.SWP_SHOWWINDOW | Win32.SWP_NOSIZE | Win32.SWP_NOMOVE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }
        public void setMinimizeWindow()
        {
            Win32.ShowWindow(childWindowHandle, Win32.SW_MINIMIZE);
        }
        public void showWindow()
        {
            Win32.ShowWindow(childWindowHandle, Win32.SW_SHOWNOACTIVATE);
        }
        public override bool Equals(object obj)
        {
            if (obj is WindowContainer)
            {
                WindowContainer win = obj as WindowContainer;
                if (win.childWindowHandle == this.childWindowHandle)
                    return true;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void setTabSwitchVisible(bool val = true)
        {
            int exStyle = (int)Win32.GetWindowLong(childWindowHandle, (int)Win32.GetWindowLongFields.GWL_EXSTYLE);
            if (!val) 
            { // adds tool window style
                exStyle |= (int)Win32.WindowStylesEx.WS_EX_TOOLWINDOW;
            }
            else
            { // removes the tool window style
                exStyle -= (int)Win32.WindowStylesEx.WS_EX_TOOLWINDOW;
            }
            Win32.SetWindowLong(childWindowHandle, (int)Win32.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

    }
}
