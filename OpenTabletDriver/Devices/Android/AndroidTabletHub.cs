using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Devices;

namespace OpenTabletDriver.Devices.Android
{
    [DeviceHub]
    public sealed class AndroidTabletHub : IDeviceHub, IDisposable
    {
        private const string DEFAULT_HOST = "127.0.0.1";
        private const int DEFAULT_PORT = 3240;
        private const int BEACON_PORT = 3242;

        public static event Action<float, float, float, float, int, int>? TabletAreaSyncRequested;
        public static AndroidTabletHub? Instance { get; private set; }

        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _wakeSignal = new(0, 1);
        private readonly object _sync = new();

        private List<IDeviceEndpoint> _oldDevices = [];
        private List<IDeviceEndpoint> _currentDevices = [];
        private AndroidTabletEndpoint? _activeEndpoint;
        private string? _lastDiscoveredBeaconIp;
        private bool _isDisposed;

        public AndroidTabletHub()
        {
            Instance = this;
            TabletizerConfig.Changed += SignalReconnect;
            StartBeaconListener();
            Task.Run(ConnectionWorkerAsync);
        }

        public AndroidTabletEndpoint? ActiveEndpoint
        {
            get
            {
                lock (_sync)
                {
                    return _activeEndpoint;
                }
            }
        }

        public static void SendAreaConfigToActiveDevice(float widthMm, float heightMm, float posXMm, float posYMm)
        {
            Instance?.ActiveEndpoint?.SendAreaConfig(widthMm, heightMm, posXMm, posYMm);
        }

        private void SignalReconnect()
        {
            if (_wakeSignal.CurrentCount == 0)
            {
                try { _wakeSignal.Release(); } catch { }
            }
        }

        public static void ApplyTabletAreaSync(float widthMm, float heightMm, float posXMm, float posYMm, int ratio, int scale)
        {
            TabletAreaSyncRequested?.Invoke(widthMm, heightMm, posXMm, posYMm, ratio, scale);
        }

        public event EventHandler<DevicesChangedEventArgs>? DevicesChanged;

        public IEnumerable<IDeviceEndpoint> GetDevices()
        {
            lock (_sync)
            {
                return _currentDevices.ToArray();
            }
        }

        private string? _lastDiagnosticMessage;
        private DateTime _lastLogTimestamp = DateTime.MinValue;

        private void LogDiagnostic(string message, LogLevel level = LogLevel.Info, int throttleSeconds = 20)
        {
            var now = DateTime.UtcNow;
            if (message != _lastDiagnosticMessage || (now - _lastLogTimestamp).TotalSeconds >= throttleSeconds)
            {
                _lastDiagnosticMessage = message;
                _lastLogTimestamp = now;
                Log.Write(nameof(AndroidTabletHub), message, level);
            }
        }

        public void OnEndpointDisconnected(AndroidTabletEndpoint endpoint)
        {
            lock (_sync)
            {
                if (_activeEndpoint == endpoint)
                {
                    Log.Write(nameof(AndroidTabletHub), $"Android tablet disconnected ({endpoint.SerialNumber}). Searching for connection...", LogLevel.Warning);
                    _oldDevices = _currentDevices;
                    _currentDevices = [];
                    _activeEndpoint = null;
                    DevicesChanged?.Invoke(this, new DevicesChangedEventArgs(_oldDevices, _currentDevices));
                }
            }
        }

        private void StartBeaconListener()
        {
            Task.Run(async () =>
            {
                UdpClient? udpClient = null;
                try
                {
                    udpClient = new UdpClient();
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, BEACON_PORT));

                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var result = await udpClient.ReceiveAsync(_cts.Token);
                        string message = System.Text.Encoding.UTF8.GetString(result.Buffer);
                        if (message.StartsWith("TABLETIZER_BEACON"))
                        {
                            var ip = result.RemoteEndPoint.Address;
                            if (ip.IsIPv4MappedToIPv6)
                                ip = ip.MapToIPv4();
                            string ipStr = ip.ToString();

                            if (_lastDiscoveredBeaconIp != ipStr)
                            {
                                _lastDiscoveredBeaconIp = ipStr;
                                Log.Write(nameof(AndroidTabletHub), $"Discovered tabletizer via Wi-Fi beacon at {ipStr}:{DEFAULT_PORT}", LogLevel.Info);
                                TabletizerConfig.Instance.LastDiscoveredWifiHost = ipStr;
                                TabletizerConfig.Instance.Save();
                                SignalReconnect();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Log.Write(nameof(AndroidTabletHub), $"Beacon listener ended: {ex.Message}", LogLevel.Debug);
                }
                finally
                {
                    udpClient?.Dispose();
                }
            }, _cts.Token);
        }

