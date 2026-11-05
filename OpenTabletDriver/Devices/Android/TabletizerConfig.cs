using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace OpenTabletDriver.Devices.Android
{
    public class TabletizerConfig
    {
        private static string ConfigPath
        {
            get
            {
                string userdataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata");
                if (!Directory.Exists(userdataDir))
                {
                    try { Directory.CreateDirectory(userdataDir); } catch { }
                }
                string localPath = Path.Combine(userdataDir, "tabletizer.json");

                // One-time migration from old AppData location if it exists and local doesn't
                if (!File.Exists(localPath))
                {
                    try
                    {
                        string oldAppData = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "OpenTabletDriver",
                            "tabletizer.json"
                        );
                        if (File.Exists(oldAppData))
                        {
                            File.Copy(oldAppData, localPath, overwrite: false);
                        }
                    }
                    catch { }
                }

                return localPath;
            }
        }

        private static TabletizerConfig? _instance;
        private static DateTime _lastLoadedTimestamp;
        private static readonly object _lock = new();

        public static TabletizerConfig Instance
        {
            get
            {
                lock (_lock)
                {
                    try
                    {
                        var lastWrite = File.Exists(ConfigPath) ? File.GetLastWriteTimeUtc(ConfigPath) : DateTime.MinValue;
                        if (_instance == null || lastWrite > _lastLoadedTimestamp)
                        {
                            _instance = Load();
                            _lastLoadedTimestamp = lastWrite;
                        }
                    }
                    catch
                    {
                        _instance ??= new TabletizerConfig();
                    }
                    return _instance;
                }
            }
        }

        public static event Action? Changed;

        public bool EnableWifi { get; set; } = true;
        public bool PreferUsb { get; set; } = true;
        public bool AutoDiscovery { get; set; } = true;
        public string? ManualWifiHost { get; set; } = string.Empty;
        public string? LastDiscoveredWifiHost { get; set; } = string.Empty;
        public int Port { get; set; } = 3240;

        public static bool TryValidateWifiHost(string? host, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(host))
                return true;

            host = host.Trim();
            if (IPAddress.TryParse(host, out var address))
            {
                if (IPAddress.IsLoopback(address))
                {
                    error = "Manual Phone IP must not be a loopback address. Leave it blank for USB/ADB.";
                    return false;
                }

                return true;
            }

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                error = "Enter a valid phone IP address or host name, or leave it blank for automatic discovery.";
                return false;
            }

            return true;
        }

        public static bool TryValidatePort(string? portText, out int port, out string? error)
        {
            error = null;
            if (!int.TryParse(portText?.Trim(), out port) || port is < 1 or > 65535)
            {
                error = "Port must be a number from 1 to 65535.";
                return false;
            }

            return true;
        }

        public static TabletizerConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var cfg = JsonConvert.DeserializeObject<TabletizerConfig>(json);
                    if (cfg != null)
                        return cfg;
                }
            }
            catch
            {
            }

            return new TabletizerConfig();
        }

        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(ConfigPath)!;
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                    File.WriteAllText(ConfigPath, json);
                    _lastLoadedTimestamp = File.GetLastWriteTimeUtc(ConfigPath);
                    Changed?.Invoke();
                }
                catch
                {
                }
            }
        }
    }
}
