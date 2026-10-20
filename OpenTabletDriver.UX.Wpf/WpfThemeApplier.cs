using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using OpenTabletDriver.UX;

namespace OpenTabletDriver.UX.Wpf
{
    public static class WpfThemeApplier
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void SetWindowDarkMode(IntPtr hwnd, bool isDark)
        {
            if (hwnd == IntPtr.Zero) return;
            int value = isDark ? 1 : 0;
            // DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 18985+ / Windows 11)
            DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
            // Older Windows 10 (17763 to 18985)
            DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
        }

        // ─── Color Palettes ────────────────────────────────────────────────────────

        // Dark
        public static readonly SolidColorBrush DarkBgBrush                   = new(Color.FromRgb(0x1E, 0x1E, 0x2E));
        public static readonly SolidColorBrush DarkPanelBrush                = new(Color.FromRgb(0x25, 0x25, 0x38));
        public static readonly SolidColorBrush DarkBorderBrush               = new(Color.FromRgb(0x3A, 0x3A, 0x5A));
        public static readonly SolidColorBrush DarkTextBrush                 = new(Color.FromRgb(0xE0, 0xE0, 0xF0));
        public static readonly SolidColorBrush DarkSubTextBrush              = new(Color.FromRgb(0xA0, 0xA0, 0xC0));
        public static readonly SolidColorBrush DarkAccentBrush               = new(Color.FromRgb(0x7A, 0xB8, 0xFF));
        public static readonly SolidColorBrush DarkButtonBrush               = new(Color.FromRgb(0x31, 0x31, 0x50));
        public static readonly SolidColorBrush DarkButtonHoverBrush          = new(Color.FromRgb(0x3E, 0x3E, 0x66));
        public static readonly SolidColorBrush DarkButtonDisabledBrush       = new(Color.FromRgb(0x22, 0x22, 0x32));
        public static readonly SolidColorBrush DarkButtonDisabledTextBrush   = new(Color.FromRgb(0x60, 0x60, 0x78));
        public static readonly SolidColorBrush DarkButtonDisabledBorderBrush = new(Color.FromRgb(0x2A, 0x2A, 0x3A));

        // Light
        public static readonly SolidColorBrush LightBgBrush                  = new(Color.FromRgb(0xF5, 0xF5, 0xF5));
        public static readonly SolidColorBrush LightPanelBrush               = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
        public static readonly SolidColorBrush LightBorderBrush              = new(Color.FromRgb(0xCC, 0xCC, 0xCC));
        public static readonly SolidColorBrush LightTextBrush                = new(Color.FromRgb(0x1A, 0x1A, 0x2E));
        public static readonly SolidColorBrush LightSubTextBrush             = new(Color.FromRgb(0x55, 0x55, 0x77));
        public static readonly SolidColorBrush LightAccentBrush              = new(Color.FromRgb(0x1A, 0x3E, 0x71));
        public static readonly SolidColorBrush LightButtonBrush              = new(Color.FromRgb(0xE8, 0xE8, 0xF0));
        public static readonly SolidColorBrush LightButtonHoverBrush         = new(Color.FromRgb(0xD0, 0xD0, 0xE8));
        public static readonly SolidColorBrush LightButtonDisabledBrush      = new(Color.FromRgb(0xF0, 0xF0, 0xF0));
        public static readonly SolidColorBrush LightButtonDisabledTextBrush  = new(Color.FromRgb(0x99, 0x99, 0x99));
        public static readonly SolidColorBrush LightButtonDisabledBorderBrush= new(Color.FromRgb(0xDC, 0xDC, 0xDC));

        public static SolidColorBrush CurrentWindowBackgroundBrush => ThemeManager.IsDark ? DarkBgBrush : LightBgBrush;
        public static SolidColorBrush CurrentTextBrush => ThemeManager.IsDark ? DarkTextBrush : LightTextBrush;

        private static ResourceDictionary? _currentThemeDictionary;

        public static void Apply()
        {
            var app = System.Windows.Application.Current;
            if (app is null) return;

            bool isDark = ThemeManager.IsDark;

            // 1. Update WPF system brushes in Application.Current.Resources
            UpdateSystemBrushes(app.Resources, isDark);

            // 2. Load / replace XAML theme dictionary
            try
            {
                string xaml = isDark ? DarkThemeXaml : LightThemeXaml;
                using var stringReader = new StringReader(xaml);
                using var xmlReader = XmlReader.Create(stringReader);
                var newDict = (ResourceDictionary)XamlReader.Load(xmlReader);

                if (_currentThemeDictionary != null)
                    app.Resources.MergedDictionaries.Remove(_currentThemeDictionary);

                app.Resources.MergedDictionaries.Add(newDict);
                _currentThemeDictionary = newDict;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load theme XAML: {ex}");
            }

            // 3. Update all currently open windows
            foreach (System.Windows.Window window in app.Windows)
            {
                try
                {
                    ApplyToWindow(window, isDark);
                }
                catch
                {
                    // Ignore transient window errors
                }
            }
        }

