using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using BinaryEncoding;
using Multiformats.Address.Protocols;
using Multiformats.Hash;

namespace Multiformats.Address;

public class Multiaddress : IEquatable<Multiaddress>, IComparable, IComparable<Multiaddress>
{
    static Multiaddress()
    {
        Setup<IP4>("ip4", 4, 32, false, ip =>
        {
            if (ip != null)
            {
                if (ip is IPAddress)
                    return new IP4((IPAddress)ip);
                else if (ip is string)
                    return new IP4((string)ip);
                else
                    throw new Exception($"Invalid IP4 address {ip}");
            }

            return new IP4();
        });
        Setup<IP6>("ip6", 41, 128, false, ip => ip != null ? new IP6((IPAddress)ip) : new IP6());
        Setup<TCP>("tcp", 6, 16, false, port => port != null ? new TCP((int)port) : new TCP());
        Setup<UDP>("udp", 17, 16, false, port => port != null ? new UDP((int)port) : new UDP());
        Setup<P2P>("p2p", 421, -1, false, address => address != null ? address is Multihash ? new P2P((Multihash)address) : new P2P((string)address) : new P2P());
        Setup<WebSocket>("ws", 477, 0, false, _ => new WebSocket());
        Setup<WebSocketSecure>("wss", 478, 0, false, _ => new WebSocketSecure());
        Setup<DCCP>("dccp", 33, 16, false, port => port != null ? new DCCP((int)port) : new DCCP());
        Setup<SCTP>("sctp", 132, 16, false, port => port != null ? new SCTP((int)port) : new SCTP());
        Setup<Unix>("unix", 400, -1, true, address => address != null ? new Unix((string)address) : new Unix());
        Setup<Onion>("onion", 444, 96, false, address => address != null ? new Onion((string)address) : new Onion());
        Setup<QUIC>("quic", 460, 0, false, _ => new QUIC());
        Setup<QUICv1>("quic-v1", 461, 0, false, _ => new QUICv1());
        Setup<WebrtcDirect>("webrtc-direct", 280, 0, false, _ => new WebrtcDirect());
        Setup<Webrtc>("webrtc", 281, 0, false, _ => new Webrtc());
        Setup<Certhash>("certhash", 466, -1, false, address => address != null ? new Certhash((string)address) : new Certhash());
        Setup<HTTP>("http", 480, 0, false, _ => new HTTP());
        Setup<HTTPS>("https", 443, 0, false, _ => new HTTPS());
        Setup<UTP>("utp", 301, 0, false, _ => new UTP());
        Setup<UDT>("udt", 302, 0, false, _ => new UDT());
        Setup<DNS>("dns", 53, -1, false, address => address != null ? new DNS((string)address) : new DNS());
        Setup<DNS4>("dns4", 54, -1, false, address => address != null ? new DNS4((string)address) : new DNS4());
        Setup<DNS6>("dns6", 55, -1, false, address => address != null ? new DNS6((string)address) : new DNS6());
        Setup<DnsAddr>("dnsaddr", 56, -1, false, address => address != null ? new DnsAddr((string)address) : new DnsAddr());
        Setup<P2PCircuit>("p2p-circuit", 290, 0, false, _ => new P2PCircuit());
        Setup<P2PWebRTCStar>("p2p-webrtc-star", 275, 0, false, _ => new P2PWebRTCStar());
        Setup<P2PWebRTCDirect>("p2p-webrtc-direct", 276, 0, false, _ => new P2PWebRTCStar());
        Setup<P2PWebSocketStar>("p2p-websocket-star", 479, 0, false, _ => new P2PWebSocketStar());
    }

    private class Protocol
    {
        public string Name { get; }
        public int Code { get; }
        public int Size { get; }
        public Func<object, MultiaddressProtocol> Factory { get; }
        public Type Type { get; }
        public bool Path { get; }

        public Protocol(string name, int code, int size, Type type, bool path, Func<object, MultiaddressProtocol> factory)
        {
            Name = name;
            Code = code;
            Size = size;
            Type = type;
            Path = path;
            Factory = factory;
        }

    }
    private const int StackBufferSize = 256;

