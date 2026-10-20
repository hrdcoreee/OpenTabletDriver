using System;
using System.Security.Principal;
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
                _ = new Application(Eto.Platforms.Wpf);
                MessageBox.Show("OpenTabletDriver should not be run with administrator privileges.\n" +
                    "Some features may not work as intended, such as Plugin Manager and Tablet Debugger.\n" +
                    "If you did not manually set OpenTabletDriver to run with administrator privileges please enable UAC to resolve this.\n" +
                    "If it isn't resolved after, you are using the Administrator named account which overrides UAC.", MessageBoxType.Warning);
                Eto.Platform.AllowReinitialize = true;
            }

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