        public static void ApplyToWindow(System.Windows.Window window, bool isDark)
        {
            var helper = new WindowInteropHelper(window);
            SetWindowDarkMode(helper.Handle, isDark);

            window.Background = isDark ? DarkBgBrush : LightBgBrush;
            window.Foreground = isDark ? DarkTextBrush : LightTextBrush;

            UpdateVisualTree(window, isDark);
            window.InvalidateVisual();
        }

        private static void UpdateSystemBrushes(ResourceDictionary res, bool isDark)
        {
            var bg       = isDark ? DarkBgBrush : LightBgBrush;
            var panel    = isDark ? DarkPanelBrush : LightPanelBrush;
            var text     = isDark ? DarkTextBrush : LightTextBrush;
            var subText  = isDark ? DarkSubTextBrush : LightSubTextBrush;
            var border   = isDark ? DarkBorderBrush : LightBorderBrush;
            var accent   = isDark ? DarkAccentBrush : LightAccentBrush;
            var btnHover = isDark ? DarkButtonHoverBrush : LightButtonHoverBrush;

            res[SystemColors.WindowBrushKey]            = bg;
            res[SystemColors.WindowTextBrushKey]        = text;
            res[SystemColors.ControlBrushKey]           = panel;
            res[SystemColors.ControlTextBrushKey]       = text;
            res[SystemColors.ControlLightBrushKey]      = panel;
            res[SystemColors.ControlLightLightBrushKey] = panel;
            res[SystemColors.ControlDarkBrushKey]       = border;
            res[SystemColors.ControlDarkDarkBrushKey]   = border;
            res[SystemColors.HighlightBrushKey]         = btnHover;
            res[SystemColors.HighlightTextBrushKey]     = text;
            res[SystemColors.MenuBrushKey]              = panel;
            res[SystemColors.MenuTextBrushKey]          = text;
            res[SystemColors.MenuBarBrushKey]           = bg;
            res[SystemColors.GrayTextBrushKey]          = subText;

            res[SystemColors.WindowColorKey]            = bg.Color;
            res[SystemColors.WindowTextColorKey]        = text.Color;
            res[SystemColors.ControlColorKey]           = panel.Color;
            res[SystemColors.ControlTextColorKey]       = text.Color;
            res[SystemColors.MenuColorKey]              = panel.Color;
            res[SystemColors.MenuTextColorKey]          = text.Color;
        }

        // ─── Visual Tree Traversal ──────────────────────────────────────────────────

