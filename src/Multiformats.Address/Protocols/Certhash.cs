using Multiformats.Base;

namespace Multiformats.Address.Protocols;

public class Certhash : MultiaddressProtocol
{
    public Certhash()
        : base("certhash", 466, -1)
    {
    }

    public Certhash(string value)
        : this()
    {
        Decode(value);
    }

    public byte[] Hash => Value is string value
        ? Multibase.Decode(value, out MultibaseEncoding _)
        : EmptyBuffer;

    public override void Decode(string value)
    {
        byte[] hash = Multibase.Decode(value, out MultibaseEncoding _);
        Value = Multibase.Encode(MultibaseEncoding.Base64Url, hash);
    }

    public override void Decode(byte[] bytes)
    {
        Value = Multibase.Encode(MultibaseEncoding.Base64Url, bytes);
    }

    public override byte[] ToBytes() => Hash;
}