    private static readonly Dictionary<string, Protocol> _protocolsByName = new Dictionary<string, Protocol>();
    private static readonly Dictionary<int, Protocol> _protocolsByCode = new Dictionary<int, Protocol>();
    private static readonly Dictionary<Type, Protocol> _protocolsByType = new Dictionary<Type, Protocol>();

    private static void Setup<TProtocol>(string name, int code, int size, bool path, Func<object, MultiaddressProtocol> factory)
        where TProtocol : MultiaddressProtocol
    {
        Protocol protocol = new Protocol(name, code, size, typeof(TProtocol), path, factory);
        _protocolsByName.Add(name, protocol);
        _protocolsByCode.Add(code, protocol);
        _protocolsByType.Add(protocol.Type, protocol);
    }

    private static Protocol FindByType(Type type) => _protocolsByType.TryGetValue(type, out Protocol protocol) ? protocol : null;
    private static Protocol FindByCode(int code) => _protocolsByCode.TryGetValue(code, out Protocol protocol) ? protocol : null;

    public List<MultiaddressProtocol> Protocols { get; init; }

    public Multiaddress()
    {
        Protocols = new List<MultiaddressProtocol>();
    }

    public Multiaddress Add<TProtocol>(object value)
        where TProtocol : MultiaddressProtocol
    {
        Protocol proto = FindByType(typeof(TProtocol));
        Protocols.Add(proto.Factory(value));
        return this;
    }

    public Multiaddress Add<TProtocol>() where TProtocol : MultiaddressProtocol => Add<TProtocol>(null);

    public Multiaddress Add(params MultiaddressProtocol[] protocols)
    {
        Protocols.AddRange(protocols);
        return this;
    }

    public TProtocol Get<TProtocol>() where TProtocol : MultiaddressProtocol
    {
        TProtocol found = null;
        bool hasMatch = false;
        foreach (MultiaddressProtocol protocol in Protocols)
        {
            if (protocol is TProtocol match)
            {
                if (hasMatch)
                    throw new InvalidOperationException("Sequence contains more than one element");

                found = match;
                hasMatch = true;
            }
        }

        return found;
    }

    public MultiaddressProtocol Get(Type multiprotocolType) => Protocols.Where(p => p.GetType() == multiprotocolType).SingleOrDefault();


    public int Remove<TProtocol>() where TProtocol : MultiaddressProtocol
    {
        var protocol = Get<TProtocol>();
        var protocolIndex = Protocols.IndexOf(protocol);
        if (protocolIndex != -1)
            Protocols.RemoveAt(protocolIndex);
        return protocolIndex;
    }

    public static Multiaddress Decode(string value)
    {
        string[] parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Multiaddress result = new Multiaddress();
        DecodeProtocols(parts, result.Protocols);
        return result;
    }

    public static Multiaddress Decode(byte[] bytes)
    {
        Multiaddress result = new Multiaddress();
        DecodeProtocols(bytes, result.Protocols);
        return result;
    }

    private static void DecodeProtocols(string[] parts, List<MultiaddressProtocol> target)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            if (!_protocolsByName.TryGetValue(parts[i], out Protocol definition))
                throw new NotSupportedException(parts[i]);

            MultiaddressProtocol protocol = definition.Factory(null);
            if (protocol.Size != 0)
            {
                if (i + 1 >= parts.Length)
                    throw new Exception("Required parameter not found");

                if (FindByCode(protocol.Code).Path)
                {
                    protocol.Decode(string.Join("/", parts.Slice(i + 1)));
                    i = parts.Length - 1;
                }
                else
                {
                    protocol.Decode(parts[++i]);
                }
            }

