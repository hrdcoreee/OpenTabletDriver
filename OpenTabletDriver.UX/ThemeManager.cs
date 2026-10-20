using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using OpenTabletDriver.Devices.Android;

namespace OpenTabletDriver.UX
{
    /// <summary>
    /// Manages the application-wide color theme preference (Light / Dark / System).
    /// Platform-specific implementations (e.g. WPF) should subscribe to
    /// <see cref="ThemeChanged"/> and apply the actual visual changes.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>Current saved theme preference.</summary>
        public static AppTheme SelectedTheme { get; private set; } = AppTheme.System;

        /// <summary>
        /// Resolved effective theme — either Light or Dark.
        /// Follows system setting when <see cref="SelectedTheme"/> is <see cref="AppTheme.System"/>.
        /// </summary>
        public static bool IsDark { get; private set; }

        /// <summary>Fired after a theme change so platform renderers can apply colors.</summary>
        public static event Action? ThemeChanged;

        /// <summary>
        /// Loads the persisted theme preference and fires <see cref="ThemeChanged"/>
        /// so platform subscribers can apply it immediately.
        /// </summary>
        public static void Initialize()
        {
            Apply(TabletizerConfig.Instance.Theme, save: false);
        }

        /// <summary>
        /// Switches to the given theme, optionally persists it, and fires <see cref="ThemeChanged"/>.
        /// </summary>
        public static void Apply(AppTheme theme, bool save = true)
        {
            SelectedTheme = theme;

            IsDark = theme switch
            {
                AppTheme.Dark   => true,
                AppTheme.Light  => false,
                AppTheme.System => IsSystemDark(),
                _               => false
            };

            if (save)
            {
                var cfg = TabletizerConfig.Instance;
                if (cfg.Theme != theme)
                {
                    cfg.Theme = theme;
                    cfg.Save();
                }
            }

            ThemeChanged?.Invoke();
        }

        /// <summary>Returns true when Windows is configured to use Dark mode.</summary>
        public static bool IsSystemDark()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                // AppsUseLightTheme: 0 = dark, 1 = light
                if (key?.GetValue("AppsUseLightTheme") is int v)
                    return v == 0;
            }
            catch { /* ignore */ }

            return false;
        }
    }
}