        private async Task ConnectionWorkerAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                bool isConnected;
                lock (_sync)
                {
                    isConnected = _activeEndpoint != null;
                }

                var config = TabletizerConfig.Instance;
                int port = config.Port > 0 ? config.Port : DEFAULT_PORT;
                if (int.TryParse(Environment.GetEnvironmentVariable("TABLETIZER_PORT"), out int pEnv))
                    port = pEnv;

                // If currently connected over Wi-Fi, but PreferUsb is enabled, check if USB ADB became available
                if (isConnected && config.PreferUsb)
                {
                    bool isWifiConnected = false;
                    lock (_sync)
                    {
                        if (_activeEndpoint != null && !_activeEndpoint.DevicePath.Contains("127.0.0.1"))
                            isWifiConnected = true;
                    }

                    if (isWifiConnected)
                    {
                        var adb = FindAdbPath();
                        if (!string.IsNullOrEmpty(adb))
                        {
                            var (adbActive, _) = TryAdbForward(adb, port);
                            if (adbActive)
                            {
                                Log.Write(nameof(AndroidTabletHub), "USB connection detected! Upgrading from Wi-Fi to zero-latency USB connection...", LogLevel.Info);
                                lock (_sync)
                                {
                                    _activeEndpoint?.Disconnect();
                                }
                                isConnected = false;
                            }
                        }
                    }
                }

                if (!isConnected)
                {
                    var candidates = new List<(string Host, int Port, string Mode)>();

                    // 1. USB Mode: check ADB
                    var adbPath = FindAdbPath() ?? await EnsureAdbInstalledAsync(_cts.Token);
                    if (!string.IsNullOrEmpty(adbPath))
                    {
                        var (adbActive, _) = TryAdbForward(adbPath, port);
                        if (adbActive)
                        {
                            candidates.Add(("127.0.0.1", port, "USB (ADB tunnel)"));
                        }
                    }

                    // 2. Wi-Fi (only if EnableWifi is true)
                    if (config.EnableWifi)
                    {
                        // Wi-Fi: Environment variable override (if set)
                        string? envHost = Environment.GetEnvironmentVariable("TABLETIZER_HOST");
                        if (!string.IsNullOrWhiteSpace(envHost) && envHost != "127.0.0.1" && envHost != "localhost")
                        {
                            candidates.Add((envHost, port, "Wi-Fi (TABLETIZER_HOST)"));
                        }

                        // Wi-Fi: Manual IP from config dialog
                        if (!string.IsNullOrWhiteSpace(config.ManualWifiHost) && config.ManualWifiHost != "127.0.0.1" && config.ManualWifiHost != "localhost")
                        {
                            if (!candidates.Any(c => c.Host == config.ManualWifiHost))
                                candidates.Add((config.ManualWifiHost, port, "Wi-Fi (Manual IP)"));
                        }

                        // Wi-Fi: Auto-discovery via active beacon or saved host
                        if (config.AutoDiscovery)
                        {
                            if (!string.IsNullOrWhiteSpace(_lastDiscoveredBeaconIp) && !candidates.Any(c => c.Host == _lastDiscoveredBeaconIp))
                            {
                                candidates.Add((_lastDiscoveredBeaconIp, port, "Wi-Fi (Auto-discovered)"));
                            }
                            else if (!string.IsNullOrWhiteSpace(config.LastDiscoveredWifiHost) && !candidates.Any(c => c.Host == config.LastDiscoveredWifiHost))
                            {
                                candidates.Add((config.LastDiscoveredWifiHost, port, "Wi-Fi (Previously Discovered)"));
                            }
                        }
                    }

                    // 3. Fallback local loopback if nothing else is available
                    if (candidates.Count == 0)
                    {
                        candidates.Add(("127.0.0.1", port, "USB (127.0.0.1)"));
                    }

                    foreach (var (host, candPort, mode) in candidates)
                    {
                        try
                        {
                            var client = new TcpClient { NoDelay = true, ReceiveBufferSize = 8192 };
                            using var connectCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(700));
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, connectCts.Token);

                            await client.ConnectAsync(host, candPort, linkedCts.Token);

                            if (client.Connected)
                            {
                                lock (_sync)
                                {
                                    var endpoint = new AndroidTabletEndpoint(this, client, host, candPort);
                                    _activeEndpoint = endpoint;
                                    _oldDevices = _currentDevices;
                                    _currentDevices = [endpoint];
                                    DevicesChanged?.Invoke(this, new DevicesChangedEventArgs(_oldDevices, _currentDevices));
                                }

                                _lastDiagnosticMessage = null;
                                Log.Write(nameof(AndroidTabletHub), $"Successfully connected to tabletizer at {host}:{candPort} via {mode}!", LogLevel.Info);
                                break;
                            }
                        }
                        catch
                        {
                            // Try next candidate
                        }
                    }

