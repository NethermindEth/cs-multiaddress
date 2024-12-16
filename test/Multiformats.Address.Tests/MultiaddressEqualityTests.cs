using Xunit;

namespace Multiformats.Address.Tests;

public class MultiaddressEqualityTests
{
    [Fact]
    public void TestEqual()
    {
        var m1 = Multiaddress.Decode("/ip4/127.0.0.1/tcp/1234");
        var m2 = Multiaddress.Decode("/ip4/127.0.0.1/tcp/1234");

        Assert.Equal(m1, m2);
        Assert.Equal(m2, m1);
        Assert.True(m1.CompareTo(m2) == 0);
        Assert.True(m2.CompareTo(m1) == 0);
    }

    [Fact]
    public void TestNotEqual()
    {
        var m1 = Multiaddress.Decode("/ip4/127.0.0.1/udp/1234");
        var m2 = Multiaddress.Decode("/ip4/127.0.0.1/tcp/1234");

        Assert.NotEqual(m1, m2);
        Assert.NotEqual(m2, m1);
        Assert.True(m1.CompareTo(m2) * m2.CompareTo(m1) < 0);
    }
}
