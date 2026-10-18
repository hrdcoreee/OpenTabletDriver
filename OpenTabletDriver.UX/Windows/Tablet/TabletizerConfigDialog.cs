using System;
using Eto.Drawing;
using Eto.Forms;
using OpenTabletDriver.Devices.Android;
using OpenTabletDriver.UX.Controls.Generic;

namespace OpenTabletDriver.UX.Windows.Tablet
{
    public class TabletizerConfigDialog : Dialog
    {
        private readonly CheckBox _autoDiscoveryCheck;
        private readonly TextBox _manualHostBox;
        private readonly TextBox _portBox;
        private readonly Label _discoveredLabel;

        public TabletizerConfigDialog()
        {
            Title = "tabletizer Settings (Android Phone Tablet)";
            Size = new Size(480, 430);
            Resizable = false;
            Padding = 15;

            var config = TabletizerConfig.Instance;

            var enableWifiCheck = new CheckBox
            {
                Text = "Enable Wireless (Wi-Fi) Connection",
                Checked = config.EnableWifi
            };

            var preferUsbCheck = new CheckBox
            {
                Text = "Prefer USB (automatically switch to USB when cable is connected)",
                Checked = config.PreferUsb
            };

            _autoDiscoveryCheck = new CheckBox
            {
                Text = "Enable Wi-Fi Auto-Discovery (UDP beacon)",
                Checked = config.AutoDiscovery,
                Enabled = config.EnableWifi
            };

            string lastDiscovered = string.IsNullOrWhiteSpace(config.LastDiscoveredWifiHost)
                ? "None"
                : config.LastDiscoveredWifiHost;

            _discoveredLabel = new Label
            {
                Text = $"Last discovered Wi-Fi phone IP: {lastDiscovered}",
                TextColor = Colors.Gray,
                Enabled = config.EnableWifi
            };

            _manualHostBox = new TextBox
            {
                Text = config.ManualWifiHost ?? string.Empty,
                PlaceholderText = "e.g. 192.168.1.50 (leave empty for auto/USB)",
                Enabled = config.EnableWifi
            };

            _portBox = new TextBox
            {
                Text = config.Port.ToString(),
                PlaceholderText = "3240"
            };

            enableWifiCheck.CheckedChanged += (s, e) =>
            {
                bool wifiEnabled = enableWifiCheck.Checked ?? true;
                _autoDiscoveryCheck.Enabled = wifiEnabled;
                _discoveredLabel.Enabled = wifiEnabled;
                _manualHostBox.Enabled = wifiEnabled;
            };

            var saveButton = new Button { Text = "Save & Connect" };
            saveButton.Click += (sender, e) =>
            {
                config.EnableWifi = enableWifiCheck.Checked ?? true;
                config.PreferUsb = preferUsbCheck.Checked ?? true;
                config.AutoDiscovery = _autoDiscoveryCheck.Checked ?? true;
                config.ManualWifiHost = _manualHostBox.Text?.Trim() ?? string.Empty;
                if (int.TryParse(_portBox.Text?.Trim(), out int port) && port > 0)
                    config.Port = port;
                config.Save();
                Close();
            };

            var cancelButton = new Button { Text = "Cancel" };
            cancelButton.Click += (sender, e) => Close();

            Content = new StackLayout
            {
                Spacing = 12,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    new Label
                    {
                        Text = "tabletizer connects your Android phone as a high-precision osu! tablet over USB or Wi-Fi.",
                        Wrap = WrapMode.Word
                    },
                    preferUsbCheck,
                    new StackLayoutItem
                    {
                        Control = new Group
                        {
                            Text = "Wireless (Wi-Fi) Settings",
                            Padding = 10,
                            Content = new StackLayout
                            {
                                Spacing = 8,
                                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                Items =
                                {
                                    enableWifiCheck,
                                    _autoDiscoveryCheck,
                                    _discoveredLabel,
                                    new Label { Text = "Manual Phone IP (optional override):" },
                                    _manualHostBox,
                                    new Label { Text = "Port (default: 3240):" },
                                    _portBox
                                }
                            }
                        }
                    },
                    new StackLayoutItem
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Control = new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 10,
                            Items = { cancelButton, saveButton }
                        }
                    }
                }
            };

            DefaultButton = saveButton;
            AbortButton = cancelButton;
        }
    }
}
