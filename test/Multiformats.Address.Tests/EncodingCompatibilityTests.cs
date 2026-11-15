using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BinaryEncoding;
using Multiformats.Address.Protocols;
using Multiformats.Base;
using Multiformats.Hash;
using Xunit;

namespace Multiformats.Address.Tests;

public class EncodingCompatibilityTests
{
    private const string PeerId = "12D3KooW9tJMax94Lrqw7Y5Qw36viGQAS2gTEPQ5Wg1vTk7xPfQs";
    private const string LegacyPeerId = "QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSupNKC";

    public static IEnumerable<object[]> Vectors()
    {
        string certhash = Multibase.Encode(MultibaseEncoding.Base64Url, Enumerable.Range(1, 34).Select(i => (byte)i).ToArray());
        string[] vectors =
        {
            "/ip4/1.2.3.4",
            "/ip4/0.0.0.0",
            "/ip6/::1",
            "/ip6/2601:9:4f81:9700:803e:ca65:66e8:c21",
            "/onion/timaq4ygg2iegci7:1234",
            "/onion/timaq4ygg2iegci7:80/http",
            "/udp/0",
            "/tcp/0",
            "/sctp/0",
            "/dccp/1234",
            "/udp/65535",
            "/tcp/65535",
            $"/p2p/{LegacyPeerId}",
            $"/p2p/{PeerId}",
            "/udp/1234/sctp/1234",
            "/udp/1234/udt",
            "/udp/1234/utp",
            "/tcp/1234/http",
            "/tcp/1234/https",
            $"/p2p/{LegacyPeerId}/tcp/1234",
            "/ip4/127.0.0.1/udp/1234",
            "/ip4/127.0.0.1/tcp/1234",
            $"/ip4/127.0.0.1/tcp/4001/p2p/{PeerId}",
            $"/ip6/2001:db8::1/udp/4001/quic-v1/p2p/{PeerId}",
            $"/ip4/127.0.0.1/udp/4001/quic/p2p/{LegacyPeerId}",
            "/unix/a/b/c/d/e",
            "/unix/stdio",
            "/ip4/1.2.3.4/tcp/80/unix/a/b/c/d/e/f",
            "/dns/www.google.com",
            "/dns4/www.google.com",
            "/dns6/www.google.com",
            "/dnsaddr/bootstrap.libp2p.io",
            $"/p2p-circuit/p2p/{LegacyPeerId}",
            $"/ip4/1.2.3.4/tcp/4001/p2p/{PeerId}/p2p-circuit/p2p/{LegacyPeerId}",
            "/p2p-webrtc-star/dns/www.google.com",
            "/p2p-webrtc-direct/dns/www.google.com",
            "/p2p-websocket-star/dns/www.google.com",
            "/quic/dns/www.google.com",
            "/ws/dns/www.google.com",
            "/wss/dns/www.google.com",
            "/dns4/example.com/tcp/443/wss",
            "/http/dns/www.google.com",
            "/https/dns/www.google.com",
            "/ip4/127.0.0.1/udp/1234/webrtc",
            $"/ip4/127.0.0.1/udp/1234/webrtc-direct/certhash/{certhash}",
        };

        return vectors.Select(v => new object[] { v });
    }

    private static byte[] LegacyToBytes(Multiaddress address) => address.Protocols.SelectMany(LegacyEncodeProtocol).ToArray();

    private static IEnumerable<byte> LegacyEncodeProtocol(MultiaddressProtocol p)
    {
        byte[] code = Binary.Varint.GetBytes((ulong)p.Code);

        if (p.Size == 0)
            return code;

        byte[] bytes = p.ToBytes();

        if (p.Size > 0)
            return code.Concat(bytes);

        byte[] prefix = Binary.Varint.GetBytes((ulong)bytes.Length);

        return code.Concat(prefix).Concat(bytes);
    }

    private static int LegacyCompare(Multiaddress a, Multiaddress b) =>
        b is not null ? LegacyToBytes(a).AsSpan().SequenceCompareTo(LegacyToBytes(b).AsSpan()) : 1;

    [Theory]
    [MemberData(nameof(Vectors))]
    public void ToBytes_MatchesLegacyAndRoundTrips(string vector)
    {
        Multiaddress decoded = Multiaddress.Decode(vector);
        byte[] bytes = decoded.ToBytes();

        Assert.Equal(LegacyToBytes(decoded), bytes);

        Multiaddress fromBytes = Multiaddress.Decode(bytes);
        Assert.Equal(bytes, fromBytes.ToBytes());
        Assert.True(decoded.Equals(fromBytes));
        Assert.Equal(0, decoded.CompareTo(fromBytes));
    }

    [Fact]
    public void CompareTo_MatchesLegacyForAllPairs()
    {
        List<Multiaddress> addresses = Vectors().Select(v => Multiaddress.Decode((string)v[0])).ToList();
        addresses.Add(new Multiaddress());
        addresses.Add(new Multiaddress().Add<IP4>("1.2.3.4").Add<TCP>(1));
        addresses.Add(new Multiaddress().Add<IP4>("1.2.3.4").Add<TCP>(256));

        foreach (Multiaddress a in addresses)
        {
            Assert.Equal(1, a.CompareTo(null));
            foreach (Multiaddress b in addresses)
            {
                Assert.Equal(Math.Sign(LegacyCompare(a, b)), Math.Sign(a.CompareTo(b)));
                Assert.Equal(LegacyCompare(a, b) == 0, a.Equals(b));
            }
        }
    }

    [Fact]
    public void ToBytes_Empty_ReturnsEmpty()
    {
        Assert.Empty(new Multiaddress().ToBytes());
    }

    [Fact]
    public void ToBytes_LongAddress_MatchesLegacy()
    {
        Multiaddress address = new Multiaddress();
        for (int i = 0; i < 20; i++)
            address.Add<DNS>(new string('a', 40 + i)).Add<TCP>(i).Add<P2P>(PeerId);

        Assert.Equal(LegacyToBytes(address), address.ToBytes());
        Assert.Equal(0, address.CompareTo(Multiaddress.Decode(address.ToBytes())));
    }

    [Fact]
    public void Decode_SkipsUnknownCodes()
    {
        byte[] ip4 = Multiaddress.Decode("/ip4/1.2.3.4").ToBytes();
        byte[] bytes = new byte[] { 0x01 }.Concat(ip4).ToArray();

        Multiaddress decoded = Multiaddress.Decode(bytes);

        Assert.Equal("/ip4/1.2.3.4", decoded.ToString());
    }

    [Fact]
    public void Get_And_Has_KeepSemantics()
    {
        Multiaddress address = Multiaddress.Decode($"/ip4/1.2.3.4/tcp/80/p2p/{PeerId}");

        Assert.True(address.Has<TCP>());
        Assert.False(address.Has<UDP>());
        Assert.True(address.Has<IP>());
        Assert.Equal(80, address.Get<TCP>().Port);
        Assert.Null(address.Get<UDP>());
        Assert.Equal(IPAddress.Parse("1.2.3.4"), address.Get<IP>().Address);
        Assert.Equal(Multihash.FromB58String(PeerId), (Multihash)address.Get<P2P>().Value);

        Multiaddress twoPorts = Multiaddress.Decode("/tcp/1/tcp/2");
        Assert.Throws<InvalidOperationException>(() => twoPorts.Get<TCP>());
    }
}
