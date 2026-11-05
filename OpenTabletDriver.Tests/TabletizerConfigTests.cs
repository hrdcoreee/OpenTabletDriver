using OpenTabletDriver.Devices.Android;
using Xunit;

namespace OpenTabletDriver.Tests
{
    public class TabletizerConfigTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("192.168.1.50")]
        [InlineData("2001:db8::1")]
        [InlineData("phone.local")]
        public void WifiHostValidation_Accepts_AutomaticIpAndHostNames(string host)
        {
            Assert.True(TabletizerConfig.TryValidateWifiHost(host, out var error));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("127.0.0.1")]
        [InlineData("not a host")]
        public void WifiHostValidation_Rejects_InvalidOrLoopbackHosts(string host)
        {
            Assert.False(TabletizerConfig.TryValidateWifiHost(host, out var error));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("1", 1)]
        [InlineData("3240", 3240)]
        [InlineData("65535", 65535)]
        public void PortValidation_Accepts_ValidTcpPorts(string text, int expectedPort)
        {
            Assert.True(TabletizerConfig.TryValidatePort(text, out var port, out var error));
            Assert.Equal(expectedPort, port);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("65536")]
        [InlineData("not-a-port")]
        public void PortValidation_Rejects_InvalidTcpPorts(string text)
        {
            Assert.False(TabletizerConfig.TryValidatePort(text, out _, out var error));
            Assert.NotNull(error);
        }
    }
}