            target.Add(protocol);
        }
    }

    private static void DecodeProtocols(byte[] bytes, List<MultiaddressProtocol> target)
    {
        int offset = 0;
        while (offset < bytes.Length)
        {
            offset += ParseProtocolCode(bytes, offset, out ushort code);
            if (_protocolsByCode.TryGetValue(code, out Protocol definition))
            {
                MultiaddressProtocol protocol = definition.Factory(null);
                offset += DecodeProtocol(protocol, bytes, offset);
                target.Add(protocol);
            }
        }
    }

    private static int ParseProtocolCode(byte[] bytes, int offset, out ushort code) => Binary.Varint.Read(bytes, offset, out code);

    private static int DecodeProtocol(MultiaddressProtocol protocol, byte[] bytes, int offset)
    {
        int start = offset;
        int count = 0;
        if (protocol.Size > 0)
        {
            count = protocol.Size / 8;
        }
        else if (protocol.Size == -1)
        {
            uint proxy = 0;
            offset += Binary.Varint.Read(bytes, offset, out proxy);
            count = (int)proxy;
        }

        if (count > 0)
        {
            protocol.Decode(bytes.Slice(offset, count));
            offset += count;
        }

        return offset - start;
    }

    public override string ToString() => Protocols.Count > 0 ? "/" + string.Join("/", Protocols.SelectMany(ProtocolToStrings)) : string.Empty;

    private static IEnumerable<string> ProtocolToStrings(MultiaddressProtocol p)
    {
        yield return p.Name;
        if (p.Value != null)
            yield return p.ToString();
    }

    public byte[] ToBytes()
    {
        byte[][] values = null;
        try
        {
            int length = MeasureEncoding(ref values);
            if (length == 0)
                return Array.Empty<byte>();

            byte[] result = new byte[length];
            WriteEncoding(result, values);
            return result;
        }
        finally
        {
            ReturnValues(values);
        }
    }

    private static bool IsAddress(Type type) => type == typeof(IP4) || type == typeof(IP6);

    private static bool IsPort(Type type) => type == typeof(TCP) || type == typeof(UDP) || type == typeof(DCCP) || type == typeof(SCTP);

    private static int AddressLength(IPAddress address) => address.AddressFamily == AddressFamily.InterNetworkV6 ? 16 : 4;

    private int MeasureEncoding(ref byte[][] values)
    {
        List<MultiaddressProtocol> protocols = Protocols;
        int length = 0;
        for (int i = 0; i < protocols.Count; i++)
        {
            MultiaddressProtocol p = protocols[i];
            length += VarintLength((ulong)p.Code);
            if (p.Size == 0)
                continue;

            int valueLength;
            Type type = p.GetType();
            if (IsAddress(type))
            {
                valueLength = AddressLength(((IP)p).Address);
            }
            else if (IsPort(type))
            {
                valueLength = sizeof(ushort);
            }
            else if (type == typeof(P2P))
            {
                valueLength = p.ToBytes().Length;
            }
            else
            {
                values ??= ArrayPool<byte[]>.Shared.Rent(protocols.Count);
                byte[] bytes = p.ToBytes();
                values[i] = bytes;
                valueLength = bytes.Length;
            }

            if (p.Size < 0)
                length += VarintLength((ulong)valueLength);

            length += valueLength;
        }

        return length;
    }

    private void WriteEncoding(Span<byte> buffer, byte[][] values)
    {
        List<MultiaddressProtocol> protocols = Protocols;
        int offset = 0;
        for (int i = 0; i < protocols.Count; i++)
        {
            MultiaddressProtocol p = protocols[i];
            offset += WriteVarint(buffer.Slice(offset), (ulong)p.Code);
            if (p.Size == 0)
                continue;

            Type type = p.GetType();
            if (IsAddress(type))
            {
                IPAddress address = ((IP)p).Address;
                if (p.Size < 0)
                    offset += WriteVarint(buffer.Slice(offset), (ulong)AddressLength(address));

                address.TryWriteBytes(buffer.Slice(offset), out int written);
                offset += written;
            }
            else if (IsPort(type))
            {
                ushort port = (ushort)(int)p.Value;
                if (p.Size < 0)
                    offset += WriteVarint(buffer.Slice(offset), sizeof(ushort));

                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset), port);
                offset += sizeof(ushort);
            }
            else
            {
                byte[] bytes = type == typeof(P2P) ? p.ToBytes() : values[i];
                if (p.Size < 0)
                    offset += WriteVarint(buffer.Slice(offset), (ulong)bytes.Length);

                bytes.CopyTo(buffer.Slice(offset));
                offset += bytes.Length;
            }
        }
    }

    private static void ReturnValues(byte[][] values)
    {
        if (values != null)
            ArrayPool<byte[]>.Shared.Return(values, clearArray: true);
    }

    private static int VarintLength(ulong value)
    {
        int length = 1;
        while (value >= 0x80)
        {
            value >>= 7;
            length++;
        }

        return length;
    }

    private static int WriteVarint(Span<byte> buffer, ulong value)
    {
        int offset = 0;
        while (value >= 0x80)
        {
            buffer[offset++] = (byte)(value | 0x80);
            value >>= 7;
        }

        buffer[offset++] = (byte)value;
        return offset;
    }

    public IEnumerable<Multiaddress> Split() => Protocols.Select(p => new Multiaddress().Add(p));

    public static Multiaddress Join(IEnumerable<Multiaddress> addresses)
    {
        var result = new Multiaddress();
        foreach (var address in addresses)
        {
            result.Add(address.Protocols.ToArray());
        }
        return result;
    }

    public Multiaddress Encapsulate(Multiaddress address)
    {
        return new Multiaddress()
            .Add(Protocols.Concat(address.Protocols).ToArray());
    }

    public Multiaddress Decapsulate(Multiaddress address)
    {
        return new Multiaddress()
            .Add(Protocols.TakeWhile(p => !address.Protocols.Any(p.Equals)).ToArray());
    }

    public override bool Equals(object obj) => CompareTo(obj) == 0;
    public bool Equals(Multiaddress other) => CompareTo(other) == 0;
    public static bool operator ==(Multiaddress b1, Multiaddress b2) => b1 is null ? b2 is null : b1.Equals(b2);
    public static bool operator !=(Multiaddress b1, Multiaddress b2) => !(b1 == b2);


    public static implicit operator Multiaddress(string value)
    {
        return Decode(value);
    }

    public bool Has<T>() where T : MultiaddressProtocol
    {
        foreach (MultiaddressProtocol protocol in Protocols)
        {
            if (protocol is T)
                return true;
        }

        return false;
    }

    public Multiaddress ReplaceOrAdd<T>(object v) where T : MultiaddressProtocol
    {
        int at = Remove<T>();
        Protocol protocolDef = FindByType(typeof(T));

        if (at != -1)
        {
            Protocols.Insert(at, protocolDef.Factory(v));
        }
        else
        {
            Protocols.Add(protocolDef.Factory(v));
        }

        return this;
    }

    public Multiaddress Clone()
    {
        return new Multiaddress() { Protocols = Protocols.ToList() };
    }

    public Multiaddress Replace<TOld, TNew>(object v) where TOld : MultiaddressProtocol where TNew : MultiaddressProtocol
    {
        int at = Remove<TOld>();
        Protocol protocolDef = FindByType(typeof(TNew));

        if (at != -1)
        {
            Protocols.Insert(at, protocolDef.Factory(v));
        }
        else
        {
            Protocols.Add(protocolDef.Factory(v));
        }

        return this;
    }

    public int CompareTo(object obj) => obj is Multiaddress other ? CompareTo(other) : 1;

    public int CompareTo(Multiaddress other)
    {
        if (other is null)
            return 1;

        byte[][] values = null;
        byte[][] otherValues = null;
        byte[] rented = null;
        byte[] otherRented = null;
        try
        {
            int length = MeasureEncoding(ref values);
            Span<byte> buffer = length <= StackBufferSize
                ? stackalloc byte[StackBufferSize]
                : (rented = ArrayPool<byte>.Shared.Rent(length));
            buffer = buffer.Slice(0, length);
            WriteEncoding(buffer, values);

            int otherLength = other.MeasureEncoding(ref otherValues);
            Span<byte> otherBuffer = otherLength <= StackBufferSize
                ? stackalloc byte[StackBufferSize]
                : (otherRented = ArrayPool<byte>.Shared.Rent(otherLength));
            otherBuffer = otherBuffer.Slice(0, otherLength);
            other.WriteEncoding(otherBuffer, otherValues);

            return ((ReadOnlySpan<byte>)buffer).SequenceCompareTo(otherBuffer);
        }
        finally
        {
            ReturnValues(values);
            ReturnValues(otherValues);
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
            if (otherRented != null)
                ArrayPool<byte>.Shared.Return(otherRented);
        }
    }
}