                    lock (_sync)
                    {
                        if (_activeEndpoint == null)
                        {
                            string candidateSummary = string.Join(", ", candidates.Select(c => $"{c.Host} ({c.Mode})"));
                            LogDiagnostic($"Waiting for tabletizer connection. Targets: [{candidateSummary}]. Ensure app is open.", LogLevel.Debug, 15);
                        }
                    }
                }

                try
                {
                    await _wakeSignal.WaitAsync(1500, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static (bool success, string diagnostic) TryAdbForward(string adbPath, int port)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = $"forward tcp:{port} tcp:{port}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit(1000);

                if (process.ExitCode == 0)
                {
                    return (true, $"ADB port forward active (tcp:{port} -> tcp:{port})");
                }

                if (error.Contains("no devices", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "ADB: No Android device detected via USB. Check cable and ensure 'USB Debugging' is enabled in Developer Options.");
                }

                if (error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "ADB: Phone is unauthorized! Unlock phone and tap 'Allow' on the 'Allow USB debugging' prompt.");
                }

                if (error.Contains("offline", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "ADB: Device is offline. Reconnect USB cable or toggle 'USB Debugging' off and on.");
                }

                if (!string.IsNullOrEmpty(error))
                {
                    return (false, $"ADB error: {error}");
                }

                return (false, $"ADB exited with code {process.ExitCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to run ADB: {ex.Message}");
            }
        }

        private static string? FindAdbPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string userdataDir = Path.Combine(baseDir, "userdata");

            var candidates = new List<string?>
            {
                Path.Combine(baseDir, "platform-tools", "adb.exe"),
                Path.Combine(baseDir, "adb.exe"),
                Path.Combine(userdataDir, "platform-tools", "adb.exe"),
                Path.Combine(userdataDir, "platform-tools", "platform-tools", "adb.exe"),
                Path.Combine(userdataDir, "adb.exe"),
                Path.Combine(Environment.GetEnvironmentVariable("ANDROID_HOME") ?? string.Empty, "platform-tools", "adb.exe"),
                Path.Combine(Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT") ?? string.Empty, "platform-tools", "adb.exe"),
                @"D:\Android\Sdk\platform-tools\adb.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Android\platform-tools\adb.exe")
            };

            foreach (var path in candidates)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }

            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var full = Path.Combine(dir.Trim(), "adb.exe");
                        if (File.Exists(full))
                            return full;
                    }
                    catch { }
                }
            }

            return null;
        }

        private static bool _isDownloadingAdb;
        private static readonly object _downloadLock = new();

        private static async Task<string?> EnsureAdbInstalledAsync(CancellationToken ct)
        {
            var existing = FindAdbPath();
            if (!string.IsNullOrEmpty(existing))
                return existing;

            lock (_downloadLock)
            {
                if (_isDownloadingAdb)
                    return null;
                _isDownloadingAdb = true;
            }

            try
            {
                Log.Write(nameof(AndroidTabletHub), "Android platform-tools (adb) not found. Automatically downloading from Google...", LogLevel.Info);

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string tempZip = Path.Combine(Path.GetTempPath(), $"platform-tools-{Guid.NewGuid():N}.zip");
                const string url = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";

                using (var httpClient = new HttpClient())
                {
                    using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    response.EnsureSuccessStatusCode();

                    using var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs, ct);
                }

                // The zip contains a top-level "platform-tools/" folder
                ZipFile.ExtractToDirectory(tempZip, baseDir, overwriteFiles: true);

                try
                {
                    File.Delete(tempZip);
                }
                catch { }

                var adbPath = FindAdbPath();
                if (!string.IsNullOrEmpty(adbPath))
                {
                    Log.Write(nameof(AndroidTabletHub), $"Platform-tools successfully installed to {adbPath}! ADB is ready.", LogLevel.Info);
                    return adbPath;
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Write(nameof(AndroidTabletHub), $"Failed to auto-download Android platform-tools: {ex.Message}", LogLevel.Warning);
                return null;
            }
            finally
            {
                lock (_downloadLock)
                {
                    _isDownloadingAdb = false;
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            TabletizerConfig.Changed -= SignalReconnect;
            _cts.Cancel();
            _cts.Dispose();
            _wakeSignal.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
