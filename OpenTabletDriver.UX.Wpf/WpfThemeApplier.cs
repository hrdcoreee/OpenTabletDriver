using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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

        private static ResourceDictionary? _currentThemeDictionary;

        public static Brush CurrentWindowBackgroundBrush => ThemeManager.IsDark
            ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E))
            : new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));

        public static Brush CurrentTextBrush => ThemeManager.IsDark
            ? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xF0))
            : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));

        public static void Apply()
        {
            var app = System.Windows.Application.Current;
            if (app is null) return;

            bool isDark = ThemeManager.IsDark;

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

            // Update all currently opened windows
            var bgBrush = CurrentWindowBackgroundBrush;
            var fgBrush = CurrentTextBrush;

            foreach (System.Windows.Window window in app.Windows)
            {
                try
                {
                    var helper = new WindowInteropHelper(window);
                    SetWindowDarkMode(helper.Handle, isDark);

                    window.Background = bgBrush;
                    window.Foreground = fgBrush;

                    UpdateVisualTree(window, isDark);
                    window.InvalidateVisual();
                }
                catch
                {
                    // Ignore transient window state issues
                }
            }
        }

        private static void UpdateVisualTree(DependencyObject parent, bool isDark)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is null) continue;

                if (child is System.Windows.Controls.TabControl tc)
                {
                    tc.Background = isDark ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E)) : new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
                    tc.BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x5A)) : new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                }
                else if (child is System.Windows.Controls.GroupBox gb)
                {
                    gb.BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x5A)) : new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                    gb.Foreground = isDark ? new SolidColorBrush(Color.FromRgb(0x7A, 0xB8, 0xFF)) : new SolidColorBrush(Color.FromRgb(0x1A, 0x3E, 0x71));
                }
                else if (child is System.Windows.Controls.Border b)
                {
                    // Check if this border is the root container of window content or inside TabControl
                    if (b.Parent is System.Windows.Controls.TabControl || b.TemplatedParent is System.Windows.Controls.TabControl)
                    {
                        b.Background = isDark ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E)) : new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
                    }
                }

                UpdateVisualTree(child, isDark);
            }
        }

        private const string DarkThemeXaml = @"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

    <!-- Palette Colors & Brushes -->
    <SolidColorBrush x:Key=""WindowBackgroundBrush"" Color=""#1E1E2E"" />
    <SolidColorBrush x:Key=""PanelBackgroundBrush"" Color=""#252538"" />
    <SolidColorBrush x:Key=""BorderBrush"" Color=""#3A3A5A"" />
    <SolidColorBrush x:Key=""TextBrush"" Color=""#E0E0F0"" />
    <SolidColorBrush x:Key=""SubTextBrush"" Color=""#A0A0C0"" />
    <SolidColorBrush x:Key=""AccentBrush"" Color=""#7AB8FF"" />
    <SolidColorBrush x:Key=""ButtonBackgroundBrush"" Color=""#313150"" />
    <SolidColorBrush x:Key=""ButtonHoverBrush"" Color=""#3E3E66"" />
    <SolidColorBrush x:Key=""ButtonPressedBrush"" Color=""#25253E"" />
    <SolidColorBrush x:Key=""ButtonDisabledBrush"" Color=""#222230"" />
    <SolidColorBrush x:Key=""ButtonDisabledTextBrush"" Color=""#606078"" />
    <SolidColorBrush x:Key=""ButtonDisabledBorderBrush"" Color=""#2A2A38"" />
    <SolidColorBrush x:Key=""InputBackgroundBrush"" Color=""#252538"" />
    <SolidColorBrush x:Key=""TabSelectedBrush"" Color=""#252538"" />
    <SolidColorBrush x:Key=""TabHoverBrush"" Color=""#2E2E44"" />

    <!-- Standard SystemColors Overrides -->
    <SolidColorBrush x:Key=""{x:Static SystemColors.WindowBrushKey}"" Color=""#1E1E2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.WindowTextBrushKey}"" Color=""#E0E0F0"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlBrushKey}"" Color=""#1E1E2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlTextBrushKey}"" Color=""#E0E0F0"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlLightBrushKey}"" Color=""#252538"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlLightLightBrushKey}"" Color=""#252538"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlDarkBrushKey}"" Color=""#3A3A5A"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlDarkDarkBrushKey}"" Color=""#3A3A5A"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.HighlightBrushKey}"" Color=""#7AB8FF"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.HighlightTextBrushKey}"" Color=""#1E1E2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.MenuBrushKey}"" Color=""#1E1E2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.MenuTextBrushKey}"" Color=""#E0E0F0"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.MenuBarBrushKey}"" Color=""#1E1E2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.GrayTextBrushKey}"" Color=""#A0A0C0"" />

    <!-- Window Style -->
    <Style TargetType=""{x:Type Window}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- TabControl Style -->
    <Style TargetType=""{x:Type TabControl}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type TabControl}"">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""*"" />
                        </Grid.RowDefinitions>
                        <TabPanel Grid.Row=""0"" IsItemsHost=""True"" Background=""{StaticResource WindowBackgroundBrush}"" />
                        <Border Grid.Row=""1"" Background=""{StaticResource WindowBackgroundBrush}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"">
                            <ContentPresenter ContentSource=""SelectedContent"" />
                        </Border>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- TabItem Style -->
    <Style TargetType=""{x:Type TabItem}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource SubTextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""Padding"" Value=""12,6,12,6"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type TabItem}"">
                    <Border Name=""Border"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""1,1,1,0"" CornerRadius=""4,4,0,0"" Margin=""2,0,2,0"" Padding=""{TemplateBinding Padding}"">
                        <ContentPresenter ContentSource=""Header"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" RecognizesAccessKey=""True"" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsSelected"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource TabSelectedBrush}"" />
                            <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""{StaticResource AccentBrush}"" />
                            <Setter Property=""Foreground"" Value=""{StaticResource AccentBrush}"" />
                        </Trigger>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource TabHoverBrush}"" />
                            <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- GroupBox Style -->
    <Style TargetType=""{x:Type GroupBox}"">
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Foreground"" Value=""{StaticResource AccentBrush}"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type GroupBox}"">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""*"" />
                        </Grid.RowDefinitions>
                        <Border Grid.Row=""0"" Padding=""4,2,4,2"">
                            <ContentPresenter ContentSource=""Header"" RecognizesAccessKey=""True"" TextElement.Foreground=""{StaticResource AccentBrush}"" TextElement.FontWeight=""SemiBold"" />
                        </Border>
                        <Border Grid.Row=""1"" Background=""{StaticResource PanelBackgroundBrush}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""4"" Padding=""{TemplateBinding Padding}"">
                            <ContentPresenter />
                        </Border>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Button Style -->
    <Style TargetType=""{x:Type Button}"">
        <Setter Property=""Background"" Value=""{StaticResource ButtonBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Padding"" Value=""10,4,10,4"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type Button}"">
                    <Border Name=""Border"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""4"" Padding=""{TemplateBinding Padding}"">
                        <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" RecognizesAccessKey=""True"" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource ButtonHoverBrush}"" />
                            <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""{StaticResource AccentBrush}"" />
                        </Trigger>
                        <Trigger Property=""IsPressed"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource ButtonPressedBrush}"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource ButtonDisabledBrush}"" />
                            <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""{StaticResource ButtonDisabledBorderBrush}"" />
                            <Setter Property=""Foreground"" Value=""{StaticResource ButtonDisabledTextBrush}"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- TextBox Style -->
    <Style TargetType=""{x:Type TextBox}"">
        <Setter Property=""Background"" Value=""{StaticResource InputBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""CaretBrush"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""SelectionBrush"" Value=""{StaticResource AccentBrush}"" />
        <Setter Property=""Padding"" Value=""4,2,4,2"" />
    </Style>

    <!-- CheckBox Style -->
    <Style TargetType=""{x:Type CheckBox}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- RadioButton Style -->
    <Style TargetType=""{x:Type RadioButton}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- Label & TextBlock Styles -->
    <Style TargetType=""{x:Type Label}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>
    <Style TargetType=""{x:Type TextBlock}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- Menu & MenuItem Styles -->
    <Style TargetType=""{x:Type Menu}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>
    <Style TargetType=""{x:Type MenuItem}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>
    <Style TargetType=""{x:Type ContextMenu}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>
    <Style TargetType=""{x:Type Separator}"">
        <Setter Property=""Background"" Value=""{StaticResource BorderBrush}"" />
    </Style>

    <!-- ListBox & ComboBox Styles -->
    <Style TargetType=""{x:Type ListBox}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>
    <Style TargetType=""{x:Type ComboBox}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>
    <Style TargetType=""{x:Type DataGrid}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""RowBackground"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""AlternatingRowBackground"" Value=""{StaticResource WindowBackgroundBrush}"" />
    </Style>

</ResourceDictionary>";

        private const string LightThemeXaml = @"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

    <!-- Palette Colors & Brushes -->
    <SolidColorBrush x:Key=""WindowBackgroundBrush"" Color=""#F5F5F5"" />
    <SolidColorBrush x:Key=""PanelBackgroundBrush"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""BorderBrush"" Color=""#CCCCCC"" />
    <SolidColorBrush x:Key=""TextBrush"" Color=""#1A1A2E"" />
    <SolidColorBrush x:Key=""SubTextBrush"" Color=""#555577"" />
    <SolidColorBrush x:Key=""AccentBrush"" Color=""#1A3E71"" />
    <SolidColorBrush x:Key=""ButtonBackgroundBrush"" Color=""#E8E8F0"" />
    <SolidColorBrush x:Key=""ButtonHoverBrush"" Color=""#D0D0E8"" />
    <SolidColorBrush x:Key=""ButtonPressedBrush"" Color=""#C0C0D8"" />
    <SolidColorBrush x:Key=""ButtonDisabledBrush"" Color=""#F0F0F0"" />
    <SolidColorBrush x:Key=""ButtonDisabledTextBrush"" Color=""#888888"" />
    <SolidColorBrush x:Key=""ButtonDisabledBorderBrush"" Color=""#E0E0E0"" />
    <SolidColorBrush x:Key=""InputBackgroundBrush"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""TabSelectedBrush"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""TabHoverBrush"" Color=""#EBEBEB"" />

    <!-- Standard SystemColors Overrides -->
    <SolidColorBrush x:Key=""{x:Static SystemColors.WindowBrushKey}"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.WindowTextBrushKey}"" Color=""#1A1A2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlBrushKey}"" Color=""#F5F5F5"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlTextBrushKey}"" Color=""#1A1A2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlLightBrushKey}"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlLightLightBrushKey}"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlDarkBrushKey}"" Color=""#CCCCCC"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.ControlDarkDarkBrushKey}"" Color=""#CCCCCC"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.HighlightBrushKey}"" Color=""#1A3E71"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.HighlightTextBrushKey}"" Color=""#FFFFFF"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.MenuBrushKey}"" Color=""#F5F5F5"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.MenuTextBrushKey}"" Color=""#1A1A2E"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.MenuBarBrushKey}"" Color=""#F5F5F5"" />
    <SolidColorBrush x:Key=""{x:Static SystemColors.GrayTextBrushKey}"" Color=""#555577"" />

    <!-- Window Style -->
    <Style TargetType=""{x:Type Window}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- TabControl Style -->
    <Style TargetType=""{x:Type TabControl}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type TabControl}"">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""*"" />
                        </Grid.RowDefinitions>
                        <TabPanel Grid.Row=""0"" IsItemsHost=""True"" Background=""{StaticResource WindowBackgroundBrush}"" />
                        <Border Grid.Row=""1"" Background=""{StaticResource PanelBackgroundBrush}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"">
                            <ContentPresenter ContentSource=""SelectedContent"" />
                        </Border>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- TabItem Style -->
    <Style TargetType=""{x:Type TabItem}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource SubTextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""Padding"" Value=""12,6,12,6"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type TabItem}"">
                    <Border Name=""Border"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""1,1,1,0"" CornerRadius=""4,4,0,0"" Margin=""2,0,2,0"" Padding=""{TemplateBinding Padding}"">
                        <ContentPresenter ContentSource=""Header"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" RecognizesAccessKey=""True"" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsSelected"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource TabSelectedBrush}"" />
                            <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""{StaticResource AccentBrush}"" />
                            <Setter Property=""Foreground"" Value=""{StaticResource AccentBrush}"" />
                        </Trigger>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource TabHoverBrush}"" />
                            <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- GroupBox Style -->
    <Style TargetType=""{x:Type GroupBox}"">
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Foreground"" Value=""{StaticResource AccentBrush}"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type GroupBox}"">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""*"" />
                        </Grid.RowDefinitions>
                        <Border Grid.Row=""0"" Padding=""4,2,4,2"">
                            <ContentPresenter ContentSource=""Header"" RecognizesAccessKey=""True"" TextElement.Foreground=""{StaticResource AccentBrush}"" TextElement.FontWeight=""SemiBold"" />
                        </Border>
                        <Border Grid.Row=""1"" Background=""{StaticResource PanelBackgroundBrush}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""4"" Padding=""{TemplateBinding Padding}"">
                            <ContentPresenter />
                        </Border>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Button Style -->
    <Style TargetType=""{x:Type Button}"">
        <Setter Property=""Background"" Value=""{StaticResource ButtonBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Padding"" Value=""10,4,10,4"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type Button}"">
                    <Border Name=""Border"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""4"" Padding=""{TemplateBinding Padding}"">
                        <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" RecognizesAccessKey=""True"" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource ButtonHoverBrush}"" />
                            <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""{StaticResource AccentBrush}"" />
                        </Trigger>
                        <Trigger Property=""IsPressed"" Value=""True"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource ButtonPressedBrush}"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter TargetName=""Border"" Property=""Background"" Value=""{StaticResource ButtonDisabledBrush}"" />
                            <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""{StaticResource ButtonDisabledBorderBrush}"" />
                            <Setter Property=""Foreground"" Value=""{StaticResource ButtonDisabledTextBrush}"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- TextBox Style -->
    <Style TargetType=""{x:Type TextBox}"">
        <Setter Property=""Background"" Value=""{StaticResource InputBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""CaretBrush"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""SelectionBrush"" Value=""{StaticResource AccentBrush}"" />
        <Setter Property=""Padding"" Value=""4,2,4,2"" />
    </Style>

    <!-- CheckBox Style -->
    <Style TargetType=""{x:Type CheckBox}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- RadioButton Style -->
    <Style TargetType=""{x:Type RadioButton}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- Label & TextBlock Styles -->
    <Style TargetType=""{x:Type Label}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>
    <Style TargetType=""{x:Type TextBlock}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>

    <!-- Menu & MenuItem Styles -->
    <Style TargetType=""{x:Type Menu}"">
        <Setter Property=""Background"" Value=""{StaticResource WindowBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>
    <Style TargetType=""{x:Type MenuItem}"">
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
    </Style>
    <Style TargetType=""{x:Type ContextMenu}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>
    <Style TargetType=""{x:Type Separator}"">
        <Setter Property=""Background"" Value=""{StaticResource BorderBrush}"" />
    </Style>

    <!-- ListBox & ComboBox Styles -->
    <Style TargetType=""{x:Type ListBox}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>
    <Style TargetType=""{x:Type ComboBox}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
    </Style>
    <Style TargetType=""{x:Type DataGrid}"">
        <Setter Property=""Background"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}"" />
        <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}"" />
        <Setter Property=""RowBackground"" Value=""{StaticResource PanelBackgroundBrush}"" />
        <Setter Property=""AlternatingRowBackground"" Value=""{StaticResource WindowBackgroundBrush}"" />
    </Style>

</ResourceDictionary>";
    }
}
