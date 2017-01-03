using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Threading.Tasks;
using Forms = System.Windows.Forms;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Diagnostics;

namespace Trapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: Add background color / image
        // TODO: Add icon file change
        // TODO: Add settings
        // TODO: improve proformance by not polling mouse
        // TODO: add movement event to title bar
        #region Keyboard and Mouse Hooks
        
        private IKeyboardMouseEvents m_GlobalHook;
        private void Subscribe()
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            m_GlobalHook = Hook.GlobalEvents();

            m_GlobalHook.MouseUpExt += GlobalHookMouseUpExt;
        }
        private void GlobalHookMouseUpExt(object sender, MouseEventExtArgs e)
        {
            IntPtr current = Win32.GetForegroundWindow();
            Win32.WinDocPos posHolder;
            if ((posHolder = isInWindow(e)) != Win32.WinDocPos.outOfWindow)
            {
                if (current != windowPtr) // and not this(MainWindow) window
                    AddWindow(new WindowContainer(windowPtr, current), posHolder);// add to list
            }
            else
            {
                RemoveWindow(new WindowContainer(windowPtr, current)); // try to remove window if exists
            }
        }
       
        private Win32.WinDocPos isInWindow(MouseEventExtArgs e)
        {
            int X = e.X;
            int Y = e.Y;
            if ((X > Left && X < Width + Left) && (Y > Top && Y < Top + Height))
            { // in window
                int space = 15;
                if((X > Left + space && X < Width + Left - space) && (Y > Top + space && Y < Top + Height - space))// main
                { 
                    return Win32.WinDocPos.main;
                }
                else if((X > Left && X < Left + space) && (Y > Top + space && Y < Top + Height - space))// left
                {
                    return Win32.WinDocPos.Left;
                }
                else if ((X > Left + space && X < Width + Left - space) && (Y > Top && Y < Top + space))// Top
                {
                    return Win32.WinDocPos.Top;
                }
                else if ((X > Width + Left - space && X < Width + Left) && (Y > Top + space && Y < Top + Height - space))// right
                {
                    return Win32.WinDocPos.Right;
                }
                else if ((X > Left + space && X < Width + Left - space) && (Y > Top + Height - space && Y < Top + Height))// bottom
                {
                    return Win32.WinDocPos.Bottom;
                }
                else if ((X > Left && X < Left + space) && (Y > Top && Y < Top + space))// TopLeft
                {
                    return Win32.WinDocPos.TopLeft;
                }
                else if ((X > Left + Width - space && X < Width + Left) && (Y > Top && Y < Top + space))// TopRight
                {
                    return Win32.WinDocPos.TopRight;
                }
                else if ((X > Left + Width - space && X < Width + Left) && (Y > Top + Height - space && Y < Top + Height))// BottomRight
                {
                    return Win32.WinDocPos.BottomRight;
                }
                else if ((X > Left && X < Left + space) && (Y > Top + Height - space && Y < Top + Height))// BottomLeft
                {
                    return Win32.WinDocPos.BottomLeft;
                }
            }
            return Win32.WinDocPos.outOfWindow;
        }
        public void Unsubscribe()
        {
            m_GlobalHook.MouseDownExt -= GlobalHookMouseUpExt;

            //It is recommened to dispose it
            m_GlobalHook.Dispose();
        }

        #endregion

        IntPtr windowPtr;
        List<Window> allWindows = new List<Window>();
        List<WindowContainer> window = new List<WindowContainer>();

        #region extentions to WindowContainerList
        private void AddWindow(WindowContainer win,Win32.WinDocPos windowDockLocation)
        {
            int index;
            if ((index = window.IndexOf(win)) == -1)
            {
                if (!Properties.Settings.Default.TabSwitchVisible)
                    win.setTabSwitchVisible(false);
                window.Add(win);
            }
            else
            {
                switch (windowDockLocation)
                {
                    case Win32.WinDocPos.main:
                        window[index].offset = win.offset; // reset offset if tried to add again
                        break;
                    case Win32.WinDocPos.TopLeft:
                    case Win32.WinDocPos.Top:
                    case Win32.WinDocPos.TopRight:
                    case Win32.WinDocPos.Right:
                    case Win32.WinDocPos.BottomRight:
                    case Win32.WinDocPos.Bottom:
                    case Win32.WinDocPos.BottomLeft:
                    case Win32.WinDocPos.Left:
                        window[index].setWindow(windowDockLocation);
                        break;
                }
            }
        }
        public void RemoveWindow(WindowContainer win)
        {
            int index;
            if((index = window.IndexOf(win)) != -1)
            {
                if (!Properties.Settings.Default.TabSwitchVisible)
                {
                    win.setTabSwitchVisible(true);
                    window[index].setTabSwitchVisible(true);
                }
                window.RemoveAt(index);
            }
        }
        #endregion


        public MainWindow()
        {
            InitializeComponent();
            
            Trapper.Background = SystemColors.HotTrackBrush;

            AppSwitcher.IsChecked = Properties.Settings.Default.TabSwitchVisible;
            Subscribe(); // get all global key events sent to program
            bool slower = true;
            if(slower)
            { // load additional methods
                this.LocationChanged += Trapper_LocationChanged;
            }
        }
        
        private void Trapper_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Trapper_MouseUp(object sender, MouseButtonEventArgs e)
        {
            foreach (WindowContainer item in window)
            {
                item.setWindow();
            }
        }

        private void Trapper_LocationChanged(object sender, EventArgs e)
        {
            if (window != null && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Task.Factory.StartNew(() =>
                {
                    foreach (WindowContainer win in window)
                        win.setWindow();
                });
            }
        }

        private void Trapper_Loaded(object sender, RoutedEventArgs e)
        {
            windowPtr = new WindowInteropHelper(Application.Current.MainWindow).EnsureHandle();
        }

        private void Trapper_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Unsubscribe();
            if (Properties.Settings.Default.TabSwitchVisible)
            {
                foreach (WindowContainer item in window)
                {
                    item.setTabSwitchVisible(true);
                    item.resetWindow();
                }
            }
            else
            {
                foreach (WindowContainer item in window)
                    item.resetWindow();
            }
        }

        private void Trapper_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    Task.Factory.StartNew(() => {
                        foreach (WindowContainer item in window)
                        {
                            item.showWindow();
                        }
                    });
                    break;
                case WindowState.Minimized:
                    Task.Factory.StartNew(() => {
                        foreach (WindowContainer item in window)
                        {
                            item.setMinimizeWindow();
                            item.resetWindow();
                        }
                    });
                    break;
            }
        }

        private void Trapper_Activated(object sender, EventArgs e)
        {
            foreach (WindowContainer item in window)
            {
                item.setWindow();
            }
        }

        private void Trapper_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (window != null && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Task.Factory.StartNew(() => {
                    foreach (WindowContainer win in window)
                        win.setWindow();
                });
            }
        }

        private void TitleBar_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).IsChecked)
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
            }
            else
            {
                this.WindowStyle = WindowStyle.None;
            }
        }

        private void pinMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = ((MenuItem)sender).IsChecked;
        }

        private void AppSwitcher_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.TabSwitchVisible = ((MenuItem)sender).IsChecked;
            Properties.Settings.Default.Save();
            
            foreach (WindowContainer item in window)
                item.setTabSwitchVisible(Properties.Settings.Default.TabSwitchVisible);
        }
    }
}
