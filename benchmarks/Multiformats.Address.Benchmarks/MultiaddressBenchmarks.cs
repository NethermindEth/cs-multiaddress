using BenchmarkDotNet.Attributes;
using Multiformats.Address.Protocols;

namespace Multiformats.Address.Benchmarks;

[MemoryDiagnoser]
public class MultiaddressBenchmarks
{
    private const string Ip4Text = "/ip4/1.2.3.4/tcp/4001/p2p/12D3KooW9tJMax94Lrqw7Y5Qw36viGQAS2gTEPQ5Wg1vTk7xPfQs";
    private const string Ip6Text = "/ip6/2001:db8::1/udp/4001/quic-v1/p2p/12D3KooW9tJMax94Lrqw7Y5Qw36viGQAS2gTEPQ5Wg1vTk7xPfQs";

    private Multiaddress _ip4;
    private Multiaddress _ip4Copy;
    private Multiaddress _ip6;
    private Multiaddress _ip6Other;
    private byte[] _ip4Bytes;

    [GlobalSetup]
    public void Setup()
    {
        _ip4 = Multiaddress.Decode(Ip4Text);
        _ip4Copy = Multiaddress.Decode(Ip4Text);
        _ip6 = Multiaddress.Decode(Ip6Text);
        _ip6Other = Multiaddress.Decode(Ip6Text.Replace("4001", "4002"));
        _ip4Bytes = _ip4.ToBytes();
        _ip4Copy.ToBytes();
        _ip6.ToBytes();
        _ip6Other.ToBytes();
    }

    [Benchmark]
    public byte[] ToBytesIp4() => _ip4.ToBytes();

    [Benchmark]
    public byte[] ToBytesIp6() => _ip6.ToBytes();

    [Benchmark]
    public bool EqualsIp4() => _ip4.Equals(_ip4Copy);

    [Benchmark]
    public int CompareToIp6() => _ip6.CompareTo(_ip6Other);

    [Benchmark]
    public Multiaddress DecodeBytesIp4() => Multiaddress.Decode(_ip4Bytes);

    [Benchmark]
    public Multiaddress DecodeStringIp4() => Multiaddress.Decode(Ip4Text);

    [Benchmark]
    public bool HasTcp() => _ip4.Has<TCP>();
}
