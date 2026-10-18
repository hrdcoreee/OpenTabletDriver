using System;
using System.IO;
using Newtonsoft.Json;

namespace OpenTabletDriver.Devices.Android
{
    public class TabletizerConfig
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenTabletDriver",
            "tabletizer.json"
        );

        private static TabletizerConfig? _instance;
        private static readonly object _lock = new();

        public static TabletizerConfig Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= Load();
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
                    Changed?.Invoke();
                }
                catch
                {
                }
            }
        }
    }
}