        public static void UpdateVisualTree(DependencyObject parent, bool isDark)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is null) continue;

                ApplyStyleToElement(child, isDark);
                UpdateVisualTree(child, isDark);
            }
        }

        private static void ApplyStyleToElement(DependencyObject element, bool isDark)
        {
            switch (element)
            {
                case System.Windows.Controls.Button btn:
                    ApplyButtonStyle(btn, isDark);
                    btn.IsEnabledChanged -= OnButtonEnabledChanged;
                    btn.IsEnabledChanged += OnButtonEnabledChanged;
                    break;

                case System.Windows.Controls.DataGrid dg:
                    ApplyDataGridStyle(dg, isDark);
                    break;

                case System.Windows.Controls.ComboBox cb:
                    cb.Background = isDark ? DarkPanelBrush : LightPanelBrush;
                    cb.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                    cb.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
                    cb.DropDownOpened -= OnComboBoxDropDownOpened;
                    cb.DropDownOpened += OnComboBoxDropDownOpened;
                    break;

                case System.Windows.Controls.TabControl tc:
                    tc.Background = isDark ? DarkBgBrush : LightBgBrush;
                    tc.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
                    tc.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                    tc.SelectionChanged -= OnTabControlSelectionChanged;
                    tc.SelectionChanged += OnTabControlSelectionChanged;
                    break;

                case System.Windows.Controls.TabItem ti:
                    ti.Background = isDark
                        ? (ti.IsSelected ? DarkPanelBrush : DarkBgBrush)
                        : (ti.IsSelected ? LightPanelBrush : LightBgBrush);
                    ti.Foreground = isDark
                        ? (ti.IsSelected ? DarkAccentBrush : DarkSubTextBrush)
                        : (ti.IsSelected ? LightAccentBrush : LightSubTextBrush);
                    ti.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
                    break;

                case System.Windows.Controls.GroupBox gb:
                    gb.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
                    gb.Foreground = isDark ? DarkAccentBrush : LightAccentBrush;
                    if (gb.Header is TextBlock htb)
                        htb.Foreground = isDark ? DarkAccentBrush : LightAccentBrush;
                    else if (gb.Header is Label hlbl)
                        hlbl.Foreground = isDark ? DarkAccentBrush : LightAccentBrush;
                    break;

                case System.Windows.Controls.TextBox tb:
                    tb.Background = isDark ? DarkPanelBrush : LightPanelBrush;
                    tb.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                    tb.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
                    tb.CaretBrush = isDark ? DarkTextBrush : LightTextBrush;
                    tb.SelectionBrush = isDark ? DarkAccentBrush : LightAccentBrush;
                    break;

                case System.Windows.Controls.Menu menu:
                    menu.Background = isDark ? DarkBgBrush : LightBgBrush;
                    menu.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                    foreach (var item in menu.Items)
                    {
                        if (item is System.Windows.Controls.MenuItem mi)
                        {
                            mi.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                            mi.Background = isDark ? DarkBgBrush : LightBgBrush;
                        }
                    }
                    break;

                case System.Windows.Controls.MenuItem mi:
                    mi.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                    break;

                case System.Windows.Controls.ContextMenu ctx:
                    StyleContextMenu(ctx, isDark);
                    break;

                case System.Windows.Controls.Border b:
                    // Color borders that wrap tab content, dialog roots or log views
                    if (b.Parent is System.Windows.Controls.TabControl || b.TemplatedParent is System.Windows.Controls.TabControl)
                    {
                        b.Background = isDark ? DarkBgBrush : LightBgBrush;
                        b.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
                    }
                    break;

                case System.Windows.Controls.ScrollViewer sv:
                    // Ensure scrollviewers (like in log grid) have matching dark backgrounds
                    if (sv.TemplatedParent is System.Windows.Controls.DataGrid)
                    {
                        sv.Background = isDark ? DarkBgBrush : LightPanelBrush;
                    }
                    break;
            }
        }

        // ─── Control Specific Updaters ──────────────────────────────────────────────

        public static void ApplyButtonStyle(System.Windows.Controls.Button btn, bool isDark)
        {
            if (!btn.IsEnabled)
            {
                btn.Background = isDark ? DarkButtonDisabledBrush : LightButtonDisabledBrush;
                btn.Foreground = isDark ? DarkButtonDisabledTextBrush : LightButtonDisabledTextBrush;
                btn.BorderBrush = isDark ? DarkButtonDisabledBorderBrush : LightButtonDisabledBorderBrush;
            }
            else
            {
                btn.Background = isDark ? DarkButtonBrush : LightButtonBrush;
                btn.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                btn.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
            }
        }

        private static void OnButtonEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
                ApplyButtonStyle(btn, ThemeManager.IsDark);
        }

        private static void ApplyDataGridStyle(System.Windows.Controls.DataGrid dg, bool isDark)
        {
            var bg = isDark ? DarkBgBrush : LightPanelBrush;
            var rowBg = isDark ? DarkPanelBrush : LightPanelBrush;
            var altRowBg = isDark ? DarkBgBrush : LightBgBrush;
            var fg = isDark ? DarkTextBrush : LightTextBrush;
            var border = isDark ? DarkBorderBrush : LightBorderBrush;

            dg.Background = bg;
            dg.RowBackground = rowBg;
            dg.AlternatingRowBackground = altRowBg;
            dg.Foreground = fg;
            dg.BorderBrush = border;
            dg.HorizontalGridLinesBrush = border;
            dg.VerticalGridLinesBrush = border;
        }

        private static void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TabControl tc)
            {
                bool isDark = ThemeManager.IsDark;
                UpdateVisualTree(tc, isDark);
            }
        }

        private static void OnComboBoxDropDownOpened(object? sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cb)
            {
                cb.ApplyTemplate();
                var popup = cb.Template?.FindName("PART_Popup", cb) as Popup;
                if (popup?.Child != null)
                {
                    UpdateVisualTree(popup.Child, ThemeManager.IsDark);
                    if (popup.Child is System.Windows.Controls.Border b)
                    {
                        b.Background = ThemeManager.IsDark ? DarkPanelBrush : LightPanelBrush;
                        b.BorderBrush = ThemeManager.IsDark ? DarkBorderBrush : LightBorderBrush;
                    }
                }
            }
        }

        // ─── Menu Popups & ContextMenus ─────────────────────────────────────────────

        public static void StyleMenuItemPopup(System.Windows.Controls.MenuItem menuItem, bool isDark)
        {
            menuItem.ApplyTemplate();
            var popup = menuItem.Template?.FindName("PART_Popup", menuItem) as Popup;
            if (popup != null && popup.Child != null)
            {
                UpdateVisualTree(popup.Child, isDark);
                if (popup.Child is System.Windows.Controls.Border b)
                {
                    b.Background = isDark ? DarkPanelBrush : LightPanelBrush;
                    b.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;
                }
            }

            foreach (var item in menuItem.Items)
            {
                if (item is System.Windows.Controls.MenuItem mi)
                {
                    mi.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                    mi.Background = isDark ? DarkPanelBrush : LightPanelBrush;
                }
                else if (item is System.Windows.Controls.Separator sep)
                {
                    sep.Background = isDark ? DarkBorderBrush : LightBorderBrush;
                }
                else if (item is DependencyObject d)
                {
                    UpdateVisualTree(d, isDark);
                }
            }
        }

        public static void StyleContextMenu(System.Windows.Controls.ContextMenu ctxMenu, bool isDark)
        {
            ctxMenu.Background = isDark ? DarkPanelBrush : LightPanelBrush;
            ctxMenu.Foreground = isDark ? DarkTextBrush : LightTextBrush;
            ctxMenu.BorderBrush = isDark ? DarkBorderBrush : LightBorderBrush;

            foreach (var item in ctxMenu.Items)
            {
                if (item is System.Windows.Controls.MenuItem mi)
                {
                    mi.Foreground = isDark ? DarkTextBrush : LightTextBrush;
                    mi.Background = isDark ? DarkPanelBrush : LightPanelBrush;
                }
                else if (item is System.Windows.Controls.Separator sep)
                {
                    sep.Background = isDark ? DarkBorderBrush : LightBorderBrush;
                }
                else if (item is DependencyObject d)
                {
                    UpdateVisualTree(d, isDark);
                }
            }
        }

        // ─── XAML Resource Dictionaries ─────────────────────────────────────────────

        private const string DarkThemeXaml = @"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

    <SolidColorBrush x:Key=""WindowBackgroundBrush"" Color=""#1E1E2E"" />
    <SolidColorBrush x:Key=""PanelBackgroundBrush"" Color=""#252538"" />
    <SolidColorBrush x:Key=""BorderBrush"" Color=""#3A3A5A"" />
    <SolidColorBrush x:Key=""TextBrush"" Color=""#E0E0F0"" />
    <SolidColorBrush x:Key=""SubTextBrush"" Color=""#A0A0C0"" />
    <SolidColorBrush x:Key=""AccentBrush"" Color=""#7AB8FF"" />
    <SolidColorBrush x:Key=""ButtonBackgroundBrush"" Color=""#313150"" />
    <SolidColorBrush x:Key=""ButtonHoverBrush"" Color=""#3E3E66"" />
    <SolidColorBrush x:Key=""ButtonDisabledBrush"" Color=""#222232"" />
    <SolidColorBrush x:Key=""ButtonDisabledTextBrush"" Color=""#606078"" />
    <SolidColorBrush x:Key=""ButtonDisabledBorderBrush"" Color=""#2A2A3A"" />

    <Style TargetType=""{x:Type Window}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <Style TargetType=""{x:Type ContextMenu}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>

