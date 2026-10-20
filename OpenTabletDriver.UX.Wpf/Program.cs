using System;
using System.Security.Principal;
using System.Windows;
using System.Windows.Interop;
using Eto.Forms;
using OpenTabletDriver.UX;

namespace OpenTabletDriver.UX.Wpf
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                _ = new Eto.Forms.Application(Eto.Platforms.Wpf);
                Eto.Forms.MessageBox.Show("OpenTabletDriver should not be run with administrator privileges.\n" +
                    "Some features may not work as intended, such as Plugin Manager and Tablet Debugger.\n" +
                    "If you did not manually set OpenTabletDriver to run with administrator privileges please enable UAC to resolve this.\n" +
                    "If it isn't resolved after, you are using the Administrator named account which overrides UAC.", MessageBoxType.Warning);
                Eto.Platform.AllowReinitialize = true;
            }

            // Hook window loaded globally so all windows get DWM dark title bar and theme
            EventManager.RegisterClassHandler(
                typeof(System.Windows.Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, e) =>
                {
                    if (s is System.Windows.Window w)
                    {
                        WpfThemeApplier.ApplyToWindow(w, ThemeManager.IsDark);
                    }
                }));

            // Hook menu popup opened globally so submenus get dark background and light text
            EventManager.RegisterClassHandler(
                typeof(System.Windows.Controls.MenuItem),
                System.Windows.Controls.MenuItem.SubmenuOpenedEvent,
                new RoutedEventHandler((s, e) =>
                {
                    if (s is System.Windows.Controls.MenuItem mi)
                        WpfThemeApplier.StyleMenuItemPopup(mi, ThemeManager.IsDark);
                }));

            // Hook context menu opened globally
            EventManager.RegisterClassHandler(
                typeof(System.Windows.Controls.ContextMenu),
                System.Windows.Controls.ContextMenu.OpenedEvent,
                new RoutedEventHandler((s, e) =>
                {
                    if (s is System.Windows.Controls.ContextMenu ctx)
                        WpfThemeApplier.StyleContextMenu(ctx, ThemeManager.IsDark);
                }));

            // Subscribe to theme changes — apply WPF resources on UI thread
            ThemeManager.ThemeChanged += () =>
            {
                var wpfApp = System.Windows.Application.Current;
                if (wpfApp?.Dispatcher != null && !wpfApp.Dispatcher.CheckAccess())
                    wpfApp.Dispatcher.Invoke(WpfThemeApplier.Apply);
                else
                    WpfThemeApplier.Apply();
            };

            App.Run(Eto.Platforms.Wpf, args);
        }
    }
}
