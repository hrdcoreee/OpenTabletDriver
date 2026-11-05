using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using OpenTabletDriver.Devices.Android;
using OpenTabletDriver.Plugin.Devices;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.UX.Controls
{
    public class TabletizerConnectionPanel : Panel
    {
        private readonly Label _statusLabel;
        private readonly CheckBox _enableWifiCheck;
        private readonly CheckBox _preferUsbCheck;
        private readonly CheckBox _autoDiscoveryCheck;
        private readonly Label _discoveredLabel;
        private readonly TextBox _manualHostBox;
        private readonly TextBox _portBox;
        private readonly Button _saveButton;
        private readonly Button _reconnectButton;
        private readonly Button _forgetDeviceButton;

        public TabletizerConnectionPanel()
        {
            var config = TabletizerConfig.Instance;

            _statusLabel = new Label
            {
                Text = "Checking connection...",
                Font = SystemFonts.Bold(13),
                TextColor = Colors.SteelBlue
            };

            _enableWifiCheck = new CheckBox
            {
                Text = "Enable Wireless (Wi-Fi) Connection",
                Checked = config.EnableWifi
            };

            _preferUsbCheck = new CheckBox
            {
                Text = "Prefer USB (Automatically upgrade from Wi-Fi when USB cable is connected)",
                Checked = config.PreferUsb
            };

            _autoDiscoveryCheck = new CheckBox
            {
                Text = "Enable Wi-Fi Auto-Discovery (UDP beacon on port 3242)",
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
                Text = config.Port > 0 ? config.Port.ToString() : "3240",
                Width = 100
            };

            _enableWifiCheck.CheckedChanged += (s, e) =>
            {
                bool isWifi = _enableWifiCheck.Checked ?? false;
                _autoDiscoveryCheck.Enabled = isWifi;
                _discoveredLabel.Enabled = isWifi;
                _manualHostBox.Enabled = isWifi;
            };

            _saveButton = new Button
            {
                Text = "Save Connection Settings",
                Width = 180
            };
            _saveButton.Click += async (s, e) =>
            {
                if (!TabletizerConfig.TryValidateWifiHost(_manualHostBox.Text, out var hostError))
                {
                    MessageBox.Show(this, hostError, "tabletizer Connection Settings", MessageBoxButtons.OK, MessageBoxType.Warning);
                    _manualHostBox.Focus();
                    return;
                }

                if (!TabletizerConfig.TryValidatePort(_portBox.Text, out int port, out var portError))
                {
                    MessageBox.Show(this, portError, "tabletizer Connection Settings", MessageBoxButtons.OK, MessageBoxType.Warning);
                    _portBox.Focus();
                    return;
                }

                config.EnableWifi = _enableWifiCheck.Checked ?? true;
                config.PreferUsb = _preferUsbCheck.Checked ?? true;
                config.AutoDiscovery = _autoDiscoveryCheck.Checked ?? true;
                config.ManualWifiHost = string.IsNullOrWhiteSpace(_manualHostBox.Text) ? null : _manualHostBox.Text.Trim();
                config.Port = port;

                config.Save();

                if (App.Driver.IsConnected)
                {
                    try
                    {
                        await App.Driver.Instance.DetectTablets();
                    }
                    catch
                    {
                    }
                }

                MessageBox.Show(this, "Connection settings saved successfully!", "tabletizer", MessageBoxButtons.OK, MessageBoxType.Information);
                RefreshStatus();
            };

            _forgetDeviceButton = new Button
            {
                Text = "Forget Remembered Device"
            };
            _forgetDeviceButton.Click += (s, e) =>
            {
                AndroidTabletHub.Instance?.ForgetLastDiscoveredWifiHost();
                if (AndroidTabletHub.Instance == null)
                {
                    config.LastDiscoveredWifiHost = null;
                    config.Save();
                }

                _discoveredLabel.Text = "Last discovered Wi-Fi phone IP: None";
                _forgetDeviceButton.Enabled = false;
                RefreshStatus();
            };

            _reconnectButton = new Button
            {
                Text = "Reconnect / Redetect",
                Width = 160
            };
            _reconnectButton.Click += async (s, e) =>
            {
                if (App.Driver.IsConnected)
                {
                    try
                    {
                        await App.Driver.Instance.DetectTablets();
                    }
                    catch
                    {
                    }
                }
                RefreshStatus();
            };

            var connectionBox = new GroupBox
            {
                Text = "Active Connection",
                Padding = new Padding(12),
                Content = new StackLayout
                {
                    Spacing = 8,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Items =
                    {
                        _statusLabel
                    }
                }
            };

            var wifiSettingsBox = new GroupBox
            {
                Text = "Connection Options",
                Padding = new Padding(12),
                Content = new StackLayout
                {
                    Spacing = 8,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Items =
                    {
                        _preferUsbCheck,
                        _enableWifiCheck,
                        _autoDiscoveryCheck,
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Items = { _discoveredLabel, _forgetDeviceButton }
                        },
                        new TableLayout
                        {
                            Spacing = new Size(10, 6),
                            Rows =
                            {
                                new TableRow(new Label { Text = "Manual Phone IP (optional):", VerticalAlignment = VerticalAlignment.Center }, _manualHostBox),
                                new TableRow(new Label { Text = "Port:", VerticalAlignment = VerticalAlignment.Center }, _portBox)
                            }
                        }
                    }
                }
            };

            var guideBox = new GroupBox
            {
                Text = "Connection Guide",
                Padding = new Padding(12),
                Content = new StackLayout
                {
                    Spacing = 6,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Items =
                    {
                        new Label
                        {
                            Text = "• USB Mode (Recommended, Lowest Latency):\n" +
                                   "  1. Connect your Android phone to PC with a USB data cable.\n" +
                                   "  2. Enable 'USB Debugging' in Developer Options on your phone.\n" +
                                   "  3. Open tabletizer app — OpenTabletDriver forwards port via ADB automatically.\n\n" +
                                   "• Wi-Fi Mode (Wireless):\n" +
                                   "  1. Connect phone and PC to the same Wi-Fi network.\n" +
                                   "  2. Open tabletizer app — UDP auto-discovery will pair automatically.\n" +
                                   "  (If network isolates multicast, enter phone IP in 'Manual Phone IP' above).",
                            TextColor = Colors.DimGray
                        }
                    }
                }
            };

            var buttonsLayout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Items =
                {
                    _saveButton,
                    _reconnectButton
                }
            };

            var contentLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Padding = new Padding(15),
                Spacing = 14,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    connectionBox,
                    wifiSettingsBox,
                    buttonsLayout,
                    guideBox
                }
            };

            Content = new Scrollable
            {
                Content = contentLayout,
                Border = BorderType.None
            };

            App.Driver.TabletsChanged += OnDriverTabletsChanged;
            UnLoad += (s, e) => App.Driver.TabletsChanged -= OnDriverTabletsChanged;
            _forgetDeviceButton.Enabled = !string.IsNullOrWhiteSpace(config.LastDiscoveredWifiHost);
            RefreshStatus();
        }

        private void OnDriverTabletsChanged(object? s, IEnumerable<TabletReference> e)
        {
            Application.Instance.AsyncInvoke(RefreshStatus);
        }

        public async void RefreshStatus()
        {
            if (!App.Driver.IsConnected)
            {
                _statusLabel.Text = "Daemon disconnected";
                _statusLabel.TextColor = Colors.Red;
                return;
            }

            try
            {
                var devices = await App.Driver.Instance.GetDevices();
                var tabletizerDevice = devices.FirstOrDefault(d =>
                    !string.IsNullOrEmpty(d.DevicePath) &&
                    d.DevicePath.StartsWith("tabletizer://", StringComparison.OrdinalIgnoreCase));

                if (tabletizerDevice != null && Uri.TryCreate(tabletizerDevice.DevicePath, UriKind.Absolute, out var uri))
                {
                    if (uri.Host == "127.0.0.1" || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    {
                        _statusLabel.Text = "Connected via USB (ADB tunnel, ~0ms latency)";
                        _statusLabel.TextColor = Colors.DarkGreen;
                    }
                    else
                    {
                        _statusLabel.Text = $"Connected via Wi-Fi ({uri.Host}:{uri.Port})";
                        _statusLabel.TextColor = Colors.DarkGreen;
                    }
                    return;
                }

                var config = TabletizerConfig.Instance;
                const string retryContext = "USB/ADB is tried before Wi-Fi targets";
                string wifiContext = !config.EnableWifi
                    ? " Wi-Fi is disabled."
                    : !config.AutoDiscovery
                        ? " Wi-Fi auto-discovery is disabled; using the manual IP only."
                        : " Wi-Fi order: TABLETIZER_HOST, manual IP, then discovered phone.";
                _statusLabel.Text = $"Disconnected — retrying every 1.5 seconds. {retryContext}.{wifiContext}";
                _statusLabel.TextColor = Colors.DarkOrange;
            }
            catch
            {
                _statusLabel.Text = "Waiting for connection status...";
                _statusLabel.TextColor = Colors.Gray;
            }
        }
    }
}
