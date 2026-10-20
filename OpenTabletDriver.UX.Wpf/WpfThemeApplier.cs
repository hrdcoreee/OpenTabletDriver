using System.Windows;
using System.Windows.Media;
using OpenTabletDriver.UX;

namespace OpenTabletDriver.UX.Wpf
{
    /// <summary>
    /// Applies WPF <see cref="ResourceDictionary"/> colors based on the current
    /// <see cref="ThemeManager"/> selection. Hook into <see cref="ThemeManager.ThemeChanged"/>
    /// and call <see cref="Apply"/> to update colors at runtime.
    /// </summary>
    public static class WpfThemeApplier
    {
        // ─── Dark palette ───────────────────────────────────────────────────────
        private static readonly Color DarkBackground      = Color.FromRgb(0x1E, 0x1E, 0x2E);
        private static readonly Color DarkPanel           = Color.FromRgb(0x2A, 0x2A, 0x3E);
        private static readonly Color DarkText            = Color.FromRgb(0xE0, 0xE0, 0xF0);
        private static readonly Color DarkSubText         = Color.FromRgb(0xA0, 0xA0, 0xC0);
        private static readonly Color DarkBorder          = Color.FromRgb(0x3A, 0x3A, 0x5A);
        private static readonly Color DarkButton          = Color.FromRgb(0x31, 0x31, 0x50);
        private static readonly Color DarkButtonHover     = Color.FromRgb(0x3E, 0x3E, 0x66);
        private static readonly Color DarkInput           = Color.FromRgb(0x25, 0x25, 0x38);
        private static readonly Color DarkAccent          = Color.FromRgb(0x7A, 0xB8, 0xFF);

        // ─── Light palette ───────────────────────────────────────────────────────
        private static readonly Color LightBackground     = Color.FromRgb(0xF5, 0xF5, 0xF5);
        private static readonly Color LightPanel          = Color.FromRgb(0xFF, 0xFF, 0xFF);
        private static readonly Color LightText           = Color.FromRgb(0x1A, 0x1A, 0x2E);
        private static readonly Color LightSubText        = Color.FromRgb(0x55, 0x55, 0x77);
        private static readonly Color LightBorder         = Color.FromRgb(0xCC, 0xCC, 0xCC);
        private static readonly Color LightButton         = Color.FromRgb(0xE8, 0xE8, 0xF0);
        private static readonly Color LightButtonHover    = Color.FromRgb(0xD0, 0xD0, 0xE8);
        private static readonly Color LightInput          = Color.FromRgb(0xFF, 0xFF, 0xFF);
        private static readonly Color LightAccent         = Color.FromRgb(0x1A, 0x3E, 0x71);

        /// <summary>
        /// Applies the current theme from <see cref="ThemeManager"/> to WPF resources.
        /// Call this from <see cref="ThemeManager.ThemeChanged"/> handler (on UI thread).
        /// </summary>
        public static void Apply()
        {
            if (System.Windows.Application.Current is null)
                return;

            if (ThemeManager.IsDark)
                ApplyPalette(
                    DarkBackground, DarkPanel, DarkText, DarkSubText,
                    DarkBorder, DarkButton, DarkButtonHover, DarkInput, DarkAccent);
            else
                ApplyPalette(
                    LightBackground, LightPanel, LightText, LightSubText,
                    LightBorder, LightButton, LightButtonHover, LightInput, LightAccent);
        }

        private static void ApplyPalette(
            Color background, Color panel, Color text, Color subText,
            Color border, Color button, Color buttonHover, Color input, Color accent)
        {
            var resources = System.Windows.Application.Current.Resources;

            // Window / app background
            SetBrush(resources, "WindowBackgroundBrush",     background);
            SetBrush(resources, "PanelBackgroundBrush",      panel);
            SetBrush(resources, "ControlBackgroundBrush",    panel);

            // Text
            SetBrush(resources, "TextBrush",                 text);
            SetBrush(resources, "SubTextBrush",              subText);
            SetBrush(resources, "LabelTextBrush",            text);

            // Borders
            SetBrush(resources, "BorderBrush",               border);

            // Buttons
            SetBrush(resources, "ButtonBackgroundBrush",     button);
            SetBrush(resources, "ButtonHoverBackgroundBrush",buttonHover);
            SetBrush(resources, "ButtonTextBrush",           text);

            // Inputs
            SetBrush(resources, "InputBackgroundBrush",      input);
            SetBrush(resources, "InputTextBrush",            text);

            // Accent
            SetBrush(resources, "AccentBrush",               accent);

            // ── Override WPF system brush keys ──────────────────────────────────
            // These affect controls that use system brushes (Button, TextBox, etc.)
            resources[SystemColors.WindowBrushKey]           = new SolidColorBrush(background);
            resources[SystemColors.WindowTextBrushKey]       = new SolidColorBrush(text);
            resources[SystemColors.ControlBrushKey]          = new SolidColorBrush(panel);
            resources[SystemColors.ControlTextBrushKey]      = new SolidColorBrush(text);
            resources[SystemColors.ControlLightBrushKey]     = new SolidColorBrush(panel);
            resources[SystemColors.ControlLightLightBrushKey]= new SolidColorBrush(panel);
            resources[SystemColors.ControlDarkBrushKey]      = new SolidColorBrush(border);
            resources[SystemColors.ControlDarkDarkBrushKey]  = new SolidColorBrush(border);
            resources[SystemColors.HighlightBrushKey]        = new SolidColorBrush(accent);
            resources[SystemColors.HighlightTextBrushKey]    = new SolidColorBrush(panel);
            resources[SystemColors.MenuBrushKey]             = new SolidColorBrush(panel);
            resources[SystemColors.MenuTextBrushKey]         = new SolidColorBrush(text);
            resources[SystemColors.MenuBarBrushKey]          = new SolidColorBrush(panel);
            resources[SystemColors.GrayTextBrushKey]         = new SolidColorBrush(subText);

            // Color keys (non-brush variants for older controls)
            resources[SystemColors.WindowColorKey]           = background;
            resources[SystemColors.WindowTextColorKey]       = text;
            resources[SystemColors.ControlColorKey]          = panel;
            resources[SystemColors.ControlTextColorKey]      = text;
            resources[SystemColors.MenuColorKey]             = panel;
            resources[SystemColors.MenuTextColorKey]         = text;
        }

        private static void SetBrush(ResourceDictionary resources, string key, Color color)
        {
            if (resources.Contains(key))
                resources[key] = new SolidColorBrush(color);
            else
                resources.Add(key, new SolidColorBrush(color));
        }
    }
}
