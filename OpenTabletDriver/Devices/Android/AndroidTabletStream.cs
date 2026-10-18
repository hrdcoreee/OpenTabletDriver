using System;
using System.IO;
using System.Net.Sockets;
using OpenTabletDriver.Plugin.Devices;

namespace OpenTabletDriver.Devices.Android
{
    public sealed class AndroidTabletStream : IDeviceEndpointStream
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly AndroidTabletEndpoint _endpoint;
        private readonly byte[] _buffer = new byte[12];
        private bool _isDisposed;

        public AndroidTabletStream(TcpClient client, AndroidTabletEndpoint endpoint)
        {
            _client = client;
            _endpoint = endpoint;
            _stream = client.GetStream();
        }

        public byte[] Read()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AndroidTabletStream));

            while (true)
            {
                int totalRead = 0;
                while (totalRead < _buffer.Length)
                {
                    int read;
                    try
                    {
                        read = _stream.Read(_buffer, totalRead, _buffer.Length - totalRead);
                    }
                    catch (Exception ex)
                    {
                        _endpoint.Disconnect();
                        throw new IOException("Android tablet socket read failed.", ex);
                    }

                    if (read <= 0)
                    {
                        _endpoint.Disconnect();
                        throw new IOException("Android tablet connection closed by remote host.");
                    }

                    totalRead += read;
                }

                // Configuration sync packet (Report ID = 2)
                if (_buffer[0] == 2)
                {
                    int ratioOrdinal = _buffer[1];
                    int scalePercent = _buffer[2];
                    int positionOrdinal = _buffer[3];
                    float widthMm = BitConverter.ToInt16(_buffer, 4) / 10f;
                    float heightMm = BitConverter.ToInt16(_buffer, 6) / 10f;
                    float posXMm = BitConverter.ToInt16(_buffer, 8) / 10f;
                    float posYMm = BitConverter.ToInt16(_buffer, 10) / 10f;

                    AndroidTabletHub.ApplyTabletAreaSync(widthMm, heightMm, posXMm, posYMm, ratioOrdinal, scalePercent);
                    continue;
                }

                var report = new byte[_buffer.Length];
                Array.Copy(_buffer, report, _buffer.Length);
                return report;
            }
        }

        public void Write(byte[] buffer)
        {
        }

        public void GetFeature(byte[] buffer)
        {
        }

        public void SetFeature(byte[] buffer)
        {
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            try
            {
                _stream.Dispose();
                _client.Dispose();
            }
            catch
            {
            }

            GC.SuppressFinalize(this);
        }
    }
}