</ResourceDictionary>";

        private const string LightThemeXaml = @"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

    <SolidColorBrush x:Key=""WindowBackgroundBrush"" Color=""#F5F5F5"" />
    <SolidColorBrush x:Key=""PanelBackgroundBrush"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""BorderBrush"" Color=""#CCCCCC"" />
    <SolidColorBrush x:Key=""TextBrush"" Color=""#1A1A2E"" />
    <SolidColorBrush x:Key=""SubTextBrush"" Color=""#555577"" />
    <SolidColorBrush x:Key=""AccentBrush"" Color=""#1A3E71"" />
    <SolidColorBrush x:Key=""ButtonBackgroundBrush"" Color=""#E8E8F0"" />
    <SolidColorBrush x:Key=""ButtonHoverBrush"" Color=""#D0D0E8"" />
    <SolidColorBrush x:Key=""ButtonDisabledBrush"" Color=""#F0F0F0"" />
    <SolidColorBrush x:Key=""ButtonDisabledTextBrush"" Color=""#999999"" />
    <SolidColorBrush x:Key=""ButtonDisabledBorderBrush"" Color=""#DCDCDC"" />

    <Style TargetType=""{x:Type Window}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <Style TargetType=""{x:Type ContextMenu}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>

</ResourceDictionary>";
    }
}
