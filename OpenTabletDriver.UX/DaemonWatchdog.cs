using System;
using System.Diagnostics;
using System.IO;
using OpenTabletDriver.Interop;
using OpenTabletDriver.Plugin;

namespace OpenTabletDriver.UX
{
    public sealed class DaemonWatchdog : IDisposable
    {
        public event EventHandler? DaemonExited;

        private Process? daemonProcess;

        private readonly static ProcessStartInfo startInfo = SystemInterop.CurrentPlatform switch
        {
            PluginPlatform.Windows => new ProcessStartInfo
            {
                FileName = Path.Join(AppContext.BaseDirectory, "OpenTabletDriver.Daemon.exe"),
                Arguments = $"--appdata \"{Path.Join(AppContext.BaseDirectory, "userdata")}\"",
                WorkingDirectory = AppContext.BaseDirectory,
                CreateNoWindow = true
            },
            PluginPlatform.MacOS => new ProcessStartInfo
            {
                FileName = Path.Join(AppContext.BaseDirectory, "OpenTabletDriver.Daemon"),
                Arguments = $"--appdata \"{Path.Join(AppContext.BaseDirectory, "userdata")}\"",
                WorkingDirectory = AppContext.BaseDirectory
            },
            _ => new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{Path.Join(AppContext.BaseDirectory, "OpenTabletDriver.Daemon.dll")}\" --appdata \"{Path.Join(AppContext.BaseDirectory, "userdata")}\"",
                WorkingDirectory = AppContext.BaseDirectory
            }
        };

        public static bool CanExecute =>
            File.Exists(startInfo.FileName) ||
            File.Exists(startInfo.Arguments);

        public void Start()
        {
            this.daemonProcess = new Process()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            this.daemonProcess.Exited += (_, e) =>
            {
                DaemonExited?.Invoke(this, EventArgs.Empty);
            };
            this.daemonProcess.Start();
        }

        public void Stop()
        {
            DaemonExited = null; // unregister all event handlers
            this.daemonProcess?.Kill();
            this.daemonProcess?.Dispose();
            this.daemonProcess = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
