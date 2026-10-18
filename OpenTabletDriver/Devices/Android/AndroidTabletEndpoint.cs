using System;
using System.Collections.Generic;
using System.Net.Sockets;
using OpenTabletDriver.Plugin.Devices;

namespace OpenTabletDriver.Devices.Android
{
    public sealed class AndroidTabletEndpoint : IDeviceEndpoint
    {
        private readonly AndroidTabletHub _hub;
        private readonly TcpClient _client;
        private bool _isDisconnected;

        public AndroidTabletEndpoint(AndroidTabletHub hub, TcpClient client, string host, int port)
        {
            _hub = hub;
            _client = client;
            DevicePath = $"tabletizer://{host}:{port}";
            SerialNumber = $"{host}:{port}";
        }

        public AndroidTabletHub Hub => _hub;

        public int ProductID => 1500;
        public int VendorID => 5824;
        public int InputReportLength => 12;
        public int OutputReportLength => 0;
        public int FeatureReportLength => 0;

        public string? Manufacturer => "tabletizer";
        public string? ProductName => "Android Tablet";
        public string? FriendlyName => "tabletizer Android Tablet";
        public string? SerialNumber { get; }
        public string DevicePath { get; }

        public bool CanOpen => !_isDisconnected && _client.Connected;

        public IDictionary<string, string> DeviceAttributes { get; } = new Dictionary<string, string>
        {
            ["Transport"] = "TCP"
        };

        public void OpenAndSendFeatureInit(byte[] initData)
        {
        }

        public void OpenAndSendOutputInit(byte[] initData)
        {
        }

        public IDeviceEndpointStream Open()
        {
            return new AndroidTabletStream(_client, this);
        }

        public string? GetDeviceString(byte index)
        {
            return null;
        }

        private readonly object _writeLock = new();

        public void SendAreaConfig(float widthMm, float heightMm, float posXMm, float posYMm)
        {
            try
            {
                if (_isDisconnected || !_client.Connected)
                    return;

                var stream = _client.GetStream();
                if (stream == null || !stream.CanWrite)
                    return;

                byte[] packet = new byte[12];
                packet[0] = 2; // Report ID 2: Config Sync (PC -> Phone)

                short w = (short)Math.Clamp((int)Math.Round(widthMm * 10f), 1, short.MaxValue);
                short h = (short)Math.Clamp((int)Math.Round(heightMm * 10f), 1, short.MaxValue);
                short x = (short)Math.Clamp((int)Math.Round(posXMm * 10f), 0, short.MaxValue);
                short y = (short)Math.Clamp((int)Math.Round(posYMm * 10f), 0, short.MaxValue);

                packet[4] = (byte)(w & 0xFF);
                packet[5] = (byte)((w >> 8) & 0xFF);
                packet[6] = (byte)(h & 0xFF);
                packet[7] = (byte)((h >> 8) & 0xFF);
                packet[8] = (byte)(x & 0xFF);
                packet[9] = (byte)((x >> 8) & 0xFF);
                packet[10] = (byte)(y & 0xFF);
                packet[11] = (byte)((y >> 8) & 0xFF);

                lock (_writeLock)
                {
                    stream.Write(packet, 0, packet.Length);
                    stream.Flush();
                }
            }
            catch
            {
            }
        }

        public void Disconnect()
        {
            if (_isDisconnected)
                return;

            _isDisconnected = true;
            _hub.OnEndpointDisconnected(this);
        }
    }
}